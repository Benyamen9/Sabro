using Sabro.Data.Entities;
using Sabro.DTOs.Segments;

namespace Sabro.Mappers
{
    public static class SegmentMapper
    {
        public static SegmentDto ToDto(this Segment segment)
        {
            return new SegmentDto
            {
                Id = segment.Id,
                VersionId = segment.VersionId,
                VersionLanguage = segment.Version?.Language ?? string.Empty,
                CanonicalRef = segment.CanonicalRef,
                Book = segment.Book,
                Chapter = segment.Chapter,
                Verse = segment.Verse,
                SegmentOrder = segment.SegmentOrder,
                Content = segment.Content,
                ValidationStatus = segment.ValidationStatus,
                CreatedAt = segment.CreatedAt,
                UpdatedAt = segment.UpdatedAt
            };
        }

        public static Segment ToEntity(this CreateSegmentDto dto)
        {
            return new Segment
            {
                VersionId = dto.VersionId,
                CanonicalRef = dto.CanonicalRef,
                Book = dto.Book,
                Chapter = dto.Chapter,
                Verse = dto.Verse,
                SegmentOrder = dto.SegmentOrder,
                Content = dto.Content,
                ValidationStatus = ValidationStatus.Draft
            };
        }
    }
}
