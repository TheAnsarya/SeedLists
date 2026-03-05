namespace SeedLists.Dat.Options;

/// <summary>
/// Runtime options for providers and output paths.
/// </summary>
public sealed class SeedListsDatOptions {
	public string OutputDirectory { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SeedLists", "dats");
	public string TosecLocalDirectory { get; set; } = @"D:\Roms\TOSEC";
	public string GoodToolsLocalDirectory { get; set; } = @"C:\~reference-roms\roms";
	public string NoIntroLocalDirectory { get; set; } = @"C:\~reference-roms\dats\nointro";
	public string StateDirectory { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SeedLists", "state");
	public bool EnableInternetDownloads { get; set; }
	public bool AllowNoIntroDownloadDuringTesting { get; set; }
	public string TosecDatFilesUrl { get; set; } = "https://www.tosecdev.org/downloads/category/22-datfiles";
	public string TosecBaseUrl { get; set; } = "https://www.tosecdev.org";
	public string NoIntroDownloadPageUrl { get; set; } = "https://datomatic.no-intro.org/index.php?page=download&s=64";
	public string NoIntroBaseUrl { get; set; } = "https://datomatic.no-intro.org";
}
