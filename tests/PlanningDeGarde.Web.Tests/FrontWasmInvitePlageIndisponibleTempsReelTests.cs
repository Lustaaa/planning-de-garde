using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.7 (🖥️ scénario IHM, <c>@erreur</c>) — <b>sélection de plage
/// indisponible en consultation seule</b>. En lecture seule (Invité), l'utilisateur <b>navigue librement</b>
/// (« Semaine suivante » décale la fenêtre au lundi 15/06/2026) MAIS toute tentative de sélectionner la
/// plage mardi 16/06 → mercredi 17/06 est <b>inerte</b> : aucun déclencheur d'écriture (bouton
/// <c>mode-plage</c> absent), aucune dialog d'affectation ne s'ouvre, et <b>aucune période n'est
/// enregistrée</b> (aucun <c>POST</c> d'écriture émis vers le canal). Navigation ouverte, écriture par
/// plage gatée (règle 9, gate <see cref="State.SessionPlanning.EstParent"/> mutualisé avec le menu
/// clic-case et le bouton de plage de Sc.5).
///
/// <para><b>Early green ANTICIPÉ (caractérisation, PAS un driver).</b> Le gate <c>EstParent</c> et le
/// trigger de plage <c>mode-plage</c> sont déjà posés par Sc.5 (bouton rendu <c>@if EstParent</c> ;
/// <c>BasculerModePlage</c>/<c>OuvrirMenu</c> sortent tôt en consultation). Ce test <b>caractérise</b>
/// l'inertie du gating mutualisé : il prouve que la navigation reste ouverte et que l'écriture par plage
/// est morte pour l'Invité, sans rien tirer en avant.</para>
///
/// <para><b>Non-vacuité (garde-fou CP — le « RED signifiant »).</b> Le test porte un <b>contrôle positif</b>
/// AVANT le négatif : en <b>Parent</b>, le déclencheur de plage <c>mode-plage</c> est bien <b>présent</b>.
/// Sans ce contrôle, le test passerait vacuously si le bouton était absent pour tous (gate cassé/inversé,
/// ou trigger jamais rendu). On bascule ensuite en Invité via le sélecteur de rôle réel et on prouve
/// l'absence du déclencheur + l'inertie du clic + <b>zéro écriture</b> (espion de transport sur
/// <c>/api/canal/</c>). Rendu sur la grille <b>réellement câblée</b> à l'API distante (DI réelle du rôle,
/// transport HTTP réel) — un bUnit à doublure ne prouverait ni le câblage distant ni l'absence de POST.</para>
/// </summary>
public sealed class FrontWasmInvitePlageIndisponibleTempsReelTests : TestContext
{
    private static readonly DateTime Mercredi_10_06_2026 = new(2026, 6, 10);

    [Fact]
    public void Should_Permettre_la_navigation_au_lundi_15_06_mais_n_ouvrir_aucun_declencheur_ni_dialog_d_affectation_et_n_enregistrer_aucune_periode_When_un_Invite_tente_de_selectionner_une_plage_de_cases_sur_l_app_reellement_cablee()
    {
        // Given — la grille réellement câblée à l'API distante, derrière un espion de transport qui compte
        // TOUT POST d'écriture (canal requête/réponse /api/canal/…). Aujourd'hui = mercredi 10/06/2026 →
        // fenêtre par défaut démarrant au lundi 08/06 (4 semaines glissantes). Affichée par défaut en Parent.
        using var api = new ApiDistanteFactory();
        var espion = new EspionEcritureHandler(api.Server.CreateHandler());
        var client = new HttpClient(espion) { BaseAddress = api.Server.BaseAddress };
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Mercredi_10_06_2026, client);

        // CONTRÔLE POSITIF (non-vacuité) — en Parent, le déclencheur de sélection de plage est PRÉSENT.
        // Sans cette borne, l'absence vérifiée plus bas pourrait être un faux vert (bouton jamais rendu).
        Assert.NotEmpty(grille.FindAll("[data-testid='mode-plage']"));
        // … témoin de fenêtre AVANT navigation : la première case affichée est le lundi 08/06.
        Assert.Equal("08/06", PremiereCaseDate(grille));

        // When — l'utilisateur bascule en « Invité (consultation seule) » via le sélecteur de rôle réel.
        this.SurDispatcher(() => grille.Find("select.form-select").Change("Invite"));

        // Then (1/3) — le déclencheur de plage DISPARAÎT (gate EstParent, le bouton n'est même plus rendu)
        // et le bandeau « lecture seule » est affiché. Sous WaitForAssertion pour laisser le re-render du
        // changement de rôle s'appliquer.
        grille.WaitForAssertion(
            () =>
            {
                Assert.Empty(grille.FindAll("[data-testid='mode-plage']"));
                Assert.Contains("lecture seule", grille.Markup, StringComparison.OrdinalIgnoreCase);
            },
            TimeSpan.FromSeconds(10));

        // When — l'Invité NAVIGUE librement : « Semaine suivante » décale la fenêtre de +7 jours.
        this.SurDispatcher(() => grille.Find("[data-testid='nav-semaine-suivante']").Click());

        // Then (2/3) — la navigation FONCTIONNE en consultation seule : la fenêtre démarre au lundi 15/06
        // (re-projection à la date naviguée, lecture seule). La case mardi 16/06 et mercredi 17/06 sont visibles.
        grille.WaitForState(() => PremiereCaseDate(grille) == "15/06", TimeSpan.FromSeconds(10));
        Assert.NotNull(GrilleRuntimeHarness.CaseDuJour(grille, "16/06"));
        Assert.NotNull(GrilleRuntimeHarness.CaseDuJour(grille, "17/06"));

        // When — l'Invité TENTE de sélectionner la plage mardi 16/06 → mercredi 17/06. Le bouton de mode
        // plage étant absent, il clique directement les deux cases (le seul geste possible).
        this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "16/06").Click());
        this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "17/06").Click());

        // Then (3/3) — toute tentative de plage est INERTE : aucun menu d'actions, aucune dialog
        // d'affectation ne s'ouvre, aucune case n'est sélectionnée (OuvrirMenu/BasculerModePlage sortent
        // tôt en consultation). Sous WaitForAssertion pour absorber un éventuel re-render résiduel.
        grille.WaitForAssertion(
            () =>
            {
                Assert.Empty(grille.FindAll("[data-testid='menu-actions-case']"));
                Assert.Empty(grille.FindAll("[data-testid='dialog-affecter-periode']"));
                Assert.Empty(grille.FindAll(".grille-jour-selectionnee"));
            },
            TimeSpan.FromSeconds(10));

        // … et AUCUNE écriture n'a transité (rempart anti vert-qui-ment) : zéro POST sur le canal
        // d'écriture pendant tout le parcours (navigation incluse — la navigation est lecture seule).
        Assert.Equal(0, espion.PostsEcriture);
    }

    /// <summary>Date (« dd/MM ») de la première case-jour rendue — témoin du début de la fenêtre courante.</summary>
    private static string PremiereCaseDate(IRenderedComponent<Web.Components.Pages.PlanningPartage> grille)
        => grille.FindAll("[data-testid='jour-case']")
            .First().QuerySelector(".grille-jour-date")!.TextContent.Trim();

    /// <summary>
    /// Espion de transport : relaie tout vers l'API distante réelle et COMPTE les <c>POST</c> vers le canal
    /// d'écriture (<c>/api/canal/…</c>). Le négociate SignalR (<c>/hubs/planning/…</c>) et les lectures de
    /// grille (<c>GET</c>) ne sont pas comptés — seule une écriture réelle ferait monter le compteur.
    /// </summary>
    private sealed class EspionEcritureHandler : DelegatingHandler
    {
        public int PostsEcriture { get; private set; }

        public EspionEcritureHandler(HttpMessageHandler inner) : base(inner) { }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Post
                && (request.RequestUri?.AbsolutePath.Contains("/api/canal/", StringComparison.Ordinal) ?? false))
            {
                PostsEcriture++;
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
