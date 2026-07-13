using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 34 — S1 — Modèle enfant enrichi d'un lien parents + commande « lier » (@back)
//   Étant donné un enfant déclaré dans le foyer (id stable + prénom, s30)
//   Et un acteur du foyer portant le rôle « Parent »
//   Quand la commande « lier un enfant à un parent » est émise (enfantId, acteurId)
//   Alors le lien enfant→parent est porté par le modèle ET persisté (relu par la query)
//   Et la query relit l'enfant avec la LISTE de ses parents liés
//   Et l'identifiant stable de l'enfant reste inchangé (enrichissement, pas recréation)
//   Et un enfant sans aucun parent lié reste valide (lien optionnel, 0 parent accepté)
//
// Périmètre S1 : chemin heureux du lien + modèle enrichi + relecture. Les règles/rejets
// (2 parents max, inexistant, non-parent, déjà lié) sont S2.
public class Scenario34_S1_LierEnfantParent
{
    private const string LeaId = "enfant-lea";
    private const string TomId = "enfant-tom";

    /// <summary>Amorce une config acteur avec un acteur AJOUTÉ en session — de type
    /// <see cref="TypeActeur.Parent"/> par défaut (Foyer.TypeParDefaut) et SANS aucun rôle — et retourne
    /// son identifiant stable. Précondition valide du lien (option A, s36 : parent liable = TypeActeur.Parent,
    /// le rôle du référentiel ne qualifie plus).</summary>
    private static (ConfigurationFoyerEnMemoire config, string parentId) FoyerAvecUnParent(string prenom)
    {
        var config = new ConfigurationFoyerEnMemoire();
        var parentId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand(prenom)).Valeur!.ActeurId;
        return (config, parentId);
    }

    // ---------- Acceptation (boucle externe, frontière Application) ----------
    [Fact]
    public void Acceptation_Should_Relire_l_enfant_avec_le_parent_lie_sur_le_meme_id_When_le_parent_est_lie()
    {
        var (config, papaId) = FoyerAvecUnParent("Papa");
        var referentiel = new FakeReferentielEnfants()
            .AvecEnfant(LeaId, "Léa")
            .AvecEnfant(TomId, "Tom");
        var handler = new LierEnfantParentHandler(referentiel, config, referentiel);

        var resultat = handler.Handle(new LierEnfantParentCommand(LeaId, papaId));

        // La commande réussit
        Assert.True(resultat.EstSucces);

        // L'enfant relu porte le parent lié, avec le MÊME identifiant stable (enrichissement)
        var lea = referentiel.EnumererEnfants().Single(e => e.Id == LeaId);
        Assert.Equal("Léa", lea.Prenom);
        Assert.Contains(papaId, lea.ParentsLies);

        // Un enfant sans aucun parent lié reste valide (0 parent accepté)
        var tom = referentiel.EnumererEnfants().Single(e => e.Id == TomId);
        Assert.Empty(tom.ParentsLies);
    }

    // ---------- Test #1 — Driver : lier fait apparaître le parent dans la liste relue ----------
    [Fact]
    public void Should_Porter_le_parent_dans_la_liste_relue_When_la_commande_lier_est_emise()
    {
        var (config, papaId) = FoyerAvecUnParent("Papa");
        var referentiel = new FakeReferentielEnfants().AvecEnfant(LeaId, "Léa");
        var handler = new LierEnfantParentHandler(referentiel, config, referentiel);

        handler.Handle(new LierEnfantParentCommand(LeaId, papaId));

        var lea = referentiel.EnumererEnfants().Single(e => e.Id == LeaId);
        Assert.Contains(papaId, lea.ParentsLies);
    }
}
