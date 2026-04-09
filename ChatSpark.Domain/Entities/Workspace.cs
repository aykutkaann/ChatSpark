using System.Text;
using System.Xml.Linq;

namespace ChatSpark.Domain.Entities
{
    public class Workspace
    {
        public Guid Id { get; private set; }
        public Guid OwnerId { get; private set; } //User

        public string Name { get;private set; } = null!;
        public string Slug { get; private set; } = null!;

        public DateTime CreatedAt { get;private set; }



        private Workspace() { }

        public static  Workspace Create(Guid ownerId, string name, string slug)
        {

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty.");
            if (string.IsNullOrWhiteSpace(slug) || slug.Any(char.IsWhiteSpace) || slug.Any(char.IsUpper) )
                throw new ArgumentException("Slug must be lowercase and contain no spaces.");

            if (ownerId == Guid.Empty)
                throw new ArgumentException("OwnerId must be a valid Guid.");
            return new Workspace
            {
                Id =Guid.NewGuid(),
                OwnerId =ownerId,
                Name = name,
                Slug = slug,
                CreatedAt = DateTime.UtcNow
            };
        }

        public void Rename(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
                throw new ArgumentException("Name cannot be emtpy.");
            Name = newName;
        }
    }
}
