namespace SeedLists.Worker;

/// <summary>
/// Background sync schedule and behavior options.
/// </summary>
public sealed class WorkerOptions {
	public int IntervalMinutes { get; set; } = 180;
	public bool ForceRefresh { get; set; }
	public int MaxRetryAttempts { get; set; } = 3;
	public int RetryDelaySeconds { get; set; } = 20;
	public bool StopCycleOnProviderFailure { get; set; }
	public bool EmitCycleSummary { get; set; } = true;
	public string[] Providers { get; set; } = ["Tosec", "GoodTools", "NoIntro", "Mame", "Mess", "Redump", "PleasureDome"];
}
