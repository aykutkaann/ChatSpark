using ChatSpark.Application.Abstractions;
using ChatSpark.Domain.Entities;
using ChatSpark.Domain.Enum;
using ChatSpark.Infrastructure.Persistence;
using ChatSpark.Shared.Dtos.Channels;
using Dapper;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;


namespace ChatSpark.Api.Endpoints
{
    public static class ChannelEndpoints
    {
        public static void MapChannelEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/api/workspaces/{workspaceId:guid}/channels").WithTags("Channels").RequireAuthorization();



            // Create Channel with workspaceId
            group.MapPost("/", async (
                Guid workspaceId,
                CreateChannelRequest request,
                ClaimsPrincipal principal,
                AppDbContext db,
                ICacheService service) =>
            {
                //Get userId
                var userId = Guid.Parse(principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);
                                                                                        
                //Check membership of workspace
                var membership = await db.WorkspaceMembers
                    .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId);


                if(membership == null || (membership.Role != Domain.Enum.Role.Admin && membership.Role != Domain.Enum.Role.Owner))
                {
                    return Results.Forbid();
                }

                //ChannelId must be unique
                var nameExists = await db.Channels.AnyAsync(c => c.WorkspaceId == workspaceId && c.Name.ToLower() == request.Name.ToLower());
                if (nameExists)
                    return Results.Conflict("This channel name is already taken");

                //Create channel with method
                var channel = Channel.Create( workspaceId, request.Name, request.IsPrivate);
                db.Channels.Add(channel);


                if (request.IsPrivate)
                {
                    var channelMember = ChannelMember.Create(channel.Id, userId);
                    db.ChannelMembers.Add(channelMember);
                }

                await db.SaveChangesAsync();

                //RAM cleaning for new channels to be shown 
                await service.RemoveByPrefixAsync($"channels:{workspaceId}:");


                return Results.Created($"/api/workspaces/{workspaceId}/channels/{channel.Id}", new ChannelResponse(
                    channel.Id,
                    channel.WorkspaceId,
                    channel.Name,
                    channel.IsPrivate,
                    channel.IsArchived,
                    channel.CreatedAt));
            });

            //GET channels with workspaceId
            group.MapGet("/", async (
                Guid workspaceId,
                IDbConnectionFactory connectionFactory,
                ClaimsPrincipal principal,
                AppDbContext db,
                ICacheService service) =>
            {
                var userId = Guid.Parse(principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);

                var isWorkspaceMember = await db.WorkspaceMembers
                        .AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId);

                if (!isWorkspaceMember)
                {
                    return Results.Forbid(); 
                }

                var cacheKey = $"channels:{workspaceId}:{userId}";

                var cachedChannels = await service.GetAsync<List<ChannelResponse>>(cacheKey);

                if(cachedChannels is not null)
                {
                    return Results.Ok(cachedChannels);
                }

                //DAPPER
                const string sql = @"
                                SELECT c.id, c.workspace_id, c.name, c.is_private, c.is_archived, c.created_at
                                FROM channels c
                                WHERE c.workspace_id = @WorkspaceId 
                                  AND c.is_archived = false 
                                  AND (
                                    c.is_private = false 
                                    OR EXISTS (
                                        SELECT 1 FROM channel_members cm 
                                        WHERE cm.channel_id = c.id AND cm.user_id = @UserId
                                    )
                                  )
                                ORDER BY c.name;";

                using var conn = await connectionFactory.CreateAsync();
                var channels = await conn.QueryAsync<ChannelResponse>(sql, new { WorkspaceId = workspaceId, UserId = userId });


                await service.SetAsync(cacheKey, channels, TimeSpan.FromMinutes(5));

                return Results.Ok(channels);
            });

            //Making a channel archived
            group.MapPost("/{channelId:guid}/archive", async (Guid channelId, AppDbContext db, ClaimsPrincipal principal, ICacheService service) =>
            {
                var userId = Guid.Parse(principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);

                var channel = await db.Channels.FindAsync(channelId);
                if (channel is null) return Results.NotFound();

                var isAuthorized = await db.WorkspaceMembers.AnyAsync(m =>
                                                                m.WorkspaceId == channel.WorkspaceId &&
                                                                m.UserId == userId &&
                                                                (m.Role == Domain.Enum.Role.Admin || m.Role == Domain.Enum.Role.Owner));

                if (!isAuthorized) return Results.Forbid();
                channel.Archive();

                await db.SaveChangesAsync();

                await service.RemoveByPrefixAsync($"channels:{channel.WorkspaceId}:");


                return Results.NoContent();


            });

            // Making a channel Unarchived
            group.MapPost("/{channelId:guid}/unarchive", async (Guid channelId, AppDbContext db, ClaimsPrincipal principal, ICacheService service) =>
            {
                var userId = Guid.Parse(principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);

                var channel = await db.Channels.FindAsync(channelId);
                if (channel is null) return Results.NotFound();

                var isAuthorized = await db.WorkspaceMembers.AnyAsync(m =>
                                                                m.WorkspaceId == channel.WorkspaceId &&
                                                                m.UserId == userId &&
                                                                (m.Role == Domain.Enum.Role.Admin || m.Role == Domain.Enum.Role.Owner));

                if (!isAuthorized) return Results.Forbid();

                channel.UnArchive();

                await db.SaveChangesAsync();

                await service.RemoveByPrefixAsync($"channels:{channel.WorkspaceId}:");


                return Results.NoContent();
            });


            //GET who is online right now
            group.MapGet("/{channelId:guid}/presence", async (Guid workspaceId, Guid channelId,
                AppDbContext db, ClaimsPrincipal principal, IConnectionMultiplexer redis) =>
            {
                var userId = Guid.Parse(principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);
                

                var isWorkspaceMember = await db.WorkspaceMembers.AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId);

                if (!isWorkspaceMember) return Results.Forbid();

                var channel = await db.Channels.FindAsync(channelId);

                if (channel is null) return Results.NotFound();

                if (channel.IsPrivate)
                {
                    var isChannelMember = await db.ChannelMembers.AnyAsync(m => m.ChannelId == channelId && m.UserId == userId);

                    if (!isChannelMember) return Results.Forbid();
                }


                var redisDb = redis.GetDatabase();
                var members = await redisDb.SetMembersAsync($"presence:{channelId}");

                var userIds = members.Select(m => Guid.Parse(m.ToString())).ToArray();

                return Results.Ok(userIds);


            });

            //GETs read notification
            group.MapGet("/{channelId:guid}/readreceipts", async (Guid workspaceId, Guid channelId,
                AppDbContext db, IConnectionMultiplexer redis, ClaimsPrincipal principal) =>
            {
                var userId = Guid.Parse(principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);

                var isWorkspaceMember = await db.WorkspaceMembers.AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId);

                if (!isWorkspaceMember) return Results.Forbid();

                var channel = await db.Channels.FindAsync(channelId);
                if (channel is null) return Results.NotFound();

                if (channel.IsPrivate)
                {
                    var isChannelMember = await db.ChannelMembers.AnyAsync(m => m.ChannelId == channelId && m.UserId == userId);

                    if (!isChannelMember) return Results.Forbid();
                }

                var redisDb =  redis.GetDatabase();

                var entires = await redisDb.HashGetAllAsync($"readreceipts:{channelId}");

                var receipts = entires.ToDictionary(e => Guid.Parse(e.Name.ToString()),
                                                    e => Guid.Parse(e.Value.ToString()));

                return Results.Ok(receipts);


            });

        }
    }
}
