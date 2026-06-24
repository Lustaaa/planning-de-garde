using System;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Tests.Builders;

/// <summary>
/// Builder de la commande de définition d'un transfert de bascule.
/// Valeurs par défaut = scénario nominal (Parent A dépose, Parent B récupère, école, 8h30 le 21/07).
/// </summary>
public sealed class TransfertBuilder
{
    private string _deposeParId = "parent-a";
    private string _recupereParId = "parent-b";
    private string _lieuId = "ecole";
    private TimeSpan _heure = new(8, 30, 0);
    private DateTime _date = new(2025, 7, 21, 0, 0, 0);

    public TransfertBuilder DeposePar(string id) { _deposeParId = id; return this; }
    public TransfertBuilder RecuperePar(string id) { _recupereParId = id; return this; }
    public TransfertBuilder AuLieu(string lieuId) { _lieuId = lieuId; return this; }
    public TransfertBuilder ALHeure(TimeSpan heure) { _heure = heure; return this; }
    public TransfertBuilder LeJour(DateTime date) { _date = date; return this; }

    public DefinirTransfertCommand Build() => new(_deposeParId, _recupereParId, _lieuId, _heure, _date);
}
