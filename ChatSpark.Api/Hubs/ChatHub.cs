using ChatSpark.Domain.Entities;
using ChatSpark.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;

namespace ChatSpark.Api.Hubs
{

    [Authorize]
    public class ChatHub(AppDbContext db, IConnectionMultiplexer connectionMultiplexer) :Hub
    {

        private static readonly ConcurrentDictionary<string, HashSet<Guid>> _connectionChannels = new();


        public async Task JoinChannel(Guid channelId)
        {
            var userId = GetUserId();

            var channel = await db.Channels.FindAsync(channelId)
                ?? throw new HubException("Channel not found.");

            if (channel.IsArchived)
                throw new HubException("Channel is archived.");

            var isWorkspaceMember = await db.WorkspaceMembers.AnyAsync(m => m.WorkspaceId == channel.WorkspaceId && m.UserId == userId);

            if (!isWorkspaceMember)
                throw new HubException("Not a workspace member.");

            if (channel.IsPrivate)
            {
                var isChannelMember = await db.ChannelMembers.AnyAsync(c => c.ChannelId == channel.Id && c.UserId == userId);

                if (!isChannelMember)
                    throw new HubException("Not a channel member.");

            }

            await Groups.AddToGroupAsync(Context.ConnectionId, channelId.ToString());

            var redis = connectionMultiplexer.GetDatabase();

            await redis.SetAddAsync($"presence:{channelId}", userId.ToString());


            _connectionChannels.AddOrUpdate(
                Context.ConnectionId,
                _ => new HashSet<Guid> { channelId },
                (_, set) => { set.Add(channelId); return set; });

            await Clients.Group(channelId.ToString()).SendAsync("UserOnline", userId);
        }

        public async Task LeaveChannel(Guid channelId)
        {
            var userId = GetUserId();
            var redis = connectionMultiplexer.GetDatabase();

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, channelId.ToString());
            await redis.SetRemoveAsync($"presence:{channelId}", userId.ToString());


            if(_connectionChannels.TryGetValue(Context.ConnectionId, out var channelIds))
            {
                channelIds.Remove(channelId);
            }

            await Clients.Group(channelId.ToString()).SendAsync("UserOffline", userId);

        }

        private Guid GetUserId()
        {

            var sub = Context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? throw new HubException("Missing sub claim.");

            return Guid.Parse(sub);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();
            var redis = connectionMultiplexer.GetDatabase();

            if (_connectionChannels.TryRemove(Context.ConnectionId, out var channelIds))
            {
                foreach (var channelId in channelIds)
                {
                    await redis.SetRemoveAsync($"presence:{channelId}", userId.ToString());
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, channelId.ToString());
                    await Clients.Group(channelId.ToString()).SendAsync("UserOffline", userId);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

    }
}
