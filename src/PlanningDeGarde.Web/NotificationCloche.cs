using System;
using System.Collections.Generic;

namespace PlanningDeGarde.Web;

/// <summary>
/// Modèle front d'une notification de la CLOCHE, miroir du DTO d'API <c>NotificationClocheVue</c> : un
/// événement du journal (délégation / reprise / transfert — informationnel) OU une proposition d'échange
/// (<see cref="Actionnable"/> = pending adressée à l'utilisateur, boutons Accepter / Refuser). <see cref="Lu"/>
/// pilote le style non-lu ; <see cref="PropositionId"/> est la clé d'action (null pour un événement du journal).
/// Les ids d'acteurs sont bruts : le nom est résolu côté composant sur le référentiel déjà chargé.
/// </summary>
public sealed record NotificationCloche(
    string Id, string Type, DateOnly Jour, string EnfantId, string CedantId, string RecevantId,
    DateTime Horodatage, bool Lu, bool Actionnable, string? PropositionId, string Statut);

/// <summary>Charge de la cloche relue via le canal de lecture (<c>GET /api/notifications/{id}</c>) : compteur de
/// non-lus (badge) + flux chrono de l'utilisateur courant.</summary>
public sealed record ClocheChargement(int NonLus, IReadOnlyList<NotificationCloche> Notifications);
