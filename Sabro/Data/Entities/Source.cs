using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.AccessControl;
using System.Xml.Linq;

namespace Sabro.Data.Entities
{
    [Index(nameof(Name), IsUnique = true)]
    [Index(nameof(SourceType))]
    [Index(nameof(Century))]
    public class Source : BaseEntity
    {
        [Required, MaxLength(300)]
        public string Name { get; set; } = null!;

        [Required]
        public SourceType SourceType { get; set; }

        public string? Description { get; set; }

        [Required]
        public int Century { get; set; }

        [Required, MaxLength(10)]
        public string Language { get; set; } = null!;

        // Navigation properties
        public ICollection<Annotation> Annotations { get; set; } = [];
        public ICollection<AuthorSource> AuthorSources { get; set; } = [];
    }

    public enum SourceType
    {
        Commentary,
        Homily,
        Letter,
        // TODO Add more types later if needed
    }
}
