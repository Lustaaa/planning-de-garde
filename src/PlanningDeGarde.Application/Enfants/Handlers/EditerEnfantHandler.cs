using System.Linq;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application.Enfants.Handlers;

/// <summary>
/// Commande d'édition du prénom d'un enfant du référentiel du foyer (config). L'identifiant stable est
/// la clé (jamais éditable) ; seul le prénom change. Mute le référentiel via le port d'écriture
/// <see cref="IEditeurEnfants"/> — dernière écriture gagne, aucun doublon.
/// </summary>
public sealed record EditerEnfantCommand(string EnfantId, string NouveauPrenom);

/// <summary>Confirmation d'une édition aboutie : l'enfant (id stable inchangé) et son nouveau prénom.</summary>
public sealed record EditerEnfantResultat(string EnfantId, string Prenom);

/// <summary>
/// Use case : éditer le prénom d'un enfant du référentiel du foyer. Applique le nouveau prénom sur
/// l'identifiant stable via le port d'écriture — l'id reste inchangé —, puis diffuse la mise à jour.
/// </summary>
public sealed class EditerEnfantHandler
{
    private readonly IEnumerationEnfants _enfants;
    private readonly IEditeurEnfants _referentiel;
    private readonly INotificateurPlanning _notificateur;

    public EditerEnfantHandler(IEnumerationEnfants enfants, IEditeurEnfants referentiel, INotificateurPlanning notificateur)
    {
        _enfants = enfants;
        _referentiel = referentiel;
        _notificateur = notificateur;
    }

    public Result<EditerEnfantResultat> Handle(EditerEnfantCommand commande)
    {
        // Garde « prénom requis » (S4, miroir S2) : un nouveau prénom vide ou tout-espaces est refusé
        // sans muter le store — l'ancien prénom est conservé (référentiel inchangé), aucune diffusion.
        if (string.IsNullOrWhiteSpace(commande.NouveauPrenom))
            return Result<EditerEnfantResultat>.Echec("prénom requis");

        // Garde « prénom déjà existant » (S4, miroir S3) : refus si un AUTRE enfant porte déjà ce prénom
        // (l'enfant édité s'exclut lui-même — éditer sur son propre prénom n'est pas un doublon). Aucun
        // doublon persisté, référentiel inchangé.
        if (_enfants.EnumererEnfants().Any(e => e.Id != commande.EnfantId && e.Prenom == commande.NouveauPrenom))
            return Result<EditerEnfantResultat>.Echec("prénom déjà existant");

        _referentiel.Editer(commande.EnfantId, commande.NouveauPrenom);
        _notificateur.NotifierMiseAJour();
        return Result<EditerEnfantResultat>.Succes(new EditerEnfantResultat(commande.EnfantId, commande.NouveauPrenom));
    }
}
