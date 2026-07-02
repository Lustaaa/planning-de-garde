using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application;

/// <summary>
/// Commande de <b>redéfinition</b> de mot de passe par jeton de réinitialisation (volet 5, s25) :
/// soumet un nouveau mot de passe accompagné du jeton reçu par mail (Sc.11). Le jeton doit être VALIDE
/// (connu, non consommé, non expiré) ; sinon la redéfinition est rejetée sans aucune mutation.
/// </summary>
public sealed record RedefinirMotDePasseCommand(string Jeton, string NouveauMotDePasse);

/// <summary>Confirmation d'une redéfinition aboutie (réponse sèche).</summary>
public sealed record MotDePasseRedefini;

/// <summary>
/// Use case : redéfinir le mot de passe d'un compte via un jeton de réinitialisation. Valide le jeton
/// (existence, non-consommation, non-expiration contre l'horloge injectée), redéfinit le mot de passe
/// haché du compte visé, puis consomme le jeton (usage unique). Tout jeton invalide → refus sans mutation.
/// </summary>
public sealed class RedefinirMotDePasseHandler
{
    private readonly IEditeurComptes _editeur;
    private readonly IReferentielJetonsReset _jetons;
    private readonly IHacheurMotDePasse _hacheur;
    private readonly IDateTimeProvider _horloge;

    public RedefinirMotDePasseHandler(
        IEditeurComptes editeur,
        IReferentielJetonsReset jetons,
        IHacheurMotDePasse hacheur,
        IDateTimeProvider horloge)
    {
        _editeur = editeur;
        _jetons = jetons;
        _hacheur = hacheur;
        _horloge = horloge;
    }

    public Result<MotDePasseRedefini> Handle(RedefinirMotDePasseCommand commande)
    {
        // Garde « jeton inconnu » : un jeton absent du store ne mute rien.
        var jeton = _jetons.Trouver(commande.Jeton);
        if (jeton is null)
            return Result<MotDePasseRedefini>.Echec("jeton invalide");

        // Garde « jeton déjà consommé » : usage unique — un jeton rejoué ne re-mute pas le mot de passe.
        if (jeton.Consomme)
            return Result<MotDePasseRedefini>.Echec("jeton invalide");

        // Garde « jeton expiré » : l'instant courant (horloge injectée) ne doit pas dépasser l'expiration.
        if (_horloge.Maintenant >= jeton.Expiration)
            return Result<MotDePasseRedefini>.Echec("jeton invalide");

        // Jeton valide : redéfinition du mot de passe HACHÉ du compte visé, puis consommation du jeton.
        _editeur.RedefinirMotDePasse(jeton.CompteId, _hacheur.Hacher(commande.NouveauMotDePasse));
        _jetons.Consommer(jeton.Jeton);
        return Result<MotDePasseRedefini>.Succes(new MotDePasseRedefini());
    }
}
