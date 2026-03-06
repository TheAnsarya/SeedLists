using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.Options;
using SeedLists.Dat.Options;
using SeedLists.Dat.Providers;

namespace SeedLists.Dat.Tests;

public sealed class TosecProviderTests {
	[Fact]
	public async Task ListAvailableAsync_IncludesLocalAndDeduplicatedRemoteEntries() {
		var localRoot = CreateTempDirectory();
		try {
			var localDat = Path.Combine(localRoot, "Atari - Sample.dat");
			var localZip = Path.Combine(localRoot, "Atari - Archive.zip");
			await File.WriteAllTextAsync(localDat, "<datafile/>", Encoding.UTF8);
			await File.WriteAllBytesAsync(localZip, BuildZipArchive("local.dat", "<datafile/>"));

			var html = """
				<html>
					<a href="/dats/tosec-collection.zip">zip 1</a>
					<a href="https://example.invalid/dats/tosec-collection.zip">zip 1 dup</a>
					<a href="/dats/tosec-update.7z">7z</a>
				</html>
				""";

			var options = Microsoft.Extensions.Options.Options.Create(new SeedListsDatOptions {
				TosecLocalDirectory = localRoot,
				EnableInternetDownloads = true,
				TosecDatFilesUrl = "https://example.invalid/index",
				TosecBaseUrl = "https://example.invalid",
			});

			var provider = new TosecProvider(options, new FakeHttpClientFactory(_ => new HttpResponseMessage(System.Net.HttpStatusCode.OK) {
				Content = new StringContent(html, Encoding.UTF8, "text/html"),
			}));

			var results = await provider.ListAvailableAsync();

			Assert.Equal(4, results.Count);
			Assert.Equal(2, results.Count(item => item.Identifier.StartsWith("url::", StringComparison.OrdinalIgnoreCase)));
			Assert.Contains(results, item => item.Identifier.StartsWith("local::", StringComparison.OrdinalIgnoreCase) && item.Description!.Contains("archive (zip)", StringComparison.OrdinalIgnoreCase));
		} finally {
			DeleteTempDirectory(localRoot);
		}
	}

	[Fact]
	public async Task ListAvailableAsync_ReturnsLocalEntriesWhenRemoteIndexFails() {
		var localRoot = CreateTempDirectory();
		try {
			var localDat = Path.Combine(localRoot, "Sega - Sample.dat");
			await File.WriteAllTextAsync(localDat, "<datafile/>", Encoding.UTF8);

			var options = Microsoft.Extensions.Options.Options.Create(new SeedListsDatOptions {
				TosecLocalDirectory = localRoot,
				EnableInternetDownloads = true,
				TosecDatFilesUrl = "https://example.invalid/index",
				TosecBaseUrl = "https://example.invalid",
			});

			var provider = new TosecProvider(options, new FakeHttpClientFactory(_ => throw new HttpRequestException("network down")));
			var results = await provider.ListAvailableAsync();

			Assert.Single(results);
			Assert.StartsWith("local::", results[0].Identifier, StringComparison.OrdinalIgnoreCase);
		} finally {
			DeleteTempDirectory(localRoot);
		}
	}

	[Fact]
	public async Task DownloadDatAsync_ExtractsPayloadFromRemoteZip() {
		var zipBytes = BuildZipArchive("remote.dat", "<datafile><game name=\"Sample\"/></datafile>");
		var options = Microsoft.Extensions.Options.Options.Create(new SeedListsDatOptions {
			EnableInternetDownloads = true,
		});

		var provider = new TosecProvider(options, new FakeHttpClientFactory(_ => new HttpResponseMessage(System.Net.HttpStatusCode.OK) {
			Content = new ByteArrayContent(zipBytes),
		}));

		await using var stream = await provider.DownloadDatAsync("url::https://example.invalid/tosec.zip");
		using var reader = new StreamReader(stream, Encoding.UTF8);
		var payload = await reader.ReadToEndAsync();

		Assert.Contains("<datafile>", payload, StringComparison.Ordinal);
	}

	[Fact]
	public async Task DownloadDatAsync_RetriesTransientFailures() {
		var attempts = 0;
		var options = Microsoft.Extensions.Options.Options.Create(new SeedListsDatOptions {
			EnableInternetDownloads = true,
		});

		var provider = new TosecProvider(options, new FakeHttpClientFactory(_ => {
			attempts++;
			if (attempts < 3) {
				return new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable) {
					Content = new StringContent("temporary"),
				};
			}

			return new HttpResponseMessage(System.Net.HttpStatusCode.OK) {
				Content = new ByteArrayContent(Encoding.UTF8.GetBytes("{\"games\":[]}")),
			};
		}));

		await using var stream = await provider.DownloadDatAsync("url::https://example.invalid/tosec.json");
		using var reader = new StreamReader(stream, Encoding.UTF8);
		var payload = await reader.ReadToEndAsync();

		Assert.Equal(3, attempts);
		Assert.Contains("games", payload, StringComparison.Ordinal);
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
