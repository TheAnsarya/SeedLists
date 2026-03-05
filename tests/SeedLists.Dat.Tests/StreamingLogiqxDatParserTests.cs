using System.Text;
using SeedLists.Dat.Parsing;

namespace SeedLists.Dat.Tests;

public sealed class StreamingJsonDatParserTests {
	[Fact]
	public async Task ParseAsync_ParsesHeaderGameAndRom() {
		var json = """
			{
				"name": "Sample Dat",
				"description": "Sample Description",
				"version": "2026-03-05",
				"homepage": "https://example.invalid",
				"provider": "Tosec",
				"games": [
					{
						"name": "Game A",
						"description": "Game A Desc",
						"publisher": "Publisher",
						"year": "1990",
						"roms": [
							{
								"name": "a.bin",
								"size": 16,
								"crc32": "ABCDEF12",
								"md5": "0011",
								"sha1": "AABBCC",
								"status": "good"
							}
						]
					}
				]
			}
			""";

		var parser = new StreamingJsonDatParser();
		await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json), writable: false);

		var result = await parser.ParseAsync(stream, "sample.json");

		Assert.Equal("Sample Dat", result.Name);
		Assert.Single(result.Games);
		Assert.Equal("Game A", result.Games[0].Name);
		Assert.Single(result.Games[0].Roms);
		Assert.Equal("abcdef12", result.Games[0].Roms[0].Crc32);
	}
}
