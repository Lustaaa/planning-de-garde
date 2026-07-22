# Sprint 46 — Annuler / reprendre une délégation d'un jour

> **Goal (G2 tranché PO)** : fermer la boucle *undo* laissée explicitement ouverte en s44 **et**
> s45 (« défaire = re-déléguer ou supprimer via s16, aucun undo dédié »). Un parent qui a délégué
> la récupération d'un jour peut **reprendre ce jour** : la case retombe sur le **fond (cycle)** et
> le **transfert bicolore dérivé s31 disparaît**. Usage réel : « finalement je peux récupérer ».

## Avancement — 6/6

| # | Scénario | Type | Statut |
|---|----------|------|--------|
| Sc.1 | Reprendre un jour délégué (ponctuel s44) → retour au fond, transfert dérivé disparu (2 adaptateurs) | @back | ✅ |
| Sc.2 | Jour sans délégation active → **no-op idempotent**, store intact (ré-annulation idempotente) | @back | ✅ |
| Sc.3 | Reprendre **UNE occurrence** (jour cliqué) d'une **plage s45** sans casser le reste de la plage | @back | ✅ |
| Sc.4 | Entrée conditionnelle « reprendre ce jour » présente si délégation active → annule, case au fond (runtime) | @ihm | ✅ |
| Sc.5 | Entrée **absente** si pas de délégation active · Invité ne voit ni menu ni entrée · Échap = Annuler | @ihm | ✅ |
| Sc.6 | Convergence temps réel de la case sur 2ᵉ écran par reprojection client SignalR, **0 GET** | @ihm | ✅ |

## Portes de conception (arbitrées AU CADRAGE — garde s44)

- **AUCUNE surface neuve.** L'action « reprendre ce jour » est une **entrée ADDITIONNELLE du menu
  clic-case EXISTANT** (à côté de « déléguer ce jour » s44 / « Affecter une période » / « Définir un
  transfert »), affichée **CONDITIONNELLEMENT** : visible **uniquement** quand la case cliquée porte
  une **délégation active** (surcharge de délégation résolue), **absente** sinon. Emplacement retenu =
  menu clic-case (cohérent s44). **Alternatives écartées** : bouton undo / toast overlay (surface
  flottante hors grille, contraire à « grille = seule surface » décidé PO s44) ; undo global.
- **Granularité retenue = UNE OCCURRENCE.** « Reprendre ce jour » annule **le seul jour cliqué**,
  **même s'il appartient à une plage `[J1..J2]` déléguée (s45)** — **PAS** toute la série/plage d'un
  coup. Les autres jours de la plage **restent délégués** ; les **transferts dérivés s31** aux
  frontières sont **recalculés** en conséquence (le trou créé produit ses propres bascules). Reprendre
  toute une plage = répéter l'action jour par jour (pas d'action « plage » dédiée ce sprint).
- **Composition, pas de neuf.** `AnnulerDelegation(jour[, enfant])` **COMPOSE la suppression de
  surcharge EXISTANTE (s16)** : **aucun modèle / commande / store neuf**, **aucune dérivation de
  transfert neuve** (le transfert s31 se re-dérive de la résolution après suppression). Deux
  adaptateurs **InMemory + Mongo durable**, écriture prouvée **store réel**.

---

## Scénarios

### @back — noyau, s'arrête à la frontière Application

```gherkin
@back @vert
Scénario: Sc.1 — Reprendre un jour délégué (ponctuel s44) → la case retombe sur le fond
  Étant donné un foyer dont le cycle de fond résout "Alice" responsable le jour J pour l'enfant E
  Et une délégation ponctuelle (s44) posée le jour J confiant la récupération à "Bruno"
  Et donc, ce jour-là, "Bruno" résolu responsable (surcharge > fond) avec un transfert dérivé s31 Alice→Bruno
  Quand j'exécute AnnulerDelegation(jour J, enfant E)
  Alors la surcharge de délégation du jour J est supprimée via le chemin EXISTANT s16 (aucun store neuf)
  Et la résolution du jour J retombe sur le FOND : "Alice" est de nouveau responsable
  Et le transfert bicolore dérivé s31 du jour J DISPARAÎT (plus de bascule Alice→Bruno)
  Et le résultat est identique sur les DEUX adaptateurs (InMemory ET Mongo durable, prouvé store réel)
```

```gherkin
@back @vert
Scénario: Sc.2 — Jour sans délégation active → no-op idempotent, store intact
  Étant donné un jour J dont la résolution ne porte AUCUNE surcharge de délégation (fond seul)
  Quand j'exécute AnnulerDelegation(jour J, enfant E)
  Alors la commande est un SUCCÈS no-op (rien à reprendre)
  Et le store reste INTACT (aucune écriture, aucune suppression collatérale)
  Et ré-exécuter AnnulerDelegation(jour J, enfant E) une seconde fois est de nouveau un no-op idempotent
  Et sur écriture concurrente le dernier écrit gagne (R11) sans doublon ni jour tiers touché
```

```gherkin
@back @vert
Scénario: Sc.3 — Reprendre UNE occurrence d'une plage s45 sans casser le reste de la plage
  Étant donné une délégation de PLAGE (s45) confiant à "Bruno" la récupération de l'enfant E du jour J1 au jour J3 inclus
  Et donc "Bruno" résolu responsable J1, J2 et J3, avec transferts dérivés aux frontières (entrée avant J1, sortie après J3)
  Quand j'exécute AnnulerDelegation(jour J2, enfant E)   # le jour cliqué au MILIEU de la plage
  Alors SEUL le jour J2 retombe sur le FOND (granularité = une occurrence, PAS toute la plage)
  Et J1 et J3 RESTENT délégués à "Bruno"
  Et les transferts dérivés s31 sont RECALCULÉS : le trou en J2 produit ses propres bascules (sortie après J1, entrée avant J3)
  Et aucune écriture partielle n'a touché J1 ni J3 (le reste de la plage est préservé)
  Et le résultat est identique sur InMemory ET Mongo durable (prouvé store réel)
```

### @ihm — menés RED→GREEN runtime

```gherkin
@ihm @vert
Scénario: Sc.4 — Entrée conditionnelle "reprendre ce jour" présente sur une case déléguée
  Étant donné un Parent connecté, sur une case portant une délégation active (Bruno résolu par surcharge)
  Quand j'ouvre le menu clic-case de cette case
  Alors l'entrée "reprendre ce jour" est PRÉSENTE, à côté de "déléguer ce jour" (s44)
  Quand je clique "reprendre ce jour"
  Alors la commande AnnulerDelegation est émise par le CANAL d'écriture (jamais la diffusion)
  Et la case relue retombe sur le FOND en runtime (responsable de fond, transfert dérivé disparu)
```

```gherkin
@ihm @vert
Scénario: Sc.5 — Entrée absente hors délégation · gating Invité · Échap = Annuler
  Étant donné un Parent connecté, sur une case SANS délégation active (fond seul)
  Quand j'ouvre le menu clic-case de cette case
  Alors l'entrée "reprendre ce jour" est ABSENTE (conditionnalité : rien à reprendre)
  Étant donné un Invité (consultation seule)
  Alors il ne voit NI le menu clic-case NI l'entrée "reprendre ce jour" (Parent-gated)
  Étant donné un Parent ayant ouvert la confirmation de reprise
  Quand j'appuie sur Échap
  Alors l'action est annulée (port IEcouteurEchapModal s33), aucune commande émise, store intact
```

```gherkin
@ihm @vert
Scénario: Sc.6 — Convergence temps réel de la case sur un 2ᵉ écran (SignalR, 0 GET)
  Étant donné deux écrans A et B affichant la même semaine, une case du jour J déléguée à "Bruno"
  Quand l'écran A reprend le jour J (AnnulerDelegation)
  Alors l'écran B CONVERGE par reprojection client SignalR : la case J retombe sur le fond (Alice), le transfert dérivé disparaît
  Et AUCUN GET dédié n'est déclenché sur le push (reprojection depuis la fenêtre déjà chargée)
```

---

# Retours produit (PO)

<!-- rempli au gate visuel G3 -->
