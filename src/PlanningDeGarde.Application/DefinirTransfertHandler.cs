using System;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application;

/// <summary>Commande de définition d'un transfert de bascule entre deux parents.</summary>
public sealed record DefinirTransfertCommand(string DeposeParId, string RecupereParId, string LieuId, TimeSpan Heure, DateTime Date);

/// <summary>Use case : définir un transfert de bascule (point A↔B) dans le planning partagé.</summary>
public sealed class DefinirTransfertHandler
{
    private readonly ITransfertRepository _transferts;

    public DefinirTransfertHandler(ITransfertRepository transferts) => _transferts = transferts;

    public Result<TransfertSnapshot> Handle(DefinirTransfertCommand commande)
    {
        var transfert = Transfert.Definir(commande.DeposeParId, commande.RecupereParId, commande.LieuId, commande.Heure, commande.Date);
        _transferts.Enregistrer(transfert);
        return Result<TransfertSnapshot>.Succes(transfert.ToSnapshot());
    }
}
