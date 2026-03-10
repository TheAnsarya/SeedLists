using System.IO.Compression;
using System.Text;
using SeedLists.Dat.Abstractions;
using SeedLists.Dat.Options;
using SeedLists.Dat.Providers;

namespace SeedLists.Dat.Tests;

public sealed class PleasureDomeProviderTests {
	[Fact]
	public async Task ListAvailableAsync_DiscoversMameAndConfiguredNonMameCategories() {
		var stateStore = new InMemoryStateStore();
		var provider = CreateProvider(new SeedListsDatOptions {
			EnableInternetDownloads = true,
			EnableRemoteVersionChecks = false,
			PleasureDomeMameIndexUrl = "https://pleasuredome.github.io/pleasuredome/mame/index.html",
			PleasureDomeNonMameIndexUrl = "https://pleasuredome.github.io/pleasuredome/nonmame/index.html",
			PleasureDomeNonMameCategorySlugs = ["fruitmachines", "pinball", "raine"],
		}, stateStore, BuildPleasureDomeHttpClientFactory());

		var results = await provider.ListAvailableAsync();

		Assert.Equal(4, results.Count);
		Assert.All(results, item => Assert.StartsWith("remote|", item.Identifier, StringComparison.OrdinalIgnoreCase));
		Assert.Contains(results, item => item.System == "MAME" && item.Name.Contains("MAME 0.286 ROMs", StringComparison.Ordinal));
		Assert.Contains(results, item => item.System == "Fruit Machines");
		Assert.Contains(results, item => item.System == "Pinball");
		Assert.Contains(results, item => item.System == "Raine");
	}

	[Fact]
	public async Task ListAvailableAsync_SkipsUnchangedEntriesAfterDownloadSetsToken() {
		var stateStore = new InMemoryStateStore();
		var provider = CreateProvider(new SeedListsDatOptions {
			EnableInternetDownloads = true,
			EnableRemoteVersionChecks = true,
			RemotePollIntervalHours = 0,
			PleasureDomeNonMameCategorySlugs = ["fruitmachines"],
			PleasureDomeMameIndexUrl = "https://pleasuredome.github.io/pleasuredome/mame/index.html",
			PleasureDomeNonMameIndexUrl = "https://pleasuredome.github.io/pleasuredome/nonmame/index.html",
		}, stateStore, BuildPleasureDomeHttpClientFactory());

		var initial = await provider.ListAvailableAsync();
		var entry = Assert.Single(initial, item => item.System == "MAME");

		await using var payload = await provider.DownloadDatAsync(entry.Identifier);
		_ = payload.Length;

		var afterDownload = await provider.ListAvailableAsync();
		Assert.DoesNotContain(afterDownload, item => item.System == "MAME");
	}

	[Fact]
	public async Task DownloadDatAsync_RemoteZip_ExtractsDatPayload() {
		var stateStore = new InMemoryStateStore();
		var provider = CreateProvider(new SeedListsDatOptions {
			EnableInternetDownloads = true,
			EnableRemoteVersionChecks = false,
		}, stateStore, BuildPleasureDomeHttpClientFactory());

		var identifier = "remote|MAME%200.286%20ROMs%20%28merged%29.zip|https://github.com/pleasuredome/pleasuredome/raw/gh-pages/mame/MAME%200.286%20ROMs%20(merged).zip";
		await using var stream = await provider.DownloadDatAsync(identifier);
		using var reader = new StreamReader(stream, Encoding.UTF8);
		var payload = await reader.ReadToEndAsync();

		Assert.Contains("<datafile>", payload, StringComparison.Ordinal);
	}

	[Fact]
	public async Task ListAvailableAsync_ReturnsLocalEntriesWhenRemoteUnavailable() {
		var root = CreateTempDirectory();
		try {
			var localDat = Path.Combine(root, "local-pleasuredome.dat");
			await File.WriteAllTextAsync(localDat, "<datafile/>", Encoding.UTF8);

			var options = new SeedListsDatOptions {
				PleasureDomeLocalDirectory = root,
				EnableInternetDownloads = true,
				EnableRemoteVersionChecks = false,
			};

			var provider = CreateProvider(
				options,
				new InMemoryStateStore(),
				new FakeHttpClientFactory(_ => throw new HttpRequestException("network unavailable")));

			var results = await provider.ListAvailableAsync();

			Assert.Single(results);
			Assert.StartsWith("local::", results[0].Identifier, StringComparison.OrdinalIgnoreCase);
		} finally {
			DeleteTempDirectory(root);
		}
	}

	private static PleasureDomeProvider CreateProvider(
		SeedListsDatOptions options,
		IDatSyncStateStore stateStore,
		IHttpClientFactory httpClientFactory) {
		return new PleasureDomeProvider(
			Microsoft.Extensions.Options.Options.Create(options),
			stateStore,
			httpClientFactory);
	}

	private static FakeHttpClientFactory BuildPleasureDomeHttpClientFactory() {
		var mamePage = """
			<html>
				Datfile: <a href="https://github.com/pleasuredome/pleasuredome/raw/gh-pages/mame/MAME%200.286%20ROMs%20(merged).zip">MAME 0.286 ROMs (merged)</a>
				Datfile: <a href="https://github.com/pleasuredome/pleasuredome/raw/gh-pages/mame/MAME%200.286%20ROMs%20(merged).zip">duplicate</a>
			</html>
			""";

		var nonMamePage = """
			<html>
				<a href="https://pleasuredome.github.io/pleasuredome/nonmame/fruitmachines/index.html">Fruit</a>
				<a href="https://pleasuredome.github.io/pleasuredome/nonmame/pinball/index.html">Pinball</a>
				<a href="https://pleasuredome.github.io/pleasuredome/nonmame/raine/index.html">Raine</a>
				<a href="https://pleasuredome.github.io/pleasuredome/nonmame/demul/index.html">Demul</a>
			</html>
			""";

		var fruitPage = """
			<html>
				Datfiles: <a href="https://github.com/pleasuredome/pleasuredome/raw/gh-pages/nonmame/fruitmachines/FruitMachines-20251022.zip">Fruit Machines</a>
			</html>
			""";

		var pinballPage = """
			<html>
				Datfiles: <a href="https://github.com/pleasuredome/pleasuredome/raw/gh-pages/nonmame/pinball/Visual%20Pinball%20(2026-01-02).zip">Visual Pinball</a>
			</html>
			""";

		var rainePage = """
			<html>
				Datfile: <a href="https://github.com/pleasuredome/pleasuredome/raw/gh-pages/nonmame/raine/Raine%200.97.5%20ROMs%20(split).zip">Raine</a>
			</html>
			""";

		var datPayload = BuildZipArchive("pleasuredome.dat", "<datafile><game name=\"sample\"><rom name=\"sample.bin\" size=\"1\" crc=\"abcdef12\"/></game></datafile>");

		return new FakeHttpClientFactory(request => {
			var url = request.RequestUri?.ToString() ?? string.Empty;
			if (string.Equals(url, "https://pleasuredome.github.io/pleasuredome/mame/index.html", StringComparison.OrdinalIgnoreCase)) {
				return HtmlResponse(mamePage);
			}

			if (string.Equals(url, "https://pleasuredome.github.io/pleasuredome/nonmame/index.html", StringComparison.OrdinalIgnoreCase)) {
				return HtmlResponse(nonMamePage);
			}

			if (string.Equals(url, "https://pleasuredome.github.io/pleasuredome/nonmame/fruitmachines/index.html", StringComparison.OrdinalIgnoreCase)) {
				return HtmlResponse(fruitPage);
			}

			if (string.Equals(url, "https://pleasuredome.github.io/pleasuredome/nonmame/pinball/index.html", StringComparison.OrdinalIgnoreCase)) {
				return HtmlResponse(pinballPage);
			}

			if (string.Equals(url, "https://pleasuredome.github.io/pleasuredome/nonmame/raine/index.html", StringComparison.OrdinalIgnoreCase)) {
				return HtmlResponse(rainePage);
			}

			if (url.Contains("/raw/gh-pages/", StringComparison.OrdinalIgnoreCase) && url.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) {
				return ZipResponse(datPayload);
			}

			return new HttpResponseMessage(System.Net.HttpStatusCode.NotFound) {
				Content = new StringContent(url, Encoding.UTF8, "text/plain"),
			};
		});
	}

	private static HttpResponseMessage HtmlResponse(string html) {
		return new HttpResponseMessage(System.Net.HttpStatusCode.OK) {
			Content = new StringContent(html, Encoding.UTF8, "text/html"),
		};
	}

	private static HttpResponseMessage ZipResponse(byte[] bytes) {
		return new HttpResponseMessage(System.Net.HttpStatusCode.OK) {
			Content = new ByteArrayContent(bytes),
		};
	}

	private static byte[] BuildZipArchive(string entryName, string entryContent) {
		using var stream = new MemoryStream();
		using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true)) {
			var entry = archive.CreateEntry(entryName);
			using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
			writer.Write(entryContent);
		}

		return stream.ToArray();
	}

	private static string CreateTempDirectory() {
		var path = Path.Combine(Path.GetTempPath(), "SeedLists.Tests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(path);
		return path;
	}

	private static void DeleteTempDirectory(string path) {
		if (!Directory.Exists(path)) {
			return;
		}

		try {
			Directory.Delete(path, recursive: true);
		} catch {
			// Best effort temp cleanup.
		}
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
