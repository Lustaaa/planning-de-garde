using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 09 — Sc.5 — Un acteur ajouté sans couleur retombe sur la teinte neutre (@limite)
//   Given le foyer affiche déjà ses acteurs colorés
//   When un parent ajoute « Papy Jo » sans lui choisir de couleur
//   And il affecte à Papy Jo la garde de Léa le 10 juin
//   Then la case du 10 juin affiche « Papy Jo » en gris
//   And la légende affiche « Papy Jo » en gris
//   And le nom « Papy Jo » est conservé
//
// CARACTÉRISATION BACKEND (tdd-auto) — early green ATTENDU (annoncé par tdd-analyse), PAS un driver.
// Aucun code neuf : le repli neutre est GARANTI PAR LE CONTRAT du port IPaletteCouleurs.CouleurDe
// (clé absente → CouleurNeutre = « gris »), miroir réel ET fake. L'ajout sans couleur (Sc.1,
// AjouterActeurHandler) n'enregistre RIEN côté couleur (write-guard `couleur is not null`), donc
// l'id neuf reste absent du set couleur → gris tombe par construction ; le nom reste résolu par
// NomDe(idNeuf) (Sc.1 enregistre le nom). Filet documentant le @limite « sans couleur → gris, nom
// conservé » (leçon s03 « repli gris »). L'acceptation runtime IHM (case + légende grises sur l'app
// réellement câblée) est menée séparément par ihm-builder.
public class Scenario5_ActeurSansCouleurTeinteNeutre
{
    private const string PapyJo = "Papy Jo";
    private const string Gris = FakeConfigurationFoyer.Neutre; // teinte neutre, contrat IPaletteCouleurs

    // 8 juin 2026 est un lundi (miroir des dates s07/s09 Sc.4) → fenêtre [08/06, …] couvrant le 10 juin.
    private static readonly DateOnly Lundi_08_06_2026 = new(2026, 6, 8);
    private static readonly DateOnly Le_10_juin = new(2026, 6, 10);

    [Fact]
    public void Should_Resoudre_la_teinte_neutre_pour_l_acteur_ajoute_en_conservant_son_nom_When_il_a_ete_ajoute_sans_couleur_puis_affecte_a_une_periode()
    {
        // --- Le foyer affiche déjà des acteurs colorés (deux acteurs semés colorés) ---
        var configuration = new FakeConfigurationFoyer(
            new Dictionary<string, string> { ["alice"] = "Alice", ["bruno"] = "Bruno" },
            new Dictionary<string, string> { ["alice"] = "bleu", ["bruno"] = "vert" });

        // --- Sc.1 : un parent ajoute « Papy Jo » SANS couleur → id stable neuf, AUCUNE couleur enregistrée ---
        var ajout = new AjouterActeurHandler(configuration).Handle(new AjouterActeurCommand(PapyJo)); // couleur omise
        Assert.True(ajout.EstSucces);
        var idNeufPapyJo = ajout.Valeur!.ActeurId;

        // --- Affectation (existant) : Papy Jo garde Léa le 10 juin, sur SON id neuf ---
        var periodes = new FakePeriodeRepository();
        var responsables = new FakeResponsableRepository().AvecResponsable(idNeufPapyJo);
        var affectation = new AffecterPeriodeHandler(periodes, responsables)
            .Handle(new AffecterPeriodeCommand(idNeufPapyJo, new DateTime(2026, 6, 10), new DateTime(2026, 6, 10)));
        Assert.True(affectation.EstSucces);

        // --- Read model s07 inchangé : nom et couleur « surfacent » l'id neuf via NomDe/CouleurDe ---
        var grille = new GrilleAgendaQuery(new FakeSlotRepository(), periodes, configuration, configuration)
            .Projeter(Lundi_08_06_2026);

        // Then : la case du 10 juin affiche « Papy Jo » en gris (teinte neutre par contrat)
        var caseDu10 = grille.Jours.Single(j => j.Date == Le_10_juin);
        Assert.Equal(PapyJo, caseDu10.NomResponsable);  // le nom « Papy Jo » est conservé
        Assert.Equal(Gris, caseDu10.CouleurResponsable); // sans couleur → repli neutre (gris)

        // And : la légende affiche « Papy Jo » en gris, le nom conservé
        var entree = Assert.Single(grille.Légende);
        Assert.Equal(idNeufPapyJo, entree.IdentifiantStable); // l'id neuf circule (jamais le libellé)
        Assert.Equal(PapyJo, entree.Nom);                     // nom conservé
        Assert.Equal(Gris, entree.Couleur);                   // teinte neutre
    }
}
