using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;
using SeedLists.Dat.Models;
using SeedLists.Dat.Parsing;
using SeedLists.Dat.Services;
using System.Text;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
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

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class CatalogNormalizationBenchmark {
	private readonly CatalogNormalizationService _normalizer = new();
	private readonly byte[] _jsonPayload = BuildJsonPayload();
	private readonly byte[] _tosecPayload = BuildTosecPayload();
	private readonly byte[] _noIntroPayload = BuildNoIntroPayload();
	private readonly byte[] _goodToolsPayload = BuildGoodToolsPayload();

	[Benchmark(Baseline = true)]
	public byte[] NormalizeJsonPassthrough() {
		return _normalizer.Normalize(_jsonPayload, DatProviderKind.Tosec, "benchmark-json.dat");
	}

	[Benchmark]
	public byte[] NormalizeTosecXmlLike() {
		return _normalizer.Normalize(_tosecPayload, DatProviderKind.Tosec, "benchmark-tosec.dat");
	}

	[Benchmark]
	public byte[] NormalizeNoIntroXmlLike() {
		return _normalizer.Normalize(_noIntroPayload, DatProviderKind.NoIntro, "benchmark-nointro.dat");
	}

	[Benchmark]
	public byte[] NormalizeGoodToolsText() {
		return _normalizer.Normalize(_goodToolsPayload, DatProviderKind.GoodTools, "benchmark-goodtools.dat");
	}

	private static byte[] BuildJsonPayload() {
		var json = """
			{
				"name": "Benchmark JSON",
				"provider": "Tosec",
				"games": [
					{
						"name": "JSON Game",
						"roms": [
							{
								"name": "json-game.bin",
								"size": 4096,
								"crc32": "abc12345"
							}
						]
					}
				]
			}
			""";

		return Encoding.UTF8.GetBytes(json);
	}

	private static byte[] BuildTosecPayload() {
		var tosec = """
			<datafile>
				<game name="TOSEC Benchmark Game">
					<description>TOSEC Benchmark Game</description>
					<manufacturer>SeedLists</manufacturer>
					<year>1994</year>
					<rom name="tosec-game.bin" size="1048576" crc="0ca6e4a0" md5="0123456789abcdef0123456789abcdef" sha1="0123456789abcdef0123456789abcdef01234567" />
				</game>
			</datafile>
			""";

		return Encoding.UTF8.GetBytes(tosec);
	}

	private static byte[] BuildNoIntroPayload() {
		var noIntro = """
			<datafile>
				<machine name="No-Intro Benchmark Game">
					<description>No-Intro Benchmark Game</description>
					<manufacturer>SeedLists</manufacturer>
					<year>2002</year>
					<rom name="nointro-game.bin" size="524288" crc="f00dcafe" md5="fedcba9876543210fedcba9876543210" sha1="89abcdef0123456789abcdef0123456789abcdef" />
				</machine>
			</datafile>
			""";

		return Encoding.UTF8.GetBytes(noIntro);
	}

	private static byte[] BuildGoodToolsPayload() {
		var goodTools = """
			Super Mario World (U) [!].smc
			Chrono Trigger (U) [!].sfc
			EarthBound (U) [!].smc
			""";

		return Encoding.UTF8.GetBytes(goodTools);
	}
}
