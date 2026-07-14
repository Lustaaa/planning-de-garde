# Sprint 40 — Complétude du couple R3 — badge « couple incomplet » (LECTURE seule)

> **Goal G2 (tranché PO — délégation, goal 1 du SM)** : **SIGNALER** (jamais imposer) la
> complétude du couple d'un enfant — payoff direct des liens posés au fil des sprints (lien
> enfant↔parent s34, éligibilité role-flag s36, **rôle-du-lien père/mère/parent-libre s37**) et
> de la vue graphe s38. Une tranche verticale de **LECTURE** :
> - **@back — statut de complétude PUR par enfant.** Composer un **statut R3** {complet /
>   incomplet / vide} **par enfant** à partir des données **déjà persistées** (liens s34 +
>   rôle-du-lien s37) — **AUCUN store neuf, AUCUNE mutation, AUCUN blocage d'écriture**. Réutiliser
>   la projection `GrapheFoyerQuery` s38 (l'enrichir) plutôt qu'une query neuve.
> - **@ihm — badge de complétude en LECTURE.** Une pastille/badge « couple incomplet » /
>   « complet » par enfant sur la vue graphe s38 (et/ou la colonne du tableau Enfants) —
>   **strictement lecture**, aucune commande émise ; **Parent-gated lecture** (Invité voit le
>   badge) ; convergence **SignalR par reprojection client** (lier/délier/changer un rôle-du-lien
>   fait converger le badge d'un 2ᵉ écran **sans rechargement, 0 GET** — garde conception s38).
>
> **Ordre d'attaque (backend d'abord, CLAUDE.md)** : @back (statut PUR + règle R3 + non-blocage
> d'écriture, deux adaptateurs) → puis @ihm (badge lecture + gating + SignalR reprojection).
>
> **HORS scope explicite (périmètre resserré, décision SM)** :
> - **TOUT blocage d'écriture lié à R3** : on ne refuse **JAMAIS** d'enregistrer / de lier un
>   enfant à **0 ou 1 parent**, ni de le laisser sans couple père+mère. **R3 est SIGNALÉE, PAS
>   IMPOSÉE** ce sprint — le graphe et la modal Enfants (s34/s37) continuent d'**accepter 0/1/2**
>   parents tels quels, **aucun nouvel invariant d'écriture**. *(Imposer « exactement 2 » à la
>   pose serait un changement de règle métier à part entière — goal séparé, hors goal.)*
> - **Édition DEPUIS le graphe** (crayon sur nœud → modal) : **goal séparé**, la vue reste
>   strictement lecture.
> - **Graphe ÉTENDU** (grands-parents, parents liés entre eux, lien enfant↔activité s35) : hors
>   scope, on s'arrête à la relation enfant → parents liés (déjà restituée s38).

## Avancement — 5/5

| # | Scénario | Type | Statut |
|--:|----------|------|:------:|
| 1 | **Statut de complétude PUR par enfant** composé des données déjà persistées (liens s34 + rôle-du-lien s37) — LECTURE PURE, deux adaptateurs (InMemory + Mongo), enrichit `GrapheFoyerQuery` s38 (pas de query neuve) | back | ✅ |
| 2 | **Règle R3 explicite** : {complet = un père ET une mère} · {incomplet = 0/1 parent, OU 2 sans le couple père+mère (ex. deux « parent-libre »)} · {vide = racine sans parent} ; cas limites (orphelin exclu du décompte, miroir Resolvable s13) | back | ✅ |
| 3 | **AUCUN blocage d'écriture** : lier/délier/enregistrer un enfant à 0/1/2 parents reste accepté (R3 signalée, pas imposée) — le calcul du statut ne modifie ni ne refuse aucune écriture ; même contrat sur les deux adaptateurs | back | ✅ |
| 4 | **Badge de complétude en LECTURE** par enfant sur la vue graphe s38 (et/ou colonne tableau Enfants) — « couple incomplet » / « complet » / « aucun parent » ; STRICTEMENT lecture, aucun contrôle d'édition, aucune commande émise | 🖥️ IHM | ✅ |
| 5 | **Parent-gated lecture** (Invité voit le badge) + convergence **SignalR par reprojection client** : lier/délier/changer un rôle-du-lien depuis la modal Enfants fait **CONVERGER le badge d'un 2ᵉ écran sans rechargement, 0 GET** (diffusion lecture seule, garde s38) | 🖥️ IHM | ✅ |

> **⚠️ Point de vigilance — définition du statut « 2 parents mais pas père+mère » (décision SM,
> tranchée).** L'invariant d'exclusivité s37 interdit **deux « père »** et **deux « mère »** sur un
> enfant, MAIS **deux « parent-libre »** (ou 1 père + 1 parent-libre, 1 mère + 1 parent-libre)
> **restent possibles**. La règle R3 « **toujours exactement 2 parents = un père ET une mère** » →
> **complet SSI l'enfant porte un lien « père » ET un lien « mère »**. Tout autre cas à 2 parents
> (deux parent-libre, père+parent-libre, mère+parent-libre) = **INCOMPLET**. 0 ou 1 parent =
> **incomplet**. Aucun parent → statut **« vide »** distinct (racine isolée légitime s38, ce n'est
> pas une anomalie à alarmer mais un état neutre). **À figer noir sur blanc dans la projection.**

> **⚠️ Lecture PURE — réutiliser `GrapheFoyerQuery` s38, pas de query parallèle (Sc.1-3).** Le
> statut se **compose** des données **déjà exposées** par `GrapheFoyerQuery` (par enfant → parents
> liés + rôle-du-lien s37). **Enrichir cette projection** d'un champ statut calculé plutôt que
> créer une query concurrente divergente : **aucune mutation, aucun store neuf, aucune persistance
> neuve** (borne anti-cliquet). Réutiliser les **deux adaptateurs** existants (InMemory seedé /
> Mongo durable, même contrat). Le statut est **présentation seule** (n'intervient ni dans la
> résolution grille/légende ni dans le gating, R10).

> **⚠️ Reflet FIDÈLE du décompte, zéro fantôme (Sc.2).** Le décompte des parents pour le statut
> résout **exclusivement** depuis le **store vivant** (id stable) : un acteur **supprimé /
> orphelin** encore référencé par un lien résiduel **n'est PAS compté** (miroir R5/R6, filtre
> `Resolvable()` s13) — un enfant dont le seul lien pointe un orphelin est **incomplet**, pas
> faussement « complet ». Un lien s34 **sans rôle-du-lien explicite** compte comme **« parent-libre »**
> (défaut neutre s37) → **ne satisfait donc PAS** à lui seul « père ET mère ».

> **⚠️ Non-blocage d'écriture (Sc.3) — R3 signalée, pas imposée.** Le calcul du statut est un
> **chemin de lecture** greffé sur la projection ; il **ne touche à aucun handler d'écriture**.
> `LierEnfantParent` / délier / la modal Enfants continuent d'**accepter 0, 1 ou 2 parents** et
> tout jeu de rôles-du-lien valide s37 **sans nouveau refus**. Un test de non-régression **prouve**
> qu'enregistrer un enfant « incomplet » **réussit** (aucune contrainte « exactement 2 » ajoutée).

---

## Scénarios

### Sc.1 — Statut de complétude PUR par enfant (enrichit `GrapheFoyerQuery` s38) @back @vert
```gherkin
Étant donné un foyer avec des enfants déclarés (référentiel s30) et des liens enfant↔parent posés
  (s34), portant des rôles-du-lien père / mère / parent-libre (s37)
Quand la projection de lecture du graphe foyer (`GrapheFoyerQuery` s38) est exécutée
Alors elle restitue, PAR enfant, en plus de ses parents liés, un STATUT de complétude du couple
Et ce statut est composé UNIQUEMENT des données déjà persistées (liens s34 + rôle-du-lien s37) —
  LECTURE PURE : aucune mutation, aucun store neuf, aucune persistance neuve (borne anti-cliquet)
Et il est réalisé sur les DEUX adaptateurs (InMemory seedé ET Mongo durable), même contrat
Et le statut est PRÉSENTATION SEULE : il n'intervient ni dans la résolution grille/légende ni dans le gating (R10)
Et aucune query parallèle n'est créée : la projection existante s38 est ENRICHIE d'un champ calculé
```

### Sc.2 — Règle R3 explicite + cas limites (décompte fidèle, zéro fantôme) @back @vert
```gherkin
Étant donné un enfant lié à un « père » ET une « mère » (rôles-du-lien s37)
Quand la projection du graphe foyer est exécutée
Alors son statut est COMPLET (un père ET une mère présents)

Étant donné un enfant lié à DEUX parents « parent-libre » (aucun père, aucune mère explicites)
Alors son statut est INCOMPLET (2 parents mais pas le couple père+mère)

Étant donné un enfant lié à UN SEUL parent (père, mère ou parent-libre)
Alors son statut est INCOMPLET (moins de deux parents)

Étant donné un enfant SANS aucun parent lié (racine isolée légitime s38)
Alors son statut est VIDE (aucun parent), état neutre distinct de « incomplet », sans erreur

Étant donné un enfant dont le seul lien pointe un acteur SUPPRIMÉ / orphelin (référence résiduelle)
Alors l'orphelin N'EST PAS compté (miroir R5/R6, filtre Resolvable s13) et l'enfant est INCOMPLET (pas faussement complet)
Et un lien s34 sans rôle-du-lien explicite compte comme « parent-libre » (défaut neutre s37, ne satisfait pas seul « père ET mère »)
```

### Sc.3 — AUCUN blocage d'écriture : R3 signalée, jamais imposée @back @vert
```gherkin
Étant donné un enfant sans parent, ou avec un seul parent, ou avec deux « parent-libre »
Quand j'enregistre / lie / délie ce parent via le canal d'écriture existant (LierEnfantParent, délier, modal Enfants)
Alors l'écriture RÉUSSIT dans tous les cas (0, 1 ou 2 parents acceptés, comme s34)
Et AUCUN nouvel invariant « exactement 2 parents » n'est imposé à la pose ni à l'enregistrement
Et le calcul du statut de complétude ne modifie, ne refuse et ne déclenche AUCUNE écriture (chemin de lecture pur)
Et ce non-blocage est vérifié à l'identique sur les DEUX adaptateurs (InMemory ET Mongo durable)
```

### Sc.4 — Badge de complétude en LECTURE par enfant @ihm @vert
```gherkin
Étant donné que j'arrive sur /configuration en tant que Parent, la vue graphe foyer (s38) rendue
Quand un enfant a un père ET une mère
Alors un BADGE « couple complet » (ou équivalent) est affiché sur son nœud/sa ligne
Quand un enfant a 0/1 parent, ou 2 parents sans le couple père+mère
Alors un BADGE « couple incomplet » est affiché sur son nœud/sa ligne
Quand un enfant n'a aucun parent lié
Alors un état neutre « aucun parent » (ou équivalent) est affiché, distinct de « incomplet », sans alarme
Et le badge est STRICTEMENT en lecture : aucun contrôle d'édition, aucune commande émise depuis le badge/le graphe
Et le badge est rendu sur la vue graphe s38 (et/ou la colonne du tableau Enfants), sans dupliquer un chemin de lecture
```

### Sc.5 — Parent-gated lecture + convergence SignalR par reprojection client @ihm @vert
```gherkin
Étant donné une identité EFFECTIVE non-Parent (Invité)
Quand j'arrive sur la Config du foyer
Alors les badges de complétude restent VISIBLES en lecture seule (Invité voit le badge), sans aucun contrôle d'édition

Étant donné deux écrans /configuration ouverts, le graphe et ses badges rendus sur les deux
Quand un lien enfant↔parent est ajouté / supprimé, ou un rôle-du-lien modifié, depuis la modal Enfants du 1ᵉʳ écran
Alors le BADGE de complétude du 2ᵉ écran CONVERGE (complet ↔ incomplet ↔ vide) SANS rechargement
Et la convergence se fait par REPROJECTION CLIENT depuis le payload diffusé (aucun GET sur push, garde conception s38)
Et la diffusion reste le canal SignalR de LECTURE SEULE (aucune écriture par la diffusion, s20 préservé)
```

---

# Retours produit (PO)
