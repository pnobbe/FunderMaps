using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FunderMaps.Core.Entities;
using FunderMaps.Core.Interfaces;
using FunderMaps.Core.Interfaces.Repositories;
using FunderMaps.Core.Types;
using Microsoft.Extensions.Configuration;
using FunderMaps.Core.Exceptions;
using FunderMaps.Core.Threading.Command;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading;

#pragma warning disable CA1812 // Internal class is never instantiated
namespace FunderMaps.Core.MapBundle.Jobs
{
    /// <summary>
    ///     Bundle job entry.
    /// </summary>
    internal class BundleJob : CommandTask
    {
        private const string TaskName = "BUNDLE_BUILDING";

        private static SemaphoreSlim storageHandle = new(1);

        private string connectionString;

        private readonly ILayerRepository _layerRepository;
        private readonly IBlobStorageService _blobStorageService;

        private static readonly FormatProperty[] exportFormats =
        {
            new FormatProperty
            {
                Format = GeometryFormat.MapboxVectorTiles,
                FormatName = "MVT",
                FormatShortName = "MVT",
                CommandOptions = "-dsco MINZOOM=14 -dsco MAXZOOM=16 -dsco COMPRESS=NO -dsco MAX_SIZE=25000000",
                ContentType = "application/x-protobuf",
            },
            new FormatProperty
            {
                Format = GeometryFormat.GeoPackage,
                FormatName = "GPKG",
                FormatShortName = "GPKG",
                Extension = ".gpkg",
                ContentType = "application/vnd.sqlite3",
            },
            new FormatProperty
            {
                Format = GeometryFormat.ESRIShapefile,
                FormatName = "ESRI Shapefile",
                FormatShortName = "SHP",
                Extension = ".shp",
                ContentType = "x-gis/x-shapefile",
            },
            new FormatProperty
            {
                Format = GeometryFormat.GeoJSON,
                FormatName = "GeoJSON",
                FormatShortName = "JSON",
                Extension = ".json",
                ContentType = "application/json",
            },
        };

        /// <summary>
        ///     Geometry format properties.
        /// </summary>
        record FormatProperty
        {
            /// <summary>
            ///     Geometry format.
            /// </summary>
            public GeometryFormat Format { get; init; }

            /// <summary>
            ///     Format name as used in system calls.
            /// </summary>
            public string FormatName { get; init; }

            /// <summary>
            ///     Format short name used in directories.
            /// </summary>
            public string FormatShortName { get; init; }

            /// <summary>
            ///     Format file extension.
            /// </summary>
            public string Extension { get; init; }

            /// <summary>
            ///     Format dataset options.
            /// </summary>
            public string CommandOptions { get; init; }

            /// <summary>
            ///     Format file content type.
            /// </summary>
            public string ContentType { get; init; }
        }

        /// <summary>
        ///     Create new instance.
        /// </summary>
        public BundleJob(
            IConfiguration configuration,
            ILayerRepository layerRepository,
            IBlobStorageService blobStorageService)
        {
            _layerRepository = layerRepository ?? throw new ArgumentNullException(nameof(layerRepository));
            _blobStorageService = blobStorageService ?? throw new ArgumentNullException(nameof(blobStorageService));

            connectionString = configuration.GetConnectionString("FunderMapsConnection");
        }

        /// <summary>
        ///     Build the geometry dataset from bundle.
        /// </summary>
        /// <param name="bundle">Bundle to build.</param>
        /// <param name="input">Dataset input.</param>
        /// <param name="format">Output format.</param>
        /// <param name="filterLayer">Filter bundle on layers.</param>
        private async Task<DataSource> BuildBundleAsync(Bundle bundle, DataSource input, GeometryFormat format, bool filterLayer = false)
        {
            FormatProperty formatProperty = exportFormats.First(f => f.Format == format);
            string blobStoragePath = $"dist/ORG{bundle.OrganizationId}/BND{bundle.Id}/{formatProperty.FormatShortName}";

            FileDataSource fileDump = new()
            {
                Format = format,
                PathPrefix = CreateDirectory(formatProperty.FormatShortName),
                Extension = formatProperty.Extension,
                Name = bundle.Id.ToString(),
            };

            var commandBuilder = new VectorDatasetBuilder(
                new()
                {
                    AdditionalOptions = formatProperty.CommandOptions,
                })
                .InputDataset(input)
                .OutputDataset(fileDump);

            if (filterLayer)
            {
                Layer layer = await _layerRepository.GetByIdAsync(bundle.LayerId);
                commandBuilder.InputLayers(new BundleLayerSource(bundle, layer, Context.Workspace));
            }

            int returnCode = await RunCommandAsync(commandBuilder.Build(formatProperty.FormatName));
            if (returnCode != 0)
            {
                throw new Exception();
            }

            // TODO: We can parallelize deletion of existing files with the uploading part by uploading to a
            //       temporary directory and then moving it when both tasks are complete. For now we wait for
            //       deletion to complete before we upload into the destination directory.
            Logger.LogTrace("Deleting existing files (if they exist)");

            // NOTE: The lock is too aggressive because the critical section is based on the
            //       context of bundle (identifier). However a conditional lock such as DCL is
            //       only conceivable when the thread is synchronized. Serialize entry to all
            //       threads on this and similair sections is a second best solution.
            await storageHandle.WaitAsync();

            try
            {
                await _blobStorageService.RemoveDirectoryAsync(blobStoragePath);

                Logger.LogTrace("Start uploading exported bundle");

                await _blobStorageService.StoreDirectoryAsync(
                    directoryName: blobStoragePath,
                    directoryPath: fileDump.PathPrefix,
                    new Core.Storage.StorageObject
                    {
                        ContentType = formatProperty.ContentType,
                        CacheControl = "public, max-age=10800",
                        IsPublic = true,
                    });
            }
            finally
            {
                storageHandle.Release();
            }

            Logger.LogDebug($"Export of format {formatProperty.FormatName} done");

            return fileDump;
        }

        /// <summary>
        ///     Run the background command.
        /// </summary>
        /// <param name="context">Command task execution context.</param>
        public override async Task ExecuteCommandAsync(CommandTaskContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var bundleBuildingContext = context.Value as BundleBuildingContext;
            if (bundleBuildingContext.Formats is null || !bundleBuildingContext.Formats.Any())
            {
                Logger.LogWarning("No formats listed for export");
                return;
            }

            await File.WriteAllTextAsync($"{context.Workspace}/BUNDLE", bundleBuildingContext.Bundle.ToString());

            List<GeometryFormat> formatList = bundleBuildingContext.Formats.Distinct().ToList();
            formatList.RemoveAll(f => f == GeometryFormat.GeoPackage);

            DataSource localCacheDataSource = await BuildBundleAsync(bundleBuildingContext.Bundle,
                new PostreSQLDataSource(connectionString),
                GeometryFormat.GeoPackage,
                filterLayer: true);

            foreach (var format in formatList)
            {
                await BuildBundleAsync(bundleBuildingContext.Bundle, localCacheDataSource, format);
            }
        }

        /// <summary>
        ///     Method to check if a task can be handeld by this job.
        /// </summary>
        /// <param name="name">The task name.</param>
        /// <param name="value">The task payload.</param>
        /// <returns><c>True</c> if method handles task, false otherwise.</returns>
        public override bool CanHandle(string name, object value)
            => name is not null && name.ToUpperInvariant() == TaskName && value is BundleBuildingContext;
    }
}
#pragma warning restore CA1812 // Internal class is never instantiated
