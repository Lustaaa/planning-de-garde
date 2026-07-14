using System;
using System.Linq;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application;

/// <summary>
/// Commande task-orientée « je ne récupère pas ce jour-là, X le fera » (s44) : déléguer la récupération
/// du jour <paramref name="Jour"/> de l'enfant <paramref name="EnfantId"/> à l'acteur
/// <paramref name="VersActeurId"/>. EXPOSE l'écriture « surcharge ponctuelle » EXISTANTE (une période
/// d'UN jour, s06) — ce n'est PAS un mécanisme neuf. Le transfert bicolore qui en résulte reste
/// AUTO-DÉRIVÉ par s31 (R24), jamais réécrit.
/// </summary>
public sealed record DeleguerRecuperationCommand(DateOnly Jour, string EnfantId, string VersActeurId);

/// <summary>
/// Use case de COMPOSITION : « déléguer la récupération d'UN jour » COMPOSE le chemin d'écriture
/// « affecter une période » (surcharge d'UN jour, s06) avec le délégataire comme responsable — miroir
/// de la façon dont <see cref="CarteDuJourQuery"/> / <see cref="AVenirQuery"/> composent
/// <see cref="GrilleAgendaQuery"/> en lecture. Aucun nouveau modèle de résolution (surcharge &gt; fond
/// &gt; neutre inchangée), aucun store neuf, aucune nouvelle dérivation de transfert : le bicolore sort
/// de s31 par construction dès que la surcharge fait basculer la responsabilité du jour.
/// </summary>
public sealed class DeleguerRecuperationHandler
{
    private readonly GrilleAgendaQuery _grille;
    private readonly IPeriodeRepository _periodes;
    private readonly IEnumerationActeursFoyer _acteurs;

    public DeleguerRecuperationHandler(
        GrilleAgendaQuery grille, IPeriodeRepository periodes, IEnumerationActeursFoyer acteurs)
    {
        _grille = grille;
        _periodes = periodes;
        _acteurs = acteurs;
    }

    public Result<PeriodeSnapshot> Handle(DeleguerRecuperationCommand commande)
    {
        // Refus de la délégation à soi-même : le délégataire est DÉJÀ le responsable RÉSOLU du jour
        // (surcharge > fond), la délégation n'apporterait aucun changement utile — aucune écriture.
        var resolu = _grille.Projeter(commande.Jour, VuePlanning.Semaine)
            .Jours.Single(j => j.Date == commande.Jour).ResponsableId;
        if (resolu == commande.VersActeurId)
            return Result<PeriodeSnapshot>.Echec(
                "Délégation à soi-même : cet acteur récupère déjà ce jour-là, aucun changement n'est nécessaire.");

        var debut = commande.Jour.ToDateTime(TimeOnly.MinValue);
        var affectation = PeriodeDeGarde.Affecter(commande.VersActeurId, debut, debut);
        if (!affectation.EstSucces)
            return Result<PeriodeSnapshot>.Echec(affectation.Motif!);

        // Last-write-wins R11 : une surcharge existante couvrant le jour est RÉAFFECTÉE (retirée avant
        // ré-écriture), jamais dupliquée — le jour n'est couvert que par la dernière écriture.
        foreach (var existante in _periodes.AllSnapshots().Where(p => CouvreLeJour(p, commande.Jour)).ToList())
            _periodes.Supprimer(existante.Id);

        var periode = affectation.Valeur!;
        _periodes.Enregistrer(periode);
        return Result<PeriodeSnapshot>.Succes(periode.ToSnapshot());
    }

    private static bool CouvreLeJour(PeriodeSnapshot periode, DateOnly jour)
        => jour >= DateOnly.FromDateTime(periode.Debut) && jour <= DateOnly.FromDateTime(periode.Fin);
}
