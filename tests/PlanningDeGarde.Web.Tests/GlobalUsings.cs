// Lot 1 (refacto hors-sprint) — l'Application est réorganisée en [BoundedContext]/[Technical]
// (dossiers Classes/ et Interfaces/ supprimés). Ces global usings ré-exposent les sous-espaces
// de noms pour que les consommateurs compilent sans éditer chaque fichier un par un.
// NB : PlanningDeGarde.Application.Foyer.Seed est volontairement EXCLU ici (le type seed
// 'Foyer' collisionnerait avec PlanningDeGarde.Web.Foyer ; le front n'en dépend pas).

global using PlanningDeGarde.Application.Activites.Handlers;
global using PlanningDeGarde.Application.Activites.Ports;
global using PlanningDeGarde.Application.Commun.Ports;
global using PlanningDeGarde.Application.Comptes.Handlers;
global using PlanningDeGarde.Application.Comptes.Ports;
global using PlanningDeGarde.Application.CyclesDeFond.Handlers;
global using PlanningDeGarde.Application.CyclesDeFond.Ports;
global using PlanningDeGarde.Application.CyclesDeFond.Queries;
global using PlanningDeGarde.Application.Delegation.Handlers;
global using PlanningDeGarde.Application.Echanges.Handlers;
global using PlanningDeGarde.Application.Echanges.Ports;
global using PlanningDeGarde.Application.Enfants.Handlers;
global using PlanningDeGarde.Application.Enfants.Ports;
global using PlanningDeGarde.Application.Foyer.Handlers;
global using PlanningDeGarde.Application.Foyer.Models;
global using PlanningDeGarde.Application.Foyer.Ports;
global using PlanningDeGarde.Application.Foyer.Queries;
global using PlanningDeGarde.Application.Imprevus.Handlers;
global using PlanningDeGarde.Application.Notifications.Handlers;
global using PlanningDeGarde.Application.Notifications.Models;
global using PlanningDeGarde.Application.Notifications.Ports;
global using PlanningDeGarde.Application.Notifications.Queries;
global using PlanningDeGarde.Application.Notifications.Services;
global using PlanningDeGarde.Application.Periodes.Handlers;
global using PlanningDeGarde.Application.Periodes.Ports;
global using PlanningDeGarde.Application.Planning.Models;
global using PlanningDeGarde.Application.Planning.Queries;
global using PlanningDeGarde.Application.Slots.Handlers;
global using PlanningDeGarde.Application.Slots.Ports;
global using PlanningDeGarde.Application.Transferts.Handlers;
global using PlanningDeGarde.Application.Transferts.Ports;

// Lot 6 (refacto hors-sprint) — les composants Blazor de PlanningDeGarde.Web sont réorganisés
// par bounded context sous Components/<BC>/ (les dossiers plats Components/ et Components/Pages
// disparaissent ; Layout passe sous Shared/Layout). Ces global usings ré-exposent les nouveaux
// sous-espaces de noms pour que les tests bUnit (RenderComponent<PlanningPartage>, <Cloche>,
// dialogs…) résolvent les types sans éditer chaque fichier un par un.
// NB : PlanningDeGarde.Web.Components.Shared n'est PAS un global using (aucun test ne rend App/
// Legende/ModalConfig par type hors des rares fichiers qui l'importent déjà) ; on évite d'exposer
// le segment de namespace .Foyer comme simple nom pour ne pas masquer le type PlanningDeGarde.Web.Foyer.
global using PlanningDeGarde.Web.Components.Shared.Layout;
global using PlanningDeGarde.Web.Components.Planning;
global using PlanningDeGarde.Web.Components.Periodes;
global using PlanningDeGarde.Web.Components.Slots;
global using PlanningDeGarde.Web.Components.Transferts;
global using PlanningDeGarde.Web.Components.Delegation;
global using PlanningDeGarde.Web.Components.Echanges;
global using PlanningDeGarde.Web.Components.Imprevus;
global using PlanningDeGarde.Web.Components.Notifications;
global using PlanningDeGarde.Web.Components.Foyer;
global using PlanningDeGarde.Web.Components.Comptes;
