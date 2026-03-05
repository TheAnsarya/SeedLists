namespace SeedLists.Dat.Models;

/// <summary>
/// High-level sync stage.
/// </summary>
public enum DatSyncPhase {
	Discovering = 0,
	Downloading = 1,
	Parsing = 2,
	Saving = 3,
	Completed = 4,
}
