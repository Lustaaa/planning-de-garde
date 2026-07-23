# Sprint 54 — Activités (slots) : wording, routes API, récurrence multi-jours, config foyer par enfant

> **Goal PO (imposé, deadline 17h aujourd'hui)** : « Terminer l'ensemble de ce qui est lié aux
> activités (slot) ». Wording intuitif (**libellés ET routes API**) · CRUD + récurrence (multi-jours) ·
> activités récurrentes visibles & configurables dans la Config du foyer, **par enfant** · **exclusion
> vacances scolaires** · **portée occurrence + série**.
>
> **⚠️ Tension deadline assumée par le PO** : « rien d'optionnel, tout est inclus » + renommage des
> routes API + 17h sont en forte tension. Aucun scénario n'est optionnel ; l'**ordre** ci-dessous fait
> tomber la valeur tôt si le temps venait malgré tout à manquer.

## Avancement — 3/10

| # | Scénario | Type | Statut |
|--:|----------|:----:|:------:|
| 1 | Renommage **libellés IHM** « slot → activité » / référentiel « Activités → Lieux » (lot atomique) | 🖥️ IHM | ✅ |
| 2 | Renommage **routes REST NESTED** `/api/slots* → /api/enfants/{id}/activites*` + client HTTP Web (lot atomique) | @back | ✅ |
| 3 | Lister les activités récurrentes **d'un enfant** (query scopée `EnfantId`) | @back | ✅ |
| 4 | Récurrence **multi-jours** : un récurrent porte un **set de jours** (école lun/mar/jeu/ven) | @back | ⏳ |
| 5 | **Éditer** une activité récurrente — **toute la série** (jours + plage + lieu) | @back | ⏳ |
| 6 | Config foyer **par enfant** : liste + **créer / éditer / SUPPRIMER** (comble le trou s31) | 🖥️ IHM | ⏳ |
| 7 | Exclusion **vacances scolaires** : plages d'exclusion par activité récurrente, projection les saute | @back | ⏳ |
| 8 | Saisie des **plages de vacances** dans la config de l'activité récurrente | 🖥️ IHM | ⏳ |
| 9 | **Exceptions d'occurrence** (Q4 « cette occurrence ») : modèle d'exceptions par date | @back | ⏳ |
| 10 | Choix **« cette occurrence / toute la série »** dans l'IHM (édition + suppression) | 🖥️ IHM | ⏳ |

`@back` = 6 (2,3,4,5,7,9) · `@ihm` = 4 (1,6,8,10).

---

## Décisions PO tranchées (cadrage) — honorées ci-dessous

- **Q1 Wording = « Activité »** : « slot » → **« activité »** dans l'IHM **ET les routes API** ; routes
  d'activité **IMBRIQUÉES sous l'enfant** (`/api/enfants/{id}/activites…`, l'enfant est le parent naturel
  de la ressource depuis s53). Le référentiel de lieux (« Activités », ex-« Lieux » s35) redevient
  **« Lieux »** (libellé **et** route) et **reste plat** (niveau foyer, liens N-M enfants).
- **Q2 Récurrence = set de jours MINIMAL** : un récurrent porte **plusieurs jours de semaine**, **sans**
  date de fin (les exceptions d'occurrence = Q4, scénarios 9/10).
- **Q3 Surface conf = navigation PAR ENFANT** dans la Config foyer (« onglets par enfant »).
- **Q4 Portée edit = occurrence + série** : « toute la série » (S5/S6) **et** « cette occurrence »
  (exceptions, S9/S10) — **les deux livrés**.
- **Contrainte vacances** : une activité récurrente type école **ne produit pas d'occurrences pendant
  les vacances** (S7/S8).

## PORTE DE CONCEPTION « surface » (garde — fixée AVANT tout scénario @ihm)

- **Surface config des récurrents = navigation PAR ENFANT dans la Config foyer** *(Q3)* : un sélecteur /
  des onglets par enfant ; sélectionner un enfant affiche **la liste de SES activités récurrentes**
  (lieu + jours + plage) avec **créer / éditer / supprimer** par ligne. — *Alternatives écartées* :
  liste **sous l'onglet « Lieux »** (regroupée par lieu, pas par enfant) ; onglet placeholder **« Slot
  récurrent »** unique s20 (sans dimension enfant) → **retiré/repurposé** au profit de la navigation
  par enfant.
- **Renommage = LOT ATOMIQUE de surface** *(garde lot atomique)* : les libellés (S1) **et** les routes
  REST + client HTTP (S2) sont deux faces du même swap `slot → activité` / `Activités → Lieux` ; **pas
  de coexistence** ancien/nouveau. S1 et S2 sont menés **groupés** (les tests Web/Api migrent avec le
  swap), suite complète verte à la bascule.

### Mapping des routes REST (fixé — NESTED sous l'enfant, cohérence ressource + verbes)

> **⚠️ Cette décision LÈVE le cadrage SM précédent** (« le renommage ne churne que les libellés
> visibles, pas les routes »). Le PO a tranché : **les routes publiques changent** et **s'imbriquent
> sous l'enfant** (ressource-parent naturelle depuis l'isolation par `EnfantId` s53). Namespaces / DTO /
> dossiers BC internes : **suivent au minimum ce que le renommage de route impose** (controllers +
> requêtes/vues touchées) ; pas de churn gratuit au-delà.
>
> **Règle de nesting appliquée** : tout ce qui est une **activité** (ponctuelle ou récurrente) est une
> **sous-ressource de l'enfant** → l'`EnfantId` passe du **corps** à l'**URL** (`/api/enfants/{enfantId}/…`)
> — y compris les opérations par id d'item, pour un vocabulaire cohérent **et** un scope défensif (l'id
> d'item doit appartenir à l'enfant de l'URL, sinon 404). Le **référentiel de lieux reste PLAT** : un
> lieu appartient au **foyer** (pas à un enfant), avec des liens N-M vers les enfants.

| Existant (s15/s29/s35) | Nouveau (s54) | Nested/plat + pourquoi |
|---|---|---|
| `POST /api/slots` | `POST /api/enfants/{enfantId}/activites` | **nested** — `EnfantId` du corps → URL |
| `DELETE /api/slots/{id}` | `DELETE /api/enfants/{enfantId}/activites/{id}` | **nested** — cohérence + scope défensif de l'id |
| `GET /api/slots/{a}/{m}/{j}` | `GET /api/enfants/{enfantId}/activites/{a}/{m}/{j}` | **nested** — activités de l'enfant couvrant la date |
| `POST /api/slots/recurrents` | `POST /api/enfants/{enfantId}/activites/recurrentes` | **nested** — `EnfantId` du corps → URL |
| `DELETE /api/slots/recurrents/{id}` | `DELETE /api/enfants/{enfantId}/activites/recurrentes/{id}` | **nested** — supprimer une série de l'enfant |
| *(neuf S3)* | `GET /api/enfants/{enfantId}/activites/recurrentes` | **nested** — récurrentes de l'enfant (liste) |
| *(neuf S5)* | `PUT /api/enfants/{enfantId}/activites/recurrentes/{id}` | **nested** — éditer une série de l'enfant |
| *(neuf S7)* | `POST/DELETE /api/enfants/{enfantId}/activites/recurrentes/{id}/exclusions` | **nested** — plage de vacances d'une série |
| *(neuf S9)* | `DELETE /api/enfants/{enfantId}/activites/recurrentes/{id}/occurrences/{a}/{m}/{j}` | **nested** — exception « cette occurrence » |
| `GET/POST/PUT/DELETE /api/foyer/activites*` | `…/api/foyer/lieux*` | **plat** — référentiel du **foyer** (ex-« Activités » s35) → **« Lieux »** |

> Le référentiel `/api/foyer/activites → /api/foyer/lieux` reste **plat** (foyer-level) et libère le mot
> « activités » pour la ressource nested. Sans ce renommage, `/api/enfants/{id}/activites` (activité
> posée) et `/api/foyer/activites` (référentiel) porteraient le **même mot pour deux concepts** —
> incohérent avec le wording PO. Le back s35 est *reversé* sur ce seul segment.
>
> **Coût maîtrisé (17h)** : le nesting ne change **que le préfixe de route + la source de l'`EnfantId`**
> (URL au lieu du corps) ; les handlers/queries restent inchangés (ils reçoivent déjà un `EnfantId`).
> Pas plus coûteux qu'un renommage plat.

## INVARIANT TRANSVERSE — isolation par enfant (garde — réutilise s53)

| Chemin (neuf/touché) | Scopé enfant ? | Action |
|---|:---:|---|
| Query « récurrentes d'un enfant » (S3) | oui | filtre `EnfantId` obligatoire, jamais de repli global |
| Éditer récurrent (S5) | oui | `EnfantId` **préservé** (jamais réaffecté par l'édition) |
| Multi-jours / projection (S4) | oui | `SlotRecurrent` porte déjà `EnfantId` ; le set de jours ne l'altère pas |
| Surface config par enfant (S6) | oui | l'onglet enfant ne liste/écrit **que** les récurrents de l'enfant sélectionné |
| Vacances (S7) | oui | plage d'exclusion rattachée au récurrent (donc à son enfant) |
| Exception d'occurrence (S9) | oui | l'exception vit sur la série (donc scopée à l'enfant de la série) |

---

## Scénarios (ordre = valeur PO tôt ; tous à livrer)

### 1. Renommage des libellés IHM — lot atomique de surface `@ihm @vert`
```gherkin
Scénario: le menu et les dialogs parlent d'« activité », pas de « slot »
  Étant donné un parent sur le planning
  Quand il clique une case et ouvre le menu d'actions
  Alors l'entrée d'écriture s'intitule « Ajouter une activité » (et non « Poser un slot »)
  Et la dialog est titrée en termes d'« activité »
  Et l'accusé de suppression affiche « Activité supprimée »

Scénario: le référentiel de lieux s'intitule « Lieux »
  Étant donné un parent sur la Config foyer
  Quand il regarde l'onglet du référentiel (ex-« Activités », s35)
  Alors il s'intitule « Lieux »
```

### 2. Renommage des routes REST NESTED + client HTTP — lot atomique `@back @vert`
```gherkin
Scénario nominal: les routes « activités » sont imbriquées sous l'enfant
  Étant donné le mapping de routes nested fixé ci-dessus
  Quand le front pose/supprime/liste une activité (ponctuelle ou récurrente) pour un enfant
  Alors il appelle /api/enfants/{enfantId}/activites* (l'EnfantId est dans l'URL, plus dans le corps)
  Et le référentiel de lieux passe à /api/foyer/lieux* (plat, foyer-level)
  Et les anciennes routes /api/slots* et /api/foyer/activites* n'existent plus

Scénario limite: scope défensif de l'id d'item
  Quand on cible DELETE/PUT une activité par id sous un enfant qui n'en est pas le propriétaire
  Alors la réponse est 404 (l'id d'item doit appartenir à l'enfant de l'URL)

Scénario: cohérence REST (ressource + verbes)
  Alors POST/DELETE/GET/PUT respectent la ressource nested (enfants/{id}/activites[/recurrentes]) et foyer/lieux
  Et Api.Tests + Web.Tests sont migrés dans le MÊME commit (aucune coexistence ancien/nouveau)
```

### 3. Lister les activités récurrentes d'un enfant `@back @vert`
```gherkin
Scénario nominal: la query ne retourne que les récurrents de CET enfant
  Étant donné des activités récurrentes pour Léa et pour Tom
  Quand on liste (GET /api/enfants/{Léa}/activites/recurrentes)
  Alors on obtient uniquement celles de Léa (lieu résolu, jours, plage, id stable)
  Et aucune de Tom n'apparaît

Scénario limite: enfant sans récurrent → liste vide (aucune erreur)
```

### 4. Récurrence multi-jours — set de jours `@back @pending`
```gherkin
Scénario nominal: une activité « École » sur plusieurs jours
  Quand on pose une activité récurrente sur {lundi, mardi, jeudi, vendredi} 8h30→16h30
  Alors elle est projetée sur CHAQUE lun/mar/jeu/ven de la fenêtre
  Et jamais sur le mercredi ni le week-end

Scénario compat: un set d'un seul jour = comportement s29 inchangé
Scénario erreur: set de jours vide → refus AVANT écriture, store intact
```

### 5. Éditer une activité récurrente — toute la série `@back @pending`
```gherkin
Scénario nominal: modifier jours + plage + lieu d'une série (PUT /api/enfants/{id}/activites/recurrentes/{id})
  Étant donné une activité récurrente existante de Léa
  Quand on l'édite (jours {lun,mar}, plage 9h→12h, lieu « Nounou »)
  Alors la projection reflète la nouvelle série
  Et l'EnfantId (Léa) est PRÉSERVÉ

Scénario erreur: durée non positive OU lieu inconnu → refus AVANT écriture, série intacte
```

### 6. Config foyer PAR ENFANT — liste + créer / éditer / SUPPRIMER `@ihm @pending`
```gherkin
Scénario conception: navigation par enfant
  Quand le parent choisit l'enfant Léa (onglet / sélecteur enfant)
  Alors il voit la LISTE des activités récurrentes de Léa (lieu, jours, plage)

Scénario nominal: supprimer un récurrent depuis l'IHM (comble le trou s31)
  Quand le parent clique « Supprimer » sur une ligne
  Alors l'activité disparaît de la liste ET de la grille (câblage réel DELETE)

Scénario nominal: créer et éditer depuis la config par enfant (série entière)
Scénario gating: Invité en lecture seule → affordances créer/éditer/supprimer absentes (R9)
```
> **Gate visuel G3** (PO, direct) après S6. Rebuild du build servi AVANT le gate *(leçon s49)*.

### 7. Exclusion vacances scolaires — modèle minimal `@back @pending`
```gherkin
Scénario nominal: « École » ne tombe pas pendant les vacances
  Étant donné « École » lun→ven pour Léa
  Et une plage d'exclusion [J1..J2] rattachée à cette activité (POST /api/enfants/{id}/activites/recurrentes/{id}/exclusions)
  Quand la grille projette une semaine incluse dans [J1..J2]
  Alors AUCUNE occurrence de l'École n'est projetée ces jours-là

Scénario limite: hors plage d'exclusion → occurrences projetées normalement
```
> **Cadrage minimal (SM)** : plages d'exclusion **saisies manuellement, rattachées à l'activité
> récurrente** (pas d'import d'un calendrier officiel). Alternative écartée : calendrier de vacances
> **du foyer** partagé → non retenu ce sprint (backlog si besoin ultérieur).

### 8. Saisie des plages de vacances par activité `@ihm @pending`
```gherkin
Scénario: déclarer une plage de vacances depuis la config de l'activité récurrente
  Quand le parent ajoute une plage [du .. au ..] à une activité récurrente
  Alors la plage est persistée et la grille cesse de projeter l'activité sur cet intervalle
```

### 9. Exceptions d'occurrence — « cette occurrence » `@back @pending`
```gherkin
Scénario nominal: supprimer une seule occurrence sans toucher la série
  Étant donné « École » lun→ven
  Quand on supprime l'occurrence d'UN mardi précis (DELETE /api/enfants/{id}/activites/recurrentes/{id}/occurrences/{date})
  Alors ce mardi n'a plus l'occurrence, les autres mardis restent projetés
  Et la série d'origine est inchangée (exception par date, persistée)

Scénario limite: ré-exclure la même occurrence = idempotent (no-op)
```

### 10. Choix « cette occurrence / toute la série » dans l'IHM `@ihm @pending`
```gherkin
Scénario: l'IHM propose la portée à l'édition/suppression d'un récurrent
  Quand le parent édite ou supprime une occurrence d'une série depuis la grille
  Alors une invite propose « cette occurrence » OU « toute la série »
  Et le back applique le chemin correspondant (exception S9 vs série entière S5)
```

---

## Priorisation & note de réalité deadline

- **Valeur PO tôt** : S1+S2 (renommage complet libellés **et** routes) d'abord — swap atomique qui
  débloque un vocabulaire cohérent ; puis S3→S6 (le noyau « config par enfant » qui **comble le trou
  s31**, alimenté par la query S3, le multi-jours S4 et l'édition S5) ; puis S7/S8 (vacances) ; puis
  S9/S10 (exceptions d'occurrence — le volet le plus coûteux, modèle d'exceptions par date).
- **Si le temps manque malgré tout** (réalité relayée, non un découpage optionnel) : l'ordre garantit
  qu'un arrêt tardif laisse quand même livrés le renommage complet, la config par enfant et le
  multi-jours — les items les plus visibles pour le PO. S9/S10 sont les derniers atteints.
- **Anti-✅-qui-ment** : un `@ihm` n'est ✅ que **prouvé runtime sur câblage réel** (endpoints réels
  renommés, store Mongo actif), jamais par doublure.

# Retours produit (PO)

<!-- rempli après le gate G3 -->
