using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Tests.Fakes;

/// <summary>
/// Spy à la main du port de droite d'envoi de mail (<see cref="IEnvoiMail"/>, volet 5 s25). Le câblage
/// SMTP réel n'est PAS testable en runtime local (entorse assumée au gate G2) : on prouve la logique
/// Application/frontière contre CE spy — il enregistre chaque mail de récupération émis (destinataire +
/// jeton) pour assertion. L'adaptateur SMTP concret est vérifié MANUELLEMENT au G3.
/// </summary>
public sealed class SpyEnvoiMail : IEnvoiMail
{
    private readonly List<MailRecuperationEmis> _mails = new();

    public void EnvoyerRecuperationMotDePasse(string destinataire, string jeton)
        => _mails.Add(new MailRecuperationEmis(destinataire, jeton));

    public int NombreDeMailsEmis => _mails.Count;

    public MailRecuperationEmis? DernierMail => _mails.LastOrDefault();

    public sealed record MailRecuperationEmis(string Destinataire, string Jeton);
}
