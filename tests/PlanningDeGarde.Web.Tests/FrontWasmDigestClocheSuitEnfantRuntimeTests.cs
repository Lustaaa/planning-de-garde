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
/// Sprint 53 — Sc.8 (🖥️ @ihm) — NIVEAU RUNTIME : la SECTION DIGEST de la cloche SUIT l'enfant sélectionné
/// (digest filtré par enfant, P3), tandis que le FLUX de notifications lu/non-lu reste GÉNÉRAL/transverse.
/// La grille réelle (<see cref="PlanningPartage"/>) republie le digest reprojeté à chaque bascule de sélecteur ;
/// la cloche (rendue à part comme dans le layout) le rend. Anti « vert qui ment » : digest issu de la VRAIE
/// projection isolée par enfant, flux issu du VRAI journal — jamais une doublure.
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmDigestClocheSuitEnfantRuntimeTests : TestContext
{
    private static readonly DateTime Lundi_29_06_2026 = new(2026, 6, 29);
    private const string LeaId = "Léa";
    private const string TomId = "tom";
    // 29/06/2026 (aujourd'hui) : ISO 27 → index 1 → fond parent-b (Bruno). Surcharge Léa ce jour = parent-a (Alice).

    private (IRenderedComponent<PlanningPartage> grille, IRenderedComponent<Cloche> cloche) RendreLayout(ApiDistanteFactory api)
    {
        var session = new SessionPlanning();
        session.Connecter("Alice", "parent-a", TypeActeur.Parent); // MonId = parent-a (les notifs le concernant s'affichent)
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
        grille.WaitForState(() => grille.FindAll("[data-testid='jour-case']").Count == 28, TimeSpan.FromSeconds(10));
        var cloche = RenderComponent<Cloche>();
        return (grille, cloche);
    }

    private static void Selectionner(IRenderedComponent<PlanningPartage> grille, string enfantId)
        => grille.Find("[data-testid='selecteur-enfant-carte']").Change(enfantId);

    [Fact]
    public void La_section_digest_suit_l_enfant_selectionne_le_flux_de_notifications_reste_transverse()
    {
        using var api = new ApiDistanteFactory();
        // Given — 2e enfant + cycle partagé N=2 (Alice/Bruno) + surcharge Léa aujourd'hui (Alice, alors que le fond est Bruno).
        api.Services.GetRequiredService<IEditeurEnfants>().Ajouter(TomId, "Tom");
        GrilleRuntimeHarness.SemerCycle(api, new CycleDeFond(2, new Dictionary<int, string> { [0] = "parent-a", [1] = "parent-b" }));
        api.Services.GetRequiredService<IPeriodeRepository>().Enregistrer(
            PeriodeDeGarde.Affecter("parent-a", Lundi_29_06_2026, Lundi_29_06_2026, LeaId).Valeur!);
        // Une notification TRANSVERSE (délégation concernant parent-a, à propos de Léa) : elle doit rester visible
        // quel que soit l'enfant sélectionné.
        api.Services.GetRequiredService<IJournalChangements>().Consigner(new EvenementChangementSnapshot(
            Guid.NewGuid().ToString("N"), TypeChangement.Delegation, new DateOnly(2026, 7, 3), LeaId, "parent-b", "parent-a", Lundi_29_06_2026));

        var (grille, cloche) = RendreLayout(api);

        // When — j'ouvre la cloche et je sélectionne Léa.
        cloche.WaitForElement("[data-testid='cloche-bouton']", TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => cloche.Find("[data-testid='cloche-bouton']").Click());
        Selectionner(grille, LeaId);

        // Then — le digest immédiat résout le responsable de LÉA (Alice, surcharge) ; la notif transverse est là.
        cloche.WaitForAssertion(
            () => Assert.Contains("Alice", cloche.Find("[data-testid='digest-immediat-responsable']").TextContent),
            TimeSpan.FromSeconds(10));
        Assert.NotEmpty(cloche.FindAll("[data-testid='cloche-notif']"));
        var notifsAvant = cloche.FindAll("[data-testid='cloche-notif']").Count;

        // When — je bascule sur Tom.
        Selectionner(grille, TomId);

        // Then — le digest se RECOMPOSE pour Tom (fond Bruno, plus Alice) ; le flux de notifications reste IDENTIQUE (transverse).
        cloche.WaitForAssertion(
            () => Assert.Contains("Bruno", cloche.Find("[data-testid='digest-immediat-responsable']").TextContent),
            TimeSpan.FromSeconds(10));
        Assert.DoesNotContain("Alice", cloche.Find("[data-testid='digest-immediat-responsable']").TextContent);
        Assert.Equal(notifsAvant, cloche.FindAll("[data-testid='cloche-notif']").Count); // flux transverse inchangé

        // And — LECTURE STRICTE de la section digest (aucun bouton / lien / champ).
        var digest = cloche.Find("[data-testid='digest-section']");
        Assert.Empty(digest.QuerySelectorAll("button"));
        Assert.Empty(digest.QuerySelectorAll("a"));
        Assert.Empty(digest.QuerySelectorAll("input"));
    }
}
