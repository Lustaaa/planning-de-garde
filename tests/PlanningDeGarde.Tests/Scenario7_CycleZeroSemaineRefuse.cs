using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 10 — Sc.7 — Définir un cycle de zéro semaine est refusé (@erreur, 🖥️ IHM)
//   Tranche BACKEND (tdd-auto) : 1 DRIVER (garde conditionnelle N ≥ 1 sur DefinirCycleHandler) + 1
//   CARACTÉRISATION (cycle précédent inchangé sur refus, ⚠️ early green attendu). Le handler, exercé
//   en SUCCÈS dès Sc.1 (N=2), accepte aujourd'hui TOUT N — y compris 0 (insensé : ISOWeek mod 0 n'est
//   pas défini). Le driver force la garde N ≥ 1 qui refuse via Result.Echec porteur du motif MÉTIER
//   « le cycle doit compter au moins une semaine », sans aucune écriture ni diffusion. CONDITIONNELLE :
//   le nominal N=2 (Sc.1) reste accepté → la garde ne vise que N < 1.
//
//   L'acceptation RUNTIME IHM (message à l'écran, cycle précédent conservé à l'affichage de la grille)
//   est menée séparément par ihm-builder sur l'app câblée. On NE teste PAS ici un rendu Blazor.
//
//   Données : foyer avec un cycle N=2 déjà défini (pair → parent-a/Alice/bleu, impair →
//   parent-b/Bruno/orange). Un parent tente d'enregistrer un cycle de ZÉRO semaine (N=0).
public class Scenario7_CycleZeroSemaineRefuse
{
    private const string ParentA = "parent-a";
    private const string ParentB = "parent-b";
    private const string Alice = "Alice";
    private const string Bruno = "Bruno";
    private const string Bleu = "bleu";
    private const string Orange = "orange";
    private const string MotifAttendu = "le cycle doit compter au moins une semaine";

    private static readonly DateOnly Lundi_29_06_2026 = new(2026, 6, 29);   // ISO 27 (impaire)
    private static readonly DateOnly Dimanche_05_07_2026 = new(2026, 7, 5);
    private static readonly DateOnly Lundi_06_07_2026 = new(2026, 7, 6);    // ISO 28 (paire)
    private static readonly DateOnly Dimanche_12_07_2026 = new(2026, 7, 12);

    private static Dictionary<int, string> MappingPairAImpairB()
        => new() { [0] = ParentA, [1] = ParentB };

    // ---------- Acceptation (boucle externe, frontière Application) ----------
    // Traduit le Gherkin sans IHM : sur un store portant déjà un cycle N=2, DefinirCycleCommand(0)
    // renvoie un Result.Echec porteur du motif clair, aucune diffusion n'est émise, et la grille
    // continue de résoudre l'alternance A/B d'origine (cycle précédent inchangé).
    [Fact]
    public void Acceptation_Should_Refuser_le_cycle_de_zero_semaine_avec_motif_clair_sans_ecraser_le_cycle_de_deux_semaines_When_un_parent_tente_d_enregistrer_zero_semaine()
    {
        var foyer = new ConfigurationFoyerEnMemoire();                 // parent-a=Alice/bleu, parent-b=Bruno/orange
        var cycleStore = new FakeReferentielCycleDeFond(new CycleDeFond(2, MappingPairAImpairB()));
        var spy = new FakeNotificateurPlanning();
        var handler = new DefinirCycleHandler(cycleStore, spy);

        var resultat = handler.Handle(new DefinirCycleCommand(0, new Dictionary<int, string>()));

        Assert.False(resultat.EstSucces);
        Assert.Equal(MotifAttendu, resultat.Motif);
        Assert.Equal(0, spy.NombreDeNotifications);                    // pas de diffusion sur refus

        // Le cycle de 2 semaines reste inchangé : la grille résout encore l'alternance A/B d'origine.
        var grille = QueryAvec(cycleStore, foyer).Projeter(Lundi_29_06_2026);
        Assert.All(JoursEntre(grille, Lundi_29_06_2026, Dimanche_05_07_2026), j => // ISO 27 impaire → Bruno
        {
            Assert.Equal(Bruno, j.NomResponsable);
            Assert.Equal(Orange, j.CouleurResponsable);
        });
        Assert.All(JoursEntre(grille, Lundi_06_07_2026, Dimanche_12_07_2026), j => // ISO 28 paire → Alice
        {
            Assert.Equal(Alice, j.NomResponsable);
            Assert.Equal(Bleu, j.CouleurResponsable);
        });
    }

    // ---------- Tests unitaires (boucle interne, TDD) ----------

    // Test #1 — DRIVER : le handler accepte aujourd'hui tout N (exercé en succès dès Sc.1). Un cycle de
    // zéro semaine doit être REFUSÉ via Result.Echec porteur du motif métier, sans diffusion. Force la
    // garde conditionnelle N ≥ 1 (ne vise que N < 1, laisse le nominal N=2 passer).
    [Fact]
    public void Should_Refuser_la_definition_du_cycle_avec_le_message_que_le_cycle_doit_compter_au_moins_une_semaine_When_un_parent_tente_d_enregistrer_un_cycle_de_zero_semaine()
    {
        var spy = new FakeNotificateurPlanning();
        var handler = new DefinirCycleHandler(
            new FakeReferentielCycleDeFond(new CycleDeFond(2, MappingPairAImpairB())), spy);

        var resultat = handler.Handle(new DefinirCycleCommand(0, new Dictionary<int, string>()));

        Assert.False(resultat.EstSucces);
        Assert.Equal(MotifAttendu, resultat.Motif);
        Assert.Equal(0, spy.NombreDeNotifications); // motif métier, pas de diffusion sur refus
    }

    // Test #2 — CARACTÉRISATION (⚠️ early green attendu — couvert par #1 : le refus retourne AVANT toute
    // écriture). Le cycle N=2 d'origine reste intact dans le store : il résout encore Parent A sur l'index
    // pair (ISO 28) et Parent B sur l'index impair (ISO 27). Filet « aucun effet de bord sur refus ».
    [Fact]
    public void Should_Laisser_le_cycle_precedent_inchange_When_la_definition_d_un_cycle_de_zero_semaine_est_refusee()
    {
        var cycleStore = new FakeReferentielCycleDeFond(new CycleDeFond(2, MappingPairAImpairB()));
        var handler = new DefinirCycleHandler(cycleStore, new FakeNotificateurPlanning());

        handler.Handle(new DefinirCycleCommand(0, new Dictionary<int, string>())); // refusé

        var cycleCourant = cycleStore.CycleCourant();
        Assert.NotNull(cycleCourant);
        Assert.Equal(2, cycleCourant!.NombreSemaines);
        Assert.Equal(ParentA, cycleCourant.ResponsableDeFond(Lundi_06_07_2026)); // ISO 28 paire
        Assert.Equal(ParentB, cycleCourant.ResponsableDeFond(Lundi_29_06_2026)); // ISO 27 impaire
    }

    // ---------- Helpers ----------

    private static GrilleAgendaQuery QueryAvec(IReferentielCycleDeFond cycle, ConfigurationFoyerEnMemoire foyer)
        => new(new FakeSlotRepository(), new FakePeriodeRepository(), foyer, foyer, cycle);

    private static IEnumerable<JourCase> JoursEntre(GrilleAgenda grille, DateOnly debut, DateOnly fin)
        => grille.Jours.Where(j => j.Date >= debut && j.Date <= fin);
}
