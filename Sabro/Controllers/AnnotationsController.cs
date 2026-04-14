using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sabro.DTOs.Annotations;
using Sabro.Services.Interfaces;
using System.Security.Claims;

namespace Sabro.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnnotationsController(IAnnotationService annotationService) : ControllerBase
    {
        private readonly IAnnotationService _annotationService = annotationService;

        // GET api/annotations/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<AnnotationDto>> GetById(int id)
        {
            var annotation = await _annotationService.GetByIdAsync(id);
            if (annotation == null) return NotFound();
            return Ok(annotation);
        }

        // GET api/annotations?segmentId=1&authorIds=1,2&page=1
        [HttpGet]
        public async Task<ActionResult> GetFiltered([FromQuery] AnnotationFilterDto filter)
        {
            var result = await _annotationService.GetFilteredAsync(filter);
            return Ok(result);
        }

        // GET api/annotations/by-segment/{segmentId}
        [HttpGet("by-segment/{segmentId:int}")]
        public async Task<ActionResult<List<AnnotationDto>>> GetBySegment(int segmentId)
        {
            var annotations = await _annotationService.GetBySegmentIdAsync(segmentId);
            return Ok(annotations);
        }

        // GET api/annotations/by-author/{authorId}?segmentId=1
        [HttpGet("by-author/{authorId:int}")]
        public async Task<ActionResult<List<AnnotationDto>>> GetByAuthor(int authorId, [FromQuery] int? segmentId)
        {
            var annotations = await _annotationService.GetByAuthorIdAsync(authorId, segmentId);
            return Ok(annotations);
        }

        // POST api/annotations
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<AnnotationDto>> Create([FromBody] CreateAnnotationDto dto)
        {
            var userId = GetUserId();
            var annotation = await _annotationService.CreateAsync(dto, userId);
            return CreatedAtAction(nameof(GetById), new { id = annotation.Id }, annotation);
        }

        // PUT api/annotations/{id}
        [HttpPut("{id:int}")]
        [Authorize]
        public async Task<ActionResult<AnnotationDto>> Update(int id, [FromBody] UpdateAnnotationDto dto)
        {
            var userId = GetUserId();
            var annotation = await _annotationService.UpdateAsync(id, dto, userId);
            return Ok(annotation);
        }

        // DELETE api/annotations/{id}
        [HttpDelete("{id:int}")]
        [Authorize]
        public async Task<ActionResult> Delete(int id)
        {
            var userId = GetUserId();
            var deleted = await _annotationService.DeleteAsync(id, userId);
            if (!deleted) return NotFound();
            return NoContent();
        }

        // PATCH api/annotations/{id}/publish
        [HttpPatch("{id:int}/publish")]
        [Authorize]
        public async Task<ActionResult> TogglePublish(int id)
        {
            var userId = GetUserId();
            var toggled = await _annotationService.TogglePublishAsync(id, userId);
            if (!toggled) return NotFound();
            return NoContent();
        }

        // POST api/annotations/{id}/validate-anchors
        [HttpPost("{id:int}/validate-anchors")]
        [Authorize]
        public async Task<ActionResult> ValidateAnchors(int id)
        {
            await _annotationService.ValidateAnchorsAsync(id);
            return NoContent();
        }

        private int GetUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("User ID claim is missing.");
            return int.Parse(claim);
        }
    }
}
