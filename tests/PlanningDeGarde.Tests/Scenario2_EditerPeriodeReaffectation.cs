using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Tests.Builders;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 17 — Scénario 2 — Réaffecter le responsable : la case affiche le nouveau, plus l'ancien (@back)
//   Étant donné un foyer dont le store comporte les acteurs "Parent A" et "Nounou"
//   Et une période durable attribue le mardi 16 juin 2026 la garde à "Nounou", d'identifiant stable connu
//   Et la case du mardi 16 juin 2026 affiche "Nounou"
//   Quand je réaffecte cette période à "Parent A" par son identifiant stable
//   Alors l'édition réussit
//   Et la case du mardi 16 juin 2026 affiche "Parent A" et sa couleur
//   Et la case du mardi 16 juin 2026 n'affiche plus "Nounou"
//
// Boucle externe à la frontière Application : le handler EditerPeriode réaffecte le responsable de la
// période existante (clé = identifiant stable de l'état observé) ; la case se re-résout via GrilleAgendaQuery
// (read model) sans logique de résolution neuve.
public class Scenario2_EditerPeriodeReaffectation
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
    public void Should_faire_afficher_le_nouveau_responsable_dans_la_case_et_plus_l_ancien_When_on_reaffecte_la_periode()
    {
        // Given — période durable du mardi 16/06/2026 affectée à "Nounou".
        var periodes = new FakePeriodeRepository();
        var responsables = new FakeResponsableRepository().AvecResponsable(ParentA).AvecResponsable(Nounou);
        new AffecterPeriodeHandler(periodes, responsables)
            .Handle(new PeriodeBuilder().PourResponsable(Nounou).Du(Mardi16).Au(Mardi16).Build());
        var query = QueryAvec(periodes);
        var etatObserve = Assert.Single(periodes.AllSnapshots());

        // And — la case du mardi 16/06/2026 affiche bien "Nounou".
        var avant = query.Projeter(Mardi16Juin2026).Jours.Single(j => j.Date == Mardi16Juin2026);
        Assert.Equal(NomNounou, avant.NomResponsable);

        // When — je réaffecte cette période à "Parent A" par son identifiant stable.
        var resultat = new EditerPeriodeHandler(periodes)
            .Handle(new EditerPeriodeCommand(etatObserve, ParentA, etatObserve.Debut, etatObserve.Fin));

        // Then — l'édition réussit.
        Assert.True(resultat.EstSucces);

        // And — la case affiche "Parent A" et sa couleur, et n'affiche plus "Nounou".
        var apres = query.Projeter(Mardi16Juin2026).Jours.Single(j => j.Date == Mardi16Juin2026);
        Assert.Equal(NomParentA, apres.NomResponsable);
        Assert.Equal(Bleu, apres.CouleurResponsable);
        Assert.NotEqual(NomNounou, apres.NomResponsable);
    }

    private static GrilleAgendaQuery QueryAvec(IPeriodeRepository periodes)
        => new(new FakeSlotRepository(), periodes,
            new FakePaletteCouleurs(new Dictionary<string, string> { [ParentA] = Bleu, [Nounou] = Vert }),
            new FakeReferentielResponsables(new Dictionary<string, string> { [ParentA] = NomParentA, [Nounou] = NomNounou }));
}
