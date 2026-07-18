using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 51 — Sc.3 — Cas limite : ré-proposition last-write-wins, jour hors fenêtre (adaptateur InMemory) (@back)
//   Étant donné un imprévu sur un jour J pour un enfant E
//   Quand un échange est proposé en réaction PUIS re-proposé (autre versActeur) sur le même jour/enfant
//   Alors la dernière proposition GAGNE (last-write-wins R11), sans doublon de proposition pending
//   Et un imprévu situé sur un jour J HORS de la fenêtre de grille chargée accepte une proposition greffée sans crash
//
// Frontière Application : la composition s51 hérite des invariants de ProposerEchange s47 (R11) et de la
// délégation s44 (surcharge d'une date isolée, insensible à la fenêtre).
public class Scenario51_S3_CasLimiteSuiteImprevu
{
    private const string LeaId = "enfant-lea";
    private static readonly DateOnly Mercredi_08_07_2026 = new(2026, 7, 8);   // ISO 28 paire → fond Alice (index 0)
    private static readonly DateOnly LointainHorsFenetre = new(2027, 3, 17);  // très loin de toute fenêtre par défaut

    private static JourCase CaseDuJour(GrilleAgendaQuery grille, DateOnly jour)
        => grille.Projeter(jour, VuePlanning.Semaine).Jours.Single(j => j.Date == jour);

    private sealed record Contexte(
        ProposerEchangeSuiteImprevuHandler Proposer, AccepterPropositionHandler Accepter,
        SignalerImprevuHandler Signaler, InMemoryPropositionEchangeRepository Propositions,
        InMemoryPeriodeRepository Periodes, GrilleAgendaQuery Grille,
        string AliceId, string BrunoId, string ChloeId);

    private static Contexte Monter()
    {
        var config = new ConfigurationFoyerEnMemoire();
        var aliceId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice")).Valeur!.ActeurId;
        var brunoId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Bruno")).Valeur!.ActeurId;
        var chloeId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Chloe")).Valeur!.ActeurId;
        var cycle = new CycleDeFondEnMemoire();
        cycle.DefinirCycle(new CycleDeFond(2, new Dictionary<int, string> { [0] = aliceId, [1] = brunoId }));

        var periodes = new InMemoryPeriodeRepository();
        var propositions = new InMemoryPropositionEchangeRepository();
        var journal = new InMemoryJournalChangements();
        var horloge = new HorlogeFigee(new DateTime(2026, 7, 1, 8, 30, 0));
        var grille = new GrilleAgendaQuery(
            new InMemorySlotRepository(), periodes, config, config,
            cycle, config, new InMemorySlotRecurrentRepository(), new InMemoryTransfertRepository());

        return new Contexte(
            new ProposerEchangeSuiteImprevuHandler(journal, new ProposerEchangeHandler(grille, propositions, config)),
            new AccepterPropositionHandler(propositions, new DeleguerRecuperationHandler(grille, periodes, config)),
            new SignalerImprevuHandler(journal, horloge, grille),
            propositions, periodes, grille, aliceId, brunoId, chloeId);
    }

    [Fact]
    public void Should_Last_write_wins_sans_doublon_When_re_proposition_greffee_sur_le_meme_imprevu()
    {
        var c = Monter();
        var imprevu = c.Signaler.Handle(new SignalerImprevuCommand(Mercredi_08_07_2026, LeaId, TypeImprevu.Malade, c.AliceId)).Valeur!;

        // Proposer vers Bruno puis RE-proposer vers Chloe sur le même jour/enfant (même imprévu).
        var premiere = c.Proposer.Handle(new ProposerEchangeSuiteImprevuCommand(imprevu.Id, c.BrunoId));
        Assert.True(premiere.EstSucces);
        var seconde = c.Proposer.Handle(new ProposerEchangeSuiteImprevuCommand(imprevu.Id, c.ChloeId));
        Assert.True(seconde.EstSucces);

        // Une seule proposition pending subsiste (la dernière), sans doublon.
        var pending = Assert.Single(c.Propositions.AllSnapshots().Where(p => p.Statut == StatutProposition.Proposee));
        Assert.Equal(c.ChloeId, pending.VersActeurId);
        Assert.NotEqual(premiere.Valeur!.Id, pending.Id); // l'ancienne pending a bien été retirée
        Assert.Empty(c.Periodes.AllSnapshots());          // toujours aucune surcharge (pending n'écrit rien)
    }

    [Fact]
    public void Should_Accepter_sans_crash_When_l_imprevu_est_sur_un_jour_hors_fenetre_chargee()
    {
        var c = Monter();
        // Imprévu sur un jour très lointain (hors de toute fenêtre par défaut) : la proposition greffée s'accepte
        // sans crash (la délégation s44 surcharge une DATE isolée, insensible à la fenêtre affichée). Le recevant
        // est l'acteur NON résolu ce jour-là (proposer au résolu = soi-même, refusé — indépendant de la parité ISO).
        var resolu = CaseDuJour(c.Grille, LointainHorsFenetre).ResponsableId;
        var recevant = resolu == c.AliceId ? c.BrunoId : c.AliceId;

        var imprevu = c.Signaler.Handle(new SignalerImprevuCommand(LointainHorsFenetre, LeaId, TypeImprevu.Retard, c.AliceId)).Valeur!;
        var proposition = c.Proposer.Handle(new ProposerEchangeSuiteImprevuCommand(imprevu.Id, recevant)).Valeur!;
        Assert.Equal(LointainHorsFenetre, proposition.Jour);
        Assert.Empty(c.Periodes.AllSnapshots());

        var accepte = c.Accepter.Handle(new AccepterPropositionCommand(proposition.Id));
        Assert.True(accepte.EstSucces);

        // La surcharge de la date lointaine est écrite, la case converge sur le recevant, sans crash.
        var surcharge = Assert.Single(c.Periodes.AllSnapshots());
        Assert.Equal(recevant, surcharge.ResponsableId);
        Assert.Equal(LointainHorsFenetre, DateOnly.FromDateTime(surcharge.Debut));
        Assert.Equal(recevant, CaseDuJour(c.Grille, LointainHorsFenetre).ResponsableId);
    }
}
