using System;
using PlanningDeGarde.Application;
using PlanningDeGarde.Tests.Builders;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 18 — Scénario 1 — Supprimer un slot le retire du store durable relu (@back)
//   Étant donné un foyer dont le store comporte le lieu "École" et l'enfant "Léa"
//   Et un slot place "Léa" à "École" le mardi 16 juin 2026 de 08h30 à 16h30, d'identifiant stable connu
//   Quand je supprime le slot par son identifiant stable
//   Alors la suppression réussit
//   Et le slot n'est plus présent dans le store relu
//
// Boucle externe à la frontière Application (handler + port d'écriture, doublures à la main).
// La clause « + redémarrage » est prouvée au runtime sur Mongo réel
// (PlanningDeGarde.Api.Tests/SupprimerSlotMongoIntegrationTests).
public class Scenario1_SupprimerSlot
{
    private static readonly DateTime Mardi16Juin2026_0830 = new(2026, 6, 16, 8, 30, 0);
    private static readonly DateTime Mardi16Juin2026_1630 = new(2026, 6, 16, 16, 30, 0);

    [Fact]
    public void Should_retirer_le_slot_du_store_relu_et_reussir_When_on_supprime_par_son_identifiant_stable()
    {
        // Given — le store comporte un slot durable plaçant "Léa" à "École" le mardi 16/06/2026 08h30–16h30.
        var slots = new FakeSlotRepository();
        var lieux = new FakeLieuRepository().AvecLieu("ecole");
        var notificateur = new FakeNotificateurPlanning();
        new PoserSlotHandler(slots, lieux, notificateur).Handle(
            new SlotBuilder().PourEnfant("lea").DansLieu("ecole").De(Mardi16Juin2026_0830).A(Mardi16Juin2026_1630).Build());
        var stocke = Assert.Single(slots.AllSnapshots());
        var idStable = stocke.Id;
        Assert.False(string.IsNullOrEmpty(idStable), "le slot enregistré doit porter un identifiant stable.");

        // When — je supprime le slot par son identifiant stable.
        var resultat = new SupprimerSlotHandler(slots).Handle(new SupprimerSlotCommand(idStable));

        // Then — la suppression réussit et le slot n'est plus présent dans le store relu.
        Assert.True(resultat.EstSucces);
        Assert.Empty(slots.AllSnapshots());
    }
}
