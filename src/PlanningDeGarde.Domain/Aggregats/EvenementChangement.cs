using System;

namespace PlanningDeGarde.Domain;

/// <summary>Nature d'un changement consigné au journal (cloche s47).</summary>
public enum TypeChangement
{
    /// <summary>Une délégation de récupération a été écrite (s44/s45 : surcharge d'un jour ou d'une plage).</summary>
    Delegation,

    /// <summary>Une reprise a été effectuée (s46 : la surcharge du jour a été SUPPRIMÉE).</summary>
    Reprise,

    /// <summary>Un transfert de bascule a été saisi (s31).</summary>
    Transfert,
}

/// <summary>
/// Événement du JOURNAL DE CHANGEMENTS (cloche s47) : trace de LECTURE horodatée d'une écriture du planning,
/// JAMAIS autorité de résolution (la vérité reste les périodes/transferts). <c>CedantId</c> / <c>RecevantId</c>
/// sont les acteurs concernés (l'un peut être vide selon le type) ; le flux d'un utilisateur retient les
/// événements où il figure. <c>Horodatage</c> = instant d'écriture (via IDateTimeProvider) pour le tri par récence
/// — indérivable de l'état courant (une reprise SUPPRIME la surcharge), d'où la nécessité du journal persisté.
/// </summary>
public sealed record EvenementChangementSnapshot(
    string Id,
    TypeChangement Type,
    DateOnly Jour,
    string EnfantId,
    string CedantId,
    string RecevantId,
    DateTime Horodatage);
