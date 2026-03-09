using System.Diagnostics;
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
        var cycle = 0;

        while (!stoppingToken.IsCancellationRequested) {
            cycle++;
            var providers = ResolveProviders(_options.Providers);
            var cycleWatch = Stopwatch.StartNew();
            var successfulProviders = 0;
            var failedProviders = 0;
            var discovered = 0;
            var processed = 0;

            foreach (var provider in providers) {
                try {
                    var report = await SyncProviderWithRetryAsync(provider, stoppingToken);
                    successfulProviders++;
                    discovered += report.DatsDiscovered;
                    processed += report.DatsProcessed;

                    _logger.LogInformation(
                        "SeedLists sync completed: cycle={Cycle} provider={Provider} discovered={Discovered} processed={Processed} failed={Failed}",
                        cycle,
                        report.Provider,
                        report.DatsDiscovered,
                        report.DatsProcessed,
                        report.DatsFailed);
                } catch (Exception ex) {
                    failedProviders++;
                    _logger.LogError(ex, "SeedLists sync failed after retries: cycle={Cycle} provider={Provider}", cycle, provider);
                    if (_options.StopCycleOnProviderFailure) {
                        _logger.LogWarning("Stopping cycle {Cycle} early due to StopCycleOnProviderFailure=true", cycle);
                        break;
                    }
                }
            }

            cycleWatch.Stop();

            if (_options.EmitCycleSummary) {
                _logger.LogInformation(
                    "SeedLists cycle summary: cycle={Cycle} providers={Providers} success={Success} failed={Failed} discovered={Discovered} processed={Processed} elapsedMs={ElapsedMs}",
                    cycle,
                    providers.Count,
                    successfulProviders,
                    failedProviders,
                    discovered,
                    processed,
                    cycleWatch.ElapsedMilliseconds);
            }

            await Task.Delay(TimeSpan.FromMinutes(Math.Max(1, _options.IntervalMinutes)), stoppingToken);
        }
    }

    private async Task<DatSyncReport> SyncProviderWithRetryAsync(DatProviderKind provider, CancellationToken cancellationToken) {
        var attempts = Math.Max(1, _options.MaxRetryAttempts);

        for (var attempt = 1; attempt <= attempts; attempt++) {
            try {
                return await _datCollectionService.SyncProviderAsync(provider, _options.ForceRefresh, cancellationToken: cancellationToken);
            } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                throw;
            } catch (Exception ex) {
                if (attempt == attempts) {
                    throw new InvalidOperationException($"Provider {provider} failed after {attempts} attempts.", ex);
                }

                _logger.LogWarning(
                    ex,
                    "Provider sync attempt failed: provider={Provider} attempt={Attempt}/{Attempts}; retrying in {DelaySeconds}s",
                    provider,
                    attempt,
                    attempts,
                    Math.Max(1, _options.RetryDelaySeconds));

                await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, _options.RetryDelaySeconds)), cancellationToken);
            }
        }

        throw new InvalidOperationException($"Provider {provider} failed unexpectedly without a result.");
    }

    private static IReadOnlyList<DatProviderKind> ResolveProviders(IEnumerable<string> configured) {
        var output = new List<DatProviderKind>();
        foreach (var value in configured) {
            if (Enum.TryParse<DatProviderKind>(value, true, out var provider) && provider != DatProviderKind.Unknown) {
                output.Add(provider);
            }
        }

        if (output.Count == 0) {
            return [DatProviderKind.Tosec, DatProviderKind.GoodTools, DatProviderKind.NoIntro, DatProviderKind.Mame, DatProviderKind.Mess, DatProviderKind.Redump];
        }

        return output;
    }
}
