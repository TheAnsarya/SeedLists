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
}
