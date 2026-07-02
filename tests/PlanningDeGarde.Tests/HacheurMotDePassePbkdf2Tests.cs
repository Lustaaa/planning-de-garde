using PlanningDeGarde.Infrastructure;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 25 — Sc.7 (cycle interne) — Hacheur de mot de passe (vrai code prod, PBKDF2)
//   Facteur mot de passe du volet 3 : le hachage est un adaptateur RÉEL (PBKDF2 salé) réalisant le port
//   IHacheurMotDePasse. Deux propriétés forcées ici : (1) le condensat n'est jamais le clair (secret non
//   fuité), (2) Verifier reconnaît le bon mot de passe contre son propre condensat. Le rejet du MAUVAIS
//   mot de passe est piloté par Sc.8 (on ne vole pas son rouge ici).
public class HacheurMotDePassePbkdf2Tests
{
    private const string MotDePasse = "s3cr3t-carole";

    [Fact]
    public void Should_Ne_jamais_produire_le_clair_When_on_hache()
    {
        var hacheur = new HacheurMotDePassePbkdf2();

        var condensat = hacheur.Hacher(MotDePasse);

        Assert.False(string.IsNullOrWhiteSpace(condensat));
        Assert.NotEqual(MotDePasse, condensat); // le condensat n'est jamais le mot de passe en clair
    }

    [Fact]
    public void Should_Reconnaitre_le_bon_mot_de_passe_When_on_verifie_contre_son_condensat()
    {
        var hacheur = new HacheurMotDePassePbkdf2();
        var condensat = hacheur.Hacher(MotDePasse);

        Assert.True(hacheur.Verifier(MotDePasse, condensat));
    }
}
