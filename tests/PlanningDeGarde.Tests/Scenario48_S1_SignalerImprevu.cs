using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 48 — Sc.1 — Signaler un imprévu consigne au JOURNAL s47 SANS toucher la résolution (@back)
//   Étant donné un jour dont le responsable est résolu (fond) et un enfant du foyer
//   Quand un parent signale un imprévu « malade » (ou « retard ») sur ce jour pour cet enfant
//   Alors un événement d'imprévu {type: malade|retard, jour, enfant, acteur signalant, horodatage} est consigné au JOURNAL
//   Et le store des surcharges reste INTACT (aucune surcharge écrite)
//   Et aucun transfert n'est dérivé, aucune bascule de responsable n'a lieu
//   Et la résolution de la case reste STRICTEMENT inchangée (le journal n'est jamais lu par la résolution)
//
// Frontière Application : SignalerImprevuHandler (écriture) + observation du journal / store / résolution.
public class Scenario48_S1_SignalerImprevu
{
    private const string LeaId = "enfant-lea";
    private static readonly DateOnly Mercredi_08_07_2026 = new(2026, 7, 8); // ISO 28 paire → fond index 0

    private static JourCase CaseDuJour(GrilleAgendaQuery grille, DateOnly jour)
        => grille.Projeter(jour, VuePlanning.Semaine).Jours.Single(j => j.Date == jour);

    [Fact]
    public void Acceptation_Should_Consigner_l_imprevu_sans_ecrire_de_surcharge_ni_changer_la_resolution()
    {
        // --- Given : foyer réel, cycle N=2 (index 0 pair → Alice), jour J résolu par le fond Alice ---
        var config = new ConfigurationFoyerEnMemoire();
        var aliceId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice")).Valeur!.ActeurId;
        var brunoId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Bruno")).Valeur!.ActeurId;
        var cycle = new CycleDeFondEnMemoire();
        cycle.DefinirCycle(new CycleDeFond(2, new Dictionary<int, string> { [0] = aliceId, [1] = brunoId }));

        var periodes = new InMemoryPeriodeRepository();
        var transferts = new InMemoryTransfertRepository();
        var journal = new InMemoryJournalChangements();
        var horloge = new HorlogeFigee(new DateTime(2026, 7, 1, 8, 30, 0));
        var grille = new GrilleAgendaQuery(
            new InMemorySlotRepository(), periodes, config, config,
            cycle, config, new InMemorySlotRecurrentRepository(), transferts);

        Assert.Equal(aliceId, CaseDuJour(grille, Mercredi_08_07_2026).ResponsableId);

        // --- When : Alice signale que Léa est MALADE le mercredi J ---
        var resultat = new SignalerImprevuHandler(journal, horloge)
            .Handle(new SignalerImprevuCommand(Mercredi_08_07_2026, LeaId, TypeImprevu.Malade, aliceId));

        // --- Then : succès, un événement d'imprévu est consigné au JOURNAL {type malade, jour, enfant, signalant, horodatage} ---
        Assert.True(resultat.EstSucces);
        var evt = Assert.Single(journal.Tout());
        Assert.Equal(TypeChangement.Imprevu, evt.Type);
        Assert.Equal(TypeImprevu.Malade, evt.Imprevu);
        Assert.Equal(Mercredi_08_07_2026, evt.Jour);
        Assert.Equal(LeaId, evt.EnfantId);
        Assert.Equal(horloge.Maintenant, evt.Horodatage);
        Assert.Contains(aliceId, new[] { evt.CedantId, evt.RecevantId }); // le signalant figure sur l'événement

        // --- Then : l'imprévu figure dans le flux de l'acteur signalant ---
        Assert.Contains(new FluxNotificationsQuery(journal).Flux(aliceId), e => e.Id == evt.Id);

        // --- Then (anti vert-qui-ment) : le store des surcharges reste INTACT, aucun transfert dérivé ---
        Assert.Empty(periodes.AllSnapshots());
        Assert.Empty(transferts.AllSnapshots());

        // --- Then : la résolution de la case est STRICTEMENT inchangée (le journal n'est jamais lu par la résolution) ---
        Assert.Equal(aliceId, CaseDuJour(grille, Mercredi_08_07_2026).ResponsableId);
    }
}
