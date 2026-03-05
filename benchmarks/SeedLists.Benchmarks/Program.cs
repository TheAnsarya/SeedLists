using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using SeedLists.Dat.Parsing;

BenchmarkRunner.Run<JsonDatParserBenchmark>();

[MemoryDiagnoser]
public class JsonDatParserBenchmark {
	private readonly StreamingJsonDatParser _parser = new();
	private readonly byte[] _datBytes = BuildDat();

	[Benchmark]
	public async Task ParseSmallDatAsync() {
		await using var stream = new MemoryStream(_datBytes, writable: false);
		_ = await _parser.ParseAsync(stream, "benchmark.json");
	}

	private static byte[] BuildDat() {
		var json = """
			{
				"name": "Benchmark DAT",
				"description": "SeedLists benchmark payload",
				"version": "2026-03-05",
				"provider": "Tosec",
				"games": [
					{
						"name": "Example Game",
						"description": "Example Game",
						"roms": [
							{
								"name": "example.bin",
								"size": 1234,
								"crc32": "ABCDEF12",
								"md5": "AABB",
								"sha1": "CCDDEE"
							}
						]
					}
				]
			}
			""";

		return System.Text.Encoding.UTF8.GetBytes(json);
	}
}
