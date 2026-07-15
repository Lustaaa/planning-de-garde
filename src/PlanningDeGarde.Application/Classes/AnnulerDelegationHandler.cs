using System;
using System.Linq;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application;

/// <summary>
/// Commande task-orientée « finalement je reprends ce jour » (s46, ferme la boucle <i>undo</i> de s44/s45) :
/// ANNULER la délégation de la récupération du jour <paramref name="Jour"/> pour l'enfant
/// <paramref name="EnfantId"/>. La granularité est <b>UNE OCCURRENCE</b> : seul le jour cliqué est repris,
/// même s'il appartient à une plage déléguée (s45). EXPOSE la SUPPRESSION de surcharge EXISTANTE (s16) — ce
/// n'est PAS un mécanisme neuf.
/// </summary>
public sealed record AnnulerDelegationCommand(DateOnly Jour, string EnfantId);

/// <summary>Accusé d'une reprise : <paramref name="AvaitDelegation"/> distingue le cas nominal (une
/// délégation active a été reprise) du no-op idempotent (rien à reprendre ce jour-là).</summary>
public sealed record AnnulerDelegationResultat(bool AvaitDelegation);

/// <summary>
/// Use case de COMPOSITION : « reprendre ce jour » COMPOSE la SUPPRESSION de surcharge EXISTANTE (s16) — la
/// case retombe sur le FOND (repli surcharge &gt; fond &gt; neutre) et le transfert bicolore dérivé s31
/// disparaît par re-dérivation. AUCUN modèle / commande / store neuf, AUCUNE dérivation de transfert neuve.
/// </summary>
public sealed class AnnulerDelegationHandler
{
    private readonly IPeriodeRepository _periodes;

    public AnnulerDelegationHandler(IPeriodeRepository periodes) => _periodes = periodes;

    public Result<AnnulerDelegationResultat> Handle(AnnulerDelegationCommand commande)
    {
        var couvrantes = _periodes.AllSnapshots().Where(p => Couvre(p, commande.Jour)).ToList();

        foreach (var surcharge in couvrantes)
            _periodes.Supprimer(surcharge.Id);

        return Result<AnnulerDelegationResultat>.Succes(new AnnulerDelegationResultat(couvrantes.Count > 0));
    }

    private static bool Couvre(PeriodeSnapshot periode, DateOnly jour)
        => DateOnly.FromDateTime(periode.Debut) <= jour && DateOnly.FromDateTime(periode.Fin) >= jour;
}
