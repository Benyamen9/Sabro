using Sabro.Data.Entities;
using Sabro.DTOs.Annotations;

namespace Sabro.Mappers
{
    /// <summary>
    /// Maps between Annotation entities and DTOs
    /// </summary>
    public static class AnnotationMapper
    {
        /// <summary>
        /// Maps Annotation entity to DTO
        /// </summary>
        public static AnnotationDto ToDto(this Annotation annotation)
        {
            return new AnnotationDto
            {
                Id = annotation.Id,
                Type = annotation.Type,

                // Author info
                AuthorId = annotation.AuthorId,
                AuthorName = annotation.Author?.Name,
                AuthorCentury = annotation.Author?.Century,
                AuthorLanguage = annotation.Author?.Language,
                AuthorCategory = annotation.Author?.Category,

                // Source info
                SourceId = annotation.SourceId,
                SourceName = annotation.Source?.Name,
                SourceType = annotation.Source?.SourceType,
                SourceLocation = annotation.SourceLocation,

                // Content
                ContentMarkdown = annotation.ContentMarkdown,
                ContentHtml = annotation.ContentHtml,

                // Hierarchy
                ParentAnnotationId = annotation.ParentAnnotationId,

                // Status
                Published = annotation.Published,
                IsOfficial = annotation.IsOfficial,
                Status = annotation.Status,

                // Audit
                CreatedAt = annotation.CreatedAt,
                UpdatedAt = annotation.UpdatedAt,
                CreatedByName = annotation.CreatedBy?.UserName,
                UpdatedByName = annotation.UpdatedBy?.UserName,

                // Related data - Anchors
                Anchors = annotation.Anchors?.Select(a => new AnnotationAnchorDto
                {
                    Id = a.Id,
                    SegmentId = a.SegmentId,
                    StartOffset = a.StartOffset,
                    EndOffset = a.EndOffset,
                    AnchorText = a.AnchorText,
                    DisplayText = a.DisplayText,
                    SegmentCanonicalRef = a.Segment?.CanonicalRef,
                    SegmentContent = a.Segment?.Content
                }).ToList(),

                // Related data - Cross References
                CrossReferences = annotation.CrossReferences?.Select(cr => new AnnotationCrossReferenceDto
                {
                    Id = cr.Id,
                    TargetCanonicalRef = cr.TargetCanonicalRef,
                    ReferenceType = cr.ReferenceType
                }).ToList()
            };
        }

        /// <summary>
        /// Maps CreateAnnotationDto to entity
        /// </summary>
        public static Annotation ToEntity(this CreateAnnotationDto dto, int userId, string contentHtml)
        {
            return new Annotation
            {
                Type = dto.Type,
                AuthorId = dto.AuthorId,
                SourceId = dto.SourceId,
                SourceLocation = dto.SourceLocation,
                ContentMarkdown = dto.ContentMarkdown,
                ContentHtml = contentHtml,
                ParentAnnotationId = dto.ParentAnnotationId,
                CreatedById = userId,
                UpdatedById = userId,
                Published = false,
                IsOfficial = true,
                Status = AnnotationStatus.Draft
            };
        }
    }
}