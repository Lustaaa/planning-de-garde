using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 28 — S9 — Rapprochement compte local ↔ Google (@back @preuve-doublure, volet 3).
//   PREUVE PAR DOUBLURE du port IFournisseurOAuth (le provider Google réel — secrets / redirect_uri /
//   callback — n'est PAS testable en runtime local : entorse G2 assumée, vérifiée MANUELLEMENT au G3).
//   Statut cible « ✅ logique / ⚠️ câblage », jamais un ✅ franc (dette de câblage provider réel, backlog).
//
//   La LOGIQUE de rapprochement existe depuis s25 (ConnexionOAuthHandler délègue à SeConnecterHandler par
//   email — Sc.14/15). Ce test s28 confirme l'EMPHASE volet 3 : un callback Google d'un email CONNU ouvre
//   la session sur le compte local EXISTANT (rattachement d'identité) SANS créer de second compte (pas de
//   double compte) ; un email inconnu / un compte Inactif est refusé, sans session. Aucun code de prod neuf
//   (le handler ne crée jamais de compte — il ne fait que résoudre l'email vers le compte existant).
public class RapprochementOAuthGoogleCompteLocalTests
{
    private const string Email = "papa@foyer.fr";
    private const string ActeurPapa = "acteur-papa";

    private static ConnexionOAuthHandler Handler(ReferentielComptesEnMemoire comptes, IdentiteExterne? identite)
        => new(new FakeFournisseurOAuth(identite), new SeConnecterHandler(comptes, new HacheurMotDePassePbkdf2()));

    // ---------- Acceptation — rattachement sur le compte local existant, PAS de double compte ----------
    [Fact]
    public void Acceptation_Should_Ouvrir_la_session_sur_le_compte_local_existant_sans_creer_de_second_compte_When_le_callback_Google_porte_un_email_connu()
    {
        var comptes = new ReferentielComptesEnMemoire();
        comptes.Creer("compte-papa", Email, StatutCompte.Actif, ActeurPapa); // compte OAuth : pas de mot de passe local
        var handler = Handler(comptes, new IdentiteExterne(Email)); // email vérifié Google = celui du compte local

        var resultat = handler.Handle(new CallbackOAuthCommand("callback-google-papa"));

        // Session ouverte sur CE compte local (identité réelle = son acteur) — rattachement d'identité.
        Assert.True(resultat.EstSucces);
        Assert.Equal(ActeurPapa, resultat.Valeur!.IdentiteReelle);

        // PAS de double compte : le référentiel porte TOUJOURS l'unique compte d'origine (aucune création).
        var tousLesComptes = comptes.EnumererComptes();
        Assert.Single(tousLesComptes);
        Assert.Equal("compte-papa", tousLesComptes.Single().Id);
    }

    // ---------- Acceptation — email inconnu : refus, aucune session ----------
    [Fact]
    public void Acceptation_Should_Refuser_sans_session_When_le_callback_Google_porte_un_email_inconnu()
    {
        var comptes = new ReferentielComptesEnMemoire(); // aucun compte
        var handler = Handler(comptes, new IdentiteExterne("inconnu@foyer.fr"));

        var resultat = handler.Handle(new CallbackOAuthCommand("callback-google-inconnu"));

        Assert.False(resultat.EstSucces);
        Assert.Null(resultat.Valeur);
        Assert.False(string.IsNullOrWhiteSpace(resultat.Motif));
    }

    // ---------- Acceptation — compte Inactif : refus, aucune session ----------
    [Fact]
    public void Acceptation_Should_Refuser_sans_session_When_le_compte_local_lie_est_inactif()
    {
        var comptes = new ReferentielComptesEnMemoire();
        comptes.Creer("compte-papa", Email, StatutCompte.Inactif, ActeurPapa);
        var handler = Handler(comptes, new IdentiteExterne(Email));

        var resultat = handler.Handle(new CallbackOAuthCommand("callback-google-inactif"));

        Assert.False(resultat.EstSucces);
        Assert.Null(resultat.Valeur);
        Assert.False(string.IsNullOrWhiteSpace(resultat.Motif));
    }
}
