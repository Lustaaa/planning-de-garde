# Sprint 52 — Échange sur une PLAGE `[J1..J2]`

> **Goal (G2 tranché PO)** : étendre le flux **proposition → accord** de l'échange s47 d'un
> **jour unique** à une **PLAGE `[J1..J2]`** (le vœu « échangeons toute la semaine de vacances »,
> consenti). **Miroir EXACT de la progression s44→s45** (la délégation directe est passée du jour
> unique à la plage), transposée au **workflow d'échange consenti s47**.

## Avancement — 8/10

| # | Scénario | Type | Statut |
|--:|---|---|:--:|
| 1 | Proposer sur plage `[J1..J3]` = UNE Proposition `pending` SANS écriture (invariant s47 prouvé) | @back | ✅ |
| 2 | Défaut `fin=début` = échange mono-jour s47 STRICTEMENT inchangé (non-régression) | @back | ✅ |
| 3 | Accepter COMPOSE la délégation-plage s45 (surcharge multi-jours + transferts aux 2 frontières) | @back | ✅ |
| 4 | Refuser retire SANS écriture → `refusé`, store intact | @back | ✅ |
| 5 | Bornes refusées AVANT écriture (fin<début / soi-même / délégataire inconnu-orphelin) | @back | ✅ |
| 6 | Ré-proposition = last-write-wins R11 sans doublon · `fin` hors fenêtre sans crash | @back | ✅ |
| 7 | Champ « jusqu'au » dans `ProposerEchangeDialog` (miroir s45), Parent-gated | @ihm | ✅ |
| 8 | Notif d'échange de plage ACTIONNABLE dans la cloche (Accepter/Refuser) chez le recevant | @ihm | ✅ |
| 9 | Échap = Annuler (port s33) · refus domaine → dialog reste ouverte + motif + saisie conservée | @ihm | ⏳ |
| 10 | Temps réel : accepter → 2ᵉ écran converge sur TOUTES les cases de la plage, 0 GET sur push | @ihm | ⏳ |

**back : 6 · ihm : 4 · total : 10.**

---

## Portes de conception — TRANCHÉES AU CADRAGE (pas au gate G3)

### 1. MULTI-ENFANTS — **TRANCHÉ : borné à UN enfant ce sprint.**
Le goal candidat évoquait « plage **/ multi-enfants** ». **Décision** : s52 est **borné à un
échange sur plage MONO-ENFANT** (un enfant, plusieurs jours contigus). **Justification** : (a) le
**vrai multi-enfants au sens R1 n'est pas encore exercé de bout en bout** (dette ouverte s30,
reliquat référentiel d'enfants) — l'empiler ici mêlerait **deux axes non éprouvés** dans un même
sprint ; (b) tenir la **cible ~2h** en réemployant à l'identique la mécanique plage s45 (mono-enfant)
sans ouvrir la question orthogonale de la sélection multi-enfants ; (c) la plage est l'extension
**directe et prouvée** (s45), le multi-enfants une **surface neuve**. → **Multi-enfants routé au
backlog** (Épic 11, « échange plage/série/**multi-enfants** »), à cadrer quand R1 sera exercé.

### 2. SURFACE — **AUCUNE surface neuve** (garde anti-rework G3 s44/s51).
On **enrichit `ProposerEchangeDialog` s47** d'un **champ « jusqu'au »** (date de fin), **miroir EXACT**
du champ ajouté à la dialog de délégation en s45. Défaut « jusqu'au » = **jour cliqué** → proposer
d'UN jour **strictement inchangé** (parité s47). **Pas** de nouvel écran, **pas** de nouvelle entrée
de menu. La notif d'échange reste **ACTIONNABLE dans la cloche** (Accepter/Refuser), inchangée.

### 3. RÉEMPLOI — **AUCUN store / modèle / commande neuf.**
Réemploi **INTÉGRAL** du modèle `Proposition` s47 (`pending → Accepter/Refuser`). `ProposerEchange`
gagne un **intervalle** `[début..fin]` ; `AccepterProposition` **compose la délégation-PLAGE s45**
(écriture surcharge multi-jours via s06 + transferts bicolores **auto-dérivés s31** aux deux
frontières). La résolution reste périodes/transferts ; le journal s47 consigne le changement.

---

## Invariants à tenir

- **Anti-vert-qui-ment (s47), explicitement prouvé** : une Proposition `pending` **n'écrit RIEN** —
  **0 surcharge posée**, store des surcharges **STRICTEMENT intact**, **aucune case de la plage
  changée** tant que non acceptée. Prouvé sur **InMemory + Mongo durable**.
- **Aucune écriture partielle sur la plage** : tout refus (fin<début, soi-même, délégataire
  inconnu/orphelin) tombe **AVANT** toute écriture — **aucun jour** de l'intervalle écrit.
- **Diffusion = lecture seule** : la convergence temps réel passe par la **reprojection client**
  depuis la diffusion **porteuse de payload** `INotificateurChangement` s47 — **0 GET sur push**.
  Écriture exclusivement sur le canal requête/réponse. Garde anti-flake **[[flake-signalr-blast-radius]]**
  respectée (classes `FrontWasm*TempsReel*` sérialisées).
- **last-write-wins R11** sur chevauchement / ré-proposition, **sans doublon**.
- **Gate visuel (leçon s49)** : l'interaction est **champ de dialog + clics** (couverte par bUnit) —
  **PAS de geste souris natif** → **pas de smoke Playwright requis**. **MAIS** rebuild du **build WASM
  SERVI** (conteneur `web` docker) **à jour** avant sollicitation PO.

---

## Scénarios

### @back

```gherkin
@back @vert
Scénario 1 — Proposer sur une plage crée UNE Proposition pending SANS aucune écriture
  Étant donné un enfant et un délégataire connus du foyer
  Et le store des surcharges dans un état connu (les cases J1, J2, J3 résolues sur le fond)
  Quand un parent propose un échange de l'enfant sur la plage [J1..J3] vers le délégataire
  Alors une SEULE Proposition « pending » est créée, portant l'intervalle [J1..J3] et l'enfant
  Et AUCUNE surcharge n'est posée (store des surcharges STRICTEMENT intact)
  Et les cases J1, J2, J3 restent résolues sur le fond (aucune bascule de responsable)
  Et l'invariant est vérifié IDENTIQUEMENT sur l'adaptateur InMemory ET sur Mongo durable
```

```gherkin
@back @vert
Scénario 2 — Défaut fin=début : échange mono-jour s47 STRICTEMENT inchangé (non-régression)
  Étant donné un enfant et un délégataire connus
  Quand un parent propose un échange sans borne de fin (fin = début = J1)
  Alors le comportement est celui de l'échange d'UN jour s47, à l'identique
  Et la Proposition pending porte l'intervalle ponctuel [J1..J1]
  Et aucune régression du flux mono-jour s47 n'est introduite
```

```gherkin
@back @vert
Scénario 3 — Accepter COMPOSE la délégation-plage s45 (surcharge multi-jours + transferts aux 2 frontières)
  Étant donné une Proposition pending sur la plage [J1..J3] pour un enfant, vers un délégataire
  Quand le recevant accepte la proposition
  Alors une surcharge est posée sur CHAQUE jour [J1..J3] (le délégataire prime sur le fond)
  Et un transfert bicolore AUTO-DÉRIVÉ s31 apparaît à l'ENTRÉE (frontière J1)
  Et un transfert bicolore AUTO-DÉRIVÉ s31 apparaît à la SORTIE (frontière J3+1)
  Et la Proposition passe à « accepté »
  Et le résultat est IDENTIQUE sur l'adaptateur InMemory ET sur Mongo durable
```

```gherkin
@back @vert
Scénario 4 — Refuser retire SANS écriture
  Étant donné une Proposition pending sur la plage [J1..J3]
  Quand le recevant refuse la proposition
  Alors la Proposition passe à « refusé »
  Et AUCUNE surcharge n'est posée sur aucun jour de la plage
  Et le store des surcharges reste STRICTEMENT intact
```

```gherkin
@back @vert
Scénario 5 — Bornes refusées AVANT écriture, sans écriture partielle
  Étant donné un parent qui compose une proposition d'échange sur plage
  Quand la borne de fin est antérieure au début (fin < début, plage vide)
  Alors la proposition est refusée AVANT écriture, aucune Proposition n'est créée
  Quand le délégataire visé est soi-même
  Alors la proposition est refusée sans écriture
  Quand le délégataire est inconnu du foyer OU orphelin (non résolvable)
  Alors la proposition est refusée AVANT écriture, AUCUN jour de la plage n'est écrit
```

```gherkin
@back @vert
Scénario 6 — Ré-proposition last-write-wins R11 · fin hors fenêtre sans crash
  Étant donné une Proposition pending sur [J1..J3] vers un délégataire A
  Quand une nouvelle proposition sur [J1..J3] est faite vers un délégataire B
  Alors la dernière écriture gagne (R11), sans doublon de Proposition pending
  Étant donné une plage dont la fin dépasse la fenêtre de grille chargée
  Quand la proposition est faite puis acceptée
  Alors l'écriture est valide sur toute la plage, sans crash
```

### @ihm

```gherkin
@ihm @vert
Scénario 7 — Champ « jusqu'au » dans ProposerEchangeDialog (miroir s45), Parent-gated
  Étant donné un Parent qui ouvre « proposer un échange » depuis le menu clic-case
  Alors la dialog ProposerEchangeDialog affiche un champ « jusqu'au », défaut = jour cliqué
  Quand il choisit un délégataire et une fin J3, puis valide
  Alors une proposition d'échange sur la plage [jour cliqué..J3] est émise (canal d'écriture)
  Et une notification apparaît chez le recevant
  Et un Invité ne voit ni le menu ni la dialog (inerte)
```

```gherkin
@ihm @vert
Scénario 8 — Notif d'échange de plage ACTIONNABLE dans la cloche
  Étant donné un recevant connecté qui a reçu une proposition d'échange sur [J1..J3]
  Quand il ouvre la cloche
  Alors la notification d'échange est ACTIONNABLE (Accepter / Refuser)
  Quand il accepte
  Alors TOUTES les cases de la plage [J1..J3] basculent sur le nouveau responsable
  Et les transferts dérivés apparaissent aux deux frontières
```

```gherkin
@ihm @pending
Scénario 9 — Échap = Annuler · refus domaine garde la dialog ouverte + saisie conservée
  Étant donné la dialog ProposerEchangeDialog ouverte avec une saisie (acteur + plage)
  Quand l'utilisateur presse Échap
  Alors la dialog se ferme (Annuler), aucune proposition émise (port IEcouteurEchapModal s33)
  Étant donné une saisie invalide (fin < début OU délégataire inconnu)
  Quand l'utilisateur valide
  Alors la dialog RESTE ouverte, un motif s'affiche, la saisie (acteur ET plage) est conservée
  Et aucune écriture n'a lieu
```

```gherkin
@ihm @pending
Scénario 10 — Temps réel : accepter → 2ᵉ écran converge sur toute la plage, 0 GET sur push
  Étant donné deux écrans connectés sur le même foyer
  Quand le recevant accepte une proposition d'échange sur la plage [J1..J3] sur le 1ᵉʳ écran
  Alors le 2ᵉ écran voit TOUTES les cases [J1..J3] converger (nouveau responsable + transferts frontières)
  Et la convergence se fait par reprojection client depuis la diffusion INotificateurChangement s47
  Et AUCUN GET n'est émis sur le push (garde anti-flake [[flake-signalr-blast-radius]] respectée)
```

---

# Retours produit (PO)

_(à remplir au gate G3)_
