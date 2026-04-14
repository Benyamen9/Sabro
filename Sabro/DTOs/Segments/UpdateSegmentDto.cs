namespace Sabro.DTOs.Segments
{
    public class UpdateSegmentDto
    {
        public string Content { get; set; } = null!;
        public string? Reason { get; set; }

        // Used for optimistic concurrency — must match the current UpdatedAt on the entity
        public DateTime UpdatedAt { get; set; }
    }
}
