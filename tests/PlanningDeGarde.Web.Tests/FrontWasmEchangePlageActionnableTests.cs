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
/// Sprint 52 — Sc.8 (🖥️ @ihm) — acceptation de NIVEAU RUNTIME : la notif d'échange de PLAGE est ACTIONNABLE dans
/// la cloche du RECEVANT (Accepter / Refuser). Accepter (via mini-dialog de confirmation) émet AccepterProposition
/// par le canal d'écriture → la délégation-plage s45 est composée : TOUTES les cases de la plage <c>[J1..J3]</c>
/// basculent sur le nouveau responsable, et les transferts dérivés s31 apparaissent aux DEUX frontières.
///
/// Anti « vert qui ment » : proposition de plage / accord prouvés jusqu'au store distant réel (repositories +
/// projection réels), jamais une doublure de transport.
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmEchangePlageActionnableTests : TestContext
{
    // Plage entièrement DANS la semaine ISO 27 (29/06 → 05/07, fond parent-b) : déléguer à parent-a DÉRIVE un
    // bicolore aux DEUX frontières (la veille de J1 = 29/06 est parent-b ; le lendemain de J3 = 03/07 est parent-b).
    private static readonly DateTime Aujourdhui = GrilleRuntimeHarness.Lundi_29_06_2026;
    private static readonly DateOnly J1 = new(2026, 6, 30);
    private static readonly DateOnly J2 = new(2026, 7, 1);
    private static readonly DateOnly J3 = new(2026, 7, 2);
    private static readonly DateOnly J3Plus1 = new(2026, 7, 3); // frontière de SORTIE

    private static SessionPlanning SessionComme(string acteurId, string nom)
    {
        var session = new SessionPlanning();
        session.Connecter(nom, acteurId, TypeActeur.Parent);
        return session;
    }

    private void CablerServices(ApiDistanteFactory api, SessionPlanning session)
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
    }

    private static async Task SemerPropositionPlageVers(ApiDistanteFactory api, string versActeurId)
    {
        var client = GrilleRuntimeHarness.ClientVers(api);
        (await client.PostAsJsonAsync("api/canal/proposer-echange",
            new ProposerEchangeRequete(J1, "Léa", versActeurId, J3))).EnsureSuccessStatusCode();
    }

    private static void SemerCycle(ApiDistanteFactory api)
        => GrilleRuntimeHarness.SemerCycle(api, new CycleDeFond(2, new Dictionary<int, string> { [0] = "parent-a", [1] = "parent-b" }));

    [Fact]
    public async Task Le_recevant_voit_la_notif_de_plage_actionnable_et_ACCEPTER_fait_converger_TOUTE_la_plage()
    {
        // Given — une proposition pending de PLAGE [29/06..01/07] adressée à parent-a (recevant), visible en cloche.
        using var api = new ApiDistanteFactory();
        SemerCycle(api);
        await SemerPropositionPlageVers(api, "parent-a");
        CablerServices(api, SessionComme("parent-a", "Alice"));
        var cloche = RenderComponent<Cloche>();

        // When — ouvrir la cloche : la notification d'échange porte Accepter / Refuser (actionnable).
        this.SurDispatcher(() => cloche.Find("[data-testid='cloche-bouton']").Click());
        cloche.WaitForAssertion(
            () =>
            {
                var notif = cloche.Find("[data-testid='cloche-panneau'] [data-testid='cloche-notif'][data-type='echange']");
                Assert.NotNull(notif.QuerySelector("[data-testid='cloche-accepter']"));
                Assert.NotNull(notif.QuerySelector("[data-testid='cloche-refuser']"));
            },
            TimeSpan.FromSeconds(10));

        // Accepter → mini-dialog de confirmation → Confirmer (émet AccepterProposition par le canal d'écriture).
        this.SurDispatcher(() => cloche.Find("[data-testid='cloche-accepter']").Click());
        cloche.WaitForAssertion(() => Assert.NotEmpty(cloche.FindAll("[data-testid='cloche-confirmer']")), TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => cloche.Find("[data-testid='cloche-confirmer']").Click());

        // Then — la proposition passe à ACCEPTÉE.
        cloche.WaitForAssertion(
            () => Assert.Contains(api.Services.GetRequiredService<IPropositionEchangeRepository>().AllSnapshots(),
                p => p.VersActeurId == "parent-a" && p.Statut == StatutProposition.Acceptee),
            TimeSpan.FromSeconds(10));

        // Then — TOUTES les cases de la plage [J1..J3] basculent sur le nouveau responsable (Alice), prouvé au
        // store distant via la projection réelle ; transferts dérivés s31 aux DEUX frontières (entrée J1, sortie J3+1).
        using var scope = api.Services.CreateScope();
        var projection = scope.ServiceProvider.GetRequiredService<GrilleAgendaQuery>();
        foreach (var d in new[] { J1, J2, J3 })
            Assert.Equal("Alice", projection.Projeter(d).Jours.Single(j => j.Date == d).NomResponsable);
        Assert.NotNull(projection.Projeter(J1).Jours.Single(j => j.Date == J1).Transfert);
        Assert.NotNull(projection.Projeter(J3Plus1).Jours.Single(j => j.Date == J3Plus1).Transfert);
    }
}
