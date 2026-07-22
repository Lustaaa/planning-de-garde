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
- [x] **Lot 3 — `PlanningDeGarde.Infrastructure`** : scindé en adaptateurs de droite par techno ;
      `Infrastructure` CONSERVÉ comme **composition root** (câblage DI), pas un adaptateur. (920/920 vert) —
      livré comme **4e lot exécuté** du programme, sur la branche `ia-refacto/lot1-application-bc-technical`.
      **Décisions/écarts** :
      - **2 nouveaux projets `src/`** (portés à **10**), namespaces alignés sur le style adaptateur existant
        (`net10.0`/`Nullable`/`ImplicitUsings`, `ProjectReference` vers `Application` pour les ports) :
        - `PlanningDeGarde.AdapterDroite.Smtp` — reçoit `EnvoiMailSmtp` (`IEnvoiMail`). **Aucun PackageReference** :
          `System.Net.Mail` (`SmtpClient`/`MailMessage`) est dans la BCL du framework partagé.
        - `PlanningDeGarde.AdapterDroite.Securite` — reçoit `HacheurMotDePassePbkdf2` (`IHacheurMotDePasse`,
          PBKDF2 = BCL) et `FournisseurOAuthGoogleNonCable` (`IFournisseurOAuth`, **placeholder inchangé** :
          la dette de câblage OAuth réel reste ouverte, non branchée). Organisés en sous-dossiers `MotDePasse/`
          et `OAuth/` sous le **namespace unique** `PlanningDeGarde.AdapterDroite.Securite` (1 seul segment DI
          à ré-exposer ; choix arrêté par le PO).
      - **`Infrastructure` reste distinct** = composition root : garde `ServiceCollectionExtensions`
        (`AjouterPlanningDeGarde`) + `GlobalUsings.cs`, référence désormais Smtp + Securite (en plus d'InMemory,
        Mongo, SignalR, Application). Aucune ligne de DI modifiée dans son comportement (déplacement + plomberie).
      - **`using PlanningDeGarde.Application;` → `...Comptes.Ports`** dans les 3 fichiers déplacés : hors de la
        composition root ils ne bénéficient plus des global usings d'Infrastructure ; les ports
        `IEnvoiMail`/`IHacheurMotDePasse`/`IFournisseurOAuth`/`IdentiteExterne` vivent en `Application.Comptes.Ports`
        depuis le lot 1.
      - **Ripple / anti-churn** : global usings `AdapterDroite.Smtp` + `AdapterDroite.Securite` ajoutés au
        `GlobalUsings.cs` d'`Infrastructure` (résolution dans `ServiceCollectionExtensions`), de `PlanningDeGarde.Tests`
        (consomme `HacheurMotDePassePbkdf2`) et d'`Api.Tests` (consomme `EnvoiMailSmtp` + `HacheurMotDePassePbkdf2`).
        **Assemblies atteintes en transitif** (Tests → Infrastructure ; Api.Tests → Api → Infrastructure → Smtp/Securite) —
        **aucune ProjectReference ajoutée aux tests** : reprend à l'identique le pattern des lots Mongo/InMemory
        (les types Mongo sont consommés pareil) et l'esprit de la consigne Api (« ne pas toucher les références si le
        transitif suffit »). `FournisseurOAuthGoogleNonCable` n'est consommé par AUCUN test (seul le fake OAuth l'est).
      - **`slnx`** : les 2 projets ajoutés (le skill `dotnet` recompile toute la solution).
- [x] **Lot 4 — `PlanningDeGarde.AdapterDroite.Mongo`** : arbo `[BC]/[Technical]`, namespaces
      `PlanningDeGarde.AdapterDroite.Mongo.<BC>.<Technical>`, dossier `Classes/` supprimé, documents
      embarqués sortis dans `DbModels`. (920/920 vert, dont 108 Api.Tests sur Mongo RÉEL — compat données
      prouvée) — livré comme **3e lot exécuté** du programme, sur la branche
      `ia-refacto/lot1-application-bc-technical`. **Décisions/écarts** :
      - **17 dépôts** rangés sous `<BC>/Repositories/` (ns `...Mongo.<BC>.Repositories`), alignés sur la
        carte figée (dont `CyclesDeFond` au pluriel).
      - **15 documents embarqués SORTIS** (1 fichier/type) sous `<BC>/DbModels/` (ns `...Mongo.<BC>.DbModels`),
        passés de `private sealed` à **`internal sealed`** (visibilité minimale : détail d'implémentation du
        store, jamais consommé hors assembly) : `AdminDocument`, `ActeurDocument`, `RoleDocument` (Foyer) ;
        `CycleDocument`, `AffectationDocument` (CyclesDeFond) ; `LectureDocument`, `EvenementDocument`
        (Notifications) ; `PeriodeDocument` (Periodes) ; `PropositionDocument` (Echanges) ; `SlotDocument`,
        `SlotRecurrentDocument` (Slots) ; `TransfertDocument` (Transferts) ; `ActiviteDocument` (Activites) ;
        `CompteDocument`, `JetonDocument` (Comptes) ; `EnfantDocument` (Enfants). **Attributs BSON / noms de
        collection INCHANGÉS** (invariant de compat des données existantes).
      - **`DateTimeMongo`** (helper de sérialisation wall-clock) → `Commun/Serialization/`
        (ns `...Mongo.Commun.Serialization`), `internal static` inchangé.
      - **`MigrationRetroAffectationEnfantsMongo`** (migration de données, pas un dépôt) → `Enfants/Migrations/`
        (ns `...Mongo.Enfants.Migrations`).
      - **Masquage `Foyer`** (rejeu du cas InMemory lot 5) : seul `ConfigurationFoyerMongo` lit le seed
        (`Foyer.CouleurNeutre` / `TypesParActeur` / `TypeParDefaut`) ; alias `using Foyer = ...Seed.Foyer;`
        **scopé dans le namespace** file-scoped. Les autres fichiers Foyer ne lisent pas le seed. `CyclesDeFond`
        déjà au pluriel → aucun masquage du type Domain.
      - **Anomalie corrigée en passant** : les fichiers vivaient en `namespace PlanningDeGarde.Infrastructure`
        (comme le seed en lot 1, l'InMemory en lot 5) ; recalés en `...Mongo.<BC>.<Technical>`.
      - **Ripple** : `Infrastructure` (ServiceCollectionExtensions cite les 17 dépôts) + `Api.Tests` (nombreux
        `*MongoTests`/durabilité + migration). Anti-churn : `global using ...Mongo.<BC>.Repositories;`
        (+ `.Enfants.Migrations`) ajoutés aux `GlobalUsings.cs` d'`Infrastructure` et `Api.Tests`. INTRA-assembly,
        le `GlobalUsings.cs` du projet Mongo ré-expose `...<BC>.DbModels` + `...Commun.Serialization` (les
        dépôts voient leurs documents, les documents voient `DateTimeMongo`, sans using par fichier). Aucune
        réf pleinement qualifiée `PlanningDeGarde.Infrastructure.<MongoType>` ni référence aux documents côté
        tests. `PlanningDeGarde.Tests` / `Web` / `Web.Tests` intacts (ne citent aucun type Mongo).
- [x] **Lot 5 — `PlanningDeGarde.AdapterDroite.InMemory`** : arbo `[BC]/[Technical]`,
      namespaces `PlanningDeGarde.AdapterDroite.InMemory.<BC>.Repositories`, dossier `Classes/`
      supprimé. (920/920 vert) — livré comme 2e lot exécuté du programme, sur la branche
      `ia-refacto/lot1-application-bc-technical`. **Décisions/écarts** :
      - Aucun type d'état/document embarqué (1 classe adaptateur par fichier) → **pas de dossier
        `DbModels/`/`Models/`** (rien à y ranger, on n'invente pas de dossier vide) ; tous les
        dépôts sous `Repositories/`.
      - `SystemDateTimeProvider` (BC **Commun**) n'est pas un dépôt : rangé sous `Commun/Services`
        (`...InMemory.Commun.Services`) — catégorie technique cohérente pour un adaptateur d'horloge.
      - **Anomalie corrigée en passant** : ces fichiers vivaient tous en `namespace
        PlanningDeGarde.Infrastructure` (comme le seed `Foyer` en lot 1) ; recalés en
        `...InMemory.<BC>.Repositories`.
      - **Masquage `Foyer`** (miroir du cas `CycleDeFond`) : le segment de namespace
        `...InMemory.Foyer` masque la classe seed `PlanningDeGarde.Application.Foyer.Seed.Foyer`
        (CS0234 sur `Foyer.Activites`, `Foyer.CouleurNeutre`, …). Un **global using alias** ne
        suffit pas (niveau unité de compilation → perd face au membre de namespace externe) : alias
        `using Foyer = ...Seed.Foyer;` **scopé dans le namespace** (après la déclaration file-scoped)
        des 7 fichiers qui lisent le seed (BC Foyer + `ReferentielActivitesEnMemoire`). **À
        rejouer pour le lot Mongo (4)** si ses fichiers Foyer lisent aussi le seed.
      - **Ripple** : seul `Infrastructure` référence l'assembly (les tests l'ont en transitif et le
        voyaient via `using PlanningDeGarde.Infrastructure;`). Anti-churn : `global using
        PlanningDeGarde.AdapterDroite.InMemory.<BC>.Repositories;` ajoutés aux `GlobalUsings.cs` de
        `Infrastructure`, `PlanningDeGarde.Tests`, `PlanningDeGarde.Api.Tests`. `Web`/`Web.Tests`
        intacts (ne référencent pas l'Infrastructure ; n'en citent les types qu'en commentaire).
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
6. **Masquage du seed `Foyer` par le segment de namespace `.Foyer`** (constaté au lot InMemory) :
   tout fichier rangé sous un namespace contenant `.Foyer` et qui lit la classe seed
   `PlanningDeGarde.Application.Foyer.Seed.Foyer` (ex. `Foyer.Activites`, `Foyer.CouleurNeutre`)
   ne compile plus (le segment de namespace masque le type). Un global using alias **ne suffit
   pas** (unité de compilation → perd face au membre de namespace externe) : poser
   `using Foyer = PlanningDeGarde.Application.Foyer.Seed.Foyer;` **dans** le namespace file-scoped
   du fichier. Idem si un jour un BC porte le segment `.CycleDeFond` (déjà évité via `CyclesDeFond`).
   Le lot **Mongo (4)** a vérifié ce point : seul `ConfigurationFoyerMongo` lit le seed → alias scopé posé,
   les autres fichiers Foyer/DbModels ne le lisent pas.
