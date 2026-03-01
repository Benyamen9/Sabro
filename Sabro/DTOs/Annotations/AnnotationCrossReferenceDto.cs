using Sabro.Data.Entities;

namespace Sabro.DTOs.Annotations
{
    /// <summary>
    /// Cross-reference to other biblical passages
    /// </summary>
    public class AnnotationCrossReferenceDto
    {
        public int Id { get; set; }

        /// <summary>
        /// Target biblical reference (e.g., "JHN.1.1", "GEN.1.1")
        /// </summary>
        public string TargetCanonicalRef { get; set; } = string.Empty;

        /// <summary>
        /// Type of cross-reference
        /// </summary>
        public ReferenceType ReferenceType { get; set; }
    }
}