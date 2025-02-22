using FunderMaps.Core.Entities;
using FunderMaps.Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace FunderMaps.Webservice.Controllers;

/// <summary>
///     Controller for all product quotas.
/// </summary>
[Route("quota")]
public sealed class QuotaController : ControllerBase
{
    private readonly ITelemetryRepository _telemetryRepository;

    /// <summary>
    ///     Create new instance.
    /// </summary>
    public QuotaController(ITelemetryRepository telemetryRepository)
        => _telemetryRepository = telemetryRepository ?? throw new ArgumentNullException(nameof(telemetryRepository));

    // GET: api/quota/usage
    /// <summary>
    ///     Request product quota usage.
    /// </summary>
    [HttpGet("usage")]
    public IAsyncEnumerable<ProductTelemetry> GetQuotaUsageAsync()
        => _telemetryRepository.ListAllUsageAsync();
}
