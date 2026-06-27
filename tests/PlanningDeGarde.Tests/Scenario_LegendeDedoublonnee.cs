using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 07 — Sc.2 — Plusieurs responsables : légende dédoublonnée (@nominal)
//   Given une période confie un jour à Alice (parent-a, bleu), un autre à Bruno (parent-b, vert),
//         et un troisième de nouveau à Alice, dans la fenêtre affichée
//   When la grille est projetée
//   Then la légende contient exactement deux entrées : Alice (bleu) une seule fois et Bruno (vert)
//
// Tranche read-model BACKEND (tdd-auto) sur GrilleAgendaQuery. Dédoublonnage par identifiant
// STABLE (règle 17, jamais le libellé). L'acceptation runtime IHM (rendu des cases + composant
// Légende à 2 entrées) est menée séparément par ihm-builder.
public class Scenario_LegendeDedoublonnee
{
    private const string ParentA = "parent-a";
    private const string ParentB = "parent-b";
    private const string Alice = "Alice";
    private const string Bruno = "Bruno";
    private const string Bleu = "bleu";
    private const string Vert = "vert";

    private static readonly DateOnly Lundi_29_06_2026 = new(2026, 6, 29);
    private static readonly DateOnly Mardi_30_06_2026 = new(2026, 6, 30);
    private static readonly DateOnly Mercredi_01_07_2026 = new(2026, 7, 1);

    private static IPaletteCouleurs PaletteAliceBleuBrunoVert()
        => new FakePaletteCouleurs(new Dictionary<string, string> { [ParentA] = Bleu, [ParentB] = Vert });

    private static IReferentielResponsables ReferentielAliceBruno()
        => new FakeReferentielResponsables(new Dictionary<string, string> { [ParentA] = Alice, [ParentB] = Bruno });

    // Alice couvre DEUX jours (29/06 et 01/07) via deux périodes distinctes ; Bruno couvre le 30/06.
    private static FakePeriodeRepository PeriodesAliceDeuxJoursEtBrunoUnJour()
    {
        var periodes = new FakePeriodeRepository();
        periodes.Enregistrer(PeriodeDeGarde.Affecter(ParentA, new DateTime(2026, 6, 29), new DateTime(2026, 6, 29)).Valeur!);
        periodes.Enregistrer(PeriodeDeGarde.Affecter(ParentB, new DateTime(2026, 6, 30), new DateTime(2026, 6, 30)).Valeur!);
        periodes.Enregistrer(PeriodeDeGarde.Affecter(ParentA, new DateTime(2026, 7, 1), new DateTime(2026, 7, 1)).Valeur!);
        return periodes;
    }

    private static GrilleAgendaQuery Query()
        => new(new FakeSlotRepository(), PeriodesAliceDeuxJoursEtBrunoUnJour(), PaletteAliceBleuBrunoVert(), ReferentielAliceBruno());

    // ---------- Tests unitaires (boucle interne, TDD) ----------

    // NOTE — Le « driver » dédoublonnage initialement prévu (Sc.2 #1) a été RETIRÉ sur décision PO
    // (porte G4) : il faisait doublon avec la garantie déjà livrée en Sc.1 (le `.Distinct()` par
    // identifiant stable de la dérivation de légende). Il passait vert d'emblée, sans rouge pilotant
    // du code neuf. Le dédoublonnage reste couvert par le read model de Sc.1 ; seule la
    // caractérisation « nom par case » subsiste ci-dessous comme filet anti-régression.

    // Test #2 — Caractérisation (early green ATTENDU, filet anti-régression — pas un driver) :
    // chaque responsable porte son propre nom dans SES cases. Déjà garanti par Sc.1 #1 (résolution
    // du nom par identifiant stable de la période, appliquée à chaque case). Empêche une régression
    // qui mélangerait les noms entre cases.
    [Fact]
    public void Should_Porter_le_nom_de_chaque_responsable_dans_ses_propres_cases_When_deux_responsables_couvrent_des_jours_differents()
    {
        var grille = Query().Projeter(Lundi_29_06_2026);

        Assert.Equal(Alice, grille.Jours.Single(j => j.Date == Lundi_29_06_2026).NomResponsable);
        Assert.Equal(Bruno, grille.Jours.Single(j => j.Date == Mardi_30_06_2026).NomResponsable);
        Assert.Equal(Alice, grille.Jours.Single(j => j.Date == Mercredi_01_07_2026).NomResponsable);
    }
}
