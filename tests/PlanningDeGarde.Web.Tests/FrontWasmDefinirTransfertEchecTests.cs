using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.3 (🖥️ scénario IHM, <c>@erreur</c> — palier 7, 3ᵉ dialog) :
/// <b>caractérisation early-green</b> de l'issue d'échec (règle 28 — un seul observable). On rend la
/// <b>vraie</b> grille <see cref="Web.Components.Pages.PlanningPartage"/> (front WASM) câblée à une
/// <b>API distante réelle</b> (<see cref="ApiDistanteFactory"/>, store réel, canal HTTP réel). Quel que
/// soit le mode d'échec — <b>refus du domaine</b> (transfert incomplet, motif propagé par le canal) ou
/// <b>API injoignable</b> (transport réellement coupé) — l'observable converge : la dialog <b>reste
/// ouverte</b>, le message s'affiche <b>dans</b> la dialog, la saisie est <b>conservée</b>, et la grille
/// reste inchangée (aucun accusé de succès, aucune écriture dans le store).
///
/// Anti « vert qui ment » : sur API injoignable, le transport est réellement coupé au niveau du handler
/// (<see cref="GrilleRuntimeHarness.ClientVersAvecEcritureInjoignable"/>), pas une doublure renvoyant un
/// 4xx ; on vérifie qu'aucune écriture n'a transité (store vide). Sur refus domaine, le motif observé
/// vient du Result réel du use case relayé par le canal HTTP réel, jamais d'une règle dupliquée dans l'UI.
/// </summary>
public sealed class FrontWasmDefinirTransfertEchecTests : TestContext
{
    // Vendredi 19/06/2026 : la case cliquée (référence dans la fenêtre démarrant au lundi 15/06).
    private static readonly DateTime Vendredi_19_06_2026 = new(2026, 6, 19);

    [Fact]
    public void Should_Laisser_la_dialog_ouverte_avec_le_motif_de_refus_dans_la_dialog_et_la_saisie_conservee_et_aucune_ecriture_When_le_domaine_refuse_le_transfert_incomplet()
    {
        // Given — la grille réellement câblée à l'API distante (store réel vierge), fenêtre couvrant le 19/06.
        using var api = new ApiDistanteFactory();
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Vendredi_19_06_2026);

        // When — un Parent ouvre la dialog depuis la case du vendredi 19/06, saisit « Parent A » dépositaire
        // et lieu « École » MAIS SANS récupérateur (transfert incomplet), puis valide.
        OuvrirDialogTransfert(grille, "19/06");
        grille.SurDispatcher(() => grille.Find("[data-testid='dialog-definir-transfert'] [data-testid='champ-depose']").Change("parent-a"));
        grille.SurDispatcher(() => grille.Find("[data-testid='dialog-definir-transfert'] [data-testid='champ-lieu']").Change("école"));
        grille.SurDispatcher(() => grille.Find("[data-testid='dialog-definir-transfert'] form").Submit());

        // Then — la dialog reste OUVERTE, le motif de refus du domaine s'affiche DANS la dialog (le motif
        // réel propagé par le canal porte « récupération »), aucun accusé de succès.
        grille.WaitForAssertion(
            () =>
            {
                Assert.NotEmpty(grille.FindAll("[data-testid='dialog-definir-transfert']"));
                var motif = grille.Find("[data-testid='dialog-definir-transfert'] [data-testid='motif-echec']");
                Assert.Contains("récupération", motif.TextContent, StringComparison.OrdinalIgnoreCase);
                Assert.Empty(grille.FindAll("[data-testid='accuse-transfert-defini']"));
            },
            TimeSpan.FromSeconds(10));

        // … la saisie est conservée à resoumettre (le dépositaire choisi est toujours là).
        Assert.Equal(
            "parent-a",
            grille.Find("[data-testid='dialog-definir-transfert'] [data-testid='champ-depose']").GetAttribute("value"));

        // … et aucun transfert n'a été inscrit dans le store de l'API distante (grille inchangée).
        using var scope = api.Services.CreateScope();
        Assert.Empty(scope.ServiceProvider.GetRequiredService<ITransfertRepository>().AllSnapshots());
    }

    [Fact]
    public void Should_Laisser_la_dialog_ouverte_avec_le_message_service_injoignable_dans_la_dialog_et_la_saisie_conservee_et_aucune_ecriture_When_l_API_est_injoignable()
    {
        // Given — la grille câblée à l'API distante, MAIS l'écriture « definir-transfert » subit un échec de
        // transport déterministe (handler levant HttpRequestException avant tout aller-retour — robuste vs
        // proxy loopback Docker). La lecture initiale de la grille transite normalement.
        using var api = new ApiDistanteFactory();
        var clientCoupe = GrilleRuntimeHarness.ClientVersAvecEcritureInjoignable(api, "definir-transfert");
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Vendredi_19_06_2026, clientCoupe);

        // When — un Parent ouvre la dialog depuis la case du vendredi 19/06, saisit un transfert COMPLET
        // (Parent A → Parent B, École, 08:30), puis valide alors que le service est injoignable.
        OuvrirDialogTransfert(grille, "19/06");
        grille.SurDispatcher(() => grille.Find("[data-testid='dialog-definir-transfert'] [data-testid='champ-depose']").Change("parent-a"));
        grille.SurDispatcher(() => grille.Find("[data-testid='dialog-definir-transfert'] [data-testid='champ-recupere']").Change("parent-b"));
        grille.SurDispatcher(() => grille.Find("[data-testid='dialog-definir-transfert'] [data-testid='champ-lieu']").Change("école"));
        grille.SurDispatcher(() => grille.Find("[data-testid='dialog-definir-transfert'] [data-testid='champ-heure']").Change("08:30"));
        grille.SurDispatcher(() => grille.Find("[data-testid='dialog-definir-transfert'] form").Submit());

        // Then — même issue unique : la dialog reste OUVERTE, le message « service injoignable » s'affiche
        // DANS la dialog, aucun accusé de succès.
        grille.WaitForAssertion(
            () =>
            {
                Assert.NotEmpty(grille.FindAll("[data-testid='dialog-definir-transfert']"));
                var motif = grille.Find("[data-testid='dialog-definir-transfert'] [data-testid='motif-echec']");
                Assert.Contains(MessagesEcriture.ServiceInjoignable, motif.TextContent);
                Assert.Empty(grille.FindAll("[data-testid='accuse-transfert-defini']"));
            },
            TimeSpan.FromSeconds(10));

        // … la saisie est conservée à resoumettre.
        Assert.Equal(
            "parent-a",
            grille.Find("[data-testid='dialog-definir-transfert'] [data-testid='champ-depose']").GetAttribute("value"));

        // … et AUCUNE écriture n'a transité jusqu'au store de l'API distante (transport réellement coupé).
        using var scope = api.Services.CreateScope();
        Assert.Empty(scope.ServiceProvider.GetRequiredService<ITransfertRepository>().AllSnapshots());
    }

    // Ouvre la dialog « Définir un transfert » depuis la case ciblée, idempotent sous WaitForAssertion
    // (robuste aux re-renders async de la connexion du hub SignalR du harnais).
    private static void OuvrirDialogTransfert(IRenderedComponent<Web.Components.Pages.PlanningPartage> grille, string jjMM)
        => grille.WaitForAssertion(
            () =>
            {
                grille.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, jjMM).Click());
                grille.SurDispatcher(() => grille.Find("[data-testid='action-definir-transfert']").Click());
                Assert.NotEmpty(grille.FindAll("[data-testid='dialog-definir-transfert']"));
            },
            TimeSpan.FromSeconds(10));
}
