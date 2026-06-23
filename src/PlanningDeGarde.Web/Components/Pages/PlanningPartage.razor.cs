using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;

namespace PlanningDeGarde.Web.Components.Pages;

/// <summary>
/// Vue centrale du planning partagé. Lit les read models (slots, périodes, transferts,
/// avertissements de chevauchement) et se rafraîchit en temps réel à la réception de
/// l'évènement SignalR. Toute écriture passe par un use case ; aucune règle métier ici.
/// </summary>
public partial class PlanningPartage
{
    private IReadOnlyList<SlotSnapshot> _slots = Array.Empty<SlotSnapshot>();
    private IReadOnlyList<PeriodeSnapshot> _periodes = Array.Empty<PeriodeSnapshot>();
    private IReadOnlyList<TransfertSnapshot> _transferts = Array.Empty<TransfertSnapshot>();
    private IReadOnlyList<DateTime> _joursAvecChevauchement = Array.Empty<DateTime>();
    private string? _responsableActuel;

    private string? _messageAction;
    private bool _actionReussie;

    // Édition inline d'une période : la base observée sert de jeton optimiste (Sc.10).
    private PeriodeSnapshot? _periodeEnEdition;
    private string _editResponsableId = "";
    private DateTime _editDebut;
    private DateTime _editFin;
    private string? _motifEditionPeriode;
    private bool _editionPeriodeReussie;

    private HubConnection? _hub;

    private string Desactive => Session.EstParent ? string.Empty : "disabled";

    private RoleAuteur RoleSelectionne
    {
        get => Session.Role;
        set { Session.Role = value; }
    }

    protected override void OnInitialized() => Charger();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        try
        {
            _hub = new HubConnectionBuilder()
                .WithUrl(Nav.ToAbsoluteUri("/hubs/planning"))
                .WithAutomaticReconnect()
                .Build();

            _hub.On(PlanningHub.EvenementMiseAJour, async () =>
            {
                Charger();
                await InvokeAsync(StateHasChanged);
            });

            await _hub.StartAsync();
        }
        catch
        {
            // Le temps réel est un confort : si le hub est indisponible, la vue reste
            // fonctionnelle (rechargement à la navigation / aux actions locales).
        }
    }

    private void Charger()
    {
        var enfant = Session.EnfantId;
        _slots = Slots.AllSnapshots()
            .Where(s => s.EnfantId == enfant)
            .OrderBy(s => s.Debut)
            .ToList();
        _periodes = Periodes.AllSnapshots().OrderBy(p => p.Debut).ToList();
        _transferts = Transferts.AllSnapshots().OrderBy(t => t.Date).ToList();
        _responsableActuel = Responsabilite.ResponsableAu(DateTime.Now);

        _joursAvecChevauchement = _slots
            .Select(s => s.Debut.Date)
            .Distinct()
            .Where(jour => JourneeEnfant.Chevauchements(enfant, jour).Count > 0)
            .OrderBy(j => j)
            .ToList();
    }

    private void DeplacerVers(SlotSnapshot slot, string? nouveauLieuId)
    {
        if (string.IsNullOrWhiteSpace(nouveauLieuId))
            return;

        var resultat = DeplacerSlot.Handle(
            new DeplacerSlotCommand(Session.Role, slot.EnfantId, slot.Debut, nouveauLieuId));

        _actionReussie = resultat.EstSucces;
        _messageAction = resultat.EstSucces
            ? $"Slot déplacé vers « {nouveauLieuId} »."
            : resultat.Motif;

        Charger();
    }

    private void OuvrirEditionPeriode(PeriodeSnapshot periode)
    {
        _periodeEnEdition = periode;          // jeton optimiste = état affiché par l'auteur
        _editResponsableId = periode.ResponsableId;
        _editDebut = periode.Debut;
        _editFin = periode.Fin;
        _motifEditionPeriode = null;
    }

    private void AnnulerEditionPeriode()
    {
        _periodeEnEdition = null;
        _motifEditionPeriode = null;
    }

    private void EnregistrerEditionPeriode()
    {
        if (_periodeEnEdition is null)
            return;

        var modification = new PeriodeSnapshot(_editResponsableId, _editDebut, _editFin);
        var resultat = ModifierPeriode.Handle(
            new ModifierPeriodeCommand(_periodeEnEdition, modification));

        _editionPeriodeReussie = resultat.EstSucces;
        if (resultat.EstSucces)
        {
            _motifEditionPeriode = null;
            _periodeEnEdition = null;
        }
        else
        {
            // État périmé : le Result invite à recharger ; on garde le formulaire ouvert.
            _motifEditionPeriode = resultat.Motif;
        }

        Charger();
    }

    private void RechargerPeriodes()
    {
        _periodeEnEdition = null;
        _motifEditionPeriode = null;
        Charger();
    }

    public async ValueTask DisposeAsync()
    {
        if (_hub is not null)
            await _hub.DisposeAsync();
    }
}
