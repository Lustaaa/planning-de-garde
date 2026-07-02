namespace PlanningDeGarde.Application;

/// <summary>
/// Port de droite du <b>facteur mot de passe</b> (volet 3, s25) : hache un mot de passe en un condensat
/// opaque (jamais le clair) et vérifie qu'un mot de passe candidat correspond à un condensat donné. La
/// réalisation concrète (PBKDF2 salé) vit en Infrastructure ; l'Application n'en dépend que par ce port.
/// Le mot de passe en clair ne transite jamais hors de ce port — seul son condensat est persisté.
/// </summary>
public interface IHacheurMotDePasse
{
    /// <summary>Produit un condensat opaque du mot de passe (jamais le clair), stockable sur le compte.</summary>
    string Hacher(string motDePasse);

    /// <summary>Vrai si le mot de passe candidat correspond au condensat fourni ; faux sinon.</summary>
    bool Verifier(string motDePasse, string condensat);
}
