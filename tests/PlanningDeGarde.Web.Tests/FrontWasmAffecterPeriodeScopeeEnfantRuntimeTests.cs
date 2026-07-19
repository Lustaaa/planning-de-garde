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
/// Sprint 53 — Sc.11 (🖥️ @ihm) — NIVEAU RUNTIME : affecter une période avec un enfant SÉLECTIONNÉ estampille
/// la période de cet enfant → elle s'affiche dans SA grille et est ABSENTE de la grille d'un AUTRE enfant après
/// bascule ; la dialog affiche l'enfant courant en LECTURE SEULE (« Pour : … (sélection courante) », pas un
/// sélecteur). Réponse au gate G3 : « dans la définition d'une période on ne choisit pas d'enfant » (hérité) et
/// « en vue X le planning n'affiche QUE les périodes de X ». Câblage RÉEL (API + store + projection), anti « vert
/// qui ment » : l'écriture transite par le canal réel et la résolution isolée par enfant est réelle.
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmAffecterPeriodeScopeeEnfantRuntimeTests : TestContext
{
    private static readonly DateTime Lundi_29_06_2026 = new(2026, 6, 29);
    private const string LeaId = "Léa";
    private const string TomId = "tom";

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

    private static string? Nom(IRenderedComponent<PlanningPartage> grille, string jjMM)
        => GrilleRuntimeHarness.CaseDuJour(grille, jjMM).QuerySelector("[data-testid='nom-responsable']")?.TextContent.Trim();

    private void Selectionner(IRenderedComponent<PlanningPartage> grille, string enfantId)
        => this.SurDispatcher(() => grille.Find("[data-testid='selecteur-enfant-carte']").Change(enfantId));

    [Fact]
    public void Affecter_une_periode_avec_Lea_selectionnee_l_affiche_chez_Lea_et_l_absente_chez_Tom_dialog_enfant_lecture_seule()
    {
        using var api = new ApiDistanteFactory();
        api.Services.GetRequiredService<IEditeurEnfants>().Ajouter(TomId, "Tom");
        // Cycle partagé N=2 (Alice/Bruno) : le 29/06 (ISO 27 → index 1) résout Bruno par le fond pour les deux enfants.
        GrilleRuntimeHarness.SemerCycle(api, new CycleDeFond(2, new Dictionary<int, string> { [0] = "parent-a", [1] = "parent-b" }));

        var grille = RendreGrille(api);
        grille.WaitForElement("[data-testid='selecteur-enfant-carte']", TimeSpan.FromSeconds(10));
        Selectionner(grille, LeaId); // Léa courante
        grille.WaitForAssertion(() => Assert.Equal("Bruno", Nom(grille, "29/06")), TimeSpan.FromSeconds(10));

        // When — j'ouvre « Affecter une période » sur le 29/06 depuis le menu clic-case.
        grille.WaitForAssertion(
            () =>
            {
                this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "29/06").Click());
                this.SurDispatcher(() => grille.Find("[data-testid='menu-actions-case'] [data-testid='action-affecter-periode']").Click());
                Assert.NotEmpty(grille.FindAll("[data-testid='dialog-affecter-periode']"));
            },
            TimeSpan.FromSeconds(10));

        // Then — la dialog affiche l'enfant COURANT en LECTURE SEULE (pas de sélecteur d'enfant).
        var ligneEnfant = grille.Find("[data-testid='affecter-periode-enfant-courant']");
        Assert.Contains("Léa", ligneEnfant.TextContent);
        Assert.Empty(grille.FindAll("[data-testid='dialog-affecter-periode'] select[data-testid='selecteur-enfant-carte']"));

        // When — je choisis Alice (parent-a) et je valide.
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-affecter-periode'] [data-testid='champ-responsable']").Change("parent-a"));
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-affecter-periode'] form").Submit());

        // Then — en vue Léa, le 29/06 fait primer Alice (surcharge scopée à Léa).
        grille.WaitForAssertion(() => Assert.Equal("Alice", Nom(grille, "29/06")), TimeSpan.FromSeconds(10));

        // Then — en basculant sur Tom, la période de Léa est ABSENTE : le 29/06 retombe sur le fond (Bruno).
        Selectionner(grille, TomId);
        grille.WaitForAssertion(() => Assert.Equal("Bruno", Nom(grille, "29/06")), TimeSpan.FromSeconds(10));
        Assert.NotEqual("Alice", Nom(grille, "29/06"));
    }
}
