using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Sabro.Data.Entities
{
    [Index(nameof(UserId), nameof(SegmentId))]
    public class UserNote : BaseEntity
    {
        public int UserId { get; set; }
        public int SegmentId { get; set; }

        [Required]
        public string ContentMarkdown { get; set; } = string.Empty;
        [Required]
        public string ContentHtml { get; set; } = string.Empty;

        // Optionnal for future use, to allow users to share notes with others or keep them private
        public bool IsPrivate { get; set; } = true;
        public bool SubmittedForReview { get; set; } = false;
        public NoteStatus Status { get; set; } = NoteStatus.Draft;

        // Navigation properties
        public ApplicationUser User { get; set; } = null!;
        public Segment Segment { get; set; } = null!;
    }

    public enum NoteStatus
    {
        Draft,          // Personal note, not shared
        PendingReview,  // Submitted for review, awaiting approval
        Approved,       // Approved and visible to others
        Rejected        // Rejected, not visible to others
    }
}
