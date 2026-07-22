using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application.Activites.Handlers;

/// <summary>
/// Commande d'édition d'une activité du référentiel du foyer (s35 Sc.2, miroir strict d'<see cref="EditerActeurCommand"/>
/// s33). L'identifiant stable est la clé (jamais éditable). Le libellé et l'adresse sont deux champs
/// <b>optionnels et indépendants</b> : un champ absent (<c>null</c>) n'est pas appliqué — la surface
/// correspondante du store n'est pas touchée (aucune écriture partielle croisée).
/// </summary>
public sealed record EditerActiviteCommand(string ActiviteId, string? Libelle = null, string? Adresse = null);

/// <summary>Confirmation de l'effet d'une édition aboutie : l'activité et ses valeurs appliquées
/// (libellé et/ou adresse ; un champ non édité reste <c>null</c>).</summary>
public sealed record EditerActiviteResultat(string ActiviteId, string? Libelle, string? Adresse = null);

/// <summary>
/// Use case : éditer une activité du foyer (libellé et/ou adresse). Mute le référentiel via le port
/// d'écriture. Le libellé et l'adresse sont deux surfaces indépendantes — éditer l'une ne touche jamais
/// l'autre (miroir strict de l'édition acteur s33).
/// </summary>
public sealed class EditerActiviteHandler
{
    private readonly IEditeurActivites _referentiel;

    public EditerActiviteHandler(IEditeurActivites referentiel)
    {
        _referentiel = referentiel;
    }

    public Result<EditerActiviteResultat> Handle(EditerActiviteCommand commande)
    {
        // Garde « libellé requis » conditionnelle (miroir R5/R10, s33) : un libellé FOURNI mais vide OU
        // tout-espaces est refusé AVANT toute écriture — ni le libellé (conservé) ni l'adresse fournie ne
        // sont écrits (aucune écriture partielle, store inchangé). La garde ne vise que le libellé fourni :
        // une édition adresse-seule (libellé null) n'est pas concernée (adresse vide reste licite).
        if (commande.Libelle is not null && string.IsNullOrWhiteSpace(commande.Libelle))
            return Result<EditerActiviteResultat>.Echec("libellé requis");

        // Libellé et adresse sont deux surfaces indépendantes : un champ absent (null) n'est pas appliqué —
        // changer l'adresse ne touche pas le libellé, renommer ne touche pas l'adresse.
        if (commande.Libelle is not null)
            _referentiel.Renommer(commande.ActiviteId, commande.Libelle);
        if (commande.Adresse is not null)
            _referentiel.ChangerAdresse(commande.ActiviteId, commande.Adresse);
        return Result<EditerActiviteResultat>.Succes(
            new EditerActiviteResultat(commande.ActiviteId, commande.Libelle, commande.Adresse));
    }
}
