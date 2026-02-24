using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Sabro.Data.Entities
{
    [Index(nameof(VersionId), nameof(CanonicalRef), IsUnique = true)]
    [Index(nameof(Book), nameof(Chapter))]
    public class Segment : BaseEntity
    {
        public int VersionId { get; set; }

        [Required, MaxLength(50)]
        public string CanonicalRef { get; set; } = null!;

        [Required, MaxLength(100)]
        public string Book { get; set; } = null!;
        public string Chapter { get; set; } = null!;
        public int Verse { get; set; }
        public int SegmentOrder { get; set; } = 1;

        [Required, MinLength(1)]
        public string Content { get; set; } = null!;

        // Navigation properties
        public TextVersion Version { get; set; } = null!;
        public ICollection<AnnotationAnchor> AnnotationAnchors { get; set; } = new List<AnnotationAnchor>();
    }
}
