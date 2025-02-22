﻿using FunderMaps.AspNetCore.DataTransferObjects;
using FunderMaps.Core.Types;
using FunderMaps.IntegrationTests.Faker;
using System.Net;
using Xunit;

namespace FunderMaps.IntegrationTests.Backend.Report;

public class IncidentTests : IClassFixture<BackendFixtureFactory>
{
    private BackendFixtureFactory Factory { get; }

    /// <summary>
    ///     Create new instance.
    /// </summary>
    public IncidentTests(BackendFixtureFactory factory)
        => Factory = factory;

    [Fact]
    public async Task UploadDocumentReturnDocument()
    {
        // Arrange
        using var formContent = new FileUploadContent(mediaType: "application/pdf", fileExtension: "pdf");
        using var client = Factory.CreateClient();

        // Act
        var response = await client.PostAsync("api/incident/upload-document", formContent);
        var returnObject = await response.Content.ReadFromJsonAsync<DocumentDto>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(returnObject.Name);
    }

    [Fact]
    public async Task UploadEmptyFormReturnBadRequest()
    {
        // Arrange
        using var formContent = new MultipartFormDataContent();
        using var client = Factory.CreateClient(OrganizationRole.Writer);

        // Act
        var response = await client.PostAsync("api/incident/upload-document", formContent);
        var returnObject = await response.Content.ReadFromJsonAsync<ProblemModel>();

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal((short)HttpStatusCode.BadRequest, returnObject.Status);
        Assert.Contains("validation", returnObject.Title, StringComparison.InvariantCultureIgnoreCase);
    }

    [Fact]
    public async Task UploadEmptyDocumentReturnBadRequest()
    {
        // Arrange
        using var formContent = new FileUploadContent(
            mediaType: "application/pdf",
            fileExtension: "pdf",
            byteContentLength: 0);
        using var client = Factory.CreateClient(OrganizationRole.Writer);

        // Act
        var response = await client.PostAsync("api/incident/upload-document", formContent);
        var returnObject = await response.Content.ReadFromJsonAsync<ProblemModel>();

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal((short)HttpStatusCode.BadRequest, returnObject.Status);
        Assert.Contains("validation", returnObject.Title, StringComparison.InvariantCultureIgnoreCase);
    }

    [Fact]
    public async Task UploadForbiddenDocumentReturnBadRequest()
    {
        // Arrange
        using var formContent = new FileUploadContent(
            mediaType: "font/woff",
            fileExtension: "woff");
        using var client = Factory.CreateClient(OrganizationRole.Writer);

        // Act
        var response = await client.PostAsync("api/incident/upload-document", formContent);
        var returnObject = await response.Content.ReadFromJsonAsync<ProblemModel>();

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal((short)HttpStatusCode.BadRequest, returnObject.Status);
        Assert.Contains("validation", returnObject.Title, StringComparison.InvariantCultureIgnoreCase);
    }

    [Fact]
    public async Task IncidentLifeCycle()
    {
        var incident = await ReportStub.CreateIncidentAsync(Factory);

        {
            // Arrange
            using var client = Factory.CreateClient();

            if (incident.DocumentFile.Length > 0)
            {
                // Act
                var response = await client.GetAsync($"api/incident/{incident.Id}/download");
                var returnObject = await response.Content.ReadFromJsonAsync<BlobAccessLinkDto>();

                // Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal("https", returnObject.AccessLink.Scheme);
            }
            else
            {
                // Act
                var response = await client.GetAsync($"api/incident/{incident.Id}/download");

                // Assert
                Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            }
        }

        {
            // Arrange
            using var client = Factory.CreateClient();

            // Act
            var response = await client.GetAsync($"api/incident/stats");
            var returnObject = await response.Content.ReadFromJsonAsync<DatasetStatsDto>();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(returnObject.Count >= 1);
        }

        {
            // Arrange
            using var client = Factory.CreateClient();

            // Act
            var response = await client.GetAsync($"api/incident/{incident.Id}");
            var returnObject = await response.Content.ReadFromJsonAsync<IncidentDto>();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.StartsWith("FIR", returnObject.Id, StringComparison.InvariantCulture);
            Assert.Equal(AuditStatus.Todo, returnObject.AuditStatus);
        }

        {
            // Arrange
            using var client = Factory.CreateClient();

            // Act
            var response = await client.GetAsync($"api/incident");
            var returnList = await response.Content.ReadFromJsonAsync<List<IncidentDto>>();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(returnList.Count >= 1);
        }

        {
            // Arrange
            using var client = Factory.CreateClient(OrganizationRole.Writer);
            var newObject = new IncidentDtoFaker()
                .RuleFor(f => f.Address, f => "gfm-bf4771778fd64beca7acd633ce10a570")
                .Generate();

            // Act
            var response = await client.PutAsJsonAsync($"api/incident/{incident.Id}", newObject);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        await ReportStub.DeleteIncidentAsync(Factory, incident);
    }
}
