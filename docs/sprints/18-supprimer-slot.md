# Sprint 18 — Supprimer un slot sur une journée depuis le menu clic-case (`supprimer-slot`)

> **Avancement : 5/10 ⏳**

| # | Scénario | Type | Statut |
|--:|----------|:----:|:------:|
| 1 | Supprimer un slot le retire du store durable relu (+ redémarrage) | @back | ✅ |
| 2 | Slot franchissant minuit : la suppression efface son rendu sur les **deux** jours | @back | ✅ |
| 3 | Pile horaire : supprimer un slot laisse les autres, dans l'ordre horaire | @back | ✅ |
| 4 | Lister les slots couvrant une date (alimente la dialog) | @back | ✅ |
| 5 | Idempotence : supprimer un slot absent / déjà supprimé = no-op qui réussit | @back | ✅ |
| 6 | Menu clic-case → dialog liste les slots → supprimer → grille relue + accusé | 🖥️ @ihm | ⏳ |
| 7 | Annulation : fermer la dialog sans supprimer ne change rien | 🖥️ @ihm | ⏳ |
| 8 | Gating Invité : aucun bouton ni commande de suppression | 🖥️ @ihm | ⏳ |
| 9 | API injoignable : la dialog reste ouverte, message d'échec, rien n'est appliqué | 🖥️ @ihm | ⏳ |
| 10 | Temps réel : la suppression propage la grille sans rechargement | 🖥️ @ihm | ⏳ |

---

> Sujet `/planning` = `supprimer-slot` (épic É6), goal tranché **G2 (PO)** au planning s18.
> Donne suite au **retour produit s17 #1** (« suppression d'un slot sur une journée »), **miroir**
> de la suppression de période livrée au s16. Le store du domaine étant **durable (Mongo, s15)**, la
> suppression touche un **store réel** → **acceptation runtime obligatoire** (rempart anti
> vert-qui-ment).
>
> **Décisions SM (déterministes, aucune porte PO).** Périmètre = **backend d'abord, IHM en fin**.
> Le chemin d'écriture passe par les **dialogs en contexte** déjà livrées (s11/s12/s16/s17) : on
> **ajoute un 6ᵉ usage au menu clic-case**, pas un écran dédié. DELETE **idempotent** (slot absent /
> déjà supprimé = no-op qui **réussit**).
>
> **Différence assumée vs suppression de période** : un slot est une **localisation** (enfant → lieu,
> plage horaire), **pas** une responsabilité de garde. Sa suppression **n'ouvre AUCUNE règle de
> résolution** : pas de repli surcharge > fond > neutre, pas d'effet sur la teinte/responsable de la
> case ni sur la légende des couleurs. Le seul effet observable est le **retrait du slot** de la case
> (et des **deux jours** s'il franchit minuit), les **autres slots restants** demeurant **empilés dans
> l'ordre horaire**. Accusé **non bloquant « Slot supprimé »** à part.
>
> **Hors scope** : édition / re-bornage d'un slot (tranche suivante éventuelle) ; slot imbriqué (É6,
> idée s07) ; suppression de transfert ; édition concurrente sous dialog (P3, derrière la stabilisation
> des flakes SignalR P2 — jamais un driver ici).

**Feature: Supprimer un slot de localisation depuis le planning.** Depuis une case du planning,
ouvrir le menu clic-case puis une **dialog listant les slots couvrant cette date** (enfant, lieu,
bornes horaires) ; en supprimer un le retire du **store durable** et le fait **disparaître de la
case relue** (et de **chacun des deux jours** s'il franchit minuit), les autres slots restant
empilés dans l'ordre horaire, avec un **accusé non bloquant**. La grille reste en **lecture seule** :
la rétroaction passe par le store relu et la **diffusion temps réel**. La suppression est
**idempotente**, **gatée** (Invité interdit) et **robuste à l'échec** (API injoignable → la dialog
reste ouverte, rien n'est appliqué).

## Analyse technique

Légère — l'incrément n'ouvre **aucune règle neuve de résolution** : la **pose** d'un slot, son
**positionnement** dans les cases jour/horaire, l'**empilement** dans l'ordre horaire et le **rendu
sur deux jours** d'un slot franchissant minuit sont acquis (s01/s03), et la durabilité du store
slots au s15. Les seuls RED neufs sont le **retrait de slot** et la **liste des slots d'une date**
(lecture pour la dialog).

- **Application — commande neuve.** `SupprimerSlotCommand(string SlotId)` → `SupprimerSlotHandler`
  renvoyant un `Result` succès/échec. **Idempotent** : un id absent renvoie **succès** (no-op),
  jamais un refus.
- **Application — lecture neuve.** `SlotsDuJourQuery(DateOnly date)` (canal lecture) renvoyant les
  slots **couvrant** la date : identifiant stable, enfant, lieu, bornes horaires. Un slot franchissant
  minuit **couvre les deux jours** → il apparaît dans la liste de chacun. Alimente la dialog ; ne
  déclenche **jamais** la diffusion.
- **Port d'écriture.** Méthode `Supprimer(string slotId)` sur le dépôt de slots existant (miroir de
  `Poser`). Clé = l'**identifiant stable** du slot, **jamais** un libellé.
- **Adaptateurs droite.** Réalisée par l'adaptateur **InMemory** (retrait de la collection) **ET**
  `AdapterDroite.Mongo` (retrait du store durable, s15). Acceptation runtime sur **Mongo réel** : le
  slot disparaît du store relu **et après redémarrage**.
- **Api (adaptateur gauche).** Endpoint canal `POST /api/canal/supprimer-slot`
  (`SupprimerSlotRequete(SlotId)`), même convention succès/échec que les autres écritures ; sur
  succès, déclenche la **diffusion temps réel**. Lecture des slots d'une date via le canal de lecture
  (jamais la diffusion).
- **CQRS préservé.** Write par le canal requête/réponse ; read + diffusion SignalR lecture seule à
  part — jamais confondus, **jamais d'écriture par la diffusion**. La grille reste en lecture seule
  (règle 14).
- **Web (IHM, lot final).** 6ᵉ usage du **menu clic-case** → `SupprimerSlotDialog` listant les slots
  de la date (réutilise le pattern dialog s11/s12/s16/s17) + bouton supprimer par ligne + accusé
  **« Slot supprimé »** à part + **gating Invité** (règle 9, déclencheur rôle mutualisé) + **échec
  API** (règle 28 : la dialog **reste ouverte**, message clair, rien appliqué) + **annulation** sans
  écriture.
- **Bornes anti-cliquet.** Aucune persistance neuve tirée : la suppression **exerce** la durabilité
  déjà acquise (s15, store slots Mongo). Respecter la convention anti-flake sur les tests *TempsReel*
  (rétrofit complet = P2, **hors scope**).
- **Tests.** `PlanningDeGarde.Tests` + `Api.Tests` pour les drivers backend (handler + retrait sur
  deux jours + pile horaire + liste + idempotence), **prouvés au runtime sur store Mongo réel**
  (rempart anti vert-qui-ment, pas de doublure comme seule preuve). `Web.Tests` (bUnit) pour le lot
  IHM final.

### Matrice de couverture

- **Nominal** : Sc.1 (suppression aboutie, store relu) · Sc.4 (liste des slots d'une date) · Sc.6 (dialog + grille relue + accusé).
- **Limite** : Sc.2 (slot à cheval sur minuit, retrait sur deux jours) · Sc.3 (pile horaire préservée) · Sc.7 (annulation sans écriture) · Sc.10 (temps réel).
- **Erreur** : Sc.5 (idempotence absent / déjà supprimé) · Sc.8 (gating Invité, règle 9) · Sc.9 (API injoignable, règle 28).

## Scénarios

10 scénarios. **Drivers backend `@back`** (1→5) = RED neuf à la frontière Application, prouvés au
runtime sur store Mongo réel. **Lot IHM `@ihm`** (6→10) = mené en RED→GREEN runtime (front WASM +
API distante + Mongo réel), filets sur dialog/gating/échec/temps réel. Chaque scénario est
**autonome** (son `Given` complet, **pas de `Background`**).

### Scenario 1 — Supprimer un slot le retire du store durable relu `@back` `@vert`

```gherkin
Scenario: Supprimer un slot le retire de la configuration persistée du domaine
  Étant donné un foyer dont le store durable comporte le lieu "École" et l'enfant "Léa"
  Et un slot durable place "Léa" à "École" le mardi 16 juin 2026 de 08h30 à 16h30, d'identifiant stable connu
  Quand je supprime le slot par son identifiant stable
  Alors la suppression réussit
  Et le slot n'est plus présent dans le store relu
  Et le store relu après redémarrage ne comporte toujours pas ce slot
```

### Scenario 2 — Slot franchissant minuit : la suppression efface son rendu sur les deux jours `@back` `@vert`

```gherkin
Scenario: Supprimer un slot à cheval sur minuit le retire des deux jours qu'il couvrait
  Étant donné un foyer dont le store durable comporte le lieu "Chez Mamie" et l'enfant "Léa"
  Et un slot durable place "Léa" à "Chez Mamie" du mardi 16 juin 2026 22h00 au mercredi 17 juin 2026 07h00
  Et ce slot est rendu à la fois sur la case du mardi 16 juin 2026 et sur celle du mercredi 17 juin 2026
  Quand je supprime ce slot par son identifiant stable
  Alors la suppression réussit
  Et le slot n'apparaît plus dans la case du mardi 16 juin 2026
  Et le slot n'apparaît plus dans la case du mercredi 17 juin 2026
```

### Scenario 3 — Pile horaire : supprimer un slot laisse les autres dans l'ordre horaire `@back` `@vert`

```gherkin
Scenario: Supprimer un slot d'une pile laisse les autres slots du jour empilés dans l'ordre horaire
  Étant donné un foyer dont le store durable comporte les lieux "École", "Piscine" et "Chez Mamie" et l'enfant "Léa"
  Et trois slots du mardi 16 juin 2026 : "École" 08h30-12h00, "Piscine" 14h00-15h30, "Chez Mamie" 17h00-19h00
  Quand je supprime le slot "Piscine" 14h00-15h30 par son identifiant stable
  Alors la suppression réussit
  Et la case du mardi 16 juin 2026 ne comporte plus le slot "Piscine"
  Et la case du mardi 16 juin 2026 comporte encore "École" 08h30-12h00 puis "Chez Mamie" 17h00-19h00, dans l'ordre horaire
```

### Scenario 4 — Lister les slots couvrant une date alimente la dialog `@back` `@vert`

```gherkin
Scenario: La lecture des slots d'une date renvoie ceux qui la couvrent, avec leur identité
  Étant donné un foyer dont le store durable comporte les lieux "École" et "Chez Mamie" et l'enfant "Léa"
  Et un slot place "Léa" à "École" le mardi 16 juin 2026 de 08h30 à 16h30
  Et un slot place "Léa" à "Chez Mamie" du mardi 16 juin 2026 22h00 au mercredi 17 juin 2026 07h00
  Et aucun slot ne couvre le jeudi 18 juin 2026
  Quand je liste les slots couvrant le mardi 16 juin 2026
  Alors la liste comporte les deux slots, chacun avec son identifiant stable, son enfant, son lieu et ses bornes horaires
  Et la liste des slots couvrant le jeudi 18 juin 2026 est vide
```

### Scenario 5 — Idempotence : supprimer un slot absent ou déjà supprimé réussit sans effet `@back` `@vert`

```gherkin
Scenario: Supprimer un slot inexistant ne change rien et ne lève aucune erreur
  Étant donné un foyer dont le store durable comporte un slot "S1" et un slot "S2"
  Quand je supprime un slot d'identifiant "slot-inexistant"
  Alors la suppression réussit sans effet
  Et le store relu comporte toujours "S1" et "S2"
  Quand je supprime une seconde fois le slot "S2"
  Alors la première suppression de "S2" réussit
  Et la seconde suppression de "S2" réussit aussi sans effet supplémentaire
  Et aucune erreur n'est levée
```

### Scenario 6 — Depuis le menu clic-case, la dialog liste les slots et la suppression relit la grille `@ihm` `@pending`

> Lot IHM final, mené en RED→GREEN runtime ; groupable avec Sc.7–Sc.10.
> Acceptation runtime : front WASM + API distante + Mongo réel.

```gherkin
Scenario: Supprimer un slot depuis sa dialog le retire de la case avec un accusé non bloquant
  Étant donné le planning affiché pour un Parent
  Et deux slots du mardi 16 juin 2026 pour "Léa" : "École" 08h30-12h00 et "Piscine" 14h00-15h30
  Quand j'ouvre le menu de la case du mardi 16 juin 2026 et choisis "Supprimer un slot"
  Alors une dialog liste les slots couvrant le mardi 16 juin 2026, dont "École" et "Piscine"
  Quand je supprime le slot "Piscine" 14h00-15h30 dans la dialog
  Alors un accusé "Slot supprimé" s'affiche à part, sans bloquer
  Et la case du mardi 16 juin 2026 ne montre plus le slot "Piscine"
  Et la case du mardi 16 juin 2026 montre encore le slot "École" 08h30-12h00
  Et le slot "Piscine" est absent du store Mongo relu
```

### Scenario 7 — Annulation : fermer la dialog sans supprimer ne change rien `@ihm` `@pending`

```gherkin
Scenario: Fermer la dialog sans confirmer de suppression laisse slots et grille inchangés
  Étant donné le planning affiché pour un Parent
  Et un slot place "Léa" à "École" le mardi 16 juin 2026 de 08h30 à 16h30
  Quand j'ouvre la dialog de suppression des slots du mardi 16 juin 2026
  Et je ferme la dialog sans supprimer
  Alors aucune commande de suppression n'est émise
  Et le slot "École" est toujours présent
  Et la case du mardi 16 juin 2026 affiche toujours le slot "École"
```

### Scenario 8 — Gating Invité : aucun bouton ni commande de suppression `@ihm` `@pending`

> Gating règle 9, déclencheur rôle mutualisé sur le contexte existant.

```gherkin
Scenario: En consultation seule, aucune suppression de slot n'est proposée
  Étant donné le planning affiché pour un Invité en consultation seule
  Et un slot place "Léa" à "École" le mardi 16 juin 2026 de 08h30 à 16h30
  Quand j'ouvre le menu de la case du mardi 16 juin 2026
  Alors l'entrée "Supprimer un slot" n'est pas proposée
  Et aucune commande de suppression ne peut être émise
  Et le slot "École" reste inchangé
```

### Scenario 9 — API injoignable : la dialog reste ouverte, rien n'est appliqué `@ihm` `@pending`

> Échec clair règle 28, registre acquis ; aucune mise en file ni rejeu (PWA = palier ultérieur).

```gherkin
Scenario: Une suppression qui n'atteint pas l'API laisse la dialog ouverte et le planning inchangé
  Étant donné le planning affiché pour un Parent
  Et un slot place "Léa" à "École" le mardi 16 juin 2026 de 08h30 à 16h30
  Quand je supprime le slot "École" dans sa dialog
  Et la commande échoue car l'API distante est injoignable
  Alors un message d'échec clair s'affiche dans la dialog
  Et la dialog reste ouverte
  Et le slot "École" est toujours présent
  Et la case du mardi 16 juin 2026 reste inchangée
  Et aucune mise en file ni rejeu n'est effectué
```

### Scenario 10 — Temps réel : la suppression propage la grille sans rechargement `@ihm` `@pending`

> Diffusion SignalR lecture seule, déclenchée par l'écriture aboutie. Respecter la convention
> anti-flake des tests *TempsReel* (rétrofit complet P2 hors scope).

```gherkin
Scenario: Supprimer un slot sur un écran rafraîchit l'autre écran sans rechargement
  Étant donné deux écrans affichant le même planning partagé, l'un piloté par un Parent
  Et un slot place "Léa" à "École" le mardi 16 juin 2026 de 08h30 à 16h30
  Quand le Parent supprime le slot "École" depuis sa dialog
  Alors le second écran voit, sans rechargement, la case du mardi 16 juin 2026 ne plus afficher le slot "École"
  Et le second écran reflète l'état du store Mongo relu
```

# Retours produit (PO)

_(à remplir au gate G3 / clôture)_
