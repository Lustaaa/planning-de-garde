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
    private readonly GrilleAgendaQuery? _grille;

    public SignalerImprevuHandler(IJournalChangements journal, IDateTimeProvider horloge, GrilleAgendaQuery? grille = null)
    {
        _journal = journal;
        _horloge = horloge;
        _grille = grille;
    }

    public Result<EvenementChangementSnapshot> Handle(SignalerImprevuCommand commande)
    {
        // Règle métier dans l'agrégat (Tell-Don't-Ask) : un type d'imprévu inconnu est REFUSÉ AVANT écriture.
        var imprevu = Imprevu.Signaler(commande.Jour, commande.EnfantId, commande.Type, commande.SignalantId, commande.Motif);
        if (!imprevu.EstSucces)
            return Result<EvenementChangementSnapshot>.Echec(imprevu.Motif!);

        // Acteur CONCERNÉ (au-delà du signalant) = responsable RÉSOLU du jour (celui qui a l'enfant ce jour-là).
        // Il est LU sans être modifié — la résolution n'est jamais altérée par le signalement (invariant s48).
        var responsableDuJour = _grille?.Projeter(commande.Jour, VuePlanning.Semaine, commande.EnfantId)
            .Jours.Single(j => j.Date == commande.Jour).ResponsableId ?? "";

        var evenement = imprevu.Valeur!.VersEvenement(responsableDuJour, _horloge.Maintenant);
        _journal.Consigner(evenement);
        return Result<EvenementChangementSnapshot>.Succes(evenement);
    }
}
