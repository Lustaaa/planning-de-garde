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
/// Sprint 54 — S8 (🖥️ @ihm — acceptation de NIVEAU RUNTIME) : depuis la config par enfant, on déclare
/// (et retire) des PLAGES DE VACANCES sur une activité récurrente. Écran <see cref="ConfigurationFoyer"/>
/// réellement câblé à l'API distante réelle (<see cref="ApiDistanteFactory"/>, store réel, canal HTTP
/// réel). L'ajout / le retrait transitent par le canal d'écriture réel
/// (POST/DELETE /api/enfants/{id}/activites/recurrentes/{id}/exclusions) : la plage est PERSISTÉE dans le
/// store (donc la grille cesse / reprend de projeter l'activité sur l'intervalle, cf. S7). Anti
/// vert-qui-ment : observé sur le store réel via le port, jamais une mutation locale.
///
/// <para><b>Réserve gate G3</b> : la saisie des champs <c>input[type=date]</c> n'est pas exercée par bUnit
/// (limite connue) — le câblage POST est prouvé via les <b>dates par défaut</b> du formulaire ; la saisie
/// manuelle des dates sera confirmée au gate navigateur PO.</para>
/// </summary>
public sealed class FrontWasmConfigVacancesRecurrentRuntimeTests : TestContext
{
    private static void SeederFoyer(ApiDistanteFactory api)
    {
        api.Services.GetRequiredService<IEditeurActivites>().Ajouter("ecole", "École");
        api.Services.GetRequiredService<IEditeurEnfants>().Ajouter("lea", "Léa");
    }

    private IRenderedComponent<ConfigurationFoyer> RendreEtOuvrirVacances(ApiDistanteFactory api)
    {
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton(new SessionPlanning());
        var config = RenderComponent<ConfigurationFoyer>();
        config.WaitForState(() => config.FindAll("[data-testid='onglet-recurrents']").Count > 0, TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => config.Find("[data-testid='onglet-recurrents']").Click());
        config.WaitForState(() => config
            .FindAll("[data-testid='onglet-enfant-recurrent']")
            .Any(o => o.GetAttribute("data-enfant-id") == "lea"), TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => config
            .FindAll("[data-testid='onglet-enfant-recurrent']")
            .Single(o => o.GetAttribute("data-enfant-id") == "lea").Click());
        // Post-s54 : la gestion des vacances vit désormais DANS la dialog d'édition de la série (fusion, n°3) —
        // plus de 🏖️ ni de dialog Vacances autonome. On ouvre l'édition (crayon) et on attend la section vacances.
        config.WaitForAssertion(
            () => Assert.NotEmpty(config.FindAll("[data-testid='crayon-recurrent']")), TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => config.Find("[data-testid='crayon-recurrent']").Click());
        config.WaitForAssertion(
            () => Assert.NotEmpty(config.FindAll("[data-testid='section-vacances-recurrent']")), TimeSpan.FromSeconds(10));
        return config;
    }

    [Fact]
    public void Should_persister_une_plage_de_vacances_dans_le_store_When_on_l_ajoute_depuis_la_config()
    {
        // Given — un récurrent « École » pour Léa, sans aucune plage de vacances.
        using var api = new ApiDistanteFactory();
        SeederFoyer(api);
        api.Services.GetRequiredService<ISlotRecurrentRepository>().Enregistrer(
            SlotRecurrent.Poser("lea", "ecole", new[] { DayOfWeek.Monday }, new TimeSpan(8, 30, 0), new TimeSpan(16, 30, 0)).Valeur!);

        var config = RendreEtOuvrirVacances(api);

        // When — on ajoute une plage de vacances (dates par défaut du formulaire) via le canal d'écriture réel.
        this.SurDispatcher(() => config.Find("[data-testid='ajouter-vacances']").Click());

        // Then — la plage apparaît dans la liste relue ET a transité jusqu'au store réel (rempart anti vert-qui-ment).
        config.WaitForAssertion(
            () => Assert.NotEmpty(config.FindAll("[data-testid='vacances-plage']")), TimeSpan.FromSeconds(10));
        Assert.Single(api.Services.GetRequiredService<ISlotRecurrentRepository>().AllSnapshots().Single().Exclusions);
    }

    [Fact]
    public void Should_retirer_une_plage_de_vacances_du_store_When_on_la_supprime_depuis_la_config()
    {
        // Given — un récurrent « École » pour Léa AVEC une plage de vacances déjà rattachée.
        using var api = new ApiDistanteFactory();
        SeederFoyer(api);
        api.Services.GetRequiredService<ISlotRecurrentRepository>().Enregistrer(
            SlotRecurrent.Poser("lea", "ecole", new[] { DayOfWeek.Monday }, new TimeSpan(8, 30, 0), new TimeSpan(16, 30, 0)).Valeur!
                .AjouterExclusion(new DateOnly(2026, 6, 29), new DateOnly(2026, 7, 5)));

        var config = RendreEtOuvrirVacances(api);

        // … la plage est listée dans la modal vacances.
        config.WaitForAssertion(
            () => Assert.Single(config.FindAll("[data-testid='vacances-plage']")), TimeSpan.FromSeconds(10));

        // When — on retire la plage via le canal d'écriture réel.
        this.SurDispatcher(() => config.Find("[data-testid='retirer-vacances']").Click());

        // Then — la plage disparaît de la liste relue ET du store réel (l'activité reprend sur l'intervalle).
        config.WaitForAssertion(
            () => Assert.Empty(config.FindAll("[data-testid='vacances-plage']")), TimeSpan.FromSeconds(10));
        Assert.Empty(api.Services.GetRequiredService<ISlotRecurrentRepository>().AllSnapshots().Single().Exclusions);
    }
}
