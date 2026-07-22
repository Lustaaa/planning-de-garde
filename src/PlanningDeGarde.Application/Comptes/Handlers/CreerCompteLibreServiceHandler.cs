using System;
using System.Linq;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application.Comptes.Handlers;

/// <summary>
/// Commande d'inscription <b>libre-service</b> : un visiteur « qui n'a pas encore de
/// compte » s'enregistre lui-même avec un email et un mot de passe. Distincte de la création admin 
/// (<see cref="CreerCompteCommand"/>, qui exige un acteur déclaré) : ici PAS d'acteur — l'association
/// et l'activation viennent plus tard. Le mot de passe est haché avant persistance.
/// </summary>
public sealed record CreerCompteLibreServiceCommand(string Email, string MotDePasse);

/// <summary>
/// Use case : inscription libre-service. Valide l'email et le mot de passe requis, hache le mot de
/// passe, génère un identifiant stable neuf opaque, puis persiste un compte <b>Inactif</b> sans acteur
/// (ActeurId null) via le port d'écriture du référentiel. L'unicité de l'email est portée par.
/// </summary>
public sealed class CreerCompteLibreServiceHandler
{
    private readonly IEnumerationComptes _comptes;
    private readonly IEditeurComptes _editeur;
    private readonly IHacheurMotDePasse _hacheur;

    public CreerCompteLibreServiceHandler(IEnumerationComptes comptes, IEditeurComptes editeur, IHacheurMotDePasse hacheur)
    {
        _comptes = comptes;
        _editeur = editeur;
        _hacheur = hacheur;
    }

    public Result<CreerCompteResultat> Handle(CreerCompteLibreServiceCommand commande)
    {
        // Garde « email requis » (Sc.9, garde s22 étendue) : email vide / tout-espaces refusé AVANT
        // toute génération d'id et toute écriture — aucun compte non résolvable persisté.
        if (string.IsNullOrWhiteSpace(commande.Email))
            return Result<CreerCompteResultat>.Echec("email requis");

        // Garde « mot de passe requis » (Sc.9) : un compte libre-service DOIT porter un facteur
        // d'authentification — mot de passe vide / tout-espaces refusé avant écriture.
        if (string.IsNullOrWhiteSpace(commande.MotDePasse))
            return Result<CreerCompteResultat>.Echec("mot de passe requis");

        // Garde « email déjà utilisé » (Sc.10, invariant email unique s22) : refus si un compte porte
        // déjà cet email — AVANT toute génération d'id et toute écriture. Aucun doublon, aucun écrasement
        // du compte existant (le référentiel reste inchangé). Unicité lue sur le référentiel courant.
        if (_comptes.EnumererComptes().Any(c => c.Email == commande.Email))
            return Result<CreerCompteResultat>.Echec("email déjà utilisé");

        // Identifiant stable neuf OPAQUE (jamais dérivé de l'email, anti-pattern s06). Le compte naît
        // Inactif (défaut s22), sans acteur (ActeurId null — association ultérieure), mot de passe haché.
        var compteId = $"compte-{Guid.NewGuid():N}";
        _editeur.Creer(compteId, commande.Email, StatutCompte.Inactif, null, _hacheur.Hacher(commande.MotDePasse));
        return Result<CreerCompteResultat>.Succes(new CreerCompteResultat(compteId));
    }
}
