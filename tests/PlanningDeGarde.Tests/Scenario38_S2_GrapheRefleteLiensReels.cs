using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 38 — Sc.2 — Le graphe reflète les liens RÉELS du store (@back)
//   Étant donné un enfant lié à un seul parent-acteur et un autre enfant sans aucun parent lié
//   Quand la query du graphe foyer est exécutée
//   Alors le premier enfant expose EXACTEMENT son parent lié (aucun acteur non lié en branche)
//   Et le second enfant est une RACINE ISOLÉE (0 parent, cas accepté s34), sans nœud fantôme
//   Étant donné un acteur supprimé (orphelin) encore référencé par un lien résiduel
//   Alors aucune branche fantôme n'est produite (repli sans nom fantôme, miroir R5/R6 et filtre Resolvable s13)
//   Étant donné un store de foyer VIDE (aucun enfant — Mongo 1er lancement, asymétrie seed s15)
//   Alors la query restitue un graphe VIDE (aucune racine), sans erreur
//
// Discriminant fort (comme s31 S9) : le référentiel de NOMS porte ENCORE une entrée stale pour l'acteur
// supprimé ; c'est le contrat d'existence (IEnumerationActeursFoyer / Resolvable) — appliqué aux branches
// — qui neutralise, pas l'absence de nom. Le lien résiduel existe (non délié) ; seule la branche orpheline
// est retirée, l'enfant reste une racine (isolée s'il ne lui reste aucun parent résoluble).
public class Scenario38_S2_GrapheRefleteLiensReels
{
    private const string LeaId = "enfant-lea";
    private const string TomId = "enfant-tom";
    private const string AliceId = "acteur-alice";
    private const string BobId = "acteur-bob";

    private static FakeReferentielResponsables Noms()
        => new(new Dictionary<string, string> { [AliceId] = "Alice", [BobId] = "Bob" });

    [Fact]
    public void Should_Exposer_exactement_le_parent_lie_et_une_racine_isolee_When_un_enfant_est_lie_et_un_autre_non()
    {
        // Given — Léa liée à Alice uniquement ; Tom sans aucun parent lié. Bob EXISTE mais n'est pas lié.
        var enfants = new FakeReferentielEnfants()
            .AvecEnfant(LeaId, "Léa")
            .AvecEnfant(TomId, "Tom");
        enfants.LierParent(LeaId, AliceId, RoleDuLien.Mere);
        var acteurs = new FakeEnumerationActeursFoyer(AliceId, BobId); // Alice et Bob existent tous deux

        var graphe = new GrapheFoyerQuery(enfants, Noms(), acteurs).Lire();

        // Léa : exactement Alice en branche (Bob, non lié, n'apparaît PAS)
        var lea = graphe.Single(e => e.EnfantId == LeaId);
        var parent = Assert.Single(lea.Parents);
        Assert.Equal(AliceId, parent.ActeurId);
        Assert.DoesNotContain(lea.Parents, p => p.ActeurId == BobId);

        // Tom : racine isolée (0 parent, cas accepté s34), sans nœud fantôme
        var tom = graphe.Single(e => e.EnfantId == TomId);
        Assert.Empty(tom.Parents);
    }

    [Fact]
    public void Should_Ne_produire_aucune_branche_fantome_When_un_acteur_orphelin_reste_reference_par_un_lien_residuel()
    {
        // Given — Léa liée à Alice (existante) ET Bob, mais Bob a été SUPPRIMÉ du référentiel (orphelin) ;
        // le lien résiduel subsiste (non délié) et le nom stale de Bob est ENCORE résoluble.
        var enfants = new FakeReferentielEnfants().AvecEnfant(LeaId, "Léa");
        enfants.LierParent(LeaId, AliceId, RoleDuLien.Mere);
        enfants.LierParent(LeaId, BobId, RoleDuLien.Pere);
        var acteurs = new FakeEnumerationActeursFoyer(AliceId); // Bob supprimé : absent du contrat d'existence

        var graphe = new GrapheFoyerQuery(enfants, Noms(), acteurs).Lire();

        // Léa : Alice reste en branche, Bob orphelin est neutralisé (aucune branche fantôme, aucun nom stale)
        var lea = graphe.Single(e => e.EnfantId == LeaId);
        var parent = Assert.Single(lea.Parents);
        Assert.Equal(AliceId, parent.ActeurId);
        Assert.DoesNotContain(lea.Parents, p => p.ActeurId == BobId);
        Assert.DoesNotContain(lea.Parents, p => p.Nom == "Bob");
    }

    [Fact]
    public void Should_Restituer_un_graphe_vide_sans_erreur_When_le_store_de_foyer_est_vide()
    {
        // Given — aucun enfant (Mongo 1er lancement, asymétrie seed s15)
        var enfants = new FakeReferentielEnfants();
        var acteurs = new FakeEnumerationActeursFoyer();

        var graphe = new GrapheFoyerQuery(enfants, Noms(), acteurs).Lire();

        // Then — graphe VIDE (aucune racine), sans erreur
        Assert.Empty(graphe);
    }
}
