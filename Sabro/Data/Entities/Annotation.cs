using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Sabro.Data.Entities
{
    [Index(nameof(AuthorId))]
    [Index(nameof(SourceId))]
    [Index(nameof(Published))]
    public class Annotation : BaseEntity
    {
        [Required]
        public AnnotationType Type { get; set; }

        public int? AuthorId { get; set; }
        public int? SourceId { get; set; }

        [MaxLength(500)]
        public string? SourceLocation { get; set; }

        [Required, MinLength(1)]
        public string ContentMarkdown { get; set; } = string.Empty;

        [Required]
        public string ContentHtml { get; set; } = string.Empty;

        public int? ParentAnnotationId { get; set; }
        public int CreatedById { get; set; }
        public int UpdatedById { get; set; }
        public bool Published { get; set; } = false;

        // Options for future use
        public bool IsOfficial { get; set; } = true; // true for translator, false for user-generated
        public AnnotationStatus Status { get; set; } = AnnotationStatus.Approved;

        // Navigation properties
        public Author? Author { get; set; }
        public Source? Source { get; set; }
        public Annotation? ParentAnnotation { get; set; }
        public ApplicationUser CreatedBy { get; set; } = null!;
        public ApplicationUser UpdatedBy { get; set; } = null!;

        public ICollection<AnnotationAnchor> Anchors { get; set; } = new List<AnnotationAnchor>();
        public ICollection<Annotation> Replies { get; set; } = new List<Annotation>();
        public ICollection<AnnotationCrossReference> CrossReferences { get; set; } = new List<AnnotationCrossReference>();
    }

    public enum AnnotationType
    {
        Comment,
        Citation,
        Note
    }

    public enum AnnotationStatus
    {
        Draft,          // Not yet published, visible only to author and admins
        PendingReview,  // Submitted for review, awaiting approval
        Approved,       // Approved and visible to everyone
        Rejected        // Rejected, not visible to anyone except author and admins
    }
}
