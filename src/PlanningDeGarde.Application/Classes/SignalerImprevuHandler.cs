using System;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application;

/// <summary>
/// Commande task-orientée « signaler un imprévu » (s48) : un parent SIGNALE un fait subi non-négocié —
/// l'enfant <paramref name="EnfantId"/> est <see cref="TypeImprevu.Malade"/> ou le parent sera
/// <see cref="TypeImprevu.Retard"/> le jour <paramref name="Jour"/>. Purement INFORMATIF : le signalement
/// consigne une trace au journal (cloche s47) mais N'ÉCRIT AUCUNE surcharge et ne touche JAMAIS la
/// résolution (le cas actionnable / négocié est l'échange s47). Le <paramref name="Motif"/> est OPTIONNEL.
/// </summary>
public sealed record SignalerImprevuCommand(
    DateOnly Jour, string EnfantId, TypeImprevu Type, string SignalantId, string Motif = "");

/// <summary>
/// Use case de SIGNALEMENT d'imprévu (s48) : consigne un <see cref="EvenementChangementSnapshot"/> de type
/// <see cref="TypeChangement.Imprevu"/> au JOURNAL DE CHANGEMENTS existant (s47, trace de LECTURE horodatée),
/// SANS aucune écriture de surcharge ni dérivation de transfert — la résolution du planning n'est JAMAIS
/// modifiée (invariant central s48). Le journal n'est jamais lu par la résolution.
/// </summary>
public sealed class SignalerImprevuHandler
{
    private readonly IJournalChangements _journal;
    private readonly IDateTimeProvider _horloge;

    public SignalerImprevuHandler(IJournalChangements journal, IDateTimeProvider horloge)
    {
        _journal = journal;
        _horloge = horloge;
    }

    public Result<EvenementChangementSnapshot> Handle(SignalerImprevuCommand commande)
    {
        var evenement = new EvenementChangementSnapshot(
            Guid.NewGuid().ToString("N"), TypeChangement.Imprevu, commande.Jour, commande.EnfantId,
            CedantId: "", RecevantId: commande.SignalantId, _horloge.Maintenant,
            commande.Type, commande.Motif);
        _journal.Consigner(evenement);
        return Result<EvenementChangementSnapshot>.Succes(evenement);
    }
}
