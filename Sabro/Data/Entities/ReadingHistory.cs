using Microsoft.EntityFrameworkCore;

namespace Sabro.Data.Entities
{
    [Index(nameof(UserId), nameof(SegmentId), IsUnique = true)]
    [Index(nameof(UserId), nameof(LastReadAt))]
    public class ReadingHistory : BaseEntity
    {
        public int UserId { get; set; }
        public int SegmentId { get; set; }
        public DateTime LastReadAt { get; set; } = DateTime.UtcNow;

        // For statistics and recommendations, we can add a read count in the future
        public int ReadCount { get; set; } = 1;

        // Navigation properties
        public ApplicationUser User { get; set; } = null!;
        public Segment Segment { get; set; } = null!;
    }
}
