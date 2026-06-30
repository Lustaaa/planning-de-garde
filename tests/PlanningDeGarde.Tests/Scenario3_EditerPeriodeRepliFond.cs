using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Tests.Builders;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 17 — Scénario 3 — Re-bornage : la portion libérée retombe sur le responsable de fond (@back)
//   Étant donné un cycle de fond N=2 mappant l'index 1 sur "Parent A"
//   Et une période durable attribue du lundi 15 au mercredi 17 juin 2026 à "Nounou" (surcharge sur le fond)
//   Et la case du lundi 15 juin 2026 (semaine ISO 25, index 1) affiche "Nounou"
//   Quand je re-borne la période pour qu'elle couvre du mardi 16 au mercredi 17 juin 2026
//   Alors l'édition réussit
//   Et la surcharge du lundi 15 juin 2026 cesse de primer
//   Et la case du lundi 15 juin 2026 retombe sur le responsable de fond "Parent A" et sa couleur
//
// CARACTÉRISATION (⚠️ early green ATTENDU, garde-fou de non-régression — PAS un driver).
// La priorité surcharge > fond > neutre est STRUCTURELLE (GrilleAgendaQuery, acquise palier 6) :
// le re-bornage (Sc.1) avance le début de la période ; la re-projection relit periodes.AllSnapshots()
// et le lundi 15 n'étant plus couvert, sa case se re-résout sur le fond (index 1 → Parent A) sans aucun
// code neuf. Ce test verrouille la COMPOSITION re-bornage → repli fond ; il ne pilote aucun rouge.
public class Scenario3_EditerPeriodeRepliFond
{
    private const string ParentA = "parent-a";
    private const string Nounou = "nounou";
    private const string NomParentA = "Parent A";
    private const string NomNounou = "Nounou";
    private const string Bleu = "bleu";
    private const string Vert = "vert";

    private static readonly DateOnly Lundi15Juin2026 = new(2026, 6, 15);
    private static readonly DateTime Lundi15 = new(2026, 6, 15, 0, 0, 0);
    private static readonly DateTime Mardi16 = new(2026, 6, 16, 0, 0, 0);
    private static readonly DateTime Mercredi17 = new(2026, 6, 17, 0, 0, 0);

    [Fact]
    public void Should_faire_retomber_le_jour_libere_sur_le_responsable_de_fond_When_on_re_borne_la_periode_de_surcharge()
    {
        // Given — cycle de fond N=2 (index 1 → Parent A) ; surcharge "Nounou" du lundi 15 → mercredi 17.
        var periodes = new FakePeriodeRepository();
        var responsables = new FakeResponsableRepository().AvecResponsable(ParentA).AvecResponsable(Nounou);
        new AffecterPeriodeHandler(periodes, responsables)
            .Handle(new PeriodeBuilder().PourResponsable(Nounou).Du(Lundi15).Au(Mercredi17).Build());
        var query = QueryAvec(periodes);
        var etatObserve = Assert.Single(periodes.AllSnapshots());

        // And — la case du lundi 15/06/2026 (ISO 25, index 1) affiche bien "Nounou" (surcharge prime).
        var avant = query.Projeter(Lundi15Juin2026).Jours.Single(j => j.Date == Lundi15Juin2026);
        Assert.Equal(NomNounou, avant.NomResponsable);

        // When — je re-borne la période pour qu'elle couvre mardi 16 → mercredi 17 (libère le lundi 15).
        var resultat = new EditerPeriodeHandler(periodes)
            .Handle(new EditerPeriodeCommand(etatObserve, etatObserve.ResponsableId, Mardi16, Mercredi17));

        // Then — l'édition réussit.
        Assert.True(resultat.EstSucces);

        // And — la surcharge cesse de primer : la case du lundi 15 retombe sur le fond "Parent A" + couleur.
        var apres = query.Projeter(Lundi15Juin2026).Jours.Single(j => j.Date == Lundi15Juin2026);
        Assert.Equal(NomParentA, apres.NomResponsable);
        Assert.Equal(Bleu, apres.CouleurResponsable);
        Assert.NotEqual(NomNounou, apres.NomResponsable);
    }

    private static GrilleAgendaQuery QueryAvec(IPeriodeRepository periodes)
        => new(new FakeSlotRepository(), periodes,
            new FakePaletteCouleurs(new Dictionary<string, string> { [ParentA] = Bleu, [Nounou] = Vert }),
            new FakeReferentielResponsables(new Dictionary<string, string> { [ParentA] = NomParentA, [Nounou] = NomNounou }),
            new FakeReferentielCycleDeFond(new CycleDeFond(2, new Dictionary<int, string> { [1] = ParentA })));
}
