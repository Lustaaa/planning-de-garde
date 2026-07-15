using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 46 — Sc.3 — Reprendre UNE occurrence (jour milieu) d'une plage s45 sans casser le reste (@back)
//   Étant donné une délégation de PLAGE (s45) confiant à "Bruno" la récupération de J1 à J3 inclus
//   Et donc "Bruno" résolu J1, J2, J3, avec transferts dérivés aux frontières
//   Quand j'exécute AnnulerDelegation(jour J2)   # le jour cliqué au MILIEU de la plage
//   Alors SEUL J2 retombe sur le FOND (granularité = une occurrence, PAS toute la plage)
//   Et J1 et J3 RESTENT délégués à "Bruno"
//   Et les transferts dérivés s31 sont RECALCULÉS : le trou en J2 produit ses propres bascules
//   Et aucune écriture partielle n'a touché J1 ni J3
//
// Frontière Application : la reprise COMPOSE la suppression s16 de la surcharge couvrant J2 puis RÉÉCRIT
// les segments restants [J1..J1] et [J3..J3] via le chemin d'écriture période EXISTANT (s06). Le trou re-dérive
// ses transferts par s31, aucune dérivation neuve.
public class Scenario46_S3_AnnulerUneOccurrenceDunePlage
{
    private const string ParentA = "parent-a";
    private const string ParentB = "parent-b";
    private const string LeaId = "enfant-lea";

    // Plage s45 (semaine ISO 28, fond Parent A) : [J1=mardi 07 .. J3=jeudi 09], J2 milieu = mercredi 08.
    private static readonly DateOnly Mardi_07 = new(2026, 7, 7);       // J1
    private static readonly DateOnly Mercredi_08 = new(2026, 7, 8);    // J2 (milieu, repris)
    private static readonly DateOnly Jeudi_09 = new(2026, 7, 9);       // J3
    private static readonly DateOnly Vendredi_10 = new(2026, 7, 10);   // J3+1

    private static Dictionary<int, string> MappingPairAImpairB() => new() { [0] = ParentA, [1] = ParentB };

    private static GrilleAgendaQuery Grille(IPeriodeRepository periodes)
        => new(
            new FakeSlotRepository(), periodes,
            new FakePaletteCouleurs(new Dictionary<string, string> { [ParentA] = "bleu", [ParentB] = "orange" }),
            new FakeReferentielResponsables(new Dictionary<string, string> { [ParentA] = "Alice", [ParentB] = "Bruno" }),
            new FakeReferentielCycleDeFond(new CycleDeFond(2, MappingPairAImpairB())),
            new FakeEnumerationActeursFoyer(ParentA, ParentB));

    private static JourCase CaseDuJour(GrilleAgendaQuery grille, DateOnly jour)
        => grille.Projeter(jour, VuePlanning.Semaine).Jours.Single(j => j.Date == jour);

    private static bool Couvre(PeriodeSnapshot p, DateOnly j)
        => DateOnly.FromDateTime(p.Debut) <= j && DateOnly.FromDateTime(p.Fin) >= j;

    [Fact]
    public void Acceptation_Should_Reprendre_seulement_le_jour_milieu_et_preserver_le_reste_de_la_plage()
    {
        var periodes = new FakePeriodeRepository();
        var grille = Grille(periodes);

        // Given — délégation de PLAGE [J1..J3] à Bruno (s45).
        new DeleguerRecuperationHandler(grille, periodes, new FakeEnumerationActeursFoyer(ParentA, ParentB))
            .Handle(new DeleguerRecuperationCommand(Mardi_07, LeaId, ParentB, Jeudi_09));
        foreach (var j in new[] { Mardi_07, Mercredi_08, Jeudi_09 })
            Assert.Equal(ParentB, CaseDuJour(grille, j).ResponsableId);
        Assert.Single(periodes.AllSnapshots());

        // When — je reprends le SEUL jour du milieu (J2).
        var resultat = new AnnulerDelegationHandler(periodes)
            .Handle(new AnnulerDelegationCommand(Mercredi_08, LeaId));
        Assert.True(resultat.EstSucces);
        Assert.True(resultat.Valeur!.AvaitDelegation);

        // Then — SEUL J2 retombe sur le FOND (Alice) ; J1 et J3 RESTENT délégués à Bruno.
        Assert.Equal(ParentA, CaseDuJour(grille, Mercredi_08).ResponsableId);
        Assert.Equal(ParentB, CaseDuJour(grille, Mardi_07).ResponsableId);
        Assert.Equal(ParentB, CaseDuJour(grille, Jeudi_09).ResponsableId);

        // Then — aucune écriture partielle n'a touché J1 ni J3 : deux surcharges [J1..J1] et [J3..J3] à Bruno,
        // AUCUNE ne couvre plus J2.
        var snaps = periodes.AllSnapshots();
        Assert.Equal(2, snaps.Count);
        Assert.All(snaps, s => Assert.Equal(ParentB, s.ResponsableId));
        Assert.Contains(snaps, s => Couvre(s, Mardi_07) && !Couvre(s, Mercredi_08));
        Assert.Contains(snaps, s => Couvre(s, Jeudi_09) && !Couvre(s, Mercredi_08));
        Assert.DoesNotContain(snaps, s => Couvre(s, Mercredi_08));

        // Then — les transferts dérivés s31 sont RECALCULÉS : le trou en J2 produit ses bascules.
        //  - sortie après J1 : à J2, Bruno → Alice
        var sortie = CaseDuJour(grille, Mercredi_08);
        Assert.NotNull(sortie.Transfert);
        Assert.Equal("Bruno", sortie.Transfert!.NomDepart);
        Assert.Equal("Alice", sortie.Transfert.NomArrivee);
        //  - entrée avant J3 : à J3, Alice → Bruno
        var entree = CaseDuJour(grille, Jeudi_09);
        Assert.NotNull(entree.Transfert);
        Assert.Equal("Alice", entree.Transfert!.NomDepart);
        Assert.Equal("Bruno", entree.Transfert.NomArrivee);
        //  - entrée initiale de plage à J1 (Alice → Bruno) et sortie finale à J3+1 (Bruno → Alice) préservées
        Assert.Equal("Bruno", CaseDuJour(grille, Mardi_07).Transfert!.NomArrivee);
        Assert.Equal("Bruno", CaseDuJour(grille, Vendredi_10).Transfert!.NomDepart);
    }

    // ---------- Acceptation runtime — adaptateur InMemory RÉEL ----------
    [Fact]
    public void Acceptation_InMemory_Should_Decouper_la_plage_en_reprenant_le_milieu()
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

        new DeleguerRecuperationHandler(grille, periodes, config)
            .Handle(new DeleguerRecuperationCommand(Mardi_07, LeaId, brunoId, Jeudi_09));

        var resultat = new AnnulerDelegationHandler(periodes)
            .Handle(new AnnulerDelegationCommand(Mercredi_08, LeaId));
        Assert.True(resultat.EstSucces);

        // Store réel : deux surcharges Bruno, J2 découvert.
        var snaps = periodes.AllSnapshots();
        Assert.Equal(2, snaps.Count);
        Assert.DoesNotContain(snaps, s => Couvre(s, Mercredi_08));

        Assert.Equal(aliceId, CaseDuJour(grille, Mercredi_08).ResponsableId);
        Assert.Equal(brunoId, CaseDuJour(grille, Mardi_07).ResponsableId);
        Assert.Equal(brunoId, CaseDuJour(grille, Jeudi_09).ResponsableId);
    }
}
