using Sabro.Data.Entities;
using Sabro.DTOs.Annotations;
using System.ComponentModel.DataAnnotations;

namespace Sabro.DTOs.Annotations
{
    public class UpdateAnnotationDto
    {
        [Required]
        public AnnotationType Type { get; set; }

        public int? AuthorId { get; set; }

        public int? SourceId { get; set; }

        [MaxLength(500)]
        public string? SourceLocation { get; set; }

        [Required, MinLength(1), MaxLength(50000)]
        public string ContentMarkdown { get; set; } = string.Empty;

        public List<CreateAnnotationAnchorDto>? Anchors { get; set; }

        public List<CreateAnnotationCrossReferenceDto>? CrossReferences { get; set; }

        [Required]
        public DateTime UpdatedAt { get; set; }
    }
}