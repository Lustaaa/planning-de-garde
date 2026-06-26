# Sprint 06 — Saisie visible

> Palier 2 « saisie visible » de la spec v06 (règles 15, 16, 17). Lève d'un coup
> les deux défauts liés du faux bug « les saisies n'apparaissent pas », à ne pas
> confondre : (A) la **date par défaut = aujourd'hui** est une évolution
> d'ergonomie de saisie (les formulaires portent une date figée 2025, la saisie
> tombe hors de la fenêtre affichée) ; (B) la **couleur du parent** est une
> non-conformité d'implémentation (un libellé fourni à la place de l'identifiant
> stable fait retomber la case sur le neutre). Aucun comportement vert n'est cassé
> par (A) ; (B) est confronté au code courant et localisé ci-dessous.

## Analyse technique

Les deux défauts vivent dans les **adaptateurs de gauche** (vues WASM + seed de
l'API), pas dans le domaine ni dans la lecture CQRS. La résolution de couleur
(`GrilleAgendaQuery.CouleurResponsableAu` → `IPaletteCouleurs.CouleurDe`,
`src/PlanningDeGarde.Application/GrilleAgendaQuery.cs:53-57`) est **correcte** et
reste **inchangée** ; le set `CouleursParActeur`
(`src/PlanningDeGarde.Infrastructure/Foyer.cs:26-32` :
`parent-a→bleu`, `parent-b→orange`, `nounou→vert`, neutre `gris`) ne bouge pas.

**(A) Date par défaut = aujourd'hui.** Les formulaires portent une date figée :
`PoserSlot.razor.cs:23-24` (`new(2025,7,15,…)`), `AffecterPeriode.razor.cs:23-24`
(`new(2025,7,14)` / `new(2025,7,21)`), `DefinirTransfert.razor:76`
(`new(2025,7,21)`, logique dans le template — dette : pas de code-behind). On
introduit une abstraction injectable `IDateTimeProvider` exposant `Today`, dont
les trois formulaires se servent pour pré-remplir leurs dates ; **jamais**
`DateTime.Today` en dur. Le double de test fixe la date concrète (ex. `2026-06-26`)
— symétrie avec le déterminisme déjà tenu côté lecture (`Projeter(dateReference)`).
La fenêtre affichée est de **35 jours datés** depuis le lundi de la semaine de la
date de référence (5 semaines).

**(B) Couleur par identifiant stable.** Le défaut est en amont, à la **source** :
`Foyer.Responsables` (`src/PlanningDeGarde.Web/Foyer.cs:17-20` et
`src/PlanningDeGarde.Infrastructure/Foyer.cs:15-18`) expose les **libellés**
`« Parent A » / « Parent B »` ; les sélecteurs bindent `value="@r"`
(`AffecterPeriode.razor:21`, `DefinirTransfert.razor:23,33`) et le seed sème ces
libellés (`src/PlanningDeGarde.Api/SeedDonneesDemo.cs:25-26,31-32`). Le canal reçoit
donc `ResponsableId = "Parent A"`, qui n'est pas une clé de `CouleursParActeur` →
repli neutre (gris). **Correction : la source fournit l'identifiant stable**
(`parent-a` / `parent-b`) au canal — sélecteur affichant le libellé mais bindant
l'identifiant, et seed semant l'identifiant — pour que le set devienne atteignable.

**Note technique — DefinirTransfert.** On corrige aussi la source de ses sélecteurs
Dépose/Récupère vers l'**identifiant stable** par cohérence, **mais sans observable
couleur** : aucun transfert n'est projeté dans la grille à ce palier (trou par
construction, palier « immédiat & événements / cloche » ultérieur). Le scénario de
cette vue ne porte donc **que la date par défaut**, pas de `Then` couleur. La
correction de date + source doit respecter la **convention code-behind** (la dette
template de cette vue est signalée).

**Garde-fous.** *Vert qui ment* : les scénarios exigent des slots/périodes
**réellement enregistrés** via le canal et **relus par la grille** (runtime WASM +
API distante), pas une grille statique. *Gris assumé ≠ gris-bug* : le repli neutre
d'un acteur **légitimement hors set** (règle 17) est conforme — distinct du gris
provoqué par un libellé fourni à la place de l'identifiant. *Déterminisme* :
`IDateTimeProvider` est **doublé** en test, jamais `DateTime.Today` en dur.

## Scénarios

Feature: Saisie visible — une saisie posée réapparaît immédiatement dans la grille,
à la bonne date (date par défaut = aujourd'hui) et en couleur du parent responsable
(couleur résolue sur l'identifiant stable de l'acteur). Couvre (A) la date par défaut
sur les trois formulaires de saisie et (B) la couleur du parent sur l'affectation de
période.

### Scenario 1 — Slot posé sans toucher aux dates réapparaît à aujourd'hui

`@nominal` `@vert`

```gherkin
Scenario: Poser un slot sans modifier la date le place au jour d'aujourd'hui
  Given la date de référence est le 26 juin 2026
  And un parent ouvre le formulaire "poser un slot"
  And il ne modifie aucune date pré-remplie
  When il pose un slot "école" de 8h30 à 16h30 et revient au planning
  Then la case du 26 juin 2026 porte le slot "école" de 8h30 à 16h30
```

### Scenario 2 — Période affectée sans toucher aux dates tombe dans la fenêtre

`@nominal` `@vert`

```gherkin
Scenario: Affecter une période sans modifier les dates la rend visible aujourd'hui
  Given la date de référence est le 26 juin 2026
  And un parent ouvre le formulaire "affecter une période"
  And il choisit le responsable "Parent A" et ne modifie aucune date pré-remplie
  When il valide l'affectation et revient au planning
  Then la case du 26 juin 2026 est colorée pour la période affectée
```

### Scenario 3 — Transfert défini sans toucher à la date prend aujourd'hui

`@nominal` `@vert`

```gherkin
Scenario: Définir un transfert sans modifier la date l'horodate à aujourd'hui
  Given la date de référence est le 26 juin 2026
  And un parent ouvre le formulaire "définir un transfert"
  And il renseigne dépose "Parent A", récupère "Parent B", lieu "école", heure 16h30
  And il ne modifie pas la date pré-remplie
  When il valide le transfert
  Then la commande envoyée au canal porte la date du 26 juin 2026
  And la saisie est acceptée sans erreur
```

### Scenario 4 — Saisie à la borne haute de la fenêtre reste visible

`@limite` `@vert`

```gherkin
Scenario: Un slot au dernier jour de la fenêtre de 35 jours reste affiché
  Given la date de référence est le 26 juin 2026
  And la grille affiche les 35 jours datés depuis le lundi 22 juin 2026
  And un parent pose un slot "domicile A" au 26 juillet 2026, dernier jour de la fenêtre
  When la grille est projetée
  Then la case du 26 juillet 2026 porte le slot "domicile A"
  And un slot posé au 27 juillet 2026 ne figure dans aucune case de la fenêtre
```

### Scenario 5 — Date figée hors fenêtre fait disparaître la saisie

`@erreur` `@vert`

```gherkin
Scenario: Une date par défaut figée en 2025 fait tomber la saisie hors de la fenêtre
  Given la date de référence est le 26 juin 2026
  And le formulaire "poser un slot" pré-remplit une date figée au 15 juillet 2025
  And un parent pose un slot sans corriger cette date
  When la grille du 26 juin 2026 est projetée
  Then aucune case de la fenêtre ne porte ce slot
  And la saisie semble avoir disparu alors qu'elle est enregistrée hors fenêtre
```

### Scenario 6 — Période affectée à un parent se colore à sa couleur

`@nominal` `@vert`

```gherkin
Scenario: Une période affectée au Parent A s'affiche en bleu et au Parent B en orange
  Given le set de couleurs associe parent-a au bleu et parent-b à l'orange
  And le sélecteur d'affectation fournit l'identifiant stable du responsable
  And une période est affectée au responsable "Parent A" du 24 au 27 juin 2026
  And une période est affectée au responsable "Parent B" du 28 au 30 juin 2026
  When la grille est projetée
  Then les cases du 24 au 27 juin 2026 sont bleues
  And les cases du 28 au 30 juin 2026 sont orange
```

### Scenario 7 — Acteur hors set retombe sur le neutre (gris assumé)

`@limite`

```gherkin
Scenario: Une période affectée à un acteur absent du set s'affiche en gris neutre
  Given le set de couleurs ne contient pas l'identifiant "grand-pere"
  And une période est affectée au responsable d'identifiant stable "grand-pere" le 24 juin 2026
  When la grille est projetée
  Then la case du 24 juin 2026 est grise par repli neutre conforme
  And ce gris traduit un acteur non encore colorié, pas un défaut de résolution
```

### Scenario 8 — Libellé fourni à la place de l'identifiant fait retomber sur gris

`@erreur`

```gherkin
Scenario: Un libellé d'affichage envoyé comme responsable fait retomber la case sur gris
  Given le set de couleurs associe parent-a au bleu
  And une période est réellement affectée au 24 juin 2026 avec le responsable "Parent A"
  And "Parent A" est le libellé d'affichage, non l'identifiant stable parent-a
  When la grille est projetée
  Then la case du 24 juin 2026 est grise alors qu'un responsable y est affecté
  And ce gris trahit un libellé fourni à la place de l'identifiant stable
```
