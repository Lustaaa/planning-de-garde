using System.Collections.Generic;
using System.Linq;

namespace PlanningDeGarde.Application.Foyer.Queries;

/// <summary>
/// Un parent lié restitué dans le graphe foyer : identifiant stable du parent-acteur, son
/// <b>nom d'affichage</b> résolu sur l'identifiant (jamais un libellé en dur) et son <b>rôle-du-lien</b>
/// (père / mère / parent-libre) porté par le lien enfant→parent. Présentation seule — le
/// rôle-du-lien n'intervient ni dans la résolution grille/légende ni dans le gating (miroir).
/// </summary>
public sealed record GrapheParentVue(string ActeurId, string Nom, RoleDuLien Role);

/// <summary>
/// Un enfant restitué comme <b>racine</b> du graphe foyer : identifiant stable, prénom
/// d'affichage, la liste de ses <b>parents liés</b> (branches) et son <b>statut de complétude du
/// couple</b> (<see cref="StatutCouple"/>). Un enfant sans aucun parent lié est une racine
/// isolée légitime (0 parent accepté) — <see cref="Parents"/> vide, statut <c>Vide</c>.
/// </summary>
public sealed record GrapheEnfantVue(string EnfantId, string Prenom, IReadOnlyList<GrapheParentVue> Parents, StatutCoupleR3 StatutCouple);

/// <summary>
/// Query de lecture <b>agrégée</b> du graphe foyer : restitue, PAR enfant en racine, ses parents
/// liés avec leur nom résolu et leur rôle-du-lien. <b>Lecture PURE</b> : compose les
/// données déjà persistées (référentiel enfants + liens + noms d'acteurs) sans aucune
/// mutation, sans nouveau store, sans persistance neuve. Réutilise les ports existants, réalisés par
/// les DEUX adaptateurs (InMemory seedé / Mongo durable). Le graphe reflète les liens RÉELS du store.
/// </summary>
public sealed class GrapheFoyerQuery
{
    private readonly IEnumerationEnfants _enfants;
    private readonly IReferentielResponsables _noms;
    private readonly IEnumerationActeursFoyer _acteurs;

    public GrapheFoyerQuery(IEnumerationEnfants enfants, IReferentielResponsables noms, IEnumerationActeursFoyer acteurs)
    {
        _enfants = enfants;
        _noms = noms;
        _acteurs = acteurs;
    }

    public IReadOnlyList<GrapheEnfantVue> Lire()
    {
        // Contrat d'existence (miroir Resolvable s13, R5/R6) : un parent-acteur SUPPRIMÉ du référentiel
        // (orphelin) mais encore référencé par un lien résiduel ne produit AUCUNE branche fantôme, même si
        // son nom stale reste résoluble. Seules les branches vers des acteurs existants sont restituées ;
        // un enfant qui n'a plus aucun parent résoluble reste une racine (isolée), jamais un nœud fantôme.
        var existants = _acteurs.EnumererActeurs();
        return _enfants.EnumererEnfants()
            .Select(e =>
            {
                var parents = e.ParentsLies
                    .Where(p => existants.Contains(p.ActeurId))
                    .Select(p => new GrapheParentVue(p.ActeurId, _noms.NomDe(p.ActeurId), p.Role))
                    .ToList();
                return new GrapheEnfantVue(e.Id, e.Prenom, parents, StatutDe(e.ParentsLies.Count, parents));
            })
            .ToList();
    }

    /// <summary>Statut de complétude du couple. <paramref name="nbLiensBruts"/> = nombre de liens
    /// RÉELS de l'enfant (avant filtre) ; <paramref name="parents"/> = parents RÉSOLUS (orphelins exclus,
    /// décompte fidèle au store vivant, miroir Resolvable). Règle : aucun lien brut → <c>Vide</c> (racine
    /// isolée légitime, état neutre) — distinct d'un enfant qui A un lien mais dont le seul parent est
    /// orphelin (lien résiduel présent → <c>Incomplet</c>, jamais « vide »). Un lien « père » ET un lien
    /// « mère » RÉSOLUS → <c>Complet</c> ; tout autre cas (0/1 parent résolu, 2 parents sans le couple
    /// père+mère, ex. deux « parent-libre ») → <c>Incomplet</c>. Un lien nu ≡ « parent-libre » (défaut
    /// neutre) et ne satisfait donc pas seul « père ET mère ». Présentation seule — SIGNALÉ, jamais
    /// IMPOSÉ (aucun blocage d'écriture).</summary>
    private static StatutCoupleR3 StatutDe(int nbLiensBruts, IReadOnlyList<GrapheParentVue> parents)
    {
        if (nbLiensBruts == 0)
            return StatutCoupleR3.Vide;
        var aPere = parents.Any(p => p.Role == RoleDuLien.Pere);
        var aMere = parents.Any(p => p.Role == RoleDuLien.Mere);
        return aPere && aMere ? StatutCoupleR3.Complet : StatutCoupleR3.Incomplet;
    }
}
