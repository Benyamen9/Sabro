using Sabro.Data.Entities;
using Sabro.DTOs.Common;
using Sabro.DTOs.Segments;

namespace Sabro.Services.Interfaces
{
    public interface ISegmentService
    {
        Task<SegmentDto?> GetByIdAsync(int id);
        Task<SegmentDto?> GetByVerseAsync(int versionId, string book, string chapter, int verse);
        Task<List<SegmentDto>> GetByChapterAsync(int versionId, string book, string chapter);
        Task<PagedResult<SegmentDto>> GetFilteredAsync(SegmentFilterDto filter);

        Task<SegmentDto> CreateAsync(CreateSegmentDto dto);
        Task<SegmentDto> UpdateAsync(int id, UpdateSegmentDto dto, int userId);
        Task<SegmentDto> TransitionStatusAsync(int id, ValidationStatus newStatus, int userId);
    }
}
