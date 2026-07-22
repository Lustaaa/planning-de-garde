using System.Linq;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application.Activites.Handlers;

/// <summary>
/// Commande de liaison d'un enfant du foyer (référentiel) à une <b>activité</b> du référentiel.
/// Lien <b>N-M</b> (plusieurs enfants partagent une activité ; un enfant porte plusieurs
/// activités) — enrichissement du modèle activité (jamais une recréation : l'id de l'activité reste la
/// clé). Le handler persiste le lien via <see cref="IEditeurActivites.LierEnfant"/>, relu ensuite par
/// la query de configuration dans <see cref="ActiviteFoyer.EnfantsLies"/>.
/// </summary>
public sealed record LierEnfantActiviteCommand(string EnfantId, string ActiviteId);

/// <summary>Confirmation d'un lien abouti : l'enfant et l'activité liés (ids stables inchangés).</summary>
public sealed record LierEnfantActiviteResultat(string EnfantId, string ActiviteId);

/// <summary>
/// Use case : lier un enfant à une activité (lien N-M, miroir du lien enfant↔parent).
/// Existence de l'enfant ET de l'activité vérifiées AVANT toute écriture (aucune écriture partielle en
/// cas de refus) ; lier un enfant déjà lié est neutre (idempotent, pas de doublon). Aucune borne de
/// cardinalité (0.N des deux côtés).
/// </summary>
public sealed class LierEnfantActiviteHandler
{
    private readonly IEnumerationActivites _activites;
    private readonly IEnumerationEnfants _enfants;
    private readonly IEditeurActivites _referentiel;

    public LierEnfantActiviteHandler(IEnumerationActivites activites, IEnumerationEnfants enfants, IEditeurActivites referentiel)
    {
        _activites = activites;
        _enfants = enfants;
        _referentiel = referentiel;
    }

    public Result<LierEnfantActiviteResultat> Handle(LierEnfantActiviteCommand commande)
    {
        // Règle 1 — l'activité désignée doit EXISTER dans le référentiel (jamais un lien fantôme) : refus
        // AVANT toute écriture (les liens existants restent intacts, aucune écriture partielle).
        if (_activites.EnumererActivites().All(a => a.Id != commande.ActiviteId))
            return Result<LierEnfantActiviteResultat>.Echec("activité inexistante");

        // Règle 2 — l'enfant désigné doit EXISTER dans le référentiel d'enfants (s30), même exigence.
        if (_enfants.EnumererEnfants().All(e => e.Id != commande.EnfantId))
            return Result<LierEnfantActiviteResultat>.Echec("enfant inexistant");

        // Existence vérifiée : on persiste le lien (le store ajoute sans doublon — déjà lié = no-op neutre).
        _referentiel.LierEnfant(commande.ActiviteId, commande.EnfantId);
        return Result<LierEnfantActiviteResultat>.Succes(new LierEnfantActiviteResultat(commande.EnfantId, commande.ActiviteId));
    }
}
