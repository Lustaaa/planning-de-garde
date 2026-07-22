using System.Net.Mail;
using PlanningDeGarde.Application.Comptes.Ports;

namespace PlanningDeGarde.AdapterDroite.Smtp;

/// <summary>
/// Adaptateur de droite <b>SMTP concret</b> réalisant <see cref="IEnvoiMail"/> : remet
/// réellement au serveur SMTP configuré (Smtp4dev en dev, Docker ; relais réel en déploiement) un mail
/// de récupération de mot de passe adressé au destinataire et porteur du jeton de réinitialisation.
/// Remplace la doublure/Spy des scénarios : l'Application ne connaît toujours que le port, jamais
/// SMTP ni le format concret du mail. Sans état hors configuration → enregistrable en singleton
/// (aucune connexion n'est ouverte hors de l'envoi effectif).
/// </summary>
public sealed class EnvoiMailSmtp : IEnvoiMail
{
    private readonly string _hote;
    private readonly int _port;
    private readonly string _expediteur;

    public EnvoiMailSmtp(string hote, int port, string expediteur)
    {
        _hote = hote;
        _port = port;
        _expediteur = expediteur;
    }

    public void EnvoyerRecuperationMotDePasse(string destinataire, string jeton)
    {
        using var client = new SmtpClient(_hote, _port);
        using var message = new MailMessage(_expediteur, destinataire)
        {
            Subject = "Récupération de votre mot de passe",
            Body = $"Pour redéfinir votre mot de passe, utilisez ce jeton de réinitialisation : {jeton}",
        };
        client.Send(message);
    }
}
