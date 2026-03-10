using System.Text.Json;
using SeedLists.Dat.Services;

namespace SeedLists.Dat.Tests;

public sealed class CatalogSchemaContractTests {
	[Fact]
	public void Schema_DefinesRequiredRootPropertiesAndProviderEnum() {
		using var schema = JsonDocument.Parse(ReadAsset("Schemas", "seedlists.catalog.schema.json"));
		var root = schema.RootElement;

		var required = root.GetProperty("required").EnumerateArray().Select(item => item.GetString()).ToArray();
		Assert.Contains("name", required);
		Assert.Contains("provider", required);
		Assert.Contains("games", required);

		var providers = root
			.GetProperty("properties")
			.GetProperty("provider")
			.GetProperty("enum")
			.EnumerateArray()
			.Select(item => item.GetString() ?? string.Empty)
			.ToArray();

		Assert.Equal(["Unknown", "NoIntro", "Tosec", "GoodTools", "Mame", "Mess", "Redump", "PleasureDome"], providers);
	}

	[Fact]
	public void SampleCatalog_IsValidAgainstRuntimeValidationContract() {
		var sample = ReadAsset("Examples", "sample-catalog.json");
		var validator = new CatalogValidationService();

		var result = validator.Validate(sample);

		Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Errors));
	}

	private static byte[] ReadAsset(params string[] segments) {
		var path = Path.Combine([AppContext.BaseDirectory, .. segments]);
		return File.ReadAllBytes(path);
	}
}
