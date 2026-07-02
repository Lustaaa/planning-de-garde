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

            // DI généralisée (Sc.9, s15) : en mode Mongo, TOUT le domaine droite devient durable —
            // chaque adaptateur Mongo write-through SURCHARGE l'enregistrement InMemory posé plus haut
            // (dernière inscription gagne à la résolution). Les slots survivent au redémarrage de l'hôte.
            services.AddSingleton<ISlotRepository>(_ => new MongoSlotRepository(connectionString, baseDeDonnees));
            services.AddSingleton<IPeriodeRepository>(_ => new MongoPeriodeRepository(connectionString, baseDeDonnees));
            services.AddSingleton<ITransfertRepository>(_ => new MongoTransfertRepository(connectionString, baseDeDonnees));

            // Singleton paresseux (créé au 1er résolu, pas au build) : la connexion + le seed-once
            // n'ont lieu qu'au premier usage, jamais au démarrage si la config Mongo est inerte.
            services.AddSingleton(_ => new ConfigurationFoyerMongo(connectionString, baseDeDonnees));
            services.AddSingleton<IReferentielResponsables>(sp => sp.GetRequiredService<ConfigurationFoyerMongo>());
            services.AddSingleton<IPaletteCouleurs>(sp => sp.GetRequiredService<ConfigurationFoyerMongo>());
            services.AddSingleton<IEditeurConfigurationFoyer>(sp => sp.GetRequiredService<ConfigurationFoyerMongo>());
            services.AddSingleton<IEnumerationActeursFoyer>(sp => sp.GetRequiredService<ConfigurationFoyerMongo>());

            // Référentiel de rôles (petit agrégat de config foyer, s21) durable Mongo, borné à la config
            // foyer (même socle Mongo, collection dédiée « roles ») : lecture IEnumerationRoles + écriture
            // IEditeurReferentielRoles, un rôle créé/renommé/supprimé survit au redémarrage de l'hôte.
            services.AddSingleton(_ => new ReferentielRolesMongo(connectionString, baseDeDonnees));
            services.AddSingleton<IEnumerationRoles>(sp => sp.GetRequiredService<ReferentielRolesMongo>());
            services.AddSingleton<IEditeurReferentielRoles>(sp => sp.GetRequiredService<ReferentielRolesMongo>());

            // Référentiel des comptes utilisateurs (petit agrégat de config foyer, s22) durable Mongo,
            // borné à la config foyer (même socle Mongo, collection dédiée « comptes ») : lecture
            // IEnumerationComptes + écriture IEditeurComptes, un compte créé survit au redémarrage.
            services.AddSingleton(_ => new ReferentielComptesMongo(connectionString, baseDeDonnees));
            services.AddSingleton<IEnumerationComptes>(sp => sp.GetRequiredService<ReferentielComptesMongo>());
            services.AddSingleton<IEditeurComptes>(sp => sp.GetRequiredService<ReferentielComptesMongo>());

            // Admins du foyer (petit agrégat de config foyer, s22) durable Mongo, borné à la config
            // foyer (même socle Mongo, collection dédiée « admins ») : lecture IEnumerationAdminsFoyer +
            // écriture IEditeurAdminsFoyer, une désignation d'admin survit au redémarrage. L'invariant
            // admin=parent reste porté par l'agrégat Domain.
            services.AddSingleton(_ => new AdminsFoyerMongo(connectionString, baseDeDonnees));
            services.AddSingleton<IEnumerationAdminsFoyer>(sp => sp.GetRequiredService<AdminsFoyerMongo>());
            services.AddSingleton<IEditeurAdminsFoyer>(sp => sp.GetRequiredService<AdminsFoyerMongo>());
        }
        else
        {
            services.AddSingleton<ConfigurationFoyerEnMemoire>();
            services.AddSingleton<IReferentielResponsables>(sp => sp.GetRequiredService<ConfigurationFoyerEnMemoire>());
            services.AddSingleton<IPaletteCouleurs>(sp => sp.GetRequiredService<ConfigurationFoyerEnMemoire>());
            services.AddSingleton<IEditeurConfigurationFoyer>(sp => sp.GetRequiredService<ConfigurationFoyerEnMemoire>());
            services.AddSingleton<IEnumerationActeursFoyer>(sp => sp.GetRequiredService<ConfigurationFoyerEnMemoire>());

            // Référentiel de rôles InMemory (volatile, re-parti vide au redémarrage) — même ports.
            services.AddSingleton<ReferentielRolesEnMemoire>();
            services.AddSingleton<IEnumerationRoles>(sp => sp.GetRequiredService<ReferentielRolesEnMemoire>());
            services.AddSingleton<IEditeurReferentielRoles>(sp => sp.GetRequiredService<ReferentielRolesEnMemoire>());

            // Référentiel des comptes utilisateurs InMemory (volatile, re-parti vide au redémarrage) — mêmes ports.
            services.AddSingleton<ReferentielComptesEnMemoire>();
            services.AddSingleton<IEnumerationComptes>(sp => sp.GetRequiredService<ReferentielComptesEnMemoire>());
            services.AddSingleton<IEditeurComptes>(sp => sp.GetRequiredService<ReferentielComptesEnMemoire>());

            // Admins du foyer InMemory (volatile, re-parti vide au redémarrage) — mêmes ports.
            services.AddSingleton<AdminsFoyerEnMemoire>();
            services.AddSingleton<IEnumerationAdminsFoyer>(sp => sp.GetRequiredService<AdminsFoyerEnMemoire>());
            services.AddSingleton<IEditeurAdminsFoyer>(sp => sp.GetRequiredService<AdminsFoyerEnMemoire>());
        }

        // Cycle de fond : adaptateur InMemory singleton par défaut (volatile) = source de vérité partagée
        // du foyer ; réalise le port cycle (lecture par GrilleAgendaQuery, écriture par DefinirCycleHandler).
        // En mode Mongo (Sc.9, s15), il est SURCHARGÉ par CycleDeFondMongo durable — le fond se re-résout
        // après redémarrage de l'hôte.
        services.AddSingleton<IReferentielCycleDeFond, CycleDeFondEnMemoire>();
        if (string.Equals(configuration?["Foyer:Persistance"], "Mongo", System.StringComparison.OrdinalIgnoreCase))
        {
            var connectionString = configuration?["Foyer:Mongo:ConnectionString"] ?? "mongodb://localhost:27017";
            var baseDeDonnees = configuration?["Foyer:Mongo:Database"] ?? "planning_de_garde";
            services.AddSingleton<IReferentielCycleDeFond>(_ => new CycleDeFondMongo(connectionString, baseDeDonnees));
        }

        // Port temps réel réel (SignalR) — remplace le fake des scénarios.
        services.AddSingleton<INotificateurPlanning, SignalRNotificateurPlanning>();

        // Use cases (handlers) et read models.
        services.AddScoped<PoserSlotHandler>();
        services.AddScoped<DeplacerSlotHandler>();
        services.AddScoped<SupprimerSlotHandler>();
        services.AddScoped<AffecterPeriodeHandler>();
        services.AddScoped<ModifierPeriodeHandler>();
        services.AddScoped<EditerPeriodeHandler>();
        services.AddScoped<SupprimerPeriodeHandler>();
        services.AddScoped<DefinirTransfertHandler>();
        services.AddScoped<EditerActeurHandler>();
        services.AddScoped<AjouterActeurHandler>();
        services.AddScoped<SupprimerActeurHandler>();
        services.AddScoped<DefinirCycleHandler>();
        services.AddScoped<CreerRoleHandler>();
        services.AddScoped<RenommerRoleHandler>();
        services.AddScoped<SupprimerRoleHandler>();
        services.AddScoped<AffecterRoleActeurHandler>();
        services.AddScoped<RetirerRoleActeurHandler>();
        services.AddScoped<CreerCompteHandler>();
        services.AddScoped<ActiverCompteHandler>();
        services.AddScoped<DesignerAdminHandler>();
        services.AddScoped<SeConnecterHandler>();
        services.AddScoped<JourneeEnfantQuery>();
        services.AddScoped<ResponsabiliteQuery>();
        services.AddScoped<GrilleAgendaQuery>();
        services.AddScoped<PeriodesDuJourQuery>();
        services.AddScoped<SlotsDuJourQuery>();

        return services;
    }
}
