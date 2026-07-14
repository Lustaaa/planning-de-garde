using System.Collections.Generic;
using System.Linq;

namespace PlanningDeGarde.Domain;

/// <summary>
/// Agrégat de l'administration du foyer : l'ensemble des acteurs désignés <b>admins</b>. Porte
/// l'invariant métier PUR « <b>admin = parent</b> » (retour PO URGENT #3) : un acteur ne peut
/// rejoindre l'ensemble des admins QUE s'il est de type Parent. L'invariant borne le <b>type</b>,
/// pas l'unicité — plusieurs parents peuvent être admins. Reconstitué depuis un snapshot d'ids
/// (persistance derrière un port), muté par <see cref="DesignerAdmin"/> (Tell-Don't-Ask : c'est
/// l'agrégat qui décide, pas le use case).
/// </summary>
public sealed class AdministrationFoyer
{
    private readonly HashSet<string> _admins;

    private AdministrationFoyer(IEnumerable<string> admins)
    {
        _admins = admins.ToHashSet();
    }

    /// <summary>Reconstitue l'administration depuis l'ensemble d'ids d'admins persisté.</summary>
    public static AdministrationFoyer FromSnapshot(IEnumerable<string> admins) => new(admins);

    /// <summary>Les ids stables des acteurs admins du foyer (snapshot lecture seule, pour persistance).</summary>
    public IReadOnlyCollection<string> Admins => _admins.ToList();

    /// <summary>
    /// Désigne l'acteur comme admin du foyer. <b>Invariant admin=parent</b> : refusé (aucune mutation)
    /// si l'acteur n'est pas de type Parent. Sinon l'acteur rejoint l'ensemble des admins (idempotent
    /// sur un acteur déjà admin — un ensemble).
    /// </summary>
    public Result<string> DesignerAdmin(string acteurId, bool acteurEstParent)
    {
        if (!acteurEstParent)
            return Result<string>.Echec("l'admin doit être un parent");

        _admins.Add(acteurId);
        return Result<string>.Succes(acteurId);
    }

    /// <summary>
    /// Retire la désignation d'admin de l'acteur (sens OFF, s41). <b>Idempotent</b> : dé-désigner un
    /// acteur déjà non-admin est un no-op qui réussit (aucune mutation). Sinon l'acteur quitte
    /// l'ensemble des admins.
    /// </summary>
    public Result<string> DeDesignerAdmin(string acteurId)
    {
        // Idempotent : retirer un acteur absent de l'ensemble est un no-op qui réussit — et NE déclenche
        // PAS la borne « dernier admin » (aucun retrait effectif).
        if (!_admins.Contains(acteurId))
            return Result<string>.Succes(acteurId);

        // Borne défensive « dernier admin » (Sc.2) : refuser AVANT écriture de retirer le seul admin
        // restant — le foyer ne se retrouve JAMAIS sans admin (cohérent avec l'invariant admin=Parent).
        if (_admins.Count == 1)
            return Result<string>.Echec("le foyer doit garder au moins un admin");

        _admins.Remove(acteurId);
        return Result<string>.Succes(acteurId);
    }
}
