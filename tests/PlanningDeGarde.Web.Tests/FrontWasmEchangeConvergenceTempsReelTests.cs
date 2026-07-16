using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Web;
using PlanningDeGarde.Web.Components;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;
using Xunit;
using static PlanningDeGarde.Web.CanalEcriture;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 47 — Sc.9 (🖥️ @ihm) — acceptation de NIVEAU RUNTIME : convergence TEMPS RÉEL de l'échange. Sur l'écran
/// de l'ÉMETTEUR (parent-b), quand le RECEVANT (parent-a, depuis sa cloche = un AUTRE écran, ici son POST accord)
/// ACCEPTE : (a) la CASE du jour CONVERGE — le recevant devient responsable (surcharge) + transfert bicolore
/// dérivé (s31) — par reprojection de la grille relue (canal de lecture, aucun GET DÉDIÉ) ; (b) la notification
/// d'échange de la cloche passe à « accepté » par REPROJECTION depuis la diffusion Proposition (0 GET). Sur un
/// REFUS : la notification se CLÔT (retirée) par reprojection, SANS aucune écriture.
///
/// Anti « vert qui ment » : accord/refus partent réellement du canal d'écriture (POST) ; la convergence de la
/// cloche passe par la diffusion porteuse de payload RÉELLE ; la case par la grille relue réelle — pas de doublure.
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmEchangeConvergenceTempsReelTests : TestContext
{
    private static readonly DateTime Aujourdhui = GrilleRuntimeHarness.Lundi_29_06_2026;
    // Jour MID-SEMAINE (mercredi 01/07, ISO 27 → fond parent-b) : déléguer à parent-a (≠ voisins parent-b) DÉRIVE
    // bien un transfert bicolore sur ce jour (s31) — contrairement au lundi bascule 29/06 où surcharger parent-a
    // (le responsable de la semaine précédente) ne ferait que DÉCALER la bascule d'un jour sans bicolore ici.
    private static readonly DateOnly Jour = new(2026, 7, 1);
    private const string CaseJJMM = "01/07";

    private static SessionPlanning SessionComme(string acteurId, string nom)
    {
        var session = new SessionPlanning();
        session.Connecter(nom, acteurId, TypeActeur.Parent);
        return session;
    }

    /// <summary>Câble le runtime de l'ÉMETTEUR (parent-b) : canal + session + hub réels. Depuis le repositionnement
    /// de la cloche dans la barre d'application (retour PO), la CASE vit dans la PAGE (PlanningPartage) et la
    /// NOTIFICATION dans la CLOCHE (composant du layout) : on rend les deux en frères, partageant ce même
    /// câblage, exactement comme le layout enveloppe la page dans l'app réelle.</summary>
    private void CablerEmetteur(ApiDistanteFactory api)
    {
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton(SessionComme("parent-b", "Bruno")); // l'émetteur observe
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

    private IRenderedComponent<PlanningPartage> RendreGrille()
    {
        var grille = RenderComponent<PlanningPartage>();
        grille.WaitForState(() => grille.FindAll("[data-testid='jour-case']").Count == 28, TimeSpan.FromSeconds(10));
        return grille;
    }

    private static async Task<string> SemerPropositionVersParentA(ApiDistanteFactory api)
    {
        var client = GrilleRuntimeHarness.ClientVers(api);
        (await client.PostAsJsonAsync("api/canal/proposer-echange",
            new ProposerEchangeRequete(Jour, "Léa", "parent-a"))).EnsureSuccessStatusCode();
        return api.Services.GetRequiredService<IPropositionEchangeRepository>().AllSnapshots()
            .Single(p => p.VersActeurId == "parent-a").Id;
    }

    private static void SemerCycle(ApiDistanteFactory api)
        => GrilleRuntimeHarness.SemerCycle(api, new CycleDeFond(2, new Dictionary<int, string> { [0] = "parent-a", [1] = "parent-b" }));

    [Fact]
    public async Task Accord_du_recevant_fait_converger_la_case_et_passe_la_notif_a_accepte_sur_l_ecran_emetteur()
    {
        // Given — proposition pending (parent-b → parent-a). L'émetteur parent-b observe (grille + cloche).
        using var api = new ApiDistanteFactory();
        SemerCycle(api);
        var propositionId = await SemerPropositionVersParentA(api);
        CablerEmetteur(api);
        var grille = RendreGrille();
        var cloche = RenderComponent<Cloche>();

        // La cloche de l'émetteur montre la proposition (informationnelle, « proposé ») ; la case 29/06 est au fond (Bruno).
        this.SurDispatcher(() => cloche.Find("[data-testid='cloche-bouton']").Click());
        cloche.WaitForAssertion(
            () => Assert.NotEmpty(cloche.FindAll("[data-testid='cloche-panneau'] [data-testid='cloche-notif'][data-type='echange']")),
            TimeSpan.FromSeconds(10));
        Assert.Equal("Bruno", GrilleRuntimeHarness.CaseDuJour(grille, CaseJJMM).QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());

        // When — le RECEVANT ACCEPTE depuis sa cloche (autre écran = POST accord).
        var client = GrilleRuntimeHarness.ClientVers(api);
        (await client.PostAsJsonAsync("api/canal/accepter-proposition", new RepondrePropositionRequete(propositionId))).EnsureSuccessStatusCode();

        // Diffusion RÉELLE repoussée en boucle de fond (idempotente) pour tomber APRÈS l'établissement des
        // connexions : MiseAJour (grille relue) + Changement (délégation → cloche) + Proposition (statut accepté → cloche).
        var evenementDeleg = api.Services.GetRequiredService<IJournalChangements>().Tout().Single(e => e.Type == TypeChangement.Delegation && e.RecevantId == "parent-a");
        var propositionAcceptee = api.Services.GetRequiredService<IPropositionEchangeRepository>().AllSnapshots().Single(p => p.Id == propositionId);
        var planning = api.Services.GetRequiredService<INotificateurPlanning>();
        var changement = api.Services.GetRequiredService<INotificateurChangement>();
        using var diffusionContinue = new CancellationTokenSource();
        var pousseur = Task.Run(async () =>
        {
            while (!diffusionContinue.IsCancellationRequested)
            {
                planning.NotifierMiseAJour();
                changement.NotifierChangement(evenementDeleg);
                changement.NotifierProposition(propositionAcceptee);
                try { await Task.Delay(150, diffusionContinue.Token); }
                catch (TaskCanceledException) { break; }
            }
        });

        try
        {
            // Then — (a) la CASE du jour converge : le recevant Alice devient responsable + transfert bicolore dérivé.
            grille.WaitForAssertion(
                () =>
                {
                    Assert.Equal("Alice", GrilleRuntimeHarness.CaseDuJour(grille, CaseJJMM).QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
                    Assert.NotNull(GrilleRuntimeHarness.CaseDuJour(grille, CaseJJMM).QuerySelector("[data-testid='case-transfert-bicolore']"));
                },
                TimeSpan.FromSeconds(15));

            // (b) la notification d'échange de la cloche passe à « accepté » (reprojection depuis la diffusion Proposition).
            cloche.WaitForAssertion(
                () =>
                {
                    var notif = cloche.Find("[data-testid='cloche-panneau'] [data-testid='cloche-notif'][data-type='echange']");
                    Assert.Contains("accepté", notif.TextContent);
                },
                TimeSpan.FromSeconds(15));
        }
        finally
        {
            diffusionContinue.Cancel();
            await pousseur;
        }
    }

    [Fact]
    public async Task Refus_du_recevant_retire_la_notif_sur_l_ecran_emetteur_sans_aucune_ecriture()
    {
        // Given — proposition pending (parent-b → parent-a) visible dans la cloche de l'émetteur.
        using var api = new ApiDistanteFactory();
        SemerCycle(api);
        var propositionId = await SemerPropositionVersParentA(api);
        CablerEmetteur(api);
        var cloche = RenderComponent<Cloche>();
        this.SurDispatcher(() => cloche.Find("[data-testid='cloche-bouton']").Click());
        cloche.WaitForAssertion(
            () => Assert.NotEmpty(cloche.FindAll("[data-testid='cloche-panneau'] [data-testid='cloche-notif'][data-type='echange']")),
            TimeSpan.FromSeconds(10));

        // When — le RECEVANT REFUSE (autre écran = POST refus).
        var client = GrilleRuntimeHarness.ClientVers(api);
        (await client.PostAsJsonAsync("api/canal/refuser-proposition", new RepondrePropositionRequete(propositionId))).EnsureSuccessStatusCode();

        var propositionRefusee = api.Services.GetRequiredService<IPropositionEchangeRepository>().AllSnapshots().Single(p => p.Id == propositionId);
        var changement = api.Services.GetRequiredService<INotificateurChangement>();
        using var diffusionContinue = new CancellationTokenSource();
        var pousseur = Task.Run(async () =>
        {
            while (!diffusionContinue.IsCancellationRequested)
            {
                changement.NotifierProposition(propositionRefusee);
                try { await Task.Delay(150, diffusionContinue.Token); }
                catch (TaskCanceledException) { break; }
            }
        });

        try
        {
            // Then — la notification d'échange se CLÔT (retirée par reprojection), et AUCUNE surcharge n'est écrite.
            cloche.WaitForAssertion(
                () => Assert.Empty(cloche.FindAll("[data-testid='cloche-panneau'] [data-testid='cloche-notif'][data-type='echange']")),
                TimeSpan.FromSeconds(15));
            Assert.Empty(api.Services.GetRequiredService<IPeriodeRepository>().AllSnapshots());
        }
        finally
        {
            diffusionContinue.Cancel();
            await pousseur;
        }
    }
}
