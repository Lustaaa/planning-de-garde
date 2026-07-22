using System.Linq;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application.Foyer.Handlers;

/// <summary>
/// Commande de <b>dé-désignation</b> d'un acteur admin du foyer (sens OFF du toggle admin, —
/// débloque le verrou ON). Cible un acteur par son id stable et retire sa désignation d'admin
/// via l'agrégat <see cref="AdministrationFoyer"/> (Domain pur, Tell-Don't-Ask) puis le port
/// d'écriture <see cref="IEditeurAdminsFoyer"/>. Aucun nouvel agrégat, aucun store neuf : réutilise
/// le référentiel d'admins. Le sens ON (<see cref="DesignerAdminHandler"/>, invariant admin=parent)
/// reste strictement inchangé.
/// </summary>
public sealed record DeDesignerAdminCommand(string ActeurId);

/// <summary>Confirmation d'une dé-désignation aboutie : l'id de l'acteur qui n'est plus admin.</summary>
public sealed record DeDesignerAdminResultat(string ActeurId);

/// <summary>
/// Use case : retirer la désignation d'admin d'un acteur. Le handler refuse un acteur inconnu du
/// référentiel AVANT toute écriture (aucune mutation), reconstitue l'agrégat
/// <see cref="AdministrationFoyer"/> depuis les admins persistés, lui demande de dé-désigner
/// (idempotent si déjà non-admin, borne « dernier admin » portée par l'agrégat), et ne persiste QUE
/// si la décision réussit.
/// </summary>
public sealed class DeDesignerAdminHandler
{
    private readonly IEnumerationAdminsFoyer _admins;
    private readonly IEditeurAdminsFoyer _editeur;
    private readonly IEnumerationActeursFoyer _acteurs;

    public DeDesignerAdminHandler(IEnumerationAdminsFoyer admins, IEditeurAdminsFoyer editeur, IEnumerationActeursFoyer acteurs)
    {
        _admins = admins;
        _editeur = editeur;
        _acteurs = acteurs;
    }

    public Result<DeDesignerAdminResultat> Handle(DeDesignerAdminCommand commande)
    {
        // Garde « acteur inconnu » (Sc.1) : un id qui ne correspond à aucun acteur du référentiel est
        // refusé AVANT toute écriture — aucune mutation, aucun admin fantôme retiré. Résolution sur les
        // ids lus du référentiel d'acteurs.
        if (!_acteurs.EnumererActeurs().Contains(commande.ActeurId))
            return Result<DeDesignerAdminResultat>.Echec("acteur introuvable");

        var administration = AdministrationFoyer.FromSnapshot(_admins.EnumererAdmins());

        var decision = administration.DeDesignerAdmin(commande.ActeurId);
        if (!decision.EstSucces)
            return Result<DeDesignerAdminResultat>.Echec(decision.Motif!);

        _editeur.DeDesignerAdmin(commande.ActeurId);
        return Result<DeDesignerAdminResultat>.Succes(new DeDesignerAdminResultat(commande.ActeurId));
    }
}
