# Sprint 42 — Qui récupère ce soir — carte du jour (NOYAU produit, lecture seule)

> **Goal G2 (tranché PO — délégation, goal 1 du SM · PIVOT assumé hors Config foyer)** : après 9
> incréments consécutifs de Config foyer (s32→s41), on **bifurque vers le NOYAU PRODUIT**. On surface
> enfin **LA** promesse de l'app — « **qui récupère l'enfant ce soir, où, et y a-t-il un transfert ?** »
> — en **payoff** de tout le foyer configuré (cycle de fond, périodes, transferts dérivés s31, slots
> de localisation s29, enfants s30). Une tranche verticale **back d'abord** puis IHM, **STRICTEMENT en
> LECTURE** :
> - **@back — query de lecture PURE.** Pour une **DATE cible + l'enfant sélectionné**, restitue : le
>   **responsable RÉSOLU** (surcharge > fond > neutre), le(s) **slot(s) de localisation du jour** (le
>   « où », s29), et le **transfert éventuel** cédant→recevant (saisi OU dérivé s31). **Aucun store
>   neuf, aucune mutation, aucune persistance neuve** ; elle **COMPOSE** les queries/services de
>   résolution **déjà livrés** (contrat **miroir `GrapheFoyerQuery` s38** : PURE, deux adaptateurs
>   InMemory + Mongo, même contrat).
> - **@back — cas limites / repli fidèle.** Aucun responsable résolu = **« personne assignée »**
>   (neutre) ; acteur **orphelin** encore référencé → **repli neutre SANS nom fantôme** (filtre
>   `Resolvable()` s13, miroir R5/R6) ; **pas de transfert** = jour unicolore ; **enfant sans slot** =
>   pas de lieu ; **bords de fenêtre** (jour non chargé).
> - **@ihm — carte « Aujourd'hui : qui / où / transfert »** en tête du planning, résolue pour le
>   **jour courant** et l'**enfant sélectionné** ; **réutilise couleurs/repli de la grille** (aucune
>   teinte réinventée) ; **Parent-gated LECTURE** (l'**Invité VOIT** la carte, lecture seule) ;
>   **convergence SignalR sans rechargement** (diffusion lecture seule s20).
>
> **Ordre d'attaque (backend d'abord, CLAUDE.md)** : @back (query PURE composée : responsable + slots
> + transfert, deux adaptateurs, cas limites/repli) → puis @ihm (carte en tête, gating lecture,
> SignalR).
>
> **HORS scope explicite (périmètre resserré, décision SM)** :
> - **Panneau cloche multi-événements** — liste des **à-venir** (transferts futurs, changements de
>   planning, notifications) : c'est l'**incrément SUIVANT** (Palier 11). Ce sprint livre le **« ce
>   soir » immédiat** (le **jour courant**), pas une timeline.
> - **Toute écriture / action depuis la carte** : la carte est **STRICTEMENT en lecture** — aucun
>   contrôle d'édition, aucune commande émise (l'écriture reste dans les dialogs de pose / la Config).
> - **Réimplémentation de la résolution** : on **compose** la résolution existante (surcharge > fond,
>   transferts s31), on **ne la réécrit pas** (cf. point de vigilance).

## Avancement — 3/5

| # | Scénario | Type | Statut |
|--:|----------|------|:------:|
| 1 | **Query PURE — composer le « qui »** : pour une DATE + l'enfant sélectionné, restitue le **responsable RÉSOLU** (surcharge > fond > neutre) + son nom/couleur, en **composant** la résolution existante ; **aucun store neuf, aucune mutation** ; contrat **miroir `GrapheFoyerQuery` s38** ; identique sur les **deux adaptateurs** (InMemory + Mongo durable) | back | ✅ |
| 2 | **Composer le « où » + le transfert du jour** : le(s) **slot(s) de localisation** du jour (s29) et le **transfert éventuel** cédant→recevant (**saisi OU dérivé s31**, priorité SAISI > DÉRIVÉ) sont restitués dans le même payload ; jour **sans transfert** = unicolore ; jour **sans slot** = pas de lieu | back | ✅ |
| 3 | **Cas limites / repli fidèle (erreur + neutre)** : aucun responsable résolu = **« personne assignée »** (neutre) ; acteur **orphelin** → **repli neutre SANS nom fantôme** (filtre `Resolvable()` s13) ; **bord de fenêtre** (jour non chargé) ; **store vide** = carte neutre sans crash ; identique sur les deux adaptateurs | back | ✅ |
| 4 | **Carte « Aujourd'hui » en tête du planning** : rend **qui / où / transfert** pour le **jour courant** + l'**enfant sélectionné**, en **réutilisant couleurs/repli de la grille** (aucune teinte réinventée) ; transfert = rendu bicolore réutilisé (présentation s29) ; **STRICTEMENT lecture** (aucun contrôle d'édition) | 🖥️ IHM | ⏳ |
| 5 | **Parent-gated LECTURE + convergence SignalR** : l'**Invité VOIT** la carte (lecture seule, aucune action) ; un changement pertinent (période/transfert/slot) fait **CONVERGER** la carte d'un 2ᵉ écran **sans rechargement**, via le canal SignalR de **lecture seule** (aucune écriture par la diffusion, s20 préservé) | 🖥️ IHM | ⏳ |

> **⚠️ Point de vigilance — COMPOSER, ne PAS réimplémenter la résolution (Sc.1-3, décision SM).** Le
> « qui » du jour est **déjà** résolu ailleurs (résolution **surcharge > fond > neutre** du palier 6,
> transferts saisis/dérivés s31, projection `GrilleAgendaQuery`). Cette query est un **agrégateur de
> lecture** : elle **appelle/compose** les services & queries existants pour une date + un enfant —
> **INTERDIT** de recopier la logique de priorité surcharge>fond ou la dérivation de transfert (source
> unique = le domaine existant, sous peine de deux vérités divergentes). Miroir strict de la démarche
> **`GrapheFoyerQuery` s38** (PURE, compose l'existant, deux adaptateurs, zéro store neuf).

> **⚠️ Repli neutre — JAMAIS de nom d'acteur orphelin (Sc.3).** Un responsable dont l'id stable n'est
> **plus** dans le store (acteur supprimé) doit retomber sur le **repli neutre** (« personne
> assignée ») **sans jamais** afficher un nom/couleur fantôme — filtre `Resolvable()` s13, miroir des
> replis R5/R6 (grille, légende, graphe s38). Un « qui récupère ce soir » qui afficherait un acteur
> supprimé serait un **vert-qui-ment**.

> **⚠️ Anti-vert-qui-ment — preuve runtime sur profil de données RÉALISTE (Sc.1-5).** La carte doit
> être prouvée sur **plusieurs profils de jour distincts** : un jour dont le responsable vient d'une
> **surcharge** (période saisie), un jour résolu par le **cycle de fond**, un jour **avec transfert**
> (saisi et/ou dérivé s31), un jour **neutre** (aucun responsable). Un test qui ne verrait qu'un seul
> profil (ou un store seedé trivial) surestimerait la couverture. Preuve finale = **round-trip runtime
> réel (Mongo durable)** + **gate navigateur PO** sur ces profils.

> **⚠️ Séparation des canaux — carte = LECTURE seule (Sc.4-5).** La carte n'émet **aucune** commande
> et n'expose **aucun** contrôle d'écriture ; sa convergence temps réel passe **exclusivement** par la
> **diffusion SignalR de lecture** (s20) — **jamais** une écriture par la diffusion (invariant
> structurel CLAUDE.md). Écriture = canal requête/réponse (dialogs de pose / Config), **hors** de cette
> carte.

---

## Scénarios

### Sc.1 — Query PURE : composer le « qui » du jour (responsable résolu) @back @vert
```gherkin
Étant donné un foyer configuré (acteurs s30, cycle de fond, périodes de garde) et un enfant sélectionné
Et une DATE cible dont le responsable est résolu par la résolution existante (surcharge > fond > neutre, palier 6)
Quand j'interroge la query de lecture « qui récupère ce jour-là » pour cette date + cet enfant
Alors elle restitue le RESPONSABLE RÉSOLU (id stable) avec son nom et sa couleur, tels que la grille les résout
Et le « qui » provient de la COMPOSITION de la résolution existante — aucune logique de priorité surcharge>fond réimplémentée
Et la query est PURE : aucune mutation, aucun store neuf, aucune persistance neuve (miroir GrapheFoyerQuery s38)
Et le résultat est IDENTIQUE sur les DEUX adaptateurs (InMemory seedé ET Mongo durable, même contrat)
```

### Sc.2 — Composer le « où » (slots) + le transfert du jour @back @vert
```gherkin
Étant donné la query de lecture du jour (Sc.1) pour une date + l'enfant sélectionné
Et cette date porte un ou plusieurs SLOTS de localisation (s29) et un TRANSFERT de responsabilité
Quand j'interroge la query
Alors elle restitue AUSSI le(s) slot(s) de localisation du jour (le « où » de la garde, s29) dans le même payload
Et elle restitue le TRANSFERT éventuel cédant → recevant (noms + couleurs résolus), qu'il soit SAISI ou DÉRIVÉ (s31), priorité SAISI > DÉRIVÉ (aucun doublon)
Et un jour SANS transfert est restitué unicolore (aucun cédant/recevant, présentation s29 inchangée)
Et un jour SANS slot est restitué SANS lieu (le « où » est simplement absent, pas d'erreur)
Et le transfert est LU sans être modifié (composition de la dérivation s31 existante, non réimplémentée)
```

### Sc.3 — Cas limites / repli fidèle : neutre, orphelin, bord de fenêtre @back @vert
```gherkin
Étant donné la query de lecture du jour (Sc.1)
Quand la date cible n'a AUCUN responsable résolu (ni surcharge, ni fond)
Alors la query restitue un état NEUTRE explicite « personne assignée » (aucun nom, aucune couleur fantôme)

Étant donné une date dont le responsable résolu pointe un acteur ORPHELIN (id stable absent du store, supprimé)
Quand j'interroge la query
Alors le responsable retombe sur le REPLI NEUTRE sans nom ni couleur fantôme (filtre Resolvable s13, miroir R5/R6)

Étant donné une date en BORD de fenêtre (jour non chargé) ou un store VIDE
Quand j'interroge la query
Alors elle restitue un état neutre sans crash (aucune racine fantôme), à l'identique sur les DEUX adaptateurs
```

### Sc.4 — Carte « Aujourd'hui » en tête du planning (rendu lecture seule) @ihm @pending
```gherkin
Étant donné le planning ouvert, un enfant sélectionné, et le jour courant résolu par la query (Sc.1-3)
Quand la page de planning s'affiche
Alors une CARTE « Aujourd'hui » est rendue EN TÊTE, affichant QUI récupère l'enfant ce soir / OÙ (slots) / le TRANSFERT éventuel
Et le nom et la couleur du responsable RÉUTILISENT la résolution couleurs/repli de la grille (aucune teinte réinventée)
Et un transfert du jour est matérialisé par le rendu BICOLORE réutilisé (présentation s29), un jour sans transfert reste unicolore
Et un jour neutre affiche « personne assignée » (repli Sc.3), sans nom fantôme
Et la carte est STRICTEMENT en LECTURE : aucun contrôle d'édition, aucune commande émise depuis la carte
```

### Sc.5 — Parent-gated LECTURE + convergence SignalR @ihm @pending
```gherkin
Étant donné une identité EFFECTIVE non-Parent (Invité)
Quand j'ouvre le planning
Alors l'Invité VOIT la carte « Aujourd'hui » (lecture seule) — la lecture n'est pas gatée, aucune action d'écriture n'y est atteignable

Étant donné deux écrans planning ouverts sur le même enfant et le même jour courant
Quand une écriture pertinente survient sur le 1ᵉʳ écran (période/transfert saisi, slot posé/supprimé) via le canal d'écriture
Alors la carte « Aujourd'hui » du 2ᵉ écran CONVERGE (qui/où/transfert recalculés) SANS rechargement
Et la convergence passe EXCLUSIVEMENT par le canal SignalR de LECTURE SEULE (aucune écriture par la diffusion, s20 préservé)
```

---

# Retours produit (PO)
