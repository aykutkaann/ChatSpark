using System;
using System.Collections.Generic;
using System.Text;

namespace ChatSpark.Domain.Entities
{
    public class Message
    {
        public Guid Id { get; private set; }
        public Guid ChannelId { get;private set; }
        public Guid SenderId { get;private set; } //User

        public string Content { get;private set; }
        public DateTime SentAt { get; private set; }
        public DateTime? EditedAt { get;private set; }

        private Message() { }

        public static  Message Create(Guid channelId, Guid senderId, string content)
        {

            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Message content cannot be empty.");

            if (channelId == Guid.Empty)
                throw new ArgumentException("ChannelId must be a valid Guid.");

            if (senderId == Guid.Empty)
                throw new ArgumentException("SenderId must be a valid Guid.");

            return new Message
            {
                Id = Guid.NewGuid(),
                ChannelId = channelId,
                SenderId = senderId,
                Content = content,
                SentAt = DateTime.UtcNow
            };
        }

        public void Edit(string newContent, Guid editorId)
        {

            if (!CanBeEditedBy(editorId))
                throw new InvalidOperationException("Only the sender can edit this message.");
            if (string.IsNullOrWhiteSpace(newContent))
                throw new ArgumentException("Content cannot be empty.");

            Content = newContent;
            EditedAt = DateTime.UtcNow;
        }
        public bool CanBeEditedBy(Guid userId) => SenderId == userId;



    }
}
