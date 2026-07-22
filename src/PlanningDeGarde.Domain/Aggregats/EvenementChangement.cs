using System;

namespace PlanningDeGarde.Domain;

/// <summary>Nature d'un changement consigné au journal (cloche).</summary>
public enum TypeChangement
{
    /// <summary>Une délégation de récupération a été écrite (: surcharge d'un jour ou d'une plage).</summary>
    Delegation,

    /// <summary>Une reprise a été effectuée (: la surcharge du jour a été SUPPRIMÉE).</summary>
    Reprise,

    /// <summary>Un transfert de bascule a été saisi.</summary>
    Transfert,

    /// <summary>Un imprévu a été SIGNALÉ (: enfant malade / retard) — trace INFORMATIVE, sans effet sur la
    /// résolution (aucune surcharge, aucun transfert, aucune bascule). Le sous-type est porté par <see cref="TypeImprevu"/>.</summary>
    Imprevu,
}

/// <summary>Nature d'un imprévu SIGNALÉ : purement informatif, jamais actionnable (l'échange couvre le négocié).</summary>
public enum TypeImprevu
{
    /// <summary>L'enfant est malade ce jour-là.</summary>
    Malade,

    /// <summary>Le parent sera en retard ce jour-là.</summary>
    Retard,
}

/// <summary>
/// Événement du JOURNAL DE CHANGEMENTS (cloche) : trace de LECTURE horodatée d'une écriture du planning,
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
    DateTime Horodatage,
    TypeImprevu? Imprevu = null,
    string Motif = "");
