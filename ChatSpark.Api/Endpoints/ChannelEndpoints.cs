using ChatSpark.Domain.Entities;
using ChatSpark.Domain.Enum;
using ChatSpark.Infrastructure.Persistence;
using ChatSpark.Shared.Dtos.Channels;
using Dapper;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


namespace ChatSpark.Api.Endpoints
{
    public static class ChannelEndpoints
    {
        public static void MapChannelEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/api/workspaces/{workspaceId:guid}/channels").WithTags("Channels").RequireAuthorization();


            group.MapPost("/", async (
                Guid workspaceId,
                CreateChannelRequest request,
                ClaimsPrincipal pricipal,
                AppDbContext db) =>
            {
                var userId = Guid.Parse(pricipal.FindFirst(ClaimTypes.NameIdentifier)!.Value);

                var membership = await db.WorkspaceMembers
                    .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId);


                if(membership == null || (membership.Role != Domain.Enum.Role.Admin && membership.Role != Role.Owner))
                {
                    return Results.Forbid();
                }


                var nameExists = await db.Channels.AnyAsync(c => c.WorkspaceId == workspaceId && c.Name == request.Name);
                if (nameExists)
                    return Results.Conflict("This channel name is already taken");

                var channel = Channel.Create( workspaceId, request.Name, request.IsPrivate);
                db.Channels.Add(channel);


                if (request.IsPrivate)
                {
                    var channelMember = ChannelMember.Create(channel.Id, userId);
                    db.ChannelMembers.Add(channelMember);
                }

                await db.SaveChangesAsync();

                return Results.Created($"/channels/{channel.Id}", new ChannelResponse(
                    channel.Id,
                    channel.WorkspaceId,
                    channel.Name,
                    channel.IsPrivate,
                    channel.IsArchived,
                    DateTime.UtcNow));
            });


            group.MapGet("/api/workspaces/{workspaceId:guid}/channels", async (
                Guid workspaceId,
                IDbConnectionFactory connectionFactory,
                ClaimsPrincipal userClaims) =>
            {
                var userId = Guid.Parse(userClaims.FindFirst(ClaimTypes.NameIdentifier)!.Value);

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

                return Results.Ok(channels);
            });

            group.MapPost("/{channelId:guid}/archive", async (Guid channelId, AppDbContext db, ClaimsPrincipal principal) =>
            {
                var userId = Guid.Parse(principal.FindFirst(ClaimTypes.NameIdentifier)!.Value);

                var channel = await db.Channels.FindAsync(channelId);
                if (channel is null) return Results.NotFound();

                var isAuthorized = await db.WorkspaceMembers.AnyAsync(m =>
                                                                m.WorkspaceId == channel.WorkspaceId &&
                                                                m.UserId == userId &&
                                                                (m.Role == Role.Admin || m.Role == Role.Owner));

                if (!isAuthorized) return Results.Forbid();
                channel.Archive();

                await db.SaveChangesAsync();

                return Results.NoContent();


            });

            group.MapPost("/{channelId:guid}/unarchive", async (Guid channelId, AppDbContext db, ClaimsPrincipal principal) =>
            {
                var userId = Guid.Parse(principal.FindFirst(ClaimTypes.NameIdentifier)!.Value);

                var channel = await db.Channels.FindAsync(channelId);
                if (channel is null) return Results.NotFound();

                var isAuthorized = await db.WorkspaceMembers.AnyAsync(m =>
                                                                m.WorkspaceId == channel.WorkspaceId &&
                                                                m.UserId == userId &&
                                                                (m.Role == Role.Admin || m.Role == Role.Owner));

                if (!isAuthorized) return Results.Forbid();

                channel.UnArchive();

                await db.SaveChangesAsync();

                return Results.NoContent();
            });

        }
    }
}
