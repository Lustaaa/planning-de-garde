# Scénario 1 — Le back démarre seul : l'API détachée enregistre une affectation sans le front

`@nominal`

[← Retour au suivi](00-sprint05-suivi.md)

> **Axe : backend.** Le driver est l'**existence d'un hôte d'API détaché** (`PlanningDeGarde.Api`,
> `public partial class ApiProgram`) qui porte le canal d'écriture, **sans référencer le projet
> front** — c'est la preuve qu'il démarre seul. Le canal invoque le handler `AffecterPeriodeHandler`
> **inchangé** ; l'observable est la projection réelle `GrilleAgendaQuery` sur le **store réel** de
> cet hôte. Aucun `.razor` ni interactivité runtime ici (la migration WASM est pilotée par Sc.2 /
> `ihm-builder`). Routé vers `tdd-auto`.
>
> **Niveau d'acceptation : intégration** (`WebApplicationFactory<ApiProgram>`), pas unit ni bUnit.

## Acceptation (BDD)

`Should_Colorer_les_cases_jour_du_lundi_22_au_vendredi_26_06_2026_de_la_couleur_bleue_de_Parent_A_dans_la_projection_reelle_When_une_affectation_est_emise_sur_le_canal_de_l_hote_d_API_demarre_seul` — ✅ GREEN

Test d'**intégration de bout en bout** sur le **nouvel hôte d'API détaché**
(`WebApplicationFactory<ApiProgram>`, store réel singleton, environnement « Testing » → store
vierge) :
- **Given** l'hôte d'API est démarré **seul** (sa fabrique ne charge **pas** le projet front) ; le
  foyer connaît « Parent A » (id palette `parent-a` → bleu) ; aucune période sur la semaine du
  lundi 22/06/2026 ;
- **When** une commande d'affectation de la période du lundi 22 au vendredi 26/06/2026 au
  responsable `parent-a` est **émise sur le canal d'écriture de l'hôte d'API** (`POST` sur
  l'endpoint d'affectation) ;
- **Then** le canal renvoie une **réponse de succès** ; **et** la projection réelle
  `GrilleAgendaQuery.Projeter(22/06/2026)` porte la **couleur bleue** de « Parent A » sur les
  cases-jour du lundi 22 au vendredi 26/06/2026.

## Tests d'intégration (ordonnés)

| # | Test (FLFI) | TPP | Contradiction | Status |
|---|-------------|-----|---------------|--------|
| 1 | `Should_Demarrer_l_hote_d_API_sans_charger_le_projet_front_When_on_inspecte_les_dependances_de_l_hote_d_API` | {} → assertion d'architecture | **Driver structurel (cœur du sprint)** : aujourd'hui un seul hôte (`PlanningDeGarde.Web`) porte tout. Le 1er rouge force l'existence de `PlanningDeGarde.Api` (`ApiProgram`) **ne référençant pas** `PlanningDeGarde.Web` — inspection des `ProjectReference` / assemblies chargées. C'est la preuve « démarre seul ». | ✅ GREEN |
| 2 | `Should_Confirmer_l_affectation_par_une_reponse_de_succes_When_la_commande_d_affectation_est_emise_sur_le_canal_de_l_hote_d_API` | nil → endpoint qui acquitte | **Driver du canal sur l'hôte détaché** : aucun canal d'écriture n'existe encore sur le **nouvel** hôte API (il vivait dans `Web/Program.cs`). Force le portage de `MapperCanalEcriture` vers `ApiProgram` relié à `AffecterPeriodeHandler` + un acquittement de succès. ⚠️ Le comportement handler « affectation réussie » est **déjà vert** (Application + Sc.3 sprint 04) — **le driver est le câblage de l'hôte API détaché, pas la règle métier**. | ✅ GREEN (caractérisation) |
| 3 | `Should_Colorer_les_cases_jour_du_lundi_22_au_vendredi_26_06_2026_de_la_couleur_bleue_de_Parent_A_dans_la_projection_reelle_When_l_affectation_a_abouti_via_le_canal_de_l_hote_d_API` | endpoint acquitté → effet observé en bout de chaîne | **Driver de bout en bout (anti early-green)** : un canal qui acquitterait sans persister dans le **store réel** de l'hôte API passe #2 mais échoue ici. Force le chemin réel endpoint → handler → store singleton de `ApiProgram` → projection réelle. ⚠️ La coloration des cases est **déjà verte** (Application + Sc.3 sprint 04) ; ici c'est sa **première observation sur le store réel du nouvel hôte détaché** — caractérisation de la chaîne API. | ✅ GREEN (caractérisation) |

## Fichiers à créer

- **Projet API** `src/PlanningDeGarde.Api/` (SDK Web, `public partial class ApiProgram`) :
  référence Application + Infrastructure, **jamais** `PlanningDeGarde.Web` ; porte
  `MapperCanalEcriture`, OpenAPI, Scalar, CORS. — scaffolding créé par `tdd-auto`.
- **Projet de test** `tests/PlanningDeGarde.Api.Tests/` (`Microsoft.AspNetCore.Mvc.Testing`,
  fabrique `WebApplicationFactory<ApiProgram>` forçant l'environnement « Testing »).
- Fichier de tests d'intégration du canal d'affectation sur l'hôte API + le **test d'architecture**
  (non-référence du front).

## Design notes

- **Store réel obligatoire** : la fabrique de test résout les `InMemory*Repository` **singletons**
  de l'hôte API et projette via le `GrilleAgendaQuery` réel — jamais une doublure sur le chemin observé.
- **Environnement « Testing »** : comme l'hôte Web (cf. `CanalEcritureFactory` sprint 04), désactive
  l'amorçage des données de démo → store vierge, projection ne reflétant que ce que le test écrit.
- **Date de référence injectée** : projeter à `22/06/2026`, jamais `DateTime.Now`.
- **Test d'architecture** : préférer l'inspection des `ProjectReference` du `.csproj` (ou des
  `AssemblyName` référencés par l'assembly `ApiProgram`) ; l'assertion = `PlanningDeGarde.Web`
  **absent**. C'est le garde-fou anti-régression du découplage.
- **Réutiliser** les `[Fact]` handler/projection existants comme filet (ne pas les dupliquer en unit
  ici) : ce dossier n'ajoute que le niveau **canal/intégration sur l'hôte API détaché**.
- **Notification temps réel** : déclenchée par l'écriture aboutie (`NotifierMiseAJour`), vérifiable
  par **Spy** sur `INotificateurPlanning` si besoin — jamais une écriture par le canal de diffusion.
  Hors observable principal.
