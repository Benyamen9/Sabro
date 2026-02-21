using Microsoft.EntityFrameworkCore;

namespace Sabro.Data.Entities
{
    [Index(nameof(ReadingListId), nameof(Order))]
    public class ReadingListItem : BaseEntity
    {
        public int ReadingListId { get; set; }
        public int SegmentId { get; set; }
        public int Order { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ReadingList ReadingList { get; set; } = null!;
        public Segment Segment { get; set; } = null!;

    }
}
