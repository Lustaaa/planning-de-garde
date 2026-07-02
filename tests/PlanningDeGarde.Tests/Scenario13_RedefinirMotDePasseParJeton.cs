using System;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 25 — Sc.13 — Récupération : jeton VALIDE → mot de passe redéfini, jeton consommé (@back)
//   Tranche BACKEND (frontière Application) : soumettre un NOUVEAU mot de passe avec un jeton de
//   réinitialisation VALIDE (émis pour un compte, cf. Sc.11, non expiré, non consommé) REDÉFINIT le mot
//   de passe du compte (HACHÉ via IHacheurMotDePasse) et CONSOMME le jeton (usage unique — 2e usage
//   échoue). Un jeton EXPIRÉ ou INCONNU est rejeté SANS aucune mutation (mot de passe inchangé).
//   Introduit le stockage serveur du jeton (port IReferentielJetonsReset, doublé à la main) + l'horloge
//   injectable (IDateTimeProvider, doublée figée) pour prouver l'expiration de façon déterministe.
public class Scenario13_RedefinirMotDePasseParJeton
{
    private const string Email = "carole@foyer.fr";
    private const string CompteId = "compte-carole";
    private const string AncienMotDePasse = "ancien-carole";
    private const string NouveauMotDePasse = "nouveau-carole";
    private static readonly DateTime Maintenant = new(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc);

    private static (ReferentielComptesEnMemoire comptes, FakeReferentielJetonsReset jetons, HacheurMotDePassePbkdf2 hacheur, HorlogeFigee horloge, RedefinirMotDePasseHandler handler) Contexte()
    {
        var hacheur = new HacheurMotDePassePbkdf2();
        var comptes = new ReferentielComptesEnMemoire();
        comptes.Creer(CompteId, Email, StatutCompte.Actif, "acteur-carole", hacheur.Hacher(AncienMotDePasse));
        var jetons = new FakeReferentielJetonsReset();
        var horloge = new HorlogeFigee(Maintenant);
        var handler = new RedefinirMotDePasseHandler(comptes, jetons, hacheur, horloge);
        return (comptes, jetons, hacheur, horloge, handler);
    }

    // ---------- Acceptation (boucle externe, frontière Application) ----------
    // Jeton VALIDE (non expiré, non consommé) : la redéfinition RÉUSSIT, le compte porte désormais le
    // NOUVEAU mot de passe (haché, vérifiable ; l'ancien ne vérifie plus), et le jeton devient consommé.
    [Fact]
    public void Acceptation_Should_Redefinir_le_mot_de_passe_hache_et_consommer_le_jeton_When_le_jeton_est_valide()
    {
        var (comptes, jetons, hacheur, _, handler) = Contexte();
        jetons.Enregistrer(new JetonReset("jeton-valide", CompteId, Maintenant.AddMinutes(30), Consomme: false));

        var resultat = handler.Handle(new RedefinirMotDePasseCommand("jeton-valide", NouveauMotDePasse));

        Assert.True(resultat.EstSucces);
        var compte = comptes.EnumererComptes().Single(c => c.Id == CompteId);
        Assert.True(hacheur.Verifier(NouveauMotDePasse, compte.MotDePasseHache!));  // nouveau MDP actif
        Assert.False(hacheur.Verifier(AncienMotDePasse, compte.MotDePasseHache!));  // ancien MDP révoqué
        Assert.True(jetons.Trouver("jeton-valide")!.Consomme);                      // jeton consommé
    }

    // ---------- Driver — usage unique : une seconde utilisation échoue ----------
    // Contradiction : sans consommation, le jeton resterait rejouable. Force l'usage unique — le 2e appel
    // avec le même jeton est REJETÉ et NE re-mute pas le mot de passe.
    [Fact]
    public void Should_Rejeter_la_seconde_utilisation_du_meme_jeton_When_il_a_deja_ete_consomme()
    {
        var (comptes, jetons, hacheur, _, handler) = Contexte();
        jetons.Enregistrer(new JetonReset("jeton-valide", CompteId, Maintenant.AddMinutes(30), Consomme: false));

        handler.Handle(new RedefinirMotDePasseCommand("jeton-valide", NouveauMotDePasse)); // 1er usage OK
        var second = handler.Handle(new RedefinirMotDePasseCommand("jeton-valide", "encore-un-autre"));

        Assert.False(second.EstSucces);                                              // 2e usage rejeté
        var compte = comptes.EnumererComptes().Single(c => c.Id == CompteId);
        Assert.True(hacheur.Verifier(NouveauMotDePasse, compte.MotDePasseHache!));   // MDP resté celui du 1er usage
    }

    // ---------- Driver — jeton EXPIRÉ rejeté sans mutation ----------
    // Contradiction : sans contrôle d'expiration, un jeton périmé serait accepté. Force le rejet quand
    // l'instant courant (horloge) dépasse l'expiration — mot de passe INCHANGÉ.
    [Fact]
    public void Should_Rejeter_sans_mutation_When_le_jeton_est_expire()
    {
        var (comptes, jetons, hacheur, horloge, handler) = Contexte();
        jetons.Enregistrer(new JetonReset("jeton-expire", CompteId, Maintenant.AddMinutes(30), Consomme: false));
        horloge.Maintenant = Maintenant.AddMinutes(31); // l'horloge dépasse l'expiration

        var resultat = handler.Handle(new RedefinirMotDePasseCommand("jeton-expire", NouveauMotDePasse));

        Assert.False(resultat.EstSucces);
        var compte = comptes.EnumererComptes().Single(c => c.Id == CompteId);
        Assert.True(hacheur.Verifier(AncienMotDePasse, compte.MotDePasseHache!)); // ancien MDP toujours actif : aucune mutation
    }

    // ---------- Driver — jeton INCONNU rejeté sans mutation ----------
    // Contradiction : un jeton absent du store ne doit rien muter. Force le rejet sans effet de bord.
    [Fact]
    public void Should_Rejeter_sans_mutation_When_le_jeton_est_inconnu()
    {
        var (comptes, _, hacheur, _, handler) = Contexte();

        var resultat = handler.Handle(new RedefinirMotDePasseCommand("jeton-jamais-emis", NouveauMotDePasse));

        Assert.False(resultat.EstSucces);
        var compte = comptes.EnumererComptes().Single(c => c.Id == CompteId);
        Assert.True(hacheur.Verifier(AncienMotDePasse, compte.MotDePasseHache!)); // aucune mutation
    }
}
