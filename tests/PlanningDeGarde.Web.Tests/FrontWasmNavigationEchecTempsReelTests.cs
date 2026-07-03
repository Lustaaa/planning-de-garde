using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using Bunit;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Web;
using PlanningDeGarde.Web.Components.Pages;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.6 (🖥️ IHM, <c>@erreur</c>) — API distante injoignable pendant la
/// navigation. Sur la <b>vraie</b> grille (<see cref="PlanningPartage"/>) câblée à une <b>API distante
/// réelle</b> (<see cref="ApiDistanteFactory"/>), la fenêtre par défaut démarre au lundi 08/06/2026, mais
/// la re-requête de la date naviguée (<c>GET /api/grille/2026/6/15</c>) subit un <b>échec de transport
/// déterministe</b> (API injoignable). Un clic « Semaine suivante » doit alors :
/// <list type="bullet">
///   <item>laisser la <b>fenêtre affichée inchangée</b> (toujours le lundi 08/06, pas de grille vide) ;</item>
///   <item>afficher un <b>message d'échec clair</b> ;</item>
///   <item><b>ne rien mettre en file ni rejouer</b> (règle 28 — échec clair, sans file ni rejeu).</item>
/// </list>
///
/// <para><b>Anti « vert qui ment » (non-divergence de l'ancre).</b> L'observable décisif : après l'échec,
/// un clic « Semaine précédente » (qui, lui, transite) ramène au lundi <b>01/06</b>. Cela ne tient QUE si
/// l'ancre est restée au 08/06 pendant l'échec — si elle avait silencieusement avancé au 15/06,
/// « précédente » ramènerait au 08/06. La preuve runtime distingue donc une fenêtre figée à l'affichage
/// d'un état de navigation divergent. Un bUnit à doublure de transport ne reproduit pas l'échec HTTP réel.</para>
/// </summary>
public sealed class FrontWasmNavigationEchecTempsReelTests : TestContext
{
    private static readonly DateTime Mercredi_10_06_2026 = new(2026, 6, 10);

    [Fact]
    public void Should_Conserver_la_fenetre_courante_du_lundi_08_06_2026_et_afficher_un_echec_clair_sans_mise_en_file_ni_rejeu_When_une_navigation_echoue_parce_que_l_API_distante_est_injoignable_sur_l_app_reellement_cablee()
    {
        // Given — l'API distante réelle, un cycle de fond semé (la grille rend des cases non triviales).
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerCycle(api,
            new CycleDeFond(2, new Dictionary<int, string> { [0] = "parent-a", [1] = "parent-b" }));

        // … client HTTP du front dont la SEULE re-requête de la date naviguée (lundi 15/06) est injoignable
        // (échec de transport déterministe). Le chargement initial (08/06) et le retour (01/06) transitent.
        var client = GrilleRuntimeHarness.ClientVersAvecLectureGrilleInjoignable(api, "/grille/2026/6/15");

        // … la grille réelle câblée, aujourd'hui = mercredi 10/06/2026 : la fenêtre par défaut (4 semaines)
        // démarre au lundi 08/06 (semaine en cours).
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Mercredi_10_06_2026, client);
        Assert.Equal("08/06", DatePremiereCase(grille));

        // When — clic « Semaine suivante » : la re-requête de la date naviguée (15/06) échoue (API injoignable).
        this.SurDispatcher(() => grille.Find("[data-testid='nav-semaine-suivante']").Click());

        // Then — un message d'échec clair s'affiche (le symptôme PO : l'écran signale l'échec, sans planter).
        grille.WaitForState(
            () => grille.FindAll("[data-testid='echec-navigation']").Count == 1,
            TimeSpan.FromSeconds(10));

        // … et la fenêtre affichée reste celle du lundi 08/06 (pas de grille vide, pas de saut au 15/06).
        Assert.Equal("08/06", DatePremiereCase(grille));

        // When — « Semaine précédente » (qui transite) depuis l'ancre conservée au 08/06.
        this.SurDispatcher(() => grille.Find("[data-testid='nav-semaine-precedente']").Click());
        grille.WaitForState(() => DatePremiereCase(grille) == "01/06", TimeSpan.FromSeconds(10));

        // Then — la fenêtre démarre au lundi 01/06 : la précédente est partie du 08/06, PAS du 15/06 — l'ancre
        // n'a donc PAS avancé pendant l'échec (aucun rejeu, aucune mise en file de la navigation échouée),
        // et le message d'échec s'efface dès qu'une navigation aboutit.
        Assert.Equal("01/06", DatePremiereCase(grille));
        Assert.Empty(grille.FindAll("[data-testid='echec-navigation']"));
    }

    // Première case rendue de la fenêtre = coin haut-gauche = début de fenêtre (Semaines[0].Jours[0]).
    private static IElement PremiereCase(IRenderedComponent<PlanningPartage> grille)
        => grille.FindAll("[data-testid='jour-case']")[0];

    private static string DatePremiereCase(IRenderedComponent<PlanningPartage> grille)
        => PremiereCase(grille).QuerySelector(".grille-jour-date")!.TextContent.Trim();
}
