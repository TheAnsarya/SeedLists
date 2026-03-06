using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.Options;
using SeedLists.Dat.Options;
using SeedLists.Dat.Providers;

namespace SeedLists.Dat.Tests;

public sealed class GoodToolsProviderTests {
	[Fact]
	public async Task ListAvailableAsync_ClassifiesFileTypesAndSystemFromFolder() {
		var root = CreateTempDirectory();
		try {
			var snesDir = Directory.CreateDirectory(Path.Combine(root, "SNES")).FullName;
			var nesDir = Directory.CreateDirectory(Path.Combine(root, "NES")).FullName;
			var gbDir = Directory.CreateDirectory(Path.Combine(root, "GB")).FullName;

			await File.WriteAllTextAsync(Path.Combine(snesDir, "snes-list.dat"), "sample", Encoding.UTF8);
			await File.WriteAllBytesAsync(Path.Combine(nesDir, "nes-list.zip"), BuildZipArchive("nes-list.dat", "sample"));
			await File.WriteAllBytesAsync(Path.Combine(gbDir, "gb-list.7z"), [0x37, 0x7a, 0xbc]);

			var provider = CreateProvider(root);
			var results = await provider.ListAvailableAsync();

			Assert.Equal(3, results.Count);
			Assert.Contains(results, item => item.System == "SNES");
			Assert.Contains(results, item => item.System == "NES" && item.Description!.Contains("archive (zip)", StringComparison.OrdinalIgnoreCase));
			Assert.Contains(results, item => item.System == "GB" && item.Description!.Contains("archive (7z)", StringComparison.OrdinalIgnoreCase));
		} finally {
			DeleteTempDirectory(root);
		}
	}

	[Fact]
	public async Task DownloadDatAsync_ExtractsPayloadFromZip() {
		var root = CreateTempDirectory();
		try {
			var zipPath = Path.Combine(root, "goodtools.zip");
			await File.WriteAllBytesAsync(zipPath, BuildZipArchive("good.dat", "GoodTools DAT content"));

			var provider = CreateProvider(root);
			await using var stream = await provider.DownloadDatAsync($"local::{zipPath}");
			using var reader = new StreamReader(stream, Encoding.UTF8);
			var payload = await reader.ReadToEndAsync();

			Assert.Equal("GoodTools DAT content", payload);
		} finally {
			DeleteTempDirectory(root);
		}
	}

	[Fact]
	public async Task DownloadDatAsync_ThrowsForSevenZipInputs() {
		var root = CreateTempDirectory();
		try {
			var sevenZipPath = Path.Combine(root, "goodtools.7z");
			await File.WriteAllBytesAsync(sevenZipPath, [0x37, 0x7a, 0xbc]);

			var provider = CreateProvider(root);
			var error = await Assert.ThrowsAsync<NotSupportedException>(async () => await provider.DownloadDatAsync($"local::{sevenZipPath}"));

			Assert.Contains(".7z", error.Message, StringComparison.Ordinal);
		} finally {
			DeleteTempDirectory(root);
		}
	}

	private static GoodToolsProvider CreateProvider(string root) {
		var options = Microsoft.Extensions.Options.Options.Create(new SeedListsDatOptions {
			GoodToolsLocalDirectory = root,
		});

		return new GoodToolsProvider(options);
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
