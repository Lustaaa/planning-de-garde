using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application.Comptes.Handlers;

/// <summary>
/// Commande de <b>définition</b> d'un mot de passe local sur un compte (s28, volet 2) : pose un mot de
/// passe (haché PBKDF2) sur un compte jusqu'ici sans mot de passe (email-only s23) ou en remplace le
/// condensat. Distincte de la redéfinition par jeton (volet 1) : ici la cible est directement le compte,
/// sans jeton de réinitialisation. Le mot de passe en clair ne transite que dans la commande, jamais ailleurs.
/// </summary>
public sealed record DefinirMotDePasseCommand(string CompteId, string MotDePasse);

/// <summary>Confirmation d'une définition de mot de passe aboutie (réponse sèche).</summary>
public sealed record MotDePasseDefini;

/// <summary>
/// Use case : poser un mot de passe local sur un compte. Hache le mot de passe (facteur PBKDF2 injecté)
/// et le persiste via le port d'écriture (<see cref="IEditeurComptes.RedefinirMotDePasse"/>) — le clair
/// n'est jamais stocké. Après quoi le compte devient connectable par « email + mot de passe »
/// (vérification côté <see cref="SeConnecterHandler"/>, inchangé).
/// </summary>
public sealed class DefinirMotDePasseHandler
{
    private readonly IEditeurComptes _editeur;
    private readonly IHacheurMotDePasse _hacheur;

    public DefinirMotDePasseHandler(IEditeurComptes editeur, IHacheurMotDePasse hacheur)
    {
        _editeur = editeur;
        _hacheur = hacheur;
    }

    public Result<MotDePasseDefini> Handle(DefinirMotDePasseCommand commande)
    {
        // Hachage à l'écriture (le clair n'est jamais persisté) puis pose du condensat sur le compte visé
        // via le port d'écriture. Le compte devient dès lors connectable par email + mot de passe.
        _editeur.RedefinirMotDePasse(commande.CompteId, _hacheur.Hacher(commande.MotDePasse));
        return Result<MotDePasseDefini>.Succes(new MotDePasseDefini());
    }
}
