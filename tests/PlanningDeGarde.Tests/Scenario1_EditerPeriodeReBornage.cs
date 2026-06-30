using System;
using PlanningDeGarde.Application;
using PlanningDeGarde.Tests.Builders;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 17 — Scénario 1 — Re-borner une période la met à jour dans le store durable relu (@back)
//   Étant donné un foyer dont le store comporte les acteurs "Parent A" et "Nounou"
//   Et une période durable attribue du lundi 15 au mercredi 17 juin 2026 la garde à "Nounou", d'identifiant stable connu
//   Quand je re-borne cette période par son identifiant stable pour qu'elle couvre du mardi 16 au mercredi 17 juin 2026
//   Alors l'édition réussit
//   Et le store relu comporte la période avec les bornes mardi 16 → mercredi 17 juin 2026
//   Et le lundi 15 juin 2026 n'est plus couvert par cette période
//
// Boucle externe à la frontière Application (handler EditerPeriode + port d'écriture, doublures à la main) ;
// clé = identifiant stable porté par le snapshot observé (PeriodeSnapshot.Id). La clause « + redémarrage » est
// prouvée au runtime sur Mongo réel (PlanningDeGarde.Api.Tests/EditerPeriodeMongoIntegrationTests).
public class Scenario1_EditerPeriodeReBornage
{
    private static readonly DateTime Lundi15Juin2026 = new(2026, 6, 15, 0, 0, 0);
    private static readonly DateTime Mardi16Juin2026 = new(2026, 6, 16, 0, 0, 0);
    private static readonly DateTime Mercredi17Juin2026 = new(2026, 6, 17, 0, 0, 0);

    [Fact]
    public void Should_mettre_a_jour_les_bornes_dans_le_store_relu_When_on_re_borne_la_periode_par_son_identifiant_stable()
    {
        // Given — une période durable du lundi 15 → mercredi 17 juin 2026 affectée à "Nounou".
        var periodes = new FakePeriodeRepository();
        var responsables = new FakeResponsableRepository().AvecResponsable("parent-a").AvecResponsable("nounou");
        new AffecterPeriodeHandler(periodes, responsables)
            .Handle(new PeriodeBuilder().PourResponsable("nounou").Du(Lundi15Juin2026).Au(Mercredi17Juin2026).Build());
        var etatObserve = Assert.Single(periodes.AllSnapshots());
        Assert.False(string.IsNullOrEmpty(etatObserve.Id), "la période enregistrée doit porter un identifiant stable.");

        // When — je re-borne la période (par son identifiant stable) pour qu'elle couvre mardi 16 → mercredi 17.
        var resultat = new EditerPeriodeHandler(periodes)
            .Handle(new EditerPeriodeCommand(etatObserve, etatObserve.ResponsableId, Mardi16Juin2026, Mercredi17Juin2026));

        // Then — l'édition réussit.
        Assert.True(resultat.EstSucces);

        // And — le store relu comporte la période avec les nouvelles bornes mardi 16 → mercredi 17.
        var relue = Assert.Single(periodes.AllSnapshots());
        Assert.Equal(Mardi16Juin2026, relue.Debut);
        Assert.Equal(Mercredi17Juin2026, relue.Fin);

        // And — le lundi 15 juin 2026 n'est plus couvert par cette période (début re-borné après lundi 15).
        Assert.True(relue.Debut > Lundi15Juin2026, "le lundi 15 ne doit plus être couvert par la période re-bornée.");
    }
}
