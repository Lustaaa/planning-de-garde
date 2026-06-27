namespace PlanningDeGarde.Web;

/// <summary>
/// Traduction <b>présentationnelle</b> d'une couleur d'acteur (jeton métier « bleu », « rose »,
/// « gris »…) en valeur CSS, partagée par toutes les vues qui rendent une couleur du foyer
/// (grille, légende, liste de configuration) pour qu'elles restent <b>cohérentes</b> : une même
/// couleur produit la même teinte partout, et tout jeton inconnu retombe sur le neutre — miroir
/// du repli gris garanti côté lecture par le contrat <c>IPaletteCouleurs</c>. Aucune règle métier :
/// la couleur de référence reste résolue derrière l'API distante ; ceci n'en est que le rendu.
/// </summary>
public static class CouleursTheme
{
    /// <summary>Couleur <b>pleine</b> du jeton (pastille de légende / liste, fond de créneau) ;
    /// repli neutre gris pour le jeton neutre ou inconnu.</summary>
    public static string Pleine(string couleur) => couleur switch
    {
        "bleu" => "#2563eb",
        "orange" => "#ea580c",
        "vert" => "#16a34a",
        "violet" => "#7c3aed",
        "rose" => "#db2777",
        "gris" => "#6b7280",
        _ => "#6b7280",
    };

    /// <summary>Teinte <b>claire</b> du jeton (fond pâle d'une case-jour, lisible avec texte sombre) ;
    /// repli blanc pour le jeton neutre ou inconnu.</summary>
    public static string Claire(string couleur) => couleur switch
    {
        "bleu" => "#dbeafe",
        "orange" => "#ffedd5",
        "vert" => "#dcfce7",
        "violet" => "#ede9fe",
        "rose" => "#fce7f3",
        "gris" => "#f1f3f5",
        _ => "#ffffff",
    };
}
