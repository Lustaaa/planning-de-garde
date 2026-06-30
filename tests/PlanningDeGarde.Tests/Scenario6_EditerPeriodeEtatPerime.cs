using System;
using PlanningDeGarde.Application;
using PlanningDeGarde.Tests.Builders;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 17 — Scénario 6 — Édition concurrente sur état périmé → rejet, rien appliqué (@back)
//   Étant donné une période durable du mardi 16 juin 2026 à "Nounou", à l'état (version) connu
//   Et une première édition réaffecte cette période à "Parent A" et aboutit
//   Quand une seconde édition tente de re-borner la même période en se fondant sur l'état initial désormais périmé
//   Alors la seconde édition est rejetée pour état périmé
//   Et le store relu reflète uniquement la première édition (responsable "Parent A")
//   Et la seconde édition n'a rien appliqué
//
// VRAI DRIVER : pilote la branche d'échec sur le rejet d'écriture périmée. Décision CP : REJET sur état
// périmé (et non last-write-wins), en RÉUTILISANT le contrôle de concurrence optimiste de l'agrégat
// période (IPeriodeRepository.Modifier renvoie false si la base observée n'est plus l'état courant, Sc.10 s01).
public class Scenario6_EditerPeriodeEtatPerime
{
    private const string ParentA = "parent-a";
    private const string Nounou = "nounou";
    private static readonly DateTime Mardi16 = new(2026, 6, 16, 0, 0, 0);
    private static readonly DateTime Mercredi17 = new(2026, 6, 17, 0, 0, 0);

    [Fact]
    public void Should_rejeter_la_seconde_edition_pour_etat_perime_et_ne_rien_appliquer_When_elle_se_fonde_sur_un_etat_devance()
    {
        // Given — période durable du mardi 16/06/2026 affectée à "Nounou" ; état initial observé (version).
        var periodes = new FakePeriodeRepository();
        var responsables = new FakeResponsableRepository().AvecResponsable(ParentA).AvecResponsable(Nounou);
        new AffecterPeriodeHandler(periodes, responsables)
            .Handle(new PeriodeBuilder().PourResponsable(Nounou).Du(Mardi16).Au(Mardi16).Build());
        var handler = new EditerPeriodeHandler(periodes);
        var etatInitial = Assert.Single(periodes.AllSnapshots());

        // And — une première édition réaffecte à "Parent A" (devance la version) et aboutit.
        var premiere = handler.Handle(new EditerPeriodeCommand(etatInitial, ParentA, Mardi16, Mardi16));
        Assert.True(premiere.EstSucces);

        // When — une seconde édition, fondée sur l'état initial désormais périmé, tente de re-borner.
        var seconde = handler.Handle(new EditerPeriodeCommand(etatInitial, etatInitial.ResponsableId, Mardi16, Mercredi17));

        // Then — la seconde édition est rejetée pour état périmé.
        Assert.False(seconde.EstSucces);
        Assert.Contains("périmé", seconde.Motif, StringComparison.OrdinalIgnoreCase);

        // And — le store relu reflète UNIQUEMENT la première édition (Parent A, bornes inchangées) :
        // la seconde n'a rien appliqué (re-bornage au mercredi 17 absent).
        var relue = Assert.Single(periodes.AllSnapshots());
        Assert.Equal(ParentA, relue.ResponsableId);
        Assert.Equal(Mardi16, relue.Debut);
        Assert.Equal(Mardi16, relue.Fin);
    }
}
