using System.Collections.Generic;

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
        };
}
