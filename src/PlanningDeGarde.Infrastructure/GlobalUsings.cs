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

// Lot 2 (refacto hors-sprint) — l'AdapterDroite.InMemory est réorganisé en [BoundedContext]/[Technical]
// (dossier Classes/ supprimé, namespaces PlanningDeGarde.AdapterDroite.InMemory.<BC>.<Technical>).
// Ces global usings ré-exposent les sous-espaces pour les consommateurs (anti-churn).
global using PlanningDeGarde.AdapterDroite.InMemory.Activites.Repositories;
global using PlanningDeGarde.AdapterDroite.InMemory.Commun.Services;
global using PlanningDeGarde.AdapterDroite.InMemory.Comptes.Repositories;
global using PlanningDeGarde.AdapterDroite.InMemory.CyclesDeFond.Repositories;
global using PlanningDeGarde.AdapterDroite.InMemory.Echanges.Repositories;
global using PlanningDeGarde.AdapterDroite.InMemory.Enfants.Repositories;
global using PlanningDeGarde.AdapterDroite.InMemory.Foyer.Repositories;
global using PlanningDeGarde.AdapterDroite.InMemory.Notifications.Repositories;
global using PlanningDeGarde.AdapterDroite.InMemory.Periodes.Repositories;
global using PlanningDeGarde.AdapterDroite.InMemory.Slots.Repositories;
global using PlanningDeGarde.AdapterDroite.InMemory.Transferts.Repositories;

// Lot 3 (refacto hors-sprint) — l'AdapterDroite.Mongo est réorganisé en [BoundedContext]/[Technical]
// (dossier Classes/ supprimé, namespaces PlanningDeGarde.AdapterDroite.Mongo.<BC>.<Technical> ; documents
// embarqués sortis sous <BC>/DbModels). Ces global usings ré-exposent les dépôts (+ la migration Enfants)
// aux consommateurs (anti-churn). Les documents restent internal (jamais consommés hors de l'assembly Mongo).
global using PlanningDeGarde.AdapterDroite.Mongo.Activites.Repositories;
global using PlanningDeGarde.AdapterDroite.Mongo.Comptes.Repositories;
global using PlanningDeGarde.AdapterDroite.Mongo.CyclesDeFond.Repositories;
global using PlanningDeGarde.AdapterDroite.Mongo.Echanges.Repositories;
global using PlanningDeGarde.AdapterDroite.Mongo.Enfants.Migrations;
global using PlanningDeGarde.AdapterDroite.Mongo.Enfants.Repositories;
global using PlanningDeGarde.AdapterDroite.Mongo.Foyer.Repositories;
global using PlanningDeGarde.AdapterDroite.Mongo.Notifications.Repositories;
global using PlanningDeGarde.AdapterDroite.Mongo.Periodes.Repositories;
global using PlanningDeGarde.AdapterDroite.Mongo.Slots.Repositories;
global using PlanningDeGarde.AdapterDroite.Mongo.Transferts.Repositories;
