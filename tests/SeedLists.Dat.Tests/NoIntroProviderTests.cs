using Microsoft.Extensions.Options;
using SeedLists.Dat.Abstractions;
using SeedLists.Dat.Options;
using SeedLists.Dat.Providers;

namespace SeedLists.Dat.Tests;

public sealed class NoIntroProviderTests {
	[Fact]
	public async Task DownloadDatAsync_BlocksWhenCooldownNotElapsed() {
		var options = Microsoft.Extensions.Options.Options.Create(new SeedListsDatOptions {
			EnableInternetDownloads = true,
			AllowNoIntroDownloadDuringTesting = false,
		});

		var state = new InMemoryStateStore();
		await state.SetDateTimeAsync("no-intro:last-download-utc", DateTimeOffset.UtcNow);

		var provider = new NoIntroProvider(options, state, new FakeHttpClientFactory(_ => new HttpResponseMessage(System.Net.HttpStatusCode.OK) {
			Content = new ByteArrayContent("<datafile/>"u8.ToArray())
		}));

		await Assert.ThrowsAsync<InvalidOperationException>(async () => await provider.DownloadDatAsync("system::64"));
	}

	[Fact]
	public async Task DownloadDatAsync_AllowsWhenTestingOverrideEnabled() {
		var options = Microsoft.Extensions.Options.Options.Create(new SeedListsDatOptions {
			EnableInternetDownloads = true,
			AllowNoIntroDownloadDuringTesting = true,
		});

		var state = new InMemoryStateStore();
		var provider = new NoIntroProvider(options, state, new FakeHttpClientFactory(_ => new HttpResponseMessage(System.Net.HttpStatusCode.OK) {
			Content = new ByteArrayContent("<datafile/>"u8.ToArray())
		}));

		await using var stream = await provider.DownloadDatAsync("system::64");
		Assert.True(stream.Length > 0);
	}

	private sealed class InMemoryStateStore : IDatSyncStateStore {
		private readonly Dictionary<string, DateTimeOffset> _values = [];

		public Task<DateTimeOffset?> GetDateTimeAsync(string key, CancellationToken cancellationToken = default) {
			_ = cancellationToken;
			return Task.FromResult(_values.TryGetValue(key, out var value) ? value : (DateTimeOffset?)null);
		}

		public Task SetDateTimeAsync(string key, DateTimeOffset value, CancellationToken cancellationToken = default) {
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
