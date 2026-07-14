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

    public void Creer(string compteId, string email, StatutCompte statut, string? acteurId, string? motDePasseHache = null)
        => _comptes[compteId] = new CompteUtilisateur(compteId, email, statut, acteurId, motDePasseHache);

    public void Desassocier(string compteId)
    {
        if (_comptes.TryGetValue(compteId, out var compte))
            _comptes[compteId] = compte with { ActeurId = null }; // repli propre, idempotent
    }

    public void Activer(string compteId)
    {
        if (_comptes.TryGetValue(compteId, out var compte))
            _comptes[compteId] = compte.Activer(); // mutation ciblée du statut, portée par l'agrégat
    }

    public void Desactiver(string compteId)
    {
        if (_comptes.TryGetValue(compteId, out var compte))
            _comptes[compteId] = compte.Desactiver(); // sens OFF s41, mutation ciblée du statut
    }

    public void RedefinirMotDePasse(string compteId, string motDePasseHache)
    {
        if (_comptes.TryGetValue(compteId, out var compte))
            _comptes[compteId] = compte with { MotDePasseHache = motDePasseHache }; // mutation ciblée du MDP
    }

    public IReadOnlyCollection<CompteUtilisateur> EnumererComptes() => _comptes.Values.ToList();
}
