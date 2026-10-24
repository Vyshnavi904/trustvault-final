using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrustVault.API.DTOs.Documents;
using TrustVault.API.Extensions;
using TrustVault.Core.Interfaces;

namespace TrustVault.API.Controllers;

[ApiController]
[Route("api/documents")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documents;

    public DocumentsController(IDocumentService documents)
    {
        _documents = documents;
    }

    [HttpGet]
    public async Task<ActionResult<DocumentListDto>> Search(
        [FromQuery] string? q,
        [FromQuery] string? category,
        [FromQuery] Guid? ownerId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = User.GetUserId();
        var result = await _documents.SearchAsync(
            new DocumentSearchQuery(q, category, ownerId, from, to, page, pageSize), userId);

        return Ok(new DocumentListDto
        {
            Documents = result.Documents.Select(DocumentDto.From),
            Total = result.Total,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpPost]
    [RequestSizeLimit(52_428_800)]
    public async Task<ActionResult<DocumentDto>> Upload(
        [FromForm] string title,
        [FromForm] string description,
        [FromForm] string category,
        IFormFile file)
    {
        var userId = User.GetUserId();
        using var stream = file.OpenReadStream();
        var doc = await _documents.UploadAsync(new DocumentUploadRequest(
            title, description, category,
            stream, file.FileName, file.ContentType,
            file.Length, userId));

        return CreatedAtAction(nameof(GetById), new { id = doc.Id }, DocumentDto.From(doc));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DocumentDto>> GetById(Guid id)
    {
        var userId = User.GetUserId();
        var doc = await _documents.GetByIdAsync(id, userId);
        return Ok(DocumentDto.From(doc));
    }

    [HttpPut("{id:guid}")]
    [RequestSizeLimit(52_428_800)]
    public async Task<ActionResult<DocumentDto>> Update(
        Guid id,
        [FromForm] string title,
        [FromForm] string description,
        [FromForm] string category,
        IFormFile? file)
    {
        var userId = User.GetUserId();

        Stream? stream = null;
        try
        {
            stream = file?.OpenReadStream();
            var doc = await _documents.UpdateAsync(new DocumentUpdateRequest(
                id, title, description, category, userId,
                stream, file?.FileName, file?.ContentType, file?.Length ?? 0));
            return Ok(DocumentDto.From(doc));
        }
        finally
        {
            stream?.Dispose();
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = User.GetUserId();
        await _documents.DeleteAsync(id, userId);
        return NoContent();
    }

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id)
    {
        var userId = User.GetUserId();
        var result = await _documents.DownloadAsync(id, userId,
            HttpContext.GetIpAddress(), HttpContext.GetUserAgent());

        return File(result.FileStream, result.MimeType, result.FileName);
    }
}
