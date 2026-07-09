# Brief PO — Refonte de la Configuration du foyer

> **Brief produit (PO)**, capté à la clôture s30 (déplacé depuis `docs/Configuration du foyer.md`).
> C'est le **retour structuré complet** annoncé au /planning s29 pour l'édition d'un acteur via
> crayon + dialog. Source d'un **candidat/épic backlog** (cf. `docs/BACKLOG.md`, épic « Refonte de la
> Configuration du foyer »). **Pas encore de la spec canonique** : sera édité en diff dans
> `docs/specs/acteurs-et-config-foyer.md` au fil des sprints qui le livrent. Contenu conservé verbatim.

## Acteurs

### Lecture

J'aimerai un tableau avec la liste des acteurs du foyer et les infos pratique en lecture seul.

- Nom de l'acteur
- email
- role dans le foyer
- etat en pastille (actif ou non, admin ou non)

Au bout du tableau une colonne action avec un crayon pour la modification. L'action modifier permet de modifier tous les champs dans une modal.

Au bas du tableau, un bouton pour ajouter un acteur, qui ouvre la modal mais avec tous les champs vide pour création.

### Modification

Tous les champs d'info sur l'acteur sont chargé dans la modal et sont modifiable.

- Nom de l'acteur
- email
- role dans le foyer
- etat en toogle (actif ou non, admin ou non)
- adresse de résidence
- couleur (avec une vrai palette)

## Role

### Lecture

Harmoniser par rapport aux acteurs. Un tableau de visu en lecture seul et une modal pour la modification.

## Cycle

Harmoniser par rapport aux acteurs. Un tableau de visu en lecture seul et une modal pour la modification. Et a minima, j'aimerai un tableau avec les cycles setté et actif

## Lieux

Je pense que le lieux se rapproche d'un acteur sans role (ex : ecole, piscine, foot). Je pense qu'il faut lier ca a l'enfant (possible que plusieurs enfants soit lié au meme lieux). Je pense que lieux n'est pas le bon terme, mais que c'est plus activité avec comme propriété :

- nom
- adresse
- liste de slot (avec un flag récurent ou non)

## Enfant

J'aimerai que quand on arrive sur la configuration du foyer, un vue en lecture seul soit affiché comme un graph avec comme racine l'enfant.

## Bonus (second sprint)

- Pouvoir configurer plusieurs enfants.
- La vu planning doit etre centré sur la garde des enfant d'un couple de parent (Pour l'instant un seul enfant)
- Un vu supplémentaire sur planning devra afficher la garde des enfant d'un couple recomposé avec des gardes différente
  - Faire une proposition de configuration du foyer pour traiter les foyé recomposé.
