using System;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Tests.Builders;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 16 — Scénario 4 — Lister les périodes couvrant une date alimente la dialog (@back)
//   Étant donné le store comporte une période "Nounou" du lundi 15 au mercredi 17 juin 2026
//   Et une période "Parent A" le mardi 16 juin 2026
//   Et aucune période ne couvre le jeudi 18 juin 2026
//   Quand je liste les périodes couvrant le mardi 16 juin 2026
//   Alors la liste comporte les deux périodes, chacune avec son identifiant stable, ses bornes et son responsable
//   Et la liste des périodes couvrant le jeudi 18 juin 2026 est vide
//
// Lecture neuve (canal lecture) : PeriodesDuJourQuery. Frontière Application (port d'écriture pour
// le Given via AffecterPeriodeHandler, query en lecture). Ne déclenche jamais la diffusion.
public class Scenario4_PeriodesDuJourQuery
{
    private const string ParentA = "parent-a";
    private const string Nounou = "nounou";

    private static readonly DateTime Lundi15 = new(2026, 6, 15, 0, 0, 0);
    private static readonly DateTime Mardi16 = new(2026, 6, 16, 0, 0, 0);
    private static readonly DateTime Mercredi17 = new(2026, 6, 17, 0, 0, 0);
    private static readonly DateOnly Mardi16Juin2026 = new(2026, 6, 16);
    private static readonly DateOnly Jeudi18Juin2026 = new(2026, 6, 18);

    [Fact]
    public void Should_lister_les_periodes_couvrant_la_date_avec_id_bornes_et_responsable_et_vide_hors_couverture()
    {
        // Given — deux périodes couvrant le mardi 16/06/2026, aucune le jeudi 18/06/2026.
        var periodes = new FakePeriodeRepository();
        var responsables = new FakeResponsableRepository().AvecResponsable(ParentA).AvecResponsable(Nounou);
        var affecter = new AffecterPeriodeHandler(periodes, responsables);
        affecter.Handle(new PeriodeBuilder().PourResponsable(Nounou).Du(Lundi15).Au(Mercredi17).Build());
        affecter.Handle(new PeriodeBuilder().PourResponsable(ParentA).Du(Mardi16).Au(Mardi16).Build());
        var query = new PeriodesDuJourQuery(periodes);

        // When — je liste les périodes couvrant le mardi 16/06/2026.
        var couvrantes = query.Lister(Mardi16Juin2026);

        // Then — les deux périodes, chacune avec identifiant stable non vide, bornes et responsable.
        Assert.Equal(2, couvrantes.Count);
        Assert.All(couvrantes, p => Assert.False(string.IsNullOrEmpty(p.Id), "chaque période listée porte son identifiant stable."));

        var nounou = Assert.Single(couvrantes, p => p.ResponsableId == Nounou);
        Assert.Equal(Lundi15, nounou.Debut);
        Assert.Equal(Mercredi17, nounou.Fin);

        var parentA = Assert.Single(couvrantes, p => p.ResponsableId == ParentA);
        Assert.Equal(Mardi16, parentA.Debut);
        Assert.Equal(Mardi16, parentA.Fin);

        // And — aucune période ne couvre le jeudi 18/06/2026.
        Assert.Empty(query.Lister(Jeudi18Juin2026));
    }
}
