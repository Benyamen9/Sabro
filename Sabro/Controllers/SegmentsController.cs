using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sabro.Data.Entities;
using Sabro.DTOs.Segments;
using Sabro.Services.Interfaces;
using System.Security.Claims;

namespace Sabro.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SegmentsController(ISegmentService segmentService) : ControllerBase
    {
        private readonly ISegmentService _segmentService = segmentService;

        // GET api/segments/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<SegmentDto>> GetById(int id)
        {
            var segment = await _segmentService.GetByIdAsync(id);
            if (segment == null) return NotFound();
            return Ok(segment);
        }

        // GET api/segments/{versionId}/{book}/{chapter}
        [HttpGet("{versionId:int}/{book}/{chapter}")]
        public async Task<ActionResult<List<SegmentDto>>> GetByChapter(int versionId, string book, string chapter)
        {
            var segments = await _segmentService.GetByChapterAsync(versionId, book, chapter);
            return Ok(segments);
        }

        // GET api/segments/{versionId}/{book}/{chapter}/{verse}
        [HttpGet("{versionId:int}/{book}/{chapter}/{verse:int}")]
        public async Task<ActionResult<SegmentDto>> GetByVerse(int versionId, string book, string chapter, int verse)
        {
            var segment = await _segmentService.GetByVerseAsync(versionId, book, chapter, verse);
            if (segment == null) return NotFound();
            return Ok(segment);
        }

        // GET api/segments?versionId=1&book=GEN&page=1&pageSize=50
        [HttpGet]
        public async Task<ActionResult<List<SegmentDto>>> GetFiltered([FromQuery] SegmentFilterDto filter)
        {
            var result = await _segmentService.GetFilteredAsync(filter);
            return Ok(result);
        }

        // POST api/segments
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<SegmentDto>> Create([FromBody] CreateSegmentDto dto)
        {
            var segment = await _segmentService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = segment.Id }, segment);
        }

        // PUT api/segments/{id}
        [HttpPut("{id:int}")]
        [Authorize]
        public async Task<ActionResult<SegmentDto>> Update(int id, [FromBody] UpdateSegmentDto dto)
        {
            var userId = GetUserId();
            var segment = await _segmentService.UpdateAsync(id, dto, userId);
            return Ok(segment);
        }

        // PATCH api/segments/{id}/status
        [HttpPatch("{id:int}/status")]
        [Authorize]
        public async Task<ActionResult<SegmentDto>> TransitionStatus(int id, [FromBody] UpdateValidationStatusDto dto)
        {
            var userId = GetUserId();
            var segment = await _segmentService.TransitionStatusAsync(id, dto.NewStatus, userId);
            return Ok(segment);
        }

        private int GetUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("User ID claim is missing.");
            return int.Parse(claim);
        }
    }
}
