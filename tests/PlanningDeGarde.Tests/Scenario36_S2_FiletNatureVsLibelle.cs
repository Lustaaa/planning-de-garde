using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 36 — Sc.2 — Filet d'invariant révisé : nature (TypeActeur) ≠ libellé de rôle (@back, option A)
//   Étant donné un acteur de type TypeActeur.Parent SANS aucun rôle affecté (RoleDe = null)
//   Quand la commande « lier » le désigne comme parent → ACCEPTÉ (le rôle n'est plus requis)
//   Étant donné un acteur de type TypeActeur.Autre (Mamie) à qui on affecte un rôle LIBELLÉ « Parent »
//   Quand la commande « lier » le désigne comme parent → REFUSÉ (TypeActeur ≠ Parent), motif restitué,
//      AUCUNE écriture partielle (les liens existants de l'enfant intacts), le libellé « Parent » SANS effet
//
// FILET DE NON-RÉGRESSION (early-green attendu, cf. fichier de sprint) : ces deux contrepoints sont verts
// par construction depuis la bascule Sc.1 — ils VERROUILLENT la source de vérité unique (le TYPE qualifie,
// jamais le libellé) contre une réintroduction future du critère « rôle du référentiel ».
public class Scenario36_S2_FiletNatureVsLibelle
{
    private const string LeaId = "enfant-lea";
    private const string GrandPere = "grand-pere"; // acteur seed de type TypeActeur.Autre (Foyer.TypesParActeur)

    // ---------- Contrepoint 1 — Parent sans rôle : le rôle n'est plus requis pour l'éligibilité ----------
    [Fact]
    public void Should_Lier_un_TypeActeur_Parent_sans_aucun_role_When_le_role_n_est_plus_requis()
    {
        var config = new ConfigurationFoyerEnMemoire();
        var papaId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Papa")).Valeur!.ActeurId;
        Assert.Equal(TypeActeur.Parent, config.TypeDe(papaId));
        Assert.Null(config.RoleDe(papaId)); // AUCUN rôle affecté

        var referentiel = new FakeReferentielEnfants().AvecEnfant(LeaId, "Léa");
        var handler = new LierEnfantParentHandler(referentiel, config, referentiel);

        var resultat = handler.Handle(new LierEnfantParentCommand(LeaId, papaId));

        Assert.True(resultat.EstSucces);
        Assert.Contains(papaId, referentiel.EnumererEnfants().Single(e => e.Id == LeaId).ParentsLies);
    }

    // ---------- Contrepoint 2 — Autre (Mamie) AVEC un rôle libellé « Parent » : refusé, sans effet du libellé ----------
    [Fact]
    public void Should_Refuser_un_TypeActeur_Autre_sans_ecriture_partielle_When_il_porte_meme_un_role_libelle_Parent()
    {
        // Given — Mamie est un acteur seed de type TypeActeur.Autre, à qui on affecte NÉANMOINS le rôle
        // du référentiel dont le libellé est littéralement « Parent » (pour prouver que ce libellé est inerte).
        var roles = new ReferentielRolesEnMemoire();
        var roleParent = new CreerRoleHandler(roles, roles).Handle(new CreerRoleCommand("Parent")).Valeur!.RoleId;
        var config = new ConfigurationFoyerEnMemoire();
        new AffecterRoleActeurHandler(roles, config).Handle(new AffecterRoleActeurCommand(GrandPere, roleParent));
        Assert.Equal(TypeActeur.Autre, config.TypeDe(GrandPere));
        Assert.Equal(roleParent, config.RoleDe(GrandPere)); // porte bien le rôle libellé « Parent »

        // Un lien existant préalable de l'enfant, pour prouver l'absence d'écriture partielle sur refus.
        var papa = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Papa")).Valeur!.ActeurId;
        var referentiel = new FakeReferentielEnfants().AvecEnfant(LeaId, "Léa");
        referentiel.LierParent(LeaId, papa);
        var handler = new LierEnfantParentHandler(referentiel, config, referentiel);

        // When — on tente de lier Mamie (Autre) malgré son libellé de rôle « Parent ».
        var resultat = handler.Handle(new LierEnfantParentCommand(LeaId, GrandPere));

        // Then — REFUS (TypeActeur ≠ Parent) : le libellé « Parent » n'a AUCUN effet ; motif restitué.
        Assert.False(resultat.EstSucces);
        Assert.Equal("acteur non parent", resultat.Motif);

        // Et AUCUNE écriture partielle : le lien préexistant est intact, Mamie n'est pas liée.
        var lea = referentiel.EnumererEnfants().Single(e => e.Id == LeaId);
        Assert.Single(lea.ParentsLies);
        Assert.Contains(papa, lea.ParentsLies);
        Assert.DoesNotContain(GrandPere, lea.ParentsLies);
    }
}
