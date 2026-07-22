using PlanningDeGarde.Domain;
using PlanningDeGarde.Application.Enfants.Ports;

namespace PlanningDeGarde.Api;

// Lot 5 (refacto hors-sprint) — read models exposés par les controllers de LECTURE (CQRS, lecture
// seule, jamais de diffusion). Forme JSON inchangée par rapport aux anciens endpoints minimal-API :
// seuls le nom du conteneur et le câblage changent. Les tests Api.Tests désérialisent ces types.

/// <summary>Vue d'un acteur du foyer pour l'écran de configuration (id + nom + couleur + type + rôle + adresse).</summary>
public sealed record ActeurFoyerVue(string Id, string Nom, string Couleur, TypeActeur Type, string? RoleId, string? Adresse);

/// <summary>Vue d'un rôle du référentiel (id + libellé + flag « est rôle parent », s36).</summary>
public sealed record RoleFoyerVue(string Id, string Libelle, bool EstRoleParent);

/// <summary>Vue d'une activité du référentiel (id + libellé + adresse + enfants liés, s35).</summary>
public sealed record ActiviteFoyerVue(string Id, string Libelle, string Adresse, IReadOnlyCollection<string> EnfantsLies);

/// <summary>Vue d'un enfant du référentiel (id + prénom + parents liés avec rôle-du-lien, s30/s34/s37).</summary>
public sealed record EnfantFoyerVue(string Id, string Prenom, IReadOnlyCollection<ParentLie> ParentsLies);

/// <summary>Vue d'un compte utilisateur du foyer (id + email + statut + acteur associé, s22).</summary>
public sealed record CompteFoyerVue(string Id, string Email, string Statut, string? ActeurId);

/// <summary>Vue d'une période couvrant une date (dialogs de suppression/édition).</summary>
public sealed record PeriodeDuJourVue(string Id, string ResponsableId, string ResponsableNom, DateTime Debut, DateTime Fin);

/// <summary>Vue d'un slot couvrant une date (dialog de suppression de slot).</summary>
public sealed record SlotDuJourVue(string Id, string EnfantId, string LieuId, DateTime Debut, DateTime Fin);

/// <summary>Vue d'un cycle de fond DÉCLARÉ (onglet Cycle de la config, s33).</summary>
public sealed record CycleFoyerVue(int IndexSemaine, string ResponsableId);

/// <summary>Vue d'une notification de la CLOCHE (s47) : événement du journal OU proposition d'échange actionnable.</summary>
public sealed record NotificationClocheVue(
    string Id, string Type, DateOnly Jour, string EnfantId, string CedantId, string RecevantId,
    DateTime Horodatage, bool Lu, bool Actionnable, string? PropositionId, string Statut);

/// <summary>Charge de la cloche pour l'utilisateur courant : compteur de non-lus (badge) + flux chrono.</summary>
public sealed record ClocheVue(int NonLus, IReadOnlyList<NotificationClocheVue> Notifications);
