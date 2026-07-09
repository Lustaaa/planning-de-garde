using System.Collections.Generic;
using System.Threading.Tasks;
using PlanningDeGarde.Web;

namespace PlanningDeGarde.Web.Tests;

/// <summary>Espion À LA MAIN du port <see cref="IPersistanceSession"/> (seul le port est doublé) : mémorise
/// les jetons persistés et sert celui amorcé à la relecture. Sert les scénarios de persistance/restauration
/// de session du sprint 31 (F5). Mime un stockage durable client (localStorage) sans navigateur.</summary>
internal sealed class SpyPersistanceSession : IPersistanceSession
{
    private SessionPersistee? _stocke;

    public SpyPersistanceSession(SessionPersistee? amorce = null) => _stocke = amorce;

    public List<SessionPersistee> JetonsPersistes { get; } = new();

    public SessionPersistee? DernierJetonPersiste => JetonsPersistes.Count == 0 ? null : JetonsPersistes[^1];

    public ValueTask PersisterAsync(SessionPersistee jeton)
    {
        JetonsPersistes.Add(jeton);
        _stocke = jeton;
        return ValueTask.CompletedTask;
    }

    public ValueTask<SessionPersistee?> LireAsync() => ValueTask.FromResult(_stocke);
}

/// <summary>Double INERTE du port <see cref="IPersistanceSession"/> pour les tests qui rendent
/// <see cref="PlanningDeGarde.Web.Components.Pages.Connexion"/> sans se soucier de la persistance (rien à
/// persister, stockage vide). Évite d'imposer une vraie persistance là où le scénario ne l'observe pas.</summary>
internal sealed class PersistanceSessionInerte : IPersistanceSession
{
    public ValueTask PersisterAsync(SessionPersistee jeton) => ValueTask.CompletedTask;

    public ValueTask<SessionPersistee?> LireAsync() => ValueTask.FromResult<SessionPersistee?>(null);
}
