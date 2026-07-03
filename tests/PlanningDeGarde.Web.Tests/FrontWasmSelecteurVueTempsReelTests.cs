using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Web.Components.Pages;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du sélecteur de vue (cohérence IHM finale du Sc.2/Sc.3 — bascule entre
/// vues prédéfinies). Sur la <b>vraie</b> grille (<see cref="PlanningPartage"/>) câblée à une <b>API
/// distante réelle</b> (<see cref="ApiDistanteFactory"/> — store réel, projection
/// <c>GrilleAgendaQuery</c> vue/span), un acteur change la vue dans le sélecteur : la fenêtre est
/// <b>re-requêtée</b> avec le paramètre de vue (GET réel) et se <b>redimensionne</b> — 4 semaines
/// glissantes (défaut, 28 cases) → Semaine (7 cases) → Mois (35 cases) — l'ancre lundi conservée, et
/// <b>aucune écriture</b> n'est émise (lecture seule).
///
/// <para><b>Ancrage.</b> Aujourd'hui = mercredi 10/06/2026 (semaine en cours lundi 08/06, défaut
/// 4 semaines). Semaine → 08→14/06 (7 cases). Mois → semaines ISO entières recouvrant juin 2026,
/// 01/06 → 05/07 (35 cases, 5 lignes), première case 01/06.</para>
///
/// <para><b>Anti « vert qui ment ».</b> Le redimensionnement est observé sur le nombre réel de cases
/// rendues, projeté par l'API distante via le canal de lecture HTTP réel (paramètre <c>?vue=</c>), et un
/// espion de transport vérifie qu'aucun POST d'écriture n'a transité pendant les bascules. Un bUnit à
/// doublure de transport ne prouverait ni la re-requête réelle ni l'absence d'écriture.</para>
/// </summary>
public sealed class FrontWasmSelecteurVueTempsReelTests : TestContext
{
    private static readonly DateTime Mercredi_10_06_2026 = new(2026, 6, 10);

    [Fact]
    public void Should_Redimensionner_la_fenetre_en_re_requetant_l_API_distante_avec_le_parametre_de_vue_28_puis_7_puis_35_cases_sans_emettre_d_ecriture_When_un_acteur_change_la_vue_dans_le_selecteur_sur_l_app_reellement_cablee()
    {
        // Given — l'API distante réelle, un cycle de fond de 2 semaines semé dans le store réel.
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerCycle(api,
            new CycleDeFond(2, new Dictionary<int, string> { [0] = "parent-a", [1] = "parent-b" }));

        // … client HTTP du front pointé sur l'API distante réelle, enveloppé d'un espion d'écriture.
        var espion = new EspionEcritureHandler(api.Server.CreateHandler());
        var client = new HttpClient(espion) { BaseAddress = api.Server.BaseAddress };

        // … la grille réelle, aujourd'hui = 10/06/2026 : fenêtre par défaut 4 semaines (28 cases rendues).
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Mercredi_10_06_2026, client);
        var selecteur = grille.Find("[data-testid='selecteur-vue']");
        Assert.Equal("4semaines", selecteur.GetAttribute("value"));

        // When — vue « Semaine » : re-requête réelle ?vue=semaine. Then — la fenêtre se réduit à 7 cases.
        this.SurDispatcher(() => grille.Find("[data-testid='selecteur-vue']").Change("semaine"));
        grille.WaitForState(() => grille.FindAll("[data-testid='jour-case']").Count == 7, TimeSpan.FromSeconds(10));
        Assert.Equal("08/06", DatePremiereCase(grille));

        // When — vue « Mois » : re-requête réelle ?vue=mois. Then — semaines ISO entières de juin 2026,
        // 35 cases (5 lignes), première case au lundi 01/06.
        this.SurDispatcher(() => grille.Find("[data-testid='selecteur-vue']").Change("mois"));
        grille.WaitForState(() => grille.FindAll("[data-testid='jour-case']").Count == 35, TimeSpan.FromSeconds(10));
        Assert.Equal("01/06", DatePremiereCase(grille));

        // … et AUCUNE écriture (POST /api/canal/…) n'a transité : changer de vue est pure re-projection.
        Assert.Empty(espion.Ecritures);
    }

    private static string DatePremiereCase(IRenderedComponent<PlanningPartage> grille)
        => grille.FindAll("[data-testid='jour-case']")[0].QuerySelector(".grille-jour-date")!.TextContent.Trim();

    /// <summary>Handler de transport qui relaie tout vers l'API distante réelle et enregistre toute
    /// écriture (POST vers le canal <c>/api/canal/…</c>) — pour prouver « aucune écriture » au runtime.</summary>
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
