using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Sabro.Data.Entities
{
    [Index(nameof(AuthorId), nameof(SourceId), nameof(Role), IsUnique = true)]
    public class AuthorSource : BaseEntity
    {
        public int AuthorId { get; set; }
        public int SourceId { get; set; }
        public AuthorRole Role { get; set; }

        // Navigation properties
        public Author Author { get; set; } = null!;
        public Source Source { get; set; } = null!;
    }

    public enum AuthorRole
    {
        Author,
        Translator,
        // TODO Add more roles later if needed
    }
}
