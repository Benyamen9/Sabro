using Sabro.Data.Entities;

namespace Sabro.DTOs.Annotations
{
    public class AnnotationDto
    {
        public int Id { get; set; }
        public AnnotationType Type { get; set; }

        // Author information
        public int? AuthorId { get; set; }
        public string? AuthorName { get; set; }
        public int? AuthorCentury { get; set; }
        public string? AuthorLanguage { get; set; }
        public AuthorCategory? AuthorCategory { get; set; }
        public bool IsSaint { get; set; }

        // Source information
        public int? SourceId { get; set; }
        public string? SourceName { get; set; }
        public SourceType? SourceType { get; set; }
        public bool IsMajor { get; set; }
        public string? SourceLocation { get; set; }

        // Content
        public string ContentMarkdown { get; set; } = string.Empty;
        public string ContentHtml { get; set; } = string.Empty;

        // Hierarchy
        public int? ParentAnnotationId { get; set; }

        // Status
        public bool Published { get; set; }
        public bool IsOfficial { get; set; }
        public AnnotationStatus Status { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? CreatedByName { get; set; }
        public string? UpdatedByName { get; set; }

        // Related data
        public List<AnnotationAnchorDto>? Anchors { get; set; }
        public List<AnnotationCrossReferenceDto>? CrossReferences { get; set; }
    }
}