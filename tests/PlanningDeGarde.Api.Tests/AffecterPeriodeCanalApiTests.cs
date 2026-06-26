using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Acceptation (intégration de bout en bout) du Sc.1 — l'hôte d'API détaché enregistre une
/// affectation sans le front. Hôte API réel (<see cref="ApiHoteFactory"/>, store réel singleton,
/// palette réelle, projection réelle <see cref="GrilleAgendaQuery"/>) : aucune doublure sur le
/// chemin observé. Le driver est l'endpoint HTTP du canal d'affectation porté sur le nouvel hôte
/// API ; l'observable est l'accusé de succès puis la couleur des cases-jour de la grille projetée.
/// Un hôte neuf par test → isolation du store.
/// </summary>
public sealed class AffecterPeriodeCanalApiTests
{
    // Affectation de Parent A (id palette « parent-a » = bleu) du lundi 22 au vendredi 26/06/2026.
    private static readonly object CommandeAffectationParentA = new
    {
        ResponsableId = "parent-a",
        Debut = new DateTime(2026, 6, 22),
        Fin = new DateTime(2026, 6, 26),
    };

    // Test #2 — driver du canal sur l'hôte détaché : le portage de MapperCanalEcriture vers
    // ApiProgram, relié à AffecterPeriodeHandler, acquitte le succès via le canal HTTP de l'API.
    [Fact]
    public async Task Should_Confirmer_l_affectation_par_une_reponse_de_succes_When_la_commande_d_affectation_est_emise_sur_le_canal_de_l_hote_d_API()
    {
        using var hote = new ApiHoteFactory();
        var client = hote.CreateClient();

        var reponse = await client.PostAsJsonAsync("/api/canal/affecter-periode", CommandeAffectationParentA);

        Assert.True(reponse.IsSuccessStatusCode, $"statut HTTP attendu de succès, obtenu {(int)reponse.StatusCode}.");
    }

    // Test #3 — driver de bout en bout (anti early-green) : le chemin réel endpoint → handler →
    // store singleton de l'hôte API → projection réelle colore les cases couvertes.
    [Fact]
    public async Task Should_Colorer_les_cases_jour_du_lundi_22_au_vendredi_26_06_2026_de_la_couleur_bleue_de_Parent_A_dans_la_projection_reelle_When_l_affectation_a_abouti_via_le_canal_de_l_hote_d_API()
    {
        using var hote = new ApiHoteFactory();
        var client = hote.CreateClient();

        var reponse = await client.PostAsJsonAsync("/api/canal/affecter-periode", CommandeAffectationParentA);
        Assert.True(reponse.IsSuccessStatusCode, $"l'affectation via le canal de l'hôte API doit aboutir, statut {(int)reponse.StatusCode}.");

        // Observable de bout en bout : la projection réelle (+ palette réelle) lit le store réel
        // singleton de l'hôte API. Date de référence injectée = lundi 22/06/2026.
        using var scope = hote.Services.CreateScope();
        var projection = scope.ServiceProvider.GetRequiredService<GrilleAgendaQuery>();
        var grille = projection.Projeter(new DateOnly(2026, 6, 22));

        var casesCouvertes = grille.Jours
            .Where(j => j.Date >= new DateOnly(2026, 6, 22) && j.Date <= new DateOnly(2026, 6, 26))
            .ToList();
        Assert.Equal(5, casesCouvertes.Count);
        Assert.All(casesCouvertes, j => Assert.Equal("bleu", j.CouleurResponsable));
    }

    // @erreur — affectation sans responsable (ResponsableId vide) : refus propagé par le canal
    // (invariant « un responsable requis » de PeriodeDeGarde.Affecter) + aucune période persistée
    // → cases en couleur neutre. Portage depuis l'ancien hôte Web vers l'hôte d'API détaché.
    private static readonly object CommandeAffectationSansResponsable = new
    {
        ResponsableId = "",
        Debut = new DateTime(2026, 6, 22),
        Fin = new DateTime(2026, 6, 26),
    };

    [Fact]
    public async Task Should_Renvoyer_une_reponse_d_echec_pour_responsable_manquant_When_la_commande_d_affectation_d_une_periode_sans_responsable_est_emise_via_le_canal_de_l_hote_d_API()
    {
        using var hote = new ApiHoteFactory();
        var client = hote.CreateClient();

        var reponse = await client.PostAsJsonAsync("/api/canal/affecter-periode", CommandeAffectationSansResponsable);

        Assert.False(reponse.IsSuccessStatusCode, $"une affectation sans responsable doit être refusée, statut obtenu {(int)reponse.StatusCode}.");
        var motif = await reponse.Content.ReadAsStringAsync();
        Assert.Contains("responsable", motif, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Should_Laisser_les_cases_du_lundi_22_au_vendredi_26_06_2026_en_couleur_neutre_dans_la_projection_reelle_When_l_affectation_sans_responsable_a_ete_refusee_via_le_canal_de_l_hote_d_API()
    {
        using var hote = new ApiHoteFactory();
        var client = hote.CreateClient();

        var reponse = await client.PostAsJsonAsync("/api/canal/affecter-periode", CommandeAffectationSansResponsable);
        Assert.False(reponse.IsSuccessStatusCode, "l'affectation sans responsable doit être refusée.");

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
