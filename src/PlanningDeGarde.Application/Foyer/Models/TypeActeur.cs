namespace PlanningDeGarde.Application.Foyer.Models;

/// <summary>
/// Type d'un acteur déclaré du foyer (mécanique de base « trois types d'acteurs »). Surfacé en
/// LECTURE SEULE depuis la déclaration seed du foyer pour piloter le rôle de
/// l'identité effective lors d'une impersonation bornée : un acteur de type <see cref="Parent"/> ou
/// <see cref="Admin"/> conserve les actions d'écriture, un acteur <see cref="Autre"/> les
/// masque (consultation seule). <see cref="Parent"/> est la valeur par défaut (premier membre) : un
/// acteur ajouté en session, sans type déclaré, est traité comme Parent — aucune saisie ni
/// persistance neuve de type (borne anti-cliquet).
/// </summary>
public enum TypeActeur
{
    Parent,
    Admin,
    Autre
}
