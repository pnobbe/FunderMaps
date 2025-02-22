﻿using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace FunderMaps.WebApi.DataTransferObjects;

/// <summary>
///     Project DTO.
/// </summary>
public sealed class ProjectDto
{
    /// <summary>
    ///     Project identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    ///     Dossier name.
    /// </summary>
    public string Dossier { get; set; }

    /// <summary>
    ///     Note.
    /// </summary>
    [DataType(DataType.MultilineText)]
    public string Note { get; set; }

    /// <summary>
    ///     Start date.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    ///     End date.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    ///     Adviser user identifier.
    /// </summary>
    [IgnoreDataMember]
    public Guid? Adviser { get; set; }

    /// <summary>
    ///     Lead user identifier.
    /// </summary>
    [IgnoreDataMember]
    public Guid? Lead { get; set; }

    /// <summary>
    ///     Creator user identifier.
    /// </summary>
    [IgnoreDataMember]
    public Guid? Creator { get; set; }

    /// <summary>
    ///     Adviser user.
    /// </summary>
    public object AdviserNavigation { get; set; }

    /// <summary>
    ///     Lead user.
    /// </summary>
    public object LeadNavigation { get; set; }

    /// <summary>
    ///     Creator user.
    /// </summary>
    public object CreatorNavigation { get; set; }

    /// <summary>
    ///     Record create date.
    /// </summary>
    public DateTime CreateDate { get; set; }

    /// <summary>
    ///     Record last update.
    /// </summary>
    public DateTime? UpdateDate { get; set; }

    /// <summary>
    ///     Record delete date.
    /// </summary>
    public DateTime? DeleteDate { get; set; }
}
