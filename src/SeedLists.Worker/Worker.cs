using Microsoft.Extensions.Options;
using SeedLists.Dat.Abstractions;
using SeedLists.Dat.Models;

namespace SeedLists.Worker;

public sealed class Worker(
    ILogger<Worker> logger,
    IDatCollectionService datCollectionService,
    IOptions<WorkerOptions> options) : BackgroundService {
    private readonly ILogger<Worker> _logger = logger;
    private readonly IDatCollectionService _datCollectionService = datCollectionService;
    private readonly WorkerOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        while (!stoppingToken.IsCancellationRequested) {
            foreach (var provider in ResolveProviders(_options.Providers)) {
                try {
                    var report = await _datCollectionService.SyncProviderAsync(provider, _options.ForceRefresh, cancellationToken: stoppingToken);
                    _logger.LogInformation(
                        "SeedLists sync completed: {Provider} discovered={Discovered} processed={Processed} failed={Failed}",
                        report.Provider,
                        report.DatsDiscovered,
                        report.DatsProcessed,
                        report.DatsFailed);
                } catch (Exception ex) {
                    _logger.LogError(ex, "SeedLists sync failed for provider {Provider}", provider);
                }
            }

            await Task.Delay(TimeSpan.FromMinutes(Math.Max(1, _options.IntervalMinutes)), stoppingToken);
        }
    }

    private static IReadOnlyList<DatProviderKind> ResolveProviders(IEnumerable<string> configured) {
        var output = new List<DatProviderKind>();
        foreach (var value in configured) {
            if (Enum.TryParse<DatProviderKind>(value, true, out var provider) && provider != DatProviderKind.Unknown) {
                output.Add(provider);
            }
        }

        if (output.Count == 0) {
            return [DatProviderKind.Tosec, DatProviderKind.GoodTools, DatProviderKind.NoIntro];
        }

        return output;
    }
}
