using System;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 08 — Sc.4 — Collision de couleur entre deux acteurs : distingués par le nom (@limite, 🖥️ IHM)
//   Tranche BACKEND (tdd-auto) : CARACTÉRISATION (early green ATTENDU, filet anti-régression — pas
//   un driver). Recolorier deux acteurs distincts vers la MÊME couleur produit DEUX entrées de
//   légende distinctes, car la légende est dédoublonnée par IDENTIFIANT STABLE (s07 Sc.2), JAMAIS
//   par couleur. Le recoloriage (Sc.2) ne fait que muter la couleur résolue ; la dédup par id reste
//   l'invariant qui garantit deux entrées même de teinte identique. Collision ASSUMÉE (règle 17 :
//   la lisibilité repose sur le nom + légende, pas la couleur seule) — pas un défaut.
//
//   L'acceptation runtime IHM (deux cases bleues distinguables par leur nom, légende à deux entrées
//   bleues nommées distinctement, sur l'app réellement câblée) est menée séparément par ihm-builder.
public class Scenario4_CollisionCouleur
{
    private const string ParentA = "parent-a";
    private const string ParentB = "parent-b";
    private const string Alice = "Alice";   // seed nom parent-a (Foyer.NomsParResponsable)
    private const string Bruno = "Bruno";   // seed nom parent-b
    private const string Bleu = "bleu";     // seed couleur parent-a (Foyer.CouleursParActeur) — couleur cible de la collision

    private static readonly DateOnly Lundi_13_07_2026 = new(2026, 7, 13);

    // Deux périodes distinctes dans la fenêtre : parent-a le 14/07, parent-b le 15/07.
    private static FakePeriodeRepository PeriodesParentASur14EtParentBSur15()
    {
        var periodes = new FakePeriodeRepository();
        periodes.Enregistrer(PeriodeDeGarde.Affecter(ParentA, new DateTime(2026, 7, 14), new DateTime(2026, 7, 14)).Valeur!);
        periodes.Enregistrer(PeriodeDeGarde.Affecter(ParentB, new DateTime(2026, 7, 15), new DateTime(2026, 7, 15)).Valeur!);
        return periodes;
    }

    // ---------- Test #1 — Caractérisation : deux acteurs même couleur → deux entrées de légende ----------
    // ⚠️ early green ANTICIPÉ (cf. table 04-*.md) : la légende de GrilleAgendaQuery est dédoublonnée par
    // identifiant stable (.Distinct() sur ResponsableId, s07 Sc.2), JAMAIS par couleur. Recolorier
    // parent-b vers la couleur de parent-a ne fusionne donc pas les deux entrées : elles restent
    // distinctes par id et par nom, simplement de même teinte. Filet documentant la collision assumée.
    [Fact]
    public void Should_Lister_deux_entrees_de_legende_de_meme_couleur_distinguees_par_leur_identifiant_et_leur_nom_When_deux_acteurs_presents_partagent_la_meme_couleur()
    {
        // Store réel seedé (parent-a → Alice/bleu, parent-b → Bruno/orange), recolorié pour provoquer
        // la collision : parent-b passe à bleu — la même couleur que parent-a.
        var configuration = new ConfigurationFoyerEnMemoire();
        configuration.Recolorier(ParentB, Bleu);
        Assert.Equal(Bleu, configuration.CouleurDe(ParentA)); // les deux acteurs résolvent désormais
        Assert.Equal(Bleu, configuration.CouleurDe(ParentB)); // ... la MÊME couleur

        var query = new GrilleAgendaQuery(
            new FakeSlotRepository(), PeriodesParentASur14EtParentBSur15(), configuration, configuration);

        var legende = query.Projeter(Lundi_13_07_2026).Légende;

        // Deux entrées distinctes malgré la teinte identique : la dédup est par id stable, pas par couleur.
        Assert.Equal(2, legende.Count);
        Assert.All(legende, e => Assert.Equal(Bleu, e.Couleur));                 // même teinte (collision assumée)
        Assert.Equal(new[] { ParentA, ParentB }, legende.Select(e => e.IdentifiantStable).OrderBy(id => id)); // deux ids distincts
        Assert.Contains(legende, e => e.Nom == Alice);                          // ... distinguées par leur nom
        Assert.Contains(legende, e => e.Nom == Bruno);
    }
}
