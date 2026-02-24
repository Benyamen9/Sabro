using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Sabro.Data.Entities
{
    [Index(nameof(SegmentId))]
    [Index(nameof(AnnotationId))]
    public class AnnotationAnchor : BaseEntity
    {
        public int AnnotationId { get; set; }
        public int SegmentId { get; set; }
        public int? StartOffset { get; set; } // Null for whole segment
        public int? EndOffset { get; set; }   // Null for whole segment
        public string? AnchorText { get; set; } // Validation
        public string? DisplayText { get; set; } // For UI display

        // Navigation properties
        public Annotation Annotation { get; set; } = null!;
        public Segment Segment { get; set; } = null!;
    }
}
