
using Microsoft.EntityFrameworkCore;
using Sabro.Data;
using Sabro.Data.Entities;
using Sabro.DTOs.Annotations;
using Sabro.DTOs.Common;
using Sabro.Exeptions;
using Sabro.Mappers;
using Sabro.Services.Interfaces;

namespace Sabro.Services
{
    /// <summary>
    /// Service for managing annotations (patristic commentaries)
    /// </summary>
    public class AnnotationService(ApplicationDbContext context, IMarkdownService markdown) : IAnnotationService
    {
        private readonly ApplicationDbContext _context = context;
        private readonly IMarkdownService _markdown = markdown;

        /// <summary>
        /// Creates a new annotation with anchors and cross-references
        /// </summary>
        public async Task<AnnotationDto> CreateAsync(CreateAnnotationDto dto, int userId)
        {
            // Convert Markdown to HTML
            var contentHtml = _markdown.ConvertToHtml(dto.ContentMarkdown);

            // Create annotation entity
            var annotation = dto.ToEntity(userId, contentHtml);

            _context.Annotations.Add(annotation);
            await _context.SaveChangesAsync();

            // Create anchors
            foreach (var anchorDto in dto.Anchors)
            {
                var anchor = new AnnotationAnchor
                {
                    AnnotationId = annotation.Id,
                    SegmentId = anchorDto.SegmentId,
                    StartOffset = anchorDto.StartOffset,
                    EndOffset = anchorDto.EndOffset,
                    AnchorText = anchorDto.AnchorText,
                    DisplayText = anchorDto.DisplayText
                };

                _context.AnnotationAnchors.Add(anchor);
            }

            // Create cross-references (optional)
            if (dto.CrossReferences != null)
            {
                foreach (var refDto in dto.CrossReferences)
                {
                    var crossRef = new AnnotationCrossReference
                    {
                        AnnotationId = annotation.Id,
                        TargetCanonicalRef = refDto.TargetCanonicalRef,
                        ReferenceType = refDto.ReferenceType
                    };

                    _context.AnnotationCrossReferences.Add(crossRef);
                }
            }

            await _context.SaveChangesAsync();

            return await GetByIdAsync(annotation.Id) ?? throw new NotFoundException("Annotation", annotation.Id);
        }

        /// <summary>
        /// Updates an existing annotation
        /// </summary>
        public async Task<AnnotationDto> UpdateAsync(int id, UpdateAnnotationDto dto, int userId)
        {
            var annotation = await _context.Annotations
                .Include(a => a.Anchors)
                .Include(a => a.CrossReferences)
                .FirstOrDefaultAsync(a => a.Id == id) ?? throw new NotFoundException("Annotation", id);

            // Optimistic locking check
            if (annotation.UpdatedAt != dto.UpdatedAt)
            {
                throw new ConcurrencyException();
            }

            // Update fields
            annotation.Type = dto.Type;
            annotation.AuthorId = dto.AuthorId;
            annotation.SourceId = dto.SourceId;
            annotation.SourceLocation = dto.SourceLocation;
            annotation.ContentMarkdown = dto.ContentMarkdown;
            annotation.ContentHtml = _markdown.ConvertToHtml(dto.ContentMarkdown);
            annotation.UpdatedById = userId;
            annotation.UpdatedAt = DateTime.UtcNow;

            // Update anchors (remove old, add new)
            if (dto.Anchors != null)
            {
                _context.AnnotationAnchors.RemoveRange(annotation.Anchors);

                foreach (var anchorDto in dto.Anchors)
                {
                    var anchor = new AnnotationAnchor
                    {
                        AnnotationId = annotation.Id,
                        SegmentId = anchorDto.SegmentId,
                        StartOffset = anchorDto.StartOffset,
                        EndOffset = anchorDto.EndOffset,
                        AnchorText = anchorDto.AnchorText,
                        DisplayText = anchorDto.DisplayText
                    };

                    _context.AnnotationAnchors.Add(anchor);
                }
            }

            await _context.SaveChangesAsync();

            return await GetByIdAsync(annotation.Id) ?? throw new NotFoundException("Annotation", annotation.Id);
        }

        /// <summary>
        /// Deletes an annotation
        /// </summary>
        public async Task<bool> DeleteAsync(int id, int userId)
        {
            var annotation = await _context.Annotations.FindAsync(id);

            if (annotation == null)
            {
                return false;
            }

            _context.Annotations.Remove(annotation);
            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Gets a single annotation by ID with all related data
        /// </summary>
        public async Task<AnnotationDto?> GetByIdAsync(int id)
        {
            var annotation = await _context.Annotations
                .Include(a => a.Author)
                .Include(a => a.Source)
                .Include(a => a.CreatedBy)
                .Include(a => a.UpdatedBy)
                .Include(a => a.Anchors)
                    .ThenInclude(aa => aa.Segment)
                .Include(a => a.CrossReferences)
                .FirstOrDefaultAsync(a => a.Id == id);

            return annotation?.ToDto();
        }

        /// <summary>
        /// Gets filtered and paginated annotations
        /// </summary>
        public async Task<PagedResult<AnnotationDto>> GetFilteredAsync(AnnotationFilterDto filter)
        {
            var query = BuildFilterQuery(filter);

            // Apply sorting
            query = ApplySorting(query, filter.SortBy);

            // Pagination
            var total = await query.CountAsync();
            var page = filter.Page ?? 1;
            var pageSize = filter.PageSize ?? 50;

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<AnnotationDto>
            {
                Items = [.. items.Select(a => a.ToDto())],
                TotalCount = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize)
            };
        }

        /// <summary>
        /// Gets all annotations for a specific segment
        /// </summary>
        public async Task<List<AnnotationDto>> GetBySegmentIdAsync(int segmentId)
        {
            var annotations = await _context.Annotations
                .Include(a => a.Author)
                .Include(a => a.Source)
                .Include(a => a.CreatedBy)
                .Include(a => a.UpdatedBy)
                .Include(a => a.Anchors)
                    .ThenInclude(aa => aa.Segment)
                .Where(a => a.Anchors.Any(aa => aa.SegmentId == segmentId) && a.Published)
                .OrderBy(a => a.Author != null ? a.Author.Century : 99)
                .ToListAsync();

            return [.. annotations.Select(a => a.ToDto())];
        }

        /// <summary>
        /// Gets all annotations by a specific author
        /// </summary>
        public async Task<List<AnnotationDto>> GetByAuthorIdAsync(int authorId, int? segmentId = null)
        {
            var query = _context.Annotations
                .Include(a => a.Author)
                .Include(a => a.Source)
                .Include(a => a.Anchors)
                    .ThenInclude(aa => aa.Segment)
                .Where(a => a.AuthorId == authorId && a.Published);

            if (segmentId.HasValue)
            {
                query = query.Where(a => a.Anchors.Any(aa => aa.SegmentId == segmentId.Value));
            }

            var annotations = await query
                .OrderBy(a => a.Anchors.First().Segment.Book)
                .ThenBy(a => a.Anchors.First().Segment.Chapter)
                .ThenBy(a => a.Anchors.First().Segment.Verse)
                .ToListAsync();

            return [.. annotations.Select(a => a.ToDto())];
        }

        /// <summary>
        /// Toggles published status of an annotation
        /// </summary>
        public async Task<bool> TogglePublishAsync(int id, int userId)
        {
            var annotation = await _context.Annotations.FindAsync(id);

            if (annotation == null)
            {
                return false;
            }

            annotation.Published = !annotation.Published;
            annotation.UpdatedById = userId;
            annotation.UpdatedAt = DateTime.UtcNow;

            if (annotation.Published)
            {
                annotation.Status = AnnotationStatus.Approved;
            }

            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Validates that all anchors reference valid text ranges
        /// </summary>
        public async Task ValidateAnchorsAsync(int annotationId)
        {
            var anchors = await _context.AnnotationAnchors
                .Include(aa => aa.Segment)
                .Where(aa => aa.AnnotationId == annotationId)
                .ToListAsync();

            foreach (var anchor in anchors)
            {
                if (anchor.StartOffset.HasValue && anchor.EndOffset.HasValue)
                {
                    var segmentLength = anchor.Segment.Content.Length;

                    if (anchor.StartOffset < 0 || anchor.EndOffset > segmentLength)
                    {
                        throw new ValidationException($"Invalid anchor range for segment {anchor.SegmentId}");
                    }

                    if (anchor.StartOffset >= anchor.EndOffset)
                    {
                        throw new ValidationException("StartOffset must be < EndOffset");
                    }

                    // Verify anchor text matches segment content
                    if (!string.IsNullOrEmpty(anchor.AnchorText))
                    {
                        var actualText = anchor.Segment.Content[
                            anchor.StartOffset.Value..anchor.EndOffset.Value];

                        if (actualText != anchor.AnchorText)
                        {
                            throw new ValidationException("Anchor text doesn't match segment content");
                        }
                    }
                }
            }
        }

        // ===== PRIVATE HELPER METHODS =====

        private IQueryable<Annotation> BuildFilterQuery(AnnotationFilterDto filter)
        {
            var query = _context.Annotations
                .Include(a => a.Author)
                .Include(a => a.Source)
                .Include(a => a.Anchors)
                .AsQueryable();

            // Filter by segment
            if (filter.SegmentId.HasValue)
            {
                query = query.Where(a => a.Anchors.Any(aa => aa.SegmentId == filter.SegmentId.Value));
            }

            // Filter by authors
            if (filter.AuthorIds != null && filter.AuthorIds.Count != 0)
            {
                query = query.Where(a => a.AuthorId.HasValue && filter.AuthorIds.Contains(a.AuthorId.Value));
            }

            // Filter by sources
            if (filter.SourceIds != null && filter.SourceIds.Count != 0)
            {
                query = query.Where(a => a.SourceId.HasValue && filter.SourceIds.Contains(a.SourceId.Value));
            }

            // Filter by source types
            if (filter.SourceTypes != null && filter.SourceTypes.Count != 0)
            {
                query = query.Where(a => a.Source != null && filter.SourceTypes.Contains(a.Source.SourceType));
            }

            // Filter by centuries
            if (filter.Centuries != null && filter.Centuries.Count != 0)
            {
                query = query.Where(a => a.Author != null && filter.Centuries.Contains(a.Author.Century));
            }

            // Filter by languages
            if (filter.Languages != null && filter.Languages.Count != 0)
            {
                query = query.Where(a => a.Author != null && filter.Languages.Contains(a.Author.Language));
            }

            // Filter by author categories
            if (filter.Categories != null && filter.Categories.Count != 0)
            {
                query = query.Where(a => a.Author != null && filter.Categories.Contains(a.Author.Category));
            }

            // Filter by annotation types
            if (filter.AnnotationStatuses != null && filter.AnnotationStatuses.Count != 0)
            {
                query = query.Where(a => filter.AnnotationStatuses.Contains(a.Status));
            }

            // TODO Filter by SourceType
            if (filter.OnlyOfficial == true)
            {
                query = query.Where(a => a.IsOfficial == true);
            }

            // Only published (for public API)
            query = query.Where(a => a.Published == true);

            return query;
        }

        private static IQueryable<Annotation> ApplySorting(IQueryable<Annotation> query, string sortBy)
        {
            return sortBy switch
            {
                "chronological" => query.OrderBy(a => a.Author != null ? a.Author.Century : 99)
                                        .ThenBy(a => a.Author != null ? a.Author.Name : ""),
                "reverse_chronological" => query.OrderByDescending(a => a.Author != null ? a.Author.Century : 0)
                                                 .ThenBy(a => a.Author != null ? a.Author.Name : ""),
                "author_name" => query.OrderBy(a => a.Author != null ? a.Author.Name : "")
                                      .ThenBy(a => a.CreatedAt),
                "source_name" => query.OrderBy(a => a.Source != null ? a.Source.Name : "")
                                      .ThenBy(a => a.CreatedAt),
                _ => query.OrderBy(a => a.CreatedAt)
            };
        }
    }
}