using System.Net;
using System.Security.Cryptography;
using System.Text;
using SeedLists.Dat.Abstractions;

namespace SeedLists.Dat.Providers;

internal static class RemoteDatSupport {
	public static string BuildRemoteIdentifier(string token, string url) {
		var encodedToken = Uri.EscapeDataString(token);
		return $"remote|{encodedToken}|{url}";
	}

	public static bool TryParseRemoteIdentifier(string identifier, out string token, out string url) {
		token = string.Empty;
		url = string.Empty;

		if (!identifier.StartsWith("remote|", StringComparison.OrdinalIgnoreCase)) {
			return false;
		}

		var parts = identifier.Split('|', 3, StringSplitOptions.None);
		if (parts.Length != 3) {
			return false;
		}

		token = Uri.UnescapeDataString(parts[1]);
		url = parts[2];
		return !string.IsNullOrWhiteSpace(url);
	}

	public static string BuildSourceToken(HttpResponseMessage response, long? fileSize = null, DateTimeOffset? lastUpdated = null) {
		var etag = response.Headers.ETag?.Tag?.Trim('"') ?? string.Empty;
		var length = fileSize ?? response.Content.Headers.ContentLength ?? 0;
		var modified = (lastUpdated ?? response.Content.Headers.LastModified)?.UtcDateTime.ToString("O") ?? string.Empty;
		return $"{etag}|{length}|{modified}";
	}

	public static string BuildStateKey(string providerPrefix, string remoteUrl) {
		var urlHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(remoteUrl))).ToLowerInvariant();
		return $"{providerPrefix}:remote:{urlHash}:token";
	}

	public static string BuildPollKey(string providerPrefix) {
		return $"{providerPrefix}:remote:last-poll-utc";
	}

	public static async Task<bool> ShouldPollAsync(
		IDatSyncStateStore stateStore,
		string providerPrefix,
		int pollIntervalHours,
		CancellationToken cancellationToken) {
		if (pollIntervalHours <= 0) {
			return true;
		}

		var lastPoll = await stateStore.GetDateTimeAsync(BuildPollKey(providerPrefix), cancellationToken);
		if (lastPoll is null) {
			return true;
		}

		return (DateTimeOffset.UtcNow - lastPoll.Value) >= TimeSpan.FromHours(pollIntervalHours);
	}

	public static Task MarkPolledAsync(IDatSyncStateStore stateStore, string providerPrefix, CancellationToken cancellationToken) {
		return stateStore.SetDateTimeAsync(BuildPollKey(providerPrefix), DateTimeOffset.UtcNow, cancellationToken);
	}

	public static async Task<bool> HasChangedAsync(
		IDatSyncStateStore stateStore,
		string providerPrefix,
		string remoteUrl,
		string token,
		CancellationToken cancellationToken) {
		var key = BuildStateKey(providerPrefix, remoteUrl);
		var previous = await stateStore.GetStringAsync(key, cancellationToken);
		return !string.Equals(previous, token, StringComparison.Ordinal);
	}

	public static Task SetTokenAsync(
		IDatSyncStateStore stateStore,
		string providerPrefix,
		string remoteUrl,
		string token,
		CancellationToken cancellationToken) {
		var key = BuildStateKey(providerPrefix, remoteUrl);
		return stateStore.SetStringAsync(key, token, cancellationToken);
	}

	public static string NormalizeUrl(string baseUrl, string href) {
		var decoded = WebUtility.HtmlDecode(href);
		if (Uri.TryCreate(decoded, UriKind.Absolute, out var absolute)) {
			return absolute.ToString();
		}

		var root = new Uri(baseUrl);
		return new Uri(root, decoded).ToString();
	}
}
