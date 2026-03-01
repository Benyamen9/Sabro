using Sabro.DTOs.Annotations;
using Sabro.DTOs.Common;

namespace Sabro.Services.Interfaces
{
    public interface IAnnotationService
    {
        Task<AnnotationDto> CreateAsync(CreateAnnotationDto dto, int userId);
        Task<AnnotationDto> UpdateAsync(int id, UpdateAnnotationDto dto, int userId);
        Task<bool> DeleteAsync(int id, int userId);
        Task<AnnotationDto?> GetByIdAsync(int id);

        Task<PagedResult<AnnotationDto>> GetFilteredAsync(AnnotationFilterDto filter);
        Task<List<AnnotationDto>> GetBySegmentIdAsync(int segmentId);
        Task<List<AnnotationDto>> GetByAuthorIdAsync(int authorId, int? segmentId = null);

        Task<bool> TogglePublishAsync(int id, int userId);

        Task ValidateAnchorsAsync(int annotationId);
    }
}
