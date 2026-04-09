using ChatSpark.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


//Database
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
            .UseSnakeCaseNamingConvention();
});


// Swagger
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


//Endpoints

app.MapGet("/health/db", async (AppDbContext context) =>
{
    var connection =  await context.Database.CanConnectAsync();

    if (connection)
    {
        return Results.Ok(new { database = "up" });
    }
    else 
    {
        return Results.Json(new { database = "down" }, statusCode: 500);
    }
});

app.Run();

