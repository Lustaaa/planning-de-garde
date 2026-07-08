using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 30 — S4 — Éditer le prénom d'un enfant existant (@back)
//   Étant donné un enfant enregistré d'identifiant stable connu, prénom "Léa"
//   Quand un Parent édite son prénom en "Léana" (clé = identifiant stable)
//   Alors la commande réussit
//   Et l'enfant relu porte le prénom "Léana" avec le même identifiant stable
//   Et la diffusion temps réel de mise à jour est déclenchée
//   # Un prénom vide ou en doublon d'un AUTRE enfant est rejeté (mêmes rejets que S2/S3), rien appliqué
//
// Miroir strict du renommage de rôle (s21 S2) : l'id stable est la clé, seul le prénom change ;
// dernière écriture gagne, self-exclusion sur le doublon (renommer sur son propre prénom n'est pas un doublon).
public class Scenario30_S4_EditerEnfant
{
    private const string LeaId = "enfant-lea";
    private const string TomId = "enfant-tom";

    // ---------- Acceptation (boucle externe, frontière Application) ----------
    [Fact]
    public void Acceptation_Should_Relire_le_nouveau_prenom_sur_le_meme_id_et_diffuser_When_le_parent_edite_le_prenom()
    {
        var referentiel = new FakeReferentielEnfants().AvecEnfant(LeaId, "Léa");
        var notificateur = new FakeNotificateurPlanning();
        var handler = new EditerEnfantHandler(referentiel, referentiel, notificateur);

        var resultat = handler.Handle(new EditerEnfantCommand(LeaId, "Léana"));

        // La commande réussit
        Assert.True(resultat.EstSucces);

        // L'enfant relu porte "Léana" avec le MÊME identifiant stable
        var enfant = referentiel.EnumererEnfants().Single(e => e.Id == LeaId);
        Assert.Equal("Léana", enfant.Prenom);

        // La diffusion temps réel de mise à jour est déclenchée
        Assert.Equal(1, notificateur.NombreDeNotifications);
    }

    // ---------- Test #1 — Driver : rejet d'un nouveau prénom vide, rien appliqué ----------
    [Fact]
    public void Should_Rejeter_sans_muter_ni_diffuser_When_le_nouveau_prenom_est_vide()
    {
        var referentiel = new FakeReferentielEnfants().AvecEnfant(LeaId, "Léa");
        var notificateur = new FakeNotificateurPlanning();
        var handler = new EditerEnfantHandler(referentiel, referentiel, notificateur);

        var resultat = handler.Handle(new EditerEnfantCommand(LeaId, "   "));

        Assert.False(resultat.EstSucces);
        Assert.False(string.IsNullOrWhiteSpace(resultat.Motif));
        Assert.Equal("Léa", referentiel.EnumererEnfants().Single(e => e.Id == LeaId).Prenom); // inchangé
        Assert.Equal(0, notificateur.NombreDeNotifications);
    }

    // ---------- Test #2 — Driver : rejet d'un doublon d'un AUTRE enfant, self-exclusion ----------
    [Fact]
    public void Should_Rejeter_le_doublon_d_un_autre_enfant_mais_accepter_son_propre_prenom_When_le_parent_edite()
    {
        var referentiel = new FakeReferentielEnfants().AvecEnfant(LeaId, "Léa").AvecEnfant(TomId, "Tom");
        var notificateur = new FakeNotificateurPlanning();
        var handler = new EditerEnfantHandler(referentiel, referentiel, notificateur);

        // Renommer Tom en "Léa" (déjà porté par un AUTRE enfant) → rejet, Tom inchangé
        var doublon = handler.Handle(new EditerEnfantCommand(TomId, "Léa"));
        Assert.False(doublon.EstSucces);
        Assert.Equal("Tom", referentiel.EnumererEnfants().Single(e => e.Id == TomId).Prenom);

        // Éditer Léa sur son PROPRE prénom "Léa" n'est PAS un doublon (self-exclusion) → succès
        var soi = handler.Handle(new EditerEnfantCommand(LeaId, "Léa"));
        Assert.True(soi.EstSucces);
    }
}
