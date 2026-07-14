using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 42 — Sc.1 — Query PURE : composer le « qui » du jour (responsable résolu) (@back)
//   Étant donné un foyer configuré (cycle de fond, périodes de garde) et un enfant sélectionné
//   Et une DATE cible dont le responsable est résolu par la résolution existante (surcharge > fond > neutre)
//   Quand j'interroge la query « qui récupère ce jour-là » pour cette date + cet enfant
//   Alors elle restitue le RESPONSABLE RÉSOLU (id stable) avec son nom et sa couleur, tels que la grille les résout
//   Et le « qui » provient de la COMPOSITION de la résolution existante (aucune priorité surcharge>fond réimplémentée)
//   Et la query est PURE (aucune mutation, aucun store neuf, miroir GrapheFoyerQuery s38)
//
// Frontière Application (query agrégée composant GrilleAgendaQuery). L'acceptation runtime sur les DEUX
// adaptateurs est portée par l'acceptation InMemory RÉELLE ci-dessous + le test Mongo (Api.Tests).
public class Scenario42_S1_CarteDuJourResponsable
{
    private const string ParentA = "parent-a";
    private const string ParentB = "parent-b";
    private const string Alice = "Alice";
    private const string Bruno = "Bruno";
    private const string Bleu = "bleu";
    private const string Orange = "orange";
    private const string LeaId = "enfant-lea";

    private static readonly DateOnly Lundi_29_06_2026 = new(2026, 6, 29);   // référence (ISO 27)
    private static readonly DateOnly Mardi_07_07_2026 = new(2026, 7, 7);    // ISO 28 paire → fond Parent A
    private static readonly DateOnly Mercredi_08_07_2026 = new(2026, 7, 8); // surcharge explicite Parent B

    private static Dictionary<int, string> MappingPairAImpairB() => new() { [0] = ParentA, [1] = ParentB };

    private static CarteDuJourQuery Query(IPeriodeRepository periodes)
        => new(new GrilleAgendaQuery(
            new FakeSlotRepository(), periodes,
            new FakePaletteCouleurs(new Dictionary<string, string> { [ParentA] = Bleu, [ParentB] = Orange }),
            new FakeReferentielResponsables(new Dictionary<string, string> { [ParentA] = Alice, [ParentB] = Bruno }),
            new FakeReferentielCycleDeFond(new CycleDeFond(2, MappingPairAImpairB())),
            new FakeEnumerationActeursFoyer(ParentA, ParentB)));

    private static FakePeriodeRepository SurchargeParentBLe08()
    {
        var periodes = new FakePeriodeRepository();
        periodes.Enregistrer(PeriodeDeGarde.Affecter(ParentB,
            new DateTime(2026, 7, 8), new DateTime(2026, 7, 8)).Valeur!);
        return periodes;
    }

    // ---------- Acceptation (boucle externe, frontière Application) ----------
    [Fact]
    public void Acceptation_Should_Restituer_le_responsable_resolu_id_nom_couleur_par_surcharge_puis_fond()
    {
        var query = Query(SurchargeParentBLe08());

        // Jour de SURCHARGE : Parent B résolu (id stable + nom + couleur), assigné
        var surcharge = query.Lire(Mercredi_08_07_2026, LeaId).Responsable;
        Assert.True(surcharge.EstAssigne);
        Assert.Equal(ParentB, surcharge.ActeurId);
        Assert.Equal(Bruno, surcharge.Nom);
        Assert.Equal(Orange, surcharge.Couleur);

        // Jour de FOND (aucune période) : Parent A résolu par le cycle (composition, pas de réimplémentation)
        var fond = query.Lire(Mardi_07_07_2026, LeaId).Responsable;
        Assert.True(fond.EstAssigne);
        Assert.Equal(ParentA, fond.ActeurId);
        Assert.Equal(Alice, fond.Nom);
        Assert.Equal(Bleu, fond.Couleur);
    }

    // ---------- Boucle interne (TDD) ----------

    // Test #1 — la carte compose la MÊME résolution que la grille (source unique) : nom/couleur identiques.
    [Fact]
    public void Should_Composer_la_meme_resolution_que_la_grille_pour_la_date()
    {
        var periodes = SurchargeParentBLe08();
        var grille = new GrilleAgendaQuery(
            new FakeSlotRepository(), periodes,
            new FakePaletteCouleurs(new Dictionary<string, string> { [ParentA] = Bleu, [ParentB] = Orange }),
            new FakeReferentielResponsables(new Dictionary<string, string> { [ParentA] = Alice, [ParentB] = Bruno }),
            new FakeReferentielCycleDeFond(new CycleDeFond(2, MappingPairAImpairB())),
            new FakeEnumerationActeursFoyer(ParentA, ParentB));
        var caseGrille = grille.Projeter(Mercredi_08_07_2026, VuePlanning.Semaine)
            .Jours.Single(j => j.Date == Mercredi_08_07_2026);

        var responsable = new CarteDuJourQuery(grille).Lire(Mercredi_08_07_2026, LeaId).Responsable;

        Assert.Equal(caseGrille.NomResponsable, responsable.Nom);
        Assert.Equal(caseGrille.CouleurResponsable, responsable.Couleur);
    }

    // ---------- Acceptation runtime — adaptateur InMemory RÉEL (1er des deux adaptateurs, Sc.1) ----------
    [Fact]
    public void Acceptation_InMemory_Should_Restituer_le_responsable_sur_les_adaptateurs_reels()
    {
        // Given — config foyer InMemory réelle (noms + couleurs + existence) + période InMemory réelle
        var config = new ConfigurationFoyerEnMemoire();
        var aliceId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice")).Valeur!.ActeurId;
        var periodes = new InMemoryPeriodeRepository();
        periodes.Enregistrer(PeriodeDeGarde.Affecter(aliceId,
            new DateTime(2026, 7, 8), new DateTime(2026, 7, 8)).Valeur!);

        var grille = new GrilleAgendaQuery(
            new InMemorySlotRepository(), periodes, config, config,
            new CycleDeFondEnMemoire(), config, new InMemorySlotRecurrentRepository(), new InMemoryTransfertRepository());

        // When — la carte câblée sur les adaptateurs RÉELS
        var responsable = new CarteDuJourQuery(grille).Lire(Mercredi_08_07_2026, LeaId).Responsable;

        // Then — Alice résolue depuis le store réel (id stable + nom), assignée
        Assert.True(responsable.EstAssigne);
        Assert.Equal(aliceId, responsable.ActeurId);
        Assert.Equal("Alice", responsable.Nom);
    }
}
