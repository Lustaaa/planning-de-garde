using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application;

/// <summary>
/// Commande d'édition volatile d'un acteur du foyer (config). L'identifiant stable est la
/// clé (jamais éditable) ; seul le nom d'affichage mute ici (la couleur émergera au Sc.2).
/// </summary>
public sealed record EditerActeurCommand(string ActeurId, string Nom);

/// <summary>Confirmation de l'effet d'une édition aboutie : l'acteur et sa valeur appliquée.</summary>
public sealed record ActeurConfigSnapshot(string ActeurId, string Nom);

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
        _configuration.Renommer(commande.ActeurId, commande.Nom);
        _notificateur.NotifierMiseAJour(); // diffusion temps réel sur édition aboutie
        return Result<ActeurConfigSnapshot>.Succes(new ActeurConfigSnapshot(commande.ActeurId, commande.Nom));
    }
}
