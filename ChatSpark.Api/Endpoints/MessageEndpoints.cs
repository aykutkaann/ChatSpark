using ChatSpark.Api.Hubs;
using ChatSpark.Domain.Entities;
using ChatSpark.Infrastructure.Persistence;
using ChatSpark.Shared.Dtos.Messages;
using Dapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ChatSpark.Api.Endpoints
{
    public static class MessageEndpoints
    {
        public static void MapMessageEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/api/channels/{channelId:guid}/messages").RequireAuthorization().WithTags("Messages");


            group.MapPost("/", async (Guid channelId, SendMessageRequest request, AppDbContext db, ClaimsPrincipal principal, IHubContext<ChatHub> hub) =>
            {
                var userId = Guid.Parse(principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);

                var channel = await db.Channels.FindAsync(channelId);
                if (channel is null) return Results.NotFound("Channel not found.");

                if (channel.IsArchived) return Results.BadRequest("Cannot send messages to an archived channel");

                var isWorkspaceMember = await db.WorkspaceMembers.AnyAsync(m => m.WorkspaceId == channel.WorkspaceId && m.UserId == userId);


                if (!isWorkspaceMember) return Results.Forbid();

                if(channel.IsPrivate)
                {
                    var isChannelMember = await db.ChannelMembers.AnyAsync(m => m.ChannelId == channel.Id && m.UserId == userId);

                    if (!isChannelMember) return Results.Forbid();
                }

                var messages = Message.Create(channelId, userId, request.Content);
                if (request.Content.Length > 4000) return Results.BadRequest();
                db.Messages.Add(messages);

                await db.SaveChangesAsync();


                await hub.Clients.Group(channelId.ToString())
                        .SendAsync("MessageREceived", new MessageResponse(
                            messages.Id,
                            messages.ChannelId,
                            messages.SenderId,
                            messages.Content,
                            messages.SentAt,
                            messages.EditedAt));

                return Results.Created($"/api/channels/{channelId}/messages/{messages.Id}", new MessageResponse(
                    messages.Id,
                    messages.ChannelId,
                    messages.SenderId,
                    messages.Content,
                    messages.SentAt,
                    messages.EditedAt));

            });


            group.MapGet("/", async (
                Guid channelId,
                DateTime? before,
                int? limit,
                AppDbContext db,
                IDbConnectionFactory connectionFac,
                ClaimsPrincipal principal) =>
            {

                var userId = Guid.Parse(principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);

                var channel = await db.Channels
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c => c.Id == channelId);
                if (channel is null) return Results.NotFound("Channel not found.");

                var isWorkspaceMember = await db.WorkspaceMembers.AnyAsync(m => m.WorkspaceId == channel.WorkspaceId && m.UserId == userId);

                if (!isWorkspaceMember) return Results.Forbid();

                if (channel.IsPrivate)
                {
                    var isChannelMember = await db.ChannelMembers.AnyAsync(c => c.ChannelId == channel.Id && c.UserId == userId);

                    if (!isChannelMember) return Results.Forbid();
                }

                var effectiveLimit = Math.Clamp(limit ?? 50, 1, 100);

                const string sql = @"
                                SELECT id, channel_id, sender_id, content, sent_at, edited_at
                                FROM messages
                                WHERE channel_id = @ChannelId 
                                  AND (@Before::timestamptz IS NULL OR sent_at < @Before::timestamptz)
                                ORDER BY sent_at DESC
                                LIMIT @Limit;";


                using var conn = await connectionFac.CreateAsync();
                var messages = await conn.QueryAsync<MessageResponse>(sql, new
                {
                    ChannelId = channelId,
                    Before = before,
                    Limit = effectiveLimit
                });

                return Results.Ok(messages);

            });
        }
    }
}
