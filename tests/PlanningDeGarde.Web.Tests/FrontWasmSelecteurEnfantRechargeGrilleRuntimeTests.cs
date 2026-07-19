using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Web;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 53 — Sc.7 (🖥️ @ihm) — NIVEAU RUNTIME : basculer le SÉLECTEUR D'ENFANT recharge la grille du BON enfant.
/// La grille réelle (<see cref="PlanningPartage"/>) est câblée à l'API distante RÉELLE (store + projection +
/// référentiel réels) : sélectionner un enfant relit la grille via le canal de lecture HTTP (paramètre enfant),
/// isolant la résolution — aucune case ne conserve la résolution de l'autre enfant. Anti « vert qui ment » : la
/// résolution provient de la VRAIE projection isolée par enfant (s53), jamais d'une doublure.
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmSelecteurEnfantRechargeGrilleRuntimeTests : TestContext
{
    private static readonly DateTime Lundi_29_06_2026 = new(2026, 6, 29);
    private const string LeaId = "Léa"; // enfant seedé au composition root (id stable = prénom)
    private const string TomId = "tom";
    // 01/07/2026 : semaine ISO 27 → index 27 % 2 = 1 → fond parent-b (Bruno). Surcharge Léa ce jour = parent-a (Alice).
    private static readonly DateTime Mercredi_01_07_2026 = new(2026, 7, 1);

    private IRenderedComponent<PlanningPartage> RendreGrille(ApiDistanteFactory api)
    {
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton(GrilleRuntimeHarness.SessionConnectee());
        Services.AddSingleton<IDateTimeProvider>(new DateTimeProviderFige(Lundi_29_06_2026));
        Services.AddSingleton(new OptionsConnexionHub
        {
            Configurer = options =>
            {
                options.HttpMessageHandlerFactory = _ => api.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            },
        });

        var grille = RenderComponent<PlanningPartage>();
        grille.WaitForState(() => grille.FindAll("[data-testid='jour-case']").Count == 28, TimeSpan.FromSeconds(10));
        return grille;
    }

    private static string ResponsableDe(IRenderedComponent<PlanningPartage> grille, DateTime jour)
    {
        var caseJour = grille.FindAll("[data-testid='jour-case']")
            .Single(c => c.GetAttribute("data-date") == jour.ToString("yyyy-MM-dd"));
        var nom = caseJour.QuerySelector("[data-testid='nom-responsable']");
        return nom?.TextContent.Trim() ?? "";
    }

    private static void Selectionner(IRenderedComponent<PlanningPartage> grille, string enfantId)
        => grille.Find("[data-testid='selecteur-enfant-carte']").Change(enfantId);

    [Fact]
    public void Basculer_le_selecteur_recharge_et_resout_la_grille_du_bon_enfant()
    {
        using var api = new ApiDistanteFactory();
        // Given — un 2e enfant (Tom) dans le référentiel réel + un cycle de fond partagé N=2 (Alice/Bruno).
        api.Services.GetRequiredService<IEditeurEnfants>().Ajouter(TomId, "Tom");
        GrilleRuntimeHarness.SemerCycle(api, new CycleDeFond(2, new Dictionary<int, string> { [0] = "parent-a", [1] = "parent-b" }));
        // Léa surchargée le 01/07 (Alice) alors que le FOND ce jour-là est Bruno → les deux enfants divergent.
        api.Services.GetRequiredService<IPeriodeRepository>().Enregistrer(
            PeriodeDeGarde.Affecter("parent-a", Mercredi_01_07_2026, Mercredi_01_07_2026, LeaId).Valeur!);

        var grille = RendreGrille(api);
        grille.WaitForElement("[data-testid='selecteur-enfant-carte']", TimeSpan.FromSeconds(10));

        // When — je sélectionne Léa : la case 01/07 résout SA surcharge (Alice).
        Selectionner(grille, LeaId);
        grille.WaitForAssertion(() => Assert.Equal("Alice", ResponsableDe(grille, Mercredi_01_07_2026)), TimeSpan.FromSeconds(10));

        // When — je sélectionne Tom : la grille RECHARGE et résout les cases de Tom (fond Bruno), aucune ne
        // conserve la résolution de Léa (Alice).
        Selectionner(grille, TomId);
        grille.WaitForAssertion(() => Assert.Equal("Bruno", ResponsableDe(grille, Mercredi_01_07_2026)), TimeSpan.FromSeconds(10));
        Assert.DoesNotContain(
            grille.FindAll("[data-testid='nom-responsable']"),
            n => n.TextContent.Contains("Alice") && ResponsableDe(grille, Mercredi_01_07_2026) == "Alice");
    }
}
