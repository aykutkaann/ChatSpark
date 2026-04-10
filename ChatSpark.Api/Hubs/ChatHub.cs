using ChatSpark.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;

namespace ChatSpark.Api.Hubs
{

    [Authorize]
    public class ChatHub(AppDbContext db) :Hub
    {
        
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
        }

        public async Task LeaveChannel(Guid channelId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, channelId.ToString());
        }

        private Guid GetUserId()
        {

            var sub = Context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? throw new HubException("Missing sub claim.");

            return Guid.Parse(sub);
        }
    }
}
