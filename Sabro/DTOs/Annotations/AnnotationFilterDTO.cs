using Sabro.Data.Entities;

namespace Sabro.DTOs.Annotations
{
    public class AnnotationFilterDto
    {
        public int? SegmentId { get; set; }

        public List<int>? AuthorIds { get; set; }

        public List<int>? SourceIds { get; set; }

        public List<SourceType>? SourceTypes { get; set; }

        public List<int>? Centuries { get; set; }

        public List<string>? Languages { get; set; }

        public List<AuthorCategory>? Categories { get; set; }
        public List<AnnotationType>? AnnotationTypes { get; set; }
        public List<AnnotationStatus>? AnnotationStatuses { get; set; }

        public bool? ExcludeModern { get; set; }

        public bool? OnlyCanonical { get; set; }

        public bool? OnlyOfficial { get; set; }

        public string SortBy { get; set; } = "chronological";

        public int? Page { get; set; } = 1;

        public int? PageSize { get; set; } = 50;
    }
}