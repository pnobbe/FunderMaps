namespace FunderMaps.WebApi;

/// <summary>
///     Application entry.
/// </summary>
public static class Program
{
    /// <summary>
    ///     Application entry point.
    /// </summary>
    /// <param name="args">Commandline arguments.</param>
    public static async Task Main(string[] args)
        => await CreateHostBuilder(args).Build().RunAsync();

    /// <summary>
    ///     Build a host and run the application.
    /// </summary>
    /// <remarks>
    ///     The signature of this method should not be changed.
    ///     External tooling expects this function be present.
    /// </remarks>
    /// <param name="args">Commandline arguments.</param>
    /// <returns>See <see cref="IHostBuilder"/>.</returns>
    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>()
                    .UseSetting(WebHostDefaults.HostingStartupAssembliesKey, "FunderMaps.AspNetCore");
            });
}
