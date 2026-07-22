// Lot 1 (refacto hors-sprint) — l'Application est réorganisée en [BoundedContext]/[Technical]
// (dossiers Classes/ et Interfaces/ supprimés). Ces global usings ré-exposent les sous-espaces
// de noms pour que les consommateurs compilent sans éditer chaque fichier un par un.

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
global using PlanningDeGarde.Application.Foyer.Seed;
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

// Lot 3 (refacto hors-sprint) — l'AdapterDroite.Mongo est réorganisé en [BoundedContext]/[Technical]
// (dossier Classes/ supprimé, namespaces PlanningDeGarde.AdapterDroite.Mongo.<BC>.<Technical>).
// Les documents/DTO embarqués sont SORTIS sous <BC>/DbModels. Ces global usings ré-exposent les
// sous-espaces INTRA-assembly (anti-churn) : un dépôt voit ses documents et l'helper de sérialisation
// sans using par fichier. (Les documents restent `internal` : détail d'implémentation du store.)
global using PlanningDeGarde.AdapterDroite.Mongo.Commun.Serialization;
global using PlanningDeGarde.AdapterDroite.Mongo.Activites.DbModels;
global using PlanningDeGarde.AdapterDroite.Mongo.Comptes.DbModels;
global using PlanningDeGarde.AdapterDroite.Mongo.CyclesDeFond.DbModels;
global using PlanningDeGarde.AdapterDroite.Mongo.Echanges.DbModels;
global using PlanningDeGarde.AdapterDroite.Mongo.Enfants.DbModels;
global using PlanningDeGarde.AdapterDroite.Mongo.Foyer.DbModels;
global using PlanningDeGarde.AdapterDroite.Mongo.Notifications.DbModels;
global using PlanningDeGarde.AdapterDroite.Mongo.Periodes.DbModels;
global using PlanningDeGarde.AdapterDroite.Mongo.Slots.DbModels;
global using PlanningDeGarde.AdapterDroite.Mongo.Transferts.DbModels;
