using System;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application.Transferts.Handlers;

/// <summary>Commande de définition d'un transfert de bascule entre deux parents. <paramref name="EnfantId"/>
/// SCOPE le transfert à l'enfant courant (hérité du sélecteur de vue, Option A).</summary>
public sealed record DefinirTransfertCommand(string DeposeParId, string RecupereParId, string LieuId, TimeSpan Heure, DateTime Date, string EnfantId = "");

/// <summary>Use case : définir un transfert de bascule (point A↔B) dans le planning partagé.</summary>
public sealed class DefinirTransfertHandler
{
    private readonly ITransfertRepository _transferts;
    private readonly IJournalChangements? _journal;
    private readonly IDateTimeProvider? _horloge;

    public DefinirTransfertHandler(
        ITransfertRepository transferts, IJournalChangements? journal = null, IDateTimeProvider? horloge = null)
    {
        _transferts = transferts;
        _journal = journal;
        _horloge = horloge;
    }

    public Result<TransfertSnapshot> Handle(DefinirTransfertCommand commande)
    {
        var definition = Transfert.Definir(commande.DeposeParId, commande.RecupereParId, commande.LieuId, commande.Heure, commande.Date, commande.EnfantId);
        if (!definition.EstSucces)
            return Result<TransfertSnapshot>.Echec(definition.Motif!);

        var transfert = definition.Valeur!;
        _transferts.Enregistrer(transfert);

        // Trace de LECTURE au journal (cloche s47) : un transfert saisi consigne un événement horodaté.
        if (_journal is not null && _horloge is not null)
            _journal.Consigner(new EvenementChangementSnapshot(
                Guid.NewGuid().ToString("N"), TypeChangement.Transfert, DateOnly.FromDateTime(commande.Date),
                commande.EnfantId, commande.DeposeParId, commande.RecupereParId, _horloge.Maintenant));

        return Result<TransfertSnapshot>.Succes(transfert.ToSnapshot());
    }
}
