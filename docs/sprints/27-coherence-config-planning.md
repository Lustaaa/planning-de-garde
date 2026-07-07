# Sprint 27 — Cohérence config foyer → planning

> **Goal (G2 tranché PO)** : rendre **effectif** pour le planning ce qui est configuré. Le trou
> réel : les **lieux** sont codés en dur (`ILieuRepository.Existe` lit `Foyer.Lieux` static ;
> les sélecteurs des dialogs itèrent `Foyer.Lieux` côté Web) — **aucun écran, aucune
> persistance, aucune propagation**. On les hisse au rang de **référentiel foyer éditable +
> persisté** (miroir strict des acteurs/rôles s20/s21) qui **pilote réellement** la saisie et la
> validation. On ajoute un **filet de non-régression** prouvant que la couleur d'acteur
> configurée reste effective sur la grille et la légende (convergence s20).
>
> **Borne (hors scope, explicite)** : **ne pas rouvrir le cycle de fond riche** (ancre/début,
> frontière de jour, plage, sur-cycle vacances, WE-only — backlog +5). Les lieux ne portent ici
> **qu'un libellé** (pas de slots imbriqués, épic 6). Un slot déjà posé sur un lieu supprimé
> **conserve** son lieu (pas de réécriture rétroactive).
>
> **Preuve = runtime réel** (store Mongo, Docker actif) pour la persistance ; **aucune doublure
> de port** dans ce sprint → pas de dette de câblage, `✅` francs autorisés une fois prouvés.

## Avancement — 6/6

| # | Scénario | Type | Statut |
|---|----------|------|--------|
| S1 | Un lieu ajouté au foyer devient disponible à la saisie | @back | ✅ |
| S2 | Un lieu supprimé quitte le référentiel et la validation de saisie | @back | ✅ |
| S3 | Rejets d'écriture d'un lieu — libellé vide ou en doublon | @back | ✅ |
| S4 | Un lieu configuré survit au redémarrage (Mongo durable) | @back | ✅ |
| S5 | Une couleur recoloriée en config est effective sur grille + légende | @back | ✅ |
| S6 | Le sélecteur de lieu des dialogs reflète la config, sans rechargement | 🖥️ IHM | ✅ |

## Scénarios

```gherkin
@back @vert
Scénario: Un lieu ajouté au foyer devient disponible à la saisie
  Étant donné le référentiel de lieux du foyer
  Quand un parent ajoute le lieu « piscine » (libellé neuf)
  Alors l'énumération des lieux du foyer contient « piscine »
  Et poser un slot au lieu « piscine » est accepté (le lieu existe désormais)
```

```gherkin
@back @vert
Scénario: Un lieu supprimé quitte le référentiel et la validation de saisie
  Étant donné le lieu « nounou » présent au référentiel du foyer
  Quand un parent supprime le lieu « nounou »
  Alors l'énumération des lieux du foyer ne contient plus « nounou »
  Et poser un nouveau slot au lieu « nounou » est refusé (lieu inconnu, aucune écriture)
  # Borne : un slot DÉJÀ posé sur « nounou » conserve son lieu (aucune réécriture rétroactive)
```

```gherkin
@back @vert
Scénario: Rejets d'écriture d'un lieu — libellé vide ou en doublon
  Étant donné le lieu « école » présent au référentiel du foyer
  Quand un parent ajoute un lieu au libellé vide
  Alors l'ajout est rejeté sans écriture (motif clair)
  Quand un parent ajoute un lieu « école » (doublon de libellé)
  Alors l'ajout est rejeté sans écriture (motif clair)
  # Miroir strict des rejets du référentiel acteurs (R5/R6) et rôles (R10)
```

```gherkin
@back @vert
Scénario: Un lieu configuré survit au redémarrage (persistance Mongo durable)
  Étant donné le mode de persistance Mongo (store réel, Docker actif)
  Et un parent qui a ajouté le lieu « piscine »
  Quand le serveur redémarre (instance de config foyer fraîche)
  Alors l'énumération des lieux relit « piscine » depuis le store durable
  # Aucun seed Mongo (parité asymétrie seed s15) ; InMemory conserve son seed pour la non-régression
```

```gherkin
@back @vert
Scénario: Une couleur recoloriée en config est effective sur la grille et la légende
  Étant donné l'acteur « parent-a » colorié « bleu » et une garde qui lui est affectée
  Quand un parent recolorie « parent-a » en « rouge » depuis l'écran de config
  Alors la case de la garde de « parent-a » se résout en « rouge » (la palette relit la dernière écriture)
  Et la légende affiche « rouge » pour « parent-a »
  # Filet de non-régression de la convergence config ↔ grille ↔ légende (s20) : config → planning tenu pour la couleur
```

```gherkin
@ihm @vert
Scénario: Le sélecteur de lieu des dialogs reflète le référentiel configuré, sans rechargement
  Étant donné l'écran de planning avec la dialog « Poser un slot » ouverte
  Quand un parent ajoute le lieu « piscine » depuis l'écran de config
  Alors le sélecteur de lieu de la dialog propose « piscine » (lu depuis le store vivant, plus la liste en dur Foyer.Lieux)
  Quand un parent supprime le lieu « nounou » depuis l'écran de config
  Alors le sélecteur de lieu ne propose plus « nounou »
  # RED→GREEN runtime ; sélecteurs de PoserSlotDialog ET DefinirTransfertDialog convergent (temps réel SignalR lecture)
```

## Notes d'ancrage (état réel du code)

- **Lieux = trou réel** : `ILieuRepository.Existe` lit `Foyer.Lieux` (static, `Infrastructure`) ;
  `PoserSlotDialog.razor` et `DefinirTransfertDialog.razor` itèrent `Foyer.Lieux` (static, `Web`)
  — **deux listes en dur non reliées, non éditables, non persistées**. Cible : un référentiel
  foyer (lecture `IEnumerationLieux` + écriture `IEditeurLieux`, id stable + libellé), réalisé
  par les **mêmes stores** que les acteurs (`ConfigurationFoyerEnMemoire` seedé / `ConfigurationFoyerMongo`
  durable sans seed), consommé par la validation ET les sélecteurs via le canal de lecture.
- **Couleurs = déjà effectives** (convergence s20, `IPaletteCouleurs` sur store vivant) → S5 est un
  **filet de non-régression**, pas un chantier neuf : garde la propagation config→grille/légende.
- **Backend d'abord** (S1→S5, frontière Application), **IHM en fin** (S6, RED→GREEN runtime).

# Retours produit (PO)

<!-- À remplir au gate G3 : retours, bugs, évolutions, nouveaux besoins. Vidé vers docs/BACKLOG.md à la clôture. -->
