using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 48 — Sc.3 — Cas limite : motif vide, jour hors fenêtre (1ᵉʳ des deux adaptateurs : InMemory) (@back)
//   Étant donné un signalement d'imprévu avec un motif optionnel LAISSÉ VIDE
//   Quand l'imprévu est consigné → l'enregistrement est valide (motif vide accepté, aucune écriture partielle)
//   Et un imprévu signalé sur un jour HORS de la fenêtre de grille chargée s'enregistre sans crash
//   (le comportement Mongo durable est prouvé en Api.Tests — 2ᵉ adaptateur).
public class Scenario48_S3_CasLimiteImprevu
{
    private const string LeaId = "enfant-lea";

    [Fact]
    public void Acceptation_Should_Accepter_motif_vide_et_jour_hors_fenetre_sans_crash()
    {
        var config = new ConfigurationFoyerEnMemoire();
        var aliceId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice")).Valeur!.ActeurId;
        var brunoId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Bruno")).Valeur!.ActeurId;
        var cycle = new CycleDeFondEnMemoire();
        cycle.DefinirCycle(new CycleDeFond(2, new Dictionary<int, string> { [0] = aliceId, [1] = brunoId }));

        var journal = new InMemoryJournalChangements();
        var periodes = new InMemoryPeriodeRepository();
        var horloge = new HorlogeFigee(new DateTime(2026, 7, 1, 8, 0, 0));
        var grille = new GrilleAgendaQuery(
            new InMemorySlotRepository(), periodes, config, config,
            cycle, config, new InMemorySlotRecurrentRepository(), new InMemoryTransfertRepository());
        var handler = new SignalerImprevuHandler(journal, horloge, grille);

        // --- When : motif LAISSÉ VIDE (défaut "") sur un jour ordinaire ---
        var motifVide = handler.Handle(new SignalerImprevuCommand(new DateOnly(2026, 7, 8), LeaId, TypeImprevu.Malade, aliceId));

        // --- Then : enregistrement valide, motif vide accepté ---
        Assert.True(motifVide.EstSucces);
        Assert.Equal("", motifVide.Valeur!.Motif);

        // --- When : un jour TRÈS ÉLOIGNÉ, hors de toute fenêtre chargée ---
        var horsFenetre = new DateOnly(2028, 12, 31);
        var loin = handler.Handle(new SignalerImprevuCommand(horsFenetre, LeaId, TypeImprevu.Retard, aliceId));

        // --- Then : enregistré sans crash, sur la bonne date ---
        Assert.True(loin.EstSucces);
        Assert.Equal(horsFenetre, loin.Valeur!.Jour);
        Assert.Equal(2, journal.Tout().Count);

        // --- Then : aucune écriture partielle côté résolution (store surcharges intact) ---
        Assert.Empty(periodes.AllSnapshots());
    }
}
