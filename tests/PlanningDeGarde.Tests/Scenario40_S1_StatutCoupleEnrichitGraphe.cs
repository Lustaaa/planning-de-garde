using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 40 — Sc.1 — Statut de complétude PUR par enfant, enrichit GrapheFoyerQuery s38 (@back)
//   Étant donné un foyer avec des enfants déclarés (s30) et des liens enfant↔parent posés (s34),
//   portant des rôles-du-lien père / mère / parent-libre (s37)
//   Quand la projection de lecture du graphe foyer (GrapheFoyerQuery s38) est exécutée
//   Alors elle restitue, PAR enfant, en plus de ses parents liés, un STATUT de complétude du couple
//   Et ce statut est composé UNIQUEMENT des données déjà persistées (liens s34 + rôle-du-lien s37) —
//     LECTURE PURE : aucune mutation, aucun store neuf, aucune persistance neuve (borne anti-cliquet)
//   Et il est réalisé sur les DEUX adaptateurs (InMemory seedé ET Mongo durable), même contrat
//   Et aucune query parallèle n'est créée : la projection existante s38 est ENRICHIE d'un champ calculé
//
// Frontière Application (query agrégée enrichie). L'acceptation Mongo (2e adaptateur) est portée par
// Scenario40_S1_StatutCoupleMongoTests (Api.Tests). Ici : le CHAMP statut existe et est composé sur la
// projection s38 (père + mère → COMPLET), sur la doublure ET sur l'adaptateur InMemory RÉEL.
public class Scenario40_S1_StatutCoupleEnrichitGraphe
{
    private const string LeaId = "enfant-lea";
    private const string AliceId = "acteur-alice";
    private const string BobId = "acteur-bob";

    [Fact]
    public void Acceptation_Should_Enrichir_chaque_enfant_d_un_statut_de_completude_du_couple()
    {
        // Given — Léa liée à Alice (mère) ET Bob (père) : le couple père+mère est complet (R3)
        var enfants = new FakeReferentielEnfants().AvecEnfant(LeaId, "Léa");
        enfants.LierParent(LeaId, AliceId, RoleDuLien.Mere);
        enfants.LierParent(LeaId, BobId, RoleDuLien.Pere);

        var noms = new FakeReferentielResponsables(new Dictionary<string, string>
        {
            [AliceId] = "Alice",
            [BobId] = "Bob",
        });
        var acteurs = new FakeEnumerationActeursFoyer(AliceId, BobId);

        // When
        var graphe = new GrapheFoyerQuery(enfants, noms, acteurs).Lire();

        // Then — chaque enfant porte un statut de complétude ; Léa (père + mère) est COMPLET
        var lea = graphe.Single(e => e.EnfantId == LeaId);
        Assert.Equal(StatutCoupleR3.Complet, lea.StatutCouple);
    }

    [Fact]
    public void Acceptation_InMemory_Should_Enrichir_le_statut_sur_l_adaptateur_reel()
    {
        // Given — store enfants InMemory réel + config foyer InMemory réelle (noms + existence résolus)
        var config = new ConfigurationFoyerEnMemoire();
        var aliceId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice")).Valeur!.ActeurId;
        var bobId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Bob")).Valeur!.ActeurId;
        var storeEnfants = new ReferentielEnfantsEnMemoire();
        storeEnfants.Ajouter(LeaId, "Léa");
        storeEnfants.LierParent(LeaId, aliceId, RoleDuLien.Mere);
        storeEnfants.LierParent(LeaId, bobId, RoleDuLien.Pere);

        // When — la query câblée sur les adaptateurs RÉELS (jamais une doublure)
        var graphe = new GrapheFoyerQuery(storeEnfants, config, config).Lire();

        // Then — Léa (père + mère) est COMPLET, statut composé depuis le store réel
        var lea = graphe.Single(e => e.EnfantId == LeaId);
        Assert.Equal(StatutCoupleR3.Complet, lea.StatutCouple);
    }
}
