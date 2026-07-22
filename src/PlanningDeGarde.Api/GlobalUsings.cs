// Lot 1 (refacto hors-sprint) — l'Application est réorganisée en [BoundedContext]/[Technical]
// (dossiers Classes/ et Interfaces/ supprimés). Ces global usings ré-exposent les sous-espaces
// de noms pour que les consommateurs compilent sans éditer chaque fichier un par un.

global using Microsoft.AspNetCore.Mvc;
global using PlanningDeGarde.Domain;
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
