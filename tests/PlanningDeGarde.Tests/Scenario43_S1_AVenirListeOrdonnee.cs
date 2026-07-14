using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 43 — Sc.1 — Query PURE : liste ordonnée des « à venir » (responsable résolu par jour) (@back)
//   Étant donné un foyer configuré (cycle de fond, périodes de garde) et un enfant sélectionné
//   Et une fenêtre de grille déjà chargée couvrant aujourd'hui et des jours suivants
//   Quand j'interroge la query « à venir » pour cette fenêtre + cet enfant
//   Alors elle restitue une LISTE ORDONNÉE (par date croissante) des JOURS À VENIR strictement après aujourd'hui
//   Et chaque entrée porte le RESPONSABLE RÉSOLU (id stable) avec son nom et sa couleur, tels que la grille les résout
//   Et le « qui » provient de la COMPOSITION de la résolution existante (aucune priorité surcharge>fond réimplémentée)
//   Et la query est PURE (aucune mutation, aucun store neuf, miroir CarteDuJourQuery s42)
//   Et le résultat est IDENTIQUE sur les DEUX adaptateurs (InMemory RÉEL ici + Mongo dans Api.Tests)
public class Scenario43_S1_AVenirListeOrdonnee
{
    private const string ParentA = "parent-a";
    private const string ParentB = "parent-b";
    private const string Alice = "Alice";
    private const string Bruno = "Bruno";
    private const string Bleu = "bleu";
    private const string Orange = "orange";
    private const string LeaId = "enfant-lea";

    private static readonly DateOnly Mardi_07_07_2026 = new(2026, 7, 7);     // « aujourd'hui » (ancre)
    private static readonly DateOnly Mercredi_08_07_2026 = new(2026, 7, 8);  // surcharge explicite Parent B

    private static Dictionary<int, string> MappingPairAImpairB() => new() { [0] = ParentA, [1] = ParentB };

    private static AVenirQuery Query(IPeriodeRepository periodes)
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
    public void Acceptation_Should_Restituer_une_liste_ordonnee_des_jours_a_venir_avec_responsable_resolu()
    {
        var query = Query(SurchargeParentBLe08());

        var aVenir = query.Lire(Mardi_07_07_2026, LeaId, VuePlanning.Semaine);

        // Uniquement les jours STRICTEMENT après aujourd'hui, dans la fenêtre (semaine du 06→12/07)
        Assert.All(aVenir, j => Assert.True(j.Date > Mardi_07_07_2026));
        Assert.DoesNotContain(aVenir, j => j.Date == Mardi_07_07_2026);

        // Ordonnée par date croissante
        Assert.Equal(aVenir.Select(j => j.Date).OrderBy(d => d), aVenir.Select(j => j.Date));

        // Jour de SURCHARGE (08/07) : Parent B résolu (id stable + nom + couleur), assigné
        var surcharge = aVenir.Single(j => j.Date == Mercredi_08_07_2026).Responsable;
        Assert.True(surcharge.EstAssigne);
        Assert.Equal(ParentB, surcharge.ActeurId);
        Assert.Equal(Bruno, surcharge.Nom);
        Assert.Equal(Orange, surcharge.Couleur);
    }

    // ---------- Boucle interne (TDD) ----------

    // La liste compose la MÊME résolution que la grille (source unique) : chaque jour à venir porte
    // exactement le responsable de la case correspondante — aucune priorité surcharge>fond réimplémentée.
    [Fact]
    public void Should_Composer_la_meme_resolution_que_la_grille_pour_chaque_jour_a_venir()
    {
        var periodes = SurchargeParentBLe08();
        var grille = new GrilleAgendaQuery(
            new FakeSlotRepository(), periodes,
            new FakePaletteCouleurs(new Dictionary<string, string> { [ParentA] = Bleu, [ParentB] = Orange }),
            new FakeReferentielResponsables(new Dictionary<string, string> { [ParentA] = Alice, [ParentB] = Bruno }),
            new FakeReferentielCycleDeFond(new CycleDeFond(2, MappingPairAImpairB())),
            new FakeEnumerationActeursFoyer(ParentA, ParentB));
        var casesGrille = grille.Projeter(Mardi_07_07_2026, VuePlanning.Semaine)
            .Jours.Where(j => j.Date > Mardi_07_07_2026).ToList();

        var aVenir = new AVenirQuery(grille).Lire(Mardi_07_07_2026, LeaId, VuePlanning.Semaine);

        Assert.Equal(casesGrille.Count, aVenir.Count);
        foreach (var jourGrille in casesGrille)
        {
            var entree = aVenir.Single(j => j.Date == jourGrille.Date);
            Assert.Equal(jourGrille.NomResponsable, entree.Responsable.Nom);
            Assert.Equal(jourGrille.CouleurResponsable, entree.Responsable.Couleur);
            Assert.Equal(jourGrille.ResponsableId, entree.Responsable.ActeurId);
        }
    }

    // ---------- Acceptation runtime — adaptateur InMemory RÉEL (1er des deux adaptateurs, Sc.1) ----------
    [Fact]
    public void Acceptation_InMemory_Should_Restituer_la_liste_a_venir_sur_les_adaptateurs_reels()
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

        // When — la liste câblée sur les adaptateurs RÉELS
        var aVenir = new AVenirQuery(grille).Lire(Mardi_07_07_2026, LeaId, VuePlanning.Semaine);

        // Then — le 08/07 (surcharge Alice) apparaît en à-venir, résolu depuis le store réel
        var jour = aVenir.Single(j => j.Date == Mercredi_08_07_2026);
        Assert.True(jour.Responsable.EstAssigne);
        Assert.Equal(aliceId, jour.Responsable.ActeurId);
        Assert.Equal("Alice", jour.Responsable.Nom);
        Assert.All(aVenir, j => Assert.True(j.Date > Mardi_07_07_2026));
    }
}
