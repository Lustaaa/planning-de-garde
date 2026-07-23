using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.4 (🖥️ IHM, <c>@limite</c>) — <b>caractérisation early-green</b> de
/// la borne dure « impersonation = lecture seule, pas d'écriture au nom de ». Sur la grille <b>réellement
/// câblée</b> (front WASM, API distante réelle, <b>canal d'écriture HTTP réel</b>), le configurateur
/// incarne Bruno (Parent, menu visible) et pose un slot le 16/06 depuis la dialog :
/// <list type="bullet">
///   <item>le slot est <b>enregistré</b> (transite jusqu'au store réel de l'API distante) ;</item>
///   <item>la commande émise porte uniquement le <b>contexte de l'identité réelle</b> du configurateur
///   (<c>EnfantId</c> = « Léa ») et <b>aucune référence à l'acteur incarné</b> (ni <c>parent-b</c>, ni
///   « Bruno ») — l'impersonation n'a pas fui dans le canal requête/réponse.</item>
/// </list>
///
/// <para>L'auteur/contexte de la commande est inspecté <b>à la frontière</b> (corps HTTP réellement émis,
/// capté par un handler de transport qui relaie ensuite vers l'API live) — jamais doublé. Early-green
/// attendu <b>par construction</b> : <c>PoserSlotDialog</c> ne lit que <c>Session.EnfantId</c>, jamais
/// l'identité effective. <b>Un ROUGE ici signalerait une fuite de l'impersonation dans le canal
/// d'écriture (régression de la borne) → escalade.</b> Contrôle de non-vacuité : la pose réussit bien
/// SOUS incarnation (menu visible), donc l'absence de fuite n'est pas due à une écriture empêchée.</para>
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmEcritureSousIdentiteReelleTempsReelTests : TestContext
{
    // Mardi 16/06/2026 : la case posée. Référence « aujourd'hui » au 16/06 → fenêtre couvrant le 16/06.
    private static readonly DateTime Mardi_16_06_2026 = new(2026, 6, 16);

    [Fact]
    public void Should_EnregistrerLeSlotSousLIdentiteReelleDuConfigurateur_When_OnPoseUnSlotEnIncarnantUnParent()
    {
        // Given — la grille réellement câblée, avec un client dont on capte le corps de l'écriture émise
        // vers le canal de pose (puis relayé vers l'API live : le store est réellement muté).
        using var api = new ApiDistanteFactory();
        var capture = new CaptureCorpsHandler(api.Server.CreateHandler(), "/activites");
        using var client = new HttpClient(capture) { BaseAddress = api.Server.BaseAddress };
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Mardi_16_06_2026, client);

        // … le configurateur incarne Bruno (Parent) : contrôle de non-vacuité — le menu d'écriture est
        // bien visible SOUS l'incarnation (l'écriture est donc possible, pas empêchée).
        grille.WaitForState(
            () => grille.FindAll("[data-testid='selecteur-incarnation'] option[value='parent-b']").Count == 1,
            TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => grille.Find("[data-testid='selecteur-incarnation']").Change("parent-b"));
        grille.WaitForAssertion(
            () => Assert.Contains("Vous incarnez Bruno",
                grille.Find("[data-testid='bandeau-incarnation']").TextContent),
            TimeSpan.FromSeconds(10));

        // When — en incarnant Bruno, il pose un slot « école » le 16/06 depuis la dialog ouverte en contexte.
        grille.WaitForAssertion(
            () =>
            {
                this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "16/06").Click());
                this.SurDispatcher(() => grille.Find("[data-testid='action-poser-slot']").Click());
                Assert.NotEmpty(grille.FindAll("[data-testid='dialog-poser-slot']"));
            },
            TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => grille.Find("[data-testid='champ-lieu']").Change("école"));
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-poser-slot'] form").Submit());

        // Then — le slot est enregistré : il réapparaît dans la case du 16/06 (relu du store réel) …
        grille.WaitForAssertion(
            () =>
            {
                Assert.Empty(grille.FindAll("[data-testid='dialog-poser-slot']"));
                var slot = GrilleRuntimeHarness.CaseDuJour(grille, "16/06")
                    .QuerySelectorAll("[data-testid='slot-case']").Single();
                Assert.Contains("école", slot.QuerySelector(".grille-slot-libelle")!.TextContent);
            },
            TimeSpan.FromSeconds(10));

        // … et a réellement transité jusqu'au store de l'API distante (rempart anti vert-qui-ment).
        using (var scope = api.Services.CreateScope())
        {
            var projection = scope.ServiceProvider.GetRequiredService<GrilleAgendaQuery>();
            var grilleStore = projection.Projeter(new DateOnly(2026, 6, 15));
            var caseStore = grilleStore.Jours.Single(j => j.Date == new DateOnly(2026, 6, 16));
            Assert.Single(caseStore.Slots, s => s.Libelle == "école");
        }

        // … ENFIN, la commande émise porte le contexte de l'IDENTITÉ RÉELLE et AUCUNE référence à l'acteur
        // incarné : pas d'écriture « au nom de » Bruno (borne du sujet). Depuis s54 l'activité est une
        // sous-ressource de l'enfant : l'EnfantId (« Léa ») voyage par l'URL (plus dans le corps). On vérifie
        // donc l'URL ciblée (enfant Léa, contexte inchangé par l'incarnation) et l'ABSENCE de toute trace
        // ASCII de l'incarné dans l'URL comme dans le corps brut.
        Assert.NotNull(capture.DerniereUri);
        Assert.Contains("Léa", Uri.UnescapeDataString(capture.DerniereUri!.AbsolutePath)); // enfant réel, dans l'URL
        Assert.DoesNotContain("parent-b", Uri.UnescapeDataString(capture.DerniereUri!.AbsolutePath));
        Assert.DoesNotContain("Bruno", Uri.UnescapeDataString(capture.DerniereUri!.AbsolutePath));
        Assert.NotNull(capture.DernierCorps);
        Assert.DoesNotContain("parent-b", capture.DernierCorps);
        Assert.DoesNotContain("Bruno", capture.DernierCorps);
    }

    /// <summary>Handler de transport qui CAPTE le corps JSON d'un POST vers l'endpoint ciblé (le dernier
    /// vu), puis relaie tel quel vers l'API distante réelle — l'écriture transite réellement, le store est
    /// muté. Le corps est rebufferisé avant relais pour rester lisible côté API (lecture non destructrice).</summary>
    private sealed class CaptureCorpsHandler : DelegatingHandler
    {
        private readonly string _suffixeEndpoint;
        public string? DernierCorps { get; private set; }
        public Uri? DerniereUri { get; private set; }

        public CaptureCorpsHandler(HttpMessageHandler inner, string suffixeEndpoint) : base(inner)
            => _suffixeEndpoint = suffixeEndpoint;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Post
                && (request.RequestUri?.AbsolutePath.EndsWith(_suffixeEndpoint, StringComparison.Ordinal) ?? false)
                && request.Content is not null)
            {
                DerniereUri = request.RequestUri;
                var corps = await request.Content.ReadAsStringAsync(cancellationToken);
                DernierCorps = corps;
                // Rebufferise le corps pour que l'API live le relise intact (le contenu d'origine peut
                // être consommé par la lecture ci-dessus selon l'implémentation).
                var entete = request.Content.Headers.ContentType;
                request.Content = new StringContent(corps);
                if (entete is not null)
                    request.Content.Headers.ContentType = entete;
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
