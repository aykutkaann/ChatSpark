using ChatSpark.Api.Hubs;
using ChatSpark.Application.Abstractions;
using ChatSpark.Domain.Entities;
using ChatSpark.Infrastructure.Persistence;
using ChatSpark.Shared.Dtos.Messages;
using ChatSpark.Shared.Events;
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


            group.MapPost("/", async (Guid channelId, SendMessageRequest request,
                AppDbContext db, ClaimsPrincipal principal, IHubContext<ChatHub> hub, IEventPublisher  publisher ) =>
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

                await publisher.PublishAsync("message-sent", new MessageSentEvent(
                    messages.Id,
                    messages.ChannelId,
                    messages.SenderId,
                    messages.Content,
                    messages.SentAt));


                await hub.Clients.Group(channelId.ToString())
                        .SendAsync("MessageREceived", new MessageResponse(
                            messages.Id,
                            messages.ChannelId,
                            messages.SenderId,
                            messages.Content,
                            messages.SentAt,
                            messages.EditedAt,
                            messages.DeletedAt));

                return Results.Created($"/api/channels/{channelId}/messages/{messages.Id}", new MessageResponse(
                    messages.Id,
                    messages.ChannelId,
                    messages.SenderId,
                    messages.Content,
                    messages.SentAt,
                    messages.EditedAt,
                    messages.DeletedAt));

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
                                SELECT id, channel_id, sender_id, content, sent_at, edited_at, deleted_at
                                FROM messages
                                WHERE channel_id = @ChannelId 
                                  AND deleted_at IS NULL
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


            group.MapPatch("/{messageId:guid}", async (Guid channelId, EditMessageRequest request, 
                Guid messageId, ClaimsPrincipal principal, IHubContext<ChatHub> hubContext, AppDbContext db) => 
            {
                var userId = Guid.Parse(principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);

                var message = await db.Messages.FindAsync(messageId);
                if (message is null) return Results.NotFound("Message not found.");

                if (message.ChannelId != channelId) return Results.NotFound("Message and Channel is not match");

                try
                {
                    message.Edit(request.Content, userId);
                }
                catch (InvalidOperationException)
                {
                    return Results.Forbid();
                }
                catch (ArgumentException err)
                {
                    return Results.BadRequest(err.Message);
                }

                await db.SaveChangesAsync();

                var response = new MessageResponse(
                    message.Id,
                    message.ChannelId,
                    message.SenderId,
                    message.Content,
                    message.SentAt,
                    message.EditedAt,
                    message.DeletedAt);

                await hubContext.Clients.Group(channelId.ToString()).SendAsync("MessageEdited", response);

                if (message.DeletedAt is not null) return Results.NotFound("Message not found.");


                return Results.Ok(response);


            });

            group.MapDelete("/{messageId:guid}", async (Guid channelId, Guid messageId, AppDbContext db,
                ClaimsPrincipal principal, IHubContext<ChatHub> hubContext) =>
            {
                var userId = Guid.Parse(principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);

                var message = await db.Messages.FindAsync(messageId);
                if (message is null) return Results.NotFound("Message not found.");

                if (message.ChannelId != channelId) return Results.NotFound("Message and Channel is not match");


                try
                {
                    message.Delete(userId);
                }
                catch (InvalidOperationException)
                {
                    return Results.Forbid();
                }
    
                await db.SaveChangesAsync();

                await hubContext.Clients.Group(channelId.ToString()).SendAsync("MessageDeleted", messageId);

                if (message.DeletedAt is not null) return Results.NotFound("Message not found.");


                return Results.NoContent();



            });
        }
    }
}
