# Programme de refonte technique — plan & suivi

Refonte technique **hors-sprint** (bypass BDD) pilotée par le PO depuis
`docs/briefs/technical-changes.md`. Traitée par lots dispatchés séparément à l'`architecte`.
Chaque lot : comportement inchangé, non-régression = **suite complète verte** (skill `dotnet`,
Docker actif), branche `ia-refacto/lotN-…`, pas de merge/PR (le PO gère la porte git).

## Synthèse finale — programme COMPLET (8/8)

Les 8 lots sont livrés verts sur la branche `ia-refacto/lot1-application-bc-technical` :
réorganisation `[BoundedContext]/[Technical]` de `Application` (1), `Mongo` (4) et `InMemory` (5) ;
Api passée en **REST + controllers MVC** par ressource (2) ; `Infrastructure` scindée en adaptateurs
de droite `Smtp`/`Securite` et conservée comme composition root (3) ; composants `Web` rangés par
bounded context (6) ; audit doublons de tests (7, 920→917) ; **doc technique auto-générée DocFX** (8).
Comportement d'exécution inchangé sur tout le programme ; suite complète **917/917 verte** au dernier lot.

## Suivi des lots

- [x] **Lot 1 — `PlanningDeGarde.Application`** : réorganisation `[BoundedContext]/[Technical]`,
      suppression des dossiers `Classes/` et `Interfaces/`, namespaces alignés. (920/920 vert)
- [x] **Lot 2 — `PlanningDeGarde.Api`** : **REST complet + controllers MVC** (décision PO). Quitté les
      minimal-APIs `MapPost` groupées (`CanalEcriture`/`CanalLecture`/`OAuthEndpoints` supprimés) pour
      **18 controllers `[ApiController]` attribute-routed, un `.cs` par ressource / bounded context**,
      routes ressource + verbes HTTP (POST create/action, PUT/DELETE id-en-chemin). Handlers/queries,
      diffusion SignalR et DI **strictement inchangés** ; front (`PlanningDeGarde.Web`) et tests
      (Api.Tests + Web.Tests) rebranchés sur les nouvelles routes/verbes. (920/920 vert, dont 108
      Api.Tests sur Mongo réel) — livré sur la branche `ia-refacto/lot1-application-bc-technical`.
      **Décisions/écarts** :
      - **DTO** : records de requête migrés en `Dtos/Requetes.cs` (namespace `PlanningDeGarde.Api`,
        records **top-level**, plus nichés dans une classe statique) ; les ids portés par l'URL sont
        **retirés du corps** (records `*Corps` réduits) ; read models en `Dtos/Vues.cs`. Forme JSON des
        champs restants **inchangée** (compat front) ; les tests référençant `CanalLecture.ActeurFoyerVue`
        recalés en `ActeurFoyerVue`, l'alias `api::…CanalEcriture.PoserSlotRequete` en `api::…PoserSlotRequete`.
      - **Contrat de réponse préservé** : `AddControllers` **retire le `StringOutputFormatter`** pour que
        `BadRequest(motif)` reste **`application/json` (`"motif"`)** comme `Results.BadRequest(string)` des
        minimal-APIs (sinon MVC l'écrit en `text/plain` et le front, qui lit `ReadFromJsonAsync<string>`,
        ne décode plus le motif). Codes retour identiques (Ok / BadRequest+motif) ; DELETE/PUT là où c'est
        naturel (pas de réécriture en 201/204).
      - **Diffusion** : chaque controller ré-appelle `INotificateurPlanning`/`INotificateurChangement`
        **exactement là où l'ancien endpoint le faisait** (audit endpoint par endpoint ; ex. `DELETE
        /api/slots/{id}` notifie, `POST /api/slots` non — le handler diffuse).
      - **Front** : les helpers génériques `AppliquerToggle`/`PosterActivite`/`PosterEnfant` (qui ne
        savaient que POSTer un corps) généralisés en `Func<Task<HttpResponseMessage>>` (PUT/DELETE inline).
      - **Tests injoignables** : le `EcritureInjoignableHandler` (harnais Web) coupait un `POST` finissant
        par un suffixe ; élargi à tout **write (POST/PUT/DELETE)** dont le chemin **contient** un fragment
        de ressource ; les gardes « 0 écriture » détectent désormais un write par **méthode** (POST/PUT/DELETE
        vers `/api/`) plutôt que par le préfixe `/api/canal`.
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
- [x] **Lot 6 — `PlanningDeGarde.Web`** : composants Blazor réorganisés **par bounded context**
      sous `Components/<BC>/` (**décision PO : dossiers par BC, PAS de RCL** — aucun nouveau projet).
      (920/920 vert, dont 347 Web.Tests bUnit) — livré sur la branche `ia-refacto/lot1-application-bc-technical`.
      **Arbo retenue** (`.razor` + partial `.razor.cs` déplacés PAR PAIRE, namespace = dossier
      `PlanningDeGarde.Web.Components.<BC>`) :
      - `Components/Shared/` : `App`, `Legende`, `ModalConfig` + `Shared/Layout/` (`BasculeTheme`,
        `MainLayout`, `MenuUtilisateur`, `NavMenu` — sous-dossier Layout conservé, ns `...Shared.Layout`).
      - `Components/Planning/` : `PlanningPartage`, `Home`.
      - `Components/Periodes/` : `AffecterPeriodeDialog`, `EditerPeriodeDialog`, `SupprimerPeriodeDialog`.
      - `Components/Slots/` : `PoserSlotDialog`, `SupprimerSlotDialog`.
      - `Components/Transferts/` : `DefinirTransfertDialog`.
      - `Components/Delegation/` : `DeleguerRecuperationDialog`, `ReprendreJourDialog`.
      - `Components/Echanges/` : `ProposerEchangeDialog`. · `Components/Imprevus/` : `SignalerImprevuDialog`.
      - `Components/Notifications/` : `Cloche`. · `Components/Foyer/` : `ConfigurationFoyer`.
      - `Components/Comptes/` : `Connexion`, `MotDePasseOublie`, `ReinitialiserMotDePasse`.
      - Les dossiers plats `Components/` et `Components/Pages/` **disparaissent** ; `Components/Layout/`
        passe sous `Shared/Layout/`.
      **Décisions/écarts** :
      - **Anti-churn (résolution des tags Blazor)** : les nouveaux namespaces `@using
        PlanningDeGarde.Web.Components.<BC>` sont ajoutés **au `Components/_Imports.razor`** (global au
        dossier) — aucun `.razor` édité un par un. `_Imports` remplace `...Components.Layout` par
        `...Components.Shared.Layout` et garde `...Components` (parent).
      - **Routes INCHANGÉES** : les `@page "..."` ne bougent pas (le déplacement change le namespace,
        pas l'URL) — zéro régression de navigation.
      - **`Program.cs`** : `using PlanningDeGarde.Web.Components;` → `...Components.Shared` (composant
        racine `App` déplacé sous Shared).
      - **Ripple `Web.Tests` (347)** : anti-churn via `GlobalUsings.cs` de test qui ré-expose les
        sous-namespaces BC (`...Planning`, `...Periodes`, …, `...Foyer`, `...Comptes`, `...Shared.Layout`) ;
        `Shared` (App/Legende/ModalConfig) N'EST PAS mis en global (2 fichiers rendant `App` portent un
        `using` per-file) pour ne pas exposer le segment `.Foyer` comme simple nom et masquer le type
        `PlanningDeGarde.Web.Foyer` (cf. note 6). Les `using ...Components.Pages;` (129) devenus
        inexistants sont **retirés** (types couverts par les global usings), `...Components.Layout` (6)
        → `...Components.Shared.Layout`, refs pleinement qualifiées `Web.Components.Pages.PlanningPartage`
        (code + `<see cref>`) recalées en `...Components.Planning.PlanningPartage`.
      - **Gardes d'asset (habillage/tokenisation)** : 10 tests lisent des `.razor`/`.razor.css` **par
        chemin codé en dur** (`Components/<X>.razor`) — chemins recalés vers `Components/<BC>/…` (dont un
        `switch` dialog→BC dans `FrontWasmDialogsHabillageCoherentTests`). Contenu des feuilles inchangé.
      - **Read-models front dupliqués** (`Foyer`, `RoleFoyer`, `EnfantFoyer`, `ActeurFoyer`,
        `SlotDuJourVue`, `PeriodeDuJourVue`, `CycleFoyer`…) : **NON unifiés** — Web et Api sont des
        déployables séparés (le front consomme l'API distante avec ses propres formes JSON) ; partager
        les DTO les coupleraient. Laissés en l'état (dette assumée, cf. note ci-dessous).
      - **Services/utilitaires à la racine du projet** (`CanalEcriture`, `ClientCanalEcriture`,
        `MessagesEcriture`, `Foyer`, `State/`, thème, écouteurs interop, session) : **laissés à plat**
        (namespace `PlanningDeGarde.Web`) — les ranger changerait le namespace et rippl­erait dans de
        nombreux `.razor.cs`/tests pour un gain faible ; hors du périmètre « simple et sûr » retenu.
- [x] **Lot 7 — Tests (audit doublons)** : audit conservateur des ~920 tests / 3 projets. **3 doublons
      confirmés supprimés** (920 → 917), tous entre `Sprint41Sc1_DeDesignerAdmin` et
      `Sprint41Sc2_BorneDernierAdmin` (même couche Domain/App, faits byte-identiques). Table d'audit
      ci-dessous. Livré sur la branche `ia-refacto/lot1-application-bc-technical`.

  **Démarche** : (1) doublons de NOM de méthode de test au sein d'un même projet → tous des helpers
  (`Dispose`, `NotifierMiseAJour`, builders) ou des noms partagés ciblant des SUT différents (dialogs
  Deleguer/Echange/Imprevu, plage vs jour) = intentionnels. (2) Similarité fichier-entier (Jaccard sur
  lignes normalisées, même projet) : **max 0.75**, aucune copie littérale — les paires proches sont des
  features sœurs (fond vs neutre, éditer vs supprimer, nominal vs plage). (3) Similarité **fait par fait**
  (corps de méthode normalisé, même projet) : **exactement 3 paires à 1.00**, toutes Sc1↔Sc2 ci-dessous ;
  aucune autre ≥ 0.95 dans toute la suite.

  | Paire de faits examinée | Couche | Verdict | Preuve / gardien |
  |---|---|---|---|
  | `Sc2.Domain_Should_Reussir_When_il_reste_au_moins_un_admin_apres_le_retrait` ↔ `Sc1.Domain_Should_Retirer_l_admin_cible_When_on_le_de_designe` | Domain pur | **DOUBLON_CONFIRME (supprimé de Sc2)** | Setup `FromSnapshot([ParentA,ParentB])`, act `DeDesignerAdmin(ParentA)`, asserts `EstSucces` + `DoesNotContain(ParentA)` + `Contains(ParentB)` **identiques**. Gardien : Sc1 (dé-désignation nominale). |
  | `Sc2.Domain_Should_Rester_no_op_When_l_acteur_deja_non_admin_et_un_seul_autre_admin_subsiste` ↔ `Sc1.Domain_Should_Reussir_en_no_op_sans_mutation_When_l_acteur_est_deja_non_admin` | Domain pur | **DOUBLON_CONFIRME (supprimé de Sc2)** | Setup `FromSnapshot([ParentB])`, act `DeDesignerAdmin(ParentA)`, asserts `EstSucces` + `Single` + `Contains(ParentB)` **identiques**. Gardien : Sc1 (idempotence no-op). |
  | `Sc2.Acceptation_Should_Reussir_When_on_de_designe_l_un_de_deux_admins` ↔ `Sc1.Acceptation_Should_Retirer_l_admin_et_persister_When_on_de_designe_un_acteur_parmi_plusieurs_admins` | Application (handler + store InMemory) | **DOUBLON_CONFIRME (supprimé de Sc2)** | Deux admins A+B, `Handle(DeDesignerAdminCommand(A))`, asserts `EstSucces` + `DoesNotContain(A)` + `Contains(B)` **identiques**. Gardien : Sc1 (persistance nominale). |
  | `Sc2` conserve `Domain_Should_Refuser_..._dernier_admin` + `Acceptation_Should_Refuser_..._dernier_admin` | Domain + App | **GARDÉ (unique)** | Cœur de la borne « dernier admin » — non couvert par Sc1. |
  | `Scenario44_S3_DeleguerDelegataireInconnu` ↔ `Scenario45_S3_DeleguerPlageDelegataireInconnu` | App | **PROCHE_MAIS_DISTINCT** | 0.86 : commandes distinctes (délégation d'un jour vs d'une plage `[J1..J2]`). |
  | `*CanalApiTests` / `*MongoIntegrationTests` / `Scenario*` ↔ `Scenario*MongoDurabiliteTests` / `FrontWasm*` | couches ≠ | **INTENTIONNEL_COUCHES** | Domaine/app vs contrat API vs store Mongo réel vs IHM runtime = stratégie backend-d'abord + acceptation runtime. |
  | ~30 paires fichier 0.55–0.75 (fond/neutre, éditer/supprimer, convergence 2 écrans, nominal/plage) | idem | **PROCHE_MAIS_DISTINCT / SUSPECT_NON_CONCLUANT** | Setups/assertions divergents (cas limite ≠ nominal). Gardés (conservatisme). |

  **Bilan** : familles examinées = 3 projets, 899 faits parsés ; verdicts = 3 `DOUBLON_CONFIRME`,
  reste `INTENTIONNEL_COUCHES` / `PROCHE_MAIS_DISTINCT`. Aucune couverture perdue (chaque comportement
  supprimé reste prouvé à l'identique par son gardien Sc1, même couche). Total **920 → 917**.
- [x] **Lot 8 — Doc-gen** : auto-générer la doc technique depuis les commentaires `///` (+ chemin
      d'intégration OpenAPI documenté). Outil **DocFX** en tool local épinglé (`.config/dotnet-tools.json`,
      v2.78.5) → génération reproductible (`dotnet tool restore && dotnet docfx docfx/docfx.json`).
      Génération **réellement prouvée** : 377 pages de référence API produites dans `docfx/_site/`
      (0 erreur). (917/917 vert) — livré sur la branche `ia-refacto/lot1-application-bc-technical`.
      **Décisions/écarts** :
      - **Émission XML activée via `src/Directory.Build.props`** (un seul fichier, pas 10 `.csproj`),
        placé dans `src/` pour ne cibler QUE les projets de production (les projets `tests/` ne
        remontent pas jusqu'à lui). `<GenerateDocumentationFile>true</...>` + `<NoWarn>$(NoWarn);CS1591</...>`
        (aucun projet `src/` n'a `TreatWarningsAsErrors` → build non cassé, mais CS1591 masqué pour une
        sortie propre et de la robustesse).
      - **Config DocFX** sous `docfx/` (`docfx.json` métadonnées→`api/`, `toc.yml`, `index.md`). Sortie
        générée (`docfx/_site/`, `docfx/api/`, `docfx/obj/`) **git-ignorée** : on ne versionne que la config.
      - **`PlanningDeGarde.Web` EXCLU de l'extraction** : DocFX lit les métadonnées via Roslyn sans
        exécuter le générateur de source Razor → les partials `.razor.cs` (Blazor) ne trouvent pas leur
        base `ComponentBase` (CS0115/CS0234). Écarté via `exclude` ; son `.xml` reste émis. Périmètre
        couvert : Domain, Application, adaptateurs droite, SignalR, Api, Infrastructure.
      - **OpenAPI** : exposé à l'exécution (`/openapi/v1.json` + Scalar `/scalar`), donc pas de `.json`
        statique au build → intégration au site DocFX **documentée** (comment produire le `.json` puis le
        référencer) plutôt que câblée. Cf. `docs/documentation-technique.md`.

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

## Lot Api : mapping des routes (ANCIENNE minimal-API → NOUVELLE REST/controller)

Verbes : **POST** = création / action ; **PUT** = mise à jour complète (id en chemin) ; **DELETE** =
suppression (id en chemin) ; **GET** = lecture. Sémantique CQRS conservée (écritures = handlers inchangés,
lectures = queries, diffusion SignalR lecture seule). Codes retour identiques (Ok / BadRequest+motif JSON).

| Ancienne route (POST) | Nouvelle route | Controller |
|---|---|---|
| `/api/canal/poser-slot` | `POST /api/slots` | `SlotsController` |
| `/api/canal/poser-slot-recurrent` | `POST /api/slots/recurrents` | `SlotsController` |
| `/api/canal/supprimer-slot` | `DELETE /api/slots/{id}` | `SlotsController` |
| `/api/canal/supprimer-slot-recurrent` | `DELETE /api/slots/recurrents/{id}` | `SlotsController` |
| (lecture) `/api/slots/{a}/{m}/{j}` | `GET /api/slots/{annee}/{mois}/{jour}` | `SlotsController` |
| `/api/canal/affecter-periode` | `POST /api/periodes` | `PeriodesController` |
| `/api/canal/editer-periode` | `PUT /api/periodes/{id}` | `PeriodesController` |
| `/api/canal/supprimer-periode` | `DELETE /api/periodes/{id}` | `PeriodesController` |
| (lecture) `/api/periodes/{a}/{m}/{j}` | `GET /api/periodes/{annee}/{mois}/{jour}` | `PeriodesController` |
| `/api/canal/definir-transfert` | `POST /api/transferts` | `TransfertsController` |
| `/api/canal/deleguer-recuperation` | `POST /api/delegations` | `DelegationsController` |
| `/api/canal/annuler-delegation` | `DELETE /api/delegations?jour=&enfant=` | `DelegationsController` |
| `/api/canal/definir-cycle` | `PUT /api/foyer/cycles` | `CyclesController` |
| (lecture) `/api/foyer/cycles` | `GET /api/foyer/cycles?enfant=` | `CyclesController` |
| `/api/canal/proposer-echange` | `POST /api/propositions` | `PropositionsController` |
| `/api/canal/proposer-echange-suite-imprevu` | `POST /api/propositions/suite-imprevu` | `PropositionsController` |
| `/api/canal/accepter-proposition` | `POST /api/propositions/{id}/acceptation` | `PropositionsController` |
| `/api/canal/refuser-proposition` | `POST /api/propositions/{id}/refus` | `PropositionsController` |
| `/api/canal/signaler-imprevu` | `POST /api/imprevus` | `ImprevusController` |
| `/api/canal/marquer-notifications-lues` | `POST /api/notifications/lues` | `NotificationsController` |
| (lecture) `/api/notifications/{u}` | `GET /api/notifications/{utilisateurId}` | `NotificationsController` |
| `/api/canal/ajouter-enfant` | `POST /api/foyer/enfants` | `EnfantsController` |
| `/api/canal/editer-enfant` | `PUT /api/foyer/enfants/{id}` | `EnfantsController` |
| `/api/canal/lier-enfant-parent` | `PUT /api/foyer/enfants/{id}/parents/{acteurId}` | `EnfantsController` |
| `/api/canal/delier-enfant-parent` | `DELETE /api/foyer/enfants/{id}/parents/{acteurId}` | `EnfantsController` |
| (lecture) `/api/foyer/enfants` | `GET /api/foyer/enfants` | `EnfantsController` |
| `/api/canal/ajouter-activite` | `POST /api/foyer/activites` | `ActivitesController` |
| `/api/canal/editer-activite` | `PUT /api/foyer/activites/{id}` | `ActivitesController` |
| `/api/canal/supprimer-activite` | `DELETE /api/foyer/activites/{id}` | `ActivitesController` |
| `/api/canal/lier-enfant-activite` | `PUT /api/foyer/activites/{id}/enfants/{enfantId}` | `ActivitesController` |
| `/api/canal/delier-enfant-activite` | `DELETE /api/foyer/activites/{id}/enfants/{enfantId}` | `ActivitesController` |
| (lecture) `/api/foyer/activites` | `GET /api/foyer/activites` | `ActivitesController` |
| `/api/canal/ajouter-acteur` | `POST /api/foyer/acteurs` | `ActeursController` |
| `/api/canal/editer-acteur` | `PUT /api/foyer/acteurs/{id}` | `ActeursController` |
| `/api/canal/supprimer-acteur` | `DELETE /api/foyer/acteurs/{id}` | `ActeursController` |
| `/api/canal/affecter-role` | `PUT /api/foyer/acteurs/{id}/role` | `ActeursController` |
| `/api/canal/retirer-role` | `DELETE /api/foyer/acteurs/{id}/role` | `ActeursController` |
| (lecture) `/api/foyer/acteurs` | `GET /api/foyer/acteurs` | `ActeursController` |
| `/api/canal/creer-role` | `POST /api/foyer/roles` | `RolesController` |
| `/api/canal/renommer-role` | `PUT /api/foyer/roles/{id}` | `RolesController` |
| `/api/canal/supprimer-role` | `DELETE /api/foyer/roles/{id}` | `RolesController` |
| `/api/canal/marquer-role-parent` | `PUT /api/foyer/roles/{id}/parent` | `RolesController` |
| (lecture) `/api/foyer/roles` | `GET /api/foyer/roles` | `RolesController` |
| `/api/canal/designer-admin` | `PUT /api/foyer/admins/{acteurId}` | `AdminsController` |
| `/api/canal/de-designer-admin` | `DELETE /api/foyer/admins/{acteurId}` | `AdminsController` |
| (lecture) `/api/foyer/admins` | `GET /api/foyer/admins` | `AdminsController` |
| `/api/canal/creer-compte` | `POST /api/foyer/comptes` | `ComptesController` |
| `/api/canal/activer-compte` | `POST /api/foyer/comptes/{id}/activation` | `ComptesController` |
| `/api/canal/desactiver-compte` | `DELETE /api/foyer/comptes/{id}/activation` | `ComptesController` |
| `/api/canal/definir-mot-de-passe` | `PUT /api/foyer/comptes/{id}/mot-de-passe` | `ComptesController` |
| `/api/canal/demander-recuperation` | `POST /api/comptes/recuperation` | `ComptesController` |
| `/api/canal/redefinir-mot-de-passe` | `POST /api/comptes/reinitialisation` | `ComptesController` |
| (lecture) `/api/foyer/comptes` | `GET /api/foyer/comptes` | `ComptesController` |
| `/api/canal/se-connecter` | `POST /api/session` | `SessionController` |
| (lecture) `/api/foyer/graphe` | `GET /api/foyer/graphe` | `FoyerController` |
| (lecture) `/api/grille/{a}/{m}/{j}` | `GET /api/grille/{annee}/{mois}/{jour}?vue=&enfant=` | `GrilleController` |
| `/api/oauth/google/demarrer` · `/callback` | `GET` idem | `OAuthController` |

**Note pour le lot Web (6)** : côté front, plusieurs read-models sont dupliqués (`SlotDuJourVue`,
`PeriodeDuJourVue`, `CycleFoyer`, `EnfantFoyer`, `ActeurFoyer`…) entre `PlanningDeGarde.Web` et
`PlanningDeGarde.Api.Dtos` — candidats à unification. Les commandes d'écriture du front vivent dans
`PlanningDeGarde.Web/CanalEcriture.cs` (records de corps) ; les composants construisent désormais l'URL
ressource + verbe eux-mêmes. Les dialogs de config (`ConfigurationFoyer.razor.cs`) portent les helpers
d'envoi génériques (`Func<Task<HttpResponseMessage>>`) — à regrouper dans un service client si le lot Web
introduit une librairie de composants.
