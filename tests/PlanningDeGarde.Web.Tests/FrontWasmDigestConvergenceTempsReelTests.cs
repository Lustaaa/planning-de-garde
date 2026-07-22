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
using PlanningDeGarde.Web.State;
using Xunit;
using static PlanningDeGarde.Web.CanalEcriture;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 50 — Sc.7 (🖥️ @ihm) — acceptation de NIVEAU RUNTIME : le DIGEST d'un 2ᵉ écran CONVERGE en TEMPS RÉEL
/// quand un changement est écrit depuis un AUTRE écran, par REPROJECTION CLIENT depuis la GRILLE relue via la
/// diffusion SignalR de LECTURE SEULE (s20) — AUCUN GET DÉDIÉ digest sur push (le digest se recalcule de la MÊME
/// grille reprojetée ; garde-fou anti-flake, même contrat que la convergence de case s44). La diffusion porte une
/// DONNÉE DE LECTURE : elle ne déclenche aucune écriture (canaux séparés).
///
/// Anti « vert qui ment » : l'écriture part réellement du canal (POST) d'un autre écran ; la convergence passe
/// par la diffusion SignalR RÉELLE + la projection réelle — jamais une doublure. La diffusion est repoussée en
/// boucle de fond (idempotente : le store est déjà muté) pour qu'un push tombe APRÈS l'établissement du hub.
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmDigestConvergenceTempsReelTests : TestContext
{
    private static readonly DateTime Lundi_29_06_2026 = new(2026, 6, 29);
    private static readonly DateOnly Mercredi_08_07_2026 = new(2026, 7, 8); // jour à venir, sans transfert au départ

    private static SessionPlanning SessionParent()
    {
        var session = new SessionPlanning();
        session.Connecter("Alice", "parent-a", TypeActeur.Parent);
        return session;
    }

    [Fact]
    public async Task Le_digest_du_2e_ecran_converge_sur_le_transfert_derive_par_reprojection_sans_ecriture()
    {
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerCycle(api, new CycleDeFond(2, new Dictionary<int, string> { [0] = "parent-a", [1] = "parent-b" }));

        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
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

        // Écran observé : la grille (charge + publie le digest) + la cloche (rend le digest), panneau ouvert.
        var grille = RenderComponent<PlanningPartage>();
        grille.WaitForState(() => grille.FindAll("[data-testid='jour-case']").Count == 28, TimeSpan.FromSeconds(10));
        var cloche = RenderComponent<Cloche>();
        this.SurDispatcher(() => cloche.Find("[data-testid='cloche-bouton']").Click());

        // Précondition — le 08/07 (mercredi, même semaine ISO que ses voisins) ne porte PAS de transfert : absent
        // de la section « à venir » du digest.
        cloche.WaitForAssertion(
            () => Assert.NotEmpty(cloche.FindAll("[data-testid='digest-section']")), TimeSpan.FromSeconds(10));
        Assert.DoesNotContain(
            cloche.FindAll("[data-testid='digest-avenir-jour']"),
            j => j.GetAttribute("data-jour") == "2026-07-08");

        // When — depuis un AUTRE écran (client réel), on délègue la récupération du 08/07 à la nounou : la
        // résolution du 08/07 bascule → un TRANSFERT dérivé apparaît ce jour-là (canal d'écriture).
        var autreEcran = GrilleRuntimeHarness.ClientVers(api);
        (await autreEcran.PostAsJsonAsync(
            "api/delegations",
            new DeleguerRecuperationRequete(Mercredi_08_07_2026, "Léa", "nounou"))).EnsureSuccessStatusCode();

        var nbPeriodesApresEcriture = api.Services.GetRequiredService<IPeriodeRepository>().AllSnapshots().Count;

        var notificateur = api.Services.GetRequiredService<INotificateurPlanning>();
        using var diffusionContinue = new CancellationTokenSource();
        var pousseur = Task.Run(async () =>
        {
            while (!diffusionContinue.IsCancellationRequested)
            {
                notificateur.NotifierMiseAJour();
                try { await Task.Delay(150, diffusionContinue.Token); }
                catch (TaskCanceledException) { break; }
            }
        });

        try
        {
            // Then — SANS aucun GET dédié digest, le digest CONVERGE : le 08/07 apparaît dans « à venir »
            // (reprojection du transfert dérivé depuis la grille relue par la diffusion de lecture seule).
            cloche.WaitForAssertion(
                () => Assert.Contains(
                    cloche.FindAll("[data-testid='digest-avenir-jour']"),
                    j => j.GetAttribute("data-jour") == "2026-07-08"),
                TimeSpan.FromSeconds(15));

            // And — la DIFFUSION porte une donnée de LECTURE : elle n'a écrit aucune surcharge (le nombre de
            // périodes reste celui d'APRÈS l'unique écriture du canal — la boucle de push ne mute rien).
            Assert.Equal(nbPeriodesApresEcriture, api.Services.GetRequiredService<IPeriodeRepository>().AllSnapshots().Count);
        }
        finally
        {
            diffusionContinue.Cancel();
            await pousseur;
        }
    }
}
