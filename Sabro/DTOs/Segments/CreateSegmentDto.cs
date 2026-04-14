namespace Sabro.DTOs.Segments
{
    public class CreateSegmentDto
    {
        public int VersionId { get; set; }
        public string CanonicalRef { get; set; } = null!;
        public string Book { get; set; } = null!;
        public string Chapter { get; set; } = null!;
        public int Verse { get; set; }
        public int SegmentOrder { get; set; } = 1;
        public string Content { get; set; } = null!;
    }
}
