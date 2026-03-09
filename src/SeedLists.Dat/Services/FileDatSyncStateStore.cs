using System.Text.Json;
using SeedLists.Dat.Abstractions;
using SeedLists.Dat.Options;
using Microsoft.Extensions.Options;

namespace SeedLists.Dat.Services;

/// <summary>
/// JSON file-backed state storage for provider sync timestamps.
/// </summary>
public sealed class FileDatSyncStateStore : IDatSyncStateStore {
	private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };
	private readonly string _stateFilePath;
	private readonly SemaphoreSlim _gate = new(1, 1);

	public FileDatSyncStateStore(IOptions<SeedListsDatOptions> options) {
		var stateDir = options.Value.StateDirectory;
		Directory.CreateDirectory(stateDir);
		_stateFilePath = Path.Combine(stateDir, "provider-sync-state.json");
	}

	public async Task<DateTimeOffset?> GetDateTimeAsync(string key, CancellationToken cancellationToken = default) {
		ArgumentException.ThrowIfNullOrWhiteSpace(key);

		await _gate.WaitAsync(cancellationToken);
		try {
			var state = await ReadStateAsync(cancellationToken);
			if (!state.TryGetValue(key, out var raw)) {
				return null;
			}

			return DateTimeOffset.TryParse(raw, out var parsed) ? parsed : null;
		} finally {
			_gate.Release();
		}
	}

	public async Task SetDateTimeAsync(string key, DateTimeOffset value, CancellationToken cancellationToken = default) {
		ArgumentException.ThrowIfNullOrWhiteSpace(key);

		await _gate.WaitAsync(cancellationToken);
		try {
			var state = await ReadStateAsync(cancellationToken);
			state[key] = value.UtcDateTime.ToString("O");
			await WriteStateAsync(state, cancellationToken);
		} finally {
			_gate.Release();
		}
	}

	public async Task<string?> GetStringAsync(string key, CancellationToken cancellationToken = default) {
		ArgumentException.ThrowIfNullOrWhiteSpace(key);

		await _gate.WaitAsync(cancellationToken);
		try {
			var state = await ReadStateAsync(cancellationToken);
			return state.TryGetValue(key, out var value) ? value : null;
		} finally {
			_gate.Release();
		}
	}

	public async Task SetStringAsync(string key, string value, CancellationToken cancellationToken = default) {
		ArgumentException.ThrowIfNullOrWhiteSpace(key);
		ArgumentException.ThrowIfNullOrWhiteSpace(value);

		await _gate.WaitAsync(cancellationToken);
		try {
			var state = await ReadStateAsync(cancellationToken);
			state[key] = value;
			await WriteStateAsync(state, cancellationToken);
		} finally {
			_gate.Release();
		}
	}

	private async Task<Dictionary<string, string>> ReadStateAsync(CancellationToken cancellationToken) {
		if (!File.Exists(_stateFilePath)) {
			return [];
		}

		await using var stream = File.OpenRead(_stateFilePath);
		var data = await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(stream, SerializerOptions, cancellationToken);
		return data ?? [];
	}

	private async Task WriteStateAsync(Dictionary<string, string> state, CancellationToken cancellationToken) {
		await using var stream = new FileStream(_stateFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
		await JsonSerializer.SerializeAsync(stream, state, SerializerOptions, cancellationToken);
	}
}
