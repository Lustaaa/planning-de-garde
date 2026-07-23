namespace PlanningDeGarde.Web;

/// <summary>
/// Corps des requêtes d'écriture émises par le front <b>WASM</b> vers les controllers REST de
/// l'<b>API distante</b> (ressources <c>/api/*</c>). Ce sont de simples DTO de transport JSON : le front
/// ne porte pas le routage (qui vit côté hôte d'API détaché, <c>PlanningDeGarde.Api</c>), il émet le verbe
/// HTTP + la ressource et n'en fournit que le corps. Les identifiants portés par l'URL (ressource ciblée)
/// NE figurent PLUS dans le corps — ils voyagent par le chemin (ex. <c>DELETE /api/slots/{id}</c>).
/// </summary>
public static class CanalEcriture
{
    /// <summary>Corps de la pose d'une activité (POST /api/enfants/{enfantId}/activites) : l'EnfantId
    /// est porté par l'URL (sous-ressource de l'enfant, s54), plus dans le corps.</summary>
    public sealed record PoserSlotRequete(string LieuId, DateTime Debut, DateTime Fin);

    /// <summary>Réponse de succès de la pose : l'avertissement de chevauchement, lu par la dialog.</summary>
    public sealed record PoserSlotReponse(bool Chevauchement);

    /// <summary>Corps de la pose d'une activité RÉCURRENTE hebdo
    /// (POST /api/enfants/{enfantId}/activites/recurrentes) : l'EnfantId est porté par l'URL.
    /// <see cref="JoursDeSemaine"/> (s54) : un set NON nul pose une série MULTI-JOURS.</summary>
    public sealed record PoserSlotRecurrentRequete(
        string LieuId, DayOfWeek JourDeSemaine, TimeSpan HeureDebut, TimeSpan HeureFin,
        bool ConditionneGarde = false, string PoseurId = "", IReadOnlyList<DayOfWeek>? JoursDeSemaine = null);

    /// <summary>Corps de l'édition d'une activité récurrente — TOUTE la série
    /// (PUT /api/enfants/{enfantId}/activites/recurrentes/{id}) : id + enfant portés par l'URL,
    /// l'EnfantId n'est jamais réaffecté (relu du slot existant côté handler).</summary>
    public sealed record ModifierSlotRecurrentCorps(
        string LieuId, IReadOnlyList<DayOfWeek> JoursDeSemaine, TimeSpan HeureDebut, TimeSpan HeureFin,
        bool ConditionneGarde = false, string PoseurId = "");

    /// <summary>Corps d'une plage d'exclusion (vacances) d'une activité récurrente
    /// (POST/DELETE /api/enfants/{enfantId}/activites/recurrentes/{id}/exclusions) : bornes calendaires incluses.</summary>
    public sealed record ExclusionCorps(DateOnly Debut, DateOnly Fin);

    /// <summary>Corps de l'affectation de période (POST /api/periodes).</summary>
    public sealed record AffecterPeriodeRequete(string ResponsableId, DateTime Debut, DateTime Fin, string EnfantId = "");

    /// <summary>Corps de l'édition d'une période (PUT /api/periodes/{id}) : l'id est porté par l'URL.</summary>
    public sealed record EditerPeriodeCorps(string NouveauResponsableId, DateTime NouveauDebut, DateTime NouvelleFin);

    /// <summary>Corps de la délégation d'une PLAGE (POST /api/delegations).</summary>
    public sealed record DeleguerRecuperationRequete(DateOnly Jour, string EnfantId, string VersActeurId, DateOnly? JourFin = null);

    /// <summary>Corps de la définition d'un transfert de bascule (POST /api/transferts).</summary>
    public sealed record DefinirTransfertRequete(string DeposeParId, string RecupereParId, string LieuId, TimeSpan Heure, DateTime Date, string EnfantId = "");

    /// <summary>Corps de l'ajout d'une activité au référentiel (POST /api/foyer/activites).</summary>
    public sealed record AjouterActiviteRequete(string Libelle);

    /// <summary>Corps de l'édition d'une activité (PUT /api/foyer/activites/{id}) : id porté par l'URL.</summary>
    public sealed record EditerActiviteCorps(string? Libelle = null, string? Adresse = null);

    /// <summary>Corps de l'ajout d'un enfant au référentiel (POST /api/foyer/enfants).</summary>
    public sealed record AjouterEnfantRequete(string Prenom);

    /// <summary>Corps de l'édition du prénom d'un enfant (PUT /api/foyer/enfants/{id}) : id porté par l'URL.</summary>
    public sealed record EditerEnfantCorps(string NouveauPrenom);

    /// <summary>Corps de la liaison enfant↔parent (PUT /api/foyer/enfants/{id}/parents/{acteurId}) :
    /// enfant et acteur portés par l'URL ; seul le rôle-du-lien voyage dans le corps.</summary>
    public sealed record LierEnfantParentCorps(PlanningDeGarde.Application.Foyer.Models.RoleDuLien Role = PlanningDeGarde.Application.Foyer.Models.RoleDuLien.ParentLibre);

    /// <summary>Corps de l'ajout d'un acteur neuf au foyer (POST /api/foyer/acteurs).</summary>
    public sealed record AjouterActeurRequete(string Nom, string? Couleur = null);

    /// <summary>Corps de l'édition d'un acteur (PUT /api/foyer/acteurs/{id}) : id porté par l'URL.</summary>
    public sealed record EditerActeurCorps(string? Nom = null, string? Couleur = null, string? Adresse = null);

    /// <summary>Corps de l'affectation d'un rôle du référentiel à un acteur (PUT /api/foyer/acteurs/{id}/role).</summary>
    public sealed record AffecterRoleCorps(string RoleId);

    /// <summary>Corps de la définition / ré-édition du cycle de fond (PUT /api/foyer/cycles) :
    /// le cycle est clé PAR ENFANT (champ EnfantId du corps).</summary>
    public sealed record DefinirCycleRequete(int NombreSemaines, IReadOnlyDictionary<int, string> Affectations, string EnfantId = "");

    /// <summary>Corps de la création d'un rôle du référentiel (POST /api/foyer/roles).</summary>
    public sealed record CreerRoleRequete(string Libelle);

    /// <summary>Corps du renommage d'un rôle (PUT /api/foyer/roles/{id}) : id porté par l'URL.</summary>
    public sealed record RenommerRoleCorps(string NouveauLibelle);

    /// <summary>Corps de la bascule du flag « est rôle parent » (PUT /api/foyer/roles/{id}/parent).</summary>
    public sealed record MarquerRoleParentCorps(bool EstParent);

    /// <summary>Corps de la création d'un compte utilisateur (POST /api/foyer/comptes).</summary>
    public sealed record CreerCompteRequete(string ActeurId, string Email);

    /// <summary>Corps de la connexion locale par email (POST /api/session).</summary>
    public sealed record SeConnecterRequete(string Email, string? MotDePasse = null);

    /// <summary>Corps de la demande de récupération de mot de passe (POST /api/comptes/recuperation).</summary>
    public sealed record DemanderRecuperationRequete(string Email);

    /// <summary>Corps de la redéfinition de mot de passe par jeton (POST /api/comptes/reinitialisation).</summary>
    public sealed record RedefinirMotDePasseRequete(string Jeton, string NouveauMotDePasse);

    /// <summary>Réponse de succès d'une connexion (type ancré) : id acteur + nom + type résolus serveur.</summary>
    public sealed record SeConnecterReponse(string ActeurId, string Nom, PlanningDeGarde.Application.Foyer.Models.TypeActeur Type);

    /// <summary>Corps « marquer lu » de la cloche (POST /api/notifications/lues).</summary>
    public sealed record MarquerNotificationsLuesRequete(string UtilisateurId, string? EvenementId = null);

    /// <summary>Corps PROPOSER un échange sur une PLAGE (POST /api/propositions).</summary>
    public sealed record ProposerEchangeRequete(DateOnly Jour, string EnfantId, string VersActeurId, DateOnly? JourFin = null);

    /// <summary>Corps « action de suivi : proposer un échange suite imprévu » (POST /api/propositions/suite-imprevu).</summary>
    public sealed record ProposerEchangeSuiteImprevuRequete(string ImprevuEvenementId, string VersActeurId);

    /// <summary>Corps SIGNALER un imprévu (POST /api/imprevus).</summary>
    public sealed record SignalerImprevuRequete(
        DateOnly Jour, string EnfantId, PlanningDeGarde.Domain.TypeImprevu Type, string SignalantId, string Motif = "");
}
