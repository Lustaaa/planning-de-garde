using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application.Echanges.Handlers;

/// <summary>Commande « j'accepte l'échange qu'on m'a proposé » : le recevant CONSENT à la proposition
/// <paramref name="PropositionId"/> — c'est ce consentement qui déclenche l'écriture (composition).</summary>
public sealed record AccepterPropositionCommand(string PropositionId);

/// <summary>
/// Use case de CONSENTEMENT : ACCEPTER une proposition <c>pending</c> COMPOSE la délégation EXISTANTE 
/// (<see cref="DeleguerRecuperationHandler"/>) sur la PLAGE <c>[Jour.JourFin]</c> portée par la proposition —
/// surcharge multi-jours écrite via le chemin, le recevant prime (surcharge &gt; fond), les transferts
/// bicolores cédant → recevant sortent AUTO-DÉRIVÉS de aux deux frontières de la plage. La
/// proposition passe alors à <see cref="StatutProposition.Acceptee"/>. Aucune nouvelle écriture ni dérivation :
/// l'accord n'est que le déclencheur de la délégation déjà éprouvée.
/// </summary>
public sealed class AccepterPropositionHandler
{
    private readonly IPropositionEchangeRepository _propositions;
    private readonly DeleguerRecuperationHandler _delegation;

    public AccepterPropositionHandler(IPropositionEchangeRepository propositions, DeleguerRecuperationHandler delegation)
    {
        _propositions = propositions;
        _delegation = delegation;
    }

    public Result<PropositionEchangeSnapshot> Handle(AccepterPropositionCommand commande)
    {
        var snapshot = _propositions.ParId(commande.PropositionId);
        if (snapshot is null)
            return Result<PropositionEchangeSnapshot>.Echec("Proposition introuvable : cet échange n'existe pas (ou plus).");

        // COMPOSITION s44 : le consentement déclenche l'écriture de la délégation EXISTANTE (surcharge + transfert
        // dérivé s31). Si la délégation échoue (ex. recevant devenu orphelin), on ne change PAS le statut — refus
        // ATOMIQUE, aucune écriture partielle.
        var delegation = _delegation.Handle(
            new DeleguerRecuperationCommand(snapshot.Jour, snapshot.EnfantId, snapshot.VersActeurId, snapshot.JourFin));
        if (!delegation.EstSucces)
            return Result<PropositionEchangeSnapshot>.Echec(delegation.Motif!);

        var proposition = PropositionEchange.FromSnapshot(snapshot);
        proposition.Accepter();
        _propositions.Sauvegarder(proposition);
        return Result<PropositionEchangeSnapshot>.Succes(proposition.ToSnapshot());
    }
}
