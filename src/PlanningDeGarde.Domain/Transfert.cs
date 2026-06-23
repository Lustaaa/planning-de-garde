using System;

namespace PlanningDeGarde.Domain;

/// <summary>
/// Snapshot immuable d'un transfert — frontière publique pour assertions / persistance.
/// </summary>
public sealed record TransfertSnapshot(string DeposeParId, string RecupereParId, string LieuId, TimeSpan Heure, DateTime Date);

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

    private Transfert(string deposeParId, string recupereParId, string lieuId, TimeSpan heure, DateTime date)
    {
        _deposeParId = deposeParId;
        _recupereParId = recupereParId;
        _lieuId = lieuId;
        _heure = heure;
        _date = date;
    }

    public static Result<Transfert> Definir(string deposeParId, string recupereParId, string lieuId, TimeSpan heure, DateTime date)
    {
        var complet = !string.IsNullOrWhiteSpace(deposeParId)
            && !string.IsNullOrWhiteSpace(recupereParId)
            && !string.IsNullOrWhiteSpace(lieuId)
            && heure != TimeSpan.Zero;
        if (!complet)
            return Result<Transfert>.Echec(
                "Transfert incomplet : la récupération et l'heure sont requises.");

        return Result<Transfert>.Succes(new Transfert(deposeParId, recupereParId, lieuId, heure, date));
    }

    public TransfertSnapshot ToSnapshot() => new(_deposeParId, _recupereParId, _lieuId, _heure, _date);
}
