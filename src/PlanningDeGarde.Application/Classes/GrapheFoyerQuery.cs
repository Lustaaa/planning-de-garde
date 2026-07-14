using System.Collections.Generic;
using System.Linq;

namespace PlanningDeGarde.Application;

/// <summary>
/// Un parent lié restitué dans le graphe foyer (s38) : identifiant stable du parent-acteur, son
/// <b>nom d'affichage</b> résolu sur l'identifiant (jamais un libellé en dur) et son <b>rôle-du-lien</b>
/// (père / mère / parent-libre, s37) porté par le lien enfant→parent. Présentation seule — le
/// rôle-du-lien n'intervient ni dans la résolution grille/légende ni dans le gating (miroir R10).
/// </summary>
public sealed record GrapheParentVue(string ActeurId, string Nom, RoleDuLien Role);

/// <summary>
/// Un enfant restitué comme <b>racine</b> du graphe foyer (s38) : identifiant stable, prénom
/// d'affichage et la liste de ses <b>parents liés</b> (branches). Un enfant sans aucun parent lié
/// est une racine isolée légitime (0 parent accepté, s34) — <see cref="Parents"/> vide.
/// </summary>
public sealed record GrapheEnfantVue(string EnfantId, string Prenom, IReadOnlyList<GrapheParentVue> Parents);

/// <summary>
/// Query de lecture <b>agrégée</b> du graphe foyer (s38) : restitue, PAR enfant en racine, ses parents
/// liés (s34) avec leur nom résolu (s5) et leur rôle-du-lien (s37). <b>Lecture PURE</b> : compose les
/// données déjà persistées (référentiel enfants s30 + liens s34/s37 + noms d'acteurs) sans aucune
/// mutation, sans nouveau store, sans persistance neuve. Réutilise les ports existants, réalisés par
/// les DEUX adaptateurs (InMemory seedé / Mongo durable). Le graphe reflète les liens RÉELS du store.
/// </summary>
public sealed class GrapheFoyerQuery
{
    private readonly IEnumerationEnfants _enfants;
    private readonly IReferentielResponsables _noms;

    public GrapheFoyerQuery(IEnumerationEnfants enfants, IReferentielResponsables noms)
    {
        _enfants = enfants;
        _noms = noms;
    }

    public IReadOnlyList<GrapheEnfantVue> Lire()
        => _enfants.EnumererEnfants()
            .Select(e => new GrapheEnfantVue(
                e.Id,
                e.Prenom,
                e.ParentsLies
                    .Select(p => new GrapheParentVue(p.ActeurId, _noms.NomDe(p.ActeurId), p.Role))
                    .ToList()))
            .ToList();
}
