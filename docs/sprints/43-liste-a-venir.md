# Sprint 43 — Liste « À venir » — qui récupère les prochains jours (NOYAU produit, lecture seule)

> **Goal G2 (tranché PO — délégation, goal 1 du SM · suite du NOYAU PRODUIT)** : après le 1ᵉʳ
> incrément du noyau produit (carte « Aujourd'hui » s42), on **PROLONGE** la promesse de l'app du
> **« ce soir » immédiat** vers les **prochains jours** — « **qui récupère demain / cette semaine, où,
> et y a-t-il un transfert ?** ». Une **liste « À venir »** sous la carte du jour, en **payoff** du
> foyer configuré (cycle de fond, périodes, transferts dérivés s31, slots s29, enfants s30). Tranche
> verticale **back d'abord** puis IHM, **STRICTEMENT en LECTURE**, **miroir strict du patron s42** :
> - **@back — query de lecture PURE `AVenirQuery` qui COMPOSE `GrilleAgendaQuery`.** Pour les **N
>   prochains jours depuis aujourd'hui** (sur la **fenêtre de grille déjà chargée**) et l'**enfant
>   sélectionné**, restitue une **liste ordonnée** de jours à venir portant chacun : le **responsable
>   RÉSOLU** (surcharge > fond > neutre), le(s) **slot(s) de localisation** du jour (le « où », s29), et
>   le **transfert éventuel** cédant→recevant (saisi OU dérivé s31). **Aucun store neuf, aucune
>   mutation, aucune persistance neuve** ; elle **RÉUTILISE** la résolution existante — miroir
>   `CarteDuJourQuery` s42 / `GrapheFoyerQuery` s38 (PURE, deux adaptateurs InMemory + Mongo, même
>   contrat).
> - **@back — cas limites / repli fidèle (miroir s42).** Aucun responsable résolu sur un jour =
>   **« personne assignée »** (neutre) ; acteur **orphelin** → **repli neutre SANS nom fantôme**
>   (filtre `Resolvable()` s13, miroir R5/R6) ; jour **sans transfert** = unicolore ; jour **sans slot**
>   = pas de lieu ; **aucun événement à venir** dans la fenêtre = **liste vide / message neutre**.
> - **@ihm — panneau/liste « À venir »** sous la carte « Aujourd'hui » s42 (demain / cette semaine),
>   **STRICTEMENT lecture** ; **réutilise couleurs/repli de la grille** (aucune teinte réinventée) ;
>   l'**Invité VOIT** la liste (lecture non gatée) ; **reprojection client** depuis la fenêtre de grille
>   déjà chargée ; **convergence SignalR par reprojection client** (0 GET sur push, garde
>   anti-amplification flake s42).
>
> **Ordre d'attaque (backend d'abord, CLAUDE.md)** : @back (query PURE composée : liste ordonnée
> responsable + slots + transfert par jour, deux adaptateurs, cas limites/repli) → puis @ihm (panneau
> sous la carte, gating lecture, reprojection SignalR).
>
> **HORS scope explicite (périmètre resserré, décision SM)** :
> - **Notifications / alertes push** (cloche qui signale un CHANGEMENT, badge « non-lu », diff) : ce
>   sprint livre l'**AFFICHAGE** de la liste des à-venir, **pas** le mécanisme de notification de
>   changement (incrément suivant, Palier 11 / Épic 9).
> - **Carte / liste PERSISTANTE hors de la semaine chargée** : **même limitation assumée que s42** — la
>   liste se reprojette depuis la **fenêtre de grille chargée**, les à-venir **au-delà** de cette
>   fenêtre ne sont pas affichés (goal séparé « carte persistante », arbitrage GET vs flake non
>   tranché).
> - **Toute écriture / action depuis la liste** : la liste est **STRICTEMENT en lecture** — aucun
>   contrôle d'édition, aucune commande émise (l'écriture reste dans les dialogs de pose / la Config).
> - **Réimplémentation de la résolution** : on **compose** la résolution existante (surcharge > fond,
>   transferts s31, slots s29), on **ne la réécrit pas** (cf. point de vigilance).

## Avancement — 4/5

| # | Scénario | Type | Statut |
|--:|----------|------|:------:|
| 1 | **Query PURE — liste ordonnée des « à venir »** : pour les **N prochains jours depuis aujourd'hui** (fenêtre de grille chargée) + l'enfant sélectionné, restitue une **liste ordonnée** de jours portant chacun le **responsable RÉSOLU** (surcharge > fond > neutre), en **composant** la résolution existante ; **aucun store neuf, aucune mutation** ; contrat **miroir `CarteDuJourQuery` s42** ; identique sur les **deux adaptateurs** (InMemory + Mongo durable) | back | ✅ |
| 2 | **Composer le « où » + le transfert par jour** : chaque entrée de la liste porte **AUSSI** le(s) **slot(s) de localisation** du jour (s29) et le **transfert éventuel** cédant→recevant (**saisi OU dérivé s31**, priorité SAISI > DÉRIVÉ) ; jour **sans transfert** = unicolore ; jour **sans slot** = pas de lieu ; transfert **LU sans modification** | back | ✅ |
| 3 | **Cas limites / repli fidèle (erreur + fenêtre vide)** : un jour sans responsable résolu = **« personne assignée »** (neutre) ; acteur **orphelin** → **repli neutre SANS nom fantôme** (filtre `Resolvable()` s13) ; **aucun événement à venir** dans la fenêtre = **liste vide / message neutre** (aucune racine fantôme, pas de crash) ; **store vide** neutre ; identique sur les deux adaptateurs | back | ✅ |
| 4 | **Panneau « À venir » sous la carte du jour** : rend une **liste ordonnée** des prochains jours (demain / reste de la semaine chargée) avec **qui / où / transfert** par jour, en **réutilisant couleurs/repli de la grille** (aucune teinte réinventée) ; transfert = rendu bicolore réutilisé (présentation s29) ; **STRICTEMENT lecture** (aucun contrôle d'édition) ; **Invité VOIT** | 🖥️ IHM | ✅ |
| 5 | **Reprojection client + convergence SignalR (0 GET)** : la liste se **reprojette** depuis la fenêtre de grille déjà chargée ; une écriture pertinente (période/transfert/slot) fait **CONVERGER** la liste d'un 2ᵉ écran **sans rechargement** et **sans GET dédié** (reprojection client, garde anti-amplification flake s42) ; **même limitation assumée** que s42 (au-delà de la fenêtre chargée, non affiché) | 🖥️ IHM | ⏳ |

> **⚠️ Point de vigilance — LONGUEUR de la fenêtre « à venir » (décision SM, Sc.1/4).** La liste couvre
> les **jours à venir DE LA FENÊTRE DE GRILLE DÉJÀ CHARGÉE** (typiquement les jours restants de la
> semaine chargée après aujourd'hui) — **cohérent avec la fenêtre de grille**, **aucun GET dédié**,
> aucune extension au-delà (même limitation que s42, assumée). Ne pas inventer une plage arbitraire ni
> charger une fenêtre supplémentaire : la source unique reste la **grille déjà en main**.

> **⚠️ Point de vigilance — COMPOSER, ne PAS réimplémenter la résolution (Sc.1-3, décision SM).** Le
> « qui / où / transfert » de chaque jour est **déjà** résolu ailleurs (résolution **surcharge > fond >
> neutre** du palier 6, transferts saisis/dérivés s31, projection slots s29 via `GrilleAgendaQuery`).
> `AVenirQuery` est un **agrégateur de lecture par jour** : elle **compose** la même mécanique que
> `CarteDuJourQuery` s42, itérée sur les jours à venir de la fenêtre — **INTERDIT** de recopier la
> priorité surcharge>fond ou la dérivation de transfert (source unique = le domaine existant, sous peine
> de deux vérités divergentes).

> **⚠️ Repli neutre — JAMAIS de nom d'acteur orphelin (Sc.3).** Un jour dont le responsable résolu
> pointe un id stable **absent** du store (acteur supprimé) retombe sur le **repli neutre** (« personne
> assignée ») **sans jamais** afficher un nom/couleur fantôme — filtre `Resolvable()` s13, miroir des
> replis R5/R6 (grille, carte du jour s42). Une liste « à venir » affichant un acteur supprimé serait un
> **vert-qui-ment**.

> **⚠️ Anti-vert-qui-ment — preuve runtime sur profil de jours RÉALISTE (Sc.1-5).** La liste doit être
> prouvée sur **plusieurs jours à venir distincts** : un jour résolu par **surcharge**, un jour résolu
> par le **cycle de fond**, un jour **avec transfert** (saisi et/ou dérivé s31), un jour **neutre**
> (aucun responsable), et une **fenêtre sans aucun à-venir**. Un test qui ne verrait qu'un seul profil
> surestimerait la couverture. Preuve finale = **round-trip runtime réel (Mongo durable)** + **gate
> navigateur PO**.

> **⚠️ Séparation des canaux — liste = LECTURE seule (Sc.4-5).** La liste n'émet **aucune** commande et
> n'expose **aucun** contrôle d'écriture ; sa convergence temps réel passe **exclusivement** par la
> **diffusion SignalR de lecture** (s20) **par reprojection client** (0 GET sur push) — **jamais** une
> écriture par la diffusion (invariant structurel CLAUDE.md). Écriture = canal requête/réponse (dialogs
> de pose / Config), **hors** de cette liste.

---

## Scénarios

### Sc.1 — Query PURE : liste ordonnée des « à venir » (responsable résolu par jour) @back @vert
```gherkin
Étant donné un foyer configuré (acteurs s30, cycle de fond, périodes de garde) et un enfant sélectionné
Et une fenêtre de grille déjà chargée couvrant aujourd'hui et des jours suivants, dont les responsables sont résolus par la résolution existante (surcharge > fond > neutre, palier 6)
Quand j'interroge la query « à venir » pour cette fenêtre + cet enfant
Alors elle restitue une LISTE ORDONNÉE (par date croissante) des JOURS À VENIR strictement après aujourd'hui et compris dans la fenêtre chargée
Et chaque entrée porte le RESPONSABLE RÉSOLU (id stable) avec son nom et sa couleur, tels que la grille les résout
Et le « qui » de chaque jour provient de la COMPOSITION de la résolution existante — aucune logique de priorité surcharge>fond réimplémentée (miroir CarteDuJourQuery s42)
Et la query est PURE : aucune mutation, aucun store neuf, aucune persistance neuve
Et le résultat est IDENTIQUE sur les DEUX adaptateurs (InMemory seedé ET Mongo durable, même contrat)
```

### Sc.2 — Composer le « où » (slots) + le transfert de chaque jour à venir @back @vert
```gherkin
Étant donné la query « à venir » (Sc.1) pour une fenêtre + l'enfant sélectionné
Et certains jours à venir portent un ou plusieurs SLOTS de localisation (s29) et/ou un TRANSFERT de responsabilité
Quand j'interroge la query
Alors chaque entrée de la liste restitue AUSSI le(s) slot(s) de localisation du jour (le « où » de la garde, s29)
Et chaque entrée restitue le TRANSFERT éventuel cédant → recevant (noms + couleurs résolus), qu'il soit SAISI ou DÉRIVÉ (s31), priorité SAISI > DÉRIVÉ (aucun doublon)
Et un jour SANS transfert est restitué unicolore (aucun cédant/recevant, présentation s29 inchangée)
Et un jour SANS slot est restitué SANS lieu (le « où » est simplement absent, pas d'erreur)
Et le transfert et les slots sont LUS sans être modifiés (composition de la dérivation s31 / projection s29 existantes, non réimplémentées)
```

### Sc.3 — Cas limites / repli fidèle : neutre, orphelin, fenêtre sans à-venir @back @vert
```gherkin
Étant donné la query « à venir » (Sc.1)
Quand un jour à venir n'a AUCUN responsable résolu (ni surcharge, ni fond)
Alors son entrée est restituée dans un état NEUTRE explicite « personne assignée » (aucun nom, aucune couleur fantôme)

Étant donné un jour à venir dont le responsable résolu pointe un acteur ORPHELIN (id stable absent du store, supprimé)
Quand j'interroge la query
Alors ce responsable retombe sur le REPLI NEUTRE sans nom ni couleur fantôme (filtre Resolvable s13, miroir R5/R6)

Étant donné une fenêtre chargée qui ne contient AUCUN jour strictement après aujourd'hui (aujourd'hui en fin de fenêtre) ou un store VIDE
Quand j'interroge la query
Alors elle restitue une LISTE VIDE (état « aucun événement à venir »), sans crash ni racine fantôme, à l'identique sur les DEUX adaptateurs
```

### Sc.4 — Panneau « À venir » sous la carte du jour (rendu lecture seule) @ihm @vert
```gherkin
Étant donné le planning ouvert avec la carte « Aujourd'hui » s42 en tête, un enfant sélectionné, et une fenêtre de grille chargée
Quand la page de planning s'affiche
Alors un PANNEAU « À venir » est rendu SOUS la carte « Aujourd'hui », listant les prochains jours (demain / reste de la semaine chargée) par date croissante
Et chaque ligne affiche QUI récupère l'enfant ce jour-là / OÙ (slots) / le TRANSFERT éventuel
Et le nom et la couleur du responsable RÉUTILISENT la résolution couleurs/repli de la grille (aucune teinte réinventée)
Et un transfert d'un jour est matérialisé par le rendu BICOLORE réutilisé (présentation s29), un jour sans transfert reste unicolore
Et un jour neutre affiche « personne assignée » (repli Sc.3) sans nom fantôme ; une fenêtre sans à-venir affiche un message neutre « aucun événement à venir »
Et le panneau est STRICTEMENT en LECTURE : aucun contrôle d'édition, aucune commande émise ; l'Invité VOIT le panneau (lecture non gatée)
```

### Sc.5 — Reprojection client + convergence SignalR (0 GET) @ihm @pending
```gherkin
Étant donné le panneau « À venir » reprojeté depuis la fenêtre de grille DÉJÀ chargée (aucun GET dédié à l'affichage)
Et deux écrans planning ouverts sur le même enfant et la même fenêtre
Quand une écriture pertinente survient sur le 1ᵉʳ écran (période/transfert saisi, slot posé/supprimé) via le canal d'écriture
Alors le panneau « À venir » du 2ᵉ écran CONVERGE (qui/où/transfert des jours recalculés) SANS rechargement
Et la convergence passe par une REPROJECTION CLIENT depuis la grille rafraîchie — AUCUN GET dédié sur push (garde anti-amplification flake s42)
Et la convergence passe EXCLUSIVEMENT par le canal SignalR de LECTURE SEULE (aucune écriture par la diffusion, s20 préservé)
Et un à-venir situé AU-DELÀ de la fenêtre de grille chargée n'est PAS affiché (même limitation assumée que s42, routée au backlog)
```

---

# Retours produit (PO)
