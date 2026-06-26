using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 07 — Sc.1 — Une période affectée affiche le nom et entre dans la légende (@nominal)
//   Given une fenêtre de 4 semaines glissantes à partir du lundi 29/06/2026
//   And une période de garde le lundi 29/06/2026 dont le responsable est Alice (parent-a, bleu)
//   When la grille est affichée
//   Then la case du lundi 29/06/2026 affiche le nom "Alice" sur fond bleu
//   And la légende contient exactement une entrée Alice (bleu)
//
// Tranche read-model BACKEND (tdd-auto) sur GrilleAgendaQuery : on assert sur le read model
// (JourCase.NomResponsable + GrilleAgenda.Légende), pas sur un rendu Blazor. Le nom est résolu
// par le port IReferentielResponsables (doublé à la main, miroir de la palette), sur l'identifiant
// STABLE de la période (règle 17 : jamais sur le libellé). L'acceptation runtime IHM (rendu du nom
// dans la case réellement câblée + composant Légende) est menée séparément par ihm-builder.
public class Scenario_PeriodeAffecteeNomEtLegende
{
    private const string ParentA = "parent-a";
    private const string Alice = "Alice";
    private const string Bleu = "bleu";

    private static readonly DateOnly Lundi_29_06_2026 = new(2026, 6, 29);

    private static IPaletteCouleurs PaletteParentABleu()
        => new FakePaletteCouleurs(new Dictionary<string, string> { [ParentA] = Bleu });

    private static IReferentielResponsables ReferentielParentAAlice()
        => new FakeReferentielResponsables(new Dictionary<string, string> { [ParentA] = Alice });

    private static FakePeriodeRepository PeriodeConfiantLeaAParentALe_29_06()
    {
        var periodes = new FakePeriodeRepository();
        var periode = PeriodeDeGarde
            .Affecter(ParentA, new DateTime(2026, 6, 29), new DateTime(2026, 6, 29))
            .Valeur!;
        periodes.Enregistrer(periode);
        return periodes;
    }

    private static GrilleAgendaQuery Query()
        => new(new FakeSlotRepository(), PeriodeConfiantLeaAParentALe_29_06(), PaletteParentABleu(), ReferentielParentAAlice());

    // ---------- Tests unitaires (boucle interne, TDD) ----------

    // Test #1 — Driver : aujourd'hui JourCase ne porte aucun nom (chaîne vide). La case couverte
    // par la période d'Alice doit exposer son nom, résolu via le port sur l'identifiant stable
    // parent-a (à côté de la couleur, déjà résolue au palier 2).
    [Fact]
    public void Should_Porter_le_nom_du_responsable_dans_les_cases_couvertes_par_sa_periode_When_une_periode_lui_est_affectee_dans_la_fenetre()
    {
        var grille = Query().Projeter(Lundi_29_06_2026);

        var caseLundi = grille.Jours.Single(j => j.Date == Lundi_29_06_2026);
        Assert.Equal(Alice, caseLundi.NomResponsable);
        Assert.Equal(Bleu, caseLundi.CouleurResponsable);
    }

    // Test #2 — Driver : il n'existe aucune entrée de légende dans le read model (collection vide).
    // Le responsable présent dans la fenêtre (Alice, dont la période couvre un jour affiché) doit
    // entrer dans la légende avec son identifiant stable, son nom et sa couleur résolus côte à côte.
    [Fact]
    public void Should_Inscrire_le_responsable_present_dans_la_legende_avec_son_nom_et_sa_couleur_When_sa_periode_couvre_un_jour_de_la_fenetre()
    {
        var grille = Query().Projeter(Lundi_29_06_2026);

        var entree = Assert.Single(grille.Légende);
        Assert.Equal(ParentA, entree.IdentifiantStable);
        Assert.Equal(Alice, entree.Nom);
        Assert.Equal(Bleu, entree.Couleur);
    }
}
