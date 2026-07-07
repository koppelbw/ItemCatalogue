using Application.DTOs;
using Application.ServicePorts;
using FluentValidation;
using FluentValidation.Results;
using ItemCatalogueAPI.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Text;

namespace ItemCatalogueAPI.Controllers;

[ApiController]
[Route("api/imports")]
[Produces("application/json")]
public sealed class ImportController(IImportJobService importJobService) : ControllerBase
{
    // 5 MB comfortably fits an ImportOptions.MaxRows-sized CSV; anything bigger is rejected at
    // the request layer before parsing starts.
    private const long MaxUploadBytes = 5 * 1024 * 1024;

    // Matches CsvItemParser's expectations: invariant dates (yyyy-MM-dd), '.' decimals, ';' to
    // separate multiple ItemTypes, and Room/Container/Owner by name or numeric id.
    private const string TemplateCsv =
        "Name,Description,ItemTypes,PurchasePrice,CurrentValue,Brand,Model,SerialNumber,PurchasedFrom,Quantity,Condition,AcquisitionType,PurchaseDate,WarrantyExpiryDate,IsStored,IsShownInUI,Room,Container,Owner,ReleaseDate,ValuationDate,AcquisitionReference\n" +
        "Desk Lamp,LED desk lamp,Electronics,19.99,15.00,Ikea,Tertial,SN-123,Ikea Store,1,Good,Purchased,2024-01-15,2026-01-15,false,false,Garage,,Will,2023-06-01,2025-01-01,INV-001\n" +
        "Bath Towels,Guest towels,Bathroom;Bedding,,,,,,,4,LikeNew,Gift,,,true,false,,Hall Closet,,,,\n";

    // POST api/imports (multipart/form-data with a "file" field)
    // Asynchronous by design: intake parses/resolves/enqueues and returns 202 immediately; the
    // actual inserts happen in the queue-triggered Function. Poll the Location header for progress.
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxUploadBytes)]
    [EnableRateLimiting(RateLimitingOptions.ImportPolicy)]
    [ProducesResponseType(typeof(ImportJobResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ImportJobResponse>> Start(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            // Same 400 problem-details channel as FluentValidation failures.
            throw new ValidationException(
            [
                new ValidationFailure("File", "Upload a non-empty CSV file in the 'file' form field."),
            ]);
        }

        await using var stream = file.OpenReadStream();
        var job = await importJobService.StartImportAsync(stream, file.FileName, cancellationToken);
        return AcceptedAtRoute("GetImportJob", new { id = job.Id }, job);
    }

    // GET api/imports/5 — poll for progress; Status/counts derive from processed chunk markers.
    [HttpGet("{id:int}", Name = "GetImportJob")]
    [ProducesResponseType(typeof(ImportJobResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ImportJobResponse>> GetStatus(int id, CancellationToken cancellationToken)
    {
        var job = await importJobService.GetStatusAsync(id, cancellationToken);
        return Ok(job);
    }

    // GET api/imports/template — downloadable starting point with the exact expected columns.
    [HttpGet("template", Name = "GetImportTemplate")]
    public IActionResult GetTemplate()
        => File(Encoding.UTF8.GetBytes(TemplateCsv), "text/csv", "item-import-template.csv");
}
