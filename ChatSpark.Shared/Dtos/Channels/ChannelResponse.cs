using System;
using System.Collections.Generic;
using System.Text;

namespace ChatSpark.Shared.Dtos.Channels
{
    public record ChannelResponse(Guid Id, Guid WorkspaceId, string Name, bool IsPrivate, bool IsArchived, DateTime CreatedAt);
}
