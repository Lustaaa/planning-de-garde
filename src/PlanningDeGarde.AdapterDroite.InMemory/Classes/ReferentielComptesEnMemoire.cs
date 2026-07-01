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

    public IReadOnlyCollection<CompteUtilisateur> EnumererComptes() => _comptes.Values.ToList();
}
