using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Tests.Fakes;

/// <summary>
/// Doublure à la main du store du référentiel de rôles : réalise le port d'écriture
/// <see cref="IEditeurReferentielRoles"/> (créer) et la lecture <see cref="IEnumerationRoles"/>
/// (énumérer) sur un dictionnaire id→libellé, pour asserter en unitaire que le handler a bien
/// persisté le rôle via le port d'écriture sur l'identifiant généré.
/// </summary>
public sealed class FakeReferentielRoles : IEditeurReferentielRoles, IEnumerationRoles
{
    private readonly Dictionary<string, string> _libelles = new();

    public void Creer(string roleId, string libelle) => _libelles[roleId] = libelle;

    public void Renommer(string roleId, string nouveauLibelle) => _libelles[roleId] = nouveauLibelle;

    public IReadOnlyCollection<RoleFoyer> EnumererRoles()
        => _libelles.Select(kv => new RoleFoyer(kv.Key, kv.Value)).ToList();
}
