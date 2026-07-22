using System;

namespace PlanningDeGarde.Domain;

/// <summary>
/// Snapshot immuable d'un transfert — frontière publique pour assertions / persistance. <paramref name="EnfantId"/>
/// SCOPE le transfert saisi à un enfant : il n'apparaît que dans la grille de CET enfant. Absent (<c>""</c>)
/// = transfert partagé/legacy (mono-enfant antérieur).
/// </summary>
public sealed record TransfertSnapshot(string DeposeParId, string RecupereParId, string LieuId, TimeSpan Heure, DateTime Date, string EnfantId = "");

/// <summary>
/// Agrégat « point de bascule A↔B » : à ce transfert, le déposant remet l'enfant au récupérant.
/// Invariant : dépose + récupère + lieu + heure tous renseignés.
/// </summary>
public sealed class Transfert
{
    private readonly string _deposeParId;
    private readonly string _recupereParId;
    private readonly string _lieuId;
    private readonly TimeSpan _heure;
    private readonly DateTime _date;
    private readonly string _enfantId;

    private Transfert(string deposeParId, string recupereParId, string lieuId, TimeSpan heure, DateTime date, string enfantId)
    {
        _deposeParId = deposeParId;
        _recupereParId = recupereParId;
        _lieuId = lieuId;
        _heure = heure;
        _date = date;
        _enfantId = enfantId;
    }

    /// <summary>Définit un transfert de bascule. <paramref name="enfantId"/> SCOPE le transfert à un
    /// enfant (hérité de la vue courante, Option A) ; absent (<c>""</c>) = partagé/legacy.</summary>
    public static Result<Transfert> Definir(string deposeParId, string recupereParId, string lieuId, TimeSpan heure, DateTime date, string enfantId = "")
    {
        var complet = !string.IsNullOrWhiteSpace(deposeParId)
            && !string.IsNullOrWhiteSpace(recupereParId)
            && !string.IsNullOrWhiteSpace(lieuId)
            && heure != TimeSpan.Zero;
        if (!complet)
            return Result<Transfert>.Echec(
                "Transfert incomplet : la récupération et l'heure sont requises.");

        return Result<Transfert>.Succes(new Transfert(deposeParId, recupereParId, lieuId, heure, date, enfantId));
    }

    public TransfertSnapshot ToSnapshot() => new(_deposeParId, _recupereParId, _lieuId, _heure, _date, _enfantId);
}
