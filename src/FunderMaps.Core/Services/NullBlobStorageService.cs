﻿using FunderMaps.Core.Interfaces;
using FunderMaps.Core.Storage;

namespace FunderMaps.Core.Services;

/// <summary>
///     Dummy file storage service.
/// </summary>
internal class NullBlobStorageService : IBlobStorageService
{
    /// <summary>
    ///     Retrieve file access link as uri.
    /// </summary>
    /// <param name="containerName">Storage container.</param>
    /// <param name="fileName">File name.</param>
    /// <param name="hoursValid">How long the link is valid in hours.</param>
    /// <returns>The generated link.</returns>
    public Task<Uri> GetAccessLinkAsync(string containerName, string fileName, double hoursValid)
        => Task.FromResult(new Uri("https://localhost/blob"));

    /// <summary>
    ///     Stores a file in Amazon S3.
    /// </summary>
    /// <param name="containerName">The container name.</param>
    /// <param name="fileName">The file name.</param>
    /// <param name="contentType">The content type.</param>
    /// <param name="stream">See <see cref="Stream"/>.</param>
    /// <param name="storageObject">Storage object settings.</param>
    /// <returns>See <see cref="ValueTask"/>.</returns>
    public Task StoreFileAsync(string containerName, string fileName, string contentType, Stream stream, StorageObject storageObject = null)
        => Task.CompletedTask;

    /// <summary>
    ///     Stores a directory in Amazon S3.
    /// </summary>
    /// <param name="directoryName">Directory name at the destination including prefix paths.</param>
    /// <param name="directoryPath">Source directory.</param>
    /// <param name="storageObject">Storage object settings.</param>
    /// <returns>See <see cref="ValueTask"/>.</returns>
    public Task StoreDirectoryAsync(string directoryName, string directoryPath, StorageObject storageObject = null)
        => Task.CompletedTask;

    /// <summary>
    ///     Removes a directory in Amazon S3.
    /// </summary>
    /// <param name="directoryPath">Full path of the directory to delete.</param>
    /// <returns>See <see cref="ValueTask"/>.</returns>
    public Task RemoveDirectoryAsync(string directoryPath)
        => Task.CompletedTask;

    /// <summary>
    ///     Test the batch service backend.
    /// </summary>
    public Task HealthCheck() => Task.CompletedTask;
}
