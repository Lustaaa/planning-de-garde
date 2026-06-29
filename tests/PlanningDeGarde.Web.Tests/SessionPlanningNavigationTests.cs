using System;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Inner-loop (boucle rapide TDD) de l'état de navigation du calendrier (Sc.1, 🖥️ IHM) — <b>PAS la
/// preuve d'acceptation</b> (celle-ci est runtime, sur l'app réellement câblée :
/// <see cref="FrontWasmNavigationSemaineTempsReelTests"/>). Pilote la dimension neuve « ancre mutable » :
/// l'ancre fixe (semaine de référence) ne se décalait pas ; la navigation force un état d'ancre
/// décalable d'une semaine, ancrée au lundi (la fenêtre projetée re-cale au lundi).
/// </summary>
public sealed class SessionPlanningNavigationTests
{
    // Ancrage du scénario : aujourd'hui = mercredi 10/06/2026, semaine en cours lundi 08/06.
    private static readonly DateOnly Mercredi_10_06_2026 = new(2026, 6, 10);

    [Fact]
    public void Should_Avancer_l_ancre_au_lundi_suivant_When_l_utilisateur_demande_la_semaine_suivante()
    {
        var session = new SessionPlanning();
        session.InitialiserAncre(Mercredi_10_06_2026); // → lundi de la semaine = 08/06

        session.SemaineSuivante();

        // L'ancre avance d'une semaine, sur le lundi suivant (ISO 25) : 15/06/2026.
        Assert.Equal(new DateOnly(2026, 6, 15), session.Ancre);
    }

    [Fact]
    public void Should_Reculer_l_ancre_au_lundi_precedent_When_l_utilisateur_demande_la_semaine_precedente()
    {
        var session = new SessionPlanning();
        session.InitialiserAncre(Mercredi_10_06_2026); // → lundi de la semaine = 08/06

        session.SemainePrecedente();

        // L'ancre recule d'une semaine, sur le lundi précédent (ISO 23) : 01/06/2026.
        Assert.Equal(new DateOnly(2026, 6, 1), session.Ancre);
    }
}
