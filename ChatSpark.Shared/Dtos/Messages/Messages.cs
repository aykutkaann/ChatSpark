using System;
using System.Collections.Generic;
using System.Text;

namespace ChatSpark.Shared.Dtos.Messages
{
    public record SendMessageRequest(string Content);

    public record MessageResponse(
        Guid Id,
        Guid ChannelId,
        Guid SenderId,
        string Content,
        DateTime SentAt,
        DateTime? EditedAt,
        DateTime? DeletedAt,
        string SenderName = "Unknown",
        string? SenderAvatarUrl = null,
        int MessageType = 0,
        string? FileUrl = null);
    public record EditMessageRequest(string Content);

}
