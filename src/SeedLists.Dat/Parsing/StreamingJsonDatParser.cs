using System.Text.Json;
using SeedLists.Dat.Abstractions;
using SeedLists.Dat.Models;

namespace SeedLists.Dat.Parsing;

/// <summary>
/// JSON parser for SeedLists DAT payloads.
/// </summary>
public sealed class StreamingJsonDatParser : IDatParser {
	public DatFormat Format => DatFormat.Json;

	public bool CanParse(string filePath) {
		if (!File.Exists(filePath)) {
			return false;
		}

		var extension = Path.GetExtension(filePath);
		return extension.Equals(".json", StringComparison.OrdinalIgnoreCase);
	}

	public async Task<DatFile> ParseAsync(string filePath, IProgress<DatParseProgress>? progress = null, CancellationToken cancellationToken = default) {
		await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
		return await ParseAsync(stream, Path.GetFileName(filePath), progress, cancellationToken);
	}

	public async Task<DatFile> ParseAsync(Stream stream, string fileName, IProgress<DatParseProgress>? progress = null, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(stream);
		ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

		var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
		var root = document.RootElement;

		var dat = new DatFile {
			FileName = fileName,
			Name = GetString(root, "name") ?? Path.GetFileNameWithoutExtension(fileName),
			Description = GetString(root, "description"),
			Version = GetString(root, "version"),
			Author = GetString(root, "author"),
			Homepage = GetString(root, "homepage"),
			System = GetString(root, "system"),
			Provider = ParseProvider(GetString(root, "provider")),
			Format = DatFormat.Json,
		};

		var gamesParsed = 0;
		var romsParsed = 0;
		if (root.TryGetProperty("games", out var gamesProperty) && gamesProperty.ValueKind == JsonValueKind.Array) {
			foreach (var gameElement in gamesProperty.EnumerateArray()) {
				var gameName = GetString(gameElement, "name");
				if (string.IsNullOrWhiteSpace(gameName)) {
					continue;
				}

				var game = new GameEntry {
					Name = gameName,
					Description = GetString(gameElement, "description"),
					Publisher = GetString(gameElement, "publisher"),
					Year = GetString(gameElement, "year"),
				};

				if (gameElement.TryGetProperty("roms", out var romsProperty) && romsProperty.ValueKind == JsonValueKind.Array) {
					foreach (var romElement in romsProperty.EnumerateArray()) {
						var romName = GetString(romElement, "name");
						if (string.IsNullOrWhiteSpace(romName)) {
							continue;
						}

						game.Roms.Add(new RomEntry {
							Name = romName,
							Size = GetInt64(romElement, "size") ?? 0,
							Crc32 = NormalizeHash(GetString(romElement, "crc32") ?? GetString(romElement, "crc")),
							Md5 = NormalizeHash(GetString(romElement, "md5")),
							Sha1 = NormalizeHash(GetString(romElement, "sha1")),
							Status = GetString(romElement, "status"),
						});
					}
				}

				dat.Games.Add(game);
				gamesParsed++;
				romsParsed += game.Roms.Count;
			}
		}

		progress?.Report(new DatParseProgress {
			Phase = "Completed",
			GamesParsed = gamesParsed,
			RomsParsed = romsParsed,
			BytesRead = stream.CanSeek ? stream.Position : 0,
			TotalBytes = stream.CanSeek ? stream.Length : null,
		});

		return dat;
	}

	private static string? GetString(JsonElement node, string propertyName) {
		if (!node.TryGetProperty(propertyName, out var property)) {
			return null;
		}

		return property.ValueKind == JsonValueKind.String ? property.GetString() : property.ToString();
	}

	private static long? GetInt64(JsonElement node, string propertyName) {
		if (!node.TryGetProperty(propertyName, out var property)) {
			return null;
		}

		if (property.ValueKind == JsonValueKind.Number && property.TryGetInt64(out var value)) {
			return value;
		}

		if (property.ValueKind == JsonValueKind.String && long.TryParse(property.GetString(), out value)) {
			return value;
		}

		return null;
	}

	private static DatProviderKind ParseProvider(string? provider) {
		if (string.IsNullOrWhiteSpace(provider)) {
			return DatProviderKind.Unknown;
		}

		if (Enum.TryParse<DatProviderKind>(provider, true, out var parsed)) {
			return parsed;
		}

		return DatProviderKind.Unknown;
	}

	private static string? NormalizeHash(string? hash) {
		return string.IsNullOrWhiteSpace(hash) ? null : hash.ToLowerInvariant();
	}
}
