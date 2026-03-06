using Microsoft.Extensions.Options;
using SeedLists.Dat.Options;
using SeedLists.Dat.Providers;
using SeedLists.Dat.Services;

namespace SeedLists.Dat.Tests;

public sealed class NoIntroProviderCooldownIntegrationTests {
	[Fact]
	public async Task DownloadDatAsync_PersistsCooldownStateAcrossProviderInstances() {
		var stateDirectory = CreateTempDirectory();
		try {
			var options = Microsoft.Extensions.Options.Options.Create(new SeedListsDatOptions {
				EnableInternetDownloads = true,
				AllowNoIntroDownloadDuringTesting = false,
				StateDirectory = stateDirectory,
			});

			var firstProvider = CreateProvider(options, new FileDatSyncStateStore(options));
			await using var firstStream = await firstProvider.DownloadDatAsync("system::64");
			Assert.True(firstStream.Length > 0);

			var secondProvider = CreateProvider(options, new FileDatSyncStateStore(options));
			var error = await Assert.ThrowsAsync<InvalidOperationException>(async () => await secondProvider.DownloadDatAsync("system::64"));
			Assert.Contains("No-Intro cooldown active", error.Message);

			var statePath = Path.Combine(stateDirectory, "provider-sync-state.json");
			Assert.True(File.Exists(statePath));
			var stateText = await File.ReadAllTextAsync(statePath);
			Assert.Contains("no-intro:last-download-utc", stateText, StringComparison.Ordinal);
		} finally {
			DeleteTempDirectory(stateDirectory);
		}
	}

	[Fact]
	public async Task DownloadDatAsync_AllowsWhenTestingOverrideEnabledWithPersistedState() {
		var stateDirectory = CreateTempDirectory();
		try {
			var options = Microsoft.Extensions.Options.Options.Create(new SeedListsDatOptions {
				EnableInternetDownloads = true,
				AllowNoIntroDownloadDuringTesting = true,
				StateDirectory = stateDirectory,
			});

			var stateStore = new FileDatSyncStateStore(options);
			await stateStore.SetDateTimeAsync("no-intro:last-download-utc", DateTimeOffset.UtcNow);

			var provider = CreateProvider(options, new FileDatSyncStateStore(options));
			await using var stream = await provider.DownloadDatAsync("system::64");
			Assert.True(stream.Length > 0);
		} finally {
			DeleteTempDirectory(stateDirectory);
		}
	}

	private static NoIntroProvider CreateProvider(IOptions<SeedListsDatOptions> options, FileDatSyncStateStore stateStore) {
		return new NoIntroProvider(options, stateStore, new FakeHttpClientFactory(_ => new HttpResponseMessage(System.Net.HttpStatusCode.OK) {
			Content = new ByteArrayContent("<datafile/>"u8.ToArray())
		}));
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
			// Cleanup best effort for Windows file lock races in test environments.
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
