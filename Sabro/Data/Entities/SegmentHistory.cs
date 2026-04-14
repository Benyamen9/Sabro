using System.ComponentModel.DataAnnotations;

namespace Sabro.Data.Entities
{
    public class SegmentHistory : BaseEntity
    {
        public int SegmentId { get; set; }

        [Required]
        public string OldContent { get; set; } = null!;

        [Required]
        public string NewContent { get; set; } = null!;

        public string? Reason { get; set; }

        public int ChangedById { get; set; }

        // Navigation properties
        public Segment Segment { get; set; } = null!;
        public ApplicationUser ChangedBy { get; set; } = null!;
    }
}
