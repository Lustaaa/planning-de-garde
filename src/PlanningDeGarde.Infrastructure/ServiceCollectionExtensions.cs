using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Infrastructure;

/// <summary>Câblage DI de l'Application + Infrastructure pour l'hôte Web.</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AjouterPlanningDeGarde(this IServiceCollection services)
    {
        // Persistance en mémoire — singletons = source de vérité partagée du foyer.
        services.AddSingleton<InMemorySlotRepository>();
        services.AddSingleton<InMemoryPeriodeRepository>();
        services.AddSingleton<InMemoryTransfertRepository>();
        services.AddSingleton<ISlotRepository>(sp => sp.GetRequiredService<InMemorySlotRepository>());
        services.AddSingleton<IPeriodeRepository>(sp => sp.GetRequiredService<InMemoryPeriodeRepository>());
        services.AddSingleton<ITransfertRepository>(sp => sp.GetRequiredService<InMemoryTransfertRepository>());
        services.AddSingleton<ILieuRepository, FoyerLieuRepository>();
        services.AddSingleton<IResponsableRepository, FoyerResponsableRepository>();

        // Configuration volatile des acteurs : store mutable singleton seedé depuis Foyer, source de
        // vérité partagée des noms ET des couleurs. Il réalise À LA FOIS les ports de LECTURE
        // IReferentielResponsables (nom) et IPaletteCouleurs (couleur) — la grille relit nom + couleur
        // édités, via GrilleAgendaQuery inchangé — et le port d'ÉCRITURE IEditeurConfigurationFoyer
        // (renommer / recolorier). Remplace les bindings statiques FoyerReferentielResponsables (Sc.1) et
        // FoyerPaletteCouleurs (Sc.2) : sans ce câblage, la grille relirait les dictionnaires figés et
        // l'édition ne suivrait pas.
        services.AddSingleton<ConfigurationFoyerEnMemoire>();
        services.AddSingleton<IReferentielResponsables>(sp => sp.GetRequiredService<ConfigurationFoyerEnMemoire>());
        services.AddSingleton<IPaletteCouleurs>(sp => sp.GetRequiredService<ConfigurationFoyerEnMemoire>());
        services.AddSingleton<IEditeurConfigurationFoyer>(sp => sp.GetRequiredService<ConfigurationFoyerEnMemoire>());

        // Port temps réel réel (SignalR) — remplace le fake des scénarios.
        services.AddSingleton<INotificateurPlanning, SignalRNotificateurPlanning>();

        // Use cases (handlers) et read models.
        services.AddScoped<PoserSlotHandler>();
        services.AddScoped<DeplacerSlotHandler>();
        services.AddScoped<AffecterPeriodeHandler>();
        services.AddScoped<ModifierPeriodeHandler>();
        services.AddScoped<DefinirTransfertHandler>();
        services.AddScoped<EditerActeurHandler>();
        services.AddScoped<JourneeEnfantQuery>();
        services.AddScoped<ResponsabiliteQuery>();
        services.AddScoped<GrilleAgendaQuery>();

        return services;
    }
}
