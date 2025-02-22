using FunderMaps.Core.Types;
using System.ComponentModel.DataAnnotations;

namespace FunderMaps.AspNetCore.DataTransferObjects;

/// <summary>
///     Organization user DTO.
/// </summary>
public record OrganizationUserDto : UserDto
{
    /// <summary>
    ///     User role in organization.
    /// </summary>
    [Required]
    public OrganizationRole OrganizationRole { get; set; } // FUTURE: init
}
