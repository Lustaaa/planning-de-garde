using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 45 — Sc.1 — Déléguer une PLAGE COMPOSE l'écriture surcharge multi-jours (nominal) (@back)
//   Étant donné une plage [J1..J2] (≥ 3 jours) TOUS résolus par le CYCLE DE FOND (acteur A), aucune surcharge
//   Et un autre acteur B éligible, présent dans le store, distinct de A
//   Quand je délègue la récupération de la plage [J1..J2] à B (DeleguerRecuperation(J1, J2, enfant, B))
//   Alors UNE SEULE surcharge couvrant [J1..J2] est écrite via le chemin d'écriture période EXISTANT (s06), responsable B
//   Et chaque jour de la plage fait PRIMER B (surcharge > fond), A restant le fond hors plage
//   Et un TRANSFERT A → B est AUTO-DÉRIVÉ à l'ENTRÉE (J1) et un TRANSFERT B → A à la SORTIE (J2+1) — s31, R24, LUS jamais réécrits
//   Et la plage réduite à UN jour (fin = début) est STRICTEMENT identique à la délégation d'un jour s44 (parité)
//
// Frontière Application : le use case COMPOSE la résolution (grille) + le chemin d'écriture période MULTI-JOURS (s06).
// AUCUN modèle/commande/store neuf ; les bicolores sortent de s31 par construction aux deux frontières.
public class Scenario45_S1_DeleguerPlageNominal
{
    private const string ParentA = "parent-a";
    private const string ParentB = "parent-b";
    private const string LeaId = "enfant-lea";

    // ISO 28 (semaine du lundi 06/07/2026) → index 0 (pair) → Parent A par le fond ; aucune surcharge ces jours-là.
    private static readonly DateOnly Lundi_06 = new(2026, 7, 6);      // J1-1 (fond A)
    private static readonly DateOnly Mardi_07 = new(2026, 7, 7);      // J1 (entrée de plage)
    private static readonly DateOnly Mercredi_08 = new(2026, 7, 8);
    private static readonly DateOnly Jeudi_09 = new(2026, 7, 9);      // J2 (fin de plage)
    private static readonly DateOnly Vendredi_10 = new(2026, 7, 10);  // J2+1 (sortie, fond A de nouveau)

    private static Dictionary<int, string> MappingPairAImpairB() => new() { [0] = ParentA, [1] = ParentB };

    private static GrilleAgendaQuery Grille(IPeriodeRepository periodes)
        => new(
            new FakeSlotRepository(), periodes,
            new FakePaletteCouleurs(new Dictionary<string, string> { [ParentA] = "bleu", [ParentB] = "orange" }),
            new FakeReferentielResponsables(new Dictionary<string, string> { [ParentA] = "Alice", [ParentB] = "Bruno" }),
            new FakeReferentielCycleDeFond(new CycleDeFond(2, MappingPairAImpairB())),
            new FakeEnumerationActeursFoyer(ParentA, ParentB));

    private static DeleguerRecuperationHandler Handler(IPeriodeRepository periodes)
        => new(Grille(periodes), periodes, new FakeEnumerationActeursFoyer(ParentA, ParentB));

    private static JourCase CaseDuJour(GrilleAgendaQuery grille, DateOnly jour)
        => grille.Projeter(jour, VuePlanning.Semaine).Jours.Single(j => j.Date == jour);

    // ---------- Test d'acceptation (boucle externe, frontière Application) ----------
    [Fact]
    public void Acceptation_Should_Ecrire_une_seule_surcharge_multi_jours_et_primer_le_delegataire_avec_transferts_aux_deux_frontieres()
    {
        var periodes = new FakePeriodeRepository();
        var grille = Grille(periodes);

        // Précondition : chaque jour de la plage est RÉSOLU PAR LE FOND (Parent A), aucune surcharge.
        foreach (var j in new[] { Mardi_07, Mercredi_08, Jeudi_09 })
            Assert.Equal(ParentA, CaseDuJour(grille, j).ResponsableId);
        Assert.Empty(periodes.AllSnapshots());

        // When — je délègue la récupération de la PLAGE [J1..J2] à Parent B.
        var resultat = Handler(periodes)
            .Handle(new DeleguerRecuperationCommand(Mardi_07, LeaId, ParentB, Jeudi_09));

        Assert.True(resultat.EstSucces);

        // Then — UNE SEULE surcharge couvrant [J1..J2], responsable B, écrite via le chemin période existant.
        var periode = Assert.Single(periodes.AllSnapshots());
        Assert.Equal(ParentB, periode.ResponsableId);
        Assert.Equal(Mardi_07, DateOnly.FromDateTime(periode.Debut));
        Assert.Equal(Jeudi_09, DateOnly.FromDateTime(periode.Fin));

        // Then — chaque jour de la plage fait désormais PRIMER B (surcharge > fond).
        foreach (var j in new[] { Mardi_07, Mercredi_08, Jeudi_09 })
            Assert.Equal(ParentB, CaseDuJour(grille, j).ResponsableId);

        // Then — A reste le fond hors plage (la veille J1-1 et le lendemain J2+1).
        Assert.Equal(ParentA, CaseDuJour(grille, Lundi_06).ResponsableId);
        Assert.Equal(ParentA, CaseDuJour(grille, Vendredi_10).ResponsableId);

        // Then — un transfert A → B est AUTO-DÉRIVÉ à l'ENTRÉE (J1), un transfert B → A à la SORTIE (J2+1).
        var entree = CaseDuJour(grille, Mardi_07);
        Assert.NotNull(entree.Transfert);
        Assert.Equal("Alice", entree.Transfert!.NomDepart);
        Assert.Equal("Bruno", entree.Transfert.NomArrivee);

        var sortie = CaseDuJour(grille, Vendredi_10);
        Assert.NotNull(sortie.Transfert);
        Assert.Equal("Bruno", sortie.Transfert!.NomDepart);
        Assert.Equal("Alice", sortie.Transfert.NomArrivee);
    }

    // ---------- Parité s44 : plage réduite à UN jour (fin = début) ----------
    [Fact]
    public void Should_Etre_identique_a_s44_When_plage_reduite_a_un_jour()
    {
        var periodes = new FakePeriodeRepository();

        var resultat = Handler(periodes)
            .Handle(new DeleguerRecuperationCommand(Mercredi_08, LeaId, ParentB, Mercredi_08));

        Assert.True(resultat.EstSucces);
        var periode = Assert.Single(periodes.AllSnapshots());
        Assert.Equal(ParentB, periode.ResponsableId);
        Assert.Equal(Mercredi_08, DateOnly.FromDateTime(periode.Debut));
        Assert.Equal(Mercredi_08, DateOnly.FromDateTime(periode.Fin));
    }

    // ---------- Acceptation runtime — adaptateur InMemory RÉEL ----------
    [Fact]
    public void Acceptation_InMemory_Should_Deleguer_une_plage_via_les_adaptateurs_reels()
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

        // Précondition : chaque jour de la plage est résolu par le fond (Alice).
        foreach (var j in new[] { Mardi_07, Mercredi_08, Jeudi_09 })
            Assert.Equal(aliceId, CaseDuJour(grille, j).ResponsableId);

        var resultat = new DeleguerRecuperationHandler(grille, periodes, config)
            .Handle(new DeleguerRecuperationCommand(Mardi_07, LeaId, brunoId, Jeudi_09));
        Assert.True(resultat.EstSucces);

        // Relecture store : UNE SEULE surcharge [J1..J2], responsable Bruno.
        var periode = Assert.Single(periodes.AllSnapshots());
        Assert.Equal(brunoId, periode.ResponsableId);
        Assert.Equal(Mardi_07, DateOnly.FromDateTime(periode.Debut));
        Assert.Equal(Jeudi_09, DateOnly.FromDateTime(periode.Fin));

        // Chaque jour prime Bruno ; transferts dérivés aux deux frontières.
        foreach (var j in new[] { Mardi_07, Mercredi_08, Jeudi_09 })
            Assert.Equal(brunoId, CaseDuJour(grille, j).ResponsableId);
        Assert.Equal("Bruno", CaseDuJour(grille, Mardi_07).Transfert!.NomArrivee);
        Assert.Equal("Bruno", CaseDuJour(grille, Vendredi_10).Transfert!.NomDepart);
    }
}
