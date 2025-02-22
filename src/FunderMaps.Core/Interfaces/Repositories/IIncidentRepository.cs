﻿using FunderMaps.Core.Entities;

namespace FunderMaps.Core.Interfaces.Repositories
{
    /// <summary>
    ///     Operations for the incident repository.
    /// </summary>
    public interface IIncidentRepository : IAsyncRepository<Incident, string>
    {
        // TODO; Remove
        IAsyncEnumerable<Incident> ListAllRecentAsync();
    }
}
