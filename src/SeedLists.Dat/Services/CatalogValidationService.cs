using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using SeedLists.Dat.Abstractions;
using SeedLists.Dat.Models;

namespace SeedLists.Dat.Services;

/// <summary>
/// Validates JSON catalogs using SeedLists schema rules.
/// </summary>
public sealed partial class CatalogValidationService : ICatalogValidationService {
	private static readonly string[] AllowedProviders = ["Unknown", "NoIntro", "Tosec", "GoodTools", "Mame", "Mess", "Redump", "PleasureDome"];

	public CatalogValidationResult Validate(ReadOnlySpan<byte> jsonUtf8) {
		if (jsonUtf8.IsEmpty) {
			return new CatalogValidationResult {
				IsValid = false,
				Errors = ["Payload is empty."],
			};
		}

		try {
			using var doc = JsonDocument.Parse(jsonUtf8.ToArray());
			var root = doc.RootElement;
			var errors = new List<string>();

			if (root.ValueKind != JsonValueKind.Object) {
				errors.Add("Root JSON value must be an object.");
				return new CatalogValidationResult { IsValid = false, Errors = errors };
			}

			var name = GetString(root, "name");
			if (string.IsNullOrWhiteSpace(name)) {
				errors.Add("Missing required property: name.");
			}

			var provider = GetString(root, "provider");
			if (string.IsNullOrWhiteSpace(provider)) {
				errors.Add("Missing required property: provider.");
			} else if (!AllowedProviders.Contains(provider, StringComparer.Ordinal)) {
				errors.Add($"Invalid provider '{provider}'. Allowed values: {string.Join(", ", AllowedProviders)}.");
			}

			if (!root.TryGetProperty("games", out var games) || games.ValueKind != JsonValueKind.Array) {
				errors.Add("Missing required property: games (array).");
			} else {
				ValidateGames(games, errors);
			}

			return new CatalogValidationResult {
				IsValid = errors.Count == 0,
				Errors = errors,
			};
		} catch (JsonException ex) {
			return new CatalogValidationResult {
				IsValid = false,
				Errors = [$"Invalid JSON: {ex.Message}"],
			};
		}
	}

	private static void ValidateGames(JsonElement games, List<string> errors) {
		for (var i = 0; i < games.GetArrayLength(); i++) {
			var game = games[i];
			if (game.ValueKind != JsonValueKind.Object) {
				errors.Add($"games[{i}] must be an object.");
				continue;
			}

			var gameName = GetString(game, "name");
			if (string.IsNullOrWhiteSpace(gameName)) {
				errors.Add($"games[{i}] missing required property: name.");
			}

			if (!game.TryGetProperty("roms", out var roms) || roms.ValueKind != JsonValueKind.Array) {
				errors.Add($"games[{i}] missing required property: roms (array).");
				continue;
			}

			for (var r = 0; r < roms.GetArrayLength(); r++) {
				var rom = roms[r];
				if (rom.ValueKind != JsonValueKind.Object) {
					errors.Add($"games[{i}].roms[{r}] must be an object.");
					continue;
				}

				var romName = GetString(rom, "name");
				if (string.IsNullOrWhiteSpace(romName)) {
					errors.Add($"games[{i}].roms[{r}] missing required property: name.");
				}

				if (!TryGetLong(rom, "size", out var size) || size < 0) {
					errors.Add($"games[{i}].roms[{r}] missing or invalid required property: size.");
				}

				ValidateHash(rom, "crc32", 8, i, r, errors);
				ValidateHash(rom, "crc", 8, i, r, errors);
				ValidateHash(rom, "md5", 32, i, r, errors);
				ValidateHash(rom, "sha1", 40, i, r, errors);
			}
		}
	}

	private static void ValidateHash(JsonElement rom, string key, int expectedLength, int gameIndex, int romIndex, List<string> errors) {
		var value = GetString(rom, key);
		if (string.IsNullOrWhiteSpace(value)) {
			return;
		}

		if (value.Length != expectedLength || !HexRegex().IsMatch(value)) {
			errors.Add($"games[{gameIndex}].roms[{romIndex}].{key} must be {expectedLength} hex characters.");
		}
	}

	private static bool TryGetLong(JsonElement node, string property, out long value) {
		value = 0;
		if (!node.TryGetProperty(property, out var prop)) {
			return false;
		}

		if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt64(out value)) {
			return true;
		}

		if (prop.ValueKind == JsonValueKind.String && long.TryParse(prop.GetString(), out value)) {
			return true;
		}

		return false;
	}

	private static string? GetString(JsonElement node, string property) {
		if (!node.TryGetProperty(property, out var prop)) {
			return null;
		}

		return prop.ValueKind == JsonValueKind.String ? prop.GetString() : prop.ToString();
	}

	[GeneratedRegex("^[a-fA-F0-9]+$", RegexOptions.Compiled)]
	private static partial Regex HexRegex();
}
