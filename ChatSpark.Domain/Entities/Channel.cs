using System;
using System.Text;
using System.Xml.Linq;

namespace ChatSpark.Domain.Entities
{
    public class Channel
    {
        public Guid Id { get;private set; }
        public Guid WorkspaceId { get; private set; }

        public string Name { get;private set; } = null!;
        public bool IsPrivate { get; private set; }
        public DateTime CreatedAt { get;private set; }



        private Channel() { }

        public static Channel Create(Guid workspaceId, string name, bool isPrivate)
        {

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Channel name cannot be empty.");

            if (workspaceId == Guid.Empty)
                throw new ArgumentException("WorkspaceId must be a valid Guid.");

            return new Channel
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                Name = name,
                IsPrivate = isPrivate,
                CreatedAt = DateTime.UtcNow
            };
        }

        public void MakePrivate()
        {
            IsPrivate = true;
        }

        public void MakePublic()
        {
            IsPrivate = false;
        }

        public void Rename(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
                throw new ArgumentException("Name cannot be emtpy.");
            Name = newName;
        }





    }
}
