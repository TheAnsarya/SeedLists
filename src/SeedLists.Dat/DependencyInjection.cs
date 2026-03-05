using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SeedLists.Dat.Abstractions;
using SeedLists.Dat.Options;
using SeedLists.Dat.Parsing;
using SeedLists.Dat.Providers;
using SeedLists.Dat.Services;

namespace SeedLists.Dat;

/// <summary>
/// Service registration helpers for SeedLists DAT components.
/// </summary>
public static class DependencyInjection {
	public static IServiceCollection AddSeedListsDat(this IServiceCollection services, IConfiguration configuration) {
		services.Configure<SeedListsDatOptions>(configuration.GetSection("SeedListsDat"));
		services.AddHttpClient();

		services.AddSingleton<IDatSyncStateStore, FileDatSyncStateStore>();
		services.AddSingleton<ICatalogValidationService, CatalogValidationService>();
		services.AddSingleton<IDatParser, StreamingJsonDatParser>();
		services.AddSingleton<IDatParserFactory, DatParserFactory>();
		services.AddSingleton<IDatProvider, TosecProvider>();
		services.AddSingleton<IDatProvider, GoodToolsProvider>();
		services.AddSingleton<IDatProvider, NoIntroProvider>();
		services.AddSingleton<IDatCollectionService, DatCollectionService>();

		return services;
	}
}
