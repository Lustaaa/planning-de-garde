using System;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 53 — Sc.6 — Suppression / orphelin d'un enfant laisse l'AUTRE intact (@back, @caractérisation)
//   Étant donné deux enfants "Léa" et "Tom" avec leurs surcharges
//   Quand un enfant devient orphelin (surcharge référençant un responsable absent + enfant hors référentiel)
//   Alors la résolution et les surcharges de l'AUTRE enfant restent STRICTEMENT intactes
//   Et aucune case de l'autre enfant ne bascule ni ne se replie à tort
//   Et la lecture ne crashe pas (repli neutre côté enfant absent, Resolvable s13)
//
// ⚠️ EARLY-GREEN ATTENDU (comme s13 Sc.3) : émerge de la COMPOSITION du filtrage EnfantId (s53 Sc.1) et du
// contrat d'existence Resolvable (s13) — aucun code neuf. Un rouge signalerait une régression de l'un des deux.
public class Scenario53_S6_OrphelinDunEnfantLaisseLautreIntact
{
    private const string LeaId = "enfant-lea";
    private const string TomId = "enfant-tom";
    private static readonly DateOnly J = new(2026, 7, 8);

    [Fact]
    public void Acceptation_InMemory_Orphelin_dun_enfant_laisse_lautre_intact_sans_crash()
    {
        var config = new ConfigurationFoyerEnMemoire();
        // Seul David existe ; le responsable de Léa n'est JAMAIS ajouté (orphelin). Aucun cycle (pas de fond).
        var david = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("David")).Valeur!.ActeurId;
        var carlaOrpheline = "acteur-absent-" + Guid.NewGuid().ToString("N");

        // Léa est orpheline : son responsable n'existe pas (jamais ajouté) — sa surcharge est orpheline (Resolvable).
        var periodes = new InMemoryPeriodeRepository();
        periodes.Enregistrer(PeriodeDeGarde.Affecter(
            carlaOrpheline, J.ToDateTime(TimeOnly.MinValue), J.ToDateTime(TimeOnly.MinValue), LeaId).Valeur!);
        periodes.Enregistrer(PeriodeDeGarde.Affecter(
            david, J.ToDateTime(TimeOnly.MinValue), J.ToDateTime(TimeOnly.MinValue), TomId).Valeur!);

        var grille = new GrilleAgendaQuery(
            new InMemorySlotRepository(), periodes, config, config,
            new CycleDeFondEnMemoire(), config, new InMemorySlotRecurrentRepository(), new InMemoryTransfertRepository());

        // Then — l'AUTRE enfant (Tom) est STRICTEMENT intact : sa surcharge résout David, aucune bascule.
        var caseTom = grille.Projeter(J, VuePlanning.Semaine, TomId).Jours.Single(j => j.Date == J);
        Assert.Equal(david, caseTom.ResponsableId);
        Assert.Equal(david, Assert.Single(periodes.AllSnapshots(), p => p.EnfantId == TomId).ResponsableId);

        // Then — lire l'enfant orphelin NE CRASHE PAS, repli neutre (responsable absent, aucun fond → Resolvable s13).
        var caseLea = grille.Projeter(J, VuePlanning.Semaine, LeaId).Jours.Single(j => j.Date == J);
        Assert.Null(caseLea.ResponsableId);
        Assert.Equal("", caseLea.NomResponsable);
        Assert.Equal(config.CouleurNeutre, caseLea.CouleurResponsable);
    }
}
