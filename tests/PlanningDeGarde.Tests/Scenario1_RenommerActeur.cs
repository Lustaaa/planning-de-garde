using System.Collections.Generic;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 08 — Sc.1 — Renommer un acteur : la case et la légende suivent (@nominal, 🖥️ IHM)
//   Tranche BACKEND (tdd-auto) : store mutable « renommer », handler EditerActeur, diffusion
//   temps réel sur succès (Spy). L'acceptation runtime (case + légende qui suivent SANS
//   rechargement, front WASM + API distante + SignalR réel) est menée séparément par ihm-builder.
//
//   On NE teste PAS ici un rendu Blazor : on pilote (1) le store réel qui remplace le
//   dictionnaire static readonly du Foyer par une valeur éditable sur l'id stable, (2) le
//   handler qui applique l'édition et confirme l'effet, (3) la notification émise sur succès.
public class Scenario1_RenommerActeur
{
    private const string ParentA = "parent-a";
    private const string Alice = "Alice";   // seed en dur (Foyer.NomsParResponsable)
    private const string Alicia = "Alicia"; // nom édité

    // ---------- Test #1 — Driver : store mutable seedé, NomDe reflète la dernière écriture ----------
    // Contradiction : aujourd'hui le nom est lu d'un dictionnaire static readonly
    // (Foyer.NomsParResponsable) — aucun moyen de renommer, la valeur résolue est immuable.
    // Force un store seedé ÉDITABLE dont NomDe(id) reflète la dernière écriture du nom.
    [Fact]
    public void Should_Resoudre_le_nouveau_nom_pour_l_identifiant_stable_When_un_acteur_deja_seme_est_renomme_dans_le_store_de_configuration()
    {
        var store = new ConfigurationFoyerEnMemoire();
        Assert.Equal(Alice, store.NomDe(ParentA)); // seed d'origine (miroir du Foyer)

        store.Renommer(ParentA, Alicia);

        Assert.Equal(Alicia, store.NomDe(ParentA)); // la lecture suit la dernière écriture
    }

    // ---------- Test #2 — Driver : la commande applique le nouveau nom et confirme l'effet ----------
    // Contradiction : il n'existe aucune commande/handler EditerActeur — rien ne mute le store.
    // Force l'orchestration : un acteur connu renommé avec un nom non vide voit le store muté
    // (via le port d'écriture) et la confirmation de l'effet renvoyée.
    [Fact]
    public void Should_Appliquer_le_nouveau_nom_et_confirmer_l_effet_When_la_commande_renomme_un_acteur_connu_avec_un_nom_non_vide()
    {
        var configuration = new FakeConfigurationFoyer(new Dictionary<string, string> { [ParentA] = Alice });
        var handler = new EditerActeurHandler(configuration, new FakeNotificateurPlanning());

        var resultat = handler.Handle(new EditerActeurCommand(ParentA, Alicia));

        Assert.True(resultat.EstSucces);                       // l'effet est confirmé
        Assert.Equal(Alicia, resultat.Valeur!.Nom);            // ... porteur du nom appliqué
        Assert.Equal(ParentA, resultat.Valeur!.ActeurId);      // ... sur l'identifiant stable inchangé
        Assert.Equal(Alicia, configuration.NomDe(ParentA));    // le store a réellement été muté
    }

    // ---------- Test #3 — Driver : un renommage abouti déclenche la diffusion temps réel (Spy) ----------
    // Contradiction : sans câblage, l'édition aboutie ne notifie personne — les autres grilles ne
    // suivraient pas. Force le déclenchement de INotificateurPlanning UNE FOIS sur succès (jamais
    // une écriture par le canal de diffusion).
    [Fact]
    public void Should_Declencher_la_diffusion_temps_reel_une_fois_When_un_renommage_aboutit()
    {
        var configuration = new FakeConfigurationFoyer(new Dictionary<string, string> { [ParentA] = Alice });
        var notificateur = new FakeNotificateurPlanning();
        var handler = new EditerActeurHandler(configuration, notificateur);

        handler.Handle(new EditerActeurCommand(ParentA, Alicia));

        Assert.Equal(1, notificateur.NombreDeNotifications); // diffusion déclenchée exactement une fois
    }
}
