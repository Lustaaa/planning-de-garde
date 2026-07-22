namespace PlanningDeGarde.Application.Foyer.Models;

/// <summary>
/// Rôle-du-lien porté par un lien enfant→parent (s37) : distingue les deux parents liés à un enfant,
/// aujourd'hui distingués par le seul nom. Vit sur le <b>lien</b> (pas sur l'acteur — le TypeActeur
/// reste au service du gating d'écriture, jamais le père/mère). Invariant minimal (s37) : pas deux
/// liens de même rôle EXCLUSIF (père/mère) sur un même enfant ; <see cref="ParentLibre"/> reste
/// répétable (compat + neutralité). Défaut neutre = <see cref="ParentLibre"/> (premier membre) : un
/// lien sans rôle explicite ≡ ancien lien nu (comportement s34 préservé, compat des liens déjà
/// persistés). Le rôle-du-lien est présentation + distinction — il n'intervient PAS dans la résolution
/// grille/légende ni dans le gating (miroir de l'invariant rôle-caractéristique R10).
/// </summary>
public enum RoleDuLien
{
    ParentLibre,
    Pere,
    Mere
}
