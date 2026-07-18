# Sprint 50 — Cloche « immédiat » : digest « qui récupère ce soir » + « transferts à venir »

> **Goal (G2 tranché PO)** : compléter le **palier 2 (cloche « immédiat »)** — ramener **DANS la
> cloche s47** le contenu utile retiré en s44 : **(a) « qui récupère aujourd'hui / ce soir »** +
> **(b) les « transferts à venir »** des N prochains jours de la fenêtre chargée. **Query PURE de
> composition** réemployant `GrilleAgendaQuery` (miroir des ex-`CarteDuJourQuery` s42 /
> `AVenirQuery` s43 retirés s44), **AUCUN store neuf, AUCUNE mutation**, résolution
> surcharge > fond > neutre **déjà livrée**. Reprojection client depuis la fenêtre grille chargée +
> convergence temps réel par la **diffusion porteuse de payload** `INotificateurChangement` (0 GET
> sur push). **Lecture stricte, aucune action.**

## Porte de conception SURFACE — TRANCHÉE AU CADRAGE (pas au gate G3)

- **Le digest est une SECTION neuve DANS le panneau déroulant de la cloche s47** (barre du haut,
  `MainLayout`) — **posée EN TÊTE du panneau, AU-DESSUS du flux chrono de notifications** (lu/non-lu).
  Le digest est **permanent et de LECTURE** (état courant « immédiat + à-venir »), distinct du flux
  d'événements horodatés en dessous.
- **Anti-cliquet s44 tenu** : la **GRILLE agenda reste la SEULE surface de lecture SUR la page
  `/planning`**. Le digest vit **UNIQUEMENT dans la cloche** (surface hors-grille **assumée** depuis
  s47) — **aucune carte « Aujourd'hui » (s42) ni panneau « À venir » (s43) réintroduits** sur la page.
- **Alternatives écartées** : (i) re-poser une carte/panneau sur `/planning` → **viole s44** ; (ii)
  un onglet/écran dédié → surface neuve non voulue. **Retenu = section en tête du panneau cloche.**
- **À valider PO au gate G3** (rendu visuel de la section), **pas la décision de surface** (actée ici).

## Limitation assumée (routée backlog)

- Le digest **se reprojette depuis la fenêtre de grille chargée** : si l'utilisateur **navigue vers une
  semaine ne contenant pas le jour courant**, la section « aujourd'hui » **disparaît** et les « à-venir »
  se bornent à la fenêtre chargée (**héritage s42/s43**, choix guidé par l'anti-amplification flake :
  **aucun GET dédié sur push**). L'arbitrage **persistance hors-fenêtre vs coût GET/flake** n'est **pas
  rouvert** ce sprint → backlog.

---

## Avancement — 1/8

| # | Scénario | Type | Statut |
|---|----------|------|--------|
| 1 | Digest « qui récupère aujourd'hui / ce soir » composé depuis `GrilleAgendaQuery` (responsable résolu + où/slot + transfert) | @back | ✅ |
| 2 | « Transferts à venir » des N prochains jours de la fenêtre chargée, chrono croissant | @back | ⏳ |
| 3 | Replis fidèles (personne assignée sans fantôme · orphelin neutre · sans transfert · sans slot) | @back | ⏳ |
| 4 | Fenêtre sans à-venir / jour courant hors-fenêtre = section vide neutre, sans crash ; invariant 0 mutation (2 adaptateurs) | @back | ⏳ |
| 5 | Section digest en TÊTE du panneau cloche (au-dessus du flux chrono), lecture stricte, Parent-gated | @ihm | ⏳ |
| 6 | Reprojection client depuis la fenêtre grille chargée (0 GET dédié) ; hors-fenêtre = digest vide neutre | @ihm | ⏳ |
| 7 | Convergence temps réel du digest d'un 2ᵉ écran par la diffusion `INotificateurChangement`, 0 GET sur push | @ihm | ⏳ |
| 8 | Gating & lecture stricte : Invité ne voit pas le digest · rien sur `/connexion` · aucune action/bouton | @ihm | ⏳ |

> **Note gate G3 (leçon s49)** : les scénarios @ihm de cloche = rendu navigateur + temps réel SignalR.
> **Rebâtir le build SERVI** (conteneur `web`, jamais `--no-build` d'un artefact périmé) **avant de
> solliciter le PO**. bUnit couvre le rendu de section + gating ; si un point sort du périmètre bUnit
> (convergence SignalR réellement servie), envisager un **smoke navigateur** (projet E2E Playwright s49,
> hors `.slnx`).

---

## Scénarios

### Sc.1 — Digest « qui récupère aujourd'hui / ce soir » (composition pure)
```gherkin
@back @vert
Scénario: le digest « immédiat » restitue le responsable du jour courant, résolu et localisé
  Étant donné un foyer configuré (cycle de fond + acteurs persistés) et un enfant sélectionné
  Et le jour courant portant un responsable résolu par la priorité surcharge > fond > neutre
  Et, ce jour-là, un slot de localisation (s29) et un transfert saisi OU auto-dérivé (s31)
  Quand je compose le digest « immédiat » pour cet enfant et ce jour
  Alors le digest expose QUI récupère (identifiant stable, jamais le libellé), OÙ (slot s29)
  Et le transfert éventuel (cédant → recevant, saisi OU dérivé s31)
  Et la composition RÉEMPLOIE GrilleAgendaQuery — sans réimplémenter la résolution/dérivation
  Et AUCUN store neuf n'est introduit, AUCUNE mutation n'est émise
  Et le comportement est identique sur les deux adaptateurs (InMemory ET Mongo durable)
```

### Sc.2 — « Transferts à venir » sur la fenêtre chargée
```gherkin
@back @pending
Scénario: le digest liste les transferts des N prochains jours de la fenêtre de grille chargée
  Étant donné une fenêtre de grille chargée couvrant les N prochains jours
  Et plusieurs jours à venir portant chacun un transfert (saisi OU auto-dérivé s31)
  Quand je compose la section « à venir » du digest pour l'enfant sélectionné
  Alors elle restitue, en ordre chronologique CROISSANT, pour chaque jour concerné :
    le jour, qui récupère (résolu surcharge > fond > neutre), le transfert et le lieu éventuel
  Et cette section est un MIROIR STRICT de la logique s43 (AVenirQuery) itérée sur les jours à venir
  Et elle ne compose que la fenêtre chargée — aucun GET dédié, aucune mutation, aucun store neuf
  Et le résultat est identique InMemory ET Mongo durable
```

### Sc.3 — Replis fidèles
```gherkin
@back @pending
Scénario: chaque cas de repli est rendu fidèlement, sans fantôme
  Étant donné une fenêtre chargée mêlant des jours aux configurations variées
  Quand je compose le digest « immédiat » et « à venir »
  Alors un jour SANS responsable résolu affiche « personne assignée » (aucun nom fantôme)
  Et un responsable ORPHELIN (id absent du référentiel) retombe en NEUTRE sans nom (R6 / Resolvable s13)
  Et un jour SANS transfert n'apparaît PAS dans la liste des transferts (ni ligne vide)
  Et un jour SANS slot est restitué SANS lieu (aucun lieu fantôme)
  Et aucun de ces replis ne déclenche d'écriture ni de mutation d'état
```

### Sc.4 — Fenêtre vide / hors-fenêtre + invariant zéro-mutation
```gherkin
@back @pending
Scénario: fenêtre sans à-venir et jour courant hors-fenêtre = digest vide neutre, store intact
  Étant donné une fenêtre chargée ne contenant NI jour courant NI aucun transfert à venir
  Quand je compose le digest « immédiat » et « à venir »
  Alors la section « immédiat » est vide neutre (message neutre, pas de crash)
  Et la section « à venir » est une liste vide (message neutre)
  Et AUCUNE surcharge n'est écrite — le store des surcharges reste STRICTEMENT intact
  Et la résolution d'aucune case n'est altérée (la query est de LECTURE pure)
  Et le comportement est prouvé identique sur les deux adaptateurs (InMemory ET Mongo durable)
```

### Sc.5 — Section digest en tête du panneau cloche (IHM lecture)
```gherkin
@ihm @pending
Scénario: le digest s'affiche EN TÊTE du panneau déroulant de la cloche, en lecture stricte
  Étant donné un utilisateur Parent connecté avec un enfant sélectionné et une grille chargée
  Quand j'ouvre le panneau déroulant de la cloche (barre du haut, MainLayout)
  Alors une SECTION « immédiat » apparaît EN TÊTE, AU-DESSUS du flux chrono de notifications s47
  Et elle affiche « qui récupère aujourd'hui / ce soir » (qui, où, transfert éventuel)
  Et, en dessous, la sous-section « à venir » liste les transferts des prochains jours chargés
  Et le flux chrono lu/non-lu s47 reste rendu EN DESSOUS, inchangé
  Et le digest est STRICTEMENT en LECTURE : aucun bouton, aucune action, aucune entrée cliquable
```

### Sc.6 — Reprojection client depuis la fenêtre grille (0 GET dédié)
```gherkin
@ihm @pending
Scénario: le digest se reprojette depuis la fenêtre grille chargée, sans GET dédié
  Étant donné le planning chargé par l'unique GET grille pour une fenêtre donnée
  Quand le panneau cloche affiche la section digest
  Alors le digest est REPROJETÉ côté client depuis la donnée déjà chargée par le GET grille
  Et aucun GET dédié « digest » n'est émis
  Et si je navigue vers une semaine NE contenant PAS le jour courant
  Alors la section « immédiat » disparaît et « à venir » se borne à la fenêtre chargée
  Et cette limitation est le comportement attendu (aucun GET sur navigation pour la combler)
```

### Sc.7 — Convergence temps réel (diffusion porteuse de payload, 0 GET sur push)
```gherkin
@ihm @pending
Scénario: un changement diffusé fait converger le digest d'un 2ᵉ écran, 0 GET sur push
  Étant donné deux écrans Parent connectés sur la même fenêtre de grille chargée
  Quand un changement (délégation / plage / reprise / transfert) est écrit depuis le 1ᵉʳ écran
  Et qu'il est diffusé par le port INotificateurChangement (EvenementChangementSnapshot, s47)
  Alors le digest du 2ᵉ écran CONVERGE par REPROJECTION CLIENT depuis la diffusion
  Et AUCUN GET n'est émis sur push (garde-fou anti-flake flake-signalr-blast-radius respecté)
  Et la diffusion porte une DONNÉE DE LECTURE — elle ne déclenche aucune écriture (canaux séparés)
```

### Sc.8 — Gating & lecture stricte
```gherkin
@ihm @pending
Scénario: le digest est Parent-gated et purement en lecture
  Étant donné un utilisateur Invité connecté
  Quand j'ouvre le panneau de la cloche
  Alors la section digest n'est PAS visible (Parent-gated, aligné sur la cloche s47)
  Et sur la page /connexion aucune cloche ni digest n'est rendu
  Étant donné un utilisateur Parent connecté
  Quand le digest est affiché
  Alors il ne porte AUCUNE action de suivi, aucun bouton, aucune entrée cliquable (lecture stricte)
```

---

# Retours produit (PO)

<!-- Rempli après le gate visuel G3. -->
