using System;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Web;
using PlanningDeGarde.Web.Components.Shared.Layout;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 47 — ajustement PO (gate visuel) : PLACEMENT de la cloche dans la BARRE D'APPLICATION (MainLayout),
/// dans l'ordre exact <b>Déconnexion — Cloche — Sombre</b>. Test d'INTÉGRATION de placement : rend le vrai
/// <see cref="MainLayout"/> (barre + ses actions réelles : menu utilisateur, cloche, bascule de thème) et
/// vérifie la STRUCTURE DOM — la cloche est bien un enfant de <c>.app-bar-actions</c>, ENTRE le menu utilisateur
/// et la bascule de thème. Vérifie aussi le GATING dans le layout (présent sur toutes les routes) : PAS de cloche
/// hors session (écran de connexion). La preuve fonctionnelle finale reste le gate navigateur du PO ; ici on
/// caractérise l'ordre/gating, que les tests de comportement (rendus en isolation) complètent.
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmClochePlacementLayoutTests : TestContext
{
    private sealed class PersistanceNulle : IPersistanceSession
    {
        public ValueTask PersisterAsync(SessionPersistee jeton) => ValueTask.CompletedTask;
        public ValueTask<SessionPersistee?> LireAsync() => ValueTask.FromResult<SessionPersistee?>(null);
        public ValueTask PurgerAsync() => ValueTask.CompletedTask;
    }

    private sealed class PreferencesClaires : IPreferencesTheme
    {
        public ValueTask<string> ThemeCourantAsync() => ValueTask.FromResult("clair");
        public ValueTask DefinirAsync(string theme) => ValueTask.CompletedTask;
    }

    private void CablerLayout(ApiDistanteFactory api, SessionPlanning session)
    {
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton(session);
        Services.AddSingleton<IPersistanceSession>(new PersistanceNulle());
        Services.AddSingleton<IPreferencesTheme>(new PreferencesClaires());
        Services.AddSingleton(new OptionsConnexionHub
        {
            Configurer = options =>
            {
                options.HttpMessageHandlerFactory = _ => api.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            },
        });
    }

    private IRenderedComponent<MainLayout> RendreLayout()
        => RenderComponent<MainLayout>(p => p.Add(l => l.Body, (RenderFragment)(_ => { })));

    private static SessionPlanning ParentConnecte()
    {
        var session = new SessionPlanning();
        session.Connecter("Alice", "parent-a", TypeActeur.Parent);
        return session;
    }

    [Fact]
    public void La_cloche_est_dans_la_barre_entre_le_menu_utilisateur_et_la_bascule_de_theme()
    {
        using var api = new ApiDistanteFactory();
        CablerLayout(api, ParentConnecte());

        var layout = RendreLayout();

        // La cloche vit bien dans le groupe d'actions de la barre.
        var actions = layout.Find(".app-bar-actions");
        Assert.NotNull(actions.QuerySelector("[data-testid='cloche-bouton']"));

        // Ordre PO exact : Déconnexion (menu utilisateur) — Cloche — Sombre (bascule de thème).
        var html = actions.InnerHtml;
        var posMenu = html.IndexOf("menu-se-deconnecter", StringComparison.Ordinal);
        var posCloche = html.IndexOf("cloche-bouton", StringComparison.Ordinal);
        var posTheme = html.IndexOf("bascule-theme", StringComparison.Ordinal);
        Assert.True(posMenu >= 0 && posCloche >= 0 && posTheme >= 0);
        Assert.True(posMenu < posCloche, "Le menu (déconnexion) doit précéder la cloche.");
        Assert.True(posCloche < posTheme, "La cloche doit précéder la bascule de thème.");
    }

    [Fact]
    public void Hors_session_aucune_cloche_dans_la_barre_pas_de_cloche_sur_l_ecran_de_connexion()
    {
        using var api = new ApiDistanteFactory();
        CablerLayout(api, new SessionPlanning()); // aucune connexion : écran de connexion / route publique

        var layout = RendreLayout();

        // Gating identique au menu utilisateur : rien tant qu'aucune session n'est ouverte.
        Assert.Empty(layout.FindAll("[data-testid='cloche-bouton']"));
        Assert.Empty(layout.FindAll("[data-testid='menu-utilisateur']"));
        // La bascule de thème, elle, reste disponible partout (repère : le layout n'est pas cassé).
        Assert.NotEmpty(layout.FindAll("[data-testid='bascule-theme']"));
    }
}
