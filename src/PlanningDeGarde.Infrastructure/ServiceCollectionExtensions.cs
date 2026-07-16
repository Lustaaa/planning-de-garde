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
        services.AddSingleton<InMemorySlotRecurrentRepository>();
        services.AddSingleton<InMemoryPeriodeRepository>();
        services.AddSingleton<InMemoryTransfertRepository>();
        services.AddSingleton<ISlotRepository>(sp => sp.GetRequiredService<InMemorySlotRepository>());
        services.AddSingleton<ISlotRecurrentRepository>(sp => sp.GetRequiredService<InMemorySlotRecurrentRepository>());
        services.AddSingleton<IPeriodeRepository>(sp => sp.GetRequiredService<InMemoryPeriodeRepository>());
        services.AddSingleton<ITransfertRepository>(sp => sp.GetRequiredService<InMemoryTransfertRepository>());
        services.AddSingleton<IResponsableRepository, FoyerResponsableRepository>();

        // Référentiel de lieux du foyer (petit agrégat de config foyer, s27) : store mutable seedé
        // depuis Foyer.Activites, réalise la lecture IEnumerationActivites (validation de pose + sélecteurs)
        // et l'écriture IEditeurActivites (ajouter). Remplace la liste en dur Foyer.Activites lue par
        // l'ancien FoyerLieuRepository (trou s27). Le remplaçant durable Mongo est branché en S4.
        services.AddSingleton<ReferentielActivitesEnMemoire>();
        services.AddSingleton<IEnumerationActivites>(sp => sp.GetRequiredService<ReferentielActivitesEnMemoire>());
        services.AddSingleton<IEditeurActivites>(sp => sp.GetRequiredService<ReferentielActivitesEnMemoire>());

        // Référentiel d'enfants du foyer (petit agrégat de config foyer hissé en 1er rang, s30 — miroir
        // strict du référentiel de lieux) : store mutable, réalise la lecture IEnumerationEnfants
        // (validation de pose + sélecteur d'enfant) et l'écriture IEditeurEnfants (ajouter / éditer).
        // Le remplaçant durable Mongo est branché en S6.
        //
        // SEED au composition root (parité seed InMemory lieux/acteurs, asymétrie s15 : jamais côté
        // Mongo) : l'enfant historique « Léa » (Foyer.Enfants) est amorcé sur un id stable = son prénom,
        // pour préserver les slots DÉJÀ posés sous l'EnfantId fantôme « Léa » (transmis par Session) — la
        // pose runtime reste acceptée (validation de pose, S7). Le seed est posé ICI (pas dans le ctor de
        // l'adaptateur) : les `new ReferentielEnfantsEnMemoire()` des tests unitaires restent vierges.
        services.AddSingleton<ReferentielEnfantsEnMemoire>(_ =>
        {
            var referentiel = new ReferentielEnfantsEnMemoire();
            foreach (var prenom in Foyer.Enfants)
                referentiel.Ajouter(prenom, prenom); // enfant historique : id stable = prénom
            return referentiel;
        });
        services.AddSingleton<IEnumerationEnfants>(sp => sp.GetRequiredService<ReferentielEnfantsEnMemoire>());
        services.AddSingleton<IEditeurEnfants>(sp => sp.GetRequiredService<ReferentielEnfantsEnMemoire>());

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
            services.AddSingleton<ISlotRecurrentRepository>(_ => new MongoSlotRecurrentRepository(connectionString, baseDeDonnees));
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

            // Référentiel de lieux (petit agrégat de config foyer, s27) durable Mongo, borné à la config
            // foyer (même socle Mongo, collection dédiée « lieux ») : lecture IEnumerationActivites (validation
            // de pose + sélecteurs) + écriture IEditeurActivites, un lieu ajouté/supprimé survit au redémarrage.
            // Aucun seed Mongo (parité asymétrie seed s15) — SURCHARGE l'enregistrement InMemory seedé posé
            // plus haut (dernière inscription gagne à la résolution).
            services.AddSingleton(_ => new ReferentielActivitesMongo(connectionString, baseDeDonnees));
            services.AddSingleton<IEnumerationActivites>(sp => sp.GetRequiredService<ReferentielActivitesMongo>());
            services.AddSingleton<IEditeurActivites>(sp => sp.GetRequiredService<ReferentielActivitesMongo>());

            // Référentiel d'enfants (petit agrégat de config foyer hissé en 1er rang, s30) durable Mongo,
            // borné à la config foyer (même socle Mongo, collection dédiée « enfants ») : lecture
            // IEnumerationEnfants (validation de pose + sélecteur) + écriture IEditeurEnfants, un enfant
            // ajouté/édité survit au redémarrage. Aucun seed Mongo (parité asymétrie seed s15) — SURCHARGE
            // l'enregistrement InMemory posé plus haut (dernière inscription gagne à la résolution).
            services.AddSingleton(_ => new ReferentielEnfantsMongo(connectionString, baseDeDonnees));
            services.AddSingleton<IEnumerationEnfants>(sp => sp.GetRequiredService<ReferentielEnfantsMongo>());
            services.AddSingleton<IEditeurEnfants>(sp => sp.GetRequiredService<ReferentielEnfantsMongo>());

            // Référentiel des comptes utilisateurs (petit agrégat de config foyer, s22) durable Mongo,
            // borné à la config foyer (même socle Mongo, collection dédiée « comptes ») : lecture
            // IEnumerationComptes + écriture IEditeurComptes, un compte créé survit au redémarrage.
            services.AddSingleton(_ => new ReferentielComptesMongo(connectionString, baseDeDonnees));
            services.AddSingleton<IEnumerationComptes>(sp => sp.GetRequiredService<ReferentielComptesMongo>());
            services.AddSingleton<IEditeurComptes>(sp => sp.GetRequiredService<ReferentielComptesMongo>());

            // Jetons de réinitialisation (s28, volet 1) durables Mongo, bornés à l'auth (collection dédiée
            // « jetons_reset ») : émission / relecture / consommation usage-unique survivent au redémarrage.
            services.AddSingleton<IReferentielJetonsReset>(_ => new ReferentielJetonsResetMongo(connectionString, baseDeDonnees));

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
            // Config acteurs InMemory SEEDÉE des affectations de rôle B2 (s36) : les acteurs-parents du
            // seed portent un rôle marqué parent (Alice → Papa, Bruno → Maman) pour rester liables sous
            // l'éligibilité role-based ; les intervenants un rôle non-parent (Nina → Nounou, grand-père →
            // Grand-parent). Posé ICI (pas dans le ctor) : les `new ConfigurationFoyerEnMemoire()` des tests
            // unitaires restent sans affectation. Parité seed InMemory enfants/rôles (asymétrie s15).
            services.AddSingleton<ConfigurationFoyerEnMemoire>(_ =>
            {
                var config = new ConfigurationFoyerEnMemoire();
                foreach (var (acteurId, roleId) in Foyer.RolesParActeur)
                    config.AffecterRole(acteurId, roleId);
                return config;
            });
            services.AddSingleton<IReferentielResponsables>(sp => sp.GetRequiredService<ConfigurationFoyerEnMemoire>());
            services.AddSingleton<IPaletteCouleurs>(sp => sp.GetRequiredService<ConfigurationFoyerEnMemoire>());
            services.AddSingleton<IEditeurConfigurationFoyer>(sp => sp.GetRequiredService<ConfigurationFoyerEnMemoire>());
            services.AddSingleton<IEnumerationActeursFoyer>(sp => sp.GetRequiredService<ConfigurationFoyerEnMemoire>());

            // Référentiel de rôles InMemory SEEDÉ (amorçage B2, s36) : les rôles Papa/Maman/Parent
            // pré-cochés « est rôle parent », Nounou/Grand-parent non — source de vérité de l'éligibilité.
            // Le pré-cochage ne vaut QUE pour ce seed initial (un rôle créé ensuite démarre non-parent).
            // Posé ICI (pas dans le ctor) : les `new ReferentielRolesEnMemoire()` des tests restent vierges.
            services.AddSingleton<ReferentielRolesEnMemoire>(_ =>
            {
                var roles = new ReferentielRolesEnMemoire();
                foreach (var seed in Foyer.RolesSeed)
                {
                    roles.Creer(seed.Id, seed.Libelle);
                    if (seed.EstRoleParent)
                        roles.MarquerParent(seed.Id, true);
                }
                return roles;
            });
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

            // Jetons de réinitialisation (s28, volet 1) InMemory (volatile, re-parti vide au redémarrage) — même port.
            services.AddSingleton<IReferentielJetonsReset, ReferentielJetonsResetEnMemoire>();
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

        // Diffusion PORTEUSE DE PAYLOAD de la cloche (s47, décision transport SM) : le client reprojette sa
        // cloche depuis la diffusion (0 GET sur push, garde-fou anti-flake). Lecture seule : la donnée diffusée
        // est une trace de lecture, l'écriture reste sur le canal requête/réponse.
        services.AddSingleton<INotificateurChangement, SignalRNotificateurChangement>();

        // Cloche s47 : journal de changements (trace de LECTURE, jamais autorité de résolution), état lu/non-lu
        // PAR utilisateur, propositions d'échange. Stores SINGLETONS = source de vérité partagée du foyer. Le
        // journal est DÉCORÉ (JournalChangementsDiffusant) : chaque consignation par un handler d'écriture DIFFUSE
        // l'événement (payload) SANS modifier les handlers (composition de ports). En mode Mongo, tout est durable.
        if (string.Equals(configuration?["Foyer:Persistance"], "Mongo", System.StringComparison.OrdinalIgnoreCase))
        {
            var connectionString = configuration?["Foyer:Mongo:ConnectionString"] ?? "mongodb://localhost:27017";
            var baseDeDonnees = configuration?["Foyer:Mongo:Database"] ?? "planning_de_garde";
            services.AddSingleton<IJournalChangements>(sp => new JournalChangementsDiffusant(
                new MongoJournalChangements(connectionString, baseDeDonnees), sp.GetRequiredService<INotificateurChangement>()));
            services.AddSingleton<IEtatLectureNotifications>(_ => new MongoEtatLectureNotifications(connectionString, baseDeDonnees));
            services.AddSingleton<IPropositionEchangeRepository>(_ => new MongoPropositionEchangeRepository(connectionString, baseDeDonnees));
        }
        else
        {
            services.AddSingleton<InMemoryJournalChangements>();
            services.AddSingleton<IJournalChangements>(sp => new JournalChangementsDiffusant(
                sp.GetRequiredService<InMemoryJournalChangements>(), sp.GetRequiredService<INotificateurChangement>()));
            services.AddSingleton<IEtatLectureNotifications, InMemoryEtatLectureNotifications>();
            services.AddSingleton<IPropositionEchangeRepository, InMemoryPropositionEchangeRepository>();
        }

        // Facteur mot de passe local (volet 3, s25) : hachage PBKDF2 salé réel, réalise IHacheurMotDePasse.
        services.AddSingleton<IHacheurMotDePasse, HacheurMotDePassePbkdf2>();

        // Horloge système réelle (s28) : réalise IDateTimeProvider côté hôte API — l'expiration des jetons
        // de réinitialisation (60 min) est datée contre l'horloge réelle. Doublée (figée) dans les tests.
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        // Fournisseur OAuth externe (s28, volet 3) : port enregistré en DI pour rendre ConnexionOAuthHandler
        // résolvable et le callback routable. L'adaptateur Google RÉEL (secrets / token endpoint) reste une
        // DETTE DE CÂBLAGE (backlog P0, vérif manuelle G3) → placeholder qui ne résout aucune identité tant
        // que le provider réel n'est pas branché. La logique de rapprochement est prouvée par doublure (S9).
        services.AddSingleton<IFournisseurOAuth, FournisseurOAuthGoogleNonCable>();

        // Canal mail réel (s28, volet 1) : adaptateur SMTP concret réalisant IEnvoiMail — remet un VRAI
        // mail de récupération au serveur SMTP configuré (Smtp4dev en dev, Docker). Remplace la doublure
        // s25 ; l'hôte/port/expéditeur sont pilotés par configuration (défauts alignés sur le run local).
        var smtpHote = configuration?["Mail:Smtp:Hote"] ?? "localhost";
        var smtpPort = int.TryParse(configuration?["Mail:Smtp:Port"], out var p) ? p : 2525;
        var smtpExpediteur = configuration?["Mail:Smtp:Expediteur"] ?? "no-reply@planning-de-garde.fr";
        services.AddSingleton<IEnvoiMail>(_ => new EnvoiMailSmtp(smtpHote, smtpPort, smtpExpediteur));

        // Use cases (handlers) et read models.
        services.AddScoped<PoserSlotHandler>();
        services.AddScoped<PoserSlotRecurrentHandler>();
        services.AddScoped<SupprimerSlotRecurrentHandler>();
        services.AddScoped<AjouterActiviteHandler>();
        services.AddScoped<SupprimerActiviteHandler>();
        services.AddScoped<EditerActiviteHandler>();
        services.AddScoped<LierEnfantActiviteHandler>();
        services.AddScoped<DelierEnfantActiviteHandler>();
        services.AddScoped<AjouterEnfantHandler>();
        services.AddScoped<EditerEnfantHandler>();
        services.AddScoped<LierEnfantParentHandler>();
        services.AddScoped<DelierEnfantParentHandler>();
        services.AddScoped<DeplacerSlotHandler>();
        services.AddScoped<SupprimerSlotHandler>();
        services.AddScoped<AffecterPeriodeHandler>();
        services.AddScoped<DeleguerRecuperationHandler>();
        services.AddScoped<AnnulerDelegationHandler>();
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
        services.AddScoped<MarquerRoleParentHandler>();
        services.AddScoped<SupprimerRoleHandler>();
        services.AddScoped<AffecterRoleActeurHandler>();
        services.AddScoped<RetirerRoleActeurHandler>();
        services.AddScoped<CreerCompteHandler>();
        services.AddScoped<CreerCompteLibreServiceHandler>();
        services.AddScoped<ActiverCompteHandler>();
        services.AddScoped<DesactiverCompteHandler>();
        services.AddScoped<DesignerAdminHandler>();
        services.AddScoped<DeDesignerAdminHandler>();
        services.AddScoped<SeConnecterHandler>();
        services.AddScoped<DemanderRecuperationMotDePasseHandler>();
        services.AddScoped<RedefinirMotDePasseHandler>();
        services.AddScoped<DefinirMotDePasseHandler>();
        services.AddScoped<ConnexionOAuthHandler>();
        services.AddScoped<JourneeEnfantQuery>();
        services.AddScoped<ResponsabiliteQuery>();
        services.AddScoped<GrilleAgendaQuery>();
        services.AddScoped<CyclesFoyerQuery>();
        services.AddScoped<GrapheFoyerQuery>();
        services.AddScoped<PeriodesDuJourQuery>();
        services.AddScoped<SlotsDuJourQuery>();

        // Cloche s47 : lecture du flux de notifications par utilisateur (+ compteur non-lus), marquer-lu,
        // et les use cases d'échange consenti (proposer / accepter / refuser).
        services.AddScoped<FluxNotificationsQuery>();
        services.AddScoped<MarquerNotificationsLuesHandler>();
        services.AddScoped<ProposerEchangeHandler>();
        services.AddScoped<AccepterPropositionHandler>();
        services.AddScoped<RefuserPropositionHandler>();

        // Signalement d'imprévu (s48) : consigne une trace au journal (décoré diffusant → cloche des concernés,
        // 0 GET), SANS aucune écriture de surcharge (résolution jamais touchée). Le journal injecté est le port
        // IJournalChangements (JournalChangementsDiffusant) : la consignation diffuse l'événement (payload).
        services.AddScoped<SignalerImprevuHandler>();

        return services;
    }
}
