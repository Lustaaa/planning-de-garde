using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Web;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 31 — Sc.4 (🖥️ @ihm — acceptation de NIVEAU RUNTIME) : bouton œil afficher/masquer le mot de passe.
/// La <b>vraie</b> page de connexion (<see cref="Connexion"/>, réellement rendue) présente un champ mot de
/// passe <b>masqué par défaut</b> (<c>type="password"</c>). Activer le bouton œil rend la saisie <b>visible en
/// clair</b> (<c>type="text"</c>) ; le ré-activer la <b>re-masque</b> (<c>type="password"</c>). Pur confort de
/// saisie : aucun flux d'auth, aucune règle métier touchés — seul l'attribut <c>type</c> du champ bascule.
/// </summary>
public sealed class FrontWasmConnexionBoutonOeilMotDePasseRuntimeTests : TestContext
{
    private static string TypeChamp(IRenderedComponent<Connexion> page)
        => page.Find("[data-testid='champ-mot-de-passe-connexion']").GetAttribute("type")!;

    [Fact]
    public void Should_basculer_le_champ_entre_masque_et_visible_When_on_active_le_bouton_oeil()
    {
        using var api = new ApiDistanteFactory();
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton(new SessionPlanning());
        Services.AddSingleton<IPersistanceSession>(new PersistanceSessionInerte());

        var connexion = RenderComponent<Connexion>();

        // Given — le champ mot de passe est masqué par défaut.
        Assert.Equal("password", TypeChamp(connexion));

        // When — j'active le bouton œil → la saisie devient visible en clair.
        this.SurDispatcher(() => connexion.Find("[data-testid='bouton-oeil-mot-de-passe']").Click());
        Assert.Equal("text", TypeChamp(connexion));

        // When — je ré-active le bouton œil → la saisie redevient masquée.
        this.SurDispatcher(() => connexion.Find("[data-testid='bouton-oeil-mot-de-passe']").Click());
        Assert.Equal("password", TypeChamp(connexion));
    }
}
