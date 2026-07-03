using System;
using System.Linq;
using System.Net.Http;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Web;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Harnais d'acceptation de NIVEAU RUNTIME des scénarios du sprint 07 : rend la <b>vraie</b> grille de
/// lecture <see cref="PlanningPartage"/> (front WASM) câblée à une <b>API distante réelle</b>
/// (<see cref="ApiDistanteFactory"/>, store réel, projection <see cref="GrilleAgendaQuery"/>, palette et
/// référentiel <b>réels</b> du foyer). Le chemin observé n'est jamais doublé : le nom et la légende
/// rendus à l'écran proviennent du référentiel réel résolu côté API, transitant par le canal de lecture
/// HTTP réel — rempart anti « vert qui ment » (un bUnit à doublure de transport ne le prouverait pas).
/// </summary>
internal static class GrilleRuntimeHarness
{
    // Lundi 29/06/2026 : date de référence des scénarios (début de la fenêtre par défaut de
    // 4 semaines glissantes — re-pointé du 5 → 4 semaines par Sprint 15 Sc.3).
    public static readonly DateTime Lundi_29_06_2026 = new(2026, 6, 29);

    /// <summary>Client HTTP du front pointé sur le transport réel de l'API distante in-test.</summary>
    public static HttpClient ClientVers(ApiDistanteFactory api)
        => new(api.Server.CreateHandler()) { BaseAddress = api.Server.BaseAddress };

    /// <summary>
    /// Navigue vers l'onglet « Période de garde » de l'écran de configuration (Sc.2, s20) : depuis ce
    /// sprint, le formulaire du cycle de fond y est cloisonné (l'onglet « Acteurs » est actif par défaut).
    /// Attend d'abord le chargement des acteurs (les options du cycle sont résolues depuis le store, énumérées
    /// sur l'onglet Acteurs actif par défaut), active l'onglet « Période de garde », puis attend que le
    /// formulaire de cycle soit rendu (index 1 présent, N=2 par défaut) — garde déterministe anti-flake.
    /// </summary>
    public static void AllerOngletPeriodeGarde(IRenderedComponent<ConfigurationFoyer> config)
    {
        config.WaitForState(
            () => config.FindAll("[data-testid='acteur-foyer']").Count > 0,
            TimeSpan.FromSeconds(10));
        // Fusion des sections (hors-sprint) : plus d'onglets — la section « Cycle de fond » est toujours
        // rendue. On attend simplement que son formulaire soit présent (garde déterministe anti-flake).
        config.WaitForElement("[data-testid='champ-cycle-index-1']", TimeSpan.FromSeconds(10));
    }

    /// <summary>
    /// Client HTTP du front pointé sur l'API distante réelle, MAIS dont une <b>écriture précise</b>
    /// (un <c>POST</c> dont le chemin se termine par <paramref name="suffixeEndpointEcriture"/>) subit un
    /// <b>échec de transport déterministe</b> (<see cref="HttpRequestException"/> levée par le handler,
    /// avant tout aller-retour réseau) — exactement le symptôme « service injoignable » côté navigateur.
    /// Les autres requêtes (énumération en lecture, etc.) transitent normalement vers l'API live.
    ///
    /// <para><b>Robustesse vs proxy loopback Docker.</b> On ne s'appuie PAS sur un <c>ConnectionRefused</c>
    /// d'un port loopback réellement libéré : quand Docker Desktop tourne, son proxy loopback intercepte
    /// la connexion et altère la sémantique du refus (l'exception n'est plus une <see cref="HttpRequestException"/>
    /// captée, ou la connexion pend) → flake environnemental. Lever l'<see cref="HttpRequestException"/> au
    /// niveau du handler reproduit le <b>contrat exact</b> que le composant attrape (catch HttpRequestException),
    /// de façon <b>déterministe que Docker tourne ou non</b>. Ce n'est pas une doublure de statut 4xx (qui serait
    /// un refus métier) : aucune réponse n'est fabriquée, l'échec est bien au transport.</para>
    /// </summary>
    public static HttpClient ClientVersAvecEcritureInjoignable(ApiDistanteFactory api, string suffixeEndpointEcriture)
        => new(new EcritureInjoignableHandler(api.Server.CreateHandler(), suffixeEndpointEcriture))
        {
            BaseAddress = api.Server.BaseAddress,
        };

    /// <summary>
    /// Client HTTP du front pointé sur l'API distante réelle, MAIS dont une <b>lecture de grille précise</b>
    /// (un <c>GET</c> dont le chemin contient <paramref name="segmentDateInjoignable"/>, p.ex.
    /// <c>/grille/2026/6/15</c>) subit un <b>échec de transport déterministe</b>
    /// (<see cref="HttpRequestException"/> levée par le handler) — exactement le symptôme « API distante
    /// injoignable pendant la navigation » (Sc.6) : la re-requête de la date naviguée échoue, alors que le
    /// chargement initial (autre date) et la navigation de retour transitent normalement.
    ///
    /// <para>Même robustesse anti-flake que <see cref="ClientVersAvecEcritureInjoignable"/> : on lève
    /// l'<see cref="HttpRequestException"/> au niveau du handler (contrat exact capté par le composant),
    /// plutôt que de dépendre d'un <c>ConnectionRefused</c> loopback dont la sémantique est altérée par le
    /// proxy de Docker Desktop. Déterministe que Docker tourne ou non.</para>
    /// </summary>
    public static HttpClient ClientVersAvecLectureGrilleInjoignable(ApiDistanteFactory api, string segmentDateInjoignable)
        => new(new LectureGrilleInjoignableHandler(api.Server.CreateHandler(), segmentDateInjoignable))
        {
            BaseAddress = api.Server.BaseAddress,
        };

    /// <summary>
    /// Handler de transport qui relaie tout vers l'API distante réelle SAUF un <c>POST</c> vers l'endpoint
    /// d'écriture ciblé, pour lequel il lève une <see cref="HttpRequestException"/> — échec de transport
    /// déterministe et indépendant de l'environnement (anti-flake proxy loopback Docker).
    /// </summary>
    private sealed class EcritureInjoignableHandler : DelegatingHandler
    {
        private readonly string _suffixeEndpointEcriture;

        public EcritureInjoignableHandler(HttpMessageHandler inner, string suffixeEndpointEcriture)
            : base(inner) => _suffixeEndpointEcriture = suffixeEndpointEcriture;

        protected override System.Threading.Tasks.Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Post
                && (request.RequestUri?.AbsolutePath.EndsWith(_suffixeEndpointEcriture, StringComparison.Ordinal) ?? false))
            {
                throw new HttpRequestException(
                    $"service injoignable (échec de transport simulé, déterministe) vers {_suffixeEndpointEcriture}");
            }

            return base.SendAsync(request, cancellationToken);
        }
    }

    /// <summary>
    /// Handler de transport qui relaie tout vers l'API distante réelle SAUF un <c>GET</c> de grille dont le
    /// chemin contient le segment de date ciblé, pour lequel il lève une <see cref="HttpRequestException"/>
    /// — échec de transport déterministe (anti-flake proxy loopback Docker). Reproduit l'API distante
    /// injoignable sur la SEULE date naviguée (Sc.6), laissant passer le chargement initial et le retour.
    /// </summary>
    private sealed class LectureGrilleInjoignableHandler : DelegatingHandler
    {
        private readonly string _segmentDateInjoignable;

        public LectureGrilleInjoignableHandler(HttpMessageHandler inner, string segmentDateInjoignable)
            : base(inner) => _segmentDateInjoignable = segmentDateInjoignable;

        protected override System.Threading.Tasks.Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Get
                && (request.RequestUri?.AbsolutePath.Contains(_segmentDateInjoignable, StringComparison.Ordinal) ?? false))
            {
                throw new HttpRequestException(
                    $"service injoignable (échec de transport simulé, déterministe) — lecture {_segmentDateInjoignable}");
            }

            return base.SendAsync(request, cancellationToken);
        }
    }

    /// <summary>
    /// Sème une période dans le store réel de l'API distante (Given d'un scénario de lecture) — la
    /// projection réelle la relira et le référentiel réel résoudra le nom du responsable.
    /// </summary>
    public static void SemerPeriode(ApiDistanteFactory api, string responsableId, DateTime debut, DateTime fin)
        => api.Services.GetRequiredService<IPeriodeRepository>()
            .Enregistrer(PeriodeDeGarde.Affecter(responsableId, debut, fin).Valeur!);

    /// <summary>
    /// Sème un slot de localisation dans le store réel de l'API distante (Given d'un scénario de
    /// suppression de slot) — la projection réelle le rendra dans la (les) case(s) qu'il couvre. Sème
    /// EN DIRECT via le port (pas la pose validée), pour pouvoir placer un lieu hors référentiel.
    /// </summary>
    public static void SemerSlot(ApiDistanteFactory api, string enfantId, string lieuId, DateTime debut, DateTime fin)
        => api.Services.GetRequiredService<ISlotRepository>()
            .Enregistrer(SlotDeLocalisation.Poser(enfantId, lieuId, debut, fin).Valeur!);

    /// <summary>
    /// Sème un cycle de fond dans le store réel de l'API distante (Given d'un scénario de navigation) —
    /// la projection réelle résout le responsable de fond par parité ISO de chaque semaine, le
    /// référentiel réel résolvant nom et couleur. Permet d'observer la re-résolution du fond à la date
    /// naviguée sans aucune saisie de période (Sc.1).
    /// </summary>
    public static void SemerCycle(ApiDistanteFactory api, CycleDeFond cycle)
        => api.Services.GetRequiredService<IReferentielCycleDeFond>().DefinirCycle(cycle);

    /// <summary>
    /// Rend la grille réelle câblée à l'API distante, à la date de référence injectée. Le hub SignalR
    /// est redirigé vers le TestServer de l'API (long polling) pour que la diffusion temps réel soit
    /// réellement observable au runtime (Sc.4) — pour les scénarios de lecture pure, il se connecte
    /// proprement sans interférer.
    /// </summary>
    public static IRenderedComponent<PlanningPartage> RendreGrille(
        Bunit.TestContext ctx, ApiDistanteFactory api, DateTime aujourdhui, HttpClient? client = null)
    {
        // Client par défaut = transport réel complet ; un appelant peut injecter un client dont une
        // écriture précise est coupée (ClientVersAvecEcritureInjoignable) tout en laissant passer la
        // lecture initiale de la grille (Sc.4 « API injoignable » via le contexte grille).
        ctx.Services.AddSingleton(client ?? ClientVers(api));
        ctx.Services.AddSingleton(SessionConnectee());
        ctx.Services.AddSingleton<IDateTimeProvider>(new DateTimeProviderFige(aujourdhui));
        ctx.Services.AddSingleton(new OptionsConnexionHub
        {
            Configurer = options =>
            {
                options.HttpMessageHandlerFactory = _ => api.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            },
        });

        var grille = ctx.RenderComponent<PlanningPartage>();

        // Le chargement de la grille (GET HTTP vers l'API distante) est asynchrone : on attend que la
        // fenêtre par défaut soit réellement projetée (28 cases-jour rendues, 4 semaines glissantes —
        // re-pointé du 5 → 4 semaines par Sc.3) avant d'observer nom/légende.
        grille.WaitForState(
            () => grille.FindAll("[data-testid='jour-case']").Count == 28,
            TimeSpan.FromSeconds(10));

        return grille;
    }

    /// <summary>
    /// Session applicative <b>connectée</b> (s25, Sc.1) : la grille (route protégée) ne se rend que sous
    /// session ouverte. Les scénarios de la grille présupposent un utilisateur connecté (l'accès a franchi
    /// la garde d'admission) — on ouvre une session pour refléter cette réalité runtime. L'identité réelle
    /// est ancrée sur un acteur de type Parent (s25 Sc.5), préservant le gating Parent des scénarios de
    /// grille pré-s25 (aucun acteur incarnable câblé ici).
    /// </summary>
    public static SessionPlanning SessionConnectee()
    {
        var session = new SessionPlanning();
        session.Connecter("utilisateur connecté", "configurateur", TypeActeur.Parent);
        return session;
    }

    /// <summary>La case-jour rendue dont l'en-tête de date affiche <paramref name="jjMM"/> (« dd/MM »).</summary>
    public static IElement CaseDuJour(IRenderedComponent<PlanningPartage> grille, string jjMM)
        => grille.FindAll("[data-testid='jour-case']")
            .Single(c => c.QuerySelector(".grille-jour-date")!.TextContent.Trim() == jjMM);
}
