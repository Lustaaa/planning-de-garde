# Sprint 13 — CRUD acteurs, tranche suppression (palier 8, `crud-acteurs-suppression`)

> Sujet du palier 8 de la spec v13 (`docs/13-specification.md`). Sprint goal (porte G2,
> PO) : compléter le cycle de vie des acteurs par le **Delete**. La suppression est
> **autorisée** (pas de refus « si références ») et **neutralise par repli** les cases
> orphelines : la surcharge orpheline cesse de primer → la case retombe sur le **fond**
> (le cycle reprend) ou sur le **neutre** si l'index n'est ni mappé ni résolu, **sans nom
> fantôme** ; si l'acteur était **mappé au cycle de fond**, son index devient **non mappé →
> neutre**. Accusé **non bloquant « Acteur supprimé »**, **pas de réaffectation automatique**
> (règle 6). La config foyer étant **persistée Mongo** (palier 5), la suppression touche un
> **store réel** → **acceptation runtime obligatoire**.
>
> **Décisions CP (déterministes, aucune porte PO)** : périmètre = **backend d'abord, IHM en
> fin**. Pas de porte G1 (le repli neutre suffit ; la liaison contraignante enfant↔responsable
> n'est pas livrée — un refus contredirait la règle 6). DELETE **idempotent** (acteur
> absent / déjà supprimé = no-op qui **réussit**, grille inchangée). Suppression des acteurs
> **seedés ET ajoutés**, uniformément derrière les ports. Accusé « **Acteur supprimé** » à
> part, non bloquant (registre avertissement, règles 16/25/28). **Hors scope** : impersonation
> (tranche 2, É10), variantes refus/réaffectation, fix dropdown « Acteur du foyer » (fix ciblé
> hors make-gherkin, en tête de sprint).

## Analyse technique

Analyse **légère** — l'incrément n'ouvre **aucune règle de gestion neuve sur la résolution** :
la priorité **surcharge > fond > neutre** (Domain) est déjà livrée au palier 6. Le seul RED
neuf est le **retrait d'acteur** lui-même et son **repli observable**.

- **Application (handler neuf).** `SupprimerActeurCommand(string ActeurId)` →
  `SupprimerActeurHandler`, renvoyant un `Result` succès/échec. **Idempotent** : supprimer un
  id absent renvoie **succès** (no-op), jamais un refus (cohérent avec le non-refus de la
  règle 6 et la sémantique DELETE).
- **Port.** Nouvelle méthode `IEditeurConfigurationFoyer.Supprimer(string acteurId)` (miroir
  écriture, à côté d'`Ajouter` / `Renommer` / `Recolorier`), qui retire l'entrée **nom** et
  **couleur**. L'identifiant stable opaque (`acteur-…`) est la clé — **jamais le libellé**.
- **Adaptateurs droite.** Réalisé par `ConfigurationFoyerEnMemoire` (retrait des dictionnaires
  `_noms` / `_couleurs`) **ET** `ConfigurationFoyerMongo` (retrait du store durable).
  Acceptation runtime sur **Mongo réel** : l'acteur disparaît du store relu (et après
  redémarrage).
- **Api.** Endpoint canal `POST /api/canal/supprimer-acteur` (corps `SupprimerActeurRequete(ActeurId)`),
  même convention succès/échec que les autres écritures. Sur succès, déclenche la diffusion
  temps réel (grilles / légendes suivent).
- **CQRS préservé.** **Write** par le canal requête/réponse ; **read + diffusion SignalR**
  lecture seule à part — jamais confondus, **jamais d'écriture par la diffusion**. La grille
  reste en **lecture seule** (règle 14).
- **Web (IHM, lot final `ihm-builder`).** Bouton supprimer dans l'écran de config + **liste
  relue** + accusé **« Acteur supprimé »** à part + **légende dédoublonnée** + **gating Invité**
  (règle 9, déclencheur mutualisé sur le contexte rôle existant) + **échec API injoignable**
  (règle 28, message clair, suppression non appliquée).
- **Bornes anti-cliquet.** Seule la **config foyer** est durable (Mongo) ; slots / périodes /
  transferts / **cycle de fond** restent **InMemory** (règle 30). Aucune persistance tirée en
  avant. La suppression **exerce** une persistance déjà acquise (palier 5), sans cliquet.
- **Hors scope différé.** Édition concurrente du même jour sous dialog (P3, derrière la
  stabilisation des flakes SignalR P2) — jamais un driver ici. Respecter la convention
  anti-flake sur les tests *TempsReel*.
- **Tests.** `PlanningDeGarde.Tests` + `Api.Tests` pour les drivers backend (handler + repli de
  résolution + idempotence), **prouvés au runtime sur store Mongo réel** (rempart anti
  vert-qui-ment, pas de doublure comme seule preuve). `Web.Tests` (bUnit) pour le lot IHM final.

### Budget de vélocité (parcimonie)

- **Drivers réels (Sc.1 → Sc.5)** = vrai RED pilotant le code neuf : retrait d'acteur + les
  trois variantes de repli + idempotence. Prouvés **runtime sur Mongo réel**.
- **Caractérisations (Sc.6 → Sc.9)** = filets sur mécaniques **déjà livrées** (IHM bouton /
  liste / légende, gating règle 9, échec API règle 28, diffusion SignalR), **probablement
  early-green**. **Lot consécutif groupable** porté par l'`ihm-builder` en fin ; chacune
  annotée `@caractérisation`. Ne pas re-driver : la résolution de priorité (Domain) et le canal
  d'écriture (Api) sont acquis.

### Matrice de couverture (règle 6 au centre)

- **Nominal** : Sc.1 (suppression aboutie, store relu) ; Sc.6 (IHM bouton + liste + message + légende).
- **Limite** : Sc.2 (repli fond), Sc.3 (repli neutre sans nom fantôme), Sc.4 (index cycle non mappé), Sc.9 (temps réel).
- **Erreur** : Sc.5 (idempotence absent / déjà supprimé), Sc.7 (gating Invité, règle 9), Sc.8 (API injoignable, règle 28).

## Scénarios

9 scénarios. **Drivers réels** (1→5) = RED neuf à la frontière Application, prouvés au runtime
sur store Mongo réel. **Caractérisations** (6→9) = lot IHM final groupable, filets sur
mécaniques déjà livrées. Chaque scénario est **autonome** (son `Given` complet, **pas de
`Background`**).

Feature: Supprimer un acteur du foyer — compléter le cycle de vie des acteurs par le Delete.
La suppression est autorisée et neutralise par repli les cases orphelines de l'acteur retiré :
sa surcharge cesse de primer et la case retombe sur le fond (le cycle reprend) ou sur le neutre
si l'index n'est ni mappé ni résolu, sans nom fantôme ; si l'acteur était mappé au cycle de
fond, son index devient non mappé → neutre. La suppression opère sur la config foyer persistée
(store Mongo réel) et s'accompagne d'un accusé non bloquant, sans réaffectation automatique. La
grille reste en lecture seule ; la rétroaction passe par le store relu et la diffusion temps réel.

### Scenario 1 — Supprimer un acteur du foyer le retire du store relu `@nominal` `@driver` `@vert`

```gherkin
Scenario: Supprimer un acteur autorisé le retire de la configuration persistée du foyer
  Étant donné un foyer dont la configuration persiste les acteurs "Parent A", "Parent B" et "Nounou"
  Et la suppression d'un acteur est autorisée sans condition de références
  Quand je supprime l'acteur "Nounou" par son identifiant stable
  Alors la suppression réussit
  Et l'acteur "Nounou" n'est plus présent dans la configuration relue du foyer
  Et les acteurs "Parent A" et "Parent B" sont toujours présents
  Et la configuration relue après redémarrage ne comporte toujours pas "Nounou"
```

### Scenario 2 — Surcharge orpheline : la case retombe sur le fond (le cycle reprend) `@limite` `@driver` `@vert`

```gherkin
Scenario: Supprimer l'acteur d'une période saisie fait retomber sa case sur le responsable de fond
  Étant donné un foyer dont la configuration persiste les acteurs "Parent A" et "Nounou"
  Et un cycle de fond de 2 semaines mappant l'index 0 sur "Parent A" et l'index 1 sur "Parent A"
  Et une période saisie attribue le mardi 16 juin 2026 à "Nounou" (surcharge sur le fond "Parent A")
  Et la case du mardi 16 juin 2026 affiche "Nounou"
  Quand je supprime l'acteur "Nounou"
  Alors la suppression réussit
  Et la surcharge orpheline du mardi 16 juin 2026 cesse de primer
  Et la case du mardi 16 juin 2026 retombe sur le responsable de fond "Parent A"
  Et la case du mardi 16 juin 2026 affiche "Parent A" et sa couleur
```

### Scenario 3 — Surcharge orpheline sur un index non résolu : la case retombe sur le neutre sans nom fantôme `@limite` `@driver` `@vert`

```gherkin
Scenario: Supprimer l'acteur d'une période saisie sur un index non mappé fait retomber sa case sur le neutre
  Étant donné un foyer dont la configuration persiste les acteurs "Parent A" et "Nounou"
  Et un cycle de fond de 2 semaines mappant l'index 0 sur "Parent A" et laissant l'index 1 non mappé
  Et une période saisie attribue le mardi 23 juin 2026 (semaine d'index 1) à "Nounou"
  Et la case du mardi 23 juin 2026 affiche "Nounou"
  Quand je supprime l'acteur "Nounou"
  Alors la suppression réussit
  Et la surcharge orpheline du mardi 23 juin 2026 cesse de primer
  Et l'index 1 du cycle n'étant ni mappé ni résolu, la case retombe sur la teinte neutre
  Et la case du mardi 23 juin 2026 n'affiche aucun nom de responsable
```

### Scenario 4 — Acteur mappé au cycle de fond : son index devient non mappé → neutre `@limite` `@driver`

```gherkin
Scenario: Supprimer un acteur mappé au cycle de fond rend son index non mappé et la case neutre
  Étant donné un foyer dont la configuration persiste les acteurs "Parent A" et "Nounou"
  Et un cycle de fond de 2 semaines mappant l'index 0 sur "Parent A" et l'index 1 sur "Nounou"
  Et aucune période n'est saisie sur la semaine d'index 1
  Et la case du mardi 23 juin 2026 (semaine d'index 1) affiche "Nounou" au titre du fond
  Quand je supprime l'acteur "Nounou"
  Alors la suppression réussit
  Et l'index 1 du cycle de fond devient non mappé
  Et la case du mardi 23 juin 2026 retombe sur la teinte neutre
  Et la case du mardi 23 juin 2026 n'affiche aucun nom fantôme
```

### Scenario 5 — Supprimer un acteur absent ou déjà supprimé est un no-op qui réussit `@erreur` `@driver`

```gherkin
Scenario: Supprimer un acteur inexistant ne change rien et ne lève aucune erreur
  Étant donné un foyer dont la configuration persiste les acteurs "Parent A" et "Parent B"
  Quand je supprime l'acteur d'identifiant "acteur-inexistant"
  Alors la suppression réussit sans effet
  Et la configuration relue comporte toujours "Parent A" et "Parent B"
  Quand je supprime une seconde fois l'acteur "Parent B"
  Alors la première suppression de "Parent B" réussit
  Et la seconde suppression de "Parent B" réussit aussi sans effet supplémentaire
  Et aucune erreur n'est levée
```

### Scenario 6 — Depuis l'écran de config, supprimer relit la liste, dédoublonne la légende et accuse réception `@nominal` `@caractérisation`

> Lot IHM final (`ihm-builder`), filet sur mécaniques déjà livrées ; groupable avec Sc.7–Sc.9.
> Acceptation runtime : front WASM + API distante + Mongo réel.

```gherkin
Scenario: Le bouton supprimer retire l'acteur de la liste et de la légende avec un accusé non bloquant
  Étant donné l'écran de configuration du foyer affiché pour un Parent
  Et le foyer comporte les acteurs "Parent A", "Parent B" et "Nounou"
  Et la légende du planning fait apparaître "Nounou"
  Quand je clique sur le bouton supprimer de l'acteur "Nounou"
  Alors l'acteur "Nounou" disparaît de la liste relue des acteurs
  Et un accusé "Acteur supprimé" s'affiche à part, sans bloquer
  Et la légende dédoublonnée du planning ne fait plus apparaître "Nounou"
  Et l'acteur "Nounou" est absent du store Mongo relu
```

### Scenario 7 — Un Invité ne peut pas supprimer d'acteur `@erreur` `@caractérisation`

> Gating règle 9, déclencheur mutualisé sur le contexte rôle existant ; filet, pas de RED neuf.

```gherkin
Scenario: En consultation seule, aucun bouton de suppression d'acteur n'est disponible
  Étant donné l'écran de configuration du foyer affiché pour un Invité en consultation seule
  Et le foyer comporte les acteurs "Parent A", "Parent B" et "Nounou"
  Alors aucun bouton supprimer n'est proposé pour les acteurs
  Et aucune commande de suppression ne peut être émise
  Et la liste des acteurs reste inchangée
```

### Scenario 8 — API injoignable : suppression non appliquée, liste et grille inchangées `@erreur` `@caractérisation`

> Échec clair règle 28, registre acquis ; filet, pas de RED neuf.

```gherkin
Scenario: Une suppression qui n'atteint pas l'API laisse la configuration et la grille inchangées
  Étant donné l'écran de configuration du foyer affiché pour un Parent
  Et le foyer comporte les acteurs "Parent A", "Parent B" et "Nounou"
  Quand je clique sur le bouton supprimer de l'acteur "Nounou"
  Et la commande échoue car l'API distante est injoignable
  Alors un message d'échec clair s'affiche à l'écran
  Et l'acteur "Nounou" est toujours présent dans la liste
  Et la grille et la légende restent inchangées
  Et aucune mise en file ni rejeu n'est effectué
```

### Scenario 9 — Temps réel : la suppression propage la grille et la légende sans rechargement `@limite` `@caractérisation`

> Diffusion SignalR lecture seule, déclenchée par l'écriture aboutie ; filet. Respecter la
> convention anti-flake des tests *TempsReel* (P2 hors scope).

```gherkin
Scenario: Supprimer un acteur sur un écran rafraîchit l'autre écran sans rechargement
  Étant donné deux écrans affichant le même planning partagé, l'un en configuration pour un Parent
  Et le foyer comporte les acteurs "Parent A" et "Nounou"
  Et une période saisie attribue le mardi 16 juin 2026 à "Nounou", avec le fond "Parent A"
  Quand le Parent supprime l'acteur "Nounou" depuis l'écran de configuration
  Alors le second écran voit, sans rechargement, la case du mardi 16 juin 2026 retomber sur "Parent A"
  Et le second écran voit la légende dédoublonnée ne plus faire apparaître "Nounou"
```
