using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Tests.Builders;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 16 — Scénario 3 — Repli neutre sans nom fantôme (index non résolu) (@back)
//   Étant donné un cycle de fond N=2 mappant index 0 → "Parent A", index 1 NON mappé
//   Et une période durable attribue le mardi 16 juin 2026 (ISO 25, index 1) à "Nounou"
//   Et la case du mardi 16 juin 2026 affiche "Nounou"
//   Quand je supprime la période du mardi 16 juin 2026
//   Alors la suppression réussit
//   Et l'index 1 n'étant ni mappé ni résolu, la case retombe sur la teinte neutre, sans nom
//
// CARACTÉRISATION (⚠️ early green ATTENDU, garde-fou de non-régression — PAS un driver).
// Repli surcharge > fond > neutre STRUCTUREL (GrilleAgendaQuery, acquis palier 6) : un index de
// fond non mappé → null → neutre (couleur neutre, nom vide), sans nom fantôme. La suppression (Sc.1)
// retire la surcharge ; la re-projection relit AllSnapshots sans elle → la case se re-résout sur le
// neutre sans code neuf. Ce test verrouille la COMPOSITION suppression → repli neutre.
public class Scenario3_SupprimerPeriodeRepliNeutre
{
    private const string ParentA = "parent-a";
    private const string Nounou = "nounou";
    private const string NomParentA = "Parent A";
    private const string NomNounou = "Nounou";
    private const string Bleu = "bleu";
    private const string Vert = "vert";

    private static readonly DateOnly Mardi16Juin2026 = new(2026, 6, 16); // ISO 25 → index 1 (non mappé)
    private static readonly DateTime Mardi16 = new(2026, 6, 16, 0, 0, 0);

    [Fact]
    public void Should_faire_retomber_la_case_sur_le_neutre_sans_nom_fantome_When_on_supprime_la_surcharge_sur_un_index_de_fond_non_mappe()
    {
        // Given — cycle N=2 (index 0 → Parent A, index 1 NON mappé) ; surcharge "Nounou" le 16/06/2026.
        var periodes = new FakePeriodeRepository();
        var responsables = new FakeResponsableRepository().AvecResponsable(ParentA).AvecResponsable(Nounou);
        new AffecterPeriodeHandler(periodes, responsables)
            .Handle(new PeriodeBuilder().PourResponsable(Nounou).Du(Mardi16).Au(Mardi16).Build());
        var query = QueryAvec(periodes);

        // And — la case du mardi 16/06/2026 affiche bien "Nounou" (la surcharge prime).
        var avant = query.Projeter(Mardi16Juin2026).Jours.Single(j => j.Date == Mardi16Juin2026);
        Assert.Equal(NomNounou, avant.NomResponsable);

        // When — je supprime la période du mardi 16/06/2026 par son identifiant stable.
        var idStable = Assert.Single(periodes.AllSnapshots()).Id;
        var resultat = new SupprimerPeriodeHandler(periodes).Handle(new SupprimerPeriodeCommand(idStable));

        // Then — la suppression réussit ; l'index 1 non mappé → la case retombe sur la teinte neutre,
        // sans nom de responsable (aucun nom fantôme).
        Assert.True(resultat.EstSucces);
        var apres = query.Projeter(Mardi16Juin2026).Jours.Single(j => j.Date == Mardi16Juin2026);
        Assert.Equal("", apres.NomResponsable);
        Assert.Equal(FakePaletteCouleurs.Neutre, apres.CouleurResponsable);
    }

    private static GrilleAgendaQuery QueryAvec(IPeriodeRepository periodes)
        => new(new FakeSlotRepository(), periodes,
            new FakePaletteCouleurs(new Dictionary<string, string> { [ParentA] = Bleu, [Nounou] = Vert }),
            new FakeReferentielResponsables(new Dictionary<string, string> { [ParentA] = NomParentA, [Nounou] = NomNounou }),
            new FakeReferentielCycleDeFond(new CycleDeFond(2, new Dictionary<int, string> { [0] = ParentA })));
}
