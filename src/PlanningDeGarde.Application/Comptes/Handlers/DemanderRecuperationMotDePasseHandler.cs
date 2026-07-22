using System;
using System.Linq;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application.Comptes.Handlers;

/// <summary>
/// Commande de <b>demande de récupération</b> de mot de passe (volet 5, s25) : sur un email, déclenche
/// l'émission d'un jeton de réinitialisation + d'un mail — SANS jamais révéler au client si l'email
/// existe (anti-énumération). Le câblage SMTP réel n'est pas testable en runtime local (entorse assumée
/// G2) : la logique est prouvée contre une doublure du port <see cref="IEnvoiMail"/>.
/// </summary>
public sealed record DemanderRecuperationMotDePasseCommand(string Email);

/// <summary>Réponse NEUTRE d'une demande de récupération : ne porte AUCUNE information sur l'existence
/// du compte ni le jeton (qui ne transite que par le canal mail). Un succès sec, identique que l'email
/// soit connu (Sc.11) ou inconnu (Sc.12) — anti-énumération.</summary>
public sealed record RecuperationDemandee;

/// <summary>
/// Use case : demander une récupération de mot de passe. Résout le compte sur son email ; s'il existe,
/// génère un jeton de réinitialisation côté serveur et remet un mail au port <see cref="IEnvoiMail"/>.
/// La réponse au client est TOUJOURS un succès neutre (aucune fuite sur l'existence du compte).
/// </summary>
public sealed class DemanderRecuperationMotDePasseHandler
{
    private readonly IEnumerationComptes _comptes;
    private readonly IEnvoiMail _mail;
    private readonly IReferentielJetonsReset _jetons;
    private readonly IDateTimeProvider _horloge;

    public DemanderRecuperationMotDePasseHandler(
        IEnumerationComptes comptes,
        IEnvoiMail mail,
        IReferentielJetonsReset jetons,
        IDateTimeProvider horloge)
    {
        _comptes = comptes;
        _mail = mail;
        _jetons = jetons;
        _horloge = horloge;
    }

    public Result<RecuperationDemandee> Handle(DemanderRecuperationMotDePasseCommand commande)
    {
        // Résolution sur l'email lu du référentiel. Un email PORTÉ par un compte déclenche l'émission
        // (jeton + mail) ; l'email inconnu ne déclenche rien (Sc.12) — mais la RÉPONSE est la même dans
        // les deux cas (anti-énumération) : un succès neutre sans jeton ni indice d'existence.
        var compte = _comptes.EnumererComptes().FirstOrDefault(c => c.Email == commande.Email);
        if (compte is not null)
        {
            // Jeton de réinitialisation OPAQUE généré côté serveur (usage unique + expiration :
            // consommation/expiration prouvées à Sc.13). Ne transite que par le canal mail, jamais
            // par la réponse au client.
            var jeton = $"reset-{Guid.NewGuid():N}";

            // Émission persistée : le jeton est enregistré au store serveur avec une expiration à 60
            // minutes (contre l'horloge injectée), de sorte que la redéfinition ultérieure (Sc.13) puisse
            // le retrouver et le consommer. Un email INCONNU n'atteint pas cette branche → aucun jeton
            // écrit, aucun mail émis (anti-énumération, S4) — mais la réponse reste identique.
            _jetons.Enregistrer(new JetonReset(jeton, compte.Id, _horloge.Maintenant.AddMinutes(60), Consomme: false));
            _mail.EnvoyerRecuperationMotDePasse(compte.Email, jeton);
        }

        return Result<RecuperationDemandee>.Succes(new RecuperationDemandee());
    }
}
