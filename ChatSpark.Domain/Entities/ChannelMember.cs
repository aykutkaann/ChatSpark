using ChatSpark.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChatSpark.Domain.Entities
{
    public class ChannelMember
    {
        public Guid Id { get; private set; }
        public Guid ChannelId { get; private set; }
        public Guid UserId { get; private set; }
        public DateTime JoinedAt { get; private set; }


        private ChannelMember() { }

        public static ChannelMember Create(Guid channelId, Guid userId)
        {
            if (channelId == Guid.Empty)
                throw new ArgumentException("ChannelId must be a valid Guid.");
            if (userId == Guid.Empty)
                throw new ArgumentException("UserId must be a valid Guid.");

            return new ChannelMember
            {
                Id = Guid.NewGuid(),
                ChannelId = channelId,
                UserId = userId,
                JoinedAt = DateTime.UtcNow
            };
        }
    }
}
