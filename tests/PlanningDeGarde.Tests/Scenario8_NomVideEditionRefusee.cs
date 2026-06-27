using System.Collections.Generic;
using PlanningDeGarde.Application;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 08 — Sc.8 — Renommer avec un nom vide : édition refusée, ancien nom conservé (@erreur, 🖥️ IHM)
//   Tranche BACKEND (tdd-auto) : garde « nom non vide » dans le handler EditerActeur (2 drivers de
//   la garde conditionnelle + 1 caractérisation d'absence-de-diffusion sur refus). L'acceptation
//   runtime (message clair à l'écran, case + légende inchangées, front WASM + API distante) est
//   menée séparément par ihm-builder.
//
//   Garde CONDITIONNELLE (pas inconditionnelle) : le nominal « renommage réussi » (Sc.1) est déjà
//   vert ; un refus inconditionnel régresserait Sc.1. La garde ne refuse que le nom vide / tout-
//   espaces, et laisse le store inchangé (ancien nom conservé) sans déclencher de diffusion.
public class Scenario8_NomVideEditionRefusee
{
    private const string ParentB = "parent-b";
    private const string Bruno = "Bruno";                          // seed en dur (ancien nom à conserver)
    private const string MotifNomVide = "le nom ne peut pas être vide"; // motif métier (pas technique)

    // ---------- Test #1 — Driver : un nom vide (chaîne vide) est refusé, ancien nom conservé ----------
    // Contradiction : le handler issu de Sc.1 applique TOUT nom fourni — une chaîne vide écraserait
    // « Bruno » par "". Force la garde « nom non vide » qui refuse (Result.Echec porteur du motif) et
    // laisse le store INCHANGÉ (ancien nom conservé).
    [Fact]
    public void Should_Refuser_l_edition_avec_un_motif_clair_et_conserver_l_ancien_nom_When_le_nom_demande_est_une_chaine_vide()
    {
        var configuration = new FakeConfigurationFoyer(new Dictionary<string, string> { [ParentB] = Bruno });
        var handler = new EditerActeurHandler(configuration, new FakeNotificateurPlanning());

        var resultat = handler.Handle(new EditerActeurCommand(ParentB, Nom: ""));

        Assert.False(resultat.EstSucces);                  // l'édition est refusée
        Assert.Equal(MotifNomVide, resultat.Motif);        // ... avec un motif métier clair
        Assert.Equal(Bruno, configuration.NomDe(ParentB)); // le store n'a pas été muté (ancien nom conservé)
    }

    // ---------- Test #2 — Driver : un nom tout-espaces est refusé, ancien nom conservé ----------
    // Contradiction : la garde minimale du #1 (« chaîne vide ») laisse PASSER un nom tout-espaces
    // (« " " » n'est pas ""), qui écraserait « Bruno » par des blancs. Force la garde sur le nom
    // UTILE (espaces ignorés), contredisant l'implémentation minimale du #1.
    [Fact]
    public void Should_Refuser_l_edition_et_conserver_l_ancien_nom_When_le_nom_demande_ne_contient_que_des_espaces()
    {
        var configuration = new FakeConfigurationFoyer(new Dictionary<string, string> { [ParentB] = Bruno });
        var handler = new EditerActeurHandler(configuration, new FakeNotificateurPlanning());

        var resultat = handler.Handle(new EditerActeurCommand(ParentB, Nom: "   "));

        Assert.False(resultat.EstSucces);                  // un nom tout-espaces est aussi refusé
        Assert.Equal(MotifNomVide, resultat.Motif);        // ... avec le même motif métier
        Assert.Equal(Bruno, configuration.NomDe(ParentB)); // l'ancien nom est conservé (store inchangé)
    }

    // ---------- Test #3 — Caractérisation : aucune diffusion temps réel sur refus (early green attendu) ----------
    // ⚠️ early green ANTICIPÉ (cf. table 08-*.md) : la notification est déclenchée APRÈS mutation
    // réussie ; un refus retourne AVANT (par construction). Filet (Spy) documentant « pas de
    // diffusion sur échec » — verrouille l'invariant d'effet de bord (jamais d'écho sur une édition
    // refusée), pas un driver.
    [Fact]
    public void Should_Ne_declencher_aucune_diffusion_temps_reel_When_une_edition_est_refusee()
    {
        var configuration = new FakeConfigurationFoyer(new Dictionary<string, string> { [ParentB] = Bruno });
        var notificateur = new FakeNotificateurPlanning();
        var handler = new EditerActeurHandler(configuration, notificateur);

        var resultat = handler.Handle(new EditerActeurCommand(ParentB, Nom: ""));

        Assert.False(resultat.EstSucces);                     // l'édition est refusée
        Assert.Equal(0, notificateur.NombreDeNotifications);  // ... donc aucune diffusion n'est émise
    }
}
