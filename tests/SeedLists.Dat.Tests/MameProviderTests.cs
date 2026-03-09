using System.IO.Compression;
using System.Text;
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

		return new MameProvider(options);
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
