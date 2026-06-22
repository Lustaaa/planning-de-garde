<!-- Généré via le skill redaction-spec (test sous pression). Exemple, non lié au produit planning-de-garde. -->

# Prise de RDV médical — agenda praticien & réservation patient

## Contexte
Application de prise de rendez-vous médicaux en ligne (style Doctolib) reliant patients et praticiens autour d'un agenda unique. Sert de produit réel, de vitrine et de support d'apprentissage.

## Objectif & arbitrage
Trois objectifs : un outil réellement utilisable au quotidien, une vitrine présentable, un terrain d'apprentissage technique.

> **Arbitre : l'usage réel tranche.** En cas de conflit, on privilégie ce qui rend l'outil fiable et utilisable plutôt que ce qui est démonstratif ou intéressant à coder.

## Séquence de livraison
1. **Agenda fiable du praticien** — source de vérité des créneaux ; sans agenda crédible, rien d'autre n'a de sens.
2. **Prise de RDV par le patient** — la valeur visible du produit : réserver un créneau libre.
3. **Dispos & types de consultation gérés par le praticien** — autonomie d'organisation : le praticien façonne son offre.
4. **Rappels & annulations** — réduit l'absentéisme et fluidifie la libération de créneaux.

## Mécaniques de base
- **Rôles** : Patient (réserve pour lui-même), Praticien (possède l'agenda), Secrétariat (agit pour un ou plusieurs praticiens).
- **Entités cœur** : créneau, praticien, patient, type de consultation (chaque type porte sa propre durée).
- **Créneau** : plage horaire rattachée à un praticien, dans un état unique (libre, réservé, indisponible, passé).
- **Type de consultation** : définit la durée du RDV ; un créneau réservé en hérite.
- **RDV** = créneau réservé + patient + type de consultation.
- **Granularité** : l'agenda se déroule par journées et plages horaires d'un praticien donné.

## Règles de gestion

### Agenda & créneaux (source de vérité)
1. **Agenda par praticien** — Chaque créneau appartient à exactement un praticien et figure sur son seul agenda.
2. **État unique du créneau** — À tout instant un créneau est dans un seul état : libre, réservé, indisponible ou passé.
3. **Non-chevauchement** — Deux créneaux d'un même praticien ne peuvent se chevaucher dans le temps.
4. **Bascule automatique en passé** — Un créneau dont l'horaire est écoulé passe à l'état « passé » et n'est plus réservable.
5. **Indisponibilité prioritaire** — Un créneau marqué indisponible par le praticien ne peut être réservé tant qu'il le reste.
6. **Vérité au moment du rendu** — L'état affiché d'un créneau reflète sa disponibilité réelle à l'instant de la consultation de l'agenda.

### Prise de rendez-vous (patient)
7. **Réservation d'un créneau libre** — Un patient ne peut réserver qu'un créneau actuellement libre.
8. **Choix du type de consultation** — Le patient sélectionne un type de consultation parmi ceux proposés par le praticien lors de la réservation.
9. **Durée dérivée du type** — La durée du RDV est imposée par le type de consultation choisi, jamais saisie librement par le patient.
10. **Premier arrivé** — En cas de demande concurrente sur le même créneau, seule la première réservation aboutit ; les autres sont refusées.
11. **Identité du réserveur** — Toute réservation est rattachée à un patient identifié, qui devient le titulaire du RDV.
12. **Réservation pour soi** — Un patient réserve pour lui-même ; la réservation pour un tiers relève du secrétariat (voir règle 20).
13. **Confirmation immédiate** — À l'issue d'une réservation aboutie, le patient reçoit une confirmation reprenant praticien, date, heure et type.

### Disponibilités & offre (praticien)
14. **Ouverture de créneaux** — Le praticien définit ses plages d'ouverture, qui génèrent les créneaux libres proposés aux patients.
15. **Fermeture / indisponibilité** — Le praticien peut rendre indisponible une plage (absence, congé) ; les créneaux libres concernés disparaissent de l'offre.
16. **Catalogue de types** — Le praticien gère la liste des types de consultation qu'il propose, chacun avec son intitulé et sa durée.
17. **Modification de durée sans rétroaction** — Changer la durée d'un type n'altère pas les RDV déjà pris sous l'ancienne durée.
18. **Protection des créneaux réservés** — Le praticien ne peut fermer une plage contenant un RDV sans traiter d'abord ce RDV (déplacement ou annulation).

### Secrétariat & délégation
19. **Action pour le praticien** — Le secrétariat réserve, déplace et annule des RDV au nom des praticiens qu'il gère, avec les mêmes règles qu'un patient.
20. **Réservation pour un tiers** — Le secrétariat peut créer un RDV au nom d'un patient identifié (prise en charge téléphonique).
21. **Périmètre limité** — Un membre du secrétariat n'agit que sur les agendas des praticiens auxquels il est rattaché.

### Rappels & annulations
22. **Rappel avant échéance** — Un rappel est adressé au patient en amont du RDV (délai défini par le praticien).
23. **Annulation par le patient** — Le patient peut annuler son propre RDV tant que le créneau n'est pas passé.
24. **Libération à l'annulation** — Un RDV annulé repasse son créneau à l'état libre, le rendant de nouveau réservable.
25. **Délai d'annulation** — Une annulation n'est possible qu'au-delà d'un délai minimal avant le RDV, fixé par le praticien.
26. **Déplacement = annulation + réservation** — Déplacer un RDV libère le créneau d'origine et en réserve un nouveau, en une opération unique.
27. **Notification d'annulation** — Toute annulation, par le patient ou côté praticien, notifie l'autre partie.

## Risques & questions ouvertes
- **Comptes patients** : compte authentifié obligatoire pour réserver, ou simple saisie d'identité ? (impact règles 11, 23)
- **Concurrence** : la règle 10 (premier arrivé) suppose un verrouillage du créneau à l'instant de la réservation — mécanique à préciser (réservation en attente vs immédiate).
- **Multi-praticiens / établissement** : la spec couvre des praticiens indépendants ; un regroupement en cabinet (agenda partagé, recherche multi-praticien) est hors périmètre actuel.
- **No-show** : aucune règle ne traite l'absence du patient au RDV (sanction, historique) — à arbitrer.
- **Canaux de notification** : rappels et confirmations (règles 13, 22, 27) ne précisent pas le canal (e-mail, SMS, in-app).
- **Fuseaux & horaires** : gestion des fuseaux et des changements d'heure non traitée.
- **Visibilité des types** : un type peut-il être réservé à certains patients (nouveau vs suivi) ? Non couvert.
