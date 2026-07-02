using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Infrastructure;

/// <summary>
/// Store mutable en mémoire du référentiel des comptes utilisateurs du foyer (nouveau petit agrégat
/// de config foyer, miroir du référentiel de rôles s21). Réalise le port de lecture
/// <see cref="IEnumerationComptes"/> et le port d'écriture <see cref="IEditeurComptes"/> sur un
/// dictionnaire id→compte. Volatile (re-parti vide au redémarrage) — le remplaçant durable est
/// <c>ReferentielComptesMongo</c> ; la résolution reste sur l'identifiant stable opaque, jamais sur
/// l'email.
/// </summary>
public sealed class ReferentielComptesEnMemoire : IEnumerationComptes, IEditeurComptes
{
    private readonly Dictionary<string, CompteUtilisateur> _comptes = new();

    public void Creer(string compteId, string email, StatutCompte statut, string acteurId)
        => _comptes[compteId] = new CompteUtilisateur(compteId, email, statut, acteurId);

    public void Desassocier(string compteId)
    {
        // Repli propre : le compte survit, sans acteur (ActeurId null). Tolérant à l'absence / à un
        // compte déjà désassocié (no-op qui réussit — idempotence Sc.6).
        if (_comptes.TryGetValue(compteId, out var compte))
            _comptes[compteId] = compte with { ActeurId = null };
    }

    public void Activer(string compteId)
    {
        // Mutation ciblée du seul statut, portée par l'agrégat (Tell-Don't-Ask). Tolérant à l'absence.
        if (_comptes.TryGetValue(compteId, out var compte))
            _comptes[compteId] = compte.Activer();
    }

    public IReadOnlyCollection<CompteUtilisateur> EnumererComptes() => _comptes.Values.ToList();
}
