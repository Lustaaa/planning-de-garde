using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 48 — Sc.2 — L'imprévu apparaît dans le flux notifications, trié par récence, lu/non-lu par utilisateur (@back)
//   Étant donné un imprévu signalé et des acteurs concernés par le jour/enfant
//   Quand le flux de notifications d'un acteur concerné est restitué
//   Alors l'événement d'imprévu y figure, trié par RÉCENCE d'écriture (le plus récent en tête)
//   Et il porte l'état lu/non-lu PAR utilisateur + entre dans le compteur de non-lus
//   Et marquer-lu est idempotent, sans affecter l'état non-lu d'un autre utilisateur
//
// Frontière Application : SignalerImprevuHandler + FluxNotificationsQuery + MarquerNotificationsLuesHandler.
public class Scenario48_S2_ImprevuDansLeFlux
{
    private const string LeaId = "enfant-lea";
    private static readonly DateOnly Mercredi_15_07_2026 = new(2026, 7, 15); // ISO 29 impaire → fond index 1 (Bruno)

    [Fact]
    public void Acceptation_Should_Figurer_dans_le_flux_des_concernes_trie_par_recence_avec_lu_non_lu_par_utilisateur()
    {
        // --- Given : cycle N=2, jour J résolu par le fond BRUNO (index 1). Alice SIGNALE sur le jour de Bruno ---
        var config = new ConfigurationFoyerEnMemoire();
        var aliceId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice")).Valeur!.ActeurId;
        var brunoId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Bruno")).Valeur!.ActeurId;
        var cycle = new CycleDeFondEnMemoire();
        cycle.DefinirCycle(new CycleDeFond(2, new Dictionary<int, string> { [0] = aliceId, [1] = brunoId }));

        var journal = new InMemoryJournalChangements();
        var horloge = new HorlogeFigee(new DateTime(2026, 7, 1, 8, 0, 0));
        var grille = new GrilleAgendaQuery(
            new InMemorySlotRepository(), new InMemoryPeriodeRepository(), config, config,
            cycle, config, new InMemorySlotRecurrentRepository(), new InMemoryTransfertRepository());
        Assert.Equal(brunoId, grille.Projeter(Mercredi_15_07_2026, VuePlanning.Semaine).Jours.Single(j => j.Date == Mercredi_15_07_2026).ResponsableId);

        var handler = new SignalerImprevuHandler(journal, horloge, grille);

        // --- When : Alice signale MALADE (t1) puis RETARD (t2, plus récent) sur le jour de Bruno ---
        var malade = handler.Handle(new SignalerImprevuCommand(Mercredi_15_07_2026, LeaId, TypeImprevu.Malade, aliceId)).Valeur!;
        horloge.Maintenant = horloge.Maintenant.AddMinutes(15);
        var retard = handler.Handle(new SignalerImprevuCommand(Mercredi_15_07_2026, LeaId, TypeImprevu.Retard, aliceId)).Valeur!;

        // --- Then : les deux acteurs sont CONCERNÉS — le signalant (Alice) ET le responsable résolu (Bruno) voient les imprévus ---
        var etat = new InMemoryEtatLectureNotifications();
        var fluxQuery = new FluxNotificationsQuery(journal, etat);

        var fluxBruno = fluxQuery.Flux(brunoId); // Bruno = responsable du jour = concerné
        Assert.Equal(new[] { retard.Id, malade.Id }, fluxBruno.Select(e => e.Id).ToArray()); // récence : retard (t2) en tête
        var fluxAlice = fluxQuery.Flux(aliceId); // Alice = signalant = concernée
        Assert.Equal(new[] { retard.Id, malade.Id }, fluxAlice.Select(e => e.Id).ToArray());

        // --- Then : lu/non-lu PAR utilisateur — 2 non-lus au départ pour chacun ---
        Assert.Equal(2, fluxQuery.NombreNonLus(brunoId));
        Assert.Equal(2, fluxQuery.NombreNonLus(aliceId));

        // --- When : Bruno marque l'imprévu « malade » lu ---
        var marquer = new MarquerNotificationsLuesHandler(fluxQuery, etat);
        marquer.Handle(new MarquerNotificationsLuesCommand(brunoId, malade.Id));

        // --- Then : compteur de Bruno = 1, Alice inchangée (2) ; marquer-lu idempotent ---
        Assert.Equal(1, fluxQuery.NombreNonLus(brunoId));
        Assert.Equal(2, fluxQuery.NombreNonLus(aliceId));
        marquer.Handle(new MarquerNotificationsLuesCommand(brunoId, malade.Id)); // idempotent
        Assert.Equal(1, fluxQuery.NombreNonLus(brunoId));
        Assert.Contains(fluxQuery.FluxAvecEtat(brunoId), n => n.Evenement.Id == malade.Id && n.Lu);
    }
}
