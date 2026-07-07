using System;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Tests.Builders;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 18 — Scénario 5 — Idempotence : supprimer un slot absent / déjà supprimé réussit sans effet (@back)
//   Étant donné le store comporte un slot "S1" et un slot "S2"
//   Quand je supprime un slot d'identifiant "slot-inexistant"
//   Alors la suppression réussit sans effet, le store comporte toujours "S1" et "S2"
//   Quand je supprime deux fois le slot "S2"
//   Alors les deux suppressions réussissent (la seconde sans effet supplémentaire), sans erreur
//
// CARACTÉRISATION (⚠️ early green ATTENDU à la frontière Application — PAS un driver). L'idempotence
// est STRUCTURELLE depuis Sc.1 : le retrait du store (RemoveAll par identifiant) est un no-op quand
// l'id est absent, et le handler renvoie un succès sans condition. Ce test verrouille la non-régression.
// Le risque réel (id non-ObjectId qui lèverait) est côté Mongo : prouvé au runtime
// (PlanningDeGarde.Api.Tests/SupprimerSlotIdempotenteMongoIntegrationTests).
public class Scenario5_SupprimerSlotIdempotente
{
    private const string S1 = "ecole";
    private const string S2 = "nounou";

    [Fact]
    public void Should_reussir_sans_effet_When_on_supprime_un_slot_absent_ou_deja_supprime()
    {
        // Given — le store comporte deux slots S1 (école) et S2 (nounou) pour Léa.
        var slots = new FakeSlotRepository();
        var lieux = new FakeReferentielLieux().AvecLieu(S1).AvecLieu(S2);
        var poser = new PoserSlotHandler(slots, lieux, new FakeNotificateurPlanning());
        poser.Handle(new SlotBuilder().PourEnfant("lea").DansLieu(S1)
            .De(new DateTime(2026, 6, 10, 8, 0, 0)).A(new DateTime(2026, 6, 10, 12, 0, 0)).Build());
        poser.Handle(new SlotBuilder().PourEnfant("lea").DansLieu(S2)
            .De(new DateTime(2026, 6, 20, 8, 0, 0)).A(new DateTime(2026, 6, 20, 12, 0, 0)).Build());
        var handler = new SupprimerSlotHandler(slots);
        var idS2 = slots.AllSnapshots().Single(s => s.LieuId == S2).Id;

        // When — je supprime un slot d'identifiant inexistant.
        var suppressionAbsente = handler.Handle(new SupprimerSlotCommand("slot-inexistant"));

        // Then — réussit sans effet : le store comporte toujours S1 et S2.
        Assert.True(suppressionAbsente.EstSucces);
        Assert.Equal(2, slots.AllSnapshots().Count);

        // When — je supprime le slot S2 une première fois, puis une seconde (déjà supprimé).
        var premiere = handler.Handle(new SupprimerSlotCommand(idS2));
        var seconde = handler.Handle(new SupprimerSlotCommand(idS2));

        // Then — les deux réussissent ; la seconde est un no-op (aucun effet supplémentaire), sans erreur.
        Assert.True(premiere.EstSucces);
        Assert.True(seconde.EstSucces);
        var restants = slots.AllSnapshots();
        Assert.Single(restants);
        Assert.Equal(S1, restants[0].LieuId); // S1 demeure
    }
}
