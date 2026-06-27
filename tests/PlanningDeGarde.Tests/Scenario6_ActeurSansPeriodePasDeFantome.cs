using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 09 — Sc.6 — Un acteur ajouté sans période ne crée pas d'entrée fantôme (@limite)
//   Given Carla vient d'être ajoutée et n'a aucune période de garde dans la fenêtre affichée
//   When la grille du planning est rendue
//   Then Carla est présente dans la liste de l'écran de configuration
//   And Carla n'apparaît dans aucune entrée de la légende
//   And Carla n'apparaît dans aucune case de la grille
//
// CARACTÉRISATION BACKEND (tdd-auto) — early green ATTENDU (annoncé par tdd-analyse), PAS un driver.
// Aucun code neuf : « pas de fantôme » découle de la COMPOSITION d'invariants déjà verts —
//   (1) ÉNUMÉRATION depuis le store (Sc.1 #3 : un acteur ajouté EXISTE dans le foyer sur son id neuf,
//       restitué par EnumererActeurs, indépendamment de toute période) ;
//   (2) LÉGENDE = présents dans la fenêtre (s07 Sc.3 : LegendeDesPresents ne retient que les ids
//       portés par une période intersectant l'intervalle) → un acteur SANS période n'y entre pas ;
//   (3) CASE nommée seulement si une période la couvre (s08 Sc.6 : hors-fenêtre, pas de fantôme).
// Ajouter un acteur ne crée AUCUNE période → ni légende ni case ne le mentionnent, MÉCANIQUEMENT.
// Filet anti-régression documentant le @limite « énumérer ≠ apparaître dans la grille ». L'acceptation
// runtime IHM (Carla présente dans la liste de l'écran config câblé mais absente de la grille rendue —
// front WASM + API distante + store durable) est menée séparément par ihm-builder.
public class Scenario6_ActeurSansPeriodePasDeFantome
{
    private const string Carla = "Carla";
    private const string Rose = "rose";

    // 8 juin 2026 est un lundi (miroir des dates s07/s09 Sc.4-5) → fenêtre [08/06, 12/07].
    private static readonly System.DateOnly Lundi_08_06_2026 = new(2026, 6, 8);

    [Fact]
    public void Should_Lister_Carla_dans_l_ecran_de_configuration_mais_l_exclure_de_toute_entree_de_legende_et_de_toute_case_When_l_acteur_ajoute_n_a_aucune_periode_dans_la_fenetre()
    {
        // --- Store réel (seedé depuis Foyer), réalisant énumération + édition + référentiel + palette ---
        var store = new ConfigurationFoyerEnMemoire();

        // --- Sc.1 : un parent ajoute Carla en rose → identifiant stable NEUF opaque, AUCUNE période ---
        var ajout = new AjouterActeurHandler(store).Handle(new AjouterActeurCommand(Carla, Rose));
        Assert.True(ajout.EstSucces);
        var idNeufCarla = ajout.Valeur!.ActeurId;

        // --- Aucune période enregistrée : Carla n'a aucune garde dans la fenêtre affichée ---
        var periodes = new FakePeriodeRepository();

        // --- Read model s07/s08 inchangé : la grille est rendue à partir des périodes (vides) ---
        var grille = new GrilleAgendaQuery(new FakeSlotRepository(), periodes, store, store)
            .Projeter(Lundi_08_06_2026);

        // Then : Carla EST présente dans la liste de l'écran de configuration (énumération depuis le store)
        Assert.Contains(idNeufCarla, store.EnumererActeurs());

        // And : Carla n'apparaît dans AUCUNE entrée de la légende — ni sur son id neuf, ni sur son libellé
        Assert.Empty(grille.Légende); // aucune période → légende vide (présents dans la fenêtre uniquement)
        Assert.DoesNotContain(grille.Légende, e => e.IdentifiantStable == idNeufCarla);
        Assert.DoesNotContain(grille.Légende, e => e.Nom == Carla);

        // And : Carla n'apparaît dans AUCUNE case de la grille (aucune case n'est nommée Carla)
        Assert.DoesNotContain(grille.Jours, j => j.NomResponsable == Carla);
        Assert.All(grille.Jours, j => Assert.Equal("", j.NomResponsable)); // aucune case nommée (pas de période)
    }
}
