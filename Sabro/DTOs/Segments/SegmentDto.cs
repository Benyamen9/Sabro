using Sabro.Data.Entities;

namespace Sabro.DTOs.Segments
{
    public class SegmentDto
    {
        public int Id { get; set; }
        public int VersionId { get; set; }
        public string VersionLanguage { get; set; } = null!;
        public string CanonicalRef { get; set; } = null!;
        public string Book { get; set; } = null!;
        public string Chapter { get; set; } = null!;
        public int Verse { get; set; }
        public int SegmentOrder { get; set; }
        public string Content { get; set; } = null!;
        public ValidationStatus ValidationStatus { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
