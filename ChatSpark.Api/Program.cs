using ChatSpark.Api.Endpoints;
using ChatSpark.Api.Hubs;
using ChatSpark.Application.Abstractions;
using ChatSpark.Infrastructure.Auth;
using ChatSpark.Infrastructure.Caching;
using ChatSpark.Infrastructure.Messaging;
using ChatSpark.Infrastructure.Persistence;
using ChatSpark.Shared.Dtos.Users;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

//Serilog

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithCorrelationIdHeader("X-Correlation-ID")
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}")
    .CreateLogger();
JwtSecurityTokenHandler.DefaultMapInboundClaims = false;



var builder = WebApplication.CreateBuilder(args);

Directory.CreateDirectory(Path.Combine(builder.Environment.WebRootPath ?? "wwwroot", "uploads"));

builder.Host.UseSerilog();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

//Database
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
            .UseSnakeCaseNamingConvention();
});

//Services
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()!;

builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();

builder.Services.AddSingleton<IDbConnectionFactory, NpgsqlConnectionFactory>();

Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")!));

builder.Services.AddSingleton<ICacheService, RedisCacheService>();

//RabbitMQ

builder.Services.AddSingleton<IEventPublisher, RabbitMqPublisher>();
builder.Services.AddHostedService<MessageSentConsumer>();


//Auth
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ClockSkew = TimeSpan.FromSeconds(30)  
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = ctx =>
            {
                Log.Warning("JWT auth failed: {Error}", ctx.Exception.Message);
                return Task.CompletedTask;
            },
            OnChallenge = ctx =>
            {
                Log.Warning("JWT challenge: {Error} {Description}", ctx.Error, ctx.ErrorDescription);
                return Task.CompletedTask;
            },
            OnMessageReceived = ctx =>
            {
                var accessToken = ctx.Request.Query["access_token"];
                var path = ctx.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    ctx.Token = accessToken;
                }

                Log.Information("JWT header present: {HasAuth}", ctx.Request.Headers.ContainsKey("Authorization"));
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

//SignalR
builder.Services.AddSignalR().AddStackExchangeRedis(builder.Configuration.GetConnectionString("Redis")!);





// Swagger
builder.Services.AddEndpointsApiExplorer();   

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseSerilogRequestLogging();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("Frontend");

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();



//Endpoints


app.MapGet("/me", (ClaimsPrincipal user) => Results.Ok(new
{
    Id = user.FindFirst("sub")?.Value,
    Email = user.FindFirst("email")?.Value
})).RequireAuthorization();

app.MapGet("/health/redis", async (IConnectionMultiplexer redis) =>
{
    var db = redis.GetDatabase();
    var pong = await db.PingAsync();
    return Results.Ok(new { redis = "up", latency = $"{pong.TotalMilliseconds}ms" });
});

app.MapGet("/health/rabbitmq", async (IEventPublisher publisher) =>
{
    return Results.Ok(new { rabbitmq = "up" });
});


app.MapAuthEndpoints();
app.MapWorkspaceEndpoints();
app.MapChannelEndpoints();

app.MapHub<ChatHub>("/hubs/chat");

app.MapMessageEndpoints();

// Profile endpoints
app.MapGet("/api/profile", async (ClaimsPrincipal principal, AppDbContext db) =>
{
    var userId = Guid.Parse(principal.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);
    var user = await db.Users.FindAsync(userId);
    if (user is null) return Results.NotFound();

    return Results.Ok(new ProfileResponse(
        user.Id, user.Email, user.DisplayName,
        user.AvatarUrl, user.Bio, user.WebsiteUrl, user.CreatedAt));
}).RequireAuthorization();

app.MapPatch("/api/profile", async (UpdateProfileRequest request, ClaimsPrincipal principal, AppDbContext db) =>
{
    var userId = Guid.Parse(principal.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);
    var user = await db.Users.FindAsync(userId);
    if (user is null) return Results.NotFound();

    if (!string.IsNullOrWhiteSpace(request.DisplayName))
        user.UpdateDisplayName(request.DisplayName);

    if (!string.IsNullOrWhiteSpace(request.AvatarUrl))
        user.UpdateAvatar(request.AvatarUrl);

    user.UpdateProfile(request.Bio, request.WebsiteUrl);

    await db.SaveChangesAsync();

    return Results.Ok(new ProfileResponse(
        user.Id, user.Email, user.DisplayName,
        user.AvatarUrl, user.Bio, user.WebsiteUrl, user.CreatedAt));
}).RequireAuthorization();


app.UseStaticFiles();

app.MapPost("/api/profile/avatar", async (IFormFile file, [FromServices] IWebHostEnvironment env) =>
{
    // 1. Validation: Type and Size
    var allowedTypes = new[] { "image/jpeg", "image/png" };
    if (!allowedTypes.Contains(file.ContentType))
    {
        return Results.BadRequest("Only JPEG and PNG images are allowed.");
    }

    if (file.Length > 2 * 1024 * 1024) 
    {
        return Results.BadRequest("File size must be under 2MB.");
    }

    var uploadsFolder = Path.Combine(env.WebRootPath, "uploads");
    if (!Directory.Exists(uploadsFolder))
    {
        Directory.CreateDirectory(uploadsFolder);
    }

    var uniqueName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
    var filePath = Path.Combine(uploadsFolder, uniqueName);

    using (var stream = new FileStream(filePath, FileMode.Create))
    {
        await file.CopyToAsync(stream);
    }


    var publicUrl = $"/uploads/{uniqueName}";

  

    return Results.Ok(new { avatarUrl = publicUrl });
})
.DisableAntiforgery().RequireAuthorization();
// Apply pending EF Core migrations on startup (needed for Docker)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();

