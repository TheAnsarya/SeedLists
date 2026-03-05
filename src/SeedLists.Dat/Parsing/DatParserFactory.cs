using SeedLists.Dat.Abstractions;
using SeedLists.Dat.Models;

namespace SeedLists.Dat.Parsing;

/// <summary>
/// Default parser factory.
/// </summary>
public sealed class DatParserFactory(IEnumerable<IDatParser> parsers) : IDatParserFactory {
	private readonly IReadOnlyList<IDatParser> _parsers = parsers.ToList();

	public IDatParser? GetParser(string filePath) {
		foreach (var parser in _parsers) {
			if (parser.CanParse(filePath)) {
				return parser;
			}
		}

		return null;
	}

	public IDatParser? GetParser(DatFormat format) {
		return _parsers.FirstOrDefault(p => p.Format == format);
	}

	public IDatParser? GetParser(Stream stream) {
		ArgumentNullException.ThrowIfNull(stream);

		return GetParser(DatFormat.Json);
	}
}
