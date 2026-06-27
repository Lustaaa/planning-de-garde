using System;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 09 — Sc.7 — Deux acteurs de même libellé reçoivent deux identifiants distincts (@limite)
//   Given une nounou « Carla » a déjà été ajoutée au foyer
//   When un parent ajoute une seconde acteur également nommée « Carla »
//   Then le foyer compte deux acteurs « Carla » portés par deux identifiants distincts
//   And la légende les dédoublonne par identifiant en deux entrées
//   And les deux « Carla » ne sont jamais fusionnées sur leur libellé
//
// CARACTÉRISATION BACKEND (tdd-auto) — early green ATTENDU (annoncé par tdd-analyse), PAS un driver.
// Aucun code neuf : « deux mêmes libellés → deux ids distincts » découle MÉCANIQUEMENT d'invariants
// déjà verts —
//   (1) ID OPAQUE GÉNÉRÉ (Sc.1 #2 : AjouterActeurHandler attribue un identifiant opaque, jamais dérivé
//       du libellé et unique) → deux ajouts du même nom donnent fatalement deux ids différents ;
//   (2) ÉNUMÉRATION par id (Sc.1 #3 : EnumererActeurs restitue chaque acteur sur SON id) → deux entrées
//       distinctes même nom identique ;
//   (3) LÉGENDE DÉDOUBLONNÉE PAR ID (s07, .Distinct() sur ResponsableId) → deux ids portés chacun par
//       une période donnent DEUX entrées de légende même libellé, jamais fusionnées sur « Carla ».
// Filet anti-régression documentant le @limite collision-de-libellé (libellé ≠ identité, invariant s06).
// L'acceptation runtime IHM (deux Carla dans l'écran config câblé + deux entrées de légende, front WASM
// + API distante + store durable) est menée séparément par ihm-builder.
public class Scenario7_DeuxLibellesIdentiquesIdsDistincts
{
    private const string Carla = "Carla";
    private const string Rose = "rose";

    // 8 juin 2026 est un lundi (miroir des dates s07/s09 Sc.4-6) → fenêtre [08/06, 12/07].
    private static readonly DateOnly Lundi_08_06_2026 = new(2026, 6, 8);

    [Fact]
    public void Should_Attribuer_deux_identifiants_distincts_aux_deux_acteurs_de_meme_libelle_sans_jamais_les_fusionner_sur_le_libelle_When_une_seconde_actrice_du_meme_nom_est_ajoutee()
    {
        // --- Store réel (seedé depuis Foyer), réalisant énumération + édition + référentiel + palette ---
        var store = new ConfigurationFoyerEnMemoire();
        var handler = new AjouterActeurHandler(store);

        // --- Given/When : « Carla » a déjà été ajoutée, un parent ajoute une SECONDE « Carla » ---
        var premiere = handler.Handle(new AjouterActeurCommand(Carla, Rose));
        var seconde = handler.Handle(new AjouterActeurCommand(Carla, Rose));
        Assert.True(premiere.EstSucces);
        Assert.True(seconde.EstSucces);
        var idCarla1 = premiere.Valeur!.ActeurId;
        var idCarla2 = seconde.Valeur!.ActeurId;

        // Then : deux identifiants DISTINCTS — l'id opaque sépare deux libellés identiques (Sc.1 #2)
        Assert.NotEqual(idCarla1, idCarla2);

        // And : le foyer compte DEUX acteurs « Carla » — l'énumération les sépare par id (jamais fusion)
        var carlas = store.EnumererActeurs().Where(id => store.NomDe(id) == Carla).ToList();
        Assert.Equal(2, carlas.Count);
        Assert.Contains(idCarla1, carlas);
        Assert.Contains(idCarla2, carlas);

        // --- Chaque « Carla » porte une période dans la fenêtre affichée (sur SON id neuf) ---
        var periodes = new FakePeriodeRepository();
        var responsables = new FakeResponsableRepository().AvecResponsable(idCarla1).AvecResponsable(idCarla2);
        var affecter = new AffecterPeriodeHandler(periodes, responsables);
        Assert.True(affecter.Handle(new AffecterPeriodeCommand(idCarla1, new DateTime(2026, 6, 8), new DateTime(2026, 6, 10))).EstSucces);
        Assert.True(affecter.Handle(new AffecterPeriodeCommand(idCarla2, new DateTime(2026, 6, 11), new DateTime(2026, 6, 12))).EstSucces);

        // --- Read model s07 inchangé : la légende dédoublonne PAR ID (jamais le libellé) ---
        var grille = new GrilleAgendaQuery(new FakeSlotRepository(), periodes, store, store).Projeter(Lundi_08_06_2026);

        // And : DEUX entrées de légende, une par identifiant — jamais fusionnées sur le libellé « Carla »
        Assert.Equal(2, grille.Légende.Count);
        Assert.All(grille.Légende, e => Assert.Equal(Carla, e.Nom));            // deux entrées au libellé identique
        Assert.Contains(grille.Légende, e => e.IdentifiantStable == idCarla1);  // ... portée par le 1er id
        Assert.Contains(grille.Légende, e => e.IdentifiantStable == idCarla2);  // ... et le 2nd id (dédoublonnage par id)
    }
}
