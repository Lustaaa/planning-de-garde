using System;
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
/// Sprint 43 — Sc.4 (🖥️ @ihm) — acceptation de NIVEAU RUNTIME : le panneau « À venir » est rendu SOUS la
/// carte « Aujourd'hui » du planning réellement câblé (API distante réelle, projection
/// <c>GrilleAgendaQuery</c> réelle). Il liste les JOURS À VENIR de la fenêtre chargée (par date croissante),
/// chacun portant le QUI (responsable résolu, couleur de la grille), le OÙ (slot de l'enfant sélectionné) et
/// le TRANSFERT éventuel (bicolore réutilisé s29). STRICTEMENT en lecture : aucun contrôle d'édition.
///
/// Anti « vert qui ment » : le panneau est REPROJETÉ CLIENT depuis la grille RÉELLE lue via HTTP — si le
/// câblage, la résolution ou le rendu manquaient, les observables (jour, nom, slot, transfert) seraient
/// absents → rouge. Profil de jours réaliste : un jour résolu, un jour neutre, un jour avec transfert.
/// </summary>
public sealed class FrontWasmAVenirPanneauRuntimeTests : TestContext
{
    private static readonly DateTime Aujourdhui = GrilleRuntimeHarness.Lundi_29_06_2026; // Lundi 29/06/2026
    private static readonly DateTime Mardi_30_06 = new(2026, 6, 30);   // à venir : responsable + slot
    private static readonly DateTime Mercredi_01_07 = new(2026, 7, 1); // à venir : transfert bicolore

    [Fact]
    public void Le_panneau_a_venir_rend_qui_ou_transfert_sous_la_carte_en_lecture_seule()
    {
        // Given — l'API distante réelle porte, sur des jours À VENIR (après le 29/06) : une période parent-a
        // (« Alice », bleu) le 30/06 + un slot de Léa à l'école le 30/06 + un transfert parent-a → parent-b le 01/07.
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerPeriode(api, "parent-a", Mardi_30_06, Mardi_30_06);
        GrilleRuntimeHarness.SemerSlot(api, "Léa", "école",
            new DateTime(2026, 6, 30, 8, 30, 0), new DateTime(2026, 6, 30, 16, 30, 0));
        GrilleRuntimeHarness.SemerTransfert(api, "parent-a", "parent-b", Mercredi_01_07);

        // When — la grille réellement câblée est affichée (fenêtre 4 semaines glissantes depuis le 29/06).
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Aujourdhui);

        // Then — un panneau « À venir » est rendu SOUS la carte « Aujourd'hui » (carte AVANT panneau dans le DOM).
        var panneau = grille.Find("[data-testid='panneau-a-venir']");
        var markup = grille.Markup;
        Assert.True(
            markup.IndexOf("carte-aujourdhui", StringComparison.Ordinal)
                < markup.IndexOf("panneau-a-venir", StringComparison.Ordinal),
            "le panneau « À venir » doit suivre la carte « Aujourd'hui » (SOUS elle)");

        // … la liste des jours à venir est ordonnée par date croissante et strictement après aujourd'hui.
        var jours = panneau.QuerySelectorAll("[data-testid='a-venir-jour']")
            .Select(j => j.GetAttribute("data-date")!).ToList();
        Assert.NotEmpty(jours);
        Assert.Equal(jours.OrderBy(d => d).ToList(), jours);
        Assert.All(jours, d => Assert.True(string.CompareOrdinal(d, "2026-06-29") > 0));

        // … le 30/06 : le QUI (Alice, bleu) résolu de la grille + le OÙ (slot de Léa à l'école).
        var jour30 = panneau.QuerySelectorAll("[data-testid='a-venir-jour']")
            .Single(j => j.GetAttribute("data-date") == "2026-06-30");
        var responsable = jour30.QuerySelector("[data-testid='a-venir-responsable']")!;
        Assert.Equal("Alice", responsable.TextContent.Trim());
        Assert.Equal("bleu", responsable.GetAttribute("data-couleur"));
        Assert.Contains("école", jour30.QuerySelector("[data-testid='a-venir-slot']")!.TextContent);

        // … le 01/07 : le TRANSFERT bicolore (cédant Alice → recevant Bruno, couleurs de la grille).
        var jour01 = panneau.QuerySelectorAll("[data-testid='a-venir-jour']")
            .Single(j => j.GetAttribute("data-date") == "2026-07-01");
        var transfert = jour01.QuerySelector("[data-testid='a-venir-transfert-bicolore']")!;
        Assert.Equal("Alice", jour01.QuerySelector("[data-testid='a-venir-transfert-cedant']")!.TextContent.Trim());
        Assert.Equal("Bruno", jour01.QuerySelector("[data-testid='a-venir-transfert-recevant']")!.TextContent.Trim());
        Assert.Equal("bleu", transfert.GetAttribute("data-couleur-depart"));
        Assert.Equal("orange", transfert.GetAttribute("data-couleur-arrivee"));

        // … un jour neutre (aucun responsable résolu) affiche « Personne assignée » sans nom fantôme.
        var jourNeutre = panneau.QuerySelectorAll("[data-testid='a-venir-jour']")
            .First(j => j.QuerySelector("[data-testid='a-venir-qui-neutre']") is not null);
        Assert.Contains("Personne assignée", jourNeutre.TextContent);

        // … le panneau reste une surface de LECTURE : ses seuls contrôles interactifs sont les actions
        //    « déléguer » par jour (s44, Parent-gated) qui OUVRENT un mini-dialog — aucune écriture dans le
        //    panneau, aucun autre contrôle d'édition. (L'Invité ne voit AUCUN bouton — gating prouvé ci-dessous.)
        Assert.Empty(panneau.QuerySelectorAll("button:not([data-testid='a-venir-deleguer'])"));
    }

    [Fact]
    public void L_invite_voit_le_panneau_a_venir_en_lecture_seule()
    {
        // Given — un responsable résolu sur un jour à venir (parent-a le 30/06), grille câblée réelle.
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerPeriode(api, "parent-a", Mardi_30_06, Mardi_30_06);
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Aujourdhui);

        // When — l'identité effective bascule en Invité (consultation seule) et la vue est re-rendue.
        var session = Services.GetRequiredService<SessionPlanning>();
        session.Role = RoleAuteur.Invite;
        grille.Render();

        // Then — l'Invité VOIT le panneau (lecture non gatée) avec le « qui » résolu, sans contrôle d'écriture.
        var panneau = grille.Find("[data-testid='panneau-a-venir']");
        var jour30 = panneau.QuerySelectorAll("[data-testid='a-venir-jour']")
            .Single(j => j.GetAttribute("data-date") == "2026-06-30");
        Assert.Equal("Alice", jour30.QuerySelector("[data-testid='a-venir-responsable']")!.TextContent.Trim());
        Assert.Empty(panneau.QuerySelectorAll("button"));
    }

    [Fact]
    public void Une_fenetre_sans_a_venir_affiche_un_message_neutre_aucun_evenement()
    {
        // Given — vue SEMAINE, « aujourd'hui » = dimanche 05/07 (dernier jour de la fenêtre 29/06→05/07) :
        // aucun jour de la fenêtre chargée n'est postérieur à aujourd'hui → aucun à-venir. Grille câblée réelle.
        using var api = new ApiDistanteFactory();
        var dimanche = new DateTime(2026, 7, 5);
        var grille = RendreGrilleSemaine(api, dimanche);

        // Then — le panneau affiche le message neutre « Aucun événement à venir », aucune ligne de jour.
        var panneau = grille.Find("[data-testid='panneau-a-venir']");
        Assert.Contains("Aucun événement à venir", panneau.QuerySelector("[data-testid='a-venir-vide']")!.TextContent);
        Assert.Empty(panneau.QuerySelectorAll("[data-testid='a-venir-jour']"));
    }

    /// <summary>Rend la grille réelle en vue SEMAINE (7 cases) à la date injectée — variante de
    /// <see cref="GrilleRuntimeHarness.RendreGrille"/> pour prouver l'à-venir vide (fenêtre courte).</summary>
    private IRenderedComponent<PlanningPartage> RendreGrilleSemaine(ApiDistanteFactory api, DateTime aujourdhui)
    {
        var session = GrilleRuntimeHarness.SessionConnectee();
        session.Vue = VuePlanning.Semaine;
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton(session);
        Services.AddSingleton<IDateTimeProvider>(new DateTimeProviderFige(aujourdhui));
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
            () => grille.FindAll("[data-testid='jour-case']").Count == 7,
            TimeSpan.FromSeconds(10));
        return grille;
    }
}
