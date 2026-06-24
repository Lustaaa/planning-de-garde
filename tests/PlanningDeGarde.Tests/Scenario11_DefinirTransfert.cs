using System;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Tests.Builders;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Scénario 11 — Définir le transfert de bascule entre deux parents (@nominal)
//   Given une période « Parent A responsable jusqu'au lundi 21/07 » existe
//   And une période « Parent B responsable à partir du lundi 21/07 » existe
//   When le Parent définit le transfert du lundi 21/07 : Parent A dépose à l'école à 8h30, Parent B récupère
//   Then le planning affiche le transfert « dépose Parent A → récupère Parent B, école, 8h30 le 21/07 »
//   And la responsabilité bascule de Parent A à Parent B à ce transfert
public class Scenario11_DefinirTransfert
{
    private static readonly DateTime Transfert21 = new(2025, 7, 21, 0, 0, 0);
    private static readonly TimeSpan Heure830 = new(8, 30, 0);

    private static DefinirTransfertHandler Handler(out FakeTransfertRepository transferts)
    {
        transferts = new FakeTransfertRepository();
        return new DefinirTransfertHandler(transferts);
    }

    private static FakePeriodeRepository DeuxPeriodesContigues()
    {
        var periodes = new FakePeriodeRepository();
        var responsables = new FakeResponsableRepository().AvecResponsable("parent-a").AvecResponsable("parent-b");
        var affecter = new AffecterPeriodeHandler(periodes, responsables);
        affecter.Handle(new PeriodeBuilder().PourResponsable("parent-a").Du(new DateTime(2025, 7, 14)).Au(Transfert21).Build());
        affecter.Handle(new PeriodeBuilder().PourResponsable("parent-b").Du(Transfert21).Au(new DateTime(2025, 7, 28)).Build());
        return periodes;
    }

    // ---------- Test d'acceptation (boucle externe, BDD) ----------

    [Fact]
    public void Should_afficher_le_transfert_dans_le_planning_partage_et_faire_basculer_la_responsabilite_au_point_de_transfert_When_un_Parent_definit_un_transfert_complet_entre_deux_periodes_contigues()
    {
        // Given — deux périodes contiguës A puis B autour du 21/07
        var periodes = DeuxPeriodesContigues();
        var handler = Handler(out var transferts);

        // When — le Parent définit le transfert complet du 21/07
        var resultat = handler.Handle(new TransfertBuilder()
            .DeposePar("parent-a").RecuperePar("parent-b").AuLieu("ecole").ALHeure(Heure830).LeJour(Transfert21).Build());

        // Then — le planning affiche le transfert
        Assert.True(resultat.EstSucces);
        var transfert = Assert.Single(transferts.AllSnapshots());
        Assert.Equal("parent-a", transfert.DeposeParId);
        Assert.Equal("parent-b", transfert.RecupereParId);
        Assert.Equal("ecole", transfert.LieuId);
        Assert.Equal(Heure830, transfert.Heure);
        Assert.Equal(Transfert21, transfert.Date);

        // And — la responsabilité bascule de A à B au point de transfert
        var responsabilite = new ResponsabiliteQuery(periodes);
        Assert.Equal("parent-a", responsabilite.ResponsableAu(Transfert21.AddDays(-1)));
        Assert.Equal("parent-b", responsabilite.ResponsableAu(Transfert21));
    }

    // ---------- Tests unitaires (boucle interne, TDD) ----------

    // Test #1 — baseline du nouvel agrégat transfert : une définition complète est confirmée
    [Fact]
    public void Should_confirmer_la_definition_du_transfert_When_un_Parent_definit_un_transfert_complet()
    {
        var handler = Handler(out _);
        var commande = new TransfertBuilder().Build();

        var resultat = handler.Handle(commande);

        Assert.True(resultat.EstSucces);
    }

    // Test #2 — le transfert défini expose déposant/récupérant/lieu/heure/date fournis (snapshot)
    [Fact]
    public void Should_exposer_le_deposant_le_recuperant_le_lieu_l_heure_et_la_date_du_transfert_When_le_transfert_a_ete_defini()
    {
        var handler = Handler(out _);
        var commande = new TransfertBuilder()
            .DeposePar("parent-a").RecuperePar("parent-b").AuLieu("ecole").ALHeure(Heure830).LeJour(Transfert21).Build();

        var resultat = handler.Handle(commande);

        Assert.True(resultat.EstSucces);
        var transfert = resultat.Valeur!;
        Assert.Equal("parent-a", transfert.DeposeParId);
        Assert.Equal("parent-b", transfert.RecupereParId);
        Assert.Equal("ecole", transfert.LieuId);
        Assert.Equal(Heure830, transfert.Heure);
        Assert.Equal(Transfert21, transfert.Date);
    }

    // Test #3 — le transfert persisté apparaît dans le planning partagé (fake repository)
    [Fact]
    public void Should_afficher_le_transfert_dans_le_planning_partage_du_foyer_When_le_transfert_a_ete_defini()
    {
        var handler = Handler(out var transferts);
        var commande = new TransfertBuilder().DeposePar("parent-a").RecuperePar("parent-b").LeJour(Transfert21).Build();

        handler.Handle(commande);

        var transfert = Assert.Single(transferts.AllSnapshots());
        Assert.Equal("parent-a", transfert.DeposeParId);
        Assert.Equal("parent-b", transfert.RecupereParId);
        Assert.Equal(Transfert21, transfert.Date);
    }

    // Test #4 — au point de transfert la responsabilité bascule du déposant au récupérant
    // (le transfert borne deux périodes contiguës : avant → déposant, à/après → récupérant)
    [Fact]
    public void Should_faire_basculer_la_responsabilite_du_deposant_au_recuperant_au_point_de_transfert_When_le_transfert_borne_deux_periodes_contigues()
    {
        var periodes = DeuxPeriodesContigues();
        var handler = Handler(out _);
        handler.Handle(new TransfertBuilder()
            .DeposePar("parent-a").RecuperePar("parent-b").LeJour(Transfert21).Build());

        var responsabilite = new ResponsabiliteQuery(periodes);

        // Avant le transfert : le déposant (Parent A) est responsable.
        Assert.Equal("parent-a", responsabilite.ResponsableAu(Transfert21.AddDays(-1)));
        // À partir du transfert : le récupérant (Parent B) est responsable (bascule).
        Assert.Equal("parent-b", responsabilite.ResponsableAu(Transfert21));
    }
}
