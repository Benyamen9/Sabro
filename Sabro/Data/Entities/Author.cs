using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace Sabro.Data.Entities
{
    [Index(nameof(Name), IsUnique = true)]
    [Index(nameof(Century))]
    [Index(nameof(Language))]
    public class Author : BaseEntity
    {
        [Required, MaxLength(200)]
        public string Name { get; set; } = null!;

        [MaxLength(200)]
        public string OriginalName { get; set; } = null!;

        [MaxLength(500)]
        public string AlsoKnownAs { get; set; } = null!;
        public int? BirthYear { get; set; }
        public int? DeathYear { get; set; }

        [Required]
        public int Century { get; set; }

        [Required, MaxLength(10)]
        public string Language { get; set; } = null!;

        [Required]
        public AuthorCategory Category { get; set; }
        public string? Biography { get; set; }

        [MaxLength(5)]
        public string? FeastDay { get; set; }
        public string? ImageUrl { get; set; }

        // Navigation properties
        public ICollection<Annotation> Annotations { get; set; } = new List<Annotation>();
        public ICollection<AuthorSource> AuthorSources { get; set; } = new List<AuthorSource>();
    }

    public enum AuthorCategory
    {
        SyriacFather,
        // TODO Add more categories later if needed
    }
}
