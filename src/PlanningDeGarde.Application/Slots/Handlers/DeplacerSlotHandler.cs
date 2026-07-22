using System;
using System.Linq;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application.Slots.Handlers;

/// <summary>Rôle d'accès de l'auteur d'une action sur le planning du foyer.</summary>
public enum RoleAuteur
{
    Parent,
    Invite
}

/// <summary>
/// Commande de déplacement d'un slot existant (identifié par enfant + début) vers un
/// nouveau lieu. Porte le rôle de l'auteur, gardé à l'entrée de l'Application.
/// </summary>
public sealed record DeplacerSlotCommand(RoleAuteur Auteur, string EnfantId, DateTime Debut, string NouveauLieuId);

/// <summary>Use case : déplacer un slot de localisation. Le droit d'écriture est gardé ici.</summary>
public sealed class DeplacerSlotHandler
{
    private readonly ISlotRepository _slots;

    public DeplacerSlotHandler(ISlotRepository slots) => _slots = slots;

    public Result<SlotSnapshot> Handle(DeplacerSlotCommand commande)
    {
        if (commande.Auteur != RoleAuteur.Parent)
            return Result<SlotSnapshot>.Echec("Action refusée : l'auteur est en consultation seule.");

        var ancien = _slots.AllSnapshots()
            .FirstOrDefault(s => s.EnfantId == commande.EnfantId && s.Debut == commande.Debut);
        if (ancien is null)
            return Result<SlotSnapshot>.Echec("Slot introuvable.");

        var pose = SlotDeLocalisation.Poser(commande.EnfantId, commande.NouveauLieuId, ancien.Debut, ancien.Fin);
        if (!pose.EstSucces)
            return Result<SlotSnapshot>.Echec(pose.Motif!);

        var deplace = pose.Valeur!;
        _slots.Remplacer(ancien, deplace);
        return Result<SlotSnapshot>.Succes(deplace.ToSnapshot());
    }
}
