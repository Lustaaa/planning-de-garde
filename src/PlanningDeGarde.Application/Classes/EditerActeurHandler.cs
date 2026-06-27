using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application;

/// <summary>
/// Commande d'édition volatile d'un acteur du foyer (config). L'identifiant stable est la
/// clé (jamais éditable). Le nom et la couleur sont deux champs <b>optionnels et indépendants</b> :
/// une édition peut ne porter que le nom (Sc.1), que la couleur (Sc.2), un champ absent (null)
/// n'est pas appliqué — la surface correspondante du store n'est pas touchée.
/// </summary>
public sealed record EditerActeurCommand(string ActeurId, string? Nom = null, string? Couleur = null);

/// <summary>Confirmation de l'effet d'une édition aboutie : l'acteur et ses valeurs appliquées
/// (nom et/ou couleur ; un champ non édité reste null).</summary>
public sealed record ActeurConfigSnapshot(string ActeurId, string? Nom, string? Couleur);

/// <summary>
/// Use case : éditer un acteur du foyer (renommer). Mute la configuration via le port
/// d'écriture, puis déclenche la diffusion temps réel sur édition aboutie — jamais
/// d'écriture par le canal de diffusion.
/// </summary>
public sealed class EditerActeurHandler
{
    private readonly IEditeurConfigurationFoyer _configuration;
    private readonly INotificateurPlanning _notificateur;

    public EditerActeurHandler(IEditeurConfigurationFoyer configuration, INotificateurPlanning notificateur)
    {
        _configuration = configuration;
        _notificateur = notificateur;
    }

    public Result<ActeurConfigSnapshot> Handle(EditerActeurCommand commande)
    {
        // Garde « nom non vide » conditionnelle (Sc.8) : un nom fourni mais vide OU tout-espaces est
        // refusé sans muter le store (l'ancien nom est conservé) ni déclencher de diffusion. La garde
        // ne s'applique qu'au nom fourni — une édition couleur-seule (nom null) n'est pas visée.
        if (commande.Nom is not null && string.IsNullOrWhiteSpace(commande.Nom))
            return Result<ActeurConfigSnapshot>.Echec("le nom ne peut pas être vide");

        // Nom et couleur sont deux surfaces indépendantes : un champ absent (null) n'est pas
        // appliqué — recolorier sans nom ne touche pas le libellé, renommer sans couleur ne
        // touche pas la teinte.
        if (commande.Nom is not null)
            _configuration.Renommer(commande.ActeurId, commande.Nom);
        if (commande.Couleur is not null)
            _configuration.Recolorier(commande.ActeurId, commande.Couleur);
        _notificateur.NotifierMiseAJour(); // diffusion temps réel sur édition aboutie
        return Result<ActeurConfigSnapshot>.Succes(new ActeurConfigSnapshot(commande.ActeurId, commande.Nom, commande.Couleur));
    }
}
