using System;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 49 — Sc.7 (🖥️ IHM, <c>@erreur</c>) — <b>Échap ANNULE</b> la sélection de plage par drag, via le port
/// <see cref="IEcouteurEchapModal"/> s33 (capture au niveau <b>document</b>, jamais un <c>@onkeydown</c> sur un
/// élément — qui passerait à vide en navigateur réel). Deux moments couverts :
/// <list type="number">
///   <item>Échap <b>pendant le drag</b> (ancre posée, surbrillance visible, avant tout mouseup) : la
///   surbrillance est retirée, aucune dialog ne s'ouvre, aucune écriture n'est émise (store intact).</item>
///   <item>Échap <b>sur une plage relâchée</b> (mouseup fait → dialog « Affecter une période » ouverte, AVANT
///   validation) : la dialog se ferme, l'état est vidé, aucune écriture n'est émise (store intact).</item>
/// </list>
/// L'écoute Échap est attachée PARESSEUSEMENT au premier drag (Attachements == 1) et rejouée via le spy du port
/// (double à la main). Grille <b>réellement câblée</b> à l'API distante (store réel) : le rempart anti « vert qui
/// ment » est la relecture directe du <see cref="IPeriodeRepository"/> distant — VIDE après annulation.
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmSelectionPlageDragEchapAnnuleTempsReelTests : TestContext
{
    private static readonly DateTime Mercredi_10_06_2026 = new(2026, 6, 10);

    /// <summary>Double à la main du port d'écoute Échap document (spy) : capte le callback d'attache et rejoue Échap.</summary>
    private sealed class EspionEchap : IEcouteurEchapModal
    {
        private Func<Task>? _onEchap;
        public int Attachements { get; private set; }

        public ValueTask<IAsyncDisposable> EcouterAsync(Func<Task> onEchap)
        {
            Attachements++;
            _onEchap = onEchap;
            return ValueTask.FromResult<IAsyncDisposable>(new Abonnement(this));
        }

        public Task DeclencherEchapDocument() => _onEchap?.Invoke() ?? Task.CompletedTask;

        private sealed class Abonnement : IAsyncDisposable
        {
            private readonly EspionEchap _espion;
            public Abonnement(EspionEchap espion) => _espion = espion;
            public ValueTask DisposeAsync() { _espion._onEchap = null; return ValueTask.CompletedTask; }
        }
    }

    [Fact]
    public void Echap_pendant_le_drag_retire_la_surbrillance_sans_ouvrir_de_dialog_ni_ecrire()
    {
        // Given — grille réelle câblée (store vierge), Parent, port Échap DOUBLÉ (spy).
        using var api = new ApiDistanteFactory();
        var espion = new EspionEchap();
        Services.AddSingleton<IEcouteurEchapModal>(espion);
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Mercredi_10_06_2026);

        // When — pointerdown sur J1 (09/06), survol (pointerover) jusqu'à J3 (11/06) : la surbrillance de plage
        // est visible et l'écoute Échap est armée PARESSEUSEMENT au premier drag (attache unique).
        this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "09/06").PointerDown());
        this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "11/06").PointerOver());
        grille.WaitForAssertion(
            () =>
            {
                Assert.Equal("1", GrilleRuntimeHarness.CaseDuJour(grille, "10/06").GetAttribute("data-plage-drag"));
                Assert.Equal(1, espion.Attachements);
            },
            TimeSpan.FromSeconds(10));

        // When — Échap document (rejoué via le callback capté par le spy), AVANT tout mouseup.
        this.SurDispatcher(() => espion.DeclencherEchapDocument().GetAwaiter().GetResult());

        // Then — la surbrillance est retirée, AUCUNE dialog de plage ne s'ouvre, et le store distant est INTACT (vide).
        grille.WaitForAssertion(
            () =>
            {
                Assert.Empty(grille.FindAll("[data-plage-drag='1']"));
                Assert.Empty(grille.FindAll("[data-testid='dialog-affecter-periode']"));
            },
            TimeSpan.FromSeconds(10));
        Assert.Empty(api.Services.GetRequiredService<IPeriodeRepository>().AllSnapshots());
    }

    [Fact]
    public void Echap_sur_une_plage_relachee_ferme_la_dialog_avant_validation_sans_ecrire()
    {
        // Given — grille réelle câblée (store vierge), Parent, ports Échap ET relâchement document DOUBLÉS (spies).
        using var api = new ApiDistanteFactory();
        var espion = new EspionEchap();
        Services.AddSingleton<IEcouteurEchapModal>(espion);
        var relachement = GrilleRuntimeHarness.DoublerRelachementPointeur(this);
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Mercredi_10_06_2026);

        // When — drag J1→J3 PUIS relâchement (pointerup document) : la dialog « Affecter une période » s'ouvre, pré-remplie.
        this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "09/06").PointerDown());
        this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "11/06").PointerOver());
        this.SurDispatcher(() => relachement.RelacherPointeurDocument().GetAwaiter().GetResult());
        grille.WaitForState(
            () => grille.FindAll("[data-testid='dialog-affecter-periode']").Count == 1,
            TimeSpan.FromSeconds(10));

        // When — Échap document AVANT de valider la dialog (plage relâchée, non confirmée).
        this.SurDispatcher(() => espion.DeclencherEchapDocument().GetAwaiter().GetResult());

        // Then — la dialog se ferme, l'état est vidé et AUCUNE écriture n'est émise : le store distant est INTACT (vide).
        grille.WaitForAssertion(
            () => Assert.Empty(grille.FindAll("[data-testid='dialog-affecter-periode']")),
            TimeSpan.FromSeconds(10));
        Assert.Empty(api.Services.GetRequiredService<IPeriodeRepository>().AllSnapshots());
    }
}
