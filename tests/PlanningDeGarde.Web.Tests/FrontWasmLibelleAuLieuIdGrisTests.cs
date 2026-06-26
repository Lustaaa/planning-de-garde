extern alias api;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using AffecterPeriodeRequete = api::PlanningDeGarde.Api.CanalEcriture.AffecterPeriodeRequete;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.8 (🖥️ scénario IHM, <c>@erreur</c>) — diagnostic du défaut (B).
/// Ce scénario <b>caractérise le gris-BUG</b> : quand la <b>source</b> fournit le <b>libellé</b>
/// « Parent A » comme <c>ResponsableId</c> au lieu de l'<b>identifiant stable</b> <c>parent-a</c>, la
/// case retombe sur <b>gris</b> <b>alors qu'un responsable y est affecté</b> — clé absente du set
/// <c>CouleursParActeur</c>. La projection est correcte (gris sur id absent, contrat
/// <see cref="IPaletteCouleurs"/>) ; le défaut est la <b>source</b>, corrigée au Sc.6.
///
/// Harnais runtime réutilisé du Sc.6 : l'<b>API distante réelle</b> (<see cref="ApiDistanteFactory"/>,
/// store réel singleton, palette réelle <c>parent-a→bleu</c>, projection réelle
/// <see cref="GrilleAgendaQuery"/>) reçoit deux affectations <b>réelles</b> via le canal, sur des jours
/// distincts : l'une avec le <b>libellé</b> « Parent A » (source NON corrigée), l'autre avec l'<b>id
/// stable</b> « parent-a » (source corrigée, Sc.6). On observe au niveau du <b>canal réel</b> que le
/// libellé produit le gris-bug, distinct de la couleur réelle de l'id stable.
///
/// Anti « vert qui ment » : on observe le canal réel (qui reçoit le <c>ResponsableId</c>) et la palette
/// réelle — un bUnit à doublure de transport ne verrait pas que le canal reçoit « Parent A ». La
/// discriminance : la case du libellé est <b>couverte par une période</b> (donc le gris vient du repli
/// sur clé absente, pas d'une absence de période) et <b>diverge</b> de la case bleue de l'id stable.
/// </summary>
public sealed class FrontWasmLibelleAuLieuIdGrisTests
{
    // Affectation avec le LIBELLÉ « Parent A » (source non corrigée) — 24/06/2026.
    private static readonly AffecterPeriodeRequete AffectationLibelle =
        new("Parent A", new DateTime(2026, 6, 24), new DateTime(2026, 6, 24));

    // Affectation avec l'IDENTIFIANT STABLE « parent-a » (source corrigée, Sc.6) — 27/06/2026.
    private static readonly AffecterPeriodeRequete AffectationIdStable =
        new("parent-a", new DateTime(2026, 6, 27), new DateTime(2026, 6, 27));

    [Fact]
    public async Task Should_Laisser_grise_la_case_du_24_06_2026_alors_qu_un_responsable_y_est_affecte_When_la_source_du_front_envoie_le_libelle_Parent_A_au_lieu_de_l_identifiant_stable_parent_a()
    {
        // Given — l'API distante réelle (palette réelle parent-a→bleu) ; un front WASM émet vers elle.
        using var apiDistante = new ApiDistanteFactory();
        var clientFront = apiDistante.CreateClient();

        // When — deux affectations RÉELLES via le canal : le libellé « Parent A » (24/06) et l'id
        // stable « parent-a » (27/06). Les deux périodes existent réellement dans le store distant.
        var reponseLibelle = await clientFront.PostAsJsonAsync("api/canal/affecter-periode", AffectationLibelle);
        var reponseIdStable = await clientFront.PostAsJsonAsync("api/canal/affecter-periode", AffectationIdStable);
        Assert.True(reponseLibelle.IsSuccessStatusCode, "l'affectation par libellé doit être enregistrée (la période existe).");
        Assert.True(reponseIdStable.IsSuccessStatusCode, "l'affectation par id stable doit être enregistrée.");

        // … la grille projetée à la semaine du lundi 22/06/2026 (projection + palette réelles).
        using var scope = apiDistante.Services.CreateScope();
        var projection = scope.ServiceProvider.GetRequiredService<GrilleAgendaQuery>();
        var palette = scope.ServiceProvider.GetRequiredService<IPaletteCouleurs>();
        var grille = projection.Projeter(new DateOnly(2026, 6, 22));

        var caseLibelle = grille.Jours.Single(j => j.Date == new DateOnly(2026, 6, 24));
        var caseIdStable = grille.Jours.Single(j => j.Date == new DateOnly(2026, 6, 27));

        // Then — la case du 24/06 est GRISE alors qu'un responsable (« Parent A », le libellé) y est
        // affecté : le libellé n'est pas une clé du set → repli neutre (gris-bug).
        Assert.Equal(palette.CouleurNeutre, caseLibelle.CouleurResponsable);

        // … et ce gris TRAHIT le libellé fourni à la place de l'id stable : l'id stable « parent-a »
        // (Sc.6) colore réellement sa case (non neutre), donc la divergence vient bien de la SOURCE
        // (mauvais identifiant), pas de la projection ni d'une absence de période.
        Assert.NotEqual(palette.CouleurNeutre, caseIdStable.CouleurResponsable);
        Assert.NotEqual(caseLibelle.CouleurResponsable, caseIdStable.CouleurResponsable);
    }
}
