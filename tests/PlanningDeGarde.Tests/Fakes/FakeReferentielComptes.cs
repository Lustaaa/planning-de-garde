using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Tests.Fakes;

/// <summary>
/// Doublure à la main du store du référentiel de comptes : réalise le port d'écriture
/// <see cref="IEditeurComptes"/> (créer) et la lecture <see cref="IEnumerationComptes"/> (énumérer)
/// sur un dictionnaire id→compte, pour asserter en unitaire que le handler a bien persisté le compte
/// via le port d'écriture sur l'identifiant généré.
/// </summary>
public sealed class FakeReferentielComptes : IEditeurComptes, IEnumerationComptes
{
    private readonly Dictionary<string, CompteUtilisateur> _comptes = new();

    public void Creer(string compteId, string email, StatutCompte statut, string acteurId)
        => _comptes[compteId] = new CompteUtilisateur(compteId, email, statut, acteurId);

    public void Desassocier(string compteId)
    {
        if (_comptes.TryGetValue(compteId, out var compte))
            _comptes[compteId] = compte with { ActeurId = null }; // repli propre, idempotent
    }

    public IReadOnlyCollection<CompteUtilisateur> EnumererComptes() => _comptes.Values.ToList();
}
