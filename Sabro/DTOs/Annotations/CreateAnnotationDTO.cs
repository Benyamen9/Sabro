using Sabro.Data.Entities;
using System.ComponentModel.DataAnnotations;

namespace Sabro.DTOs.Annotations
{
    public class CreateAnnotationDto
    {
        [Required]
        public AnnotationType Type { get; set; }

        public int? AuthorId { get; set; }

        public int? SourceId { get; set; }

        [MaxLength(500)]
        public string? SourceLocation { get; set; }

        [Required, MinLength(1), MaxLength(50000)]
        public string ContentMarkdown { get; set; } = string.Empty;

        public int? ParentAnnotationId { get; set; }

        [Required, MinLength(1)]
        public List<CreateAnnotationAnchorDto> Anchors { get; set; } = [];

        public List<CreateAnnotationCrossReferenceDto>? CrossReferences { get; set; }
    }

    public class CreateAnnotationAnchorDto
    {
        [Required]
        public int SegmentId { get; set; }

        public int? StartOffset { get; set; }

        public int? EndOffset { get; set; }

        [MaxLength(1000)]
        public string? AnchorText { get; set; }

        [MaxLength(200)]
        public string? DisplayText { get; set; }
    }

    public class CreateAnnotationCrossReferenceDto
    {
        [Required, MaxLength(50)]
        public string TargetCanonicalRef { get; set; } = string.Empty;

        [Required]
        public ReferenceType ReferenceType { get; set; }
    }
}