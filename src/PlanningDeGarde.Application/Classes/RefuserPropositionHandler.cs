using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application;

/// <summary>Commande « je décline l'échange qu'on m'a proposé » (s47) : le recevant REFUSE la proposition
/// <paramref name="PropositionId"/> — elle se clôt sans aucune écriture.</summary>
public sealed record RefuserPropositionCommand(string PropositionId);

/// <summary>
/// Use case de REFUS : REFUSER une proposition <c>pending</c> la fait passer à
/// <see cref="StatutProposition.Refusee"/>, AUCUNE surcharge n'est écrite, le store des surcharges reste
/// intact (la résolution de la case est inchangée). Le consentement n'ayant pas été donné, rien n'est composé.
/// </summary>
public sealed class RefuserPropositionHandler
{
    private readonly IPropositionEchangeRepository _propositions;

    public RefuserPropositionHandler(IPropositionEchangeRepository propositions) => _propositions = propositions;

    public Result<PropositionEchangeSnapshot> Handle(RefuserPropositionCommand commande)
    {
        var snapshot = _propositions.ParId(commande.PropositionId);
        if (snapshot is null)
            return Result<PropositionEchangeSnapshot>.Echec("Proposition introuvable : cet échange n'existe pas (ou plus).");

        var proposition = PropositionEchange.FromSnapshot(snapshot);
        proposition.Refuser(); // aucune écriture de surcharge : le store reste intact
        _propositions.Sauvegarder(proposition);
        return Result<PropositionEchangeSnapshot>.Succes(proposition.ToSnapshot());
    }
}
