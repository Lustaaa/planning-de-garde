using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation (intégration de bout en bout) du canal d'affectation de période — scénarios
/// 3 (@nominal, coloration des cases couvertes) et 4 (@erreur, responsable manquant refusé sans
/// effet coloré). Hôte Web réel (<see cref="CanalEcritureFactory"/>, store réel singleton,
/// palette réelle <see cref="GrilleAgendaQuery"/>) : aucune doublure sur le chemin observé. Le
/// driver est l'endpoint HTTP du canal d'affectation, l'observable est la couleur des cases-jour
/// de la grille projetée. Un hôte neuf par test → isolation du store.
/// </summary>
public sealed class AffecterPeriodeCanalTests
{
    // Affectation de Parent A (id palette « parent-a » = bleu) du lundi 22 au vendredi 26/06/2026.
    // La palette réelle (FoyerPaletteCouleurs) mappe la clé « parent-a » → « bleu » : c'est l'id
    // technique du responsable que le canal transmet au handler, jamais re-mappé ici (règle de
    // coloration inchangée, déjà verte).
    private static readonly object CommandeAffectationParentA = new
    {
        ResponsableId = "parent-a",
        Debut = new DateTime(2026, 6, 22),
        Fin = new DateTime(2026, 6, 26),
    };

    [Fact]
    public async Task Should_Confirmer_l_affectation_par_une_reponse_de_succes_When_la_commande_d_affectation_d_une_periode_avec_responsable_est_emise_via_le_canal_requete_reponse()
    {
        using var hote = new CanalEcritureFactory();
        var client = hote.CreateClient();

        var reponse = await client.PostAsJsonAsync("/api/canal/affecter-periode", CommandeAffectationParentA);

        Assert.True(reponse.IsSuccessStatusCode, $"statut HTTP attendu de succès, obtenu {(int)reponse.StatusCode}.");
    }

    [Fact]
    public async Task Should_Colorer_les_cases_jour_du_lundi_22_au_vendredi_26_06_2026_de_la_couleur_de_Parent_A_dans_la_projection_reelle_When_l_affectation_a_abouti_via_le_canal()
    {
        using var hote = new CanalEcritureFactory();
        var client = hote.CreateClient();

        var reponse = await client.PostAsJsonAsync("/api/canal/affecter-periode", CommandeAffectationParentA);
        Assert.True(reponse.IsSuccessStatusCode, $"l'affectation via le canal doit aboutir, statut {(int)reponse.StatusCode}.");

        // Observable de bout en bout : la projection réelle (+ palette réelle) lit le store réel
        // singleton de l'hôte. Date de référence injectée = lundi 22/06/2026.
        using var scope = hote.Services.CreateScope();
        var projection = scope.ServiceProvider.GetRequiredService<GrilleAgendaQuery>();
        var grille = projection.Projeter(new DateOnly(2026, 6, 22));

        var casesCouvertes = grille.Jours
            .Where(j => j.Date >= new DateOnly(2026, 6, 22) && j.Date <= new DateOnly(2026, 6, 26))
            .ToList();
        Assert.Equal(5, casesCouvertes.Count);
        Assert.All(casesCouvertes, j => Assert.Equal("bleu", j.CouleurResponsable));
    }

    // Scénario 4 (@erreur) — affectation sans responsable (ResponsableId vide) : refus propagé
    // par le canal (invariant « un responsable requis » de PeriodeDeGarde.Affecter) + aucune
    // période persistée → cases en couleur neutre.
    private static readonly object CommandeAffectationSansResponsable = new
    {
        ResponsableId = "", // aucun responsable → invariant PeriodeDeGarde.Affecter violé
        Debut = new DateTime(2026, 6, 22),
        Fin = new DateTime(2026, 6, 26),
    };

    [Fact]
    public async Task Should_Renvoyer_une_reponse_d_echec_pour_responsable_manquant_When_la_commande_d_affectation_d_une_periode_sans_responsable_est_emise_via_le_canal()
    {
        using var hote = new CanalEcritureFactory();
        var client = hote.CreateClient();

        var reponse = await client.PostAsJsonAsync("/api/canal/affecter-periode", CommandeAffectationSansResponsable);

        Assert.False(reponse.IsSuccessStatusCode, $"une affectation sans responsable doit être refusée, statut obtenu {(int)reponse.StatusCode}.");
        var motif = await reponse.Content.ReadAsStringAsync();
        Assert.Contains("responsable", motif, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Should_Laisser_les_cases_du_lundi_22_au_vendredi_26_06_2026_en_couleur_neutre_dans_la_projection_reelle_When_l_affectation_sans_responsable_a_ete_refusee_via_le_canal()
    {
        using var hote = new CanalEcritureFactory();
        var client = hote.CreateClient();

        var reponse = await client.PostAsJsonAsync("/api/canal/affecter-periode", CommandeAffectationSansResponsable);
        Assert.False(reponse.IsSuccessStatusCode, "l'affectation sans responsable doit être refusée.");

        // Absence d'effet coloré observée sur le store réel via la projection réelle : aucune
        // période persistée → les cases concernées conservent la couleur neutre (gris).
        using var scope = hote.Services.CreateScope();
        var projection = scope.ServiceProvider.GetRequiredService<GrilleAgendaQuery>();
        var grille = projection.Projeter(new DateOnly(2026, 6, 22));

        var casesConcernees = grille.Jours
            .Where(j => j.Date >= new DateOnly(2026, 6, 22) && j.Date <= new DateOnly(2026, 6, 26))
            .ToList();
        Assert.Equal(5, casesConcernees.Count);
        Assert.All(casesConcernees, j => Assert.Equal("gris", j.CouleurResponsable));
    }
}
