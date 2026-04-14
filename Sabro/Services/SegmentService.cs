using Microsoft.EntityFrameworkCore;
using Sabro.Data;
using Sabro.Data.Entities;
using Sabro.DTOs.Common;
using Sabro.DTOs.Segments;
using Sabro.Exeptions;
using Sabro.Mappers;
using Sabro.Services.Interfaces;

namespace Sabro.Services
{
    public class SegmentService(ApplicationDbContext context) : ISegmentService
    {
        private readonly ApplicationDbContext _context = context;

        public async Task<SegmentDto?> GetByIdAsync(int id)
        {
            var segment = await _context.Segments
                .Include(s => s.Version)
                .FirstOrDefaultAsync(s => s.Id == id);

            return segment?.ToDto();
        }

        public async Task<SegmentDto?> GetByVerseAsync(int versionId, string book, string chapter, int verse)
        {
            var segment = await _context.Segments
                .Include(s => s.Version)
                .FirstOrDefaultAsync(s =>
                    s.VersionId == versionId &&
                    s.Book == book &&
                    s.Chapter == chapter &&
                    s.Verse == verse);

            return segment?.ToDto();
        }

        public async Task<List<SegmentDto>> GetByChapterAsync(int versionId, string book, string chapter)
        {
            var segments = await _context.Segments
                .Include(s => s.Version)
                .Where(s => s.VersionId == versionId && s.Book == book && s.Chapter == chapter)
                .OrderBy(s => s.SegmentOrder)
                .ThenBy(s => s.Verse)
                .ToListAsync();

            return [.. segments.Select(s => s.ToDto())];
        }

        public async Task<PagedResult<SegmentDto>> GetFilteredAsync(SegmentFilterDto filter)
        {
            var query = _context.Segments
                .Include(s => s.Version)
                .AsQueryable();

            if (filter.VersionId.HasValue)
                query = query.Where(s => s.VersionId == filter.VersionId.Value);

            if (!string.IsNullOrWhiteSpace(filter.Book))
                query = query.Where(s => s.Book == filter.Book);

            if (!string.IsNullOrWhiteSpace(filter.Chapter))
                query = query.Where(s => s.Chapter == filter.Chapter);

            if (filter.Status.HasValue)
                query = query.Where(s => s.ValidationStatus == filter.Status.Value);

            if (!string.IsNullOrWhiteSpace(filter.SearchQuery))
                query = query.Where(s => s.Content.Contains(filter.SearchQuery));

            var total = await query.CountAsync();

            var items = await query
                .OrderBy(s => s.Book)
                .ThenBy(s => s.Chapter)
                .ThenBy(s => s.SegmentOrder)
                .ThenBy(s => s.Verse)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return new PagedResult<SegmentDto>
            {
                Items = [.. items.Select(s => s.ToDto())],
                TotalCount = total,
                Page = filter.Page,
                PageSize = filter.PageSize,
                TotalPages = (int)Math.Ceiling(total / (double)filter.PageSize)
            };
        }

        public async Task<SegmentDto> CreateAsync(CreateSegmentDto dto)
        {
            var versionExists = await _context.TextVersions.AnyAsync(v => v.Id == dto.VersionId);
            if (!versionExists)
                throw new NotFoundException("TextVersion", dto.VersionId);

            var duplicate = await _context.Segments.AnyAsync(s =>
                s.VersionId == dto.VersionId &&
                s.CanonicalRef == dto.CanonicalRef);

            if (duplicate)
                throw new ValidationException($"A segment with CanonicalRef '{dto.CanonicalRef}' already exists in this version.");

            var segment = dto.ToEntity();
            _context.Segments.Add(segment);
            await _context.SaveChangesAsync();

            return await GetByIdAsync(segment.Id) ?? throw new NotFoundException("Segment", segment.Id);
        }

        public async Task<SegmentDto> UpdateAsync(int id, UpdateSegmentDto dto, int userId)
        {
            var segment = await _context.Segments
                .Include(s => s.Version)
                .FirstOrDefaultAsync(s => s.Id == id)
                ?? throw new NotFoundException("Segment", id);

            if (segment.UpdatedAt != dto.UpdatedAt)
                throw new ConcurrencyException();

            var history = new SegmentHistory
            {
                SegmentId = segment.Id,
                OldContent = segment.Content,
                NewContent = dto.Content,
                Reason = dto.Reason,
                ChangedById = userId
            };

            segment.Content = dto.Content;
            segment.UpdatedAt = DateTime.UtcNow;

            _context.SegmentHistories.Add(history);
            await _context.SaveChangesAsync();

            return segment.ToDto();
        }

        public async Task<SegmentDto> TransitionStatusAsync(int id, ValidationStatus newStatus, int userId)
        {
            var segment = await _context.Segments
                .Include(s => s.Version)
                .FirstOrDefaultAsync(s => s.Id == id)
                ?? throw new NotFoundException("Segment", id);

            ValidateTransition(segment.ValidationStatus, newStatus);

            segment.ValidationStatus = newStatus;
            segment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return segment.ToDto();
        }

        // ===== PRIVATE HELPERS =====

        private static void ValidateTransition(ValidationStatus current, ValidationStatus next)
        {
            var allowed = current switch
            {
                ValidationStatus.Draft        => [ValidationStatus.SelfReview],
                ValidationStatus.SelfReview   => [ValidationStatus.Draft, ValidationStatus.FinalReview],
                ValidationStatus.FinalReview  => [ValidationStatus.SelfReview, ValidationStatus.Approved, ValidationStatus.NeedsRevision],
                ValidationStatus.NeedsRevision => [ValidationStatus.Draft],
                ValidationStatus.Approved     => [ValidationStatus.FinalReview],
                _ => Array.Empty<ValidationStatus>()
            };

            if (!allowed.Contains(next))
                throw new ValidationException(
                    $"Cannot transition from '{current}' to '{next}'. " +
                    $"Allowed: {string.Join(", ", allowed)}.");
        }
    }
}
