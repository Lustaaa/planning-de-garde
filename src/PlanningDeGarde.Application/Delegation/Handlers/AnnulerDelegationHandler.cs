using System;
using System.Linq;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application.Delegation.Handlers;

/// <summary>
/// Commande task-orientée « finalement je reprends ce jour » (ferme la boucle <i>undo</i>) :
/// ANNULER la délégation de la récupération du jour <paramref name="Jour"/> pour l'enfant
/// <paramref name="EnfantId"/>. La granularité est <b>UNE OCCURRENCE</b> : seul le jour cliqué est repris,
/// même s'il appartient à une plage déléguée. EXPOSE la SUPPRESSION de surcharge EXISTANTE — ce
/// n'est PAS un mécanisme neuf.
/// </summary>
public sealed record AnnulerDelegationCommand(DateOnly Jour, string EnfantId);

/// <summary>Accusé d'une reprise : <paramref name="AvaitDelegation"/> distingue le cas nominal (une
/// délégation active a été reprise) du no-op idempotent (rien à reprendre ce jour-là).</summary>
public sealed record AnnulerDelegationResultat(bool AvaitDelegation);

/// <summary>
/// Use case de COMPOSITION : « reprendre ce jour » COMPOSE la SUPPRESSION de surcharge EXISTANTE — la
/// case retombe sur le FOND (repli surcharge &gt; fond &gt; neutre) et le transfert bicolore dérivé 
/// disparaît par re-dérivation. AUCUN modèle / commande / store neuf, AUCUNE dérivation de transfert neuve.
/// </summary>
public sealed class AnnulerDelegationHandler
{
    private readonly IPeriodeRepository _periodes;
    private readonly IJournalChangements? _journal;
    private readonly IDateTimeProvider? _horloge;

    public AnnulerDelegationHandler(
        IPeriodeRepository periodes, IJournalChangements? journal = null, IDateTimeProvider? horloge = null)
    {
        _periodes = periodes;
        _journal = journal;
        _horloge = horloge;
    }

    public Result<AnnulerDelegationResultat> Handle(AnnulerDelegationCommand commande)
    {
        var jour = commande.Jour;
        // ISOLATION STRICTE s53 (gate G3) : ne reprend QUE les surcharges DE CET ENFANT couvrant le jour —
        // reprendre le jour de Léa ne retire jamais la surcharge de Tom (pas de suppression inter-enfants).
        var couvrantes = _periodes.AllSnapshots()
            .Where(p => p.EnfantId == commande.EnfantId && Couvre(p, jour)).ToList();

        foreach (var surcharge in couvrantes)
        {
            // Trace de LECTURE au journal (cloche s47) : une REPRISE consigne son événement AVANT de supprimer la
            // surcharge — le journal ne dérive PAS de l'état courant (la suppression n'y laisserait aucune trace).
            ConsignerAuJournal(TypeChangement.Reprise, jour, commande.EnfantId, surcharge.ResponsableId, "");
            // Granularité = UNE occurrence : la surcharge couvrant le jour est retirée via le chemin s16,
            // puis les segments RESTANTS (avant / après le jour repris) sont réécrits via le chemin période
            // EXISTANT (s06) — une plage [J1..J3] reprise en J2 laisse [J1..J2-1] et [J2+1..J3] intacts.
            _periodes.Supprimer(surcharge.Id);
            var debut = DateOnly.FromDateTime(surcharge.Debut);
            var fin = DateOnly.FromDateTime(surcharge.Fin);
            // Segments restants RÉÉCRITS en conservant l'EnfantId de la surcharge (s53) : la reprise d'un jour
            // au milieu d'une plage ne « dé-scope » pas les jours restants (ils resteraient sinon en bucket "").
            if (debut < jour)
                ReecrireSegment(surcharge.ResponsableId, debut, jour.AddDays(-1), surcharge.EnfantId);
            if (fin > jour)
                ReecrireSegment(surcharge.ResponsableId, jour.AddDays(1), fin, surcharge.EnfantId);
        }

        return Result<AnnulerDelegationResultat>.Succes(new AnnulerDelegationResultat(couvrantes.Count > 0));
    }

    private void ConsignerAuJournal(TypeChangement type, DateOnly jour, string enfantId, string cedantId, string recevantId)
    {
        if (_journal is null || _horloge is null)
            return;
        _journal.Consigner(new EvenementChangementSnapshot(
            Guid.NewGuid().ToString("N"), type, jour, enfantId, cedantId, recevantId, _horloge.Maintenant));
    }

    private void ReecrireSegment(string responsableId, DateOnly debut, DateOnly fin, string enfantId)
        => _periodes.Enregistrer(PeriodeDeGarde.Affecter(
            responsableId, debut.ToDateTime(TimeOnly.MinValue), fin.ToDateTime(TimeOnly.MinValue), enfantId).Valeur!);

    private static bool Couvre(PeriodeSnapshot periode, DateOnly jour)
        => DateOnly.FromDateTime(periode.Debut) <= jour && DateOnly.FromDateTime(periode.Fin) >= jour;
}
