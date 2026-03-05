namespace SeedLists.Dat.Abstractions;

/// <summary>
/// Persists provider sync state, including last remote run timestamps.
/// </summary>
public interface IDatSyncStateStore {
	Task<DateTimeOffset?> GetDateTimeAsync(string key, CancellationToken cancellationToken = default);
	Task SetDateTimeAsync(string key, DateTimeOffset value, CancellationToken cancellationToken = default);
}
