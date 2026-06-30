using System;
using PlanningDeGarde.Application;
using PlanningDeGarde.Tests.Builders;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 17 — Scénario 5 — Bornes invalides (fin antérieure au début) refusées, période inchangée (@back)
//   Étant donné une période durable du lundi 15 au mercredi 17 juin 2026 à "Nounou", d'identifiant stable connu
//   Quand je tente de re-borner cette période pour qu'elle finisse avant son début (fin = dimanche 14 juin 2026)
//   Alors l'édition est refusée avec un message clair sur les bornes
//   Et le store relu comporte toujours la période d'origine du lundi 15 au mercredi 17 juin 2026
//
// VRAI DRIVER : pilote la première branche d'échec du handler EditerPeriode. L'invariant « la fin ne
// précède pas le début » appartient à l'agrégat PeriodeDeGarde (Tell-Don't-Ask) ; le handler le consulte
// AVANT toute écriture → rejet propre, port d'écriture jamais touché (rien appliqué).
public class Scenario5_EditerPeriodeBornesInvalides
{
    private const string Nounou = "nounou";
    private static readonly DateTime Dimanche14 = new(2026, 6, 14, 0, 0, 0);
    private static readonly DateTime Lundi15 = new(2026, 6, 15, 0, 0, 0);
    private static readonly DateTime Mercredi17 = new(2026, 6, 17, 0, 0, 0);

    [Fact]
    public void Should_refuser_l_edition_et_laisser_la_periode_inchangee_When_la_fin_est_anterieure_au_debut()
    {
        // Given — période durable du lundi 15 → mercredi 17 juin 2026 affectée à "Nounou".
        var periodes = new FakePeriodeRepository();
        var responsables = new FakeResponsableRepository().AvecResponsable("parent-a").AvecResponsable(Nounou);
        new AffecterPeriodeHandler(periodes, responsables)
            .Handle(new PeriodeBuilder().PourResponsable(Nounou).Du(Lundi15).Au(Mercredi17).Build());
        var etatObserve = Assert.Single(periodes.AllSnapshots());

        // When — je tente de re-borner pour que la fin (dimanche 14) précède le début (lundi 15).
        var resultat = new EditerPeriodeHandler(periodes)
            .Handle(new EditerPeriodeCommand(etatObserve, etatObserve.ResponsableId, Lundi15, Dimanche14));

        // Then — l'édition est refusée avec un message clair sur les bornes.
        Assert.False(resultat.EstSucces);
        Assert.Contains("born", resultat.Motif, StringComparison.OrdinalIgnoreCase);

        // And — le store relu comporte toujours la période d'origine (lundi 15 → mercredi 17), inchangée.
        var relue = Assert.Single(periodes.AllSnapshots());
        Assert.Equal(Lundi15, relue.Debut);
        Assert.Equal(Mercredi17, relue.Fin);
        Assert.Equal(Nounou, relue.ResponsableId);
    }
}
