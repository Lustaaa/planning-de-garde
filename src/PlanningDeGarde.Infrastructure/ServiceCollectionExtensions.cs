using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Infrastructure;

/// <summary>Câblage DI de l'Application + Infrastructure pour l'hôte Web.</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AjouterPlanningDeGarde(this IServiceCollection services, IConfiguration? configuration = null)
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

        // Configuration des acteurs (noms ET couleurs) : un store mutable singleton réalise À LA FOIS
        // les ports de LECTURE IReferentielResponsables (nom) et IPaletteCouleurs (couleur) — la grille
        // relit nom + couleur édités, via GrilleAgendaQuery inchangé —, le port d'ÉNUMÉRATION
        // IEnumerationActeursFoyer (écran config) et le port d'ÉCRITURE IEditeurConfigurationFoyer
        // (ajouter / renommer / recolorier). Deux réalisations derrière les MÊMES ports :
        //   - Mongo DURABLE (Foyer:Persistance = "Mongo") : l'ajout et l'édition survivent au redémarrage
        //     (pivot Sc.3) — SEULE la config foyer passe durable (borne anti-cliquet, règle 30) ;
        //   - InMemory (défaut) : volatile, re-seedé au redémarrage (comportement antérieur préservé).
        if (string.Equals(configuration?["Foyer:Persistance"], "Mongo", System.StringComparison.OrdinalIgnoreCase))
        {
            var connectionString = configuration?["Foyer:Mongo:ConnectionString"] ?? "mongodb://localhost:27017";
            var baseDeDonnees = configuration?["Foyer:Mongo:Database"] ?? "planning_de_garde";
            // Singleton paresseux (créé au 1er résolu, pas au build) : la connexion + le seed-once
            // n'ont lieu qu'au premier usage, jamais au démarrage si la config Mongo est inerte.
            services.AddSingleton(_ => new ConfigurationFoyerMongo(connectionString, baseDeDonnees));
            services.AddSingleton<IReferentielResponsables>(sp => sp.GetRequiredService<ConfigurationFoyerMongo>());
            services.AddSingleton<IPaletteCouleurs>(sp => sp.GetRequiredService<ConfigurationFoyerMongo>());
            services.AddSingleton<IEditeurConfigurationFoyer>(sp => sp.GetRequiredService<ConfigurationFoyerMongo>());
            services.AddSingleton<IEnumerationActeursFoyer>(sp => sp.GetRequiredService<ConfigurationFoyerMongo>());
        }
        else
        {
            services.AddSingleton<ConfigurationFoyerEnMemoire>();
            services.AddSingleton<IReferentielResponsables>(sp => sp.GetRequiredService<ConfigurationFoyerEnMemoire>());
            services.AddSingleton<IPaletteCouleurs>(sp => sp.GetRequiredService<ConfigurationFoyerEnMemoire>());
            services.AddSingleton<IEditeurConfigurationFoyer>(sp => sp.GetRequiredService<ConfigurationFoyerEnMemoire>());
            services.AddSingleton<IEnumerationActeursFoyer>(sp => sp.GetRequiredService<ConfigurationFoyerEnMemoire>());
        }

        // Cycle de fond (palier 6) : adaptateur InMemory singleton = source de vérité partagée du
        // foyer, volatile (PAS Mongo — durabilité portée par un palier ultérieur, borne anti-cliquet
        // règle 30). Réalise le port cycle (lecture par GrilleAgendaQuery, écriture par DefinirCycleHandler).
        services.AddSingleton<IReferentielCycleDeFond, CycleDeFondEnMemoire>();

        // Port temps réel réel (SignalR) — remplace le fake des scénarios.
        services.AddSingleton<INotificateurPlanning, SignalRNotificateurPlanning>();

        // Use cases (handlers) et read models.
        services.AddScoped<PoserSlotHandler>();
        services.AddScoped<DeplacerSlotHandler>();
        services.AddScoped<AffecterPeriodeHandler>();
        services.AddScoped<ModifierPeriodeHandler>();
        services.AddScoped<DefinirTransfertHandler>();
        services.AddScoped<EditerActeurHandler>();
        services.AddScoped<AjouterActeurHandler>();
        services.AddScoped<SupprimerActeurHandler>();
        services.AddScoped<DefinirCycleHandler>();
        services.AddScoped<JourneeEnfantQuery>();
        services.AddScoped<ResponsabiliteQuery>();
        services.AddScoped<GrilleAgendaQuery>();

        return services;
    }
}
