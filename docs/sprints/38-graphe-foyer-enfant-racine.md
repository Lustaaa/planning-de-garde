# Sprint 38 — Vue foyer « graphe enfant-racine » (lecture seule)

> **Goal G2 (tranché PO — délégation, goal 1 du SM)** : **RESTITUER visuellement** le foyer
> câblé au fil des sprints (lien enfant↔parent posé s34, éligibilité role-flag s36, rôle-du-lien
> père/mère/parent-libre s37) sous forme d'une **VUE en LECTURE SEULE** — un **graphe avec
> l'ENFANT en RACINE** et ses parents en branches — affichée **à l'arrivée sur la Config du
> foyer** (briefing PO direct : « quand on arrive sur la configuration du foyer, une vue en
> lecture seule affichée comme un graph avec comme racine l'enfant »). Une tranche verticale :
> - **@back neuf — query de lecture agrégée.** Une query de lecture (ex. `GrapheFoyerQuery`)
>   restitue, **PAR enfant**, ses **parents liés** avec leur **rôle-du-lien** {père, mère,
>   parent-libre} (s37). **Lecture PURE** — aucune mutation, aucun nouveau store, **deux
>   adaptateurs** (InMemory + Mongo) réutilisés. Le graphe **reflète les liens RÉELS** du store.
> - **@ihm — surface NEUVE de lecture.** Une vue rendant chaque **enfant en RACINE**, ses parents
>   en branches « **nom (rôle-du-lien)** » ; store vide → **message neutre, zéro fantôme** ;
>   familles recomposées **visibles par construction** ; **Parent-gated lecture** (Invité voit la
>   vue) + convergence **SignalR** (un lien modifié dans la modal Enfants converge le graphe).
>
> **Ordre d'attaque (backend d'abord, CLAUDE.md)** : @back (query agrégée + reflet fidèle des
> liens réels) → puis @ihm (rendu graphe enfant-racine + recomposé + gating + SignalR).
>
> **HORS scope explicite (périmètre resserré, décision SM)** :
> - **R3 « exactement 2 parents » / statut de complétude du couple** (père manquant, mère
>   manquante) : **goal séparé**. Ce sprint **n'impose et ne signale AUCUNE** complétude — il
>   **restitue** les liens tels qu'ils existent (0, 1 ou 2 parents), sans nouvel invariant.
> - **Édition DEPUIS le graphe** : la vue est **STRICTEMENT lecture seule**. Aucune commande n'est
>   émise depuis le graphe ; l'écriture reste dans la modal Enfants (s34/s37), non retouchée ici.
>   *(L'« édition fantôme au clic » du brief est un volet distinct, tension inline vs modal à
>   arbitrer en G2 — hors scope.)*
> - **Graphe ÉTENDU au-delà d'enfant→parents** : grands-parents, parents liés entre eux via leurs
>   enfants (graphe foyer complet R3), lien enfant↔activité (s35) dans le graphe : **non traités**
>   ce sprint. Le graphe s'arrête à la relation **enfant → parents liés**.
> - **Vue planning centrée couple / vue recomposée du planning** (bonus brief 2ᵉ temps) : hors
>   goal — ici c'est la vue **Config foyer**, pas le planning.

## Avancement — 3/5

| # | Scénario | Type | Statut |
|--:|----------|------|:------:|
| 1 | **Query de lecture agrégée** (ex. `GrapheFoyerQuery`) : PAR enfant, ses parents liés avec rôle-du-lien {père/mère/parent-libre} (s37) — lecture PURE, deux adaptateurs (InMemory + Mongo) | back | ✅ |
| 2 | Le graphe **reflète les liens RÉELS** du store (s34/s36/s37) : parents non liés **absents** ; enfant sans parent = **racine isolée** (0 parent accepté s34) ; ordre/forme stables | back | ✅ |
| 3 | **Vue lecture seule** à l'arrivée sur Config foyer : chaque **enfant en RACINE**, branches parents « **nom (rôle-du-lien)** » ; store vide → **message neutre, zéro fantôme** | 🖥️ IHM | ✅ |
| 4 | **Familles recomposées visibles** : deux enfants de parents distincts = **deux racines** ; un **parent partagé** apparaît sur les deux enfants (reflet des liens réels, aucun nouvel invariant) | 🖥️ IHM | ⏳ |
| 5 | **Parent-gated lecture** (Invité voit la vue) + convergence **SignalR** : lier/délier/changer un rôle-du-lien depuis la modal Enfants fait **CONVERGER le graphe sans rechargement** (diffusion lecture seule) | 🖥️ IHM | ⏳ |

> **⚠️ Point de vigilance — OÙ vit la vue (décision SM, tranchée).** Le brief PO dit « **quand on
> arrive sur la Config du foyer** » → la vue est rendue **au chargement de `/configuration`**
> (vue d'accueil de la Config foyer, avant / au-dessus des onglets, ou onglet par défaut selon la
> structure existante — au jugement dev-team, tant que c'est ce qu'on voit **à l'arrivée**). C'est
> une **SURFACE NEUVE de lecture**, **PAS un swap** d'une surface existante : **aucune garde
> lot-atomique requise** (rien n'est retiré, on ajoute une vue). Réutiliser le **patron
> d'affichage lecture** existant (tableaux lecture seule Config foyer, hub SignalR lecture s20) si
> pertinent, sans dupliquer un chemin de lecture.

> **⚠️ Lecture PURE — aucune écriture, borne anti-cliquet (Sc.1-2).** La query agrégée est un
> **chemin de LECTURE** : elle **compose** les données déjà persistées (référentiel enfants s30 +
> liens enfant↔parent s34/s37 + éligibilité role-flag s36 + noms d'acteurs s5) **sans aucune
> mutation**, **sans nouveau store**, **sans persistance neuve**. Elle réutilise les **deux
> adaptateurs** existants (InMemory seedé / Mongo durable). Si la donnée nécessaire est **déjà
> exposée** par une query de config existante (early-green possible : la query Enfants s34/s37
> restitue déjà parents + rôle-du-lien par enfant), la dev-team le **signale** au SM qui tranche
> (query dédiée `GrapheFoyerQuery` vs réutilisation/adaptation de la query existante + projection
> IHM) **sans** réinventer un chemin de lecture parallèle divergent.

> **⚠️ Reflet FIDÈLE, zéro fantôme (Sc.2-3).** Le graphe résout les branches **exclusivement**
> depuis le **store vivant** (id stable, jamais un libellé en dur) : un parent **non lié**
> n'apparaît **pas** sous un enfant ; un acteur **supprimé / orphelin** ne laisse **aucun nom
> fantôme** (miroir R5/R6, filtre `Resolvable()` s13) ; un enfant **sans parent** est une
> **racine isolée** légitime (0 parent accepté s34). **Store vide** (Mongo 1er lancement, asymétrie
> seed s15) → **message neutre** (« Aucun enfant, ajoutez-en. » ou équivalent), **jamais** de nœud
> fantôme.

---

## Scénarios

### Sc.1 — Query de lecture agrégée : enfants → parents liés (avec rôle-du-lien) @back @vert
```gherkin
Étant donné un foyer avec des enfants déclarés (référentiel s30) et des liens enfant↔parent posés
  (s34), certains portant un rôle-du-lien père / mère / parent-libre (s37)
Quand la query de lecture du graphe foyer (ex. « GrapheFoyerQuery ») est exécutée
Alors elle restitue, PAR enfant, la liste de ses parents liés avec, pour chacun, son NOM et son rôle-du-lien
Et c'est une LECTURE PURE : aucune mutation, aucun store neuf, aucune persistance neuve (borne anti-cliquet)
Et elle est réalisée sur les DEUX adaptateurs (InMemory seedé ET Mongo durable), même contrat
Et un lien s34 sans rôle-du-lien explicite est restitué à « parent-libre » (défaut neutre s37, aucune régression)
Et le rôle-du-lien restitué N'INTERVIENT PAS dans la résolution grille/légende ni le gating (présentation seule, R10)
```

### Sc.2 — Le graphe reflète les liens RÉELS du store @back @vert
```gherkin
Étant donné un enfant lié à un seul parent-acteur et un autre enfant sans aucun parent lié
Quand la query du graphe foyer est exécutée
Alors le premier enfant expose exactement son parent lié (nom + rôle-du-lien), aucun acteur non lié en branche
Et le second enfant est une RACINE ISOLÉE (0 parent, cas accepté s34), sans nœud fantôme
Étant donné un acteur supprimé du référentiel (orphelin) encore référencé par un lien résiduel
Alors aucune branche fantôme n'est produite (repli sans nom fantôme, miroir R5/R6 et filtre Resolvable s13)
Étant donné un store de foyer VIDE (aucun enfant — Mongo 1er lancement, asymétrie seed s15)
Alors la query restitue un graphe VIDE (aucune racine), sans erreur
```

### Sc.3 — Vue lecture seule à l'arrivée sur Config foyer : enfant en racine @ihm @vert
```gherkin
Étant donné que j'arrive sur /configuration en tant que Parent
Quand la Config du foyer est rendue
Alors une VUE en LECTURE SEULE affiche le foyer comme un GRAPHE avec chaque ENFANT en RACINE
Et sous chaque enfant, ses parents liés apparaissent en branches, affichés « nom (rôle-du-lien) » (père / mère / parent)
Et la vue est STRICTEMENT en lecture : aucun contrôle d'édition, aucune commande émise depuis le graphe
Étant donné un foyer SANS aucun enfant (store vide, Mongo 1er lancement)
Quand j'arrive sur la Config du foyer
Alors la vue affiche un MESSAGE NEUTRE (« Aucun enfant, ajoutez-en. » ou équivalent), zéro nœud fantôme
```

### Sc.4 — Familles recomposées visibles par construction @ihm @pending
```gherkin
Étant donné deux enfants liés à des parents-acteurs DIFFÉRENTS
Quand la vue graphe est rendue
Alors chaque enfant apparaît comme une RACINE distincte, avec ses propres branches parents
Étant donné un parent-acteur lié à DEUX enfants distincts (parent partagé)
Quand la vue graphe est rendue
Alors ce parent apparaît en branche SOUS chacun des deux enfants (reflet fidèle des liens réels)
Et AUCUN nouvel invariant n'est imposé (ni « exactement 2 parents », ni complétude du couple — hors scope)
Et le graphe reste cohérent quel que soit le nombre d'enfants et de parents partagés
```

### Sc.5 — Parent-gated lecture + convergence SignalR @ihm @pending
```gherkin
Étant donné une identité EFFECTIVE non-Parent (Invité)
Quand j'arrive sur la Config du foyer
Alors la vue graphe reste VISIBLE en lecture seule (Invité voit la vue), sans aucun contrôle d'édition
Étant donné deux écrans /configuration ouverts, le graphe rendu sur les deux
Quand un lien enfant↔parent est ajouté / supprimé, ou un rôle-du-lien modifié, depuis la modal Enfants du 1ᵉʳ écran
Alors le graphe du 2ᵉ écran CONVERGE (racine, branches, nom + rôle-du-lien) SANS rechargement
Et la convergence se fait par le canal SignalR de LECTURE SEULE (aucune écriture par la diffusion, s20 préservé)
```

---

# Retours produit (PO)
