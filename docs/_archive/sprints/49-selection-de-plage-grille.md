# Sprint 49 — Sélection de plage sur la grille (tranche 2 du palier 9)

> **Goal (G2, tranché PO)** : le hub `/planning` gagne la **sélection d'une plage de cases par
> DRAG** sur la grille agenda pour **affecter une période sur l'intervalle** choisi. On **réemploie
> STRICTEMENT** la dialog « Affecter une période » (écriture-en-contexte s06) : le back multi-jours
> existe déjà (une période EST un intervalle `[début..fin]`, prouvé par s06 et réexercé par les
> plages s45). Ce sprint **n'ajoute AUCUNE mécanique d'écriture, AUCUN store, AUCUNE persistance** :
> la sélection est un **état d'interaction client VOLATILE** (borne anti-cliquet). Enrichit la grille
> **sans toucher aux dialogs déjà livrées**.

## Avancement — 8/8

| # | Scénario | Type | Statut |
|---|----------|------|--------|
| 1 | Filet non-régression : affecter une période sur un intervalle `[J1..J3]` pose la surcharge sur CHAQUE jour (réemploi s06, deux adaptateurs) | back | ✅ |
| 2 | Filet : intervalle d'UN seul jour `[J..J]` = période ponctuelle inchangée, aucune écriture doublonnée | back | ✅ |
| 3 | Nominal : drag de J1 à J3 → dialog « Affecter une période » EXISTANTE pré-remplie `début=J1 fin=J3` → valider écrit sur l'intervalle → grille converge | 🖥️ IHM | ✅ |
| 4 | Limite : une seule case sans drag = **clic simple INCHANGÉ** (menu clic-case s'ouvre, PAS la dialog plage) | 🖥️ IHM | ✅ |
| 5 | Limite : drag en **sens inverse** (J3→J1) → intervalle **NORMALISÉ** `[min..max]`, dialog `début ≤ fin` (jamais plage vide/inversée) | 🖥️ IHM | ✅ |
| 6 | Limite : drag **débordant la fenêtre de vue** courante → sélection **BORNÉE à la vue chargée**, aucune case hors-vue, aucune navigation, aucune persistance | 🖥️ IHM | ✅ |
| 7 | Erreur/annulation : **Échap** pendant/après la sélection **ANNULE** (aucune dialog, aucune écriture, surbrillance retirée) — port `IEcouteurEchapModal` s33 | 🖥️ IHM | ✅ |
| 8 | Gating : **Invité** (non-Parent) ne peut PAS sélectionner (drag inerte, aucune dialog) ; **Parent** seul sélectionne | 🖥️ IHM | ✅ |

**Répartition** : 2 `@back` (filets de non-régression du chemin d'écriture réutilisé) · 6 `@ihm`
(la surface de sélection est le cœur du sprint, menée RED→GREEN **runtime** sur app câblée).

---

## PORTE DE CONCEPTION — surface d'AFFORDANCE du drag (à confirmer PO au cadrage / G3)

La sélection introduit une **nouvelle surface d'INTERACTION** (pas une surface de lecture, pas une
surface d'écriture neuve : elle **ouvre la dialog existante**). Choix de conception à valider :

- **Emplacement retenu** : **drag directement sur les cases de la grille agenda** (mousedown sur J1,
  survol jusqu'à J3, mouseup) avec **surbrillance progressive** des cases pendant le geste. Le
  relâchement ouvre la dialog « Affecter une période » pré-remplie sur l'intervalle.
- **Alternatives écartées** : (a) bouton « mode sélection » à activer avant de cliquer ; (b) cases à
  cocher par jour puis bouton « Affecter » ; (c) champs de dates manuels début/fin. → écartées :
  friction supérieure, redondantes avec la saisie déjà offerte dans la dialog.
- **Aucune surface de LECTURE neuve** ; **aucune persistance** de l'état de sélection.

> Point remonté au thread principal **en même temps que le tranchage du goal** (garde surface s44).
> Les `@ihm` ne sont menés RED→GREEN qu'une fois l'affordance confirmée.

---

## Scénarios

### Sc.1 — `@back @vert` Filet : affecter une période sur un intervalle multi-jours

```gherkin
Fonctionnalité: Écriture d'une période sur un intervalle (chemin réutilisé par la sélection de plage)
  Contexte:
    Étant donné un foyer configuré avec un cycle de fond et l'acteur "Alice"
    Et un intervalle de trois jours consécutifs J1 < J2 < J3 dans la fenêtre chargée

  Scénario: Affecter une période sur [J1..J3] pose la surcharge sur chaque jour
    Quand une période affectant "Alice" est écrite sur l'intervalle [J1..J3]
    Alors le responsable résolu de J1, J2 et J3 est "Alice" (surcharge B prime sur le fond)
    Et le comportement est identique sur l'adaptateur InMemory et sur Mongo durable
    # Early-green attendu : une période EST un intervalle (s06), réexercé par les plages s45.
    # Ce scénario est un FILET de non-régression du chemin d'écriture que la sélection réutilise.
```

### Sc.2 — `@back @vert` Filet : intervalle d'un seul jour = période ponctuelle

```gherkin
  Scénario: Affecter une période sur [J..J] écrit exactement un jour, sans doublon
    Étant donné un jour unique J dans la fenêtre chargée
    Quand une période affectant "Alice" est écrite sur l'intervalle [J..J]
    Alors seul le jour J porte la surcharge, J-1 et J+1 restent sur le fond
    Et aucune écriture n'est doublonnée (last-write-wins R11, un seul enregistrement)
    Et le comportement est identique InMemory et Mongo durable
    # Garantit que la sélection d'un seul jour (Sc.4) retombe sur le comportement ponctuel connu.
```

### Sc.3 — `@ihm @vert` Nominal : drag → dialog pré-remplie → écriture → convergence

```gherkin
Fonctionnalité: Sélection d'une plage de cases sur la grille agenda
  Contexte:
    Étant donné un utilisateur connecté et Parent sur le hub "/planning"
    Et la grille agenda affichée sur la vue courante

  Scénario: Drag de J1 à J3 ouvre la dialog "Affecter une période" pré-remplie sur l'intervalle
    Quand l'utilisateur presse sur la case J1, survole jusqu'à la case J3, puis relâche
    Alors les cases J1, J2 et J3 sont mises en surbrillance pendant le geste
    Et au relâchement la dialog "Affecter une période" EXISTANTE (s06) s'ouvre
    Et elle est pré-remplie avec début = J1 et fin = J3 (aucune dialog neuve)
    Quand l'utilisateur choisit "Alice" et valide
    Alors la période est écrite sur l'intervalle via le canal d'écriture (réemploi s06)
    Et la grille converge : J1, J2, J3 rendent "Alice"
    # Mené RED->GREEN runtime sur app câblée (store réel), pas de doublure de grille.
```

### Sc.4 — `@ihm @vert` Limite : une seule case sans drag = clic simple inchangé

```gherkin
  Scénario: Un mousedown/mouseup sur la même case conserve le comportement de clic existant
    Quand l'utilisateur clique une case unique J (sans déplacement)
    Alors le menu clic-case EXISTANT s'ouvre (Affecter une période / Définir un transfert / …)
    Et la dialog "Affecter une période" pré-remplie sur une PLAGE n'est PAS ouverte
    Et le comportement de clic simple livré antérieurement est strictement préservé (non-régression)
```

### Sc.5 — `@ihm @vert` Limite : drag en sens inverse normalisé

```gherkin
  Scénario: Drag de J3 vers J1 (sens inverse) normalise l'intervalle
    Quand l'utilisateur presse sur J3, survole jusqu'à J1, puis relâche
    Alors la surbrillance couvre J1, J2, J3 (même intervalle que le sens direct)
    Et la dialog s'ouvre avec début = J1 et fin = J3 (début ≤ fin garanti)
    Et jamais avec une plage inversée ou vide
```

### Sc.6 — `@ihm @vert` Limite : débordement borné à la vue, sans persistance

```gherkin
  Scénario: Un drag qui déborde la fenêtre de vue reste borné à la vue chargée
    Étant donné une vue courante (semaine / 4 semaines glissantes / mois)
    Quand l'utilisateur presse sur une case interne et tire au-delà du bord de la vue
    Alors seules les cases DE LA VUE chargée sont sélectionnées (aucune case hors-vue)
    Et aucune navigation passé/futur n'est déclenchée par le geste
    Et l'état de sélection n'est PAS persisté (volatil, borne anti-cliquet)
    Et un changement de vue ou un rechargement efface la sélection
```

### Sc.7 — `@ihm @vert` Erreur/annulation : Échap annule la sélection

```gherkin
  Scénario: Échap annule la sélection sans ouvrir de dialog ni écrire
    Quand l'utilisateur a une sélection en cours (ou une plage relâchée avant validation)
    Et qu'il presse Échap
    Alors la surbrillance est retirée
    Et aucune dialog "Affecter une période" ne s'ouvre / ne reste ouverte
    Et aucune écriture n'est émise (store intact)
    # Réemploi du port IEcouteurEchapModal s33 (capture au niveau document).
```

### Sc.8 — `@ihm @vert` Gating : Parent seul sélectionne

```gherkin
  Scénario: L'Invité ne peut pas sélectionner de plage
    Étant donné un utilisateur en mode Invité (non-Parent) sur "/planning"
    Quand il presse et tire sur les cases de la grille
    Alors aucune surbrillance de sélection n'apparaît et aucune dialog ne s'ouvre (drag inerte)
    Et seul un utilisateur Parent peut sélectionner une plage (Parent-gated)
```

---

## Notes de cadrage

- **Réemploi strict** : la dialog « Affecter une période » (s06) est la SEULE surface d'écriture ;
  elle est simplement **pré-remplie** avec l'intervalle sélectionné. Aucun handler, aucune commande,
  aucun DTO neuf côté écriture.
- **Back déjà présent** : Sc.1 & Sc.2 sont des **filets** (early-green attendu). Si vert au premier
  run, les **conserver** comme non-régression (ne pas supprimer, ne pas investiguer un faux trou).
- **Aucune persistance** : la sélection vit dans l'état de composant Blazor, effacée au changement de
  vue / rechargement / Échap. Aucune migration, aucun store, aucun read model.
- **Temps réel non concerné** par la sélection elle-même (état client volatil) ; la **convergence de
  la grille** après écriture (Sc.3) repose sur les mécaniques SignalR déjà livrées (reprojection
  client, 0 GET sur push, garde anti-flake `SignalRTempsReelCollection` respectée).

## Correctif du gate G3 — le drag ne fonctionnait pas en navigateur RÉEL

Le PO a testé au navigateur (`http://localhost:5081`) : appuyer sur une case et glisser ne produisait NI la
surbrillance des cases intermédiaires NI l'ouverture de la dialog au relâchement — alors que la suite bUnit
était verte (elle invoque les handlers Blazor en C#, sans reproduire le comportement souris natif).

- **Cause.** Le drag était câblé sur les **mouse events** (`@onmousedown`/`@onmouseover`/`@onmouseup` par
  case) sans neutraliser la **sélection de texte native** du navigateur : au glisser, le navigateur démarrait
  une sélection de texte (les cases portent du texte) qui **avalait** les `mouseover`/`mouseup` intermédiaires
  → aucune case survolée n'était surlignée. De plus, un relâchement **hors d'une case** (gouttière, bord,
  document) n'atteignait aucun `@onmouseup` de case → la plage n'était jamais finalisée.
- **Correctif.** (1) **Pointer events** (`@onpointerdown`/`@onpointerover`) — voie fiable pour un drag
  continu. (2) `@onpointerdown:preventDefault` + `user-select:none`/`touch-action:none` (classe
  `grille-plage-selectionnable`) + `draggable="false"` : neutralisent la sélection/drag natifs, les
  événements de pointeur circulent jusqu'aux cases intermédiaires. (3) Nouveau **port hexagonal**
  `IEcouteurRelachementPointeur` (adaptateur JS `document.addEventListener('pointerup')`, module
  `window.pdgPointeur`, attaché **eager** au 1ᵉʳ rendu, détaché au Dispose) : le relâchement est capté au
  niveau **document**, donc la plage se finalise **où que le bouton soit lâché** — même hors case.
- **Preuve.** Les tests d'acceptation runtime ont été **migrés sur la vraie voie d'événements** (pointer
  events) et le relâchement passe désormais par le **port document doublé** (spy `EspionRelachementPointeur`),
  jamais un `@onmouseup` de case. Un **rempart de non-régression** vérifie que les cases portent la classe qui
  neutralise la sélection native. **Limite honnête (assumée)** : bUnit **ne peut pas** reproduire le geste
  souris natif du navigateur ni exécuter le `addEventListener` JS — le fonctionnement effectif du drag reste
  **non couvrable par bUnit**, à **vérifier manuellement au gate PO** (comme l'Échap document s33).

## 2ᵉ correctif du gate G3 — le drag ne surlignait AUCUNE case (mécanique de survol refondue)

Nouveau constat runtime du PO (front réel `5292`) après le 1ᵉʳ correctif : plus de sélection de texte native, mais
le drag ne surligne **aucune** case intermédiaire et n'ouvre **aucune** dialog au relâchement — la mécanique de
survol elle-même ne s'arme pas en navigateur réel (bUnit reste aveugle à ce geste).

- **Cause.** Le survol reposait sur des `@onpointerover` **posés par case** : pendant un glisser souris continu ces
  événements par-case sont **fragiles / manqués** (et seraient court-circuités par toute capture de pointeur), si
  bien que le curseur ne progresse pas, le seuil « ≥ 1 case ≠ ancre » n'est jamais atteint et le geste retombe en
  clic simple — aucune surbrillance, aucune dialog.
- **Correctif.** Refonte de la mécanique de survol sur le pattern FIABLE, **résolu au niveau DOCUMENT** : (1)
  `pointerdown` pose l'ancre (aucun `setPointerCapture` — absent, vérifié). (2) Nouveau **port hexagonal**
  `IEcouteurMouvementPointeur` (module JS `window.pdgPointeur.attacherMouvement`, `document.addEventListener('pointermove')`)
  qui, à chaque déplacement **bouton primaire appuyé** (`e.buttons & 1`, aucune inondation d'interop hors geste),
  résout la case sous le curseur par `document.elementFromPoint(clientX, clientY)` → plus proche
  `[data-testid="jour-case"]` → lit son **`data-date`** (attribut ajouté sur chaque case) et le remonte à
  `SurvolerCasePlageParDate` qui met à jour le curseur → surbrillance `[min..max]`. (3) Les `@onpointerover` par
  case sont **retirés** (voie fragile écartée). `pointerup` document (port s49) finalise inchangé.
- **Preuve.** Les tests d'acceptation Sc.3-8 exercent désormais la VRAIE voie : le survol passe par le **spy du port
  document** (`EspionMouvementPointeur.DeplacerVersCase`) alimenté avec le `data-date` RÉEL lu sur la case cible
  (exactement ce que `elementFromPoint` résoudrait), jamais un `@onpointerover` de case. Significativité prouvée par
  mutation (neutraliser la mise à jour du curseur → 6/8 rouges). JS `pointermove`+`elementFromPoint` confirmé **servi
  sur `5292`**. **Limite honnête (assumée)** : bUnit ne peut ni exécuter `elementFromPoint` ni rejouer le geste souris
  natif — le fonctionnement effectif du drag reste **non couvrable par bUnit**, à **vérifier manuellement au gate PO**.

## 3ᵉ correctif du gate G3 — la CAUSE RÉELLE était un BUILD SERVI PÉRIMÉ (pas le code)

Après 3 échecs au gate malgré des correctifs successifs (pointer events, `user-select`, `pointermove`
document + `elementFromPoint` + `data-date`), mise en place d'un **harnais navigateur Playwright**
(`tests/PlanningDeGarde.Web.E2E`, hors `.slnx`) pour piloter un **vrai Chromium** contre l'app servie
et **observer** au lieu de coder à l'aveugle (bUnit ne sait pas rejouer le geste souris natif).

- **Observation runtime (preuves).** Connecté en Parent sur `http://localhost:5292`, le drag J1→J3
  ne surlignait **aucune** case (`data-plage-drag=1 : []`) et n'ouvrait **aucune** dialog. Or les
  événements pointeur natifs arrivaient bien (`down/move buttons=1 … up`) et `window.pdgPointeur`
  existait. Dump du DOM servi : la case rendue était `class="grille-jour grille-jour-cliquable  "`,
  data-testid `jour-case`, data-couleur `bleu` — **SANS `data-date`, SANS `grille-plage-selectionnable`,
  SANS `@onpointerdown`**. Le module JS lisait donc un `data-date` **vide** → `Survoler("")` →
  `TryParseExact` échoue → curseur jamais mis à jour → zéro surbrillance, zéro dialog.
- **Cause réelle.** Le **front servi était un build WASM périmé**, antérieur au câblage drag s49. Le
  `docker-compose.yml` a un service `build` **one-shot** qui compile Api+Web dans le volume nommé
  `build-artifacts` ; `web` sert ensuite `--no-build` **depuis ce volume**. Le conteneur `web` (créé
  le 15/07, jamais recréé) resservait l'artefact du **17/07 21:18**, antérieur à la source courante
  (18/07). Recharger l'onglet ne changeait rien : le conteneur resservait l'ancien WASM. **Les 3
  correctifs précédents, corrects en source, n'ont donc JAMAIS atteint le navigateur du PO.**
- **Correctif.** **Aucun changement de code de prod** : la source était déjà correcte. Rebuild du
  stack — `docker compose up build --force-recreate` (recompile la source courante dans le volume)
  puis `docker compose up -d --force-recreate web api` (ressert les binaires frais).
- **Preuve (Playwright, red→green).** Sur le build **périmé** : `data-date` vide, surbrillance `[]`,
  dialog `0`. Sur le build **ré-compilé** : `data-date=2026-07-20/21/22`, surbrillance progressive
  `[20,21] → [20,21,22]`, dialog « Affecter une période » ouverte et **pré-remplie Début=J1 Fin=J3**.
  Deux **smoke tests** navigateur figent ce contrat (`SmokeDragPlage`) : (a) drag J1→J3 surligne + ouvre
  la dialog pré-remplie ; (b) clic simple ouvre le menu clic-case, **pas** la dialog plage. Lancement
  et pré-requis : `tests/PlanningDeGarde.Web.E2E/README.md`.
- **Anti-récidive.** Au prochain gate visuel, **recompiler le build servi** avant de tester
  (`docker compose up build --force-recreate`) — un simple rechargement d'onglet ne reflète pas la
  source. Le harnais Playwright reste disponible pour ré-observer tout symptôme au navigateur réel.

---

# Retours produit (PO)

_VIDE au gate G3 — livraison validée par le PO **sans aucun retour produit** (fusion backlog s49 : rien à
reporter). Les 3 allers-retours du gate portaient sur un **build servi périmé** (bug d'environnement, pas de
retour produit) — capturé en rétro méthode (`JOURNAL-METHODE.md` s49)._
