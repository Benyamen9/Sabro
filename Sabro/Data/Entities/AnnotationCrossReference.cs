using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.ComponentModel.DataAnnotations;

namespace Sabro.Data.Entities
{
    [Index(nameof(AnnotationId))]
    [Index(nameof(TargetCanonicalRef))]
    public class AnnotationCrossReference : BaseEntity
    {
        public int AnnotationId { get; set; }

        [MaxLength(50)]
        public string TargetCanonicalRef { get; set; } = null!; // COR1:1.1
        public ReferenceType ReferenceType { get; set; }

        // Navigation properties
        public Annotation Annotation { get; set; } = null!;
    }

    public enum ReferenceType
    {
        Biblical,       // Reference to a specific verse or passage in the Bible
        Lexical,        // Reference to a specific word or phrase in the original language
        Thematic,       // Reference to a theme or topic (e.g. "faith", "love")
        CrossBook,      // Reference to another annotation or segment within the same book
        CrossSegment,   // Reference to another annotation or segment in a different book
        Other           // Any other type of reference (e.g. historical, cultural)
    }
}
