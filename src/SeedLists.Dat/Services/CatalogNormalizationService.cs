using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using SeedLists.Dat.Abstractions;
using SeedLists.Dat.Models;

namespace SeedLists.Dat.Services;

/// <summary>
/// Provider payload normalization into canonical JSON catalog structure.
/// </summary>
public sealed class CatalogNormalizationService : ICatalogNormalizationService {
	private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

	public byte[] Normalize(ReadOnlySpan<byte> payload, DatProviderKind provider, string sourceName) {
		if (payload.IsEmpty) {
			throw new InvalidOperationException("Provider payload is empty.");
		}

		if (TryParseJson(payload, out var rootObject)) {
			ApplyDefaults(rootObject!, provider, sourceName);
			return JsonSerializer.SerializeToUtf8Bytes(rootObject, SerializerOptions);
		}

		var text = DecodeBestEffort(payload);
		var wrapped = new JsonObject {
			["name"] = sourceName,
			["provider"] = provider.ToString(),
			["description"] = "Non-JSON provider payload was wrapped into canonical SeedLists JSON envelope.",
			["version"] = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd"),
			["games"] = new JsonArray(),
			["rawPreview"] = text.Length > 4096 ? text[..4096] : text,
		};

		return JsonSerializer.SerializeToUtf8Bytes(wrapped, SerializerOptions);
	}

	private static bool TryParseJson(ReadOnlySpan<byte> payload, out JsonObject? rootObject) {
		rootObject = null;

		try {
			var node = JsonNode.Parse(payload.ToArray());
			if (node is JsonObject obj) {
				rootObject = obj;
				return true;
			}
		} catch (JsonException) {
			return false;
		}

		return false;
	}

	private static void ApplyDefaults(JsonObject root, DatProviderKind provider, string sourceName) {
		if (!root.TryGetPropertyValue("name", out var nameNode) || string.IsNullOrWhiteSpace(nameNode?.GetValue<string>())) {
			root["name"] = sourceName;
		}

		if (!root.TryGetPropertyValue("provider", out var providerNode) || string.IsNullOrWhiteSpace(providerNode?.GetValue<string>())) {
			root["provider"] = provider.ToString();
		}

		if (!root.TryGetPropertyValue("games", out var gamesNode) || gamesNode is not JsonArray) {
			root["games"] = new JsonArray();
		}
	}

	private static string DecodeBestEffort(ReadOnlySpan<byte> payload) {
		try {
			return Encoding.UTF8.GetString(payload);
		} catch {
			return Convert.ToBase64String(payload);
		}
	}
}
