# Sprint 29 — Slots récurrents + transfert bicolore sur la grille

> **Goal (G2 tranché PO)** : goal **combiné**, deux volets.
> 1. **Configuration de slot RÉCURRENT** (valeur produit prioritaire). Exemple PO : *« Piscine tous
>    les samedis chez papa de 11h30 à 12h15 »*. Déclarer un slot qui **se répète** (récurrence
>    **hebdomadaire simple** : un **jour de semaine** + une **plage horaire début→fin** + un **lieu**)
>    et le voir **matérialisé sur toutes les occurrences** du planning. Récurrence **posée en cohérence
>    DDD/hexa** avec le slot existant (`SlotDeLocalisation`, `ISlotRepository`, `PoserSlot`).
> 2. **Transfert bicolore sur la grille** (retour s17 #7). Rendre un **transfert déjà saisi** visible
>    sur la **case du jour** : case **bicolore**, **diagonale** séparant couleur de **départ** (acteur
>    cédant) et couleur d'**arrivée** (acteur recevant), couleurs résolues depuis le **référentiel
>    acteurs**, lisibilité conforme (R18-R22), **légende** signalant le motif bicolore. Jour **sans**
>    transfert = **unicolore inchangé** (non-régression).

## Périmètre — DANS / HORS scope

- **DANS** : volet 1 complet (agrégat + port + commande/handler de pose récurrente hebdo, validation
  lieu/plage miroir `PoserSlot`, **persistance durable Mongo** + InMemory, **projection des occurrences**
  dans `GrilleAgendaQuery`, suppression idempotente par id stable, **IHM de configuration**) ; volet 2
  (transfert du jour **lu dans le modèle de grille**, info bicolore + légende, **rendu diagonal IHM**).
- **HORS scope (backlog)** : récurrences calendaires **riches** (bi-hebdo, mensuelle, fins de série,
  exceptions/occurrences supprimées, sur-cycle vacances) — **hebdo simple uniquement** ; **édition**
  d'un slot récurrent existant (pose + suppression seulement) ; transfert **bicolore** = présentation,
  **aucun** changement du modèle de transfert ni de la résolution de responsabilité (surcharge > fond >
  neutre inchangée).

> **Décision SM — le slot reste une LOCALISATION (sans responsable).** L'agrégat `SlotDeLocalisation`
> est **orthogonal à la responsabilité** (invariant spec `saisie-et-grille` / `ecriture-en-contexte`).
> Le slot récurrent suit ce modèle : **enfant + lieu + plage horaire hebdo**, **pas** de responsable
> embarqué. Dans l'exemple PO, *« chez papa »* est le **lieu**. La responsabilité de la case continue
> de se résoudre par **période / cycle de fond** — elle n'est **pas** portée par le slot. (Si le PO
> veut réellement un responsable dans le slot récurrent, c'est une **révision de règle hors boucle**,
> à trancher au gate — ne pas casser l'invariant en sprint.)

> **Preuve = runtime réel** (Docker/Mongo actifs, suite complète sans filtre). Volet 1 persistance
> prouvée sur **store Mongo durable** (parité slot ponctuel s15). **Aucune** entorse de preuve par
> doublure ici → statuts `⏳`/`🔴`/`✅` **francs**, pas de dette de câblage.

## Avancement — 5/14 (back 11 · IHM 3)

| # | Scénario | Type | Statut |
|---|----------|------|:------:|
| S1 | Poser un slot récurrent hebdo valide → succès + snapshot (jour, plage, lieu, enfant, id stable) | @back | ✅ |
| S2 | Rejet : lieu inconnu du foyer → échec **sans écriture** (miroir `PoserSlot`) | @back | ✅ |
| S3 | Rejet : plage horaire non positive (fin ≤ début) → échec **sans écriture** | @back | ✅ |
| S4 | Projection grille : le slot récurrent apparaît sur **CHAQUE occurrence** du bon jour dans la fenêtre | @back | ✅ |
| S5 | Limite : le slot récurrent **n'apparaît sur aucun autre jour de semaine** | @back | ✅ |
| S6 | Persistance durable Mongo : un slot récurrent **survit au redémarrage** (parité slot ponctuel s15) | @back | ⏳ |
| S7 | Suppression **idempotente** par id stable (no-op si absent) + **diffusion temps réel** | @back | ⏳ |
| S8 | IHM : configurer un slot récurrent → occurrences visibles sur toutes les cases du bon jour (RED→GREEN) | 🖥️ IHM | ⏳ |
| S9 | Projection grille : un jour **avec transfert** porte l'info **bicolore** (couleurs départ/arrivée résolues sur acteurs) | @back | ⏳ |
| S10 | Limite : un jour **sans transfert** reste **unicolore**, aucune info bicolore (non-régression) | @back | ⏳ |
| S11 | Légende : le motif **bicolore = transfert** est signalé quand un transfert est présent dans la fenêtre | @back | ⏳ |
| S12 | Erreur : transfert dont un acteur a été **supprimé** → couleur **neutre** pour l'orphelin (pas de couleur fantôme) | @back | ⏳ |
| S13 | IHM : rendu **diagonal bicolore** de la case (départ/arrivée), lisibilité conforme (nom + légende) — **gate G3** | 🖥️ IHM | ⏳ |
| S14 | IHM : jour **sans transfert** = case **unicolore inchangée** (non-régression visuelle) | 🖥️ IHM | ⏳ |

## Scénarios

### Volet 1 — Slot récurrent hebdomadaire (back : frontière Application)

```gherkin
@back @vert
Scénario: S1 — Poser un slot récurrent hebdomadaire valide
  Étant donné un foyer dont le référentiel de lieux contient "Piscine"
  Et un enfant déclaré du foyer
  Quand un Parent pose un slot récurrent le samedi de 11h30 à 12h15 au lieu "Piscine"
  Alors la commande réussit
  Et le slot récurrent est enregistré avec un identifiant stable neuf (jamais un libellé)
  Et son snapshot porte : jour de semaine = samedi, heure début = 11h30, heure fin = 12h15, lieu, enfant
  Et la diffusion temps réel de mise à jour est déclenchée

@back @vert
Scénario: S2 — Rejet d'un slot récurrent sur un lieu inconnu du foyer
  Étant donné un foyer dont le référentiel de lieux NE contient PAS "Dojo"
  Quand un Parent pose un slot récurrent le mercredi au lieu "Dojo"
  Alors la commande échoue avec un motif clair (lieu inexistant)
  Et aucun slot récurrent n'est enregistré
  Et aucune diffusion n'est déclenchée

@back @vert
Scénario: S3 — Rejet d'une plage horaire non positive
  Étant donné un foyer dont le référentiel de lieux contient "Piscine"
  Quand un Parent pose un slot récurrent le samedi de 12h15 à 11h30 au lieu "Piscine"
  Alors la commande échoue (la durée doit être strictement positive)
  Et aucun slot récurrent n'est enregistré

@back @vert
Scénario: S4 — Le slot récurrent se matérialise sur toutes les occurrences du bon jour
  Étant donné un slot récurrent enregistré le samedi de 11h30 à 12h15 au lieu "Piscine"
  Quand on projette la grille agenda sur une fenêtre de 4 semaines
  Alors chaque case de samedi de la fenêtre porte une entrée de slot "Piscine" 11h30–12h15
  Et les bornes horaires sont identiques sur toutes les occurrences
  Et l'entrée s'empile dans l'ordre horaire avec les slots ponctuels du même jour

@back @vert
Scénario: S5 — Le slot récurrent n'apparaît sur aucun autre jour de semaine
  Étant donné un slot récurrent enregistré le samedi de 11h30 à 12h15 au lieu "Piscine"
  Quand on projette la grille agenda sur une fenêtre de 4 semaines
  Alors aucune case d'un jour autre que samedi ne porte l'entrée "Piscine" 11h30–12h15

@back @pending
Scénario: S6 — Un slot récurrent persiste sur le store durable Mongo
  Étant donné le store Mongo durable actif
  Et un slot récurrent enregistré le samedi de 11h30 à 12h15 au lieu "Piscine"
  Quand le référentiel des slots récurrents est relu après redémarrage (nouvelle instance de dépôt)
  Alors le slot récurrent est toujours présent avec son identifiant stable et son snapshot intacts
  # Parité avec l'asymétrie seed s15 : en mode Mongo, aucun slot récurrent seedé au 1er lancement

@back @pending
Scénario: S7 — Suppression idempotente d'un slot récurrent par identifiant stable
  Étant donné un slot récurrent enregistré d'identifiant stable connu
  Quand un Parent supprime ce slot récurrent par son identifiant stable
  Alors le slot récurrent est retiré du store durable
  Et la diffusion temps réel de mise à jour est déclenchée
  Et ses occurrences disparaissent de toutes les cases à la re-projection
  Quand la même suppression est rejouée avec le même identifiant (déjà absent)
  Alors la commande réussit en no-op (idempotence), sans erreur
```

### Volet 1 — IHM de configuration (menée RED→GREEN runtime, fin de sprint)

```gherkin
@ihm @pending
Scénario: S8 — Configurer un slot récurrent depuis l'IHM
  Étant donné un Parent connecté et un foyer avec le lieu "Piscine" et un enfant déclaré
  Quand il configure un slot récurrent : jour = samedi, de 11h30 à 12h15, lieu "Piscine"
  Et valide
  Alors la grille affiche le slot "Piscine" 11h30–12h15 sur chaque samedi visible
  Et un lieu inconnu ou une plage invalide laisse un message d'erreur sans rien enregistrer
```

### Volet 2 — Transfert bicolore sur la grille (back : modèle de lecture)

```gherkin
@back @pending
Scénario: S9 — Un jour avec transfert porte l'information bicolore
  Étant donné deux acteurs "Papa" et "Maman" du référentiel, chacun avec sa couleur
  Et un transfert saisi le jour J : déposé par "Papa", récupéré par "Maman", à un lieu et une heure
  Quand on projette la grille agenda sur une fenêtre couvrant J
  Alors la case du jour J porte une information bicolore
  Et la couleur de départ = la couleur de "Papa" (déposant), résolue sur son identifiant stable
  Et la couleur d'arrivée = la couleur de "Maman" (récupérant), résolue sur son identifiant stable
  Et la résolution de responsabilité de la case (surcharge > fond > neutre) est inchangée

@back @pending
Scénario: S10 — Un jour sans transfert reste unicolore (non-régression)
  Étant donné aucun transfert saisi le jour K
  Quand on projette la grille agenda sur une fenêtre couvrant K
  Alors la case du jour K ne porte aucune information bicolore
  Et son rendu de couleur unique (responsable résolu) est inchangé

@back @pending
Scénario: S11 — La légende signale le motif bicolore = transfert
  Étant donné un transfert saisi dans la fenêtre projetée
  Quand on projette la grille agenda
  Alors la légende porte une entrée signalant le motif bicolore comme un transfert
  Et cette entrée est absente quand aucun transfert ne couvre la fenêtre

@back @pending
Scénario: S12 — Acteur d'un transfert supprimé : neutre, pas de couleur fantôme
  Étant donné un transfert le jour J dont le récupérant a depuis été supprimé du foyer
  Quand on projette la grille agenda sur une fenêtre couvrant J
  Alors la couleur d'arrivée retombe sur la couleur neutre (orphelin neutralisé, cf. Resolvable)
  Et aucune couleur ni nom fantôme n'est produit pour l'acteur absent
```

### Volet 2 — Rendu IHM bicolore (menée RED→GREEN runtime, fin de sprint, gate G3)

```gherkin
@ihm @pending
Scénario: S13 — Rendu diagonal bicolore d'une case de transfert
  Étant donné une case portant l'information bicolore (départ "Papa" / arrivée "Maman")
  Quand la grille est rendue
  Alors la case est coupée par une diagonale séparant la couleur de départ de la couleur d'arrivée
  Et le nom du responsable et la légende restent lisibles (R18–R22)
  Et la légende indique le motif bicolore = transfert
  # Validation visuelle au gate G3

@ihm @pending
Scénario: S14 — Une case sans transfert reste unicolore (non-régression visuelle)
  Étant donné une case d'un jour sans transfert
  Quand la grille est rendue
  Alors la case est unicolore, identique au rendu antérieur (aucune diagonale)
```

# Retours produit (PO)

<!-- Rempli après le gate G3, consommé à la /cloture. -->
