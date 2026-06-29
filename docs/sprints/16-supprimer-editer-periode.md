# Sprint 16 — Supprimer une période depuis le menu clic-case (`supprimer-editer-periode`)

> **Avancement : 5/10 ⏳**

| # | Scénario | Type | Statut |
|--:|----------|:----:|:------:|
| 1 | Supprimer une période la retire du store durable relu (+ redémarrage) | @back | ✅ |
| 2 | Repli fond : la case retombe sur le responsable de fond (le cycle reprend) | @back | ✅ |
| 3 | Repli neutre sans nom fantôme (index non résolu) | @back | ✅ |
| 4 | Lister les périodes couvrant une date (alimente la dialog) | @back | ✅ |
| 5 | Idempotence : supprimer une période absente / déjà supprimée = no-op qui réussit | @back | ✅ |
| 6 | Menu clic-case → dialog liste les périodes → supprimer → grille relue + accusé | 🖥️ @ihm | ⏳ |
| 7 | Annulation : fermer la dialog sans supprimer ne change rien | 🖥️ @ihm | ⏳ |
| 8 | Gating Invité : aucun bouton ni commande de suppression | 🖥️ @ihm | ⏳ |
| 9 | API injoignable : la dialog reste ouverte, message d'échec, rien n'est appliqué | 🖥️ @ihm | ⏳ |
| 10 | Temps réel : la suppression propage grille + légende sans rechargement | 🖥️ @ihm | ⏳ |

---

> Sujet `/2-make-gherkin` = `supprimer-editer-periode` (épics É7 + É12), goal tranché **G2 (PO)**.
> Comble la dette **« trou fonctionnel assumé »** (retours s02 #6 · s03) : aucune édition ni
> suppression de période depuis l'IHM aujourd'hui. Le store du domaine étant **durable (Mongo,
> s15)**, la suppression touche un **store réel** → **acceptation runtime obligatoire**.
>
> **Décisions CP (déterministes, aucune porte PO).** Périmètre = **backend d'abord, IHM en fin**.
> Le chemin d'écriture passe par les **dialogs en contexte** déjà livrées (s11/s12) : on **ajoute
> un 4ᵉ usage au menu clic-case**, pas un écran dédié. DELETE **idempotent** (période absente /
> déjà supprimée = no-op qui **réussit**). Suppression d'une **surcharge** (période saisie) →
> repli **surcharge > fond > neutre** déjà livré (palier 6) ; la case se re-résout, **sans nom
> fantôme**. Accusé **non bloquant « Période supprimée »** à part. **Hors scope** : l'**édition**
> de période (re-borner / réaffecter — tranche suivante, le titre du goal la nomme mais la cadrage
> G2 ne livre que le Delete) ; suppression de slot / transfert ; édition concurrente sous dialog
> (P3, derrière la stabilisation des flakes SignalR P2 — jamais un driver ici).

**Feature: Supprimer une période de garde depuis le planning.** Depuis une case du planning,
ouvrir le menu clic-case puis une **dialog listant les périodes couvrant cette date** ; en
supprimer une la retire du **store durable** et fait **re-résoudre** la case (elle retombe sur le
responsable de **fond** si le cycle le résout, sinon sur le **neutre**, sans nom fantôme), avec un
**accusé non bloquant**. La grille reste en **lecture seule** : la rétroaction passe par le store
relu et la **diffusion temps réel**. La suppression est **idempotente**, **gatée** (Invité interdit)
et **robuste à l'échec** (API injoignable → la dialog reste ouverte, rien n'est appliqué).

## Analyse technique

Légère — l'incrément n'ouvre **aucune règle neuve de résolution** : la priorité **surcharge > fond
> neutre** (Domain) est acquise au palier 6, et la durabilité du store périodes au s15. Les seuls
RED neufs sont le **retrait de période** et la **liste des périodes d'une date** (lecture pour la
dialog).

- **Application — commande neuve.** `SupprimerPeriodeCommand(string PeriodeId)` →
  `SupprimerPeriodeHandler` renvoyant un `Result` succès/échec. **Idempotent** : un id absent
  renvoie **succès** (no-op), jamais un refus.
- **Application — lecture neuve.** `PeriodesDuJourQuery(DateOnly date)` (canal lecture) renvoyant
  les périodes **couvrant** la date : identifiant stable, bornes, responsable. Alimente la dialog ;
  ne déclenche **jamais** la diffusion.
- **Port d'écriture.** Méthode `Supprimer(string periodeId)` sur le dépôt de périodes existant
  (miroir d'`Affecter`). Clé = l'**identifiant stable** de la période, **jamais** un libellé.
- **Adaptateurs droite.** Réalisée par l'adaptateur **InMemory** (retrait de la collection) **ET**
  `AdapterDroite.Mongo` (retrait du store durable, s15). Acceptation runtime sur **Mongo réel** : la
  période disparaît du store relu **et après redémarrage**.
- **Api (adaptateur gauche).** Endpoint canal `POST /api/canal/supprimer-periode`
  (`SupprimerPeriodeRequete(PeriodeId)`), même convention succès/échec que les autres écritures ;
  sur succès, déclenche la **diffusion temps réel**. Lecture des périodes d'une date via le canal de
  lecture (jamais la diffusion).
- **CQRS préservé.** Write par le canal requête/réponse ; read + diffusion SignalR lecture seule à
  part — jamais confondus, **jamais d'écriture par la diffusion**. La grille reste en lecture seule
  (règle 14).
- **Web (IHM, lot final `ihm-builder`).** 4ᵉ usage du **menu clic-case** → `SupprimerPeriodeDialog`
  listant les périodes de la date (réutilise le pattern dialog s11/s12) + bouton supprimer par
  ligne + accusé **« Période supprimée »** à part + **gating Invité** (règle 9, déclencheur rôle
  mutualisé) + **échec API** (règle 28 : la dialog **reste ouverte**, message clair, rien appliqué) +
  **annulation** sans écriture.
- **Bornes anti-cliquet.** Aucune persistance neuve tirée : la suppression **exerce** la durabilité
  déjà acquise (s09 config foyer, s15 reste du domaine). Respecter la convention anti-flake sur les
  tests *TempsReel* (rétrofit complet = P2, **hors scope**).
- **Tests.** `PlanningDeGarde.Tests` + `Api.Tests` pour les drivers backend (handler + repli de
  résolution + liste + idempotence), **prouvés au runtime sur store Mongo réel** (rempart anti
  vert-qui-ment, pas de doublure comme seule preuve). `Web.Tests` (bUnit) pour le lot IHM final.

### Matrice de couverture

- **Nominal** : Sc.1 (suppression aboutie, store relu) · Sc.4 (liste des périodes d'une date) · Sc.6 (dialog + grille relue + accusé).
- **Limite** : Sc.2 (repli fond) · Sc.3 (repli neutre sans nom fantôme) · Sc.7 (annulation sans écriture) · Sc.10 (temps réel).
- **Erreur** : Sc.5 (idempotence absent / déjà supprimé) · Sc.8 (gating Invité, règle 9) · Sc.9 (API injoignable, règle 28).

## Scénarios

10 scénarios. **Drivers backend `@back`** (1→5) = RED neuf à la frontière Application, prouvés au
runtime sur store Mongo réel. **Lot IHM `@ihm`** (6→10) = mené en RED→GREEN runtime par
l'`ihm-builder` (front WASM + API distante + Mongo réel), filets sur dialog/gating/échec/temps réel.
Chaque scénario est **autonome** (son `Given` complet, **pas de `Background`**).

### Scenario 1 — Supprimer une période la retire du store durable relu `@back` `@vert`

```gherkin
Scenario: Supprimer une période la retire de la configuration persistée du domaine
  Étant donné un foyer dont le store durable comporte les acteurs "Parent A" et "Nounou"
  Et une période durable attribue le mardi 16 juin 2026 à "Nounou", d'identifiant stable connu
  Quand je supprime la période par son identifiant stable
  Alors la suppression réussit
  Et la période n'est plus présente dans le store relu
  Et le store relu après redémarrage ne comporte toujours pas cette période
```

### Scenario 2 — Repli fond : la case retombe sur le responsable de fond (le cycle reprend) `@back` `@vert`

```gherkin
Scenario: Supprimer une période saisie fait retomber sa case sur le responsable de fond
  Étant donné un foyer dont le store durable comporte les acteurs "Parent A" et "Nounou"
  Et un cycle de fond de 2 semaines mappant l'index 0 et l'index 1 sur "Parent A"
  Et une période durable attribue le mardi 16 juin 2026 à "Nounou" (surcharge sur le fond "Parent A")
  Et la case du mardi 16 juin 2026 affiche "Nounou"
  Quand je supprime la période du mardi 16 juin 2026
  Alors la suppression réussit
  Et la surcharge du mardi 16 juin 2026 cesse de primer
  Et la case du mardi 16 juin 2026 retombe sur le responsable de fond "Parent A"
  Et la case du mardi 16 juin 2026 affiche "Parent A" et sa couleur
```

### Scenario 3 — Repli neutre sans nom fantôme (index non résolu) `@back` `@vert`

```gherkin
Scenario: Supprimer une période sur un index de fond non mappé fait retomber sa case sur le neutre
  Étant donné un foyer dont le store durable comporte les acteurs "Parent A" et "Nounou"
  Et un cycle de fond de 2 semaines mappant l'index 0 sur "Parent A" et laissant l'index 1 non mappé
  Et une période durable attribue le mardi 16 juin 2026 (semaine ISO 25, index 1) à "Nounou"
  Et la case du mardi 16 juin 2026 affiche "Nounou"
  Quand je supprime la période du mardi 16 juin 2026
  Alors la suppression réussit
  Et l'index 1 du cycle n'étant ni mappé ni résolu, la case retombe sur la teinte neutre
  Et la case du mardi 16 juin 2026 n'affiche aucun nom de responsable
```

### Scenario 4 — Lister les périodes couvrant une date alimente la dialog `@back` `@vert`

```gherkin
Scenario: La lecture des périodes d'une date renvoie celles qui la couvrent, avec leur identité
  Étant donné un foyer dont le store durable comporte les acteurs "Parent A" et "Nounou"
  Et une période attribue du lundi 15 au mercredi 17 juin 2026 la garde à "Nounou"
  Et une période attribue le mardi 16 juin 2026 la garde à "Parent A"
  Et aucune période ne couvre le jeudi 18 juin 2026
  Quand je liste les périodes couvrant le mardi 16 juin 2026
  Alors la liste comporte les deux périodes, chacune avec son identifiant stable, ses bornes et son responsable
  Et la liste des périodes couvrant le jeudi 18 juin 2026 est vide
```

### Scenario 5 — Idempotence : supprimer une période absente ou déjà supprimée réussit sans effet `@back` `@vert`

```gherkin
Scenario: Supprimer une période inexistante ne change rien et ne lève aucune erreur
  Étant donné un foyer dont le store durable comporte une période "P1" et une période "P2"
  Quand je supprime une période d'identifiant "periode-inexistante"
  Alors la suppression réussit sans effet
  Et le store relu comporte toujours "P1" et "P2"
  Quand je supprime une seconde fois la période "P2"
  Alors la première suppression de "P2" réussit
  Et la seconde suppression de "P2" réussit aussi sans effet supplémentaire
  Et aucune erreur n'est levée
```

### Scenario 6 — Depuis le menu clic-case, la dialog liste les périodes et la suppression relit la grille `@ihm` `@pending`

> Lot IHM final (`ihm-builder`), mené en RED→GREEN runtime ; groupable avec Sc.7–Sc.10.
> Acceptation runtime : front WASM + API distante + Mongo réel.

```gherkin
Scenario: Supprimer une période depuis sa dialog la retire et fait re-résoudre la case avec un accusé non bloquant
  Étant donné le planning affiché pour un Parent, avec le fond "Parent A" résolu le mardi 16 juin 2026
  Et une période attribue le mardi 16 juin 2026 à "Nounou", surfacée dans la case et la légende
  Quand j'ouvre le menu de la case du mardi 16 juin 2026 et choisis "Supprimer une période"
  Alors une dialog liste les périodes couvrant le mardi 16 juin 2026, dont celle de "Nounou"
  Quand je supprime la période de "Nounou" dans la dialog
  Alors un accusé "Période supprimée" s'affiche à part, sans bloquer
  Et la case du mardi 16 juin 2026 retombe sur "Parent A" et sa couleur
  Et la légende dédoublonnée ne fait plus apparaître "Nounou" si plus aucune période ne le porte
  Et la période est absente du store Mongo relu
```

### Scenario 7 — Annulation : fermer la dialog sans supprimer ne change rien `@ihm` `@pending`

```gherkin
Scenario: Fermer la dialog sans confirmer de suppression laisse périodes et grille inchangées
  Étant donné le planning affiché pour un Parent
  Et une période attribue le mardi 16 juin 2026 à "Nounou"
  Quand j'ouvre la dialog de suppression des périodes du mardi 16 juin 2026
  Et je ferme la dialog sans supprimer
  Alors aucune commande de suppression n'est émise
  Et la période de "Nounou" est toujours présente
  Et la case du mardi 16 juin 2026 affiche toujours "Nounou"
```

### Scenario 8 — Gating Invité : aucun bouton ni commande de suppression `@ihm` `@pending`

> Gating règle 9, déclencheur rôle mutualisé sur le contexte existant.

```gherkin
Scenario: En consultation seule, aucune suppression de période n'est proposée
  Étant donné le planning affiché pour un Invité en consultation seule
  Et une période attribue le mardi 16 juin 2026 à "Nounou"
  Quand j'ouvre le menu de la case du mardi 16 juin 2026
  Alors l'entrée "Supprimer une période" n'est pas proposée
  Et aucune commande de suppression ne peut être émise
  Et la période de "Nounou" reste inchangée
```

### Scenario 9 — API injoignable : la dialog reste ouverte, rien n'est appliqué `@ihm` `@pending`

> Échec clair règle 28, registre acquis ; aucune mise en file ni rejeu (PWA = palier ultérieur).

```gherkin
Scenario: Une suppression qui n'atteint pas l'API laisse la dialog ouverte et le planning inchangé
  Étant donné le planning affiché pour un Parent
  Et une période attribue le mardi 16 juin 2026 à "Nounou"
  Quand je supprime la période de "Nounou" dans sa dialog
  Et la commande échoue car l'API distante est injoignable
  Alors un message d'échec clair s'affiche dans la dialog
  Et la dialog reste ouverte
  Et la période de "Nounou" est toujours présente
  Et la case et la légende du mardi 16 juin 2026 restent inchangées
  Et aucune mise en file ni rejeu n'est effectué
```

### Scenario 10 — Temps réel : la suppression propage grille et légende sans rechargement `@ihm` `@pending`

> Diffusion SignalR lecture seule, déclenchée par l'écriture aboutie. Respecter la convention
> anti-flake des tests *TempsReel* (rétrofit complet P2 hors scope).

```gherkin
Scenario: Supprimer une période sur un écran rafraîchit l'autre écran sans rechargement
  Étant donné deux écrans affichant le même planning partagé, l'un piloté par un Parent
  Et le fond "Parent A" résolu le mardi 16 juin 2026
  Et une période attribue le mardi 16 juin 2026 à "Nounou"
  Quand le Parent supprime la période de "Nounou" depuis sa dialog
  Alors le second écran voit, sans rechargement, la case du mardi 16 juin 2026 retomber sur "Parent A"
  Et le second écran voit la légende dédoublonnée ne plus faire apparaître "Nounou" si plus aucune période ne le porte
```

# Retours produit (PO)

_(à remplir au gate G3 / clôture)_
