using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;
using SeedLists.Dat.Abstractions;
using SeedLists.Dat.Models;
using SeedLists.Dat.Options;
using SeedLists.Dat.Parsing;
using SeedLists.Dat.Providers;
using SeedLists.Dat.Services;
using System.Text;
using System.Text.Json;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class JsonDatParserBenchmark {
	private readonly StreamingJsonDatParser _parser = new();
	private readonly byte[] _smallDatBytes = BuildDat(gameCount: 1, romsPerGame: 1);
	private readonly byte[] _mediumDatBytes = BuildDat(gameCount: 80, romsPerGame: 4);
	private readonly byte[] _largeDatBytes = BuildDat(gameCount: 400, romsPerGame: 6);

	[Benchmark(Baseline = true)]
	public async Task ParseSmallDatAsync() {
		await ParsePayloadAsync(_smallDatBytes, "benchmark-small.json");
	}

	[Benchmark]
	public async Task ParseMediumDatAsync() {
		await ParsePayloadAsync(_mediumDatBytes, "benchmark-medium.json");
	}

	[Benchmark]
	public async Task ParseLargeDatAsync() {
		await ParsePayloadAsync(_largeDatBytes, "benchmark-large.json");
	}

	private async Task ParsePayloadAsync(byte[] payload, string fileName) {
		await using var stream = new MemoryStream(payload, writable: false);
		_ = await _parser.ParseAsync(stream, fileName);
	}

	private static byte[] BuildDat(int gameCount, int romsPerGame) {
		using var stream = new MemoryStream();
		using var writer = new Utf8JsonWriter(stream);

		writer.WriteStartObject();
		writer.WriteString("name", $"Benchmark DAT {gameCount}x{romsPerGame}");
		writer.WriteString("description", "SeedLists parser benchmark payload");
		writer.WriteString("version", "2026-03-06");
		writer.WriteString("provider", "Tosec");
		writer.WriteStartArray("games");

		for (var game = 1; game <= gameCount; game++) {
			writer.WriteStartObject();
			writer.WriteString("name", $"Game {game:0000}");
			writer.WriteString("description", $"Benchmark Game {game:0000}");
			writer.WriteString("publisher", "SeedLists");
			writer.WriteString("year", (1980 + (game % 30)).ToString());
			writer.WriteStartArray("roms");

			for (var rom = 1; rom <= romsPerGame; rom++) {
				writer.WriteStartObject();
				writer.WriteString("name", $"game-{game:0000}-rom-{rom:00}.bin");
				writer.WriteNumber("size", 16384 + (rom * 128));
				writer.WriteString("crc32", $"{game:x4}{rom:x4}");
				writer.WriteString("md5", $"{game:x8}{rom:x8}{game:x8}{rom:x8}");
				writer.WriteString("sha1", $"{game:x8}{rom:x8}{game:x8}{rom:x8}{game:x8}");
				writer.WriteEndObject();
			}

			writer.WriteEndArray();
			writer.WriteEndObject();
		}

		writer.WriteEndArray();
		writer.WriteEndObject();
		writer.Flush();

		return stream.ToArray();
	}
}

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class CatalogNormalizationBenchmark {
	private readonly CatalogNormalizationService _normalizer = new();
	private readonly byte[] _jsonPayload = BuildJsonPayload();
	private readonly byte[] _jsonPayloadPadded;
	private readonly int _jsonPayloadOffset = 11;
	private readonly byte[] _tosecPayload = BuildTosecPayload();
	private readonly byte[] _noIntroPayload = BuildNoIntroPayload();
	private readonly byte[] _goodToolsPayload = BuildGoodToolsPayload();

	public CatalogNormalizationBenchmark() {
		_jsonPayloadPadded = new byte[_jsonPayload.Length + 32];
		Array.Fill(_jsonPayloadPadded, (byte)'_');
		_jsonPayload.CopyTo(_jsonPayloadPadded.AsSpan(_jsonPayloadOffset));
	}

	[Benchmark(Baseline = true)]
	public byte[] NormalizeJsonPassthrough() {
		return _normalizer.Normalize(_jsonPayload, DatProviderKind.Tosec, "benchmark-json.dat");
	}

	[Benchmark]
	public byte[] NormalizeJsonPassthroughSlicedSpan() {
		return _normalizer.Normalize(
			_jsonPayloadPadded.AsSpan(_jsonPayloadOffset, _jsonPayload.Length),
			DatProviderKind.Tosec,
			"benchmark-json-sliced.dat");
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

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class PleasureDomeDiscoveryBenchmark {
	private PleasureDomeProvider _providerWithoutVersionChecks = null!;
	private PleasureDomeProvider _providerWithVersionChecks = null!;

	[GlobalSetup]
	public void Setup() {
		var mameLinks = BuildLinks("https://github.com/pleasuredome/pleasuredome/raw/gh-pages/mame/MAME%200.286%20ROMs%20(merged)-", 120);
		var fruitLinks = BuildLinks("https://github.com/pleasuredome/pleasuredome/raw/gh-pages/nonmame/fruitmachines/FruitMachines-", 60);
		var pinballLinks = BuildLinks("https://github.com/pleasuredome/pleasuredome/raw/gh-pages/nonmame/pinball/Visual%20Pinball%20", 60);
		var raineLinks = BuildLinks("https://github.com/pleasuredome/pleasuredome/raw/gh-pages/nonmame/raine/Raine%200.97.5%20ROMs%20(split)-", 60);

		var mamePage = BuildDatPage(mameLinks);
		var nonMameIndex = """
			<html>
				<a href="https://pleasuredome.github.io/pleasuredome/nonmame/fruitmachines/index.html">Fruit</a>
				<a href="https://pleasuredome.github.io/pleasuredome/nonmame/pinball/index.html">Pinball</a>
				<a href="https://pleasuredome.github.io/pleasuredome/nonmame/raine/index.html">Raine</a>
			</html>
			""";

		var fruitPage = BuildDatPage(fruitLinks);
		var pinballPage = BuildDatPage(pinballLinks);
		var rainePage = BuildDatPage(raineLinks);

		var baseOptions = new SeedListsDatOptions {
			EnableInternetDownloads = true,
			PleasureDomeMameIndexUrl = "https://pleasuredome.github.io/pleasuredome/mame/index.html",
			PleasureDomeNonMameIndexUrl = "https://pleasuredome.github.io/pleasuredome/nonmame/index.html",
			PleasureDomeNonMameCategorySlugs = ["fruitmachines", "pinball", "raine"],
			RemotePollIntervalHours = 0,
		};

		var responses = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
			["https://pleasuredome.github.io/pleasuredome/mame/index.html"] = mamePage,
			["https://pleasuredome.github.io/pleasuredome/nonmame/index.html"] = nonMameIndex,
			["https://pleasuredome.github.io/pleasuredome/nonmame/fruitmachines/index.html"] = fruitPage,
			["https://pleasuredome.github.io/pleasuredome/nonmame/pinball/index.html"] = pinballPage,
			["https://pleasuredome.github.io/pleasuredome/nonmame/raine/index.html"] = rainePage,
		};

		_providerWithoutVersionChecks = new PleasureDomeProvider(
			Microsoft.Extensions.Options.Options.Create(new SeedListsDatOptions {
				EnableInternetDownloads = baseOptions.EnableInternetDownloads,
				EnableRemoteVersionChecks = false,
				RemotePollIntervalHours = baseOptions.RemotePollIntervalHours,
				PleasureDomeMameIndexUrl = baseOptions.PleasureDomeMameIndexUrl,
				PleasureDomeNonMameIndexUrl = baseOptions.PleasureDomeNonMameIndexUrl,
				PleasureDomeNonMameCategorySlugs = baseOptions.PleasureDomeNonMameCategorySlugs,
			}),
			new InMemoryStateStore(),
			new FakeHttpClientFactory(request => HtmlResponse(responses[request.RequestUri!.ToString()])));

		_providerWithVersionChecks = new PleasureDomeProvider(
			Microsoft.Extensions.Options.Options.Create(new SeedListsDatOptions {
				EnableInternetDownloads = baseOptions.EnableInternetDownloads,
				EnableRemoteVersionChecks = true,
				RemotePollIntervalHours = baseOptions.RemotePollIntervalHours,
				PleasureDomeMameIndexUrl = baseOptions.PleasureDomeMameIndexUrl,
				PleasureDomeNonMameIndexUrl = baseOptions.PleasureDomeNonMameIndexUrl,
				PleasureDomeNonMameCategorySlugs = baseOptions.PleasureDomeNonMameCategorySlugs,
			}),
			new InMemoryStateStore(),
			new FakeHttpClientFactory(request => HtmlResponse(responses[request.RequestUri!.ToString()])));
	}

	[Benchmark(Baseline = true)]
	public async Task<int> DiscoverPleasureDomeDatLinks() {
		var results = await _providerWithoutVersionChecks.ListAvailableAsync();
		return results.Count;
	}

	[Benchmark]
	public async Task<int> DiscoverPleasureDomeDatLinksWithVersionChecks() {
		var results = await _providerWithVersionChecks.ListAvailableAsync();
		return results.Count;
	}

	private static string BuildDatPage(IReadOnlyList<string> links) {
		var sb = new StringBuilder();
		sb.Append("<html>");
		foreach (var link in links) {
			sb.Append("Datfile: <a href=\"");
			sb.Append(link);
			sb.Append("\">entry</a>");
		}

		sb.Append("</html>");
		return sb.ToString();
	}

	private static IReadOnlyList<string> BuildLinks(string prefix, int count) {
		var output = new List<string>(count);
		for (var i = 0; i < count; i++) {
			output.Add($"{prefix}{i:000}.zip");
		}

		return output;
	}

	private static HttpResponseMessage HtmlResponse(string html) {
		return new HttpResponseMessage(System.Net.HttpStatusCode.OK) {
			Content = new StringContent(html, Encoding.UTF8, "text/html"),
		};
	}

	private sealed class InMemoryStateStore : IDatSyncStateStore {
		private readonly Dictionary<string, string> _values = [];

		public Task<DateTimeOffset?> GetDateTimeAsync(string key, CancellationToken cancellationToken = default) {
			_ = cancellationToken;
			if (!_values.TryGetValue(key, out var raw) || !DateTimeOffset.TryParse(raw, out var parsed)) {
				return Task.FromResult<DateTimeOffset?>(null);
			}

			return Task.FromResult<DateTimeOffset?>(parsed);
		}

		public Task SetDateTimeAsync(string key, DateTimeOffset value, CancellationToken cancellationToken = default) {
			_ = cancellationToken;
			_values[key] = value.UtcDateTime.ToString("O");
			return Task.CompletedTask;
		}

		public Task<string?> GetStringAsync(string key, CancellationToken cancellationToken = default) {
			_ = cancellationToken;
			return Task.FromResult(_values.TryGetValue(key, out var value) ? value : null);
		}

		public Task SetStringAsync(string key, string value, CancellationToken cancellationToken = default) {
			_ = cancellationToken;
			_values[key] = value;
			return Task.CompletedTask;
		}
	}

	private sealed class FakeHttpClientFactory(Func<HttpRequestMessage, HttpResponseMessage> responder) : IHttpClientFactory {
		private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder = responder;

		public HttpClient CreateClient(string name) {
			_ = name;
			return new HttpClient(new FakeHttpMessageHandler(_responder), disposeHandler: true);
		}
	}

	private sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler {
		private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder = responder;

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
			_ = cancellationToken;
			return Task.FromResult(_responder(request));
		}
	}
}
