using System;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 08 — Sc.6 — Éditer un acteur hors fenêtre : pas d'entrée fantôme (@limite, 🖥️ IHM)
//   Tranche BACKEND (tdd-auto) : CARACTÉRISATION (early green ATTENDU, filet anti-régression — pas
//   un driver). Renommer un acteur (« parent-c ») qui n'a AUCUNE période dans la fenêtre de 5
//   semaines affichée est CONFIRMÉ (la validation = « id stable connu + nom non vide », INDÉPENDANTE
//   de la fenêtre — Sc.1 #2), mais la légende de la fenêtre courante ne fait apparaître AUCUNE entrée
//   pour cet acteur : la légende dérive des présents (périodes intersectant la fenêtre, s07 Sc.3),
//   jamais du catalogue du foyer — pas d'entrée fantôme PAR CONSTRUCTION. Aucun rouge attendu.
//
//   L'acceptation runtime IHM (l'écran de config confirme « Mathilde », la grille de la fenêtre
//   reste inchangée et la légende n'introduit aucune entrée fantôme, sur l'app réellement câblée)
//   est menée séparément par ihm-builder.
public class Scenario6_ActeurHorsFenetre
{
    private const string ParentC = "parent-c"; // id stable édité, sans période dans la fenêtre
    private const string ParentA = "parent-a"; // présent dans la fenêtre (case du 14/07) — témoin de légende
    private const string Mathilde = "Mathilde"; // nom édité de parent-c
    private const string Alice = "Alice";        // seed nom parent-a (Foyer.NomsParResponsable)

    private static readonly DateOnly Lundi_13_07_2026 = new(2026, 7, 13); // fenêtre de 5 semaines : 13/07 → 16/08/2026

    // parent-a a une période DANS la fenêtre (case du 14/07) → présent en légende ;
    // parent-c n'a AUCUNE période dans la fenêtre → ne doit pas y figurer (pas d'entrée fantôme).
    private static FakePeriodeRepository PeriodeParentASur14_SansParentC()
    {
        var periodes = new FakePeriodeRepository();
        periodes.Enregistrer(PeriodeDeGarde.Affecter(ParentA, new DateTime(2026, 7, 14), new DateTime(2026, 7, 14)).Valeur!);
        return periodes;
    }

    // ---------- Test #1 — Caractérisation : édition confirmée hors fenêtre, sans entrée fantôme ----------
    // ⚠️ early green ANTICIPÉ (cf. table 06-*.md) : le handler valide l'EXISTENCE de l'id (pas la
    // présence en fenêtre) → confirme l'édition ; la légende de GrilleAgendaQuery dérive des périodes
    // COUVRANT la fenêtre (LegendeDesPresents, s07 Sc.3) → un acteur édité mais absent de la fenêtre
    // n'y apparaît jamais. Aucun rouge — filet documentant « confirmation ≠ présence en grille ».
    [Fact]
    public void Should_Confirmer_le_renommage_sans_faire_apparaitre_l_acteur_en_legende_When_l_acteur_renomme_n_a_aucune_periode_dans_la_fenetre_affichee()
    {
        var configuration = new ConfigurationFoyerEnMemoire();
        var handler = new EditerActeurHandler(configuration, new FakeNotificateurPlanning());

        // L'édition est confirmée — validation « id stable connu + nom non vide », INDÉPENDANTE de la
        // fenêtre affichée : un acteur peut être édité même sans période visible.
        var resultat = handler.Handle(new EditerActeurCommand(ParentC, Mathilde));
        Assert.True(resultat.EstSucces);                      // édition confirmée
        Assert.Equal(Mathilde, resultat.Valeur!.Nom);         // ... porteuse du nom appliqué
        Assert.Equal(Mathilde, configuration.NomDe(ParentC)); // le store reflète désormais « Mathilde »

        // Légende de la fenêtre courante : parent-a est présent (case du 14/07), parent-c ne l'est pas.
        var query = new GrilleAgendaQuery(
            new FakeSlotRepository(), PeriodeParentASur14_SansParentC(), configuration, configuration);
        var legende = query.Projeter(Lundi_13_07_2026).Légende;

        // Pas d'entrée fantôme : seul le présent (parent-a) figure ; l'acteur édité hors fenêtre reste absent.
        Assert.DoesNotContain(legende, e => e.IdentifiantStable == ParentC); // parent-c absent malgré l'édition
        Assert.DoesNotContain(legende, e => e.Nom == Mathilde);              // ... son nouveau nom n'apparaît pas
        var entree = Assert.Single(legende);                                 // seul le responsable présent dans la fenêtre
        Assert.Equal(ParentA, entree.IdentifiantStable);
        Assert.Equal(Alice, entree.Nom);
    }
}
