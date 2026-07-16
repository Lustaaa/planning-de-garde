using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 48 — Sc.4 — Cas erreur / invariant : type inconnu refusé, journal reste non-autorité (@back)
//   Étant donné une demande portant un type d'imprévu INCONNU (ni malade ni retard)
//   Quand la commande est traitée → elle est REFUSÉE AVANT écriture (aucun événement, aucune écriture partielle)
//   Et pour un signalement valide, aucune lecture ultérieure de la résolution ne consulte le journal
//   Et écrire/supprimer une surcharge par ailleurs n'altère jamais la vérité via le journal (séparation tenue)
public class Scenario48_S4_ImprevuTypeInconnu
{
    private const string LeaId = "enfant-lea";
    private static readonly DateOnly Mercredi_08_07_2026 = new(2026, 7, 8); // ISO 28 paire → fond Alice (index 0)

    private static JourCase CaseDuJour(GrilleAgendaQuery grille, DateOnly jour)
        => grille.Projeter(jour, VuePlanning.Semaine).Jours.Single(j => j.Date == jour);

    [Fact]
    public void Acceptation_Should_Refuser_type_inconnu_avant_ecriture_et_tenir_la_separation_journal_resolution()
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

        // --- When : un type d'imprévu INCONNU (hors { Malade, Retard }) est signalé ---
        var inconnu = handler.Handle(new SignalerImprevuCommand(Mercredi_08_07_2026, LeaId, (TypeImprevu)99, aliceId));

        // --- Then : REFUSÉ AVANT écriture — aucun événement consigné, aucune écriture partielle ---
        Assert.False(inconnu.EstSucces);
        Assert.Empty(journal.Tout());

        // --- Given : un signalement VALIDE ---
        handler.Handle(new SignalerImprevuCommand(Mercredi_08_07_2026, LeaId, TypeImprevu.Malade, aliceId));
        Assert.Single(journal.Tout());

        // --- Then : la résolution IGNORE le journal — la case reste sur le fond Alice malgré l'imprévu consigné ---
        Assert.Equal(aliceId, CaseDuJour(grille, Mercredi_08_07_2026).ResponsableId);

        // --- Then : séparation tenue — écrire une surcharge fait basculer la résolution (périodes = vérité), pas le journal ---
        new DeleguerRecuperationHandler(grille, periodes, config)
            .Handle(new DeleguerRecuperationCommand(Mercredi_08_07_2026, LeaId, brunoId));
        Assert.Equal(brunoId, CaseDuJour(grille, Mercredi_08_07_2026).ResponsableId);

        // --- Then : supprimer la surcharge fait retomber la résolution sur le fond — le journal reste non-autorité ---
        new AnnulerDelegationHandler(periodes)
            .Handle(new AnnulerDelegationCommand(Mercredi_08_07_2026, LeaId));
        Assert.Equal(aliceId, CaseDuJour(grille, Mercredi_08_07_2026).ResponsableId);
    }
}
