using System;
using System.Collections.Generic;
using System.Text;

namespace ChatSpark.Shared.Dtos.Channels
{

    public record CreateChannelRequest(string Name, bool IsPrivate);
}
