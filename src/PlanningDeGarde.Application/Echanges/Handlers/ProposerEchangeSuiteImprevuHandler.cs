using System.Linq;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application.Echanges.Handlers;

/// <summary>Commande « action de suivi sur un imprévu : proposer un échange » (s51) : EN RÉACTION à un imprévu
/// déjà consigné au journal (<paramref name="ImprevuEvenementId"/>, s48), un parent PROPOSE un échange vers
/// <paramref name="VersActeurId"/>. Le jour + l'enfant sont HÉRITÉS de l'imprévu (le proposant ne choisit que le
/// recevant) — l'imprévu lui-même n'est jamais muté (il reste un FAIT informatif au journal).</summary>
public sealed record ProposerEchangeSuiteImprevuCommand(string ImprevuEvenementId, string VersActeurId);

/// <summary>
/// Use case de COMPOSITION (s51) : greffe <see cref="ProposerEchangeHandler"/> (s47) EN RÉACTION à un imprévu
/// journalisé (s48). Lit l'imprévu au JOURNAL (trace de LECTURE, jamais mutée) pour en hériter le jour + l'enfant,
/// puis délègue à la proposition d'échange s47 — une <see cref="PropositionEchange"/> <c>pending</c> pré-remplie,
/// SANS aucune écriture de surcharge. AUCUN modèle/store neuf : réemploi intégral du journal s48 et de la
/// proposition s47. GARDE DE DISTINCTION : l'imprévu (fait) et la proposition (échange) restent SÉPARÉS — la
/// composition ne « résout » pas l'imprévu, elle crée un SECOND événement distinct (la proposition).
/// </summary>
public sealed class ProposerEchangeSuiteImprevuHandler
{
    private readonly IJournalChangements _journal;
    private readonly ProposerEchangeHandler _proposer;

    public ProposerEchangeSuiteImprevuHandler(IJournalChangements journal, ProposerEchangeHandler proposer)
    {
        _journal = journal;
        _proposer = proposer;
    }

    public Result<PropositionEchangeSnapshot> Handle(ProposerEchangeSuiteImprevuCommand commande)
    {
        // L'imprévu est LU au journal (jour + enfant hérités), jamais modifié — il reste un fait informatif.
        var imprevu = _journal.Tout().First(e => e.Id == commande.ImprevuEvenementId && e.Type == TypeChangement.Imprevu);

        // Délègue à la proposition s47 : elle porte déjà toutes les gardes (soi-même / recevant inconnu / orphelin
        // refusés AVANT écriture, last-write-wins R11) et n'écrit AUCUNE surcharge (canal de consentement).
        return _proposer.Handle(new ProposerEchangeCommand(imprevu.Jour, imprevu.EnfantId, commande.VersActeurId));
    }
}
