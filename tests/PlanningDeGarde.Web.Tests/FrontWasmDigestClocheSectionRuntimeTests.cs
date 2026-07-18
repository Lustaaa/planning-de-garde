using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Web;
using PlanningDeGarde.Web.Components;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 50 — Sc.5 (🖥️ @ihm) — acceptation de NIVEAU RUNTIME : la SECTION DIGEST « immédiat » s'affiche EN TÊTE
/// du panneau déroulant de la cloche (barre d'application), AU-DESSUS du flux chrono de notifications s47. La
/// GRILLE réelle (<see cref="PlanningPartage"/>) chargée depuis l'API distante RÉELLE (store + projection +
/// référentiel réels) PUBLIE le digest reprojeté ; la CLOCHE, rendue à part (comme dans le layout), s'y abonne
/// et le rend — reprojection LECTURE stricte, aucune action / bouton / entrée cliquable, Parent-gated.
///
/// Anti « vert qui ment » : le digest provient de la VRAIE projection (cycle de fond réel → responsable résolu +
/// bascules dérivées s31), relue par le canal de lecture réel — jamais une doublure.
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmDigestClocheSectionRuntimeTests : TestContext
{
    private static readonly DateTime Lundi_29_06_2026 = new(2026, 6, 29);

    /// <summary>Câble les services PARTAGÉS (scoped/singleton) du layout : un seul client, une session Parent
    /// connectée, l'horloge figée, le hub redirigé vers l'API réelle, et l'ÉTAT DIGEST partagé (grille → cloche).</summary>
    private (IRenderedComponent<PlanningPartage> grille, IRenderedComponent<Cloche> cloche) RendreLayout(
        ApiDistanteFactory api, SessionPlanning session)
    {
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton(session);
        Services.AddSingleton<IDateTimeProvider>(new DateTimeProviderFige(Lundi_29_06_2026));
        Services.AddSingleton(new EtatDigestPartage());
        Services.AddSingleton(new OptionsConnexionHub
        {
            Configurer = options =>
            {
                options.HttpMessageHandlerFactory = _ => api.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            },
        });

        var grille = RenderComponent<PlanningPartage>();
        grille.WaitForState(
            () => grille.FindAll("[data-testid='jour-case']").Count == 28, TimeSpan.FromSeconds(10));
        var cloche = RenderComponent<Cloche>();
        return (grille, cloche);
    }

    private static SessionPlanning SessionParent()
    {
        var session = new SessionPlanning();
        session.Connecter("Alice", "parent-a", TypeActeur.Parent);
        return session;
    }

    [Fact]
    public void La_section_digest_s_affiche_en_tete_du_panneau_au_dessus_du_flux_chrono_en_lecture_stricte()
    {
        using var api = new ApiDistanteFactory();
        // Cycle de fond réel N=2 → responsable résolu chaque jour + bascules dérivées aux frontières de semaine.
        GrilleRuntimeHarness.SemerCycle(api, new CycleDeFond(2, new Dictionary<int, string> { [0] = "parent-a", [1] = "parent-b" }));

        var (_, cloche) = RendreLayout(api, SessionParent());

        // When — j'ouvre le panneau déroulant de la cloche.
        cloche.WaitForElement("[data-testid='cloche-bouton']", TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => cloche.Find("[data-testid='cloche-bouton']").Click());

        // Then — une SECTION digest apparaît, EN TÊTE, AU-DESSUS du flux chrono (l'ordre DOM le prouve).
        cloche.WaitForAssertion(
            () =>
            {
                var panneau = cloche.Find("[data-testid='cloche-panneau']");
                var enfants = panneau.Children.ToList();
                var section = panneau.QuerySelector("[data-testid='digest-section']");
                var entete = panneau.QuerySelector(".pdg-cloche-entete");
                Assert.NotNull(section);
                Assert.NotNull(entete);
                // ordre DOM (enfants directs du panneau) : la section digest précède l'entête « Notifications ».
                Assert.True(
                    enfants.IndexOf(section!) < enfants.IndexOf(entete!),
                    "la section digest doit être rendue EN TÊTE, au-dessus du flux chrono de notifications");
            },
            TimeSpan.FromSeconds(10));

        // And — « qui récupère aujourd'hui » est résolu (un nom assigné, jamais un fantôme).
        cloche.WaitForAssertion(
            () =>
            {
                var immediat = cloche.Find("[data-testid='digest-immediat']");
                Assert.Contains("Récupère", immediat.TextContent);
                Assert.True(
                    immediat.TextContent.Contains("Alice") || immediat.TextContent.Contains("Bruno"),
                    $"responsable résolu attendu (Alice/Bruno), obtenu : {immediat.TextContent}");
            },
            TimeSpan.FromSeconds(10));

        // And — la sous-section « à venir » liste les transferts des jours à venir (bascules dérivées du cycle).
        cloche.WaitForAssertion(
            () => Assert.NotEmpty(cloche.FindAll("[data-testid='digest-avenir-jour']")),
            TimeSpan.FromSeconds(10));

        // And — LECTURE STRICTE : la section digest ne porte AUCUN bouton ni entrée cliquable.
        var digest = cloche.Find("[data-testid='digest-section']");
        Assert.Empty(digest.QuerySelectorAll("button"));
        Assert.Empty(digest.QuerySelectorAll("a"));
        Assert.Empty(digest.QuerySelectorAll("input"));
    }
}
