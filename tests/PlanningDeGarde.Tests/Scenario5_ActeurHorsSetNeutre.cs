using System;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 08 — Sc.5 — Éditer un acteur hors set de couleurs : nom suivi, teinte neutre conservée (@limite, 🖥️ IHM)
//   Tranche BACKEND (tdd-auto) : CARACTÉRISATION (early green ATTENDU, filet anti-régression — pas
//   un driver). Renommer un acteur ABSENT du set de couleurs (« grand-pere », seedé « grand-père »,
//   hors Foyer.CouleursParActeur → repli neutre gris) met à jour SON NOM sans lui CRÉER de couleur :
//   nom et couleur sont deux surfaces INDÉPENDANTES du store, résolues séparément sur l'id stable
//   (s07 Sc.5). Renommer mute la seule surface nom ; CouleurDe retombe sur le neutre par repli
//   (acteur absent du set) PAR CONSTRUCTION — le renommage ne touche jamais la couleur.
//
//   L'acceptation runtime IHM (case du 17/07 + légende affichant « Papy Jo » mais RESTANT grises,
//   sur l'app réellement câblée) est menée séparément par ihm-builder.
public class Scenario5_ActeurHorsSetNeutre
{
    private const string GrandPere = "grand-pere";   // id stable hors Foyer.CouleursParActeur
    private const string GrandPereNom = "grand-père"; // seed nom (Foyer.NomsParResponsable)
    private const string PapyJo = "Papy Jo";          // nom édité
    private const string Gris = "gris";               // teinte neutre de repli (Foyer.CouleurNeutre)

    private static readonly DateOnly Lundi_13_07_2026 = new(2026, 7, 13);

    // grand-pere a une période dans la fenêtre (case du 17/07) pour apparaître en légende.
    private static FakePeriodeRepository PeriodeGrandPereSur17()
    {
        var periodes = new FakePeriodeRepository();
        periodes.Enregistrer(PeriodeDeGarde.Affecter(GrandPere, new DateTime(2026, 7, 17), new DateTime(2026, 7, 17)).Valeur!);
        return periodes;
    }

    // ---------- Test #1 — Caractérisation : renommer un acteur hors set suit le nom, garde le neutre ----------
    // ⚠️ early green ANTICIPÉ (cf. table 05-*.md) : nom et couleur sont deux surfaces indépendantes du
    // store ; Renommer ne mute QUE le nom (_noms), CouleurDe retombe sur Foyer.CouleurNeutre car
    // grand-pere est absent du set (_couleurs). Aucun rouge — filet documentant « renommer ≠ créer
    // une couleur », l'acteur hors set reste neutre après renommage.
    [Fact]
    public void Should_Suivre_le_nouveau_nom_en_conservant_la_teinte_neutre_When_un_acteur_absent_du_set_de_couleurs_est_renomme_sans_recolorier()
    {
        // Store réel seedé : grand-pere → « grand-père » (nom), aucune couleur dans le set → repli neutre.
        var configuration = new ConfigurationFoyerEnMemoire();
        Assert.Equal(GrandPereNom, configuration.NomDe(GrandPere)); // seed nom d'origine
        Assert.Equal(Gris, configuration.CouleurDe(GrandPere));     // ... et teinte neutre par repli (hors set)

        configuration.Renommer(GrandPere, PapyJo); // renommage SANS recoloriage

        Assert.Equal(PapyJo, configuration.NomDe(GrandPere)); // le nom suit la dernière écriture
        Assert.Equal(Gris, configuration.CouleurDe(GrandPere)); // ... la couleur reste neutre : renommer ne crée pas de couleur

        // Côté projection : la légende affiche le nouveau nom mais conserve la teinte neutre (case du 17/07).
        var query = new GrilleAgendaQuery(
            new FakeSlotRepository(), PeriodeGrandPereSur17(), configuration, configuration);
        var entree = Assert.Single(query.Projeter(Lundi_13_07_2026).Légende);
        Assert.Equal(GrandPere, entree.IdentifiantStable); // résolu sur l'id stable inchangé
        Assert.Equal(PapyJo, entree.Nom);                   // nom édité suivi en légende
        Assert.Equal(Gris, entree.Couleur);                 // teinte neutre conservée (pas de couleur créée)
    }
}
