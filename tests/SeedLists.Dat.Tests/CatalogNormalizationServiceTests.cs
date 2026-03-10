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
	public void Normalize_ParsesJsonFromSlicedSpanWithoutPaddingLeak() {
		var jsonPayload = Encoding.UTF8.GetBytes("{\"provider\":\"Tosec\",\"games\":[]}");
		var paddedBuffer = new byte[jsonPayload.Length + 16];
		Array.Fill(paddedBuffer, (byte)'x');
		jsonPayload.CopyTo(paddedBuffer.AsSpan(8));

		var service = new CatalogNormalizationService();
		var normalized = service.Normalize(paddedBuffer.AsSpan(8, jsonPayload.Length), DatProviderKind.Tosec, "Sliced Provider");

		using var doc = JsonDocument.Parse(normalized);
		var root = doc.RootElement;

		Assert.Equal("Sliced Provider", root.GetProperty("name").GetString());
		Assert.Equal("Tosec", root.GetProperty("provider").GetString());
		Assert.Equal(JsonValueKind.Array, root.GetProperty("games").ValueKind);
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

	[Theory]
	[InlineData("tosec-sample.dat", DatProviderKind.Tosec, "Sample TOSEC Game", "sample-tosec.bin")]
	[InlineData("nointro-sample.dat", DatProviderKind.NoIntro, "Sample NoIntro Game", "sample-nointro.bin")]
	[InlineData("nointro-sample.dat", DatProviderKind.PleasureDome, "Sample NoIntro Game", "sample-nointro.bin")]
	public void Normalize_MapsXmlLikeFixtureFiles(string fileName, DatProviderKind provider, string expectedGameName, string expectedRomName) {
		var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);
		var payload = File.ReadAllBytes(fixturePath);

		var service = new CatalogNormalizationService();
		var normalized = service.Normalize(payload, provider, fileName);

		using var doc = JsonDocument.Parse(normalized);
		var root = doc.RootElement;
		var game = root.GetProperty("games")[0];
		var rom = game.GetProperty("roms")[0];

		Assert.Equal(provider.ToString(), root.GetProperty("provider").GetString());
		Assert.Equal(expectedGameName, game.GetProperty("name").GetString());
		Assert.Equal(expectedRomName, rom.GetProperty("name").GetString());
	}

	[Fact]
	public void Normalize_MapsGoodToolsFixtureFileIntoGames() {
		var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "goodtools-sample.dat");
		var payload = File.ReadAllBytes(fixturePath);

		var service = new CatalogNormalizationService();
		var normalized = service.Normalize(payload, DatProviderKind.GoodTools, "goodtools-sample.dat");

		using var doc = JsonDocument.Parse(normalized);
		var root = doc.RootElement;

		Assert.Equal("GoodTools", root.GetProperty("provider").GetString());
		Assert.Equal(3, root.GetProperty("games").GetArrayLength());
	}

	[Fact]
	public void Normalize_MalformedTosecFixture_FallsBackToWrapper() {
		var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "malformed-tosec-no-game-name.dat");
		var payload = File.ReadAllBytes(fixturePath);

		var service = new CatalogNormalizationService();
		var normalized = service.Normalize(payload, DatProviderKind.Tosec, "malformed-tosec-no-game-name.dat");

		using var doc = JsonDocument.Parse(normalized);
		var root = doc.RootElement;

		Assert.Equal("Tosec", root.GetProperty("provider").GetString());
		Assert.Equal(0, root.GetProperty("games").GetArrayLength());
		Assert.True(root.TryGetProperty("rawPreview", out _));
	}

	[Fact]
	public void Normalize_MalformedGoodToolsFixture_FallsBackToWrapper() {
		var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "malformed-goodtools-no-rom-lines.dat");
		var payload = File.ReadAllBytes(fixturePath);

		var service = new CatalogNormalizationService();
		var normalized = service.Normalize(payload, DatProviderKind.GoodTools, "malformed-goodtools-no-rom-lines.dat");

		using var doc = JsonDocument.Parse(normalized);
		var root = doc.RootElement;

		Assert.Equal("GoodTools", root.GetProperty("provider").GetString());
		Assert.Equal(0, root.GetProperty("games").GetArrayLength());
		Assert.True(root.TryGetProperty("rawPreview", out _));
	}

	[Fact]
	public void Normalize_MalformedNoIntroFixture_ProducesValidationDiagnostics() {
		var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "malformed-nointro-bad-hash.dat");
		var payload = File.ReadAllBytes(fixturePath);

		var normalization = new CatalogNormalizationService();
		var normalized = normalization.Normalize(payload, DatProviderKind.NoIntro, "malformed-nointro-bad-hash.dat");

		var validation = new CatalogValidationService();
		var result = validation.Validate(normalized);

		Assert.False(result.IsValid);
		Assert.Contains(result.Errors, error => error.Contains("crc32", StringComparison.Ordinal));
		Assert.Contains(result.Errors, error => error.Contains("md5", StringComparison.Ordinal));
		Assert.Contains(result.Errors, error => error.Contains("sha1", StringComparison.Ordinal));
	}
}
