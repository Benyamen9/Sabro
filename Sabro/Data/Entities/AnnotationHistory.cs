using System.ComponentModel.DataAnnotations;

namespace Sabro.Data.Entities
{
    public class AnnotationHistory : BaseEntity
    {
        public int AnnotationId { get; set; }

        [Required]
        public string OldContentMarkdown { get; set; } = null!;

        [Required]
        public string NewContentMarkdown { get; set; } = null!;

        public string? Reason { get; set; }

        public int ChangedById { get; set; }

        // Navigation properties
        public Annotation Annotation { get; set; } = null!;
        public ApplicationUser ChangedBy { get; set; } = null!;
    }
}
