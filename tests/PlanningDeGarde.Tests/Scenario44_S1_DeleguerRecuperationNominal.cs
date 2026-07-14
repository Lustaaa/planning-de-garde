using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 44 — Sc.1 — Déléguer un jour COMPOSE l'écriture surcharge ponctuelle (nominal) (@back)
//   Étant donné un foyer configuré (acteurs, cycle de fond, enfant) et un jour J résolu PAR LE FOND (acteur A)
//   Et un autre acteur B éligible, présent dans le store, distinct de A
//   Quand je délègue la récupération du jour J à l'acteur B (DeleguerRecuperation(J, enfant, B))
//   Alors une SURCHARGE d'UN jour (J→J) est écrite via le chemin d'écriture EXISTANT (s06), responsable B
//   Et la case du jour J fait PRIMER B (surcharge > fond), A restant le fond des autres jours
//   Et un TRANSFERT A → B est AUTO-DÉRIVÉ pour J (s31, R24), LU jamais réécrit
//
// Frontière Application : le use case COMPOSE la résolution (grille) + le chemin d'écriture période (s06).
// Aucune commande de transfert neuve, aucun store neuf, aucun modèle de résolution recopié.
public class Scenario44_S1_DeleguerRecuperationNominal
{
    private const string ParentA = "parent-a";
    private const string ParentB = "parent-b";
    private const string LeaId = "enfant-lea";

    // ISO 28 (semaine du lundi 06/07/2026) → index 0 (pair) → Parent A par le fond ; aucune surcharge ces jours-là.
    private static readonly DateOnly Mercredi_08_07_2026 = new(2026, 7, 8);
    private static readonly DateOnly Mardi_07_07_2026 = new(2026, 7, 7);   // veille, même semaine ISO → fond Parent A aussi

    private static Dictionary<int, string> MappingPairAImpairB() => new() { [0] = ParentA, [1] = ParentB };

    private static GrilleAgendaQuery Grille(IPeriodeRepository periodes)
        => new(
            new FakeSlotRepository(), periodes,
            new FakePaletteCouleurs(new Dictionary<string, string> { [ParentA] = "bleu", [ParentB] = "orange" }),
            new FakeReferentielResponsables(new Dictionary<string, string> { [ParentA] = "Alice", [ParentB] = "Bruno" }),
            new FakeReferentielCycleDeFond(new CycleDeFond(2, MappingPairAImpairB())),
            new FakeEnumerationActeursFoyer(ParentA, ParentB));

    // ---------- Test d'acceptation (boucle externe, frontière Application) ----------
    [Fact]
    public void Acceptation_Should_Ecrire_une_surcharge_dun_jour_et_faire_primer_le_delegataire_avec_transfert_derive()
    {
        var periodes = new FakePeriodeRepository();
        var grille = Grille(periodes);

        // Précondition : le jour J est RÉSOLU PAR LE FOND (Parent A), aucune surcharge.
        var avant = new CarteDuJourQuery(grille).Lire(Mercredi_08_07_2026, LeaId).Responsable;
        Assert.Equal(ParentA, avant.ActeurId);
        Assert.Empty(periodes.AllSnapshots());

        // When — je délègue la récupération du jour J à Parent B.
        var resultat = new DeleguerRecuperationHandler(grille, periodes, new FakeEnumerationActeursFoyer(ParentA, ParentB))
            .Handle(new DeleguerRecuperationCommand(Mercredi_08_07_2026, LeaId, ParentB));

        Assert.True(resultat.EstSucces);

        // Then — UNE surcharge d'UN seul jour (J→J), responsable B, écrite via le chemin période existant.
        var periode = Assert.Single(periodes.AllSnapshots());
        Assert.Equal(ParentB, periode.ResponsableId);
        Assert.Equal(Mercredi_08_07_2026, DateOnly.FromDateTime(periode.Debut));
        Assert.Equal(Mercredi_08_07_2026, DateOnly.FromDateTime(periode.Fin));

        // Then — la case du jour J fait désormais PRIMER B (surcharge > fond).
        var apres = new CarteDuJourQuery(grille).Lire(Mercredi_08_07_2026, LeaId);
        Assert.Equal(ParentB, apres.Responsable.ActeurId);
        Assert.Equal("Bruno", apres.Responsable.Nom);

        // Then — A reste le fond des autres jours (la veille, même semaine ISO).
        Assert.Equal(ParentA, new CarteDuJourQuery(grille).Lire(Mardi_07_07_2026, LeaId).Responsable.ActeurId);

        // Then — un transfert A → B est AUTO-DÉRIVÉ pour J (bascule fond→surcharge), LU jamais réécrit.
        Assert.NotNull(apres.Transfert);
        Assert.Equal("Alice", apres.Transfert!.CedantNom);
        Assert.Equal("Bruno", apres.Transfert.RecevantNom);
    }

    // ---------- Acceptation runtime — adaptateur InMemory RÉEL (1er des deux adaptateurs) ----------
    [Fact]
    public void Acceptation_InMemory_Should_Deleguer_via_les_adaptateurs_reels()
    {
        var config = new ConfigurationFoyerEnMemoire();
        var aliceId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice")).Valeur!.ActeurId;
        var brunoId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Bruno")).Valeur!.ActeurId;

        var cycle = new CycleDeFondEnMemoire();
        cycle.DefinirCycle(new CycleDeFond(2, new Dictionary<int, string> { [0] = aliceId, [1] = brunoId }));

        var periodes = new InMemoryPeriodeRepository();
        var grille = new GrilleAgendaQuery(
            new InMemorySlotRepository(), periodes, config, config,
            cycle, config, new InMemorySlotRecurrentRepository(), new InMemoryTransfertRepository());

        // Précondition : le jour J est résolu par le fond (Alice).
        Assert.Equal(aliceId, new CarteDuJourQuery(grille).Lire(Mercredi_08_07_2026, LeaId).Responsable.ActeurId);

        var resultat = new DeleguerRecuperationHandler(grille, periodes, config)
            .Handle(new DeleguerRecuperationCommand(Mercredi_08_07_2026, LeaId, brunoId));
        Assert.True(resultat.EstSucces);

        // Relecture store : surcharge d'un jour, responsable Bruno.
        var periode = Assert.Single(periodes.AllSnapshots());
        Assert.Equal(brunoId, periode.ResponsableId);

        var carte = new CarteDuJourQuery(grille).Lire(Mercredi_08_07_2026, LeaId);
        Assert.Equal(brunoId, carte.Responsable.ActeurId);
        Assert.NotNull(carte.Transfert);
        Assert.Equal("Alice", carte.Transfert!.CedantNom);
        Assert.Equal("Bruno", carte.Transfert.RecevantNom);
    }
}
