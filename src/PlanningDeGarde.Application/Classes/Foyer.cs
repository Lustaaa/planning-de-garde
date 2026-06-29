using System.Collections.Generic;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Infrastructure;

/// <summary>Référentiel de départ du foyer (enfants, lieux, responsables) pour l'IHM.</summary>
public static class Foyer
{
    public static readonly IReadOnlyList<string> Enfants = new[] { "Léa" };

    public static readonly IReadOnlyList<string> Lieux = new[]
    {
        "école", "domicile A", "domicile B", "nounou"
    };

    /// <summary>
    /// Identifiants stables des responsables du foyer (clés du set <see cref="CouleursParActeur"/>).
    /// La validation d'existence et le seed s'appuient sur ces identifiants — et non sur les libellés
    /// d'affichage — pour que le canal reçoive l'id atteignable par le set de couleurs (cadrage (B)).
    /// </summary>
    public static readonly IReadOnlyList<string> Responsables = new[]
    {
        "parent-a", "parent-b"
    };

    /// <summary>
    /// Set de couleurs par défaut acteur → couleur (source de vérité avant personnalisation
    /// par utilisateur — règle 16 hors périmètre). Tout acteur absent reçoit le repli neutre
    /// <see cref="CouleurNeutre"/> (règle 15, exercée au Sc.8). Étendu aux acteurs
    /// non-responsables (nounou, école) aux Sc.4/Sc.8.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> CouleursParActeur =
        new Dictionary<string, string>
        {
            ["parent-a"] = "bleu",
            ["parent-b"] = "orange",
            ["nounou"] = "vert",
            // Responsable au nom long (Sc.7 lisibilité) : identifiant stable colorié en bleu — la
            // longueur du libellé est un enjeu de PRÉSENTATION (troncature + survol), pas de couleur.
            ["parent-c"] = "bleu",
        };

    /// <summary>Couleur neutre (repli déterministe) pour tout acteur absent du set.</summary>
    public const string CouleurNeutre = "gris";

    /// <summary>
    /// Référentiel des noms d'affichage par identifiant stable (source de vérité du libellé
    /// humain — résolu en lecture par <see cref="FoyerReferentielResponsables"/>, miroir du set
    /// de couleurs). Déclare aussi l'acteur hors-set couleur « grand-père » (identifiant stable
    /// valide, absent de <see cref="CouleursParActeur"/> → couleur neutre, mais nom conservé).
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> NomsParResponsable =
        new Dictionary<string, string>
        {
            ["parent-a"] = "Alice",
            ["parent-b"] = "Bruno",
            ["grand-pere"] = "grand-père",
            // Nom long (Sc.7) : adopte le nom du scénario pour rendre la troncature visible au runtime
            // sans altérer la donnée (le read model porte toujours le nom complet).
            ["parent-c"] = "Marie-Hélène Grand-Dubois",
            // Acteur de type « Autre » (sprint 14, impersonation) : la nounou, déjà colorée en vert,
            // gagne un nom d'affichage pour être énumérée et incarnable. Incarnée, elle masque les
            // actions d'écriture (consultation seule, règle 8).
            ["nounou"] = "Nina la nounou",
        };

    /// <summary>
    /// Type déclaré (Admin / Parent / Autre) par acteur, surfacé en <b>lecture seule</b> depuis ce seed
    /// (D3, sprint 14) pour piloter le rôle de l'identité effective lors d'une impersonation bornée.
    /// Un acteur absent de ce dictionnaire — typiquement un acteur ajouté en session — est traité comme
    /// <see cref="TypeActeur.Parent"/> par défaut (aucune saisie ni persistance neuve de type, borne
    /// anti-cliquet règle 30). Source de vérité du type : la déclaration du foyer, jamais persistée.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, TypeActeur> TypesParActeur =
        new Dictionary<string, TypeActeur>
        {
            ["parent-a"] = TypeActeur.Parent,   // Alice — parent (le configurateur)
            ["parent-b"] = TypeActeur.Parent,   // Bruno — parent
            ["parent-c"] = TypeActeur.Admin,    // Marie-Hélène — administratrice du foyer
            ["grand-pere"] = TypeActeur.Autre,  // grand-père — autre intervenant (consultation seule)
            ["nounou"] = TypeActeur.Autre,      // Nina la nounou — autre intervenante (consultation seule)
        };

    /// <summary>Type par défaut d'un acteur non déclaré dans <see cref="TypesParActeur"/> (acteur ajouté
    /// en session) : Parent (aucune saisie de type, borne anti-cliquet règle 30).</summary>
    public const TypeActeur TypeParDefaut = TypeActeur.Parent;
}
