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
/// Acceptation de NIVEAU RUNTIME du Sc.4 (🖥️ IHM, <c>@limite</c>) — <b>retour à la semaine en cours
/// après navigation</b>. Sur la <b>vraie</b> grille (<see cref="PlanningPartage"/>) câblée à une <b>API
/// distante réelle</b> (<see cref="ApiDistanteFactory"/> — store réel, projection
/// <c>GrilleAgendaQuery</c>, référentiel + palette réels du foyer), un acteur navigue deux fois
/// « Semaine suivante » (fenêtre au lundi 22/06), puis clique « Aujourd'hui » : la fenêtre
/// <b>revient</b> sur la semaine en cours (lundi 08/06, fond Alice bleu) via une re-requête HTTP réelle
/// de l'API distante à l'ancre réinitialisée, et <b>aucune écriture</b> n'est émise (lecture seule).
///
/// <para><b>Ancrage.</b> Aujourd'hui = mercredi 10/06/2026 (semaine en cours lundi 08/06, vue par défaut
/// 4 semaines). Un cycle de fond de 2 semaines est semé dans le store réel (index 0 → parent-a « Alice »
/// bleu, index 1 → parent-b « Bruno » orange) : les semaines ISO paires (index 0) résolvent Alice. Le
/// lundi 08/06 est en ISO 24 (paire → Alice bleu). Deux « Semaine suivante » portent la fenêtre au lundi
/// 22/06 ; « Aujourd'hui » réinitialise l'ancre au lundi de la date du jour (08/06) via le port
/// d'horloge injecté, jamais <c>DateTime.Now</c>.</para>
///
/// <para><b>Anti « vert qui ment ».</b> Le retour est observé sur la <b>première case rendue</b> de la
/// fenêtre (coin haut-gauche = début de fenêtre), et le fond Alice provient du référentiel réel résolu
/// côté API et transitant par le canal de lecture HTTP réel. Un <b>espion de transport</b> vérifie
/// qu'<b>aucun POST d'écriture</b> (<c>/api/canal/…</c>) n'a transité pendant toute la navigation et le
/// retour. Un bUnit à doublure de transport ne prouverait ni la re-requête réelle à l'ancre
/// réinitialisée, ni l'absence d'écriture.</para>
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmRetourAujourdhuiTempsReelTests : TestContext
{
    private static readonly DateTime Mercredi_10_06_2026 = new(2026, 6, 10);

    [Fact]
    public void Should_Ramener_la_fenetre_au_lundi_08_06_2026_avec_le_fond_Alice_sans_emettre_d_ecriture_When_l_utilisateur_clique_Aujourd_hui_apres_avoir_navigue_deux_semaines_en_avant_sur_l_app_reellement_cablee()
    {
        // Given — l'API distante réelle (store vierge : aucune période). On sème un cycle de fond de
        // 2 semaines : index 0 → parent-a (Alice, bleu), index 1 → parent-b (Bruno, orange). Les
        // semaines ISO paires (index 0) résolvent donc le fond Alice, sans aucune saisie de période.
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerCycle(api,
            new CycleDeFond(2, new Dictionary<int, string> { [0] = "parent-a", [1] = "parent-b" }));

        // … client HTTP du front pointé sur l'API distante réelle, mais ENVELOPPÉ d'un espion qui
        // enregistre toute écriture (POST /api/canal/…) — pour prouver « aucune écriture » au runtime.
        var espion = new EspionEcritureHandler(api.Server.CreateHandler());
        var client = new HttpClient(espion) { BaseAddress = api.Server.BaseAddress };

        // … la grille réelle câblée à l'API distante, aujourd'hui = mercredi 10/06/2026. La fenêtre par
        // défaut (4 semaines) démarre au lundi 08/06 (semaine en cours, ISO 24 paire → Alice bleu).
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Mercredi_10_06_2026, client);
        Assert.Equal("08/06", DatePremiereCase(grille));

        // When — deux clics « Semaine suivante » portent la fenêtre au lundi 22/06 (08/06 → 15/06 → 22/06).
        this.SurDispatcher(() => grille.Find("[data-testid='nav-semaine-suivante']").Click());
        grille.WaitForState(() => DatePremiereCase(grille) == "15/06", TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => grille.Find("[data-testid='nav-semaine-suivante']").Click());
        grille.WaitForState(() => DatePremiereCase(grille) == "22/06", TimeSpan.FromSeconds(10));

        // When — clic « Aujourd'hui » : l'ancre est réinitialisée au lundi de la date du jour et l'API
        // distante est re-requêtée à cette ancre (GET réel). On attend le retour de la première case au 08/06.
        this.SurDispatcher(() => grille.Find("[data-testid='nav-aujourdhui']").Click());
        grille.WaitForState(() => DatePremiereCase(grille) == "08/06", TimeSpan.FromSeconds(10));

        // Then — la fenêtre redémarre au lundi 08/06 (ISO 24 paire) et le fond s'y re-résout sur Alice bleu.
        var premiere = PremiereCase(grille);
        Assert.Equal("Alice", premiere.QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
        Assert.Equal("bleu", premiere.GetAttribute("data-couleur"));

        // … et AUCUNE écriture (POST /api/canal/…) n'a transité pendant la navigation NI le retour : le
        // bouton « Aujourd'hui » est une pure re-projection en lecture (re-requête GET), jamais une écriture.
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
            if ((request.Method == HttpMethod.Post || request.Method == HttpMethod.Put || request.Method == HttpMethod.Delete)
                && (request.RequestUri?.AbsolutePath.StartsWith("/api/", StringComparison.Ordinal) ?? false))
            {
                Ecritures.Add($"{request.Method} {request.RequestUri.AbsolutePath}");
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
