using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application.Foyer.Handlers;

/// <summary>
/// Commande de désignation d'un acteur comme admin du foyer (config). Le handler lit le type de
/// l'acteur (read-only, s14), reconstitue l'agrégat <see cref="AdministrationFoyer"/> depuis les
/// admins persistés, lui demande de désigner l'admin (invariant admin=parent), et ne persiste QUE
/// si l'invariant est tenu.
/// </summary>
public sealed record DesignerAdminCommand(string ActeurId);

/// <summary>Confirmation d'une désignation aboutie : l'id de l'acteur devenu admin du foyer.</summary>
public sealed record DesignerAdminResultat(string ActeurId);

/// <summary>
/// Use case : désigner un acteur comme admin du foyer. La règle admin=parent vit dans l'agrégat
/// Domain <see cref="AdministrationFoyer"/> (invariant PUR) ; le handler orchestre lecture du type,
/// décision de l'agrégat, puis persistance write-through en cas de succès.
/// </summary>
public sealed class DesignerAdminHandler
{
    private readonly IEnumerationAdminsFoyer _admins;
    private readonly IEditeurAdminsFoyer _editeur;
    private readonly IEnumerationActeursFoyer _acteurs;

    public DesignerAdminHandler(IEnumerationAdminsFoyer admins, IEditeurAdminsFoyer editeur, IEnumerationActeursFoyer acteurs)
    {
        _admins = admins;
        _editeur = editeur;
        _acteurs = acteurs;
    }

    public Result<DesignerAdminResultat> Handle(DesignerAdminCommand commande)
    {
        var administration = AdministrationFoyer.FromSnapshot(_admins.EnumererAdmins());
        var acteurEstParent = _acteurs.TypeDe(commande.ActeurId) == TypeActeur.Parent;

        var decision = administration.DesignerAdmin(commande.ActeurId, acteurEstParent);
        if (!decision.EstSucces)
            return Result<DesignerAdminResultat>.Echec(decision.Motif!);

        _editeur.DesignerAdmin(commande.ActeurId);
        return Result<DesignerAdminResultat>.Succes(new DesignerAdminResultat(commande.ActeurId));
    }
}
