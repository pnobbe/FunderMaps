using AutoMapper;
using FunderMaps.AspNetCore.DataAnnotations;
using FunderMaps.AspNetCore.DataTransferObjects;
using FunderMaps.Core.Entities;
using FunderMaps.Core.Exceptions;
using FunderMaps.Core.Helpers;
using FunderMaps.Core.Interfaces;
using FunderMaps.Core.Interfaces.Repositories;
using FunderMaps.Core.Types;
using FunderMaps.WebApi.DataTransferObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace FunderMaps.WebApi.Controllers.Report;

/// <summary>
///     Endpoint controller for inquiry operations.
/// </summary>
[Route("inquiry")]
public class InquiryController : ControllerBase
{
    private readonly IMapper _mapper;
    private readonly Core.AppContext _appContext;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IUserRepository _userRepository;
    private readonly IInquiryRepository _inquiryRepository;
    private readonly IBlobStorageService _blobStorageService;
    // private readonly INotifyService _notifyService;

    /// <summary>
    ///     Create new instance.
    /// </summary>
    public InquiryController(
        IMapper mapper,
        Core.AppContext appContext,
        IOrganizationRepository organizationRepository,
        IUserRepository userRepository,
        IInquiryRepository inquiryRepository,
        IBlobStorageService blobStorageService)
    {
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _appContext = appContext ?? throw new ArgumentNullException(nameof(appContext));
        _organizationRepository = organizationRepository ?? throw new ArgumentNullException(nameof(organizationRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _inquiryRepository = inquiryRepository ?? throw new ArgumentNullException(nameof(inquiryRepository));
        _blobStorageService = blobStorageService ?? throw new ArgumentNullException(nameof(blobStorageService));
        // _notifyService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
    }

    // GET: api/inquiry/stats
    /// <summary>
    ///     Return inquiry statistics.
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStatsAsync()
    {
        // Map.
        DatasetStatsDto output = new()
        {
            Count = await _inquiryRepository.CountAsync(),
        };

        // Return.
        return Ok(output);
    }

    // GET: api/inquiry/{id}
    /// <summary>
    ///     Return inquiry by id.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetAsync(int id)
    {
        // Act.
        var inquiry = await _inquiryRepository.GetByIdAsync(id);

        // Map.
        var output = _mapper.Map<InquiryDto>(inquiry);

        // Return.
        return Ok(output);
    }

    // GET: api/inquiry
    /// <summary>
    ///     Return all inquiries.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllAsync([FromQuery] PaginationDto pagination)
    {
        // Act.
        IAsyncEnumerable<InquiryFull> organizationList = _inquiryRepository.ListAllAsync(pagination.Navigation);

        // Map.
        var output = await _mapper.MapAsync<IList<InquiryDto>, InquiryFull>(organizationList);

        // Return.
        return Ok(output);
    }

    // POST: api/inquiry
    /// <summary>
    ///     Create inquiry.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "WriterAdministratorPolicy")]
    public async Task<IActionResult> CreateAsync([FromBody] InquiryDto input)
    {
        // Map.
        var inquiry = _mapper.Map<InquiryFull>(input);
        if (_appContext.UserId == input.Reviewer)
        {
            throw new AuthorizationException();
        }

        // Act.
        inquiry = await _inquiryRepository.AddGetAsync(inquiry);

        // Map.
        var output = _mapper.Map<InquiryDto>(inquiry);

        // Return.
        return Ok(output);
    }

    // POST: api/inquiry/upload-document
    /// <summary>
    ///     Upload document to the backstore.
    /// </summary>
    [HttpPost("upload-document")]
    [RequestSizeLimit(128 * 1024 * 1024)]
    [Authorize(Policy = "WriterAdministratorPolicy")]
    public async Task<IActionResult> UploadDocumentAsync([Required][FormFile(Core.Constants.AllowedFileMimes)] IFormFile input)
    {
        // Act.
        var storeFileName = FileHelper.GetUniqueName(input.FileName);
        await _blobStorageService.StoreFileAsync(
            containerName: Core.Constants.InquiryStorageFolderName,
            fileName: storeFileName,
            contentType: input.ContentType,
            stream: input.OpenReadStream());

        DocumentDto output = new()
        {
            Name = storeFileName,
        };

        // Return.
        return Ok(output);
    }

    // GET: api/inquiry/download
    /// <summary>
    ///     Retrieve document access link.
    /// </summary>
    [HttpGet("{id:int}/download")]
    public async Task<IActionResult> GetDocumentAccessLinkAsync(int id)
    {
        // Act.
        InquiryFull inquiry = await _inquiryRepository.GetByIdAsync(id);
        Uri link = await _blobStorageService.GetAccessLinkAsync(
            containerName: Core.Constants.InquiryStorageFolderName,
            fileName: inquiry.DocumentFile,
            hoursValid: 1);

        // Map.
        BlobAccessLinkDto result = new()
        {
            AccessLink = link
        };

        // Return.
        return Ok(result);
    }

    // PUT: api/inquiry/{id}
    /// <summary>
    ///     Update inquiry by id.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = "WriterAdministratorPolicy")]
    public async Task<IActionResult> UpdateAsync(int id, [FromBody] InquiryDto input)
    {
        // Map.
        var inquiry = _mapper.Map<InquiryFull>(input);
        inquiry.Id = id;

        InquiryFull inquiry_existing = await _inquiryRepository.GetByIdAsync(id);
        if (inquiry_existing.Attribution.Creator == input.Reviewer)
        {
            throw new AuthorizationException();
        }

        // Act.
        await _inquiryRepository.UpdateAsync(inquiry);

        // FUTURE: Does this make sense?
        // Only when this item was rejected can we move into
        // a pending state after update.
        if (inquiry.State.AuditStatus == AuditStatus.Rejected)
        {
            // Transition.
            inquiry.State.TransitionToPending();

            // Act.
            await _inquiryRepository.SetAuditStatusAsync(inquiry.Id, inquiry);
        }

        // Return.
        return NoContent();
    }

    // POST: api/inquiry/{id}/reset
    /// <summary>
    ///     Reset inquiry status to pending by id.
    /// </summary>
    [HttpPost("{id:int}/reset")]
    [Authorize(Policy = "SuperuserAdministratorPolicy")]
    public async Task<IActionResult> ResetAsync(int id)
    {
        // Act.
        InquiryFull inquiry = await _inquiryRepository.GetByIdAsync(id);

        // Transition.
        inquiry.State.TransitionToPending();

        // Act.
        await _inquiryRepository.SetAuditStatusAsync(inquiry.Id, inquiry);

        // Return.
        return NoContent();
    }

    // POST: api/inquiry/{id}/status_review
    /// <summary>
    ///     Set inquiry status to review by id.
    /// </summary>
    [HttpPost("{id:int}/status_review")]
    [Authorize(Policy = "WriterAdministratorPolicy")]
    public async Task<IActionResult> SetStatusReviewAsync(int id)
    {
        // Act.
        InquiryFull inquiry = await _inquiryRepository.GetByIdAsync(id);
        Organization organization = await _organizationRepository.GetByIdAsync(_appContext.TenantId);
        User creator = await _userRepository.GetByIdAsync(inquiry.Attribution.Creator);
        User reviewer = await _userRepository.GetByIdAsync(inquiry.Attribution.Reviewer.Value);

        // Transition.
        inquiry.State.TransitionToReview();

        // Act.
        await _inquiryRepository.SetAuditStatusAsync(inquiry.Id, inquiry);

        // string subject = $"FunderMaps - Rapportage ter review";

        // object header = new
        // {
        //     Title = subject,
        //     Preheader = "Rapportage ter review wordt aangeboden."
        // };

        // string footer = "Dit bericht wordt verstuurd wanneer een rapportage ter review wordt aangeboden.";

        // await _notifyService.NotifyAsync(new()
        // {
        //     Recipients = new List<string> { reviewer.Email },
        //     Subject = subject,
        //     Template = "InquiryReview",
        //     Items = new Dictionary<string, object>
        //         {
        //             { "header", header },
        //             { "footer", footer },
        //             { "creator", creator.ToString() },
        //             { "organization", organization.ToString() },
        //             { "inquiry", inquiry },
        //             { "redirect_link", $"{Request.Scheme}://{Request.Host}/inquiry/{inquiry.Id}" },
        //         },
        // });

        // Return.
        return NoContent();
    }

    // POST: api/inquiry/{id}/status_rejected
    /// <summary>
    ///     Set inquiry status to rejected by id.
    /// </summary>
    [HttpPost("{id:int}/status_rejected")]
    [Authorize(Policy = "VerifierAdministratorPolicy")]
    public async Task<IActionResult> SetStatusRejectedAsync(int id, StatusChangeDto input)
    {
        // Act.
        InquiryFull inquiry = await _inquiryRepository.GetByIdAsync(id);
        Organization organization = await _organizationRepository.GetByIdAsync(_appContext.TenantId);
        User reviewer = await _userRepository.GetByIdAsync(inquiry.Attribution.Reviewer.Value);
        User creator = await _userRepository.GetByIdAsync(inquiry.Attribution.Creator);

        // Transition.
        inquiry.State.TransitionToRejected();

        // Act.
        await _inquiryRepository.SetAuditStatusAsync(inquiry.Id, inquiry);

        // string subject = $"FunderMaps - Rapportage afgekeurd";

        // object header = new
        // {
        //     Title = subject,
        //     Preheader = "Rapportage is afgekeurd."
        // };

        // string footer = "Dit bericht wordt verstuurd wanneer een rapportage is afgekeurd.";

        // await _notifyService.NotifyAsync(new()
        // {
        //     Recipients = new List<string> { creator.Email },
        //     Subject = subject,
        //     Template = "InquiryRejected",
        //     Items = new Dictionary<string, object>
        //         {
        //             { "header", header },
        //             { "footer", footer },
        //             { "reviewer", reviewer.ToString() },
        //             { "organization", organization.ToString() },
        //             { "inquiry", inquiry },
        //             { "message", input.Message },
        //             { "redirect_link", $"{Request.Scheme}://{Request.Host}/inquiry/{inquiry.Id}" },
        //         },
        // });

        // Return.
        return NoContent();
    }

    // POST: api/inquiry/{id}/status_approved
    /// <summary>
    ///     Set inquiry status to done by id.
    /// </summary>
    [HttpPost("{id:int}/status_approved")]
    [Authorize(Policy = "VerifierAdministratorPolicy")]
    public async Task<IActionResult> SetStatusApprovedAsync(int id)
    {
        // Act.
        InquiryFull inquiry = await _inquiryRepository.GetByIdAsync(id);
        Organization organization = await _organizationRepository.GetByIdAsync(_appContext.TenantId);
        User reviewer = await _userRepository.GetByIdAsync(inquiry.Attribution.Reviewer.Value);
        User creator = await _userRepository.GetByIdAsync(inquiry.Attribution.Creator);

        // Transition.
        inquiry.State.TransitionToDone();

        // Act.
        await _inquiryRepository.SetAuditStatusAsync(inquiry.Id, inquiry);

        // string subject = $"FunderMaps - Rapportage goedgekeurd";

        // object header = new
        // {
        //     Title = subject,
        //     Preheader = "Rapportage is goedgekeurd."
        // };

        // string footer = "Dit bericht wordt verstuurd wanneer een rapportage is goedgekeurd.";

        // await _notifyService.NotifyAsync(new()
        // {
        //     Recipients = new List<string> { creator.Email },
        //     Subject = "FunderMaps - Rapportage goedgekeurd",
        //     Template = "InquiryApproved",
        //     Items = new Dictionary<string, object>
        //         {
        //             { "header", header },
        //             { "footer", footer },
        //             { "reviewer", reviewer.ToString() },
        //             { "organization", organization.ToString() },
        //             { "inquiry", inquiry },
        //         },
        // });

        // Return.
        return NoContent();
    }

    // DELETE: api/inquiry/{id}
    /// <summary>
    ///     Delete inquiry by id.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "SuperuserAdministratorPolicy")]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        // Act.
        await _inquiryRepository.DeleteAsync(id);

        // Return.
        return NoContent();
    }
}
