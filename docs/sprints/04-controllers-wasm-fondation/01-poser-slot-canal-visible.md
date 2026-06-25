# Scénario 1 — Poser un slot via le canal d'écriture le rend visible dans sa case jour/horaire

`@nominal`

[← Retour au suivi](00-sprint04-suivi.md)

> **Axe : backend.** Le canal requête/réponse est l'**adaptateur de gauche** (endpoint HTTP)
> qui invoque le handler `PoserSlotHandler` **inchangé** ; l'observable est la projection
> réelle `GrilleAgendaQuery`. Aucun `.razor` ni interactivité runtime n'est piloté ici (la
> migration front WASM est un invariant non-codant). Routé vers `tdd-auto`.
>
> **Niveau d'acceptation : intégration** (`WebApplicationFactory`), pas unit ni bUnit.

## Acceptation (BDD)

`Should_Faire_apparaitre_le_slot_ecole_08h30_16h30_dans_la_case_du_mercredi_24_06_2026_de_la_grille_projetee_When_la_commande_de_pose_de_Lea_est_emise_via_le_canal_requete_reponse` — ✅ GREEN

Test d'**intégration de bout en bout** sur l'hôte Web réel (`WebApplicationFactory<Program>`,
store réel singleton) :
- **Given** le foyer connaît « école » (référentiel réel `FoyerLieuRepository`) ; aucun slot
  pour le mercredi 24/06/2026 ;
- **When** la commande de pose (Léa, « école », 24/06/2026 08:30→16:30) est **émise via le
  canal requête/réponse** (`POST` sur l'endpoint d'écriture) ;
- **Then** le canal renvoie une **réponse de succès** ; **et** la projection réelle
  `GrilleAgendaQuery.Projeter(22/06/2026)` porte dans la case du mercredi 24/06 un slot
  « école » de 08:30 à 16:30.

## Tests d'intégration (ordonnés)

| # | Test (FLFI) | TPP | Contradiction | Status |
|---|-------------|-----|---------------|--------|
| 1 | `Should_Confirmer_la_pose_par_une_reponse_de_succes_When_la_commande_de_pose_d_un_slot_valide_est_emise_via_le_canal_requete_reponse` | nil → endpoint qui acquitte | **Driver du canal** : aucun endpoint d'écriture n'existe (le front appelle le handler en direct). Le 1er rouge force l'existence du canal HTTP relié à `PoserSlotHandler` et un acquittement de succès. ⚠️ Le comportement handler « pose réussie » est déjà vert (Sc.1, `Scenario1_PoserSlot`) — **c'est le câblage HTTP qui est le driver, pas la règle métier**. | ✅ GREEN |
| 2 | `Should_Faire_apparaitre_le_slot_ecole_08h30_16h30_dans_la_case_du_mercredi_24_06_2026_de_la_projection_reelle_When_la_pose_de_Lea_a_abouti_via_le_canal` | endpoint acquitté → effet observé en bout de chaîne | **Driver de bout en bout (anti early-green)** : un canal qui acquitterait sans persister dans le **store réel** passe le #1 mais échoue ici. Force le chemin réel endpoint → handler → store singleton → projection réelle. ⚠️ La projection « slot dans la case du jour » est déjà verte en isolé (`Scenario_SlotDansCaseDuJour`) ; ici elle est exercée sur le **store réel après le canal** — caractérisation de la chaîne, mais c'est sa **première observation post-canal**. | ✅ GREEN (caractérisation) |

## Fichiers à créer

- Test d'intégration `tests/PlanningDeGarde.Web.Tests/` (nouveau fichier de scénario de canal
  d'écriture, niveau `WebApplicationFactory`).
- Endpoint HTTP du canal d'écriture « pose de slot » sur l'hôte Web (production) —
  scaffolding créé par `tdd-auto`.
- Référence `Microsoft.AspNetCore.Mvc.Testing` à ajouter à `PlanningDeGarde.Web.Tests.csproj`.

## Design notes

- **Store réel obligatoire** : la fabrique de test doit résoudre les `InMemory*Repository`
  **singletons** de l'hôte (source de vérité du foyer) et projeter via le `GrilleAgendaQuery`
  réel — jamais une doublure sur le chemin observé.
- **Date de référence injectée** : projeter à `22/06/2026` (lundi de la semaine de référence),
  jamais `DateTime.Now`.
- **Notification temps réel** : déclenchée par l'écriture aboutie (`NotifierMiseAJour`),
  vérifiable par **Spy** sur `INotificateurPlanning` si besoin — **jamais** une écriture par
  le canal de diffusion (lecture seule). Hors observable principal de ce scénario.
- **Réutiliser** les `[Fact]` handler/projection existants comme filet (ne pas les dupliquer
  en unit ici) : ce dossier n'ajoute que le niveau **canal/intégration**.
