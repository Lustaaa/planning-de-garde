# Programme de refonte technique — plan & suivi

Refonte technique **hors-sprint** (bypass BDD) pilotée par le PO depuis
`docs/briefs/technical-changes.md`. Traitée par lots dispatchés séparément à l'`architecte`.
Chaque lot : comportement inchangé, non-régression = **suite complète verte** (skill `dotnet`,
Docker actif), branche `ia-refacto/lotN-…`, pas de merge/PR (le PO gère la porte git).

## Suivi des lots

- [x] **Lot 1 — `PlanningDeGarde.Application`** : réorganisation `[BoundedContext]/[Technical]`,
      suppression des dossiers `Classes/` et `Interfaces/`, namespaces alignés. (920/920 vert)
- [ ] **Lot 2 — `PlanningDeGarde.Api`** : tendre vers REST, un `XxxController` par ressource
      (`api/notifications` → `NotificationsController`, `api/foyer` → `FoyerController`, …), un
      fichier `.cs` par contrôleur.
- [ ] **Lot 3 — `PlanningDeGarde.Infrastructure`** : le scinder en adaptateurs de droite par techno
      (Smtp, Auth) plutôt qu'un fourre-tout ; clarifier son statut d'adaptateur vs composition root.
- [ ] **Lot 4 — `PlanningDeGarde.AdapterDroite.Mongo`** : arbo `[BC]/[Technical]`
      (`Slots/Repositories`, `Slots/DbModels`, …), namespaces `Mongo.Slots.Repositories`, sortir les
      documents embarqués (`AdminDocument`) dans `DbModels`, repos dans `Repositories`.
- [ ] **Lot 5 — `PlanningDeGarde.AdapterDroite.InMemory`** : même arbo `[BC]/[Technical]`,
      namespaces `InMemory.Slots.Repositories`, etc.
- [ ] **Lot 6 — `PlanningDeGarde.Web`** : réorganiser les composants par bounded context ;
      envisager un projet de librairie de composants.
- [ ] **Lot 7 — Tests** : traquer les doublons, prouver qu'ils sont réellement en double, supprimer.
- [ ] **Lot 8 — Doc-gen** : auto-générer (à la compilation) la doc technique depuis les commentaires
      `///` + commentaires d'API.

---

## Carte des bounded contexts (FIGÉE — à réutiliser par les lots InMemory/Mongo/Web)

Catégories techniques `[Technical]` : `Handlers` (commande + handler), `Queries`, `Ports`
(interfaces = ports gauche/droite), `Models` (read models / enums / valeurs), + ponctuellement
`Services` (service applicatif) et `Seed` (données d'amorçage). Namespaces alignés sur les dossiers :
`PlanningDeGarde.Application.<BC>.<Technical>`.

| Bounded context | Handlers | Queries | Ports | Autres |
|---|---|---|---|---|
| **Planning** | — | GrilleAgendaQuery, ResponsabiliteQuery, JourneeEnfantQuery, PeriodesDuJourQuery, SlotsDuJourQuery | — | Models : GrilleAgenda (+ read models JourCase/SlotCase/EntreeLegende…), VuePlanning |
| **Periodes** | AffecterPeriode, EditerPeriode, ModifierPeriode, SupprimerPeriode | — | IPeriodeRepository | — |
| **Slots** | PoserSlot, PoserSlotRecurrent, DeplacerSlot, SupprimerSlot, SupprimerSlotRecurrent | — | ISlotRepository, ISlotRecurrentRepository | — |
| **Transferts** | DefinirTransfert | — | ITransfertRepository | — |
| **CyclesDeFond** ⚠ | DefinirCycle | CyclesFoyerQuery | IReferentielCycleDeFond | — |
| **Delegation** | DeleguerRecuperation, AnnulerDelegation | — | — | — |
| **Echanges** | ProposerEchange, ProposerEchangeSuiteImprevu, AccepterProposition, RefuserProposition | — | IPropositionEchangeRepository | — |
| **Imprevus** | SignalerImprevu | — | — | — |
| **Notifications** | MarquerNotificationsLues | FluxNotificationsQuery, DigestImmediatQuery | INotificateurChangement, IJournalChangements, IEtatLectureNotifications | Models : DigestImmediat (+ JourDigest/ResponsableDuJour…) · Services : JournalChangementsDiffusant |
| **Enfants** | AjouterEnfant, EditerEnfant, LierEnfantParent, DelierEnfantParent | — | IEnumerationEnfants, IEditeurEnfants (read model EnfantFoyer) | — |
| **Activites** | AjouterActivite, EditerActivite, SupprimerActivite, LierEnfantActivite, DelierEnfantActivite | — | IEnumerationActivites, IEditeurActivites | — |
| **Foyer** | AjouterActeur, EditerActeur, SupprimerActeur, AffecterRoleActeur, RetirerRoleActeur, CreerRole, RenommerRole, SupprimerRole, MarquerRoleParent, DesignerAdmin, DeDesignerAdmin | GrapheFoyerQuery, ResoudreActeurParDefautQuery | IReferentielResponsables, IPaletteCouleurs, IEditeurConfigurationFoyer, IEditeurReferentielRoles, IEnumerationRoles, IEnumerationAdminsFoyer, IEditeurAdminsFoyer, IEnumerationActeursFoyer, IResponsableRepository (read model RoleFoyer) | Models : TypeActeur, RoleDuLien, StatutCoupleR3, AvertissementChevauchement · **Seed** : `Foyer` (référentiel d'amorçage) |
| **Comptes** | CreerCompte, CreerCompteLibreService, ActiverCompte, DesactiverCompte, SeConnecter, DefinirMotDePasse, RedefinirMotDePasse, DemanderRecuperationMotDePasse, ConnexionOAuth | — | IEnumerationComptes, IEditeurComptes, IReferentielJetonsReset, IHacheurMotDePasse, IFournisseurOAuth, IEnvoiMail | — |
| **Commun** | — | — | IDateTimeProvider, INotificateurPlanning (port de diffusion transverse) | — |

⚠ **CyclesDeFond** (au pluriel) et non `CycleDeFond` : le segment de namespace **masquerait**
le type du Domain `PlanningDeGarde.Domain.CycleDeFond` (erreur CS0118). Les lots InMemory/Mongo
DOIVENT utiliser le même segment `CyclesDeFond`.

## Notes pour les lots suivants

1. **Alignement obligatoire sur cette carte** : InMemory (lot 5) et Mongo (lot 4) rangent leurs
   repos/documents sous les **mêmes** bounded contexts (`Slots/Repositories`, `Foyer/DbModels`, …).
2. **Seed `Foyer`** : ce référentiel statique vivait en `namespace PlanningDeGarde.Infrastructure`
   **tout en étant physiquement dans l'assembly Application** (anomalie historique). Lot 1 l'a
   recalé en `PlanningDeGarde.Application.Foyer.Seed`. Si le lot Infrastructure (3) veut relocaliser
   les données d'amorçage, repartir de là. Un seul consommateur qualifiait le type en dur
   (`AucunLibelleFictifExposeTests`, alias corrigé).
3. **Stratégie global usings** : chaque projet consommateur porte un `GlobalUsings.cs` qui
   ré-expose les sous-espaces de noms Application (anti-churn : pas d'édition fichier par fichier).
   **Web** et **Web.Tests** EXCLUENT `PlanningDeGarde.Application.Foyer.Seed` : le type seed `Foyer`
   collisionnerait avec `PlanningDeGarde.Web.Foyer`. Les lots Mongo/InMemory produiront leurs propres
   sous-namespaces `Mongo.<BC>.*` / `InMemory.<BC>.*` ; prévoir la même mécanique pour la composition
   root (Infrastructure) si besoin.
4. **Types read-model dupliqués côté Web** (`Foyer`, `RoleFoyer`, `EnfantFoyer`, `ActeurFoyer`…) :
   ils masquent leurs homonymes Application par précédence de même namespace ; certains tests
   désambiguïsent en pleinement qualifié (`PlanningDeGarde.Application.Enfants.Ports.EnfantFoyer`).
   À garder en tête pour le lot Web (6), qui pourrait unifier ces doublons.
5. **Références pleinement qualifiées** : un changement de namespace casse aussi les usages
   `PlanningDeGarde.Application.<Type>` en dur (pas couverts par les global usings). Les rechercher
   à chaque lot (`grep "PlanningDeGarde.<Assembly>.<Type>"`).
