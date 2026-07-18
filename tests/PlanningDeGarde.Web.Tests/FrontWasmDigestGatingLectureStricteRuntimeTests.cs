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
/// Sprint 50 — Sc.8 (🖥️ @ihm) — acceptation de NIVEAU RUNTIME : le digest est Parent-gated (aligné sur la cloche
/// s47) et PUREMENT en lecture. Un Invité ne voit NI la cloche NI le digest ; hors session (page /connexion)
/// aucune cloche ni digest n'est rendu ; et quand un Parent voit le digest, il ne porte AUCUNE action de suivi,
/// aucun bouton, aucune entrée cliquable. Câblage réel (store / projection / canal / SignalR TestServer).
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmDigestGatingLectureStricteRuntimeTests : TestContext
{
    private static readonly DateTime Lundi_29_06_2026 = new(2026, 6, 29);

    private void CablerHubEtEtat(ApiDistanteFactory api, SessionPlanning session)
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
    }

    [Fact]
    public void Un_invite_ne_voit_ni_la_cloche_ni_le_digest()
    {
        using var api = new ApiDistanteFactory();
        var session = new SessionPlanning();
        session.Connecter("Alice", "parent-a", TypeActeur.Parent);
        session.Role = RoleAuteur.Invite; // consultation seule → non-Parent effectif
        CablerHubEtEtat(api, session);

        var cloche = RenderComponent<Cloche>();

        // Parent-gated : ni entrée cloche, ni section digest.
        Assert.Empty(cloche.FindAll("[data-testid='cloche-bouton']"));
        Assert.Empty(cloche.FindAll("[data-testid='digest-section']"));
    }

    [Fact]
    public void Hors_session_page_connexion_aucune_cloche_ni_digest()
    {
        using var api = new ApiDistanteFactory();
        CablerHubEtEtat(api, new SessionPlanning()); // aucune session ouverte (état de la page /connexion)

        var cloche = RenderComponent<Cloche>();

        Assert.Empty(cloche.FindAll("[data-testid='cloche-bouton']"));
        Assert.Empty(cloche.FindAll("[data-testid='digest-section']"));
    }

    [Fact]
    public void Un_Parent_voit_le_digest_strictement_en_lecture_sans_aucune_action()
    {
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerCycle(api, new CycleDeFond(2, new Dictionary<int, string> { [0] = "parent-a", [1] = "parent-b" }));
        var session = new SessionPlanning();
        session.Connecter("Alice", "parent-a", TypeActeur.Parent);
        CablerHubEtEtat(api, session);

        // La grille publie le digest ; la cloche le rend. On ouvre le panneau.
        var grille = RenderComponent<PlanningPartage>();
        grille.WaitForState(() => grille.FindAll("[data-testid='jour-case']").Count == 28, TimeSpan.FromSeconds(10));
        var cloche = RenderComponent<Cloche>();
        this.SurDispatcher(() => cloche.Find("[data-testid='cloche-bouton']").Click());

        cloche.WaitForAssertion(
            () => Assert.Contains("Récupère", cloche.Find("[data-testid='digest-immediat']").TextContent),
            TimeSpan.FromSeconds(10));

        // LECTURE STRICTE : la section digest ne porte aucune action de suivi (bouton / lien / champ / cliquable).
        var digest = cloche.Find("[data-testid='digest-section']");
        Assert.Empty(digest.QuerySelectorAll("button"));
        Assert.Empty(digest.QuerySelectorAll("a"));
        Assert.Empty(digest.QuerySelectorAll("input"));
        Assert.Empty(digest.QuerySelectorAll("[role='button']"));
    }
}
