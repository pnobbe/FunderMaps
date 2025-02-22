namespace FunderMaps.Core.Email;

/// <summary>
///     Dummy email service.
/// </summary>
internal class NullEmailService : IEmailService
{
    /// <summary>
    ///     Send email message.
    /// </summary>
    /// <param name="emailMessage">Message to send.</param>
    /// <param name="token">Cancellation token.</param>
    public Task SendAsync(EmailMessage emailMessage, CancellationToken token) => Task.CompletedTask;

    /// <summary>
    ///     Test the email service backend.
    /// </summary>
    public Task HealthCheck() => Task.CompletedTask;
}
