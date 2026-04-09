using ChatSpark.Domain.Enum;

namespace ChatSpark.Domain.Entities
{
    public class WorkspaceMember
    {
        public Guid Id { get;private set; }
        public Guid WorkspaceId { get; private set; }
        public Guid UserId { get; private set; }
        public Role Role { get; private set; }
        public DateTime JoinedAt { get; private set; }

        private WorkspaceMember () { }

        public static WorkspaceMember Create(Guid workspaceId, Guid userId, Role role)
        {
            if(workspaceId ==Guid.Empty)
                throw new ArgumentException("WorkspaceId must be a valid Guid.");
            if (userId == Guid.Empty)
                throw new ArgumentException("UserId must be a valid Guid.");

            return new WorkspaceMember
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                UserId = userId,
                Role = role,
                JoinedAt = DateTime.UtcNow

            };

        }


        public void  ChangeRole(Role newRole)
        {
            if (Role == Role.Owner)
                throw new InvalidOperationException("Owner roles cannot be changed.");

            Role = newRole;
        }


    }
}
