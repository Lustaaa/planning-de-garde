namespace PlanningDeGarde.Application.Comptes.Ports;

/// <summary>
/// Port de droite d'<b>envoi de mail</b> : remet au canal mail un message de
/// récupération de mot de passe (destinataire + jeton de réinitialisation). Réalisé par un adaptateur
/// SMTP concret en runtime (<b>vérifié manuellement au G3</b> — non testable en runtime local) et par
/// une doublure/Spy dans les tests (preuve de la logique Application/frontière). L'Application n'en
/// dépend que par ce port ; elle ne connaît ni SMTP ni le format concret du mail.
/// </summary>
public interface IEnvoiMail
{
    /// <summary>Remet au canal mail un message de récupération adressé au destinataire, porteur du
    /// jeton de réinitialisation (lien/code) à usage unique et expiration généré côté serveur.</summary>
    void EnvoyerRecuperationMotDePasse(string destinataire, string jeton);
}
