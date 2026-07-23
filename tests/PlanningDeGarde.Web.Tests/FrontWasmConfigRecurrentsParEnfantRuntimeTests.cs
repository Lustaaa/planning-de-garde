using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 54 — S6 (🖥️ @ihm — acceptation de NIVEAU RUNTIME) : la Config foyer, navigation PAR ENFANT,
/// liste les activités récurrentes de l'enfant sélectionné (lieu + jours + plage) et permet de les
/// SUPPRIMER (comble le trou s31). Écran <see cref="ConfigurationFoyer"/> réellement câblé à l'API
/// distante réelle (<see cref="ApiDistanteFactory"/>, store réel, canal HTTP réel). La suppression
/// transite par le canal d'écriture réel (DELETE /api/enfants/{id}/activites/recurrentes/{id}) : le
/// récurrent disparaît de la liste relue (GET) ET du store (donc de la grille projetée). Aucune doublure
/// sur le chemin observé.
/// </summary>
public sealed class FrontWasmConfigRecurrentsParEnfantRuntimeTests : TestContext
{
    [Fact]
    public void Should_lister_les_recurrents_de_l_enfant_puis_les_supprimer_depuis_la_config_par_enfant()
    {
        // Given — le foyer connaît le lieu « École » et l'enfant « Léa » ; un récurrent multi-jours (lun/mar)
        // est posé pour Léa dans le store réel de l'API distante.
        using var api = new ApiDistanteFactory();
        api.Services.GetRequiredService<IEditeurActivites>().Ajouter("ecole", "École");
        api.Services.GetRequiredService<IEditeurEnfants>().Ajouter("lea", "Léa");
        api.Services.GetRequiredService<ISlotRecurrentRepository>().Enregistrer(
            SlotRecurrent.Poser("lea", "ecole", new[] { DayOfWeek.Monday, DayOfWeek.Tuesday }, new TimeSpan(8, 30, 0), new TimeSpan(16, 30, 0)).Valeur!);

        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton(new SessionPlanning());

        var config = RenderComponent<ConfigurationFoyer>();
        config.WaitForState(() => config.FindAll("[data-testid='onglet-recurrents']").Count > 0, TimeSpan.FromSeconds(10));

        // When — on ouvre l'onglet « Activités récurrentes » et on sélectionne l'enfant Léa.
        this.SurDispatcher(() => config.Find("[data-testid='onglet-recurrents']").Click());
        this.SurDispatcher(() => config.Find("[data-testid='selecteur-enfant-recurrent']").Change("lea"));

        // Then — la liste relue (GET réel) porte le récurrent « École » de Léa avec ses jours (Lun, Mar).
        config.WaitForAssertion(
            () =>
            {
                var ligne = Assert.Single(config.FindAll("[data-testid='recurrent-ligne']"));
                Assert.Contains("École", ligne.TextContent);
                Assert.Contains("Lun", config.Find("[data-testid='recurrent-jours']").TextContent);
                Assert.Contains("Mar", config.Find("[data-testid='recurrent-jours']").TextContent);
            },
            TimeSpan.FromSeconds(10));

        // When — on supprime ce récurrent depuis l'IHM (canal d'écriture réel DELETE).
        this.SurDispatcher(() => config.Find("[data-testid='supprimer-recurrent']").Click());

        // Then — sans rechargement, la liste relue ne porte plus aucun récurrent pour Léa.
        config.WaitForAssertion(
            () => Assert.Empty(config.FindAll("[data-testid='recurrent-ligne']")),
            TimeSpan.FromSeconds(10));

        // … et il a réellement disparu du STORE de l'API distante (donc de la grille projetée) — rempart
        // anti vert-qui-ment : observé via le port réel, pas une mutation locale de la liste.
        Assert.Empty(api.Services.GetRequiredService<ISlotRecurrentRepository>().AllSnapshots());
    }

    [Fact]
    public void Should_creer_une_serie_multi_jours_depuis_la_config_par_enfant_transitant_jusqu_au_store()
    {
        // Given — le foyer connaît le lieu « École » et l'enfant « Léa », sans aucun récurrent.
        using var api = new ApiDistanteFactory();
        api.Services.GetRequiredService<IEditeurActivites>().Ajouter("ecole", "École");
        api.Services.GetRequiredService<IEditeurEnfants>().Ajouter("lea", "Léa");
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton(new SessionPlanning());

        var config = RenderComponent<ConfigurationFoyer>();
        config.WaitForState(() => config.FindAll("[data-testid='onglet-recurrents']").Count > 0, TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => config.Find("[data-testid='onglet-recurrents']").Click());
        this.SurDispatcher(() => config.Find("[data-testid='selecteur-enfant-recurrent']").Change("lea"));
        config.WaitForAssertion(
            () => Assert.NotEmpty(config.FindAll("[data-testid='bouton-ajouter-recurrent']")), TimeSpan.FromSeconds(10));

        // When — on ouvre la modal d'ajout, choisit « École », coche {lundi, jeudi} (plage par défaut
        // 08:30→16:30 du formulaire) et enregistre.
        this.SurDispatcher(() => config.Find("[data-testid='bouton-ajouter-recurrent']").Click());
        this.SurDispatcher(() => config.Find("[data-testid='champ-lieu-recurrent']").Change("ecole"));
        this.SurDispatcher(() => config.Find("[data-testid='jour-Monday']").Change(true));
        this.SurDispatcher(() => config.Find("[data-testid='jour-Thursday']").Change(true));
        this.SurDispatcher(() => config.Find("#form-recurrent").Submit());

        // Then — la série apparaît dans la liste relue ET a transité jusqu'au store réel avec son set {lun, jeu}
        // et la plage du formulaire (08:30→16:30) — preuve du câblage POST multi-jours de bout en bout.
        config.WaitForAssertion(
            () => Assert.Single(config.FindAll("[data-testid='recurrent-ligne']")), TimeSpan.FromSeconds(10));
        var enregistre = Assert.Single(api.Services.GetRequiredService<ISlotRecurrentRepository>().AllSnapshots());
        Assert.Equal("lea", enregistre.EnfantId);
        Assert.Equal("ecole", enregistre.LieuId);
        Assert.Equal(new[] { DayOfWeek.Monday, DayOfWeek.Thursday }, enregistre.JoursDeSemaine);
        Assert.Equal(new TimeSpan(8, 30, 0), enregistre.HeureDebut);
        Assert.Equal(new TimeSpan(16, 30, 0), enregistre.HeureFin);
    }

    [Fact]
    public void Should_masquer_les_affordances_creer_editer_supprimer_When_un_invite_consulte_la_config_par_enfant()
    {
        // Given — un récurrent de Léa dans le store, mais l'utilisateur est en consultation seule (Invité, R9).
        using var api = new ApiDistanteFactory();
        api.Services.GetRequiredService<IEditeurActivites>().Ajouter("ecole", "École");
        api.Services.GetRequiredService<IEditeurEnfants>().Ajouter("lea", "Léa");
        api.Services.GetRequiredService<ISlotRecurrentRepository>().Enregistrer(
            SlotRecurrent.Poser("lea", "ecole", new[] { DayOfWeek.Monday }, new TimeSpan(8, 30, 0), new TimeSpan(16, 30, 0)).Valeur!);
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        var session = new SessionPlanning { Role = RoleAuteur.Invite };
        Services.AddSingleton(session);

        var config = RenderComponent<ConfigurationFoyer>();
        config.WaitForState(() => config.FindAll("[data-testid='onglet-recurrents']").Count > 0, TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => config.Find("[data-testid='onglet-recurrents']").Click());
        this.SurDispatcher(() => config.Find("[data-testid='selecteur-enfant-recurrent']").Change("lea"));

        // Then — la LISTE reste visible en lecture (le récurrent est là), mais AUCUNE affordance d'écriture :
        // ni « Ajouter », ni crayon, ni corbeille.
        config.WaitForAssertion(
            () => Assert.NotEmpty(config.FindAll("[data-testid='recurrent-ligne']")), TimeSpan.FromSeconds(10));
        Assert.Empty(config.FindAll("[data-testid='bouton-ajouter-recurrent']"));
        Assert.Empty(config.FindAll("[data-testid='crayon-recurrent']"));
        Assert.Empty(config.FindAll("[data-testid='supprimer-recurrent']"));
    }
}
