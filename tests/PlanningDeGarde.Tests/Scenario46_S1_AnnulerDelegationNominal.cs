using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 46 — Sc.1 — Reprendre un jour délégué (ponctuel s44) → la case retombe sur le fond (@back)
//   Étant donné un cycle de fond qui résout "Alice" responsable le jour J
//   Et une délégation ponctuelle (s44) posée le jour J confiant la récupération à "Bruno"
//   Et donc "Bruno" résolu ce jour-là (surcharge > fond) avec un transfert dérivé s31 Alice→Bruno
//   Quand j'exécute AnnulerDelegation(jour J, enfant E)
//   Alors la surcharge du jour J est supprimée via le chemin EXISTANT s16 (aucun store neuf)
//   Et la résolution retombe sur le FOND : "Alice" est de nouveau responsable
//   Et le transfert bicolore dérivé s31 du jour J DISPARAÎT
//   Et le résultat est identique sur les DEUX adaptateurs (InMemory prouvé ici, Mongo dans Api.Tests)
//
// Frontière Application : le use case COMPOSE la suppression de surcharge EXISTANTE (s16) — aucun store,
// aucun modèle, aucune dérivation neuve. Le transfert s31 se re-dérive de la résolution après suppression.
public class Scenario46_S1_AnnulerDelegationNominal
{
    private const string ParentA = "parent-a";
    private const string ParentB = "parent-b";
    private const string LeaId = "enfant-lea";

    // ISO 28 (semaine du lundi 06/07/2026) → index 0 (pair) → Parent A par le fond ; aucune surcharge ces jours-là.
    private static readonly DateOnly Mercredi_08_07_2026 = new(2026, 7, 8);
    private static readonly DateOnly Mardi_07_07_2026 = new(2026, 7, 7);

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

    // ---------- Test d'acceptation (boucle externe, frontière Application) ----------
    [Fact]
    public void Acceptation_Should_Supprimer_la_surcharge_et_faire_retomber_la_case_sur_le_fond_sans_transfert()
    {
        var periodes = new FakePeriodeRepository();
        var grille = Grille(periodes);

        // Given — la récupération du jour J a été DÉLÉGUÉE à Bruno (s44) : la case résout Bruno, transfert dérivé Alice→Bruno.
        new DeleguerRecuperationHandler(grille, periodes, new FakeEnumerationActeursFoyer(ParentA, ParentB))
            .Handle(new DeleguerRecuperationCommand(Mercredi_08_07_2026, LeaId, ParentB));
        var avant = CaseDuJour(grille, Mercredi_08_07_2026);
        Assert.Equal(ParentB, avant.ResponsableId);
        Assert.NotNull(avant.Transfert);
        Assert.Single(periodes.AllSnapshots());

        // When — je reprends ce jour.
        var resultat = new AnnulerDelegationHandler(periodes)
            .Handle(new AnnulerDelegationCommand(Mercredi_08_07_2026, LeaId));

        Assert.True(resultat.EstSucces);
        Assert.True(resultat.Valeur!.AvaitDelegation);

        // Then — la surcharge est supprimée du store (chemin s16).
        Assert.Empty(periodes.AllSnapshots());

        // Then — la case retombe sur le FOND : Alice de nouveau responsable, aucun transfert dérivé.
        var apres = CaseDuJour(grille, Mercredi_08_07_2026);
        Assert.Equal(ParentA, apres.ResponsableId);
        Assert.Equal("Alice", apres.NomResponsable);
        Assert.Null(apres.Transfert);

        // Then — les jours voisins de fond restent intacts.
        Assert.Equal(ParentA, CaseDuJour(grille, Mardi_07_07_2026).ResponsableId);
    }

    // ---------- Acceptation runtime — adaptateur InMemory RÉEL (1er des deux adaptateurs) ----------
    [Fact]
    public void Acceptation_InMemory_Should_Reprendre_le_jour_via_les_adaptateurs_reels()
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

        // Given — délégation posée à Bruno via le chemin réel.
        new DeleguerRecuperationHandler(grille, periodes, config)
            .Handle(new DeleguerRecuperationCommand(Mercredi_08_07_2026, LeaId, brunoId));
        Assert.Equal(brunoId, CaseDuJour(grille, Mercredi_08_07_2026).ResponsableId);

        // When — je reprends ce jour.
        var resultat = new AnnulerDelegationHandler(periodes)
            .Handle(new AnnulerDelegationCommand(Mercredi_08_07_2026, LeaId));
        Assert.True(resultat.EstSucces);

        // Then — store vidé, case résolue par le fond (Alice), transfert dérivé disparu.
        Assert.Empty(periodes.AllSnapshots());
        var caseJour = CaseDuJour(grille, Mercredi_08_07_2026);
        Assert.Equal(aliceId, caseJour.ResponsableId);
        Assert.Null(caseJour.Transfert);
    }
}
