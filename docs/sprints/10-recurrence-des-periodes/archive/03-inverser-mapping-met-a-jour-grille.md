# Sc.3 — Inverser le mapping du cycle met à jour la grille sans rechargement

`@nominal` `🖥️ IHM`

↩ Retour : [00-sprint10-suivi.md](00-sprint10-suivi.md)

**Routage** : tranche **backend = CARACTÉRISATION** (`tdd-auto`, ⚠️ early green **attendu** : re-projection
après ré-définition du mapping — **pas** un driver) **+** acceptation **runtime IHM driver**
(`ihm-builder` : la grille suit l'inversion du mapping **sans rechargement** via SignalR — le vrai
symptôme PO). Le « sans rechargement » est un fait **runtime/SignalR** ; bUnit seul ne le prouve jamais.

> **Données** : cycle N=2 défini pair → `parent-a` (Alice bleu), impair → `parent-b` (Bruno orange). La
> grille affiche Parent A bleu sur ISO 28 (06–12/07/2026). Inversion : pair → `parent-b`, impair →
> `parent-a`. Attendu : ISO 28 affiche désormais Parent B orange, en case comme en légende.

## Acceptation (BDD) — niveau **runtime/IHM** (routé `ihm-builder`)

> Sur l'app réellement câblée, la grille affiche Parent A bleu sur la semaine ISO 28 ; un parent inverse
> le mapping depuis l'écran de configuration ; **sans rechargement**, les cases du 06 au 12/07 passent à
> Parent B orange, en case comme en légende — la grille suit par diffusion SignalR. **Pas** un test bUnit
> à doublure (qui ne prouve ni la DI réelle, ni le chemin d'écriture du cycle, ni la diffusion temps réel).

`Should_Mettre_a_jour_la_grille_vers_Parent_B_orange_sur_la_semaine_ISO_28_sans_rechargement_When_un_parent_inverse_le_mapping_du_cycle_depuis_la_configuration` — ✅ GREEN *(runtime, `ihm-builder` — `FrontWasmGrilleInverserMappingCycleTempsReelTests`)*

> **Early-green de câblage** (comme Sc.1) : aucune correction de prod — la définition/ré-édition du cycle
> (canal HTTP `POST /api/canal/definir-cycle`, `DefinirCycleHandler` qui écrase le store ET déclenche la
> diffusion), l'abonnement SignalR de `PlanningPartage` (re-projection sur `MiseAJour`) et la résolution du
> fond (Sc.1) délivraient déjà l'observable « sans rechargement ». **Non-vacuité prouvée** : (1) baseline
> « Alice bleu » asserté AVANT l'inversion → la bascule vers « Bruno orange » est réellement observée ;
> (2) neutralisation temporaire de l'abonnement au hub (`_hub.On(MiseAJour)` vidé) → la grille reste sur
> Alice/bleu → **rouge** sur la bascule (timeout `WaitForAssertion`), puis revert. Pompe de diffusion de
> fond (idempotente) pour rendre le push déterministe APRÈS établissement du long polling vers le TestServer.

## Tests unitaires backend (boucle interne, `tdd-auto`)

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| 1 | `Should_Resoudre_Parent_B_orange_sur_la_semaine_ISO_28_When_le_mapping_du_cycle_a_ete_inverse` | — | ⚠️ **probablement early green — couvert par Sc.1 + dernière-écriture-gagne du store cycle (caractérisation, pas driver)**. Ré-définir le cycle écrase le mapping (affectation sur le singleton, sans version) ; `GrilleAgendaQuery` **relit** le port cycle → résout le nouveau mapping (ISO 28 paire → index pair → désormais Parent B). Aucune logique neuve : re-projection d'un read model inchangé sur un état réécrit. `tdd-auto` marquera ✅ GREEN (caractérisation). | ✅ GREEN (caractérisation) |

## Fichiers à créer / modifier (backend uniquement ici)

- **Aucun fichier neuf** — réutilise la résolution du fond (Sc.1) + l'écrasement du cycle par
  `DefinirCycleHandler` (dernière écriture gagne, patron config foyer).
- **Doublures tests** — `Fake` du port cycle re-défini en cours de test (mapping inversé) ;
  `GrilleAgendaQuery` re-projeté après inversion.
- **Volet runtime IHM (routé `ihm-builder`)** — écran config éditant le mapping ; diffusion SignalR sur
  ré-définition aboutie ; grille re-rendue **sans rechargement**.

## Design notes

- **Édition immédiate, cohérente avec l'édition acteurs (s08).** Ré-éditer le mapping suit le même patron
  que renommer/recolorier un acteur : le store écrase, la grille relit, la diffusion propage.
- **Le « sans rechargement » est l'observable runtime central** — prouvé sur l'app câblée (SignalR),
  jamais par un test backend à doublure ni un bUnit composant.
- **Caractérisation conservée** : filet de non-régression de la re-projection après ré-définition ; pas
  de rouge attendu côté backend, le driver est runtime.
