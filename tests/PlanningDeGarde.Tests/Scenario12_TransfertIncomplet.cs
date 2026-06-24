using System;
using PlanningDeGarde.Application;
using PlanningDeGarde.Tests.Builders;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Scénario 12 — Transfert incomplet refusé (@erreur)
//   Given une période « Parent A responsable jusqu'au lundi 21/07 » existe
//   When le Parent définit le transfert du 21/07 avec dépose par Parent A à l'école,
//        sans préciser qui récupère ni à quelle heure
//   Then la définition du transfert est refusée car la récupération et l'heure sont requises
//   And aucun transfert « du 21/07 » n'apparaît dans le planning partagé
//
// Invariant de complétude porté par l'agrégat Transfert (un seul @erreur couvre
// récupérant + heure manquants — même comportement de refus, données groupées).
public class Scenario12_TransfertIncomplet
{
    private static readonly DateTime Transfert21 = new(2025, 7, 21, 0, 0, 0);

    private static DefinirTransfertHandler Handler(out FakeTransfertRepository transferts)
    {
        transferts = new FakeTransfertRepository();
        return new DefinirTransfertHandler(transferts);
    }

    // Transfert incomplet : récupérant absent et heure non renseignée.
    private static DefinirTransfertCommand TransfertIncomplet()
        => new TransfertBuilder()
            .DeposePar("parent-a").RecuperePar("").AuLieu("ecole").ALHeure(TimeSpan.Zero).LeJour(Transfert21)
            .Build();

    // ---------- Test d'acceptation (boucle externe, BDD) ----------

    [Fact]
    public void Should_refuser_la_definition_car_la_recuperation_et_l_heure_sont_requises_et_n_inscrire_aucun_transfert_When_un_Parent_definit_un_transfert_sans_recuperant_ni_heure()
    {
        // Given / When
        var handler = Handler(out var transferts);
        var resultat = handler.Handle(TransfertIncomplet());

        // Then — la définition est refusée car récupération + heure requises
        Assert.False(resultat.EstSucces);

        // And — aucun transfert n'apparaît dans le planning partagé
        Assert.Empty(transferts.AllSnapshots());
    }

    // ---------- Tests unitaires (boucle interne, TDD) ----------

    // Test #1 — la définition est refusée quand le récupérant et l'heure sont absents
    // (garde de complétude conditionnelle dès le départ : Sc.11 nominal est vert)
    [Fact]
    public void Should_refuser_la_definition_au_motif_de_recuperation_et_heure_requises_When_le_recuperant_et_l_heure_du_transfert_sont_absents()
    {
        var handler = Handler(out _);

        var resultat = handler.Handle(TransfertIncomplet());

        Assert.False(resultat.EstSucces);
    }

    // Test #2 — un refus pour transfert incomplet ne produit aucun effet de bord
    // (aucun transfert inscrit dans le planning partagé)
    [Fact]
    public void Should_n_inscrire_aucun_transfert_dans_le_planning_partage_When_la_definition_est_refusee_pour_transfert_incomplet()
    {
        var handler = Handler(out var transferts);

        handler.Handle(TransfertIncomplet());

        Assert.Empty(transferts.AllSnapshots());
    }
}
