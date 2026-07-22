using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Api;

// Lot 5 (refacto hors-sprint) — corps des requêtes REST reçus par les controllers MVC (un par
// bounded context / ressource). Ce sont de simples DTO de transport JSON : les identifiants portés
// par l'URL (ressource ciblée) NE figurent PLUS dans le corps (ex. l'id d'un slot supprimé voyage
// par DELETE /api/slots/{id}, pas dans le body). La forme JSON des champs restants est inchangée
// (compatibilité front). Les handlers invoqués derrière restent STRICTEMENT les mêmes.

/// <summary>Corps de la requête de pose de slot (POST /api/slots).</summary>
public sealed record PoserSlotRequete(string EnfantId, string LieuId, DateTime Debut, DateTime Fin);

/// <summary>Réponse de succès de la pose : porte l'avertissement de chevauchement (règle 16).</summary>
public sealed record PoserSlotReponse(bool Chevauchement);

/// <summary>Corps de la pose d'un slot RÉCURRENT hebdo (POST /api/slots/recurrents, s29).</summary>
public sealed record PoserSlotRecurrentRequete(
    string EnfantId, string LieuId, DayOfWeek JourDeSemaine, TimeSpan HeureDebut, TimeSpan HeureFin,
    bool ConditionneGarde = false, string PoseurId = "");

/// <summary>Corps de l'affectation de période (POST /api/periodes).</summary>
public sealed record AffecterPeriodeRequete(string ResponsableId, DateTime Debut, DateTime Fin, string EnfantId = "");

/// <summary>Corps de l'édition d'une période (PUT /api/periodes/{id}) : l'id est porté par l'URL ;
/// l'état observé (jeton de concurrence) est résolu côté serveur sur cet id avant d'appeler le handler.</summary>
public sealed record EditerPeriodeCorps(string NouveauResponsableId, DateTime NouveauDebut, DateTime NouvelleFin);

/// <summary>Corps de la délégation de la récupération d'une PLAGE (POST /api/delegations, s44→s45).</summary>
public sealed record DeleguerRecuperationRequete(DateOnly Jour, string EnfantId, string VersActeurId, DateOnly? JourFin = null);

/// <summary>Corps de la définition d'un transfert de bascule (POST /api/transferts).</summary>
public sealed record DefinirTransfertRequete(string DeposeParId, string RecupereParId, string LieuId, TimeSpan Heure, DateTime Date, string EnfantId = "");

/// <summary>Corps de l'ajout d'une activité au référentiel (POST /api/foyer/activites, s35).</summary>
public sealed record AjouterActiviteRequete(string Libelle);

/// <summary>Corps de l'édition d'une activité (PUT /api/foyer/activites/{id}, s35) : id porté par l'URL.</summary>
public sealed record EditerActiviteCorps(string? Libelle = null, string? Adresse = null);

/// <summary>Corps de l'ajout d'un enfant au référentiel (POST /api/foyer/enfants, s30).</summary>
public sealed record AjouterEnfantRequete(string Prenom);

/// <summary>Corps de l'édition du prénom d'un enfant (PUT /api/foyer/enfants/{id}, s30) : id porté par l'URL.</summary>
public sealed record EditerEnfantCorps(string NouveauPrenom);

/// <summary>Corps de la liaison enfant↔parent (PUT /api/foyer/enfants/{id}/parents/{acteurId}, s34/s37) :
/// enfant et acteur portés par l'URL ; seul le rôle-du-lien voyage dans le corps.</summary>
public sealed record LierEnfantParentCorps(RoleDuLien Role = RoleDuLien.ParentLibre);

/// <summary>Corps de l'ajout d'un acteur neuf au foyer (POST /api/foyer/acteurs).</summary>
public sealed record AjouterActeurRequete(string Nom, string? Couleur = null);

/// <summary>Corps de l'édition d'un acteur (PUT /api/foyer/acteurs/{id}) : id porté par l'URL.</summary>
public sealed record EditerActeurCorps(string? Nom = null, string? Couleur = null, string? Adresse = null);

/// <summary>Corps de l'affectation d'un rôle du référentiel à un acteur (PUT /api/foyer/acteurs/{id}/role, s21).</summary>
public sealed record AffecterRoleCorps(string RoleId);

/// <summary>Corps de la définition / ré-édition du cycle de fond (PUT /api/foyer/cycles, palier 6) :
/// le cycle est clé PAR ENFANT (champ EnfantId du corps), pas par une ressource d'URL.</summary>
public sealed record DefinirCycleRequete(int NombreSemaines, IReadOnlyDictionary<int, string> Affectations, string EnfantId = "");

/// <summary>Corps de la création d'un rôle du référentiel (POST /api/foyer/roles, s21).</summary>
public sealed record CreerRoleRequete(string Libelle);

/// <summary>Corps du renommage d'un rôle (PUT /api/foyer/roles/{id}) : id porté par l'URL.</summary>
public sealed record RenommerRoleCorps(string NouveauLibelle);

/// <summary>Corps de la bascule du flag « est rôle parent » (PUT /api/foyer/roles/{id}/parent, s36).</summary>
public sealed record MarquerRoleParentCorps(bool EstParent);

/// <summary>Corps de la création d'un compte utilisateur (POST /api/foyer/comptes, s22).</summary>
public sealed record CreerCompteRequete(string ActeurId, string Email);

/// <summary>Corps de la définition d'un mot de passe (PUT /api/foyer/comptes/{id}/mot-de-passe, s28) : id URL.</summary>
public sealed record DefinirMotDePasseCorps(string MotDePasse);

/// <summary>Corps de la connexion locale par email (POST /api/session, s23).</summary>
public sealed record SeConnecterRequete(string Email, string? MotDePasse = null);

/// <summary>Réponse de succès d'une connexion (s23 ; type ancré s25) : identité réelle + nom + type résolus serveur.</summary>
public sealed record SeConnecterReponse(string ActeurId, string Nom, TypeActeur Type);

/// <summary>Corps de la demande de récupération de mot de passe (POST /api/comptes/recuperation, s28).</summary>
public sealed record DemanderRecuperationRequete(string Email);

/// <summary>Corps de la redéfinition de mot de passe par jeton (POST /api/comptes/reinitialisation, s28).</summary>
public sealed record RedefinirMotDePasseRequete(string Jeton, string NouveauMotDePasse);

/// <summary>Corps « marquer lu » de la cloche (POST /api/notifications/lues, s47).</summary>
public sealed record MarquerNotificationsLuesRequete(string UtilisateurId, string? EvenementId = null);

/// <summary>Corps PROPOSER un échange sur une PLAGE (POST /api/propositions, s47→s52).</summary>
public sealed record ProposerEchangeRequete(DateOnly Jour, string EnfantId, string VersActeurId, DateOnly? JourFin = null);

/// <summary>Corps « action de suivi : proposer un échange suite imprévu » (POST /api/propositions/suite-imprevu, s51).</summary>
public sealed record ProposerEchangeSuiteImprevuRequete(string ImprevuEvenementId, string VersActeurId);

/// <summary>Corps SIGNALER un imprévu (POST /api/imprevus, s48).</summary>
public sealed record SignalerImprevuRequete(DateOnly Jour, string EnfantId, TypeImprevu Type, string SignalantId, string Motif = "");
