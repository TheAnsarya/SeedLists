using System.Text;
using System.Text.Json;
using SeedLists.Dat.Models;
using SeedLists.Dat.Services;

namespace SeedLists.Dat.Tests;

public sealed class CatalogNormalizationServiceTests {
	[Fact]
	public void Normalize_WrapsNonJsonIntoCanonicalEnvelope() {
		var payload = Encoding.UTF8.GetBytes("legacy DAT content");
		var service = new CatalogNormalizationService();

		var normalized = service.Normalize(payload, DatProviderKind.GoodTools, "Legacy Source");
		using var doc = JsonDocument.Parse(normalized);
		var root = doc.RootElement;

		Assert.Equal("Legacy Source", root.GetProperty("name").GetString());
		Assert.Equal("GoodTools", root.GetProperty("provider").GetString());
		Assert.Equal(JsonValueKind.Array, root.GetProperty("games").ValueKind);
	}

	[Fact]
	public void Normalize_AddsMissingDefaultsToJsonPayload() {
		var payload = Encoding.UTF8.GetBytes("{\"games\":[]}");
		var service = new CatalogNormalizationService();

		var normalized = service.Normalize(payload, DatProviderKind.Tosec, "From Provider");
		using var doc = JsonDocument.Parse(normalized);
		var root = doc.RootElement;

		Assert.Equal("From Provider", root.GetProperty("name").GetString());
		Assert.Equal("Tosec", root.GetProperty("provider").GetString());
	}

	[Fact]
	public void Normalize_MapsXmlLikeNoIntroCatalogIntoGamesAndRoms() {
		var payload = Encoding.UTF8.GetBytes("""
			<datafile>
				<game name="Alpha Game">
					<description>Alpha Description</description>
					<manufacturer>Alpha Pub</manufacturer>
					<year>1992</year>
					<rom name="alpha.bin" size="42" crc="ABCDEF12" md5="0123456789abcdef0123456789abcdef" sha1="0123456789abcdef0123456789abcdef01234567" status="good" />
				</game>
			</datafile>
			""");

		var service = new CatalogNormalizationService();
		var normalized = service.Normalize(payload, DatProviderKind.NoIntro, "NoIntro Source");

		using var doc = JsonDocument.Parse(normalized);
		var root = doc.RootElement;
		var game = root.GetProperty("games")[0];
		var rom = game.GetProperty("roms")[0];

		Assert.Equal("NoIntro", root.GetProperty("provider").GetString());
		Assert.Equal("Alpha Game", game.GetProperty("name").GetString());
		Assert.Equal("alpha.bin", rom.GetProperty("name").GetString());
		Assert.Equal(42, rom.GetProperty("size").GetInt64());
		Assert.Equal("ABCDEF12", rom.GetProperty("crc32").GetString());
	}

	[Fact]
	public void Normalize_MapsGoodToolsTextLinesIntoGameEntries() {
		var payload = Encoding.UTF8.GetBytes("""
			# goodtools sample
			Super Game (USA).zip
			Another Title (Rev 1).nes
			""");

		var service = new CatalogNormalizationService();
		var normalized = service.Normalize(payload, DatProviderKind.GoodTools, "GoodTools Source");

		using var doc = JsonDocument.Parse(normalized);
		var root = doc.RootElement;

		Assert.Equal("GoodTools", root.GetProperty("provider").GetString());
		Assert.True(root.GetProperty("games").GetArrayLength() >= 2);
	}
}
