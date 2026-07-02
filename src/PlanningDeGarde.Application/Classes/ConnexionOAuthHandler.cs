using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application;

/// <summary>
/// Commande de traitement d'un <b>callback OAuth</b> (volet 4, s25) : le retour du provider externe
/// (Google/Microsoft/Apple) est passé au serveur qui le résout en identité puis ouvre une session. Le
/// callback opaque est fourni tel quel ; sa résolution est déléguée au port <see cref="IFournisseurOAuth"/>.
/// </summary>
public sealed record CallbackOAuthCommand(string Callback);

/// <summary>
/// Use case : connexion via OAuth externe. Résout le callback en une identité externe (email vérifié)
/// via le port <see cref="IFournisseurOAuth"/>, puis ouvre la session par le <b>MÊME chemin</b> que la
/// connexion locale s23 — il délègue à <see cref="SeConnecterHandler"/> (résolution compte→acteur,
/// contrôle Actif, ancrage de l'identité réelle sur l'acteur du compte, cf. Sc.5). AUCUN agrégat durable
/// neuf : OAuth est un facteur d'entrée supplémentaire branché DEVANT le même chemin de session.
/// </summary>
public sealed class ConnexionOAuthHandler
{
    private readonly IFournisseurOAuth _fournisseur;
    private readonly SeConnecterHandler _seConnecter;

    public ConnexionOAuthHandler(IFournisseurOAuth fournisseur, SeConnecterHandler seConnecter)
    {
        _fournisseur = fournisseur;
        _seConnecter = seConnecter;
    }

    public Result<SessionOuverte> Handle(CallbackOAuthCommand commande)
    {
        // Le provider (via le port) restitue l'identité externe : email vérifié servant de clé de
        // résolution vers un compte du foyer — même clé que la connexion locale s23.
        var identite = _fournisseur.ResoudreIdentite(commande.Callback);

        // Garde « identité non résolue » (Sc.15) : un callback dont le provider ne restitue aucune
        // identité exploitable est refusé, sans session — pas de délégation avec un email absent.
        if (identite is null)
            return Result<SessionOuverte>.Echec("identité OAuth non résolue");

        // Ouverture de session par le MÊME chemin s23 : le handler local résout le compte sur l'email,
        // contrôle son activation et ancre l'identité réelle sur son acteur (Sc.5). OAuth = facteur
        // d'entrée, pas un nouveau chemin de session (aucun agrégat durable neuf). Les refus « email
        // inconnu » / « compte non activé » (Sc.15) sont ceux de s23/s24, propagés tels quels.
        return _seConnecter.Handle(new SeConnecterCommand(identite.Email));
    }
}
