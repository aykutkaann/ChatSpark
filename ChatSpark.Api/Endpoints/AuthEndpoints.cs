using ChatSpark.Application.Abstractions;
using ChatSpark.Domain.Entities;
using ChatSpark.Infrastructure.Persistence;
using ChatSpark.Shared.Dtos.Auth;
using Microsoft.EntityFrameworkCore;

namespace ChatSpark.Api.Endpoints
{
    public static class AuthEndpoints
    {
        public static void MapAuthEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/api/auth").WithTags("Auth");

            group.MapPost("/register",
                async (
                    RegisterRequest request,
                    AppDbContext db,
                    IPasswordHasher passwordHasher,
                    ITokenService tokenService) =>
                {
                    if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
                        return Results.BadRequest("Invalid email.");
                    if (string.IsNullOrWhiteSpace(request.DisplayName))
                        return Results.BadRequest("Display name is required.");
                    if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
                        return Results.BadRequest("Password must be at least 8 characters.");

                    var existing = await db.Users.AnyAsync(u => u.Email == request.Email);
                    if (existing)
                        return Results.Conflict("Email already registered.");

                    var hashedPassword = passwordHasher.Hash(request.Password);

                    var user = User.Create(request.Email, request.DisplayName, hashedPassword);
                    await db.Users.AddAsync(user);

                    var (accessToken, accessExpires) = tokenService.GenerateAccessToken(user);
                    var (rawRefreshToken, tokenHash, refreshExpires) = tokenService.GenerateRefreshToken();

                    var refreshToken = RefreshToken.Create(user.Id, tokenHash, refreshExpires);
                    await db.RefreshTokens.AddAsync(refreshToken);

                    await db.SaveChangesAsync();

                    return Results.Ok(new AuthResponse(accessToken, rawRefreshToken, user.DisplayName, accessExpires));
                });

            group.MapPost("/login",
                async (
                    LoginRequest request,
                    AppDbContext db,
                    IPasswordHasher passwordHasher,
                    ITokenService tokenService) =>
                {
                    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

                    if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
                        return Results.Unauthorized();

                    var (accessToken, accessExpires) = tokenService.GenerateAccessToken(user);
                    var (rawRefreshToken, tokenHash, refreshExpires) = tokenService.GenerateRefreshToken();

                    var refreshToken = RefreshToken.Create(user.Id, tokenHash, refreshExpires);
                    await db.RefreshTokens.AddAsync(refreshToken);

                    await db.SaveChangesAsync();

                    return Results.Ok(new AuthResponse(accessToken, rawRefreshToken, user.DisplayName, accessExpires));
                });

            group.MapPost("/refresh",
                async (
                    RefreshRequest request,
                    AppDbContext db,
                    ITokenService tokenService) =>
                {
                    if (string.IsNullOrWhiteSpace(request.RefreshToken))
                        return Results.Unauthorized();

                    var incomingHash = tokenService.HashRefreshToken(request.RefreshToken);

                    var stored = await db.RefreshTokens
                        .FirstOrDefaultAsync(t => t.TokenHash == incomingHash);

                    if (stored is null)
                        return Results.Unauthorized();

                    if (!stored.IsActive)
                    {
                        var activeTokens = await db.RefreshTokens
                            .Where(t => t.UserId == stored.UserId && t.RevokedAt == null)
                            .ToListAsync();

                        foreach (var t in activeTokens)
                            t.Revoke();

                        await db.SaveChangesAsync();
                        return Results.Unauthorized();
                    }

                    var user = await db.Users.FindAsync(stored.UserId);
                    if (user is null)
                        return Results.Unauthorized();

                    var (accessToken, accessExpires) = tokenService.GenerateAccessToken(user);
                    var (newRaw, newHash, newRefreshExpires) = tokenService.GenerateRefreshToken();

                    var newRefreshToken = RefreshToken.Create(user.Id, newHash, newRefreshExpires);
                    await db.RefreshTokens.AddAsync(newRefreshToken);
                    stored.Revoke(replacedByTokenHash: newHash);

                    await db.SaveChangesAsync();

                    return Results.Ok(new AuthResponse(accessToken, newRaw, user.DisplayName, accessExpires));
                });
        }
    }
}
