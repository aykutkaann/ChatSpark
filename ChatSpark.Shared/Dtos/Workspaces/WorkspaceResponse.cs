using System;
using System.Collections.Generic;
using System.Text;

namespace ChatSpark.Shared.Dtos.Workspaces
{

    public record WorkspaceResponse(Guid Id, string Name, string Slug, Guid OwnerId, DateTime CreatedAt);
}
