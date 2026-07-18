# Sprint 49 — Sélection de plage sur la grille (tranche 2 du palier 9)

> **Goal (G2, tranché PO)** : le hub `/planning` gagne la **sélection d'une plage de cases par
> DRAG** sur la grille agenda pour **affecter une période sur l'intervalle** choisi. On **réemploie
> STRICTEMENT** la dialog « Affecter une période » (écriture-en-contexte s06) : le back multi-jours
> existe déjà (une période EST un intervalle `[début..fin]`, prouvé par s06 et réexercé par les
> plages s45). Ce sprint **n'ajoute AUCUNE mécanique d'écriture, AUCUN store, AUCUNE persistance** :
> la sélection est un **état d'interaction client VOLATILE** (borne anti-cliquet). Enrichit la grille
> **sans toucher aux dialogs déjà livrées**.

## Avancement — 5/8

| # | Scénario | Type | Statut |
|---|----------|------|--------|
| 1 | Filet non-régression : affecter une période sur un intervalle `[J1..J3]` pose la surcharge sur CHAQUE jour (réemploi s06, deux adaptateurs) | back | ✅ |
| 2 | Filet : intervalle d'UN seul jour `[J..J]` = période ponctuelle inchangée, aucune écriture doublonnée | back | ✅ |
| 3 | Nominal : drag de J1 à J3 → dialog « Affecter une période » EXISTANTE pré-remplie `début=J1 fin=J3` → valider écrit sur l'intervalle → grille converge | 🖥️ IHM | ✅ |
| 4 | Limite : une seule case sans drag = **clic simple INCHANGÉ** (menu clic-case s'ouvre, PAS la dialog plage) | 🖥️ IHM | ✅ |
| 5 | Limite : drag en **sens inverse** (J3→J1) → intervalle **NORMALISÉ** `[min..max]`, dialog `début ≤ fin` (jamais plage vide/inversée) | 🖥️ IHM | ✅ |
| 6 | Limite : drag **débordant la fenêtre de vue** courante → sélection **BORNÉE à la vue chargée**, aucune case hors-vue, aucune navigation, aucune persistance | 🖥️ IHM | ⏳ |
| 7 | Erreur/annulation : **Échap** pendant/après la sélection **ANNULE** (aucune dialog, aucune écriture, surbrillance retirée) — port `IEcouteurEchapModal` s33 | 🖥️ IHM | ⏳ |
| 8 | Gating : **Invité** (non-Parent) ne peut PAS sélectionner (drag inerte, aucune dialog) ; **Parent** seul sélectionne | 🖥️ IHM | ⏳ |

**Répartition** : 2 `@back` (filets de non-régression du chemin d'écriture réutilisé) · 6 `@ihm`
(la surface de sélection est le cœur du sprint, menée RED→GREEN **runtime** sur app câblée).

---

## PORTE DE CONCEPTION — surface d'AFFORDANCE du drag (à confirmer PO au cadrage / G3)

La sélection introduit une **nouvelle surface d'INTERACTION** (pas une surface de lecture, pas une
surface d'écriture neuve : elle **ouvre la dialog existante**). Choix de conception à valider :

- **Emplacement retenu** : **drag directement sur les cases de la grille agenda** (mousedown sur J1,
  survol jusqu'à J3, mouseup) avec **surbrillance progressive** des cases pendant le geste. Le
  relâchement ouvre la dialog « Affecter une période » pré-remplie sur l'intervalle.
- **Alternatives écartées** : (a) bouton « mode sélection » à activer avant de cliquer ; (b) cases à
  cocher par jour puis bouton « Affecter » ; (c) champs de dates manuels début/fin. → écartées :
  friction supérieure, redondantes avec la saisie déjà offerte dans la dialog.
- **Aucune surface de LECTURE neuve** ; **aucune persistance** de l'état de sélection.

> Point remonté au thread principal **en même temps que le tranchage du goal** (garde surface s44).
> Les `@ihm` ne sont menés RED→GREEN qu'une fois l'affordance confirmée.

---

## Scénarios

### Sc.1 — `@back @vert` Filet : affecter une période sur un intervalle multi-jours

```gherkin
Fonctionnalité: Écriture d'une période sur un intervalle (chemin réutilisé par la sélection de plage)
  Contexte:
    Étant donné un foyer configuré avec un cycle de fond et l'acteur "Alice"
    Et un intervalle de trois jours consécutifs J1 < J2 < J3 dans la fenêtre chargée

  Scénario: Affecter une période sur [J1..J3] pose la surcharge sur chaque jour
    Quand une période affectant "Alice" est écrite sur l'intervalle [J1..J3]
    Alors le responsable résolu de J1, J2 et J3 est "Alice" (surcharge B prime sur le fond)
    Et le comportement est identique sur l'adaptateur InMemory et sur Mongo durable
    # Early-green attendu : une période EST un intervalle (s06), réexercé par les plages s45.
    # Ce scénario est un FILET de non-régression du chemin d'écriture que la sélection réutilise.
```

### Sc.2 — `@back @vert` Filet : intervalle d'un seul jour = période ponctuelle

```gherkin
  Scénario: Affecter une période sur [J..J] écrit exactement un jour, sans doublon
    Étant donné un jour unique J dans la fenêtre chargée
    Quand une période affectant "Alice" est écrite sur l'intervalle [J..J]
    Alors seul le jour J porte la surcharge, J-1 et J+1 restent sur le fond
    Et aucune écriture n'est doublonnée (last-write-wins R11, un seul enregistrement)
    Et le comportement est identique InMemory et Mongo durable
    # Garantit que la sélection d'un seul jour (Sc.4) retombe sur le comportement ponctuel connu.
```

### Sc.3 — `@ihm @vert` Nominal : drag → dialog pré-remplie → écriture → convergence

```gherkin
Fonctionnalité: Sélection d'une plage de cases sur la grille agenda
  Contexte:
    Étant donné un utilisateur connecté et Parent sur le hub "/planning"
    Et la grille agenda affichée sur la vue courante

  Scénario: Drag de J1 à J3 ouvre la dialog "Affecter une période" pré-remplie sur l'intervalle
    Quand l'utilisateur presse sur la case J1, survole jusqu'à la case J3, puis relâche
    Alors les cases J1, J2 et J3 sont mises en surbrillance pendant le geste
    Et au relâchement la dialog "Affecter une période" EXISTANTE (s06) s'ouvre
    Et elle est pré-remplie avec début = J1 et fin = J3 (aucune dialog neuve)
    Quand l'utilisateur choisit "Alice" et valide
    Alors la période est écrite sur l'intervalle via le canal d'écriture (réemploi s06)
    Et la grille converge : J1, J2, J3 rendent "Alice"
    # Mené RED->GREEN runtime sur app câblée (store réel), pas de doublure de grille.
```

### Sc.4 — `@ihm @vert` Limite : une seule case sans drag = clic simple inchangé

```gherkin
  Scénario: Un mousedown/mouseup sur la même case conserve le comportement de clic existant
    Quand l'utilisateur clique une case unique J (sans déplacement)
    Alors le menu clic-case EXISTANT s'ouvre (Affecter une période / Définir un transfert / …)
    Et la dialog "Affecter une période" pré-remplie sur une PLAGE n'est PAS ouverte
    Et le comportement de clic simple livré antérieurement est strictement préservé (non-régression)
```

### Sc.5 — `@ihm @vert` Limite : drag en sens inverse normalisé

```gherkin
  Scénario: Drag de J3 vers J1 (sens inverse) normalise l'intervalle
    Quand l'utilisateur presse sur J3, survole jusqu'à J1, puis relâche
    Alors la surbrillance couvre J1, J2, J3 (même intervalle que le sens direct)
    Et la dialog s'ouvre avec début = J1 et fin = J3 (début ≤ fin garanti)
    Et jamais avec une plage inversée ou vide
```

### Sc.6 — `@ihm @pending` Limite : débordement borné à la vue, sans persistance

```gherkin
  Scénario: Un drag qui déborde la fenêtre de vue reste borné à la vue chargée
    Étant donné une vue courante (semaine / 4 semaines glissantes / mois)
    Quand l'utilisateur presse sur une case interne et tire au-delà du bord de la vue
    Alors seules les cases DE LA VUE chargée sont sélectionnées (aucune case hors-vue)
    Et aucune navigation passé/futur n'est déclenchée par le geste
    Et l'état de sélection n'est PAS persisté (volatil, borne anti-cliquet)
    Et un changement de vue ou un rechargement efface la sélection
```

### Sc.7 — `@ihm @pending` Erreur/annulation : Échap annule la sélection

```gherkin
  Scénario: Échap annule la sélection sans ouvrir de dialog ni écrire
    Quand l'utilisateur a une sélection en cours (ou une plage relâchée avant validation)
    Et qu'il presse Échap
    Alors la surbrillance est retirée
    Et aucune dialog "Affecter une période" ne s'ouvre / ne reste ouverte
    Et aucune écriture n'est émise (store intact)
    # Réemploi du port IEcouteurEchapModal s33 (capture au niveau document).
```

### Sc.8 — `@ihm @pending` Gating : Parent seul sélectionne

```gherkin
  Scénario: L'Invité ne peut pas sélectionner de plage
    Étant donné un utilisateur en mode Invité (non-Parent) sur "/planning"
    Quand il presse et tire sur les cases de la grille
    Alors aucune surbrillance de sélection n'apparaît et aucune dialog ne s'ouvre (drag inerte)
    Et seul un utilisateur Parent peut sélectionner une plage (Parent-gated)
```

---

## Notes de cadrage

- **Réemploi strict** : la dialog « Affecter une période » (s06) est la SEULE surface d'écriture ;
  elle est simplement **pré-remplie** avec l'intervalle sélectionné. Aucun handler, aucune commande,
  aucun DTO neuf côté écriture.
- **Back déjà présent** : Sc.1 & Sc.2 sont des **filets** (early-green attendu). Si vert au premier
  run, les **conserver** comme non-régression (ne pas supprimer, ne pas investiguer un faux trou).
- **Aucune persistance** : la sélection vit dans l'état de composant Blazor, effacée au changement de
  vue / rechargement / Échap. Aucune migration, aucun store, aucun read model.
- **Temps réel non concerné** par la sélection elle-même (état client volatil) ; la **convergence de
  la grille** après écriture (Sc.3) repose sur les mécaniques SignalR déjà livrées (reprojection
  client, 0 GET sur push, garde anti-flake `SignalRTempsReelCollection` respectée).

---

# Retours produit (PO)

_(vide — à remplir au gate G3)_
