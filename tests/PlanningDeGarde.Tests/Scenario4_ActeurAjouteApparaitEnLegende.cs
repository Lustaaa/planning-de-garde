using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 09 — Sc.4 — Un acteur ajouté apparaît en légende une fois une période affectée (@nominal)
//   Given Carla vient d'être ajoutée avec la couleur rose et n'a encore aucune période
//   When un parent affecte à Carla la garde de Léa du 8 au 12 juin
//   Then la légende fait apparaître une entrée « Carla » en rose
//   And cette entrée est portée par l'identifiant stable neuf de Carla
//   And les cases du 8 au 12 juin affichent « Carla » en rose
//
// CARACTÉRISATION BACKEND (tdd-auto) — early green ATTENDU (annoncé par tdd-analyse), PAS un driver.
// Aucun code neuf : prouve que l'IDENTIFIANT NEUF (issu de Sc.1, AjouterActeurHandler) CIRCULE de
// bout en bout jusqu'au read model. Composition : Sc.1 (l'ajout enregistre id→nom+couleur) +
// AffecterPeriodeHandler (existant) + GrilleAgendaQuery (s07, inchangé : résout NomDe/CouleurDe sur
// l'id stable et dédoublonne la légende PAR ID). Filet anti-régression. L'acceptation runtime IHM
// (grille câblée, légende sur l'id neuf, front WASM + API distante + store durable) est menée
// séparément par ihm-builder.
public class Scenario4_ActeurAjouteApparaitEnLegende
{
    private const string Carla = "Carla";
    private const string Rose = "rose";

    // 8 juin 2026 est un lundi (miroir des dates s07 : 29/06/2026 lundi) → fenêtre [08/06, 12/07].
    private static readonly DateOnly Lundi_08_06_2026 = new(2026, 6, 8);

    private static readonly DateOnly[] Jours_08_au_12_juin =
    {
        new(2026, 6, 8), new(2026, 6, 9), new(2026, 6, 10), new(2026, 6, 11), new(2026, 6, 12),
    };

    [Fact]
    public void Should_Faire_apparaitre_une_entree_de_legende_Carla_en_rose_sur_l_identifiant_neuf_et_nommer_les_cases_couvertes_When_une_periode_est_affectee_a_l_acteur_ajoute()
    {
        // --- Configuration foyer vierge, réalisant les 3 ports (édition + référentiel nom + palette) ---
        var configuration = new FakeConfigurationFoyer(new Dictionary<string, string>());

        // --- Sc.1 : un parent ajoute Carla en rose → identifiant stable NEUF opaque ---
        var ajout = new AjouterActeurHandler(configuration).Handle(new AjouterActeurCommand(Carla, Rose));
        Assert.True(ajout.EstSucces);
        var idNeufCarla = ajout.Valeur!.ActeurId;

        // --- Affectation (existant) : Carla garde Léa du 8 au 12 juin, sur SON id neuf ---
        var periodes = new FakePeriodeRepository();
        var responsables = new FakeResponsableRepository().AvecResponsable(idNeufCarla);
        var affectation = new AffecterPeriodeHandler(periodes, responsables)
            .Handle(new AffecterPeriodeCommand(idNeufCarla, new DateTime(2026, 6, 8), new DateTime(2026, 6, 12)));
        Assert.True(affectation.EstSucces);

        // --- Read model s07 inchangé : la légende/case « surfacent » l'id neuf déjà enregistré ---
        var grille = new GrilleAgendaQuery(new FakeSlotRepository(), periodes, configuration, configuration)
            .Projeter(Lundi_08_06_2026);

        // Then : une entrée de légende « Carla » rose, portée par l'identifiant stable NEUF de Carla
        var entree = Assert.Single(grille.Légende);
        Assert.Equal(idNeufCarla, entree.IdentifiantStable); // l'id neuf circule jusqu'à la légende (jamais le libellé)
        Assert.Equal(Carla, entree.Nom);
        Assert.Equal(Rose, entree.Couleur);

        // And : les cases du 8 au 12 juin affichent « Carla » en rose
        foreach (var jour in Jours_08_au_12_juin)
        {
            var caseJour = grille.Jours.Single(j => j.Date == jour);
            Assert.Equal(Carla, caseJour.NomResponsable);
            Assert.Equal(Rose, caseJour.CouleurResponsable);
        }
    }
}
