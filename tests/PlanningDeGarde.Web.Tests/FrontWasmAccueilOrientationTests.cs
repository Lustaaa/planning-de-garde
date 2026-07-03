using System;
using System.IO;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 26 — Sc.8 (🖥️ @ihm, accueil) — le point d'entrée oriente clairement. Test de NIVEAU RUNTIME sur
/// la vraie page <see cref="Home"/> : elle rend une orientation « Cocon élevé » (marque + lien) tout en
/// conservant son flux — la landing redirige toujours vers la page de connexion dédiée (s24 Sc.8). Le lien
/// d'orientation mène à la MÊME destination qu'avant (/connexion). Une garde d'asset vérifie la tokenisation
/// (surface/marque via <c>--pdg-*</c> et Fraunces) — condition du rendu correct clair ET sombre.
/// </summary>
public sealed class FrontWasmAccueilOrientationTests : TestContext
{
    [Fact]
    public void L_accueil_oriente_avec_une_marque_et_un_lien_et_redirige_toujours_vers_la_connexion()
    {
        // Given — un utilisateur non connecté ouvre l'app (route « / » = Home).
        Services.AddSingleton(new SessionPlanning());
        var nav = Services.GetRequiredService<NavigationManager>();

        // When — la landing est rendue.
        var home = RenderComponent<Home>();

        // Then — une orientation « Cocon élevé » est rendue (marque + lien vers la connexion)…
        Assert.NotNull(home.Find("[data-testid='accueil-orientation']"));
        var lien = home.Find("[data-testid='accueil-lien-connexion']");
        Assert.EndsWith("connexion", lien.GetAttribute("href"), StringComparison.OrdinalIgnoreCase);

        // … ET le flux d'orientation est inchangé : la landing redirige vers la page de connexion dédiée.
        Assert.EndsWith("connexion", nav.Uri, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("planning", nav.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void L_accueil_est_tokenise_et_la_marque_en_Fraunces()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "PlanningDeGarde.slnx")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        var razor = File.ReadAllText(Path.Combine(
            dir!.FullName, "src", "PlanningDeGarde.Web", "Components", "Pages", "Home.razor"));

        Assert.Contains("accueil-orientation", razor, StringComparison.Ordinal);
        Assert.Contains("var(--pdg-", razor, StringComparison.Ordinal);
        Assert.Contains("var(--pdg-font-titre)", razor, StringComparison.Ordinal);
    }
}
