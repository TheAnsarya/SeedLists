using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using SeedLists.Dat.Abstractions;
using SeedLists.Dat.Models;

namespace SeedLists.Dat.Services;

/// <summary>
/// Provider payload normalization into canonical JSON catalog structure.
/// </summary>
public sealed partial class CatalogNormalizationService : ICatalogNormalizationService {
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
		var mappedCatalog = provider switch {
			DatProviderKind.Tosec => TryMapXmlLikeCatalog(text, provider, sourceName),
			DatProviderKind.NoIntro => TryMapXmlLikeCatalog(text, provider, sourceName),
			DatProviderKind.Mame => TryMapXmlLikeCatalog(text, provider, sourceName),
			DatProviderKind.Mess => TryMapXmlLikeCatalog(text, provider, sourceName),
			DatProviderKind.Redump => TryMapXmlLikeCatalog(text, provider, sourceName),
			DatProviderKind.PleasureDome => TryMapXmlLikeCatalog(text, provider, sourceName),
			DatProviderKind.GoodTools => TryMapGoodToolsCatalog(text, provider, sourceName),
			_ => null,
		};

		if (mappedCatalog is not null) {
			return JsonSerializer.SerializeToUtf8Bytes(mappedCatalog, SerializerOptions);
		}

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
			var node = JsonNode.Parse(payload);
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

	private static JsonObject? TryMapXmlLikeCatalog(string text, DatProviderKind provider, string sourceName) {
		var gameMatches = XmlLikeGameRegex().Matches(text);
		if (gameMatches.Count == 0) {
			return null;
		}

		var games = new JsonArray();
		foreach (Match gameMatch in gameMatches) {
			var gameName = gameMatch.Groups["name"].Value.Trim();
			if (string.IsNullOrWhiteSpace(gameName)) {
				continue;
			}

			var body = gameMatch.Groups["body"].Value;
			var gameNode = new JsonObject {
				["name"] = gameName,
				["description"] = ExtractElementValue(body, "description"),
				["publisher"] = ExtractElementValue(body, "manufacturer"),
				["year"] = ExtractElementValue(body, "year"),
				["roms"] = new JsonArray(),
			};

			var romNodes = (JsonArray)gameNode["roms"]!;
			foreach (Match romMatch in XmlLikeRomRegex().Matches(body)) {
				var romAttributes = ParseAttributes(romMatch.Groups["attrs"].Value);
				if (!romAttributes.TryGetValue("name", out var romName) || string.IsNullOrWhiteSpace(romName)) {
					continue;
				}

				var romNode = new JsonObject {
					["name"] = romName,
					["size"] = TryParseLong(romAttributes, "size") ?? 0,
					["crc32"] = TryGetValue(romAttributes, "crc"),
					["md5"] = TryGetValue(romAttributes, "md5"),
					["sha1"] = TryGetValue(romAttributes, "sha1"),
					["status"] = TryGetValue(romAttributes, "status"),
				};

				romNodes.Add(romNode);
			}

			games.Add(gameNode);
		}

		if (games.Count == 0) {
			return null;
		}

		return new JsonObject {
			["name"] = sourceName,
			["provider"] = provider.ToString(),
			["description"] = "Provider payload mapped from XML-like DAT text.",
			["version"] = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd"),
			["games"] = games,
		};
	}

	private static JsonObject? TryMapGoodToolsCatalog(string text, DatProviderKind provider, string sourceName) {
		var games = new JsonArray();

		foreach (var rawLine in text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)) {
			var line = rawLine.Trim();
			if (line.Length == 0 || line.StartsWith('#') || line.StartsWith(';') || line.StartsWith('[')) {
				continue;
			}

			var romMatch = GoodToolsRomNameRegex().Match(line);
			if (!romMatch.Success) {
				continue;
			}

			var romName = romMatch.Groups["name"].Value.Trim();
			var gameNode = new JsonObject {
				["name"] = Path.GetFileNameWithoutExtension(romName),
				["roms"] = new JsonArray {
					new JsonObject {
						["name"] = romName,
						["size"] = 0,
					}
				},
			};

			games.Add(gameNode);
		}

		if (games.Count == 0) {
			return null;
		}

		return new JsonObject {
			["name"] = sourceName,
			["provider"] = provider.ToString(),
			["description"] = "Provider payload mapped from GoodTools text format heuristics.",
			["version"] = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd"),
			["games"] = games,
		};
	}

	private static Dictionary<string, string> ParseAttributes(string attributeSegment) {
		var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (Match match in AttributeRegex().Matches(attributeSegment)) {
			var key = match.Groups["name"].Value;
			var value = match.Groups["value"].Value;
			if (!string.IsNullOrWhiteSpace(key)) {
				attributes[key] = value;
			}
		}

		return attributes;
	}

	private static string? ExtractElementValue(string body, string elementName) {
		var match = Regex.Match(
			body,
			$"<{elementName}\\b[^>]*>(?<value>.*?)</{elementName}>",
			RegexOptions.IgnoreCase | RegexOptions.Singleline);

		if (!match.Success) {
			return null;
		}

		var value = match.Groups["value"].Value.Trim();
		return string.IsNullOrWhiteSpace(value) ? null : value;
	}

	private static string? TryGetValue(Dictionary<string, string> dictionary, string key) {
		return dictionary.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
			? value
			: null;
	}

	private static long? TryParseLong(Dictionary<string, string> dictionary, string key) {
		if (!dictionary.TryGetValue(key, out var value)) {
			return null;
		}

		return long.TryParse(value, out var parsed) ? parsed : null;
	}

	[GeneratedRegex("<(?:game|machine)\\b[^>]*name=\"(?<name>[^\"]+)\"[^>]*>(?<body>.*?)</(?:game|machine)>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
	private static partial Regex XmlLikeGameRegex();

	[GeneratedRegex("<rom\\b(?<attrs>[^>]*)/?>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
	private static partial Regex XmlLikeRomRegex();

	[GeneratedRegex("(?<name>[A-Za-z0-9 _\\-\\.\\(\\)\\[\\]]+\\.(?:zip|7z|bin|rom|iso|chd|nes|sfc|smc|gba|gb|gbc|n64))", RegexOptions.IgnoreCase)]
	private static partial Regex GoodToolsRomNameRegex();

	[GeneratedRegex("(?<name>[A-Za-z0-9_\\-:]+)\\s*=\\s*\"(?<value>[^\"]*)\"", RegexOptions.IgnoreCase)]
	private static partial Regex AttributeRegex();
}
