using System;
using System.IO;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 26 — Sc.7 (🖥️ @ihm, connexion) — la page d'entrée est soignée (« Cocon élevé ») et les boutons
/// OAuth sont habillés. Test de NIVEAU RUNTIME (vraie page <see cref="Connexion"/> câblée à l'API distante
/// réelle) : la page présente une carte tokenisée et une marque, et chaque bouton provider porte une
/// identité visuelle (classe <c>oauth-*</c>) tout en gardant ses data-testid et son flux d'auth (Sc.16
/// s25 le prouve : le clic déclenche toujours le vrai flux). Une garde d'asset vérifie la tokenisation
/// (surface/bordure via <c>--pdg-*</c>, marque en Fraunces) — condition du rendu correct clair ET sombre.
/// </summary>
public sealed class FrontWasmConnexionPageSoigneeTests : TestContext
{
    private void CablerConnexion(ApiDistanteFactory api)
    {
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton<IPersistanceSession>(new PersistanceSessionInerte());
        Services.AddSingleton(new SessionPlanning());
    }

    [Fact]
    public void La_page_presente_une_carte_tokenisee_une_marque_et_des_boutons_OAuth_habilles()
    {
        // Given — la vraie page /connexion câblée à l'API distante réelle.
        using var api = new ApiDistanteFactory();
        CablerConnexion(api);

        // When — la page s'affiche.
        var connexion = RenderComponent<Connexion>();

        // Then — carte d'entrée soignée + marque.
        Assert.NotNull(connexion.Find(".connexion-carte"));
        Assert.NotNull(connexion.Find(".connexion-marque"));

        // … et les boutons OAuth portent chacun leur identité visuelle (data-testid + flux inchangés).
        Assert.NotNull(connexion.Find("[data-testid='bouton-oauth-google'].oauth-google"));
        Assert.NotNull(connexion.Find("[data-testid='bouton-oauth-microsoft'].oauth-microsoft"));
        Assert.NotNull(connexion.Find("[data-testid='bouton-oauth-apple'].oauth-apple"));
    }

    [Fact]
    public void La_page_de_connexion_est_tokenisee_et_la_marque_en_Fraunces()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "PlanningDeGarde.slnx")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        var razor = File.ReadAllText(Path.Combine(
            dir!.FullName, "src", "PlanningDeGarde.Web", "Components", "Pages", "Connexion.razor"));

        Assert.Contains(".connexion-carte", razor, StringComparison.Ordinal);
        Assert.Contains("var(--pdg-card)", razor, StringComparison.Ordinal);
        Assert.Contains("var(--pdg-font-titre)", razor, StringComparison.Ordinal);
    }
}
