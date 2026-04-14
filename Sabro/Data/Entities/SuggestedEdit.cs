using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Sabro.Data.Entities
{
    [Index(nameof(SegmentId), nameof(Status))]
    [Index(nameof(ReviewerId))]
    public class SuggestedEdit : BaseEntity
    {
        public int SegmentId { get; set; }
        public int ReviewerId { get; set; }

        [Required]
        public string SuggestedContent { get; set; } = null!;

        public string? Comment { get; set; }

        public SuggestedEditStatus Status { get; set; } = SuggestedEditStatus.Pending;

        public string? RejectionReason { get; set; }

        public int? ResolvedById { get; set; }
        public DateTime? ResolvedAt { get; set; }

        // Navigation properties
        public Segment Segment { get; set; } = null!;
        public ApplicationUser Reviewer { get; set; } = null!;
        public ApplicationUser? ResolvedBy { get; set; }
    }

    public enum SuggestedEditStatus
    {
        Pending,
        Accepted,
        Rejected
    }
}
