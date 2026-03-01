namespace Sabro.DTOs.Annotations
{
    /// <summary>
    /// Represents an anchor point on a biblical segment
    /// </summary>
    public class AnnotationAnchorDto
    {
        public int Id { get; set; }

        public int SegmentId { get; set; }

        /// <summary>
        /// Start position in segment text (null = entire segment)
        /// </summary>
        public int? StartOffset { get; set; }

        /// <summary>
        /// End position in segment text (null = entire segment)
        /// </summary>
        public int? EndOffset { get; set; }

        /// <summary>
        /// Text being annotated (for validation)
        /// </summary>
        public string? AnchorText { get; set; }

        /// <summary>
        /// Display text override (optional)
        /// </summary>
        public string? DisplayText { get; set; }

        // Related segment info
        public string? SegmentCanonicalRef { get; set; }
        public string? SegmentContent { get; set; }
    }
}