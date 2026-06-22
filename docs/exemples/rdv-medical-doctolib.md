<!-- Généré via le skill redaction-spec (test sous pression). Exemple, non lié au produit planning-de-garde. -->

# Prise de RDV médical — vitrine agenda praticien

## Contexte

Application de prise de rendez-vous médicaux en ligne, dans l'esprit de Doctolib, reliant patients et praticiens autour d'un agenda unique. Le praticien configure ses disponibilités et ses types de consultation ; l'agenda en découle et expose des créneaux ; le patient réserve un créneau libre.

Ce cadrage est une **vitrine** destinée à être présentée et démontrée en direct, et non un produit de production. Les sujets pénibles inhérents au domaine (no-show, RGPD / données de santé, double-booking concurrent, course sur un même créneau) sont volontairement écartés de l'implémentation réelle : ils sont **seedés, simulés ou scénarisés** pour la démonstration.

## Objectif & arbitrage

L'objectif est de produire un outil **utilisable** et une **vitrine présentable**. L'apprentissage technique est explicitement écarté comme but prioritaire : on optimise l'effet démontrable, pas la robustesse de production.

> **Arbitre : la vitrine tranche.** Quand « utilisable » et « vitrine » s'opposent, l'effet démontrable l'emporte. Concrètement, ce qui est réellement implémenté se limite au CRUD praticien (dispositions + types de consultation) et à la réservation simple d'un créneau libre. Tout le reste — rappels, annulations, déplacements, gestion des cas pénibles — est **seedé / simulé / scénarisé** pour servir la démo, jamais construit comme une vraie mécanique.

## Séquence de livraison

1. **Back-office praticien — cœur de la démo.** CRUD des dispositions (plages horaires) et des types de consultation (chacun portant sa durée). Le praticien configure son agenda en live : c'est l'effet démontrable central.
2. **Agenda crédible et peuplé.** Les créneaux sont générés à partir de la configuration du praticien, avec des états lisibles (libre / réservé / indisponible / passé).
3. **Parcours patient au second plan.** Un patient réserve un créneau libre. Présent et fonctionnel, mais non prioritaire dans la mise en scène.
4. **Rappels & annulations simulés.** Scénarisés pour la démo (déroulé visuel), sans véritable mécanique de notification ni de relance.

## Mécaniques de base

- **Praticien** : utilisateur qui configure son agenda (plages et types de consultation).
- **Agenda** : vue unique rattachée à un praticien, peuplée de créneaux.
- **Créneau** : plage horaire élémentaire rattachée à un praticien, dans un état unique à un instant donné (libre, réservé, indisponible, passé).
- **Type de consultation** : modèle de rendez-vous portant sa durée et son intitulé.
- **Patient** : utilisateur qui réserve un créneau libre.
- **Rendez-vous (RDV)** : créneau réservé associé à un patient et à un type de consultation.

## Règles de gestion

### Configuration praticien

1. **CRUD dispositions** — le praticien crée, édite et supprime ses plages de disponibilité, en tant que cœur de la démonstration.
2. **CRUD types de consultation** — le praticien crée et édite des types de consultation, chacun portant son intitulé et sa durée.
3. **Non-chevauchement** — deux plages de disponibilité d'un même praticien ne peuvent pas se chevaucher dans le temps.

### Génération & états de l'agenda

4. **Génération des créneaux** — les créneaux sont dérivés automatiquement des plages de disponibilité configurées par le praticien.
5. **État unique** — à un instant donné, un créneau porte un seul état parmi libre, réservé, indisponible et passé.
6. **Bascule en passé** — un créneau dont l'horaire est dépassé passe automatiquement à l'état passé, indépendamment de son état antérieur.

### Réservation patient

7. **Réservation d'un créneau libre** — un patient peut réserver un créneau uniquement lorsqu'il est à l'état libre.
8. **Durée dérivée du type** — la durée du rendez-vous est déterminée par le type de consultation choisi, et non saisie librement.
9. **Protection du créneau réservé** — un créneau passé à l'état réservé n'est plus proposable à la réservation par un autre patient.

### Annulation & déplacement (simulés)

10. **Annulation simulée** — l'annulation d'un rendez-vous est scénarisée pour la démo et libère visuellement le créneau, sans mécanique de notification réelle.
11. **Déplacement simulé** — le déplacement d'un rendez-vous vers un autre créneau libre est scénarisé pour la démo, sans traitement transactionnel réel.

## Risques & questions ouvertes

- **Effet moins « waouh » côté patient** — une démo centrée sur la configuration praticien peut sembler moins spectaculaire qu'un parcours patient complet ; à équilibrer dans la mise en scène.
- **Tenue de la frontière seedé / réel** — risque de confusion sur ce qui est réellement implémenté (CRUD praticien + réservation simple) versus scénarisé (rappels, annulations, déplacements) ; à expliciter en démo.
- **Double-booking visible** — un même créneau réservé deux fois apparaîtrait comme un défaut majeur si la scénarisation est mal calée ; à verrouiller côté mise en scène.
- **Crédibilité de l'agenda** — sans gestion patient complète, l'agenda peuplé doit rester convaincant ; dépend de la qualité du seed.
- **RGPD / no-show hors périmètre** — données de santé et no-show sont explicitement hors implémentation, assumés comme dette de démo.
