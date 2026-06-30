using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 19 — Sc.1 — La grille ne résout que des acteurs déclarés (id stable) (@back)
//   Étant donné un foyer dont le store d'acteurs contient des acteurs déclarés (id stables)
//     Et au moins une période affectée et un cycle de fond mappés sur ces id stables
//   Quand on projette la grille agenda
//   Alors chaque case résolue affiche le nom et la couleur lus depuis le store vivant des acteurs déclarés
//     Et aucune case ne provient d'un libellé d'acteur codé en dur (« Parent A », « Parent B »)
//     Et la légende ne liste que les acteurs déclarés effectivement référencés
//
// Acceptation à la frontière Application (CQRS lecture) : on projette via GrilleAgendaQuery sur des
// PORTS doublés à la main (store vivant des acteurs déclarés). Les acteurs déclarés portent des
// libellés CHOISIS (« Camille » / « Damien ») et des couleurs CHOISIES (« violet » / « turquoise »)
// — ni le seed Alice/Bruno, ni les fictifs « Parent A / Parent B » — afin que toute résolution
// depuis un libellé codé en dur ferait DIVERGER les noms/couleurs attendus (test mutation-sensible).
// La surcharge (période) ET le fond (cycle) sont mappés sur les id stables déclarés ; la résolution
// nom+couleur se fait exclusivement sur l'identifiant via IReferentielResponsables / IPaletteCouleurs,
// gatée par l'énumération du store vivant (IEnumerationActeursFoyer). L'acceptation RUNTIME (front
// WASM + API distante + SignalR) est menée séparément (Sc.5/6/7).
//
// Dates déterministes (Projeter(ancre) — jamais Now) : fenêtre 4 semaines (28 j) ancrée au lundi
// 29/06/2026. ISO 27 (29/06, impaire → index 1) ; ISO 28 (06/07, paire → index 0).
public class Scenario1_GrilleResoutDepuisStoreVivant
{
    private const string Resp1 = "resp-1";          // acteur déclaré, porteur d'une période (surcharge)
    private const string Resp2 = "resp-2";          // acteur déclaré, porteur du cycle de fond
    private const string Camille = "Camille";       // libellé CHOISI (ni seed, ni fictif)
    private const string Damien = "Damien";         // libellé CHOISI (ni seed, ni fictif)
    private const string Violet = "violet";
    private const string Turquoise = "turquoise";

    private static readonly DateOnly Lundi_29_06_2026 = new(2026, 6, 29); // ISO 27 (impaire) — fond resp-2
    private static readonly DateOnly Lundi_06_07_2026 = new(2026, 7, 6);  // ISO 28 (paire)   — période resp-1

    [Fact]
    public void Acceptation_Should_Resoudre_nom_et_couleur_depuis_le_store_vivant_des_acteurs_declares_sans_aucun_libelle_en_dur_et_limiter_la_legende_aux_declares_references_When_la_grille_est_projetee()
    {
        // --- Given : store vivant = acteurs déclarés (id stables) avec libellés/couleurs CHOISIS ---
        var acteursDeclares = new FakeEnumerationActeursFoyer(Resp1, Resp2);
        var referentiel = new FakeReferentielResponsables(new Dictionary<string, string>
        {
            [Resp1] = Camille,
            [Resp2] = Damien,
        });
        var palette = new FakePaletteCouleurs(new Dictionary<string, string>
        {
            [Resp1] = Violet,
            [Resp2] = Turquoise,
        });

        // une période affectée à resp-1 (surcharge) le 06/07 (ISO paire, sans fond)
        var periodes = new FakePeriodeRepository();
        periodes.Enregistrer(PeriodeDeGarde
            .Affecter(Resp1, Lundi_06_07_2026.ToDateTime(TimeOnly.MinValue), Lundi_06_07_2026.ToDateTime(TimeOnly.MinValue))
            .Valeur!);

        // un cycle de fond mappé sur resp-2 pour l'index impair (les semaines ISO impaires de la fenêtre)
        var cycle = new FakeReferentielCycleDeFond(new CycleDeFond(2, new Dictionary<int, string> { [1] = Resp2 }));

        var query = new GrilleAgendaQuery(new FakeSlotRepository(), periodes, palette, referentiel, cycle, acteursDeclares);

        // --- When : projection de la grille (fenêtre 4 semaines ancrée au 29/06/2026) ---
        var grille = query.Projeter(Lundi_29_06_2026);

        // --- Then : la case de surcharge résout nom+couleur du store vivant (resp-1) ---
        var caseSurcharge = grille.Jours.Single(j => j.Date == Lundi_06_07_2026);
        Assert.Equal(Camille, caseSurcharge.NomResponsable);
        Assert.Equal(Violet, caseSurcharge.CouleurResponsable);

        // --- Then : la case de fond résout nom+couleur du store vivant (resp-2) ---
        var caseFond = grille.Jours.Single(j => j.Date == Lundi_29_06_2026);
        Assert.Equal(Damien, caseFond.NomResponsable);
        Assert.Equal(Turquoise, caseFond.CouleurResponsable);

        // --- Then : aucune case ne provient d'un libellé d'acteur codé en dur ---
        Assert.DoesNotContain(grille.Jours, j => j.NomResponsable is "Parent A" or "Parent B");

        // --- Then : la légende ne liste QUE les acteurs déclarés effectivement référencés (resp-1, resp-2) ---
        Assert.Equal(
            new[] { Resp1, Resp2 }.OrderBy(x => x),
            grille.Légende.Select(e => e.IdentifiantStable).OrderBy(x => x));
        Assert.All(grille.Légende, e => Assert.Contains(e.IdentifiantStable, acteursDeclares.EnumererActeurs()));
        Assert.DoesNotContain(grille.Légende, e => e.Nom is "Parent A" or "Parent B");

        // la légende porte les libellés/couleurs résolus du store vivant (jamais un libellé en dur)
        Assert.Equal(Camille, grille.Légende.Single(e => e.IdentifiantStable == Resp1).Nom);
        Assert.Equal(Damien, grille.Légende.Single(e => e.IdentifiantStable == Resp2).Nom);
    }
}
