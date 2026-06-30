using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Tests.Builders;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 17 — Scénario 4 — Re-bornage : portion libérée sur index non mappé → neutre sans nom fantôme (@back)
//   Étant donné un cycle de fond N=2 mappant l'index 0 → "Parent A", l'index 1 NON mappé
//   Et une période durable attribue du lundi 15 au mercredi 17 juin 2026 à "Nounou"
//   Et la case du lundi 15 juin 2026 (semaine ISO 25, index 1) affiche "Nounou"
//   Quand je re-borne la période pour qu'elle couvre du mardi 16 au mercredi 17 juin 2026
//   Alors l'édition réussit
//   Et l'index 1 n'étant ni mappé ni résolu, la case du lundi 15 retombe sur la teinte neutre, sans nom
//
// CARACTÉRISATION (⚠️ early green ATTENDU, garde-fou de non-régression — PAS un driver).
// Repli surcharge > fond > neutre STRUCTUREL (GrilleAgendaQuery, acquis palier 6) : un index de fond non
// mappé → null → neutre (couleur neutre, nom vide), sans nom fantôme. Le re-bornage (Sc.1) avance le début ;
// le lundi 15 n'étant plus couvert et son index de fond (1) non mappé, sa case se re-résout sur le neutre
// sans code neuf. Ce test verrouille la COMPOSITION re-bornage → repli neutre.
public class Scenario4_EditerPeriodeRepliNeutre
{
    private const string ParentA = "parent-a";
    private const string Nounou = "nounou";
    private const string NomParentA = "Parent A";
    private const string NomNounou = "Nounou";
    private const string Bleu = "bleu";
    private const string Vert = "vert";

    private static readonly DateOnly Lundi15Juin2026 = new(2026, 6, 15); // ISO 25 → index 1 (non mappé)
    private static readonly DateTime Lundi15 = new(2026, 6, 15, 0, 0, 0);
    private static readonly DateTime Mardi16 = new(2026, 6, 16, 0, 0, 0);
    private static readonly DateTime Mercredi17 = new(2026, 6, 17, 0, 0, 0);

    [Fact]
    public void Should_faire_retomber_le_jour_libere_sur_le_neutre_sans_nom_fantome_When_son_index_de_fond_est_non_mappe()
    {
        // Given — cycle N=2 (index 0 → Parent A, index 1 NON mappé) ; surcharge "Nounou" du lundi 15 → mercredi 17.
        var periodes = new FakePeriodeRepository();
        var responsables = new FakeResponsableRepository().AvecResponsable(ParentA).AvecResponsable(Nounou);
        new AffecterPeriodeHandler(periodes, responsables)
            .Handle(new PeriodeBuilder().PourResponsable(Nounou).Du(Lundi15).Au(Mercredi17).Build());
        var query = QueryAvec(periodes);
        var etatObserve = Assert.Single(periodes.AllSnapshots());

        // And — la case du lundi 15/06/2026 (ISO 25, index 1) affiche bien "Nounou" (la surcharge prime).
        var avant = query.Projeter(Lundi15Juin2026).Jours.Single(j => j.Date == Lundi15Juin2026);
        Assert.Equal(NomNounou, avant.NomResponsable);

        // When — je re-borne la période pour qu'elle couvre mardi 16 → mercredi 17 (libère le lundi 15).
        var resultat = new EditerPeriodeHandler(periodes)
            .Handle(new EditerPeriodeCommand(etatObserve, etatObserve.ResponsableId, Mardi16, Mercredi17));

        // Then — l'édition réussit ; l'index 1 non mappé → la case du lundi 15 retombe sur la teinte neutre,
        // sans nom de responsable (aucun nom fantôme).
        Assert.True(resultat.EstSucces);
        var apres = query.Projeter(Lundi15Juin2026).Jours.Single(j => j.Date == Lundi15Juin2026);
        Assert.Equal("", apres.NomResponsable);
        Assert.Equal(FakePaletteCouleurs.Neutre, apres.CouleurResponsable);
    }

    private static GrilleAgendaQuery QueryAvec(IPeriodeRepository periodes)
        => new(new FakeSlotRepository(), periodes,
            new FakePaletteCouleurs(new Dictionary<string, string> { [ParentA] = Bleu, [Nounou] = Vert }),
            new FakeReferentielResponsables(new Dictionary<string, string> { [ParentA] = NomParentA, [Nounou] = NomNounou }),
            new FakeReferentielCycleDeFond(new CycleDeFond(2, new Dictionary<int, string> { [0] = ParentA })));
}
