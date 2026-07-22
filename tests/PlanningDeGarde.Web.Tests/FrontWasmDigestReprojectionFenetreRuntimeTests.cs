using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Web;
using PlanningDeGarde.Web.Components;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 50 — Sc.6 (🖥️ @ihm) — acceptation de NIVEAU RUNTIME : le digest se REPROJETTE côté client depuis la
/// fenêtre de grille DÉJÀ chargée par l'unique GET grille — AUCUN GET dédié « digest ». Preuve stricte : un
/// handler COMPTE les GET <c>/api/grille</c> ; ouvrir la cloche et afficher le digest N'AJOUTE AUCUN GET (le
/// digest est reprojeté de la donnée déjà en mémoire). Et si l'on navigue vers une semaine NE contenant PAS le
/// jour courant, la section « immédiat » disparaît (vide neutre) et « à venir » se borne à la fenêtre chargée —
/// limitation ASSUMÉE (aucun GET sur navigation pour la combler, garde-fou anti-flake).
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmDigestReprojectionFenetreRuntimeTests : TestContext
{
    private static readonly DateTime Lundi_29_06_2026 = new(2026, 6, 29);

    /// <summary>Handler qui relaie tout vers l'API réelle en COMPTANT les GET de grille (<c>/api/grille</c>) :
    /// preuve que le digest n'émet aucun GET dédié (le compteur ne bouge que sur les chargements de la grille).</summary>
    private sealed class CompteurGrilleGet : DelegatingHandler
    {
        private int _gets;
        public int GrilleGets => Volatile.Read(ref _gets);
        public CompteurGrilleGet(HttpMessageHandler inner) : base(inner) { }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Get && (request.RequestUri?.AbsolutePath.Contains("/api/grille", StringComparison.Ordinal) ?? false))
                Interlocked.Increment(ref _gets);
            return base.SendAsync(request, cancellationToken);
        }
    }

    private static SessionPlanning SessionParent()
    {
        var session = new SessionPlanning();
        session.Connecter("Alice", "parent-a", TypeActeur.Parent);
        return session;
    }

    [Fact]
    public void Le_digest_se_reprojette_depuis_la_fenetre_chargee_sans_GET_dedie_et_hors_fenetre_devient_vide_neutre()
    {
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerCycle(api, new CycleDeFond(2, new Dictionary<int, string> { [0] = "parent-a", [1] = "parent-b" }));

        var compteur = new CompteurGrilleGet(api.Server.CreateHandler());
        var client = new HttpClient(compteur) { BaseAddress = api.Server.BaseAddress };

        Services.AddSingleton(client);
        Services.AddSingleton(SessionParent());
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

        // Chargement initial de la grille (unique GET grille pour la fenêtre courante contenant le 29/06).
        var grille = RenderComponent<PlanningPartage>();
        grille.WaitForState(() => grille.FindAll("[data-testid='jour-case']").Count == 28, TimeSpan.FromSeconds(10));
        var cloche = RenderComponent<Cloche>();

        var getsApresChargement = compteur.GrilleGets;
        Assert.Equal(1, getsApresChargement); // exactement le GET grille initial

        // When — j'ouvre la cloche et affiche le digest.
        this.SurDispatcher(() => cloche.Find("[data-testid='cloche-bouton']").Click());
        cloche.WaitForAssertion(
            () => Assert.Contains("Récupère", cloche.Find("[data-testid='digest-immediat']").TextContent),
            TimeSpan.FromSeconds(10));

        // Then — afficher le digest N'A AJOUTÉ AUCUN GET : il est REPROJETÉ de la donnée déjà chargée (0 GET dédié).
        Assert.Equal(getsApresChargement, compteur.GrilleGets);

        // When — je navigue vers la semaine suivante : la fenêtre chargée ne contient PLUS le jour courant (29/06).
        this.SurDispatcher(() => grille.Find("[data-testid='nav-semaine-suivante']").Click());
        grille.WaitForAssertion(() => Assert.Equal(2, compteur.GrilleGets), TimeSpan.FromSeconds(10)); // 1 GET grille de navigation

        // Then — la section « immédiat » disparaît (vide neutre), SANS aucun GET dédié pour la combler.
        cloche.WaitForAssertion(
            () =>
            {
                Assert.NotEmpty(cloche.FindAll("[data-testid='digest-immediat-vide']"));
                Assert.Empty(cloche.FindAll("[data-testid='digest-immediat-responsable']"));
            },
            TimeSpan.FromSeconds(10));
        Assert.Equal(2, compteur.GrilleGets); // toujours aucun GET dédié digest (borne = fenêtre chargée)

        // And — « à venir » se borne à la fenêtre CHARGÉE (aucune entrée avant le lundi 06/07, début de la fenêtre).
        var jours = cloche.FindAll("[data-testid='digest-avenir-jour']");
        Assert.All(jours, j =>
        {
            var date = DateOnly.ParseExact(j.GetAttribute("data-jour")!, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            Assert.True(date >= new DateOnly(2026, 7, 6), $"entrée à-venir hors fenêtre chargée : {date}");
        });
    }
}
