using System;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 08 — Sc.7 — Deux écrans renomment le même acteur : dernière écriture gagne, les grilles convergent (@limite, 🖥️ IHM)
//   Tranche BACKEND (tdd-auto) : CARACTÉRISATION (early green ATTENDU, filet anti-régression — pas
//   un driver). Deux écrans partagent le MÊME store singleton serveur (décision CP : mémoire
//   partagée du foyer). Renommer le même acteur (parent-b « Bruno ») deux fois successivement
//   (« Bruno M. » par un premier écran, puis « Bruno Martin » par un second) ÉCRASE la valeur :
//   Renommer est une affectation sur l'id stable (_noms[acteurId] = nouveauNom), SANS version ni
//   garde de conflit → dernière-écriture-gagne PAR CONSTRUCTION, aucune édition rejetée. Aucun
//   rouge — filet documentant l'absence de rejet/version (YAGNI, cohérent règle 25).
//
//   Le VRAI driver du scénario est RUNTIME : la convergence des DEUX grilles via SignalR sur l'app
//   réellement câblée (deux clients, hub réel, re-render sans rechargement) est menée séparément
//   par ihm-builder — bUnit seul ne prouve jamais ce câblage.
public class Scenario7_DerniereEcritureGagne
{
    private const string ParentB = "parent-b";       // id stable édité par les deux écrans
    private const string BrunoSeed = "Bruno";         // seed nom (Foyer.NomsParResponsable)
    private const string BrunoM = "Bruno M.";         // 1re écriture (premier écran)
    private const string BrunoMartin = "Bruno Martin"; // 2e écriture (second écran) — gagnante

    private static readonly DateOnly Lundi_13_07_2026 = new(2026, 7, 13); // fenêtre : 13/07 → 16/08/2026

    // parent-b a une période couvrant le 15/07 (dans la fenêtre) → présent en grille (case + légende).
    private static FakePeriodeRepository PeriodeParentBSur15()
    {
        var periodes = new FakePeriodeRepository();
        periodes.Enregistrer(PeriodeDeGarde.Affecter(ParentB, new DateTime(2026, 7, 15), new DateTime(2026, 7, 15)).Valeur!);
        return periodes;
    }

    // ---------- Test #1 — Caractérisation : dernière écriture gagne, aucune édition rejetée ----------
    // ⚠️ early green ANTICIPÉ (cf. table 07-*.md) : le store partagé écrase le nom (affectation, pas de
    // version ni de garde de conflit) → la seconde écriture l'emporte ; les deux handlers réussissent
    // (validation « id connu + nom non vide », indépendante d'un quelconque numéro de révision). Aucun
    // rouge — filet documentant « deux écrans → convergence vers la dernière valeur, sans rejet ».
    [Fact]
    public void Should_Resoudre_le_dernier_nom_ecrit_sans_rejeter_aucune_edition_When_le_meme_acteur_est_renomme_deux_fois_successivement_dans_le_store_partage()
    {
        // Store singleton PARTAGÉ par les deux écrans (mémoire commune du foyer — décision CP).
        var configuration = new ConfigurationFoyerEnMemoire();
        Assert.Equal(BrunoSeed, configuration.NomDe(ParentB)); // seed nom d'origine

        // Deux écrans pilotent le MÊME store via leur propre handler (deux canaux d'écriture distincts).
        var ecranA = new EditerActeurHandler(configuration, new FakeNotificateurPlanning());
        var ecranB = new EditerActeurHandler(configuration, new FakeNotificateurPlanning());

        var premiere = ecranA.Handle(new EditerActeurCommand(ParentB, BrunoM));      // 1er écran : « Bruno M. »
        var seconde = ecranB.Handle(new EditerActeurCommand(ParentB, BrunoMartin));  // 2e écran : « Bruno Martin »

        // Aucune édition rejetée : pas de garde de conflit / version — les deux écritures aboutissent.
        Assert.True(premiere.EstSucces);
        Assert.True(seconde.EstSucces);

        // Dernière écriture gagne : le store résout la valeur du second écran (la première est écrasée).
        Assert.Equal(BrunoMartin, configuration.NomDe(ParentB));

        // Côté projection : les deux grilles relisent le MÊME store → convergence vers « Bruno Martin »
        // dans la case du 15/07 ET en légende (résolu sur l'id stable inchangé).
        var query = new GrilleAgendaQuery(
            new FakeSlotRepository(), PeriodeParentBSur15(), configuration, configuration);
        var grille = query.Projeter(Lundi_13_07_2026);

        var caseDu15 = grille.Jours.Single(j => j.Date == new DateOnly(2026, 7, 15));
        Assert.Equal(BrunoMartin, caseDu15.NomResponsable); // la case converge vers la dernière valeur écrite

        var entree = Assert.Single(grille.Légende);
        Assert.Equal(ParentB, entree.IdentifiantStable); // résolu sur l'id stable (jamais le libellé)
        Assert.Equal(BrunoMartin, entree.Nom);            // la légende converge vers la dernière valeur
    }
}
