using System;
using System.Collections.Generic;
using System.Text;

namespace ChatSpark.Shared.Events
{

    public record MessageSentEvent(
        Guid MessageId,
        Guid ChannelId,
        Guid SenderId,
        string Content,
        DateTime SentAt);

}
