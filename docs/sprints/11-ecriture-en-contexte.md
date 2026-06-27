# Sprint 11 — Écriture en contexte : dialogs depuis le planning

> Palier 7 de la spec v11 (`docs/11-specification.md`). Sprint goal (porte G2, PO) :
> déplacer la saisie là où on lit. Supprimer les écrans dédiés **Poser un slot** et
> **Affecter une période** et les rouvrir comme **2 dialogs ouvertes depuis une case
> cliquée** du planning, pré-remplies sur la **date de la case**. Le **transfert** est
> une **tranche de secours** (3ᵉ dialog), livrée si le scope ~2h tient, sinon séquencée
> juste derrière — jamais reportée en bloc.

## Analyse technique

Analyse **légère** — l'incrément n'ouvre **aucune règle de gestion neuve** : c'est le
**déplacement de la saisie en contexte**, pas une mécanique métier nouvelle.

- **Couche unique touchée = Web (Blazor WASM).** `Application` / `Domain` / adaptateurs
  **inchangés** : **aucun handler neuf**. On réutilise les commandes/handlers `PoserSlot`
  et `AffecterPeriode`, le **canal HTTP** (`POST poser-slot` / `POST affecter-periode`) et
  la **diffusion SignalR lecture seule** déjà livrés (s04→s10). CQRS préservé : **write**
  par canal requête/réponse, **read + diffusion** par `GrilleAgendaQuery` + SignalR —
  jamais confondus, **jamais d'écriture par la diffusion**.
- **Extraction en dialogs.** Les formulaires de `PoserSlot.razor` / `AffecterPeriode.razor`
  deviennent des **composants dialog (modal)** réutilisables, **déclenchés depuis
  `PlanningPartage.razor`**. Le **clic sur une case** (`data-testid="jour-case"`) ouvre la
  dialog correspondante.
- **Suppression des écrans dédiés.** Les routes `/planning/poser-slot` et
  `/planning/affecter-periode` sont **retirées**, ainsi que leurs **liens-barre**
  (`PlanningPartage.razor`). Le lien **Définir un transfert** reste tant que le transfert
  est en tranche de secours.
- **Ancrage case + date de contexte.** La case fournit la **date pré-remplie**, qui
  **prime** sur le défaut `IDateTimeProvider` « aujourd'hui » (**règle 17 composée**, non
  révisée : le défaut nu ne vaut que **hors-contexte**). Pré-remplissage par **une** case
  ≠ **sélection de plage** (intervalle multi-cases) → **hors scope**.
- **Rétroaction par issue** (grille en **lecture seule**, règle 14, jamais d'écriture par
  la grille) :
  - **succès** → la dialog **se ferme**, la grille relue via retour commande/diffusion ;
  - **refus domaine OU API injoignable** (règle 28) → **un seul observable** : la dialog
    **reste ouverte**, message d'erreur **dans la dialog**, **saisie conservée**, grille
    **inchangée** ;
  - **chevauchement** (règle 16) → écriture **aboutie** : la dialog **se ferme**, le slot
    **réapparaît**, l'avertissement s'affiche **à part** (toast/bandeau), **non bloquant**.
- **Droits Invité.** Le déclencheur d'écriture migre de l'écran dédié **vers la case** :
  le **gater** en consultation seule (Invité, règle 9) est du **rendu conditionnel IHM
  neuf**, réutilisant le **contexte rôle existant** (`SessionPlanning`, acquis s01) — **ni
  auth ni impersonation** tirées (paliers 8/15 intacts).
- **Borne anti-cliquet.** Aucune persistance tirée en avant : **slots / périodes restent
  InMemory**.
- **Tests.** `Web.Tests` (bUnit) pour l'ouverture/fermeture des dialogs, le pré-remplissage
  par la date de la case, le message d'erreur **dans** la dialog, le rendu conditionnel
  Invité. **Acceptation runtime obligatoire** sur app **réellement câblée** (front WASM +
  API distante + SignalR + Mongo config foyer) — **rempart anti vert-qui-ment** : prouver
  qu'une saisie **réellement enregistrée** réapparaît **positionnée, colorée et nommée** à
  la **date de la case**.

### Caractérisations hors numérotation (filets déjà verts, non re-drivés)

- **Convergence temps réel sous dialog ouverte** : pendant qu'une dialog est ouverte, une
  écriture d'un autre acteur **rafraîchit la grille** par diffusion SignalR **sans fermer
  ni perdre** la dialog (**dernière écriture gagne**, règle 11) — acquis s10.
- **Validation domaine sous-jacente** (durée nulle, lieu inexistant, responsable requis) —
  verte s01, **couverte indirectement** par le Scenario Outline n°4 via ses `Examples`.

### Candidats « tranche de secours » (hors numérotation principale)

- **3ᵉ dialog — Définir un transfert** ouverte depuis une case : livrée **si le scope ~2h
  tient**, sinon séquencée juste derrière. **Jamais** un driver de ce fichier.
- **Édition concurrente du même jour** pendant qu'une dialog est ouverte (résolution à la
  validation) : **hors scope** ce sprint, candidat séquençable derrière.

## Scénarios

7 scénarios. **Drivers** (1→6) = comportement IHM neuf (ouverture en contexte +
réapparition + erreur dans la dialog + droit Invité). **Caractérisation** (7) = filet sur
un invariant déjà couvert (règle 16, accepté + averti), groupable. Chaque scénario est
**autonome** (son `Given` complet, pas de `Background`).

Feature: Écriture en contexte — l'utilisateur agit là où il lit. Depuis le planning, un
clic sur une case ouvre la dialog d'écriture (Poser un slot, Affecter une période)
pré-remplie sur la date de cette case ; la saisie validée réapparaît immédiatement dans la
grille, positionnée à la bonne date, colorée et nommée par responsable, prouvée sur
câblage réel. La grille reste en lecture seule : la rétroaction passe par la dialog ou un
bandeau, jamais par une écriture de la grille.

### Scenario 1 — Poser un slot depuis une case du planning `@nominal` `@vert`

```gherkin
Scenario: Poser un slot depuis une case ouvre la dialog et le slot réapparaît dans cette case
  Étant donné le planning partagé affiché pour un Parent
  Et la case du mardi 16 juin 2026, sans slot
  Quand je clique sur la case du mardi 16 juin 2026
  Alors la dialog "Poser un slot" s'ouvre
  Quand je choisis le lieu "École" de 08:30 à 16:30
  Et je valide la dialog
  Alors la dialog se ferme
  Et un slot "École" de 08:30 à 16:30 apparaît dans la case du mardi 16 juin 2026
```

### Scenario 2 — Affecter une période depuis une case du planning `@nominal` `@vert`

```gherkin
Scenario: Affecter une période depuis une case colore et nomme la case au responsable choisi
  Étant donné le planning partagé affiché pour un Parent
  Et la case du mercredi 17 juin 2026, sans responsable affiché
  Et le foyer comporte l'acteur "Alice" avec sa couleur propre
  Quand je clique sur la case du mercredi 17 juin 2026
  Alors la dialog "Affecter une période" s'ouvre
  Quand je choisis "Alice" comme responsable
  Et je valide la dialog
  Alors la dialog se ferme
  Et la case du mercredi 17 juin 2026 affiche le nom "Alice"
  Et la case du mercredi 17 juin 2026 prend la couleur propre d'"Alice"
  Et la légende agrège "Alice" avec sa couleur
```

### Scenario 3 — La dialog se pré-remplit sur la date de la case cliquée `@limite` `@vert`

```gherkin
Scenario: Ouvrir une dialog depuis une case future pré-remplit la saisie sur la date de cette case
  Étant donné le planning partagé affiché pour un Parent
  Et la date de référence "aujourd'hui" est le lundi 15 juin 2026
  Et la case du jeudi 25 juin 2026 est visible dans la fenêtre
  Quand je clique sur la case du jeudi 25 juin 2026
  Alors la dialog "Poser un slot" s'ouvre
  Et la date de la saisie est pré-remplie sur le jeudi 25 juin 2026
  Et la date de la saisie n'est pas le lundi 15 juin 2026
  Quand je choisis le lieu "Maison" de 17:00 à 19:00 et je valide la dialog
  Alors un slot apparaît dans la case du jeudi 25 juin 2026
```

### Scenario 4 — Échec clair : la dialog reste ouverte et conserve la saisie `@erreur`

```gherkin
Scenario Outline: Une commande qui n'aboutit pas laisse la dialog ouverte et la grille inchangée
  Étant donné le planning partagé affiché pour un Parent
  Et la case du vendredi 19 juin 2026, sans slot
  Quand je clique sur la case du vendredi 19 juin 2026
  Et je saisis <saisie>
  Et je valide la dialog
  Et la commande échoue pour cause de <cause>
  Alors la dialog "Poser un slot" reste ouverte
  Et le message "<message>" s'affiche dans la dialog
  Et ma saisie <saisie> est conservée à resoumettre
  Et la grille reste inchangée
  Et aucun slot n'apparaît dans la case du vendredi 19 juin 2026

  Examples:
    | cause            | saisie                            | message                              |
    | refus du domaine | le lieu "Atlantide" de 08:00 à 09:00 | Lieu inconnu : saisie non appliquée  |
    | API injoignable  | le lieu "École" de 08:00 à 09:00     | Service indisponible : à resoumettre |
```

### Scenario 5 — Annuler la dialog ne modifie pas le planning `@limite`

```gherkin
Scenario: Annuler une dialog n'émet aucune écriture et laisse la grille intacte
  Étant donné le planning partagé affiché pour un Parent
  Et la case du samedi 20 juin 2026 affiche le responsable de fond "Bruno"
  Quand je clique sur la case du samedi 20 juin 2026
  Et la dialog "Affecter une période" s'ouvre
  Et je choisis "Alice" comme responsable
  Et j'annule la dialog sans valider
  Alors la dialog se ferme
  Et aucune écriture n'est émise
  Et la case du samedi 20 juin 2026 affiche toujours le responsable de fond "Bruno"
```

### Scenario 6 — Un Invité ne peut pas ouvrir la dialog depuis une case `@erreur`

```gherkin
Scenario: En consultation seule, cliquer une case n'ouvre aucune dialog d'écriture
  Étant donné le planning partagé affiché pour un Invité en consultation seule
  Et la case du mardi 16 juin 2026 est visible dans la fenêtre
  Quand je clique sur la case du mardi 16 juin 2026
  Alors aucune dialog d'écriture ne s'ouvre
  Et le déclencheur d'écriture de la case est désactivé
  Et la grille reste en lecture seule
```

### Scenario 7 — Slot chevauchant accepté avec avertissement non bloquant `@limite`

> Caractérisation (filet) : la **règle 16** (accepté + averti) est déjà verte depuis le
> s01 ; seul l'**habillage IHM** est neuf (fermeture de la dialog + avertissement à part,
> non bloquant). Probablement early-green ; groupable avec les filets de validation domaine.

```gherkin
Scenario: Poser un slot qui en chevauche un autre est accepté et signalé sans bloquer
  Étant donné le planning partagé affiché pour un Parent
  Et la case du lundi 22 juin 2026 contient déjà un slot "École" de 08:00 à 12:00
  Quand je clique sur la case du lundi 22 juin 2026
  Et je choisis le lieu "Nounou" de 10:00 à 14:00
  Et je valide la dialog
  Alors la dialog se ferme
  Et un slot "Nounou" de 10:00 à 14:00 apparaît dans la case du lundi 22 juin 2026
  Et un avertissement de chevauchement s'affiche à part, sans bloquer
  Et le slot "École" de 08:00 à 12:00 reste présent dans la case
```
