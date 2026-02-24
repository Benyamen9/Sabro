using System.ComponentModel.DataAnnotations;

namespace Sabro.Data.Entities
{
    public class TextVersion : BaseEntity
    {
        [Required, MaxLength(200)]
        public string Name { get; set; } = null!;

        [Required, MaxLength(10)]
        public string Language { get; set; } = null!;
        public string? Description { get; set; }
        public bool Published { get; set; }

        // Navigation properties
        public ICollection<Segment> Segments { get; set; } = new List<Segment>();
    }
}
