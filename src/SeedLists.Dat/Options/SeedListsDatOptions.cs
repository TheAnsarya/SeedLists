namespace SeedLists.Dat.Options;

/// <summary>
/// Runtime options for providers and output paths.
/// </summary>
public sealed class SeedListsDatOptions {
	public string OutputDirectory { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SeedLists", "dats");
	public string IngestionDatabasePath { get; set; } = "ingestion\\ingestion-ledger.sqlite";
	public string TosecLocalDirectory { get; set; } = @"D:\Roms\TOSEC";
	public string GoodToolsLocalDirectory { get; set; } = @"C:\~reference-roms\roms";
	public string NoIntroLocalDirectory { get; set; } = @"C:\~reference-roms\dats\nointro";
	public string MameLocalDirectory { get; set; } = @"C:\~reference-roms\dats\mame";
	public string MessLocalDirectory { get; set; } = @"C:\~reference-roms\dats\mess";
	public string RedumpLocalDirectory { get; set; } = @"C:\~reference-roms\dats\redump";
	public string PleasureDomeLocalDirectory { get; set; } = @"C:\~reference-roms\dats\pleasuredome";
	public string FruitMachineLocalDirectory { get; set; } = @"C:\~reference-roms\dats\fruit-machines";
	public string StateDirectory { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SeedLists", "state");
	public int MaxDatsPerRun { get; set; }
	public string[] IncludeNamePatterns { get; set; } = [];
	public string[] ExcludeNamePatterns { get; set; } = [];
	public bool EnableInternetDownloads { get; set; }
	public bool EnableRemoteVersionChecks { get; set; } = true;
	public int RemotePollIntervalHours { get; set; } = 24;
	public bool AllowNoIntroDownloadDuringTesting { get; set; }
	public string TosecDatFilesUrl { get; set; } = "https://www.tosecdev.org/downloads/category/22-datfiles";
	public string TosecBaseUrl { get; set; } = "https://www.tosecdev.org";
	public string NoIntroDownloadPageUrl { get; set; } = "https://datomatic.no-intro.org/index.php?page=download&s=64";
	public string NoIntroBaseUrl { get; set; } = "https://datomatic.no-intro.org";
	public string MameRemoteIndexUrl { get; set; } = "https://www.progettosnaps.net/dats/MAME/";
	public string MessRemoteIndexUrl { get; set; } = "https://www.progettosnaps.net/dats/";
	public string RedumpRemoteIndexUrl { get; set; } = "https://www.redump.org/downloads/";
	public string PleasureDomeMameIndexUrl { get; set; } = "https://pleasuredome.github.io/pleasuredome/mame/index.html";
	public string PleasureDomeNonMameIndexUrl { get; set; } = "https://pleasuredome.github.io/pleasuredome/nonmame/index.html";
	public string[] PleasureDomeNonMameCategorySlugs { get; set; } = ["fruitmachines", "pinball", "raine"];
	public string[] GoodToolsRemoteDatUrls { get; set; } = ["https://archive.org/download/GoodTools.Collection.2025.04.10.RomVault/%21Support%20files/Goodinfo.cfg%20%28all%20dumps%20enabled%29.zip"];
	public string[] RedumpRemoteDatUrls { get; set; } = [];
	public string[] FruitMachineRemoteDatUrls { get; set; } = [];
}
