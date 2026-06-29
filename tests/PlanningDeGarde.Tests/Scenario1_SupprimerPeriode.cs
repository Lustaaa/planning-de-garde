using System;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Tests.Builders;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 16 — Scénario 1 — Supprimer une période la retire du store durable relu (@back)
//   Étant donné un foyer dont le store comporte les acteurs "Parent A" et "Nounou"
//   Et une période attribue le mardi 16 juin 2026 à "Nounou", d'identifiant stable connu
//   Quand je supprime la période par son identifiant stable
//   Alors la suppression réussit
//   Et la période n'est plus présente dans le store relu
//
// Boucle externe à la frontière Application (handler + port d'écriture, doublures à la main).
// La clause « + redémarrage » est prouvée au runtime sur Mongo réel
// (PlanningDeGarde.Api.Tests/SupprimerPeriodeMongoIntegrationTests).
public class Scenario1_SupprimerPeriode
{
    private static readonly DateTime Mardi16Juin2026 = new(2026, 6, 16, 0, 0, 0);

    [Fact]
    public void Should_retirer_la_periode_du_store_relu_et_reussir_When_on_supprime_par_son_identifiant_stable()
    {
        // Given — le store comporte une période durable du mardi 16/06/2026 affectée à "Nounou".
        var periodes = new FakePeriodeRepository();
        var responsables = new FakeResponsableRepository().AvecResponsable("parent-a").AvecResponsable("nounou");
        new AffecterPeriodeHandler(periodes, responsables)
            .Handle(new PeriodeBuilder().PourResponsable("nounou").Du(Mardi16Juin2026).Au(Mardi16Juin2026).Build());
        var stockee = Assert.Single(periodes.AllSnapshots());
        var idStable = stockee.Id;
        Assert.False(string.IsNullOrEmpty(idStable), "la période enregistrée doit porter un identifiant stable.");

        // When — je supprime la période par son identifiant stable.
        var resultat = new SupprimerPeriodeHandler(periodes).Handle(new SupprimerPeriodeCommand(idStable));

        // Then — la suppression réussit et la période n'est plus présente dans le store relu.
        Assert.True(resultat.EstSucces);
        Assert.Empty(periodes.AllSnapshots());
    }
}
