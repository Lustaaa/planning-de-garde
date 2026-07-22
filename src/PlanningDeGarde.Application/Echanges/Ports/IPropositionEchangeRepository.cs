using System.Collections.Generic;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application.Echanges.Ports;

/// <summary>
/// Port de persistance des propositions d'échange consenti (s47). Une proposition <c>pending</c> n'écrit
/// AUCUNE surcharge : c'est un canal de consentement, jamais une source de vérité de la résolution (la
/// vérité reste les périodes/transferts). <see cref="Sauvegarder"/> est un UPSERT par identifiant stable
/// (transition de statut proposé → accepté / refusé). <see cref="Supprimer"/> est idempotent.
/// </summary>
public interface IPropositionEchangeRepository
{
    void Sauvegarder(PropositionEchange proposition);
    IReadOnlyList<PropositionEchangeSnapshot> AllSnapshots();
    PropositionEchangeSnapshot? ParId(string id);
    void Supprimer(string id);
}
