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
/// Sprint 51 — Sc.5 (🖥️ @ihm) — acceptation de NIVEAU RUNTIME : l'ACTION DE SUIVI « proposer un échange » vit DANS
/// la notif d'imprévu de la CLOCHE s47 (barre d'application), réellement câblée (store réel, journal réel, canal
/// réel, SignalR réel). L'action est CONTEXTUALISÉE (jour + enfant de l'imprévu) → ouvre la mini-dialog « proposer
/// un échange » s47 RÉUTILISÉE, PRÉ-REMPLIE : le Parent ne choisit que le versActeur ; valider émet la commande via
/// le CANAL D'ÉCRITURE (use case de COMPOSITION s51) — jamais la diffusion, AUCUNE surcharge (canal de consentement).
/// Échap ferme la mini-dialog sans commande (port IEcouteurEchapModal s33). Un Invité ne voit NI la cloche NI
/// l'action (Parent-gated) ; la notif d'imprévu reste INFORMATIVE (marquer-lu conservé, aucune mutation du fait).
///
/// Anti « vert qui ment » : la proposition greffée est prouvée jusqu'au store réel (proposition pending, 0 surcharge),
/// l'imprévu prouvé inchangé au journal réel — jamais une doublure de transport.
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmProposerEchangeSuiteImprevuTests : TestContext
{
    private static readonly DateOnly Jour = new(2026, 6, 29); // 29/06 → responsable de fond parent-b (ISO 27)

    /// <summary>Double à la main du port d'écoute Échap (spy).</summary>
    private sealed class EspionEchap : IEcouteurEchapModal
    {
        private Func<Task>? _onEchap;
        public int Attachements { get; private set; }
        public ValueTask<IAsyncDisposable> EcouterAsync(Func<Task> onEchap)
        {
            Attachements++;
            _onEchap = onEchap;
            return ValueTask.FromResult<IAsyncDisposable>(new Abonnement(this));
        }
        public Task DeclencherEchapDocument() => _onEchap?.Invoke() ?? Task.CompletedTask;
        private sealed class Abonnement : IAsyncDisposable
        {
            private readonly EspionEchap _e;
            public Abonnement(EspionEchap e) => _e = e;
            public ValueTask DisposeAsync() { _e._onEchap = null; return ValueTask.CompletedTask; }
        }
    }

    private static SessionPlanning SessionComme(string acteurId, string nom)
    {
        var session = new SessionPlanning();
        session.Connecter(nom, acteurId, TypeActeur.Parent);
        return session;
    }

    private IRenderedComponent<Cloche> Rendre(ApiDistanteFactory api, SessionPlanning session, IEcouteurEchapModal? echap = null)
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
        if (echap is not null)
            Services.AddSingleton(echap);
        return RenderComponent<Cloche>();
    }

    /// <summary>Sème un VRAI imprévu (canal réel) sur le 29/06 pour l'enfant « Léa », signalé par
    /// <paramref name="signalantId"/> (concerne le signalant ET le responsable résolu du jour).</summary>
    private static async Task SemerImprevu(ApiDistanteFactory api, TypeImprevu type, string signalantId)
    {
        var client = GrilleRuntimeHarness.ClientVers(api);
        (await client.PostAsJsonAsync(
            "api/canal/signaler-imprevu",
            new SignalerImprevuRequete(Jour, "Léa", type, signalantId, ""))).EnsureSuccessStatusCode();
    }

    private static void SemerCycle(ApiDistanteFactory api)
        => GrilleRuntimeHarness.SemerCycle(api, new CycleDeFond(2, new Dictionary<int, string> { [0] = "parent-a", [1] = "parent-b" }));

    /// <summary>Ouvre le panneau de la cloche puis clique l'action « proposer un échange » de la notif d'imprévu.</summary>
    private void OuvrirDialogViaNotifImprevu(IRenderedComponent<Cloche> cloche)
    {
        this.SurDispatcher(() => cloche.Find("[data-testid='cloche-bouton']").Click());
        cloche.WaitForAssertion(
            () =>
            {
                var notif = cloche.Find("[data-testid='cloche-panneau'] [data-testid='cloche-notif'][data-type='imprevu']");
                Assert.NotNull(notif.QuerySelector("[data-testid='cloche-proposer-suite-imprevu']"));
            },
            TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => cloche.Find("[data-testid='cloche-proposer-suite-imprevu']").Click());
        cloche.WaitForAssertion(
            () => Assert.NotEmpty(cloche.FindAll("[data-testid='dialog-proposer']")), TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task La_notif_imprevu_porte_l_action_proposer_echange_qui_ouvre_le_mini_dialog_prerempli()
    {
        using var api = new ApiDistanteFactory();
        SemerCycle(api);
        await SemerImprevu(api, TypeImprevu.Malade, "parent-a");
        // Parent connecté = parent-b (Bruno), responsable RÉSOLU du 29/06 → concerné par l'imprévu.
        var cloche = Rendre(api, SessionComme("parent-b", "Bruno"));

        OuvrirDialogViaNotifImprevu(cloche);

        // La mini-dialog « proposer un échange » s47 est ouverte, PRÉ-REMPLIE (contexte du 29/06) et offre le
        // choix du versActeur ; l'imprévu reste informatif (aucun accepter/refuser sur la notif d'imprévu).
        var dialog = cloche.Find("[data-testid='dialog-proposer']");
        Assert.NotNull(dialog.QuerySelector("[data-testid='champ-recevant']"));
        Assert.Contains("29/06/2026", dialog.TextContent);
        var notif = cloche.Find("[data-testid='cloche-notif'][data-type='imprevu']");
        Assert.Null(notif.QuerySelector("[data-testid='cloche-accepter']"));
    }

    [Fact]
    public async Task Proposer_via_le_dialog_emet_par_le_canal_ecriture_une_proposition_greffee_SANS_surcharge()
    {
        using var api = new ApiDistanteFactory();
        SemerCycle(api);
        await SemerImprevu(api, TypeImprevu.Malade, "parent-a");
        var cloche = Rendre(api, SessionComme("parent-b", "Bruno"));

        OuvrirDialogViaNotifImprevu(cloche);
        // Le Parent ne choisit QUE le versActeur (jour + enfant hérités de l'imprévu), puis valide.
        this.SurDispatcher(() => cloche.Find("[data-testid='champ-recevant']").Change("parent-a"));
        this.SurDispatcher(() => cloche.Find("[data-testid='dialog-proposer'] form").Submit());

        // Then — une proposition PENDING greffée (jour 29/06, enfant Léa, cédant résolu parent-b → parent-a) au store
        // réel, la mini-dialog se ferme, ET AUCUNE surcharge n'est écrite (canal de consentement).
        cloche.WaitForAssertion(
            () =>
            {
                Assert.Empty(cloche.FindAll("[data-testid='dialog-proposer']"));
                Assert.Contains(
                    api.Services.GetRequiredService<IPropositionEchangeRepository>().AllSnapshots(),
                    p => p.Jour == Jour && p.EnfantId == "Léa" && p.DeActeurId == "parent-b"
                        && p.VersActeurId == "parent-a" && p.Statut == StatutProposition.Proposee);
            },
            TimeSpan.FromSeconds(10));
        Assert.Empty(api.Services.GetRequiredService<IPeriodeRepository>().AllSnapshots());
        // L'imprévu d'origine reste au journal réel, inchangé (fait informatif non muté par la proposition).
        Assert.Contains(
            api.Services.GetRequiredService<IJournalChangements>().Tout(),
            e => e.Type == TypeChangement.Imprevu && e.Jour == Jour && e.Imprevu == TypeImprevu.Malade);
    }

    [Fact]
    public async Task Echap_ferme_le_mini_dialog_sans_emettre_aucune_commande()
    {
        using var api = new ApiDistanteFactory();
        SemerCycle(api);
        await SemerImprevu(api, TypeImprevu.Malade, "parent-a");
        var espion = new EspionEchap();
        var cloche = Rendre(api, SessionComme("parent-b", "Bruno"), espion);

        OuvrirDialogViaNotifImprevu(cloche);
        // La mini-dialog ouverte a ATTACHÉ l'écouteur document (capture au niveau document s33).
        cloche.WaitForAssertion(() => Assert.True(espion.Attachements >= 1), TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => cloche.Find("[data-testid='champ-recevant']").Change("parent-a"));

        // When — Échap document (rejoué via le callback capté par le spy) : ferme la mini-dialog SANS émettre.
        this.SurDispatcher(() => espion.DeclencherEchapDocument().GetAwaiter().GetResult());

        cloche.WaitForAssertion(
            () => Assert.Empty(cloche.FindAll("[data-testid='dialog-proposer']")), TimeSpan.FromSeconds(10));
        Assert.Empty(api.Services.GetRequiredService<IPropositionEchangeRepository>().AllSnapshots());
        Assert.Empty(api.Services.GetRequiredService<IPeriodeRepository>().AllSnapshots());
    }

    [Fact]
    public async Task L_invite_ne_voit_ni_la_cloche_ni_l_action_de_suivi()
    {
        using var api = new ApiDistanteFactory();
        SemerCycle(api);
        await SemerImprevu(api, TypeImprevu.Malade, "parent-a");
        var session = SessionComme("parent-b", "Bruno");
        session.Role = RoleAuteur.Invite; // consultation seule → non-Parent
        var cloche = Rendre(api, session);

        // Then — la cloche n'est même pas rendue (Parent-gated), donc aucune action de suivi n'est accessible.
        Assert.Empty(cloche.FindAll("[data-testid='cloche-bouton']"));
        Assert.Empty(cloche.FindAll("[data-testid='cloche-proposer-suite-imprevu']"));
    }
}
