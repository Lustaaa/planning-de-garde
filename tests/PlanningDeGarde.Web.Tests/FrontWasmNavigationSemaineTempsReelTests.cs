using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Web.Components.Pages;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.1 (🖥️ IHM, <c>@nominal</c>) — naviguer d'une semaine vers le
/// futur ou le passé. Sur la <b>vraie</b> grille (<see cref="PlanningPartage"/>) câblée à une <b>API
/// distante réelle</b> (<see cref="ApiDistanteFactory"/> — store réel, projection
/// <c>GrilleAgendaQuery</c>, référentiel + palette réels du foyer), un acteur clique
/// « Semaine suivante » / « Semaine précédente » : la fenêtre se <b>décale d'une semaine</b> et
/// <b>re-projette</b> (re-requête HTTP réelle de l'API distante à la date naviguée), le fond se
/// <b>re-résout</b> à cette date, et <b>aucune écriture</b> n'est émise (lecture seule).
///
/// <para><b>Ancrage.</b> Aujourd'hui = mercredi 10/06/2026 (semaine en cours lundi 08/06, vue par défaut
/// 4 semaines). Un cycle de fond de 2 semaines est semé dans le store réel (index 0 → parent-a « Alice »
/// bleu, index 1 → parent-b « Bruno » orange) : les semaines ISO impaires (index 1) résolvent Bruno.
/// « Semaine suivante » → fenêtre démarrant au lundi 15/06 (ISO 25 impaire → Bruno) ; « Semaine
/// précédente » (deux pas depuis là) → lundi 01/06 (ISO 23 impaire → Bruno).</para>
///
/// <para><b>Anti « vert qui ment ».</b> Le décalage est observé sur la <b>première case rendue</b> de la
/// fenêtre (le coin haut-gauche = début de fenêtre), et le fond Bruno provient du référentiel réel
/// résolu côté API et transitant par le canal de lecture HTTP réel. Un <b>espion de transport</b>
/// vérifie qu'<b>aucun POST d'écriture</b> (<c>/api/canal/…</c>) n'a transité pendant la navigation. Un
/// bUnit à doublure de transport ne prouverait ni la re-requête réelle, ni l'absence d'écriture.</para>
/// </summary>
public sealed class FrontWasmNavigationSemaineTempsReelTests : TestContext
{
    private static readonly DateTime Mercredi_10_06_2026 = new(2026, 6, 10);

    [Fact]
    public void Should_Decaler_la_fenetre_d_une_semaine_au_lundi_15_06_puis_au_lundi_01_06_et_re_resoudre_le_fond_sur_Bruno_sans_emettre_d_ecriture_When_un_acteur_clique_Semaine_suivante_puis_Semaine_precedente_sur_l_app_reellement_cablee()
    {
        // Given — l'API distante réelle (store vierge : aucune période). On sème un cycle de fond de
        // 2 semaines : index 0 → parent-a (Alice, bleu), index 1 → parent-b (Bruno, orange). Les
        // semaines ISO impaires (index 1) résolvent donc le fond Bruno, sans aucune saisie de période.
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerCycle(api,
            new CycleDeFond(2, new Dictionary<int, string> { [0] = "parent-a", [1] = "parent-b" }));

        // … client HTTP du front pointé sur l'API distante réelle, mais ENVELOPPÉ d'un espion qui
        // enregistre toute écriture (POST /api/canal/…) — pour prouver « aucune écriture » au runtime.
        var espion = new EspionEcritureHandler(api.Server.CreateHandler());
        var client = new HttpClient(espion) { BaseAddress = api.Server.BaseAddress };

        // … la grille réelle câblée à l'API distante, aujourd'hui = mercredi 10/06/2026. La fenêtre par
        // défaut (4 semaines) démarre au lundi 08/06 (semaine en cours) — 28 cases attendues.
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Mercredi_10_06_2026, client);
        Assert.Equal("08/06", DatePremiereCase(grille));

        // When — clic « Semaine suivante » : la fenêtre se décale et l'API distante est re-requêtée à
        // la date naviguée (GET réel). On attend que la première case devienne le lundi 15/06.
        grille.Find("[data-testid='nav-semaine-suivante']").Click();
        grille.WaitForState(() => DatePremiereCase(grille) == "15/06", TimeSpan.FromSeconds(10));

        // Then — la fenêtre démarre au lundi 15/06 (ISO 25 impaire) et le fond s'y re-résout sur Bruno.
        var premiere = PremiereCase(grille);
        Assert.Equal("Bruno", premiere.QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
        Assert.Equal("orange", premiere.GetAttribute("data-couleur"));

        // When — deux pas « Semaine précédente » depuis le 15/06 ramènent au lundi 01/06 (08/06 puis 01/06).
        grille.Find("[data-testid='nav-semaine-precedente']").Click();
        grille.WaitForState(() => DatePremiereCase(grille) == "08/06", TimeSpan.FromSeconds(10));
        grille.Find("[data-testid='nav-semaine-precedente']").Click();
        grille.WaitForState(() => DatePremiereCase(grille) == "01/06", TimeSpan.FromSeconds(10));

        // Then — la fenêtre démarre au lundi 01/06 (ISO 23 impaire) et le fond s'y re-résout sur Bruno.
        var premierePrec = PremiereCase(grille);
        Assert.Equal("Bruno", premierePrec.QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
        Assert.Equal("orange", premierePrec.GetAttribute("data-couleur"));

        // … et AUCUNE écriture (POST /api/canal/…) n'a transité pendant toute la navigation : la
        // navigation est purement une re-projection en lecture (re-requête GET), jamais une écriture.
        Assert.Empty(espion.Ecritures);
    }

    // Première case rendue de la fenêtre = coin haut-gauche = début de fenêtre (Semaines[0].Jours[0]).
    private static IElement PremiereCase(IRenderedComponent<PlanningPartage> grille)
        => grille.FindAll("[data-testid='jour-case']")[0];

    private static string DatePremiereCase(IRenderedComponent<PlanningPartage> grille)
        => PremiereCase(grille).QuerySelector(".grille-jour-date")!.TextContent.Trim();

    /// <summary>
    /// Handler de transport qui relaie tout vers l'API distante réelle et <b>enregistre</b> toute
    /// écriture (POST vers le canal <c>/api/canal/…</c>). Le trafic SignalR (négociation/long polling
    /// sur <c>/hubs/…</c>) est ignoré : seules les écritures métier comptent pour « aucune écriture ».
    /// </summary>
    private sealed class EspionEcritureHandler : DelegatingHandler
    {
        public List<string> Ecritures { get; } = new();

        public EspionEcritureHandler(HttpMessageHandler inner) : base(inner) { }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Post
                && (request.RequestUri?.AbsolutePath.StartsWith("/api/canal", StringComparison.Ordinal) ?? false))
            {
                Ecritures.Add($"{request.Method} {request.RequestUri.AbsolutePath}");
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
