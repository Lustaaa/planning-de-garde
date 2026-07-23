using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 54 — S9 — câblage runtime de l'exception d'occurrence sur l'hôte d'API RÉEL (DI réelle, store
/// réel) : supprimer UNE occurrence d'un mardi précis via le CANAL D'ÉCRITURE réel
/// (DELETE /api/enfants/{id}/activites/recurrentes/{id}/occurrences/{a}/{m}/{j}) fait que la grille
/// projetée réellement (GET /api/grille/…) ne matérialise plus cette occurrence, mais conserve les autres
/// mardis de la série.
/// </summary>
public sealed class ExceptionOccurrenceCanalEtGrilleRuntimeTests
{
    private sealed class HoteApi : WebApplicationFactory<ApiProgram>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Testing");
    }

    [Fact]
    public async Task Should_retirer_la_seule_occurrence_du_mardi_cible_et_garder_les_autres_When_on_supprime_cette_occurrence_via_le_canal_reel()
    {
        using var hote = new HoteApi();
        var client = hote.CreateClient();
        hote.Services.GetRequiredService<IEditeurActivites>().Ajouter("ecole", "ecole");
        hote.Services.GetRequiredService<IEditeurEnfants>().Ajouter("lea", "lea");

        // Given — une activité récurrente « ecole » chaque mardi pour Léa.
        var pose = await client.PostAsJsonAsync("/api/enfants/lea/activites/recurrentes", new
        {
            LieuId = "ecole",
            JourDeSemaine = DayOfWeek.Tuesday,
            HeureDebut = new TimeSpan(8, 30, 0),
            HeureFin = new TimeSpan(16, 30, 0),
        });
        Assert.True(pose.IsSuccessStatusCode, $"la pose doit aboutir, statut {(int)pose.StatusCode}.");
        var id = hote.Services.GetRequiredService<ISlotRecurrentRepository>().AllSnapshots().Single().Id;

        // When — on supprime l'occurrence du mardi 30/06/2026 précis via le canal d'écriture réel.
        var suppression = await client.DeleteAsync($"/api/enfants/lea/activites/recurrentes/{id}/occurrences/2026/6/30");
        Assert.True(suppression.IsSuccessStatusCode, $"la suppression d'occurrence doit aboutir, statut {(int)suppression.StatusCode}.");

        // Then — la grille projetée réellement (fenêtre dès le 24/06) ne porte plus « ecole » le mardi 30/06,
        // mais le porte le mardi suivant 07/07 (la série continue).
        var grille = await client.GetFromJsonAsync<GrilleAgenda>("/api/grille/2026/6/24");
        Assert.NotNull(grille);
        var mardiExclu = grille!.Jours.Single(j => j.Date == new DateOnly(2026, 6, 30));
        var mardiSuivant = grille!.Jours.Single(j => j.Date == new DateOnly(2026, 7, 7));
        Assert.DoesNotContain(mardiExclu.Slots, s => s.Libelle == "ecole");
        Assert.Contains(mardiSuivant.Slots, s => s.Libelle == "ecole");
    }
}
