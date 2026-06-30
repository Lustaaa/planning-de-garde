using System.Linq;
using Xunit;
using FoyerWeb = PlanningDeGarde.Web.Foyer;
using FoyerDomaine = PlanningDeGarde.Infrastructure.Foyer;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 19 — Sc.4 (@back, borne) — Aucune constante de domaine AFFICHABLE n'expose un libellé
/// fictif « Parent A » / « Parent B ». Garde-fou de cohérence (pas un test runtime/bUnit : pure
/// assertion sur des constantes statiques) qui verrouille le retrait des libellés fictifs des
/// référentiels d'affichage consommés par les sélecteurs de l'IHM (<c>Foyer.Responsables</c>,
/// peuplant les dialogs d'affectation et de transfert ; <c>Foyer.ActeursEditables</c>, peuplant
/// l'écran de configuration). Le seed BACKEND (InMemory, <c>Infrastructure.Foyer.NomsParResponsable</c>)
/// est conservé (asymétrie s15) mais porte des libellés réels/neutres — il ne doit pas davantage
/// exposer « Parent A » / « Parent B ».
///
/// Les identifiants stables (<c>parent-a</c> / <c>parent-b</c>) restent intacts : seule la chaîne
/// d'affichage fictive est proscrite, jamais la clé de résolution.
/// </summary>
public sealed class AucunLibelleFictifExposeTests
{
    private static readonly string[] LibellesFictifs = { "Parent A", "Parent B" };

    [Fact]
    public void Les_referentiels_d_affichage_des_selecteurs_n_exposent_aucun_libelle_fictif_Parent_A_ou_Parent_B()
    {
        var libellesSelecteurs = FoyerWeb.Responsables.Select(r => r.Libelle)
            .Concat(FoyerWeb.ActeursEditables.Select(a => a.Libelle));

        Assert.DoesNotContain(libellesSelecteurs, l => LibellesFictifs.Contains(l));
    }

    [Fact]
    public void Le_seed_backend_conserve_pour_la_non_regression_n_expose_aucun_libelle_fictif_Parent_A_ou_Parent_B()
    {
        Assert.DoesNotContain(FoyerDomaine.NomsParResponsable.Values, n => LibellesFictifs.Contains(n));
    }
}
