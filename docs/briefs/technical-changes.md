# Modification technique 

Après une revue humaine, il y a des points technique importants a revoir pour que l'application soit maintenable par un humain et d'avantage évolutive. 

## Remarqque générale

- est ce que c'est possible d'auto générer (a la compilation) la documentation technique du projet ? En se basant sur les commentaires "///" et le commentaire d'api ?  

## PlanningDeGarde.Api

- j'aimerai qu'on tende plus vers du rest full.
- est ce que tu peux dispatcher les routes dans des controller qui porte le meme nom
  - api/notifications => NotificationController dans un fichier .cs distint
  - api/foyer => FoyerController dans un fichier .cs distint
  - api/periodes => PériodeController dans un fichier .cs distint
  - ...
- 

## PlanningDeGarde.AdapterDroite.Mongo

Confirme que tu es bien dans une modélisation NoSQL.

- AdminsFoyerMongo me semble etre un repository est ce que tu peux reorganiser ces fichier dans un repertoire nommé Repository
- Classe embeded AdminDocument ne devrait pas etre dans un fichier a part dans un repertoire DbModels ?
- est ce que les fichier ne devrait pas etre trié dans des repertoires [BoundedContext]/[Technical]
  - ex : Slots/Repositories - Slots/DbModels - ...
  - et avoir des namespace du genre Mongo.Slots.Repositories ou Mongo.Slots.DbModels

## PlanningDeGarde.AdapterDroite.InMemory

- est ce que les fichier ne devrait pas etre trié dans des repertoires [BoundedContext]/[Technical]
  - ex : Slots/Repositories - Slots/DbModels - ...
  - et avoir des namespace du genre InMemory.Slots.Repositories ou InMemory.Slots.DbModels

## PlanningDeGarde.Application

Réorganiser l'ensemble des fichier cs en [BoundedContext]/[Technical]. Tu peux supprimer les dossiers Classes et Interfaces Existant
ex : Slots/Handlers - Activites/Interfaces

## PlanningDeGarde.Infrastructure

Ce projet ne devrait pas etre un adaptateur ? Ou etre scinder en plusieur adapter de droite ?
- Smtp
- Auth 

## PlanningDeGarde.Web

Réorganiser les composants par bounded context, ou me proposer une organusation. Peut etre ajouter un projet de librairie de composant.

## Les tests

Parmis tous les tests, je pense qu'il doit y avoir des doublons, est ce que tu peux les trouver, vérifier qu'ils sont bien en double et les supprimer ?