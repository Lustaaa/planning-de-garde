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

    public static readonly IReadOnlyList<string> Responsables = new[]
    {
        "Parent A", "Parent B"
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
}
