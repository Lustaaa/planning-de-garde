using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application;

/// <summary>Commande « j'accepte l'échange qu'on m'a proposé » (s47) : le recevant CONSENT à la proposition
/// <paramref name="PropositionId"/> — c'est ce consentement qui déclenche l'écriture (composition s44).</summary>
public sealed record AccepterPropositionCommand(string PropositionId);

/// <summary>
/// Use case de CONSENTEMENT : ACCEPTER une proposition <c>pending</c> COMPOSE la délégation EXISTANTE s44
/// (<see cref="DeleguerRecuperationHandler"/>) — surcharge du jour écrite via le chemin s06, le recevant
/// prime (surcharge &gt; fond), le transfert bicolore cédant → recevant sort AUTO-DÉRIVÉ de s31 (R24). La
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
        var delegation = _delegation.Handle(new DeleguerRecuperationCommand(snapshot.Jour, snapshot.EnfantId, snapshot.VersActeurId));
        if (!delegation.EstSucces)
            return Result<PropositionEchangeSnapshot>.Echec(delegation.Motif!);

        var proposition = PropositionEchange.FromSnapshot(snapshot);
        proposition.Accepter();
        _propositions.Sauvegarder(proposition);
        return Result<PropositionEchangeSnapshot>.Succes(proposition.ToSnapshot());
    }
}
