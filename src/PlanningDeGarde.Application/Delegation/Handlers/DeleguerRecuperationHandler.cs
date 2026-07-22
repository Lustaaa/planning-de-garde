using System;
using System.Linq;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application.Delegation.Handlers;

/// <summary>
/// Commande task-orientée « je ne récupère pas ces jours-là, X le fera » (s44 → s45) : déléguer la
/// récupération de la PLAGE <c>[<paramref name="Jour"/>..<paramref name="JourFin"/>]</c> de l'enfant
/// <paramref name="EnfantId"/> à l'acteur <paramref name="VersActeurId"/>. <paramref name="JourFin"/> est
/// la date de fin (INCLUSE) ; <b>absente (null) = plage réduite à UN jour</b> (<c>fin = début</c>) → parité
/// STRICTE avec la délégation d'un jour s44. EXPOSE l'écriture « surcharge » EXISTANTE (une période
/// <c>[début..fin]</c>, s06) — ce n'est PAS un mécanisme neuf. Les transferts bicolores aux frontières de
/// la plage restent AUTO-DÉRIVÉS par s31 (R24), jamais réécrits.
/// </summary>
public sealed record DeleguerRecuperationCommand(
    DateOnly Jour, string EnfantId, string VersActeurId, DateOnly? JourFin = null);

/// <summary>
/// Use case de COMPOSITION : « déléguer la récupération d'UN jour » COMPOSE le chemin d'écriture
/// « affecter une période » (surcharge d'UN jour, s06) avec le délégataire comme responsable — il
/// COMPOSE la résolution existante portée par <see cref="GrilleAgendaQuery"/> en lecture, sans la
/// réimplémenter. Aucun nouveau modèle de résolution (surcharge &gt; fond
/// &gt; neutre inchangée), aucun store neuf, aucune nouvelle dérivation de transfert : le bicolore sort
/// de s31 par construction dès que la surcharge fait basculer la responsabilité du jour.
/// </summary>
public sealed class DeleguerRecuperationHandler
{
    private readonly GrilleAgendaQuery _grille;
    private readonly IPeriodeRepository _periodes;
    private readonly IEnumerationActeursFoyer _acteurs;
    private readonly IJournalChangements? _journal;
    private readonly IDateTimeProvider? _horloge;

    public DeleguerRecuperationHandler(
        GrilleAgendaQuery grille, IPeriodeRepository periodes, IEnumerationActeursFoyer acteurs,
        IJournalChangements? journal = null, IDateTimeProvider? horloge = null)
    {
        _grille = grille;
        _periodes = periodes;
        _acteurs = acteurs;
        _journal = journal;
        _horloge = horloge;
    }

    public Result<PeriodeSnapshot> Handle(DeleguerRecuperationCommand commande)
    {
        // Refus AVANT toute écriture d'un délégataire INCONNU / ORPHELIN (id stable absent du store) :
        // jamais de surcharge pointant un acteur qui n'existe pas (repli neutre / vert-qui-ment évités).
        if (!_acteurs.EnumererActeurs().Contains(commande.VersActeurId))
            return Result<PeriodeSnapshot>.Echec(
                "Délégataire inconnu : cet acteur n'existe pas (ou plus) dans le foyer.");

        // Fin ABSENTE = plage réduite à UN jour (parité s44). La borne fin < début (plage vide) est
        // rejetée AVANT écriture par l'agrégat (bornes invalides) — refus ATOMIQUE, aucun jour écrit.
        var debutJour = commande.Jour;
        var finJour = commande.JourFin ?? commande.Jour;
        // ISOLATION s53 : la surcharge est SCOPÉE à l'enfant délégué — elle n'entre que dans SA résolution,
        // jamais dans celle d'un autre enfant.
        var affectation = PeriodeDeGarde.Affecter(
            commande.VersActeurId, debutJour.ToDateTime(TimeOnly.MinValue), finJour.ToDateTime(TimeOnly.MinValue), commande.EnfantId);
        if (!affectation.EstSucces)
            return Result<PeriodeSnapshot>.Echec(affectation.Motif!);

        // Refus de la délégation à soi-même : le délégataire est DÉJÀ le responsable RÉSOLU de CHAQUE jour
        // de la plage (surcharge > fond) — la délégation n'apporterait aucun changement utile, aucune écriture.
        if (TouteLaPlageResoutDeja(debutJour, finJour, commande.VersActeurId, commande.EnfantId))
            return Result<PeriodeSnapshot>.Echec(
                "Délégation à soi-même : cet acteur récupère déjà ces jours-là, aucun changement n'est nécessaire.");

        // Last-write-wins R11 : toute surcharge existante DE CET ENFANT chevauchant la plage est RÉAFFECTÉE
        // (retirée avant ré-écriture), jamais dupliquée. SCOPÉE STRICTEMENT à l'enfant (s53, gate G3) : la
        // surcharge d'un AUTRE enfant sur le même jour COEXISTE (pas de last-write-wins ENTRE enfants) ; les
        // écritures estampillent désormais toujours l'enfant (aucune période "" partagée créée).
        foreach (var existante in _periodes.AllSnapshots()
            .Where(p => p.EnfantId == commande.EnfantId && Chevauche(p, debutJour, finJour)).ToList())
            _periodes.Supprimer(existante.Id);

        // Cédant = responsable RÉSOLU du jour AVANT écriture (celui qui devait récupérer) — lu pour la trace,
        // sur la résolution DE CET ENFANT.
        var cedant = _grille.Projeter(debutJour, VuePlanning.Semaine, commande.EnfantId)
            .Jours.Single(j => j.Date == debutJour).ResponsableId ?? "";

        var periode = affectation.Valeur!;
        _periodes.Enregistrer(periode);

        // Trace de LECTURE au journal (cloche s47) : une délégation consigne un événement horodaté. Ne change
        // JAMAIS la résolution (la vérité reste les périodes). Optionnel : absent (null) = comportement antérieur.
        ConsignerAuJournal(TypeChangement.Delegation, debutJour, commande.EnfantId, cedant, commande.VersActeurId);

        return Result<PeriodeSnapshot>.Succes(periode.ToSnapshot());
    }

    private void ConsignerAuJournal(TypeChangement type, DateOnly jour, string enfantId, string cedantId, string recevantId)
    {
        if (_journal is null || _horloge is null)
            return;
        _journal.Consigner(new EvenementChangementSnapshot(
            Guid.NewGuid().ToString("N"), type, jour, enfantId, cedantId, recevantId, _horloge.Maintenant));
    }

    private bool TouteLaPlageResoutDeja(DateOnly debut, DateOnly fin, string acteurId, string enfantId)
    {
        for (var jour = debut; jour <= fin; jour = jour.AddDays(1))
        {
            var resolu = _grille.Projeter(jour, VuePlanning.Semaine, enfantId)
                .Jours.Single(j => j.Date == jour).ResponsableId;
            if (resolu != acteurId)
                return false;
        }
        return true;
    }

    private static bool Chevauche(PeriodeSnapshot periode, DateOnly debut, DateOnly fin)
        => DateOnly.FromDateTime(periode.Debut) <= fin && DateOnly.FromDateTime(periode.Fin) >= debut;
}
