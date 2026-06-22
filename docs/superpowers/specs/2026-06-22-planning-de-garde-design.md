# Planning de garde — Spécification fonctionnelle

> Portée : fonctionnel uniquement. Aucun choix technique n'est fixé.

## 1. Objectif

Savoir **où est l'enfant et qui en est responsable**, à tout moment, et
**planifier les semaines de garde** de façon partagée entre parents et
intervenants.

## 2. Acteurs & rôles

- **Personnes** : parents, nounou, grand-parent, intervenant ponctuel.
- **Permissions fines, attribuées par personne** (cumulables) :
  - `voir` — consulter le planning
  - `proposer` — suggérer un créneau / un échange (sans l'appliquer)
  - `modifier` — créer / éditer des créneaux
  - `valider` — accepter ou refuser les demandes d'échange
  - `gérer accès` — inviter des personnes, régler leurs droits

## 3. Concepts

- **Enfant** : un seul pour la v1 (modèle prévu pour en accueillir plusieurs
  plus tard).
- **Lieu** : domicile parent A / B, école, chez la nounou, activité… (liste
  éditable).
- **Créneau de garde** — brique de base :
  - heure de début → heure de fin
  - **responsable** : la personne en charge
  - **lieu**
  - **activité** : école, repas, sommeil, activité, temps libre…
  - **transferts** : qui dépose / qui récupère, et à quelle heure
- **Journée** : suite de créneaux enchaînés
  (ex. dépose 7h15 chez la nounou → école 8h05 → nounou 11h45 → …).

## 4. Récurrence

- **Cycle multi-semaines** (ex. semaine paire / semaine impaire) qui se répète
  automatiquement.
- **Exceptions ponctuelles** : surcharge d'un jour précis (vacances, imprévu)
  sans casser le cycle de fond.

## 5. Échanges

- **Demande d'échange** : une personne **propose** un changement → la / les
  personne(s) ayant le droit `valider` **accepte(nt) ou refuse(nt)** → le
  planning est mis à jour si accepté.
- **Historique** des demandes : en attente / acceptée / refusée.

## 6. Notifications

- **Dans l'app** : badge + liste « en attente / changements récents ».
- **Hors app** : email et / ou push.
- Événements notifiés : demande d'échange, changement du planning, rappel de
  transfert.

## 7. Vues

- **Principale — la semaine** : tous les créneaux, responsables, lieux et
  transferts de la semaine en cours.
- **Aujourd'hui / maintenant** : où se trouve l'enfant à l'instant, et les
  prochains transferts.
- **À traiter** : demandes en attente et changements récents.

## 8. Hors périmètre (v1)

- Suivi des heures et paiement de la nounou
- Gestion de plusieurs enfants
- Messagerie entre intervenants
