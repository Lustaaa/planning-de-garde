using System;
using Bunit;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 24 — Sc.10 (🖥️ @ihm — acceptation de NIVEAU RUNTIME) : le <b>bandeau de connexion inline</b> de la
/// vue planning est <b>retiré</b> (retour PO : « sans aucun sens ») — <b>un seul chemin d'entrée</b> = la page
/// de connexion dédiée (Sc.8). Sur la vue planning réellement câblée (<see cref="PlanningPartage"/>, API
/// distante réelle <see cref="ApiDistanteFactory"/>, store réel, DI réelle, hub SignalR réel), aucune
/// affordance de connexion inline n'est exposée (ni champ email, ni bouton « Se connecter », ni motif inline),
/// tandis que le <b>reste du planning fonctionne</b> (grille 28 cases 4 semaines, légende, sélecteurs
/// d'incarnation / rôle / vue, barre de navigation — non-régression s19/s20).
/// </summary>
public sealed class FrontWasmPlanningSansBandeauLoginInlineRuntimeTests : TestContext
{
    [Fact]
    public void Le_planning_n_expose_plus_le_bandeau_login_inline_mais_le_reste_du_planning_fonctionne()
    {
        // Given/When — la vue planning réellement câblée à l'API distante réelle est rendue (fenêtre par
        // défaut projetée : 28 cases-jour = 4 semaines glissantes). Une période d'Alice (parent-a) est semée
        // dans la fenêtre pour que la légende soit peuplée (contrôle de non-régression de la lecture).
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerPeriode(api, "parent-a",
            GrilleRuntimeHarness.Lundi_29_06_2026.AddDays(1),
            GrilleRuntimeHarness.Lundi_29_06_2026.AddDays(1).AddHours(12));
        var planning = GrilleRuntimeHarness.RendreGrille(this, api, GrilleRuntimeHarness.Lundi_29_06_2026);

        // Then — plus AUCUNE affordance de connexion inline : ni champ email, ni bouton « Se connecter »,
        // ni état/motif de connexion inline (un seul chemin d'entrée = la page dédiée /connexion, Sc.8).
        Assert.Empty(planning.FindAll("[data-testid='champ-email-connexion']"));
        Assert.Empty(planning.FindAll("[data-testid='bouton-se-connecter']"));
        Assert.Empty(planning.FindAll("[data-testid='motif-connexion']"));
        Assert.Empty(planning.FindAll("[data-testid='bandeau-connexion']"));

        // Non-régression du reste du planning (s19/s20) — grille, légende, sélecteurs et navigation intacts.
        Assert.Equal(28, planning.FindAll("[data-testid='jour-case']").Count);
        Assert.NotEmpty(planning.FindAll("[data-testid='legende-entree']"));
        Assert.NotNull(planning.Find("[data-testid='selecteur-incarnation']"));
        Assert.NotNull(planning.Find("[data-testid='selecteur-vue']"));
        Assert.NotNull(planning.Find("[data-testid='barre-navigation']"));
    }
}
