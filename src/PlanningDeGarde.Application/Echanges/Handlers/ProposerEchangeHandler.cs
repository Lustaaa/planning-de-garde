using System;
using System.Linq;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application.Echanges.Handlers;

/// <summary>
/// Commande task-orientée « je ne peux pas récupérer ce jour, échange-le avec moi » : PROPOSER
/// l'échange du jour <paramref name="Jour"/> (enfant <paramref name="EnfantId"/>) vers l'acteur tiers
/// <paramref name="VersActeurId"/>. Contrairement à la délégation (unilatérale, effet immédiat), la
/// proposition n'écrit RIEN et ne change pas la résolution : seul le consentement du recevant (Accepter)
/// déclenchera l'écriture.
/// </summary>
public sealed record ProposerEchangeCommand(
    DateOnly Jour, string EnfantId, string VersActeurId, DateOnly? JourFin = null);

/// <summary>
/// Use case de PROPOSITION : enregistre une <see cref="PropositionEchange"/> <c>pending</c> notifiée au
/// recevant, SANS aucune écriture de surcharge (le store des surcharges reste intact, la résolution de la
/// case est inchangée). Refuse AVANT écriture un recevant inconnu/orphelin ou une proposition à soi-même.
/// Last-write-wins : une seule proposition pending subsiste par jour/enfant, sans doublon.
/// </summary>
public sealed class ProposerEchangeHandler
{
    private readonly GrilleAgendaQuery _grille;
    private readonly IPropositionEchangeRepository _propositions;
    private readonly IEnumerationActeursFoyer _acteurs;

    public ProposerEchangeHandler(
        GrilleAgendaQuery grille, IPropositionEchangeRepository propositions, IEnumerationActeursFoyer acteurs)
    {
        _grille = grille;
        _propositions = propositions;
        _acteurs = acteurs;
    }

    public Result<PropositionEchangeSnapshot> Handle(ProposerEchangeCommand commande)
    {
        // Refus AVANT toute écriture d'un recevant INCONNU / ORPHELIN (id stable absent du store) : jamais
        // de proposition pointant un acteur qui n'existe pas (miroir de la garde s44, vert-qui-ment évité).
        if (!_acteurs.EnumererActeurs().Contains(commande.VersActeurId))
            return Result<PropositionEchangeSnapshot>.Echec(
                "Recevant inconnu : cet acteur n'existe pas (ou plus) dans le foyer.");

        // Cédant = responsable RÉSOLU du jour (surcharge > fond) DE L'ENFANT ciblé (s53 : jamais pollué par la
        // surcharge d'un autre enfant) — LU sans le modifier. Proposer à soi-même (recevant = résolu) est refusé
        // par l'agrégat, AVANT toute écriture.
        var cedant = _grille.Projeter(commande.Jour, VuePlanning.Semaine, commande.EnfantId)
            .Jours.Single(j => j.Date == commande.Jour).ResponsableId ?? "";
        var proposition = PropositionEchange.Proposer(
            commande.Jour, commande.EnfantId, cedant, commande.VersActeurId, commande.JourFin);
        if (!proposition.EstSucces)
            return Result<PropositionEchangeSnapshot>.Echec(proposition.Motif!);

        // Last-write-wins R11 : toute proposition PENDING existante sur le même jour/enfant est retirée avant
        // enregistrement — une seule proposition pending subsiste, sans doublon. AUCUNE surcharge n'est écrite.
        foreach (var existante in _propositions.AllSnapshots()
                     .Where(p => p.Jour == commande.Jour && p.EnfantId == commande.EnfantId && p.Statut == StatutProposition.Proposee)
                     .ToList())
            _propositions.Supprimer(existante.Id);

        _propositions.Sauvegarder(proposition.Valeur!);
        return Result<PropositionEchangeSnapshot>.Succes(proposition.Valeur!.ToSnapshot());
    }
}
