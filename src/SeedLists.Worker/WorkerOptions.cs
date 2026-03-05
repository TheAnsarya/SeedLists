namespace SeedLists.Worker;

/// <summary>
/// Background sync schedule and behavior options.
/// </summary>
public sealed class WorkerOptions {
	public int IntervalMinutes { get; set; } = 180;
	public bool ForceRefresh { get; set; }
	public string[] Providers { get; set; } = ["Tosec", "GoodTools", "NoIntro"];
}
