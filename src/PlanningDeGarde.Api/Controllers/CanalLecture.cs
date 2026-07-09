using System.Linq;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Api;

/// <summary>
/// Adaptateur de lecture (CQRS) porté sur l'hôte d'API détaché : expose la projection
/// <see cref="GrilleAgendaQuery"/> de la grille agenda comme endpoint HTTP que le front
/// <b>WASM</b> consomme à distance (le navigateur n'a pas accès au store ni à la projection
/// en DI directe — il lit la grille via l'API distante, comme il y écrit via le canal
/// d'écriture). Lecture seule : n'écrit jamais, ne déclenche jamais la diffusion.
/// </summary>
public static class CanalLecture
{
    /// <summary>Vue d'un acteur du foyer énumérée pour l'écran de configuration : identifiant stable
    /// (clé de résolution) + nom d'affichage courant + couleur courante, tous deux résolus sur cet
    /// identifiant (couleur neutre par contrat si l'acteur n'en a pas) — pour que la liste de
    /// configuration affiche le nom ET sa pastille de couleur, cohérente avec la grille. Le
    /// <see cref="Type"/> (Admin / Parent / Autre) est surfacé en LECTURE SEULE depuis le seed (D3,
    /// sprint 14) pour piloter le rôle de l'identité effective lors d'une impersonation bornée. Le
    /// <see cref="RoleId"/> (s21) est l'identifiant stable du rôle du référentiel porté par l'acteur, ou
    /// <c>null</c> = « sans rôle » (attribut optionnel, neutre assumé) — pour afficher le rôle courant et
    /// pré-sélectionner le sélecteur borné au référentiel (jamais un libellé en dur).</summary>
    public sealed record ActeurFoyerVue(string Id, string Nom, string Couleur, TypeActeur Type, string? RoleId);

    /// <summary>Vue d'un rôle du référentiel du foyer énumérée pour l'écran de configuration (onglet
    /// Acteurs, s21) : identifiant stable opaque (clé, jamais le libellé) + libellé d'affichage éditable.
    /// Alimente la liste des rôles et le sélecteur de rôle borné au référentiel (jamais de rôle en dur).</summary>
    public sealed record RoleFoyerVue(string Id, string Libelle);

    /// <summary>Vue d'un lieu du référentiel du foyer énumérée pour l'écran de configuration (onglet Lieux,
    /// s27) et les sélecteurs de lieu des dialogs : identifiant stable (clé, bindé par les sélecteurs) +
    /// libellé d'affichage. Lue depuis le store vivant (jamais la liste en dur Foyer.Lieux).</summary>
    public sealed record LieuFoyerVue(string Id, string Libelle);

    /// <summary>Vue d'un enfant du référentiel du foyer énumérée pour l'écran de configuration (onglet Enfants,
    /// s30) et le sélecteur d'enfant des dialogs de pose : identifiant stable opaque (clé, bindé par les
    /// sélecteurs, jamais le prénom) + prénom d'affichage. Lue depuis le store vivant (jamais un enfant en dur).</summary>
    public sealed record EnfantFoyerVue(string Id, string Prenom);

    /// <summary>Vue d'un compte utilisateur du foyer énumérée pour l'écran de configuration (onglet Acteurs,
    /// s22) : identifiant stable opaque (clé, jamais l'email) + email + statut (« inactif » / « actif ») +
    /// id stable de l'acteur associé, ou <c>null</c> quand le compte est désassocié (Sc.6). Alimente
    /// l'affichage du compte associé à un acteur et son statut, sans règle métier côté UI.</summary>
    public sealed record CompteFoyerVue(string Id, string Email, string Statut, string? ActeurId);

    /// <summary>Vue d'une période couvrant une date, pour alimenter les dialogs de suppression et d'édition :
    /// identifiant stable (clé, jamais le libellé), identifiant stable du responsable (pour pré-sélectionner
    /// l'édition), nom d'affichage du responsable résolu sur l'id, et bornes datées. Lecture seule — ne
    /// déclenche jamais la diffusion.</summary>
    public sealed record PeriodeDuJourVue(string Id, string ResponsableId, string ResponsableNom, DateTime Debut, DateTime Fin);

    /// <summary>Vue d'un slot couvrant une date, pour alimenter la dialog de suppression de slot : identifiant
    /// stable (clé, jamais le libellé), enfant, lieu (libellé d'affichage), bornes horaires datées. Lecture
    /// seule — ne déclenche jamais la diffusion.</summary>
    public sealed record SlotDuJourVue(string Id, string EnfantId, string LieuId, DateTime Debut, DateTime Fin);

    public static IEndpointRouteBuilder MapperCanalLecture(this IEndpointRouteBuilder routes)
    {
        // Énumération des acteurs du foyer DEPUIS LE STORE (et non la liste statique front
        // Foyer.ActeursEditables) : l'écran de configuration la lit pour faire apparaître un acteur
        // fraîchement ajouté (Sc.1). Le nom est résolu sur l'identifiant stable (jamais le libellé).
        routes.MapGet("/api/foyer/acteurs",
            (IEnumerationActeursFoyer enumeration, IReferentielResponsables referentiel, IPaletteCouleurs palette) =>
            {
                var acteurs = enumeration.EnumererActeurs()
                    .Select(id => new ActeurFoyerVue(
                        id, referentiel.NomDe(id), palette.CouleurDe(id), enumeration.TypeDe(id), enumeration.RoleDe(id)))
                    .ToList();
                return Results.Ok(acteurs);
            });

        // Énumération des rôles du référentiel du foyer DEPUIS LE STORE (s21) : l'onglet Acteurs de
        // l'écran config la lit pour lister les rôles et alimenter le sélecteur borné au référentiel
        // (jamais de rôle en dur). Lecture seule — ne déclenche jamais la diffusion.
        routes.MapGet("/api/foyer/roles",
            (IEnumerationRoles roles) =>
            {
                var vues = roles.EnumererRoles()
                    .Select(r => new RoleFoyerVue(r.Id, r.Libelle))
                    .ToList();
                return Results.Ok(vues);
            });

        // Énumération des lieux du référentiel du foyer DEPUIS LE STORE VIVANT (s27) : l'onglet Lieux de
        // l'écran config la lit pour lister les lieux, ET les dialogs (Poser un slot / Définir un transfert)
        // pour peupler leur sélecteur de lieu — un seul canal de lecture, jamais la liste en dur Foyer.Lieux.
        // Lecture seule — ne déclenche jamais la diffusion.
        routes.MapGet("/api/foyer/lieux",
            (IEnumerationLieux lieux) =>
            {
                var vues = lieux.EnumererLieux()
                    .Select(l => new LieuFoyerVue(l.Id, l.Libelle))
                    .ToList();
                return Results.Ok(vues);
            });

        // Énumération des enfants du référentiel du foyer DEPUIS LE STORE VIVANT (s30) : l'onglet Enfants de
        // l'écran config la lit pour lister les enfants, ET la dialog « Poser un slot » pour peupler son
        // sélecteur d'enfant — un seul canal de lecture, jamais un enfant en dur / fantôme. Lecture seule.
        routes.MapGet("/api/foyer/enfants",
            (IEnumerationEnfants enfants) =>
            {
                var vues = enfants.EnumererEnfants()
                    .Select(e => new EnfantFoyerVue(e.Id, e.Prenom))
                    .ToList();
                return Results.Ok(vues);
            });

        // Énumération des comptes utilisateurs du foyer DEPUIS LE STORE (s22) : l'onglet Acteurs de
        // l'écran config la lit pour afficher le compte associé à chaque acteur et son statut. Lecture
        // seule — ne déclenche jamais la diffusion. Le statut est rendu en libellé minuscule stable.
        routes.MapGet("/api/foyer/comptes",
            (IEnumerationComptes comptes) =>
            {
                var vues = comptes.EnumererComptes()
                    .Select(c => new CompteFoyerVue(c.Id, c.Email, c.Statut.ToString().ToLowerInvariant(), c.ActeurId))
                    .ToList();
                return Results.Ok(vues);
            });

        // Énumération des ids d'acteurs admins du foyer DEPUIS LE STORE (s22) : l'onglet Acteurs de l'écran
        // config la lit pour marquer l'acteur admin. Lecture seule — ne déclenche jamais la diffusion.
        routes.MapGet("/api/foyer/admins",
            (IEnumerationAdminsFoyer admins) => Results.Ok(admins.EnumererAdmins().ToList()));

        // Grille projetée à une ANCRE (date de référence / date naviguée), passée en segments yyyy/MM/dd
        // pour le déterminisme côté front (jamais DateTime.Now côté serveur), plus un paramètre de VUE
        // (span : semaine / 4semaines / mois) sur le canal de LECTURE (CQRS — ne déclenche jamais la
        // diffusion, Sc.1/Sc.2). Compatibilité ascendante : sans vue → défaut 4 semaines glissantes
        // (Sc.3). Renvoie le read model GrilleAgenda (records framework-free de l'Application) en JSON.
        routes.MapGet("/api/grille/{annee:int}/{mois:int}/{jour:int}",
            (int annee, int mois, int jour, string? vue, GrilleAgendaQuery projection) =>
            {
                var grille = projection.Projeter(new DateOnly(annee, mois, jour), VueDepuis(vue));
                return Results.Ok(grille);
            });

        // Périodes COUVRANT une date (canal de lecture, CQRS) — alimente la dialog de suppression du menu
        // clic-case. Chaque période est rendue avec son identifiant stable, le nom du responsable résolu
        // sur l'identifiant (jamais le libellé brut) et ses bornes. Ne déclenche jamais la diffusion.
        routes.MapGet("/api/periodes/{annee:int}/{mois:int}/{jour:int}",
            (int annee, int mois, int jour, PeriodesDuJourQuery periodes, IReferentielResponsables referentiel) =>
            {
                var vues = periodes.Lister(new DateOnly(annee, mois, jour))
                    .Select(p => new PeriodeDuJourVue(p.Id, p.ResponsableId, referentiel.NomDe(p.ResponsableId), p.Debut, p.Fin))
                    .ToList();
                return Results.Ok(vues);
            });

        // Slots COUVRANT une date (canal de lecture, CQRS) — alimente la dialog de suppression de slot du
        // menu clic-case. Chaque slot est rendu avec son identifiant stable, son enfant, son lieu et ses
        // bornes horaires. Un slot franchissant minuit couvre ses deux jours. Ne déclenche jamais la diffusion.
        routes.MapGet("/api/slots/{annee:int}/{mois:int}/{jour:int}",
            (int annee, int mois, int jour, SlotsDuJourQuery slots) =>
            {
                var vues = slots.Lister(new DateOnly(annee, mois, jour))
                    .Select(s => new SlotDuJourVue(s.Id, s.EnfantId, s.LieuId, s.Debut, s.Fin))
                    .ToList();
                return Results.Ok(vues);
            });

        return routes;
    }

    /// <summary>Résout le code de vue (paramètre de lecture) en <see cref="VuePlanning"/> : <c>semaine</c>
    /// / <c>mois</c>, sinon <see cref="VuePlanning.QuatreSemaines"/> par défaut (et compat ascendante :
    /// absence de vue → 4 semaines glissantes, Sc.3).</summary>
    private static VuePlanning VueDepuis(string? code) => code switch
    {
        "semaine" => VuePlanning.Semaine,
        "mois" => VuePlanning.Mois,
        _ => VuePlanning.QuatreSemaines,
    };
}
