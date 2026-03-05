using System.Text;
using SeedLists.Dat.Services;

namespace SeedLists.Dat.Tests;

public sealed class CatalogValidationServiceTests {
	[Fact]
	public void Validate_ReturnsValidForWellFormedCatalog() {
		var json = """
			{
				"name": "Catalog",
				"provider": "Tosec",
				"games": [
					{
						"name": "Game",
						"roms": [
							{
								"name": "game.bin",
								"size": 128,
								"crc32": "abcdef12"
							}
						]
					}
				]
			}
			""";

		var service = new CatalogValidationService();
		var result = service.Validate(Encoding.UTF8.GetBytes(json));

		Assert.True(result.IsValid);
		Assert.Empty(result.Errors);
	}

	[Fact]
	public void Validate_ReturnsErrorsForInvalidCatalog() {
		var json = """
			{
				"provider": "BadProvider",
				"games": [
					{
						"roms": [
							{
								"name": "bad.bin",
								"size": -1,
								"crc32": "xyz"
							}
						]
					}
				]
			}
			""";

		var service = new CatalogValidationService();
		var result = service.Validate(Encoding.UTF8.GetBytes(json));

		Assert.False(result.IsValid);
		Assert.NotEmpty(result.Errors);
		Assert.Contains(result.Errors, error => error.Contains("Missing required property: name", StringComparison.Ordinal));
		Assert.Contains(result.Errors, error => error.Contains("Invalid provider", StringComparison.Ordinal));
	}
}
