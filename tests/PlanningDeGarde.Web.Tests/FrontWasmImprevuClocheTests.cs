using System;
using System.Collections.Generic;
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
/// Sprint 48 — Sc.6 (🖥️ @ihm) — acceptation de NIVEAU RUNTIME : la notification d'imprévu apparaît dans la CLOCHE
/// s47 (barre d'application), réellement câblée (store réel, journal réel, projection réelle, canal réel). Un
/// imprévu « malade » signalé sur le 29/06 pour un enfant produit une notification INFORMATIVE « Léa est malade
/// le 29/06 » (et « X sera en retard le 29/06 » pour un retard) ; elle porte l'état lu/non-lu et l'action
/// marquer-lu comme les autres événements du journal, MAIS N'EXPOSE AUCUNE action de suivi (pas d'accepter /
/// refuser — l'imprévu informatif n'est pas négociable ; le cas actionnable est l'échange s47).
///
/// Anti « vert qui ment » : la notification provient d'une VRAIE écriture (POST signaler-imprevu) consignée au
/// journal réel, relue par le canal de lecture réel — jamais une doublure.
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmImprevuClocheTests : TestContext
{
    private static readonly DateOnly Jour = new(2026, 6, 29); // 29/06 → responsable de fond parent-b (ISO 27)

    private static SessionPlanning SessionComme(string acteurId, string nom)
    {
        var session = new SessionPlanning();
        session.Connecter(nom, acteurId, TypeActeur.Parent);
        return session;
    }

    /// <summary>Rend la CLOCHE en ISOLATION, câblée comme dans le layout (composant autonome branché sur l'API
    /// distante réelle + le hub SignalR réel). Rempart anti vert-qui-ment préservé (store / projection réels).</summary>
    private IRenderedComponent<Cloche> Rendre(ApiDistanteFactory api, SessionPlanning session)
    {
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton(session);
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

    /// <summary>Sème un VRAI imprévu (canal réel) sur le 29/06 pour l'enfant « Léa », signalé par
    /// <paramref name="signalantId"/> : le concerné est le signalant (recevant) ET le responsable résolu du jour
    /// (cédant). Rempart anti vert-qui-ment : le journal est alimenté par l'écriture réelle.</summary>
    private static async Task SemerImprevu(ApiDistanteFactory api, TypeImprevu type, string signalantId)
    {
        var client = GrilleRuntimeHarness.ClientVers(api);
        (await client.PostAsJsonAsync(
            "api/imprevus",
            new SignalerImprevuRequete(Jour, "Léa", type, signalantId, ""))).EnsureSuccessStatusCode();
    }

    private static void SemerCycle(ApiDistanteFactory api)
        => GrilleRuntimeHarness.SemerCycle(api, new CycleDeFond(2, new Dictionary<int, string> { [0] = "parent-a", [1] = "parent-b" }));

    [Fact]
    public async Task Un_imprevu_malade_apparait_dans_la_cloche_informatif_avec_marquer_lu_et_sans_action_de_suivi()
    {
        using var api = new ApiDistanteFactory();
        SemerCycle(api);
        // Given — un imprévu « malade » sur le 29/06 pour Léa, signalé par parent-a (Alice).
        await SemerImprevu(api, TypeImprevu.Malade, "parent-a");

        // Parent connecté = parent-a (concerné : signalant).
        var grille = Rendre(api, SessionComme("parent-a", "Alice"));

        // Then — la cloche affiche un badge = 1 (imprévu non lu me concernant).
        grille.WaitForAssertion(
            () => Assert.Equal("1", grille.Find("[data-testid='cloche-badge']").TextContent.Trim()),
            TimeSpan.FromSeconds(10));

        // When — clic sur la cloche → panneau : la notification d'imprévu est INFORMATIVE « Léa est malade le 29/06 ».
        this.SurDispatcher(() => grille.Find("[data-testid='cloche-bouton']").Click());
        grille.WaitForAssertion(
            () =>
            {
                var notif = grille.Find("[data-testid='cloche-panneau'] [data-testid='cloche-notif'][data-type='imprevu']");
                Assert.Contains("Léa est malade le 29/06", notif.TextContent);
                Assert.Equal("0", notif.GetAttribute("data-lu")); // non-lu
                // Informatif : action marquer-lu présente, MAIS aucune action de suivi (accepter / refuser).
                Assert.NotNull(notif.QuerySelector("[data-testid='cloche-marquer-lu']"));
                Assert.Null(notif.QuerySelector("[data-testid='cloche-accepter']"));
                Assert.Null(notif.QuerySelector("[data-testid='cloche-refuser']"));
            },
            TimeSpan.FromSeconds(10));

        // When — marquer lu → le compteur passe à 0 (badge retiré) : l'état lu est persisté par utilisateur.
        this.SurDispatcher(() => grille.Find("[data-testid='cloche-marquer-lu']").Click());
        grille.WaitForAssertion(
            () => Assert.Empty(grille.FindAll("[data-testid='cloche-badge']")),
            TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task Un_imprevu_retard_apparait_dans_la_cloche_du_responsable_avec_le_libelle_retard()
    {
        using var api = new ApiDistanteFactory();
        SemerCycle(api);
        // Given — un imprévu « retard » sur le 29/06, signalé par parent-a (Alice).
        await SemerImprevu(api, TypeImprevu.Retard, "parent-a");

        // Parent connecté = parent-b (Bruno), responsable RÉSOLU du 29/06 → concerné (cédant).
        var grille = Rendre(api, SessionComme("parent-b", "Bruno"));

        // When — clic sur la cloche → panneau : la notification informe « Alice sera en retard le 29/06 ».
        this.SurDispatcher(() => grille.Find("[data-testid='cloche-bouton']").Click());
        grille.WaitForAssertion(
            () =>
            {
                var notif = grille.Find("[data-testid='cloche-panneau'] [data-testid='cloche-notif'][data-type='imprevu']");
                Assert.Contains("Alice sera en retard le 29/06", notif.TextContent);
                Assert.Null(notif.QuerySelector("[data-testid='cloche-accepter']")); // non négociable
            },
            TimeSpan.FromSeconds(10));
    }
}
