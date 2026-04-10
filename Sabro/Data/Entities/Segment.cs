using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Sabro.Data.Entities
{
    [Index(nameof(VersionId), nameof(CanonicalRef), IsUnique = true)]
    [Index(nameof(Book), nameof(Chapter))]
    public class Segment(string book, string chapter, int verse) : BaseEntity
    {
        public int VersionId { get; set; }

        [Required, MaxLength(50)]
        public string CanonicalRef { get; set; } = null!;

        [Required, MaxLength(100)]
        public string Book { get; } = book;

        [Required, MaxLength(20)]
        public string Chapter { get; } = chapter;
        public int Verse { get; } = verse;
        public int SegmentOrder { get; set; } = 1;

        [Required, MinLength(1)]
        public string Content { get; set; } = null!;

        public ValidationStatus ValidationStatus { get; set; } = ValidationStatus.Draft;

        // Navigation properties
        public TextVersion Version { get; set; } = null!;
        public ICollection<AnnotationAnchor> AnnotationAnchors { get; set; } = [];
    }

    public enum ValidationStatus
    {
        Draft,
        SelfReview,
        FinalReview,
        Approved,
        NeedsRevision
    }
}
