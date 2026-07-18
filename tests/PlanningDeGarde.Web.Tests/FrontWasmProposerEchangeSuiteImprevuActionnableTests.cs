using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
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
using static PlanningDeGarde.Web.CanalEcriture;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 51 — Sc.6 (🖥️ @ihm) — acceptation de NIVEAU RUNTIME : la proposition GREFFÉE SUR UN IMPRÉVU (composition
/// s51) est un échange s47 STANDARD — elle apparaît ACTIONNABLE (Accepter / Refuser) chez le RECEVANT dans SA cloche
/// s47, réellement câblée (store réel, journal réel, canal réel, SignalR réel). Accepter (via mini-dialog de
/// confirmation) COMPOSE la délégation s44 → la case du jour bascule sur le recevant (surcharge + transfert dérivé
/// s31) ; Refuser clôt sans aucune écriture. L'imprévu d'origine reste, lui, une notif INFORMATIVE inchangée.
///
/// Anti « vert qui ment » : accord/refus prouvés jusqu'au store distant réel ; l'imprévu prouvé inchangé au journal.
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmProposerEchangeSuiteImprevuActionnableTests : TestContext
{
    private static readonly DateTime Aujourdhui = GrilleRuntimeHarness.Lundi_29_06_2026; // 29/06 → parent-b (fond)
    private static readonly DateOnly Jour = new(2026, 6, 29);

    private static SessionPlanning SessionComme(string acteurId, string nom)
    {
        var session = new SessionPlanning();
        session.Connecter(nom, acteurId, TypeActeur.Parent);
        return session;
    }

    private IRenderedComponent<Cloche> RendreCloche(ApiDistanteFactory api, SessionPlanning session)
    {
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton(session);
        Services.AddSingleton<IDateTimeProvider>(new DateTimeProviderFige(Aujourdhui));
        Services.AddSingleton(new OptionsConnexionHub
        {
            Configurer = options =>
            {
                options.HttpMessageHandlerFactory = _ => api.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            },
        });
        return RenderComponent<Cloche>();
    }

    /// <summary>Sème un imprévu (signalé par <paramref name="signalantId"/>) PUIS une proposition GREFFÉE dessus
    /// (composition s51) vers <paramref name="versActeurId"/>, par le canal réel — cédant = responsable résolu.</summary>
    private static async Task SemerImprevuPuisPropositionGreffee(ApiDistanteFactory api, string signalantId, string versActeurId)
    {
        var client = GrilleRuntimeHarness.ClientVers(api);
        (await client.PostAsJsonAsync("api/canal/signaler-imprevu",
            new SignalerImprevuRequete(Jour, "Léa", TypeImprevu.Malade, signalantId, ""))).EnsureSuccessStatusCode();
        var imprevuId = api.Services.GetRequiredService<IJournalChangements>().Tout()
            .Single(e => e.Type == TypeChangement.Imprevu).Id;
        (await client.PostAsJsonAsync("api/canal/proposer-echange-suite-imprevu",
            new ProposerEchangeSuiteImprevuRequete(imprevuId, versActeurId))).EnsureSuccessStatusCode();
    }

    private static void SemerCycle(ApiDistanteFactory api)
        => GrilleRuntimeHarness.SemerCycle(api, new CycleDeFond(2, new Dictionary<int, string> { [0] = "parent-a", [1] = "parent-b" }));

    [Fact]
    public async Task La_proposition_greffee_est_actionnable_chez_le_recevant_et_ACCEPTER_compose_la_delegation()
    {
        // Given — imprévu signalé par parent-a (concerné) + proposition greffée vers parent-a (le recevant).
        using var api = new ApiDistanteFactory();
        SemerCycle(api);
        await SemerImprevuPuisPropositionGreffee(api, "parent-a", "parent-a");
        var cloche = RendreCloche(api, SessionComme("parent-a", "Alice"));

        // When — parent-a ouvre SA cloche : la proposition greffée y est ACTIONNABLE (Accepter / Refuser) ; l'imprévu
        // d'origine reste une notif INFORMATIVE (aucun accepter/refuser sur la notif d'imprévu).
        this.SurDispatcher(() => cloche.Find("[data-testid='cloche-bouton']").Click());
        cloche.WaitForAssertion(
            () =>
            {
                var echange = cloche.Find("[data-testid='cloche-panneau'] [data-testid='cloche-notif'][data-type='echange']");
                Assert.NotNull(echange.QuerySelector("[data-testid='cloche-accepter']"));
                Assert.NotNull(echange.QuerySelector("[data-testid='cloche-refuser']"));
                var imprevu = cloche.Find("[data-testid='cloche-panneau'] [data-testid='cloche-notif'][data-type='imprevu']");
                Assert.Null(imprevu.QuerySelector("[data-testid='cloche-accepter']")); // informatif, non négociable
            },
            TimeSpan.FromSeconds(10));

        // Accepter → mini-dialog de confirmation → Confirmer (émet AccepterProposition par le canal d'écriture).
        this.SurDispatcher(() => cloche.Find("[data-testid='cloche-accepter']").Click());
        cloche.WaitForAssertion(() => Assert.NotEmpty(cloche.FindAll("[data-testid='cloche-confirmer']")), TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => cloche.Find("[data-testid='cloche-confirmer']").Click());

        // Then — la proposition passe à ACCEPTÉE et la délégation s44 est composée : la case du jour bascule sur Alice.
        cloche.WaitForAssertion(
            () => Assert.Contains(api.Services.GetRequiredService<IPropositionEchangeRepository>().AllSnapshots(),
                p => p.VersActeurId == "parent-a" && p.Statut == StatutProposition.Acceptee),
            TimeSpan.FromSeconds(10));
        using var scope = api.Services.CreateScope();
        var projection = scope.ServiceProvider.GetRequiredService<GrilleAgendaQuery>();
        Assert.Equal("Alice", projection.Projeter(Jour).Jours.Single(j => j.Date == Jour).NomResponsable);

        // Then — l'imprévu d'origine reste au journal, inchangé (fait informatif non muté / non « résolu »).
        Assert.Contains(api.Services.GetRequiredService<IJournalChangements>().Tout(),
            e => e.Type == TypeChangement.Imprevu && e.Jour == Jour && e.Imprevu == TypeImprevu.Malade);
    }

    [Fact]
    public async Task La_proposition_greffee_REFUSEE_par_le_recevant_ne_produit_aucune_ecriture_l_imprevu_reste_informatif()
    {
        using var api = new ApiDistanteFactory();
        SemerCycle(api);
        await SemerImprevuPuisPropositionGreffee(api, "parent-a", "parent-a");
        var cloche = RendreCloche(api, SessionComme("parent-a", "Alice"));

        this.SurDispatcher(() => cloche.Find("[data-testid='cloche-bouton']").Click());
        cloche.WaitForAssertion(() => Assert.NotEmpty(cloche.FindAll("[data-testid='cloche-refuser']")), TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => cloche.Find("[data-testid='cloche-refuser']").Click());
        cloche.WaitForAssertion(() => Assert.NotEmpty(cloche.FindAll("[data-testid='cloche-confirmer']")), TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => cloche.Find("[data-testid='cloche-confirmer']").Click());

        // Then — la proposition passe à REFUSÉE, AUCUNE surcharge n'est écrite (store intact).
        cloche.WaitForAssertion(
            () => Assert.Contains(api.Services.GetRequiredService<IPropositionEchangeRepository>().AllSnapshots(),
                p => p.VersActeurId == "parent-a" && p.Statut == StatutProposition.Refusee),
            TimeSpan.FromSeconds(10));
        Assert.Empty(api.Services.GetRequiredService<IPeriodeRepository>().AllSnapshots());
        // L'imprévu d'origine reste une notif INFORMATIVE inchangée au journal.
        Assert.Contains(api.Services.GetRequiredService<IJournalChangements>().Tout(),
            e => e.Type == TypeChangement.Imprevu && e.Jour == Jour && e.Imprevu == TypeImprevu.Malade);
    }
}
