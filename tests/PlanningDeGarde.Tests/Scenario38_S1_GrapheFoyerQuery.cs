using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 38 — Sc.1 — Query de lecture agrégée : enfants → parents liés (avec rôle-du-lien) (@back)
//   Étant donné un foyer avec des enfants déclarés (s30) et des liens enfant↔parent posés (s34),
//   certains portant un rôle-du-lien père / mère / parent-libre (s37)
//   Quand la query de lecture du graphe foyer (GrapheFoyerQuery) est exécutée
//   Alors elle restitue, PAR enfant, la liste de ses parents liés avec, pour chacun, son NOM et son rôle-du-lien
//   Et c'est une LECTURE PURE : aucune mutation, aucun store neuf (elle compose des ports de lecture)
//   Et un lien s34 sans rôle-du-lien explicite est restitué à « parent-libre » (défaut neutre s37)
//
// Frontière Application (query agrégée). L'acceptation runtime sur les DEUX adaptateurs (InMemory seedé
// réel + Mongo durable) est portée par Scenario38_S1_GrapheFoyerInMemory (ci-dessous) et le test Mongo
// Api.Tests (GrapheFoyerMongoTests). Ici : contrat de composition (nom résolu + rôle-du-lien par enfant).
public class Scenario38_S1_GrapheFoyerQuery
{
    private const string LeaId = "enfant-lea";
    private const string TomId = "enfant-tom";
    private const string AliceId = "acteur-alice";
    private const string BobId = "acteur-bob";

    // ---------- Acceptation (boucle externe, frontière Application) ----------
    [Fact]
    public void Acceptation_Should_Restituer_par_enfant_ses_parents_lies_avec_nom_et_role_du_lien()
    {
        // Given — Léa liée à Alice (mère) et Bob (père) ; Tom lié à Alice sans rôle explicite (s34 nu → parent-libre)
        var enfants = new FakeReferentielEnfants()
            .AvecEnfant(LeaId, "Léa")
            .AvecEnfant(TomId, "Tom");
        enfants.LierParent(LeaId, AliceId, RoleDuLien.Mere);
        enfants.LierParent(LeaId, BobId, RoleDuLien.Pere);
        enfants.LierParent(TomId, AliceId); // lien nu → rôle-du-lien par défaut = parent-libre (s37)

        var noms = new FakeReferentielResponsables(new Dictionary<string, string>
        {
            [AliceId] = "Alice",
            [BobId] = "Bob",
        });

        // When
        var graphe = new GrapheFoyerQuery(enfants, noms).Lire();

        // Then — chaque enfant en racine, avec ses parents liés (nom résolu + rôle-du-lien)
        var lea = graphe.Single(e => e.EnfantId == LeaId);
        Assert.Equal("Léa", lea.Prenom);
        Assert.Contains(lea.Parents, p => p.ActeurId == AliceId && p.Nom == "Alice" && p.Role == RoleDuLien.Mere);
        Assert.Contains(lea.Parents, p => p.ActeurId == BobId && p.Nom == "Bob" && p.Role == RoleDuLien.Pere);

        // Tom : lien s34 nu restitué à « parent-libre » (défaut neutre s37), nom résolu
        var tom = graphe.Single(e => e.EnfantId == TomId);
        var parentDeTom = Assert.Single(tom.Parents);
        Assert.Equal(AliceId, parentDeTom.ActeurId);
        Assert.Equal("Alice", parentDeTom.Nom);
        Assert.Equal(RoleDuLien.ParentLibre, parentDeTom.Role);
    }

    // ---------- Acceptation runtime — adaptateur InMemory RÉEL (1er des deux adaptateurs, Sc.1) ----------
    [Fact]
    public void Acceptation_InMemory_Should_Restituer_le_graphe_sur_les_adaptateurs_reels()
    {
        // Given — store enfants InMemory réel + config foyer InMemory réelle (noms résolus depuis le store)
        var config = new ConfigurationFoyerEnMemoire();
        var aliceId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice")).Valeur!.ActeurId;
        var storeEnfants = new ReferentielEnfantsEnMemoire();
        storeEnfants.Ajouter(LeaId, "Léa");
        storeEnfants.LierParent(LeaId, aliceId, RoleDuLien.Mere);

        // When — la query câblée sur les adaptateurs RÉELS (jamais une doublure)
        var graphe = new GrapheFoyerQuery(storeEnfants, config).Lire();

        // Then — Léa en racine, Alice en branche (nom résolu depuis le store réel + rôle mère)
        var lea = graphe.Single(e => e.EnfantId == LeaId);
        var parent = Assert.Single(lea.Parents);
        Assert.Equal(aliceId, parent.ActeurId);
        Assert.Equal("Alice", parent.Nom);
        Assert.Equal(RoleDuLien.Mere, parent.Role);
    }
}
