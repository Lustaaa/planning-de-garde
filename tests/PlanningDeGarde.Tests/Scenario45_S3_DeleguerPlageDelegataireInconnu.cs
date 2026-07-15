using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 45 — Sc.3 — Cas ERREUR plage : délégataire inconnu / orphelin (@back)
//   Étant donné une plage [J1..J2] et un identifiant d'acteur délégataire ABSENT du store (inconnu / supprimé)
//   Quand je tente de déléguer la récupération de la plage [J1..J2] à cet acteur
//   Alors la délégation est REFUSÉE AVANT toute écriture (validation d'existence du délégataire)
//   Et le store des périodes reste INTACT — AUCUN jour de la plage écrit, aucune écriture partielle
//   Et le comportement est IDENTIQUE sur les deux adaptateurs (InMemory prouvé ici, Mongo en Api.Tests)
//
// Frontière Application (DeleguerRecuperationHandler). Refus = ATOMIQUE sur toute la plage.
public class Scenario45_S3_DeleguerPlageDelegataireInconnu
{
    private const string ParentA = "parent-a";
    private const string ParentB = "parent-b";
    private const string Fantome = "acteur-supprime-ou-inconnu";
    private const string LeaId = "enfant-lea";

    private static readonly DateOnly Mardi_07 = new(2026, 7, 7);   // J1
    private static readonly DateOnly Jeudi_09 = new(2026, 7, 9);   // J2

    private static GrilleAgendaQuery Grille(IPeriodeRepository periodes)
        => new(
            new FakeSlotRepository(), periodes,
            new FakePaletteCouleurs(new Dictionary<string, string> { [ParentA] = "bleu", [ParentB] = "orange" }),
            new FakeReferentielResponsables(new Dictionary<string, string> { [ParentA] = "Alice", [ParentB] = "Bruno" }),
            new FakeReferentielCycleDeFond(new CycleDeFond(2, new Dictionary<int, string> { [0] = ParentA, [1] = ParentB })),
            new FakeEnumerationActeursFoyer(ParentA, ParentB));

    [Fact]
    public void Should_Refuser_avant_ecriture_When_le_delegataire_dune_plage_est_absent_du_store()
    {
        var periodes = new FakePeriodeRepository();
        var handler = new DeleguerRecuperationHandler(Grille(periodes), periodes, new FakeEnumerationActeursFoyer(ParentA, ParentB));

        var resultat = handler.Handle(new DeleguerRecuperationCommand(Mardi_07, LeaId, Fantome, Jeudi_09));

        Assert.False(resultat.EstSucces);
        Assert.False(string.IsNullOrWhiteSpace(resultat.Motif));
        // Store INTACT : refus AVANT écriture, AUCUN jour de la plage écrit (aucune écriture partielle).
        Assert.Empty(periodes.AllSnapshots());
    }

    // ---------- Acceptation runtime — adaptateur InMemory RÉEL ----------
    [Fact]
    public void Acceptation_InMemory_Should_Refuser_la_plage_vers_un_delegataire_orphelin_store_intact()
    {
        var config = new ConfigurationFoyerEnMemoire();
        var aliceId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice")).Valeur!.ActeurId;
        var orphelinId = "acteur-absent-" + Guid.NewGuid().ToString("N");

        var cycle = new CycleDeFondEnMemoire();
        cycle.DefinirCycle(new CycleDeFond(1, new Dictionary<int, string> { [0] = aliceId }));

        var periodes = new InMemoryPeriodeRepository();
        var grille = new GrilleAgendaQuery(
            new InMemorySlotRepository(), periodes, config, config,
            cycle, config, new InMemorySlotRecurrentRepository(), new InMemoryTransfertRepository());

        var resultat = new DeleguerRecuperationHandler(grille, periodes, config)
            .Handle(new DeleguerRecuperationCommand(Mardi_07, LeaId, orphelinId, Jeudi_09));

        Assert.False(resultat.EstSucces);
        Assert.Empty(periodes.AllSnapshots());
    }
}
