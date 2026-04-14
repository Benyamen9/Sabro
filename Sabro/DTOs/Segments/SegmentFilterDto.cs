using Sabro.Data.Entities;

namespace Sabro.DTOs.Segments
{
    public class SegmentFilterDto
    {
        public int? VersionId { get; set; }
        public string? Book { get; set; }
        public string? Chapter { get; set; }
        public ValidationStatus? Status { get; set; }
        public string? SearchQuery { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
}
