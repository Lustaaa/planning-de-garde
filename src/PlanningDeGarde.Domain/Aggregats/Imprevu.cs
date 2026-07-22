using System;

namespace PlanningDeGarde.Domain;

/// <summary>
/// Agrégat « imprévu signalé » : un parent SIGNALE un fait subi non-négocié (enfant
/// <see cref="TypeImprevu.Malade"/> / parent <see cref="TypeImprevu.Retard"/>) sur un jour, pour un enfant,
/// avec un motif OPTIONNEL. Purement INFORMATIF : il produit une trace de LECTURE au journal (cloche),
/// JAMAIS une écriture de résolution. Invariant : le <see cref="TypeImprevu"/> doit être connu (malade ou
/// retard) — un type inconnu est REFUSÉ avant toute écriture (Tell-Don't-Ask, la règle vit dans l'agrégat).
/// </summary>
public sealed class Imprevu
{
    private Imprevu(string id, DateOnly jour, string enfantId, TypeImprevu type, string signalantId, string motif)
    {
        Id = id;
        Jour = jour;
        EnfantId = enfantId;
        Type = type;
        SignalantId = signalantId;
        Motif = motif;
    }

    public string Id { get; }
    public DateOnly Jour { get; }
    public string EnfantId { get; }
    public TypeImprevu Type { get; }
    public string SignalantId { get; }
    public string Motif { get; }

    public static Result<Imprevu> Signaler(DateOnly jour, string enfantId, TypeImprevu type, string signalantId, string motif)
    {
        if (!Enum.IsDefined(type))
            return Result<Imprevu>.Echec("Type d'imprévu inconnu : seuls « malade » et « retard » sont acceptés.");

        return Result<Imprevu>.Succes(
            new Imprevu(Guid.NewGuid().ToString("N"), jour, enfantId, type, signalantId, motif ?? ""));
    }

    /// <summary>Produit la trace de LECTURE horodatée pour le journal (cloche). <paramref name="responsableDuJourId"/>
    /// est l'acteur RÉSOLU du jour (autre acteur concerné) — lu sans jamais modifier la résolution.</summary>
    public EvenementChangementSnapshot VersEvenement(string responsableDuJourId, DateTime horodatage)
        => new(Id, TypeChangement.Imprevu, Jour, EnfantId,
            CedantId: responsableDuJourId, RecevantId: SignalantId, horodatage, Type, Motif);
}
