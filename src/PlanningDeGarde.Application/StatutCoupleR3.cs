namespace PlanningDeGarde.Application;

/// <summary>
/// Statut de complétude du couple d'un enfant (R3, s40) — <b>SIGNALÉ, jamais IMPOSÉ</b> : composé en
/// LECTURE PURE depuis les données déjà persistées (liens enfant↔parent s34 + rôle-du-lien père / mère /
/// parent-libre s37), il n'ajoute AUCUN invariant d'écriture (0/1/2 parents restent acceptés à la pose).
/// Règle R3 : <see cref="Complet"/> SSI l'enfant porte un lien « père » ET un lien « mère » ; tout autre
/// cas à 1 ou 2 parents (deux parent-libre, père+parent-libre, mère+parent-libre, un seul parent) =
/// <see cref="Incomplet"/> ; aucun parent lié = <see cref="Vide"/> (racine isolée légitime s38, état neutre
/// distinct d'une anomalie). Présentation seule — n'intervient ni dans la résolution grille/légende ni dans
/// le gating (miroir R10). Le décompte est fidèle au store vivant : un orphelin (acteur supprimé encore
/// référencé) n'est PAS compté (filtre Resolvable s13, déjà appliqué aux branches du graphe).
/// </summary>
public enum StatutCoupleR3
{
    Vide,
    Incomplet,
    Complet
}
