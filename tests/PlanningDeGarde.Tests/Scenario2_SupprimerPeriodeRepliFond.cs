using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Tests.Builders;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 16 — Scénario 2 — Repli fond : la case retombe sur le responsable de fond (@back)
//   Étant donné un cycle de fond N=2 mappant index 0 ET 1 sur "Parent A"
//   Et une période durable attribue le mardi 16 juin 2026 à "Nounou" (surcharge sur le fond "Parent A")
//   Et la case du mardi 16 juin 2026 affiche "Nounou"
//   Quand je supprime la période du mardi 16 juin 2026
//   Alors la suppression réussit
//   Et la case du mardi 16 juin 2026 retombe sur le responsable de fond "Parent A" et sa couleur
//
// CARACTÉRISATION (⚠️ early green ATTENDU, garde-fou de non-régression — PAS un driver).
// La priorité surcharge > fond > neutre est STRUCTURELLE (GrilleAgendaQuery, acquise palier 6) :
// la suppression (Sc.1) retire la surcharge du store ; la re-projection relit periodes.AllSnapshots()
// sans elle, donc la case se re-résout sur le fond sans aucun code neuf. Ce test verrouille la
// COMPOSITION suppression → repli fond ; il ne pilote aucun rouge.
public class Scenario2_SupprimerPeriodeRepliFond
{
    private const string ParentA = "parent-a";
    private const string Nounou = "nounou";
    private const string NomParentA = "Parent A";
    private const string NomNounou = "Nounou";
    private const string Bleu = "bleu";
    private const string Vert = "vert";

    private static readonly DateOnly Mardi16Juin2026 = new(2026, 6, 16);
    private static readonly DateTime Mardi16 = new(2026, 6, 16, 0, 0, 0);

    [Fact]
    public void Should_faire_retomber_la_case_sur_le_responsable_de_fond_When_on_supprime_la_periode_de_surcharge()
    {
        // Given — cycle de fond N=2 (index 0 ET 1 → Parent A) ; surcharge "Nounou" le mardi 16/06/2026.
        var periodes = new FakePeriodeRepository();
        var responsables = new FakeResponsableRepository().AvecResponsable(ParentA).AvecResponsable(Nounou);
        new AffecterPeriodeHandler(periodes, responsables)
            .Handle(new PeriodeBuilder().PourResponsable(Nounou).Du(Mardi16).Au(Mardi16).Build());
        var query = QueryAvec(periodes);

        // And — la case du mardi 16/06/2026 affiche bien "Nounou" (la surcharge prime sur le fond).
        var avant = query.Projeter(Mardi16Juin2026).Jours.Single(j => j.Date == Mardi16Juin2026);
        Assert.Equal(NomNounou, avant.NomResponsable);

        // When — je supprime la période du mardi 16/06/2026 par son identifiant stable.
        var idStable = Assert.Single(periodes.AllSnapshots()).Id;
        var resultat = new SupprimerPeriodeHandler(periodes).Handle(new SupprimerPeriodeCommand(idStable));

        // Then — la suppression réussit et la case retombe sur le fond "Parent A" (nom + couleur).
        Assert.True(resultat.EstSucces);
        var apres = query.Projeter(Mardi16Juin2026).Jours.Single(j => j.Date == Mardi16Juin2026);
        Assert.Equal(NomParentA, apres.NomResponsable);
        Assert.Equal(Bleu, apres.CouleurResponsable);
    }

    private static GrilleAgendaQuery QueryAvec(IPeriodeRepository periodes)
        => new(new FakeSlotRepository(), periodes,
            new FakePaletteCouleurs(new Dictionary<string, string> { [ParentA] = Bleu, [Nounou] = Vert }),
            new FakeReferentielResponsables(new Dictionary<string, string> { [ParentA] = NomParentA, [Nounou] = NomNounou }),
            new FakeReferentielCycleDeFond(new CycleDeFond(2, new Dictionary<int, string> { [0] = ParentA, [1] = ParentA })));
}
