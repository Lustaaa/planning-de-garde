using System;
using System.Linq;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application.Slots.Handlers;

/// <summary>Commande d'ajout d'une plage d'exclusion (vacances) [<paramref name="Debut"/>..<paramref name="Fin"/>]
/// à une activité récurrente existante (s54), désignée par son <paramref name="SlotId"/> stable.</summary>
public sealed record AjouterExclusionRecurrentCommand(string SlotId, DateOnly Debut, DateOnly Fin);

/// <summary>Commande de retrait d'une plage d'exclusion d'une activité récurrente (s54).</summary>
public sealed record SupprimerExclusionRecurrentCommand(string SlotId, DateOnly Debut, DateOnly Fin);

/// <summary>
/// Use case : ajouter une plage d'exclusion (vacances) à une série récurrente. Relit le slot par son
/// identifiant stable, y ajoute la plage (invariant idempotent porté par l'agrégat), le réécrit EN PLACE
/// (même identifiant) puis diffuse la mise à jour temps réel — la grille cesse de projeter l'activité sur
/// l'intervalle sans rechargement.
/// </summary>
public sealed class AjouterExclusionRecurrentHandler
{
    private readonly ISlotRecurrentRepository _slots;
    private readonly INotificateurPlanning _notificateur;

    public AjouterExclusionRecurrentHandler(ISlotRecurrentRepository slots, INotificateurPlanning notificateur)
    {
        _slots = slots;
        _notificateur = notificateur;
    }

    public Result<SlotRecurrentSnapshot> Handle(AjouterExclusionRecurrentCommand commande)
    {
        var existant = _slots.AllSnapshots().FirstOrDefault(s => s.Id == commande.SlotId);
        if (existant is null)
            return Result<SlotRecurrentSnapshot>.Echec("Le slot récurrent à modifier n'existe pas.");

        var modifie = SlotRecurrent.FromSnapshot(existant).AjouterExclusion(commande.Debut, commande.Fin);
        _slots.Remplacer(commande.SlotId, modifie);
        _notificateur.NotifierMiseAJour();
        return Result<SlotRecurrentSnapshot>.Succes(modifie.ToSnapshot() with { Id = commande.SlotId });
    }
}

/// <summary>Use case : retirer une plage d'exclusion d'une série récurrente (s54). Miroir idempotent de
/// l'ajout : l'activité reprend ses occurrences sur l'intervalle réintégré.</summary>
public sealed class SupprimerExclusionRecurrentHandler
{
    private readonly ISlotRecurrentRepository _slots;
    private readonly INotificateurPlanning _notificateur;

    public SupprimerExclusionRecurrentHandler(ISlotRecurrentRepository slots, INotificateurPlanning notificateur)
    {
        _slots = slots;
        _notificateur = notificateur;
    }

    public Result<SlotRecurrentSnapshot> Handle(SupprimerExclusionRecurrentCommand commande)
    {
        var existant = _slots.AllSnapshots().FirstOrDefault(s => s.Id == commande.SlotId);
        if (existant is null)
            return Result<SlotRecurrentSnapshot>.Echec("Le slot récurrent à modifier n'existe pas.");

        var modifie = SlotRecurrent.FromSnapshot(existant).RetirerExclusion(commande.Debut, commande.Fin);
        _slots.Remplacer(commande.SlotId, modifie);
        _notificateur.NotifierMiseAJour();
        return Result<SlotRecurrentSnapshot>.Succes(modifie.ToSnapshot() with { Id = commande.SlotId });
    }
}
