using FunderMaps.Core.Email;
using FunderMaps.Core.Entities;
using FunderMaps.Core.Interfaces;
using FunderMaps.Core.Interfaces.Repositories;
using FunderMaps.Core.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FunderMaps.Core.IncidentReport;

// FUTURE: Revamp this service.
/// <summary>
///     Service to the incidents.
/// </summary>
internal class IncidentService : IIncidentService // TODO: inherit from AppServiceBase
{
    private readonly IncidentOptions _options;
    private readonly IContactRepository _contactRepository;
    private readonly IIncidentRepository _incidentRepository;
    private readonly IGeocoderTranslation _geocoderTranslation;
    private readonly IEmailService _emailService;
    private readonly ILogger<IncidentService> _logger;

    /// <summary>
    ///     Create new instance.
    /// </summary>
    public IncidentService(
        IOptions<IncidentOptions> options,
        IContactRepository contactRepository,
        IIncidentRepository incidentRepository,
        IGeocoderTranslation geocoderTranslation,
        IEmailService emailService,
        ILogger<IncidentService> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _contactRepository = contactRepository ?? throw new ArgumentNullException(nameof(contactRepository));
        _incidentRepository = incidentRepository ?? throw new ArgumentNullException(nameof(incidentRepository));
        _geocoderTranslation = geocoderTranslation ?? throw new ArgumentNullException(nameof(geocoderTranslation));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public static string ToFoundationType(FoundationType? value)
        => value switch
        {
            FoundationType.Wood => "Hout",
            FoundationType.WoodAmsterdam => "Hout",
            FoundationType.WoodRotterdam => "Hout",
            FoundationType.WoodCharger => "Hout",
            FoundationType.WoodRotterdamAmsterdam => "Hout",
            FoundationType.WoodRotterdamArch => "Hout",
            FoundationType.WoodAmsterdamArch => "Hout",
            FoundationType.Concrete => "Beton",
            FoundationType.NoPile => "Niet onderheid",
            FoundationType.NoPileMasonry => "Niet onderheid",
            FoundationType.NoPileStrips => "Niet onderheid",
            FoundationType.NoPileBearingFloor => "Niet onderheid",
            FoundationType.NoPileConcreteFloor => "Niet onderheid",
            FoundationType.NoPileSlit => "Niet onderheid",
            FoundationType.WeightedPile => "Beton met verzwaardepunt",
            FoundationType.Combined => "Gecombineerd",
            FoundationType.SteelPile => "Stalen buispaal",
            FoundationType.Other => "Overig",
            _ => "Onbekend",
        };

    public static string ToBoolean(bool? value)
        => value switch
        {
            true => "Ja",
            false => "Nee",
            _ => "Onbekend"
        };

    public static string ToFoundationDamageCause(FoundationDamageCause? value)
        => value switch
        {
            FoundationDamageCause.Drainage => "Ontwateringsdiepte onvoldoende",
            FoundationDamageCause.Drystand => "Droogstand",
            FoundationDamageCause.ConstructionFlaw => "Verkeerd gefundeerd",
            FoundationDamageCause.Overcharge => "Overbelasting",
            FoundationDamageCause.OverchargeNegativeCling => "Overbelasting/Negatievekleef",
            FoundationDamageCause.NegativeCling => "Negatievekleef",
            FoundationDamageCause.BioInfection => "Bacteriele aantasting",
            FoundationDamageCause.FungusInfection => "Schimmelaantasting",
            FoundationDamageCause.BioFungusInfection => "Schimmel/bacterieen",
            FoundationDamageCause.FoundationFlaw => "Verkeerd gefundeerd",
            FoundationDamageCause.ConstructionHeave => "Woning omhooggedrukt",
            FoundationDamageCause.Subsidence => "Bodemdaling",
            FoundationDamageCause.Vegetation => "Wortels/planten",
            FoundationDamageCause.Gas => "Gaswinning/mijnbouw",
            FoundationDamageCause.Vibrations => "Verkeer",
            FoundationDamageCause.PartialFoundationRecovery => "Naastgelegen funderingsherstel",
            FoundationDamageCause.JapanseKnotweed => "Japanse duizendknoop",
            _ => "Onbekend",
        };

    public static string ToFoundationDamageCharacteristics(FoundationDamageCharacteristics? value)
        => value switch
        {
            FoundationDamageCharacteristics.JammingDoorWindow => "Klemmende ramen/deuren",
            FoundationDamageCharacteristics.Crack => "Scheuren",
            FoundationDamageCharacteristics.Skewed => "Scheefstand",
            FoundationDamageCharacteristics.CrawlspaceFlooding => "Water in kruipruimte",
            FoundationDamageCharacteristics.ThresholdAboveSubsurface => "Maaiveld lager dan dorpel",
            FoundationDamageCharacteristics.ThresholdBelowSubsurface => "Drempel lager dan maaiveld",
            FoundationDamageCharacteristics.CrookedFloorWall => "Scheve vloer",
            _ => "Onbekend",
        };

    public static string ArrayToFoundationDamageCharacteristics(IEnumerable<FoundationDamageCharacteristics> values)
    {
        if (values is not null && values.Any())
        {
            return string.Join(", ", values.Select(value => ToFoundationDamageCharacteristics(value)));
        }
        else
        {
            return "Onbekend";
        }
    }

    public static string ToEnvironmentDamageCharacteristics(EnvironmentDamageCharacteristics? value)
        => value switch
        {
            EnvironmentDamageCharacteristics.Subsidence => "Bodemdaling",
            EnvironmentDamageCharacteristics.SaggingSewerConnection => "Verzakkend riool",
            EnvironmentDamageCharacteristics.SaggingCablesPipes => "Verzakkende kabels/leidingen",
            EnvironmentDamageCharacteristics.Flooding => "Wateroverlast",
            EnvironmentDamageCharacteristics.FoundationDamageNearby => "Funderingschade in wijk",
            EnvironmentDamageCharacteristics.Elevation => "Recent opgehoogd",
            EnvironmentDamageCharacteristics.IncreasingTraffic => "Verkeerstoename",
            EnvironmentDamageCharacteristics.ConstructionNearby => "Werkzaamheden in wijk",
            EnvironmentDamageCharacteristics.VegetationNearby => "Bomen nabij",
            EnvironmentDamageCharacteristics.SewageLeakage => "Lekkend riool",
            EnvironmentDamageCharacteristics.LowGroundWater => "Wateronderlast",
            _ => "Onbekend",
        };

    public static string ArrayToEnvironmentDamageCharacteristics(IEnumerable<EnvironmentDamageCharacteristics> values)
    {
        if (values is not null && values.Any())
        {
            return string.Join(", ", values.Select(value => ToEnvironmentDamageCharacteristics(value)));
        }
        else
        {
            return "Onbekend";
        }
    }

    // FUTURE: split logic, hard to read.
    /// <summary>
    ///     Register a new incident.
    /// </summary>
    /// <param name="incident">Incident to process.</param>
    /// <param name="meta">Optional metadata.</param>
    public async Task<Incident> AddAsync(Incident incident, object? meta = null)
    {
        Address address = await _geocoderTranslation.GetAddressIdAsync(incident.Address);

        incident.Address = address.Id;
        incident.Meta = meta;
        incident.AuditStatus = AuditStatus.Todo;

        var to = new EmailAddress
        {
            Address = incident.ContactNavigation.Email,
            Name = incident.ContactNavigation.Name
        };

        var name = incident.ContactNavigation.Name;
        var email = incident.ContactNavigation.Email;
        var phone = incident.ContactNavigation.PhoneNumber;

        await _contactRepository.AddAsync(incident.ContactNavigation);
        incident.Email = email;
        incident = await _incidentRepository.AddGetAsync(incident);

        await _emailService.SendAsync(new EmailMessage
        {
            ToAddresses = new[] { to },
            Subject = $"Nieuwe melding: {incident.Id}",
            Template = "incident-customer",
            Varaibles = new Dictionary<string, object>
            {
                { "id", incident.Id },
                { "name", name },
                { "phone", phone },
                { "email", email },
                { "address", address.FullAddress },
                { "note", incident.Note },
                { "foundationType", ToFoundationType(incident.FoundationType) },
                { "chainedBuilding", ToBoolean(incident.ChainedBuilding) },
                { "owner", ToBoolean(incident.Owner) },
                { "neighborRecovery", ToBoolean(incident.NeighborRecovery) },
                { "foundationDamageCause", ToFoundationDamageCause(incident.FoundationDamageCause) },
                { "foundationDamageCharacteristics", ArrayToFoundationDamageCharacteristics(incident.FoundationDamageCharacteristics) },
                { "environmentDamageCharacteristics", ArrayToEnvironmentDamageCharacteristics(incident.EnvironmentDamageCharacteristics) },
            }
        });

        foreach (var recipient in _options.Recipients)
        {
            var to2 = new EmailAddress
            {
                Address = recipient,
            };

            await _emailService.SendAsync(new EmailMessage
            {
                ToAddresses = new[] { to2 },
                Subject = $"Nieuwe melding: {incident.Id}",
                Template = "incident-reviewer",
                Varaibles = new Dictionary<string, object>
            {
                { "id", incident.Id },
                { "name", name },
                { "phone", phone },
                { "email", email },
                { "address", address.FullAddress },
                { "note", incident.Note },
                { "foundationType", ToFoundationType(incident.FoundationType) },
                { "chainedBuilding", ToBoolean(incident.ChainedBuilding) },
                { "owner", ToBoolean(incident.Owner) },
                { "neighborRecovery", ToBoolean(incident.NeighborRecovery) },
                { "foundationDamageCause", ToFoundationDamageCause(incident.FoundationDamageCause) },
                { "foundationDamageCharacteristics", ArrayToFoundationDamageCharacteristics(incident.FoundationDamageCharacteristics) },
                { "environmentDamageCharacteristics", ArrayToEnvironmentDamageCharacteristics(incident.EnvironmentDamageCharacteristics) },
            }
            });
        }

        _logger.LogInformation($"Incident {incident.Id} was registered");

        return incident;
    }
}
