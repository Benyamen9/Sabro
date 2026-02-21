using Microsoft.EntityFrameworkCore;

namespace Sabro.Data.Entities
{
    [Index(nameof(UserId), nameof(SegmentId), IsUnique = true)]
    [Index(nameof(UserId), nameof(AnnotationId), IsUnique = true)]
    public class UserFavorite : BaseEntity
    {
        public int UserId { get; set; }
        public int? SegmentId { get; set; }
        public int? AnnotationId { get; set; }

        // Navigation properties
        public ApplicationUser User { get; set; } = null!;
        public Segment? Segment { get; set; }
        public Annotation? Annotation { get; set; }
    }
}
