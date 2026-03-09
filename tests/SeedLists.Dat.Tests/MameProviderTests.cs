using System.IO.Compression;
using System.Text;
using SeedLists.Dat.Abstractions;
using SeedLists.Dat.Options;
using SeedLists.Dat.Providers;

namespace SeedLists.Dat.Tests;

public sealed class MameProviderTests {
	[Fact]
	public async Task ListAvailableAsync_ClassifiesFileTypesAndSystemFromFolder() {
		var root = CreateTempDirectory();
		try {
			var arcadeDir = Directory.CreateDirectory(Path.Combine(root, "arcade")).FullName;
			var fruitDir = Directory.CreateDirectory(Path.Combine(root, "fruit-machines")).FullName;

			await File.WriteAllTextAsync(Path.Combine(arcadeDir, "mame-main.dat"), "sample", Encoding.UTF8);
			await File.WriteAllBytesAsync(Path.Combine(fruitDir, "fruit-pack.zip"), BuildZipArchive("fruit.dat", "sample"));

			var provider = CreateProvider(root);
			var results = await provider.ListAvailableAsync();

			Assert.Equal(2, results.Count);
			Assert.Contains(results, item => item.System == "arcade");
			Assert.Contains(results, item => item.System == "fruit-machines" && item.Description!.Contains("archive (zip)", StringComparison.OrdinalIgnoreCase));
		} finally {
			DeleteTempDirectory(root);
		}
	}

	[Fact]
	public async Task DownloadDatAsync_ExtractsPayloadFromZip() {
		var root = CreateTempDirectory();
		try {
			var zipPath = Path.Combine(root, "mame.zip");
			await File.WriteAllBytesAsync(zipPath, BuildZipArchive("mame.dat", "MAME DAT content"));

			var provider = CreateProvider(root);
			await using var stream = await provider.DownloadDatAsync($"local::{zipPath}");
			using var reader = new StreamReader(stream, Encoding.UTF8);
			var payload = await reader.ReadToEndAsync();

			Assert.Equal("MAME DAT content", payload);
		} finally {
			DeleteTempDirectory(root);
		}
	}

	private static MameProvider CreateProvider(string root) {
		var options = Microsoft.Extensions.Options.Options.Create(new SeedListsDatOptions {
			MameLocalDirectory = root,
		});

		return new MameProvider(
			options,
			new InMemoryStateStore(),
			new FakeHttpClientFactory(_ => new HttpResponseMessage(System.Net.HttpStatusCode.OK) {
				Content = new StringContent("<html/>", Encoding.UTF8, "text/html"),
			}));
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
}
