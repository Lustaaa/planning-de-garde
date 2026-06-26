using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation (intégration de bout en bout) du canal d'écriture « pose de slot » — scénarios
/// 1 (@nominal, pose visible) et 2 (@erreur, lieu absent refusé sans effet de bord). Hôte Web
/// réel (<see cref="WebApplicationFactory{T}"/>), store réel singleton, projection réelle
/// <see cref="GrilleAgendaQuery"/> : aucune doublure sur le chemin observé (anti « vert qui
/// ment »). Le driver est l'endpoint HTTP du canal requête/réponse, l'observable est la grille
/// projetée à la semaine de référence. Un hôte neuf par test (store singleton frais) → isolation.
/// </summary>
public sealed class PoserSlotCanalTests
{
    // Mercredi 24/06/2026, école, 08:30 → 16:30 (commande de pose de Léa).
    private static readonly object CommandePoseLea = new
    {
        EnfantId = "Léa",
        LieuId = "école",
        Debut = new DateTime(2026, 6, 24, 8, 30, 0),
        Fin = new DateTime(2026, 6, 24, 16, 30, 0),
    };

    [Fact]
    public async Task Should_Confirmer_la_pose_par_une_reponse_de_succes_When_la_commande_de_pose_d_un_slot_valide_est_emise_via_le_canal_requete_reponse()
    {
        using var hote = new CanalEcritureFactory();
        var client = hote.CreateClient();

        var reponse = await client.PostAsJsonAsync("/api/canal/poser-slot", CommandePoseLea);

        Assert.True(reponse.IsSuccessStatusCode, $"statut HTTP attendu de succès, obtenu {(int)reponse.StatusCode}.");
    }

    [Fact]
    public async Task Should_Faire_apparaitre_le_slot_ecole_08h30_16h30_dans_la_case_du_mercredi_24_06_2026_de_la_projection_reelle_When_la_pose_de_Lea_a_abouti_via_le_canal()
    {
        using var hote = new CanalEcritureFactory();
        var client = hote.CreateClient();

        var reponse = await client.PostAsJsonAsync("/api/canal/poser-slot", CommandePoseLea);
        Assert.True(reponse.IsSuccessStatusCode, $"la pose via le canal doit aboutir, statut {(int)reponse.StatusCode}.");

        // Observable de bout en bout : la projection réelle lit le store réel singleton de l'hôte
        // (aucune doublure sur le chemin observé). Date de référence injectée = lundi 22/06/2026.
        using var scope = hote.Services.CreateScope();
        var projection = scope.ServiceProvider.GetRequiredService<GrilleAgendaQuery>();
        var grille = projection.Projeter(new DateOnly(2026, 6, 22));

        var caseMercredi = grille.Jours.Single(j => j.Date == new DateOnly(2026, 6, 24));
        var slot = Assert.Single(caseMercredi.Slots, s => s.Libelle == "école");
        Assert.Equal(new TimeOnly(8, 30), slot.Debut);
        Assert.Equal(new TimeOnly(16, 30), slot.Fin);
    }

    // Scénario 2 (@erreur) — pose au lieu « piscine » absent du foyer : refus propagé par le
    // canal + aucun effet de bord observable sur le store réel.
    private static readonly object CommandePoseLieuAbsent = new
    {
        EnfantId = "Léa",
        LieuId = "piscine", // absent du référentiel réel du foyer (école, domicile A/B, nounou)
        Debut = new DateTime(2026, 6, 24, 8, 30, 0),
        Fin = new DateTime(2026, 6, 24, 16, 30, 0),
    };

    [Fact]
    public async Task Should_Renvoyer_une_reponse_d_echec_au_motif_que_le_lieu_vise_n_existe_pas_When_la_commande_de_pose_au_lieu_piscine_absent_du_foyer_est_emise_via_le_canal()
    {
        using var hote = new CanalEcritureFactory();
        var client = hote.CreateClient();

        var reponse = await client.PostAsJsonAsync("/api/canal/poser-slot", CommandePoseLieuAbsent);

        Assert.False(reponse.IsSuccessStatusCode, $"un lieu absent doit être refusé, statut obtenu {(int)reponse.StatusCode}.");
        var motif = await reponse.Content.ReadAsStringAsync();
        Assert.Contains("lieu", motif, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("n'existe pas", motif, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Should_Laisser_la_case_du_mercredi_24_06_2026_sans_aucun_slot_piscine_dans_la_projection_reelle_When_la_pose_au_lieu_absent_a_ete_refusee_via_le_canal()
    {
        using var hote = new CanalEcritureFactory();
        var client = hote.CreateClient();

        var reponse = await client.PostAsJsonAsync("/api/canal/poser-slot", CommandePoseLieuAbsent);
        Assert.False(reponse.IsSuccessStatusCode, "la pose au lieu absent doit être refusée.");

        // Absence d'effet de bord observée sur le store réel via la projection réelle.
        using var scope = hote.Services.CreateScope();
        var projection = scope.ServiceProvider.GetRequiredService<GrilleAgendaQuery>();
        var grille = projection.Projeter(new DateOnly(2026, 6, 22));

        Assert.DoesNotContain(grille.Jours.SelectMany(j => j.Slots), s => s.Libelle == "piscine");
        var caseMercredi = grille.Jours.Single(j => j.Date == new DateOnly(2026, 6, 24));
        Assert.Empty(caseMercredi.Slots);
    }
}
