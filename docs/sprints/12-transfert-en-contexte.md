# Sprint 12 — 3ᵉ dialog « Définir un transfert » en contexte (reliquat palier 7, referme l'épic É12)

> Reliquat du palier 7 de la spec v12 (`docs/12-specification.md`). Sprint goal (porte G2,
> PO) : ajouter une **3ᵉ entrée** au menu clic-case ouvrant la dialog « Définir un
> transfert » pré-remplie sur la **date de la case**, puis **retirer la dernière saisie
> dédiée** (route + page + lien `/planning/definir-transfert`). À la livraison, **plus aucun
> écran de saisie dédié ne subsiste** → l'épic « écriture en contexte » est **refermé**.
>
> **Décision CP (observable, Option 3 cadrée conservativement)** : au **succès**, la dialog
> **se ferme** ET un accusé **non bloquant « Transfert défini »** s'affiche **à part**
> (toast/bandeau), en **réutilisant le mécanisme d'avertissement à part** acquis au Sc.7 du
> s11 — c'est un **feedback d'action transitoire**, **pas** un rendu/listing du transfert
> (aucun qui/quand/où en case) → **règle 27 préservée**. L'**acceptation runtime** (rempart
> anti vert-qui-ment, obligatoire) relit le transfert depuis le **store réel InMemory** via
> le pattern `DefinirTransfertCanalApiTests` étendu, **sans rendu grille ni endpoint de
> lecture neuf**.

## Analyse technique

Analyse **légère** — l'incrément n'ouvre **aucune règle de gestion neuve** : c'est le
**déplacement de la saisie du transfert en contexte**, pas une mécanique métier nouvelle.

- **Couche unique touchée = Web (Blazor WASM).** `Application` / `Domain` / adaptateurs
  **inchangés** : **aucun handler neuf**. On réutilise la commande/handler
  **`DefinirTransfert`**, le **canal HTTP** (`POST /api/canal/definir-transfert`) et la
  **diffusion SignalR lecture seule** déjà livrés (s01→s05). CQRS préservé : **write** par
  canal requête/réponse, **read + diffusion** par query + SignalR — jamais confondus,
  **jamais d'écriture par la diffusion**.
- **Extraction en dialog.** Le formulaire de `DefinirTransfert.razor` (dépose / récupère /
  lieu / date / heure) devient un **composant dialog (modal)** réutilisable, déclenché
  depuis le planning. Les sélecteurs dépose/récupère bindent l'**identifiant stable**
  (`parent-a` / `parent-b`), **jamais** le libellé (cohérent avec la résolution couleur,
  règle 19).
- **Menu clic-case = 3ᵉ entrée.** Le menu d'actions ouvert au clic sur une case passe de 2 à
  **3 entrées** (Poser un slot / Affecter une période / **Définir un transfert**). Le
  **gating reste mutualisé** sur le **déclencheur unique** (`Session.EstParent`, règle 9) :
  ajouter une entrée ne change pas le point d'application du droit.
- **Ancrage date de contexte (règle 17).** La date de la **case cliquée prime** sur le
  défaut `IDateTimeProvider` « aujourd'hui ». La page actuelle pré-remplit
  `Horloge.Aujourdhui` en `OnInitialized` ; en contexte, la dialog reçoit la date de la
  case. Le **repli horloge n'est pas supprimé** du port (garde-fou règle 17), il devient
  code mort tant que toute saisie passe par une case.
- **Rétroaction par issue** (grille en **lecture seule**, règle 14, jamais d'écriture par la
  grille) :
  - **succès** → la dialog **se ferme** + accusé **« Transfert défini »** affiché **à part**,
    **non bloquant** (mécanisme du Sc.7 s11 réutilisé) ;
  - **refus domaine OU API injoignable** (règle 28) → **un seul observable** : la dialog
    **reste ouverte**, message d'erreur **dans la dialog**, **saisie conservée** à
    resoumettre, grille **inchangée**.
- **Retrait de l'écran dédié.** Les routes/page/lien `/planning/definir-transfert` sont
  **retirés**, **uniquement APRÈS** que l'acceptation runtime de la dialog prouve la
  **couverture intégrale** de l'écran supprimé (borne du Risque P1).
- **Bornes anti-cliquet.** Le transfert **reste InMemory** (règle 30) ; grille **lecture
  seule** (règle 14, **annuler** n'émet aucune commande) ; **pas** de rendu du transfert en
  case ni d'amorce du **panneau cloche** (palier 14 / É9, **hors scope**). Ni auth ni
  impersonation tirées (paliers 9/16 intacts).
- **Tests.** `Web.Tests` (bUnit) : ouverture de la 3ᵉ entrée, pré-remplissage par la date de
  la case, accusé « Transfert défini » au succès, message **dans** la dialog au refus / API
  injoignable, gating Invité, absence de route/page/lien dédiés. **Acceptation runtime
  obligatoire** sur app **réellement câblée** (front WASM + API distante + SignalR + store
  réel) : un transfert **réellement enregistré** via la dialog est **relu depuis le store**
  (`DefinirTransfertCanalApiTests` étendu). Respecter la **convention anti-flake** sur les
  tests *TempsReel* (P2 stabilisation hors scope).

### Caractérisations hors numérotation (filets déjà verts, non re-drivés)

- **Invariant domaine « transfert incomplet »** (récupération ou heure manquante →
  `TimeSpan.Zero`) — vert s01 (`Scenario11_DefinirTransfert`, `Scenario12_TransfertIncomplet`) ;
  **couvert indirectement** par le Scenario Outline n°3 via ses `Examples` (refus du domaine).
- **Gating du menu lui-même** — acquis s11 (l'Invité ne voit pas le menu) ; ici seul l'ajout
  de la 3ᵉ entrée est neuf, et le gating reste mutualisé (rien à re-driver côté menu, seuls
  les contrôles positif/négatif sur l'accès au menu sont portés, Sc.1 / Sc.4).
- **Convergence temps réel sous dialog ouverte** (le rafraîchissement de fond n'interfère pas
  avec la dialog) — acquis s10 ; **hors numérotation**.
- **Édition concurrente du même jour sous dialog ouverte** — **P3 différée** (derrière
  stabilisation flakes SignalR P2) ; **hors scope**, jamais un driver de ce fichier.

## Scénarios

6 scénarios. **Drivers réels** (1→5) = comportement IHM neuf (3ᵉ dialog en contexte + accusé
succès + échec dans la dialog + gating + retrait du dernier écran dédié). **Caractérisation**
(6) = filet groupable sur un invariant déjà couvert. Chaque scénario est **autonome** (son
`Given` complet, **pas de `Background`**).

Feature: Définir un transfert en contexte — l'utilisateur agit là où il lit. Depuis le
planning, un clic sur une case ouvre le menu d'actions dont la 3ᵉ entrée ouvre la dialog
« Définir un transfert » pré-remplie sur la date de cette case ; la saisie validée
(dépositaire, récupérateur, lieu, heure) est enregistrée via le canal existant, la dialog se
ferme et un accusé non bloquant le confirme, l'enregistrement étant prouvé par relecture du
store réel au runtime. La grille reste en lecture seule et le transfert n'y est pas rendu
(panneau cloche séquencé) : la rétroaction passe par la dialog ou un accusé à part, jamais
par une écriture de la grille. À la livraison, plus aucun écran de saisie dédié ne subsiste.

### Scenario 1 — Définir un transfert depuis une case via le menu clic-case `@nominal`

```gherkin
Scenario: Définir un transfert depuis une case ferme la dialog et affiche l'accusé "Transfert défini"
  Étant donné le planning partagé affiché pour un Parent
  Et le foyer comporte les acteurs "Parent A" et "Parent B" et le lieu "École"
  Et la case du mardi 16 juin 2026 est visible dans la fenêtre
  Quand je clique sur la case du mardi 16 juin 2026
  Alors le menu d'actions s'ouvre et propose l'entrée "Définir un transfert"
  Quand je choisis l'entrée "Définir un transfert"
  Alors la dialog "Définir un transfert" s'ouvre
  Quand je choisis "Parent A" comme dépositaire, "Parent B" comme récupérateur, le lieu "École" à 08:30
  Et je valide la dialog
  Alors la dialog se ferme
  Et un accusé "Transfert défini" s'affiche à part, sans bloquer
  Et le transfert "Parent A" → "Parent B" au lieu "École" le mardi 16 juin 2026 à 08:30 est relu depuis le store
```

### Scenario 2 — La dialog se pré-remplit sur la date de la case cliquée `@limite`

```gherkin
Scenario: Ouvrir la dialog de transfert depuis une case future pré-remplit la saisie sur la date de cette case
  Étant donné le planning partagé affiché pour un Parent
  Et la date de référence "aujourd'hui" est le lundi 15 juin 2026
  Et la case du jeudi 25 juin 2026 est visible dans la fenêtre
  Quand je clique sur la case du jeudi 25 juin 2026
  Et je choisis l'entrée "Définir un transfert"
  Alors la dialog "Définir un transfert" s'ouvre
  Et la date du transfert est pré-remplie sur le jeudi 25 juin 2026
  Et la date du transfert n'est pas le lundi 15 juin 2026
```

### Scenario 3 — Échec : la dialog reste ouverte et conserve la saisie `@erreur`

```gherkin
Scenario Outline: Un transfert qui n'aboutit pas laisse la dialog ouverte et la grille inchangée
  Étant donné le planning partagé affiché pour un Parent
  Et la case du vendredi 19 juin 2026 est visible dans la fenêtre
  Quand je clique sur la case du vendredi 19 juin 2026
  Et je choisis l'entrée "Définir un transfert"
  Et je saisis <saisie>
  Et je valide la dialog
  Et la commande échoue pour cause de <cause>
  Alors la dialog "Définir un transfert" reste ouverte
  Et le message "<message>" s'affiche dans la dialog
  Et ma saisie <saisie> est conservée à resoumettre
  Et la grille reste inchangée

  Examples:
    | cause            | saisie                                                          | message                                          |
    | refus du domaine | "Parent A" comme dépositaire, sans récupérateur, le lieu "École" | Transfert incomplet : la récupération est requise |
    | API injoignable  | "Parent A" → "Parent B" au lieu "École" à 08:30                  | Service indisponible : à resoumettre              |
```

### Scenario 4 — Un Invité ne peut pas ouvrir le menu depuis une case `@erreur`

```gherkin
Scenario: En consultation seule, cliquer une case n'ouvre aucun menu ni dialog de transfert
  Étant donné le planning partagé affiché pour un Invité en consultation seule
  Et la case du mardi 16 juin 2026 est visible dans la fenêtre
  Quand je clique sur la case du mardi 16 juin 2026
  Alors aucun menu d'actions ne s'ouvre
  Et aucune dialog "Définir un transfert" ne s'ouvre
  Et le déclencheur d'écriture de la case est désactivé
  Et la grille reste en lecture seule
```

> Contrôle positif Parent **en regard** : l'ouverture du menu par un Parent (et la présence
> de la 3ᵉ entrée « Définir un transfert ») est portée par le Scenario 1.

### Scenario 5 — La page de saisie dédiée « Définir un transfert » n'existe plus `@limite`

```gherkin
Scenario: Le transfert ne se saisit plus que depuis une case, aucun écran dédié ne subsiste
  Étant donné le planning partagé affiché pour un Parent
  Quand je cherche un lien "Définir un transfert" dans la barre du planning
  Alors aucun lien vers un écran de saisie dédié de transfert n'est présent
  Quand j'ouvre directement la route "/planning/definir-transfert"
  Alors la route n'existe plus
  Et le seul chemin pour définir un transfert est la dialog ouverte depuis une case
```

### Scenario 6 — Annuler la dialog n'émet aucune écriture `@limite` `@caractérisation`

> Caractérisation (filet) : le pattern « annuler une dialog » est acquis au s11 ; il est ici
> transposé au transfert. Probablement early-green ; **groupable** avec les filets du même
> pattern et l'invariant « transfert incomplet » (absorbé par les `Examples` du Sc.3).

```gherkin
Scenario: Annuler la dialog de transfert n'émet aucune commande et laisse la grille intacte
  Étant donné le planning partagé affiché pour un Parent
  Et la case du samedi 20 juin 2026 est visible dans la fenêtre
  Quand je clique sur la case du samedi 20 juin 2026
  Et je choisis l'entrée "Définir un transfert"
  Et la dialog "Définir un transfert" s'ouvre
  Et je choisis "Parent A" comme dépositaire et "Parent B" comme récupérateur
  Et j'annule la dialog sans valider
  Alors la dialog se ferme
  Et aucune écriture n'est émise
  Et aucun accusé "Transfert défini" ne s'affiche
  Et la grille reste inchangée
```
