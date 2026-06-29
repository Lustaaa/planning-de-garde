using System;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Tests.Builders;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 16 — Scénario 5 — Idempotence : supprimer une période absente / déjà supprimée réussit (@back)
//   Étant donné le store comporte une période "P1" et une période "P2"
//   Quand je supprime une période d'identifiant "periode-inexistante"
//   Alors la suppression réussit sans effet, le store comporte toujours "P1" et "P2"
//   Quand je supprime deux fois la période "P2"
//   Alors les deux suppressions réussissent (la seconde sans effet supplémentaire), sans erreur
//
// CARACTÉRISATION (⚠️ early green ATTENDU à la frontière Application — PAS un driver). L'idempotence
// est STRUCTURELLE depuis Sc.1 : le retrait du store (RemoveAll par identifiant) est un no-op quand
// l'id est absent, et le handler renvoie un succès sans condition. Ce test verrouille la non-régression.
// Le risque réel (id non-ObjectId qui lèverait) est côté Mongo : prouvé au runtime
// (PlanningDeGarde.Api.Tests/SupprimerPeriodeIdempotenteMongoIntegrationTests).
public class Scenario5_SupprimerPeriodeIdempotente
{
    private const string ParentA = "parent-a";
    private const string Nounou = "nounou";

    [Fact]
    public void Should_reussir_sans_effet_When_on_supprime_une_periode_absente_ou_deja_supprimee()
    {
        // Given — le store comporte deux périodes P1 (Parent A) et P2 (Nounou).
        var periodes = new FakePeriodeRepository();
        var responsables = new FakeResponsableRepository().AvecResponsable(ParentA).AvecResponsable(Nounou);
        var affecter = new AffecterPeriodeHandler(periodes, responsables);
        affecter.Handle(new PeriodeBuilder().PourResponsable(ParentA).Du(new DateTime(2026, 6, 10)).Au(new DateTime(2026, 6, 10)).Build());
        affecter.Handle(new PeriodeBuilder().PourResponsable(Nounou).Du(new DateTime(2026, 6, 20)).Au(new DateTime(2026, 6, 20)).Build());
        var handler = new SupprimerPeriodeHandler(periodes);
        var idP2 = periodes.AllSnapshots().Single(p => p.ResponsableId == Nounou).Id;

        // When — je supprime une période d'identifiant inexistant.
        var suppressionAbsente = handler.Handle(new SupprimerPeriodeCommand("periode-inexistante"));

        // Then — réussit sans effet : le store comporte toujours P1 et P2.
        Assert.True(suppressionAbsente.EstSucces);
        Assert.Equal(2, periodes.AllSnapshots().Count);

        // When — je supprime la période P2 une première fois.
        var premiere = handler.Handle(new SupprimerPeriodeCommand(idP2));
        // And — je la supprime une seconde fois (déjà supprimée).
        var seconde = handler.Handle(new SupprimerPeriodeCommand(idP2));

        // Then — les deux réussissent ; la seconde est un no-op (aucun effet supplémentaire), sans erreur.
        Assert.True(premiere.EstSucces);
        Assert.True(seconde.EstSucces);
        var restantes = periodes.AllSnapshots();
        Assert.Single(restantes);
        Assert.Equal(ParentA, restantes[0].ResponsableId); // P1 demeure
    }
}
