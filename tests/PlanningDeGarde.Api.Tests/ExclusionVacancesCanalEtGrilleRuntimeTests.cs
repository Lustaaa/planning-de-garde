using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 54 — S7 — câblage runtime de l'exclusion vacances sur l'hôte d'API RÉEL (DI réelle, store réel) :
/// poser une activité récurrente puis lui rattacher une plage d'exclusion via le CANAL D'ÉCRITURE réel
/// (POST /api/enfants/{id}/activites/recurrentes/{id}/exclusions) fait que la grille projetée réellement
/// (GET /api/grille/…) ne matérialise plus AUCUNE occurrence sur l'intervalle — hors plage, elle demeure.
/// </summary>
public sealed class ExclusionVacancesCanalEtGrilleRuntimeTests
{
    private sealed class HoteApi : WebApplicationFactory<ApiProgram>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Testing");
    }

    [Fact]
    public async Task Should_cesser_de_projeter_l_activite_pendant_la_plage_d_exclusion_When_une_plage_est_rattachee_via_le_canal_reel()
    {
        using var hote = new HoteApi();
        var client = hote.CreateClient();
        hote.Services.GetRequiredService<IEditeurActivites>().Ajouter("ecole", "ecole");
        hote.Services.GetRequiredService<IEditeurEnfants>().Ajouter("lea", "lea");

        // Given — une activité récurrente « ecole » chaque lundi pour Léa.
        var pose = await client.PostAsJsonAsync("/api/enfants/lea/activites/recurrentes", new
        {
            LieuId = "ecole",
            JourDeSemaine = DayOfWeek.Monday,
            HeureDebut = new TimeSpan(8, 30, 0),
            HeureFin = new TimeSpan(16, 30, 0),
        });
        Assert.True(pose.IsSuccessStatusCode, $"la pose du récurrent doit aboutir, statut {(int)pose.StatusCode}.");
        var id = hote.Services.GetRequiredService<ISlotRecurrentRepository>().AllSnapshots().Single().Id;

        // When — on rattache une plage d'exclusion couvrant le lundi 29/06/2026 via le canal d'écriture réel.
        var exclusion = await client.PostAsJsonAsync(
            $"/api/enfants/lea/activites/recurrentes/{id}/exclusions",
            new { Debut = new DateOnly(2026, 6, 29), Fin = new DateOnly(2026, 7, 5) });
        Assert.True(exclusion.IsSuccessStatusCode, $"l'ajout d'exclusion doit aboutir, statut {(int)exclusion.StatusCode}.");

        // Then — la grille projetée réellement (fenêtre dès le 24/06) ne porte plus « ecole » le lundi 29/06 exclu,
        // mais le porte le lundi 06/07 hors plage.
        var grille = await client.GetFromJsonAsync<GrilleAgenda>("/api/grille/2026/6/24");
        Assert.NotNull(grille);
        var lundiExclu = grille!.Jours.Single(j => j.Date == new DateOnly(2026, 6, 29));
        var lundiHorsPlage = grille!.Jours.Single(j => j.Date == new DateOnly(2026, 7, 6));
        Assert.DoesNotContain(lundiExclu.Slots, s => s.Libelle == "ecole");
        Assert.Contains(lundiHorsPlage.Slots, s => s.Libelle == "ecole");
    }
}
