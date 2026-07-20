using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 47 — Sc.1 — Journal de changements append-only, trace de lecture horodatée (@back)
//   Étant donné des handlers d'écriture existants (délégation s44, reprise s46, transfert s31)
//   Quand chacun réalise son écriture, il consigne un événement au JOURNAL DE CHANGEMENTS
//   Et en particulier une REPRISE s46 (qui SUPPRIME la surcharge) consigne bien son événement
//   Quand on interroge le flux d'un utilisateur destinataire
//   Alors chaque changement le concernant apparaît, trié par RÉCENCE de l'écriture (le plus récent en tête)
//   Et le journal est une TRACE DE LECTURE : il n'est JAMAIS lu par la résolution (la vérité reste périodes/transferts)
//
// Frontière Application : instrumentation des handlers d'écriture + FluxNotificationsQuery (lecture pure).
public class Scenario47_S1_JournalChangements
{
    private const string LeaId = "enfant-lea";
    private const string Creche = "lieu-creche";
    private static readonly DateOnly Mercredi_08_07_2026 = new(2026, 7, 8); // ISO 28 paire → fond acteur index 0
    private static readonly DateOnly Jeudi_09_07_2026 = new(2026, 7, 9);    // même semaine ISO 28 → fond index 0

    private static JourCase CaseDuJour(GrilleAgendaQuery grille, DateOnly jour)
        => grille.Projeter(jour, VuePlanning.Semaine).Jours.Single(j => j.Date == jour);

    [Fact]
    public void Acceptation_Should_Consigner_chaque_ecriture_incluant_la_reprise_sans_etre_lu_par_la_resolution()
    {
        // --- Given : foyer InMemory réel, cycle N=2 (index 0 pair → Alice), jour J résolu par le fond Alice ---
        var config = new ConfigurationFoyerEnMemoire();
        var aliceId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice")).Valeur!.ActeurId;
        var brunoId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Bruno")).Valeur!.ActeurId;
        var cycle = new CycleDeFondEnMemoire();
        var cycleDef = new CycleDeFond(2, new Dictionary<int, string> { [0] = aliceId, [1] = brunoId }); cycle.DefinirCycle(cycleDef); cycle.DefinirCycle(cycleDef, LeaId);

        var periodes = new InMemoryPeriodeRepository();
        var transferts = new InMemoryTransfertRepository();
        var journal = new InMemoryJournalChangements();
        var horloge = new HorlogeFigee(new DateTime(2026, 7, 1, 8, 0, 0));
        var grille = new GrilleAgendaQuery(
            new InMemorySlotRepository(), periodes, config, config,
            cycle, config, new InMemorySlotRecurrentRepository(), transferts);

        Assert.Equal(aliceId, CaseDuJour(grille, Mercredi_08_07_2026).ResponsableId);

        // --- t1 : DÉLÉGATION Alice → Bruno du jour J (s44) ---
        new DeleguerRecuperationHandler(grille, periodes, config, journal, horloge)
            .Handle(new DeleguerRecuperationCommand(Mercredi_08_07_2026, LeaId, brunoId));

        // --- t2 : TRANSFERT saisi Alice → Bruno un autre jour (s31) ---
        horloge.Maintenant = horloge.Maintenant.AddMinutes(10);
        new DefinirTransfertHandler(transferts, journal, horloge)
            .Handle(new DefinirTransfertCommand(aliceId, brunoId, Creche, TimeSpan.FromHours(17), Jeudi_09_07_2026.ToDateTime(TimeOnly.MinValue)));

        // --- t3 : REPRISE du jour J (s46) — SUPPRIME la surcharge, mais consigne son événement ---
        horloge.Maintenant = horloge.Maintenant.AddMinutes(10);
        new AnnulerDelegationHandler(periodes, journal, horloge)
            .Handle(new AnnulerDelegationCommand(Mercredi_08_07_2026, LeaId));

        // --- Then : le journal ne dérive PAS de l'état courant — la reprise a consigné son événement... ---
        Assert.Contains(journal.Tout(), e => e.Type == TypeChangement.Reprise && e.Jour == Mercredi_08_07_2026 && e.CedantId == brunoId);
        // ... alors même que la SURCHARGE a disparu (état courant vide) et que la case retombe sur le FOND Alice.
        Assert.Empty(periodes.AllSnapshots());
        Assert.Equal(aliceId, CaseDuJour(grille, Mercredi_08_07_2026).ResponsableId); // résolution IGNORE le journal

        // --- Then : le flux de Bruno (destinataire concerné par les 3 écritures), trié par RÉCENCE (récent en tête) ---
        var fluxBruno = new FluxNotificationsQuery(journal).Flux(brunoId);
        Assert.Equal(3, fluxBruno.Count);
        Assert.Equal(new[] { TypeChangement.Reprise, TypeChangement.Transfert, TypeChangement.Delegation },
            fluxBruno.Select(e => e.Type).ToArray());
        // Horodatages STRICTEMENT décroissants (récence de l'écriture).
        Assert.True(fluxBruno[0].Horodatage > fluxBruno[1].Horodatage && fluxBruno[1].Horodatage > fluxBruno[2].Horodatage);

        // --- Then : « le concernant » — le flux d'Alice ne retient que les écritures où elle figure (déleg + transfert) ---
        var fluxAlice = new FluxNotificationsQuery(journal).Flux(aliceId);
        Assert.Equal(new[] { TypeChangement.Transfert, TypeChangement.Delegation }, fluxAlice.Select(e => e.Type).ToArray());
    }
}
