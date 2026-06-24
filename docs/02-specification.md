# Planning de garde — Organisation des semaines de garde

> Version 02 · consolide la v01 + docs/sprints/01-semaine-de-garde/99-besoins-fin-itération.md.

## Contexte

Une app où parents et intervenants (nounou, grands-parents…) organisent à
l'avance et partagent les semaines de garde des enfants d'un foyer. Le hub
`/planning` est la mémoire partagée du foyer : un calendrier navigable où l'on
lit qui garde qui, où, quand, et d'où l'on agit. Les acteurs réels du foyer
s'authentifient pour que le planning reflète la réalité plutôt que des SMS
éparpillés.

## Objectif & arbitrage

L'app poursuit trois buts : être un **outil réellement utilisé**, servir de
**vitrine** technique, et rester un **terrain d'apprentissage**. En cas de
conflit entre les trois, on garde ce qui sert l'usage quotidien et on coupe le
reste.

> **Arbitre : l'usage réel tranche.** Entre deux besoins qui s'opposent, gagne
> celui qui rend le hub utilisable au quotidien : les données et le câblage qui
> débloquent l'usage priment sur l'ergonomie de surface, qui prime elle-même
> sur l'ouverture de l'accès.

## Séquence de livraison

Tous les besoins comptent, mais ils sont **séquencés** (pas livrés en bloc).
Chaque phase doit être adoptée avant la suivante :

1. **Mémoire partagée fiable** — UNE source de vérité commune (qui garde qui, où, quand) à la place des SMS éparpillés. *Socle : sans lui, rien d'autre ne sert.*
2. **Immédiat & rappels** — « qui récupère ce soir », où est l'enfant maintenant, rappels de transfert présentés comme événements à venir. *Valeur quotidienne, faible coût de saisie.*
3. **Modèle d'acteurs & foyer** — déclarer les acteurs réels du foyer (Admin, Parents, Autres) dans un écran de configuration, pour que le planning s'adresse à de vraies personnes. *Prérequis de l'ouverture de l'accès.*
4. **Anticipation & cycle** — cycle multi-semaines récurrent et projection sur les semaines / vacances à venir, visualisé dans un calendrier navigable (semaine + 4 semaines). *Plus lourd à saisir, vient une fois le socle adopté.*
5. **Imprévu & échange** — gérer enfant malade / retard / échange de dernière minute, avec accord entre parents et transferts ponctuels à l'exception. *Le plus délicat ; après que l'adoption à deux est acquise.*
6. **Ouverture de l'accès** — landing page et authentification des acteurs réels (email via fournisseurs Gmail / Apple / Microsoft) pour lever le risque mortel d'adoption de l'autre parent. *Vient après le socle et le modèle d'acteurs.*

## Mécaniques de base

- Un créneau de garde a un horaire (début → fin), un responsable unique, un lieu et une activité
- Une journée est une suite de créneaux enchaînés
- Le planning suit un cycle de plusieurs semaines qui se répète automatiquement
- Le hub `/planning` est un calendrier navigable façon agenda (semaine en cours + 4 semaines suivantes), où la responsabilité de chaque garde se lit par un code couleur
- Les actions d'écriture (poser un slot, affecter une période, ajuster un transfert) se font en contexte depuis le calendrier, via des dialogs alimentées par les acteurs et lieux du foyer
- Les transferts et changements à venir sont présentés comme des événements dans un panneau de notifications (cloche), pas comme un tableau permanent
- Trois types d'acteurs : Admin, Parent, Autre — chacun avec un affichage adapté à son type
- La composition du foyer (enfants, parents, acteurs autres) se déclare dans un écran de configuration distinct du planning

## Règles de gestion

### Foyer & acteurs

1. **Multi-enfants** — Un foyer compte au moins un enfant, et peut en compter plusieurs, chacun avec sa propre organisation de garde

2. **Familles recomposées** — Un foyer peut mélanger des enfants de parents différents, tous visibles dans le même planning

3. **Toujours deux parents** — Un foyer a toujours exactement deux parents ; tant qu'un seul est inscrit, il peut saisir les informations de l'autre

4. **Acteurs « autres »** — Un foyer peut avoir N acteurs « autres » (nounou, grands-parents…), éditables par les parents ou par l'acteur lui-même

5. **Configuration distincte du planning** — Déclarer qui sont les acteurs (parents, autres) se fait dans l'écran de configuration du foyer ; affecter qui garde quand reste une action contextuelle du calendrier, alimentée par les acteurs du foyer

### Rôles & accès

6. **Trois types d'acteurs** — Trois types d'accès : Admin (gère tout, y compris la configuration du foyer), Parent (gère les créneaux, lieux, acteurs et invitations de son foyer), Autre (consultation et édition limitée à ses propres informations). L'affichage s'adapte au type

7. **Modification réservée aux parents et à l'admin** — Seul un Parent (ou l'Admin) peut créer ou éditer un créneau ou une période ; un acteur « Autre » n'édite que ses propres informations

### Planning & créneaux

8. **Responsable unique** — Un créneau a toujours un seul responsable (pas de « X ou Y »)

9. **Cycle récurrent** — Le planning se répète selon un cycle de plusieurs semaines (ex : semaine paire / impaire)

10. **Exception ponctuelle** — Un jour précis peut être surchargé sans casser le cycle de fond ; le cycle reprend ensuite

11. **Lieux éditables** — La liste des lieux (domicile A/B, école, nounou, activité…) est librement modifiable et sert de référentiel aux sélecteurs de saisie

### Transferts

12. **Transfert dérivé par défaut** — Le transfert d'un enfant (qui dépose, qui récupère, à quelle heure) est dérivé automatiquement du planning par défaut : c'est le cas nominal, sans saisie

13. **Transfert modifiable et ponctuel** — Un transfert auto-calculé peut être modifié, et des transferts ponctuels peuvent être proposés ou ajoutés à l'exception (urgence, changement d'emploi du temps)

### Modifications & notifications

14. **Modification directe** — Un Parent applique son changement immédiatement ; les autres acteurs concernés sont notifiés (pas de workflow de validation en v1)

15. **Notifications & événements à venir** — Les notifications in-app couvrent les changements de planning et les rappels de transfert ; les transferts et changements à venir sont consultables dans un panneau d'événements accessible via une cloche

## Risques & questions ouvertes

- **Adoption de l'autre parent (risque mortel)** — L'app n'a de valeur que si l'autre parent l'utilise aussi ; sinon le planning est faux. L'ouverture de l'accès (auth réelle) la traite, mais elle vient tard dans la séquence : ne pas laisser glisser indéfiniment.
- **Différenciation** — Vs Cozi / FamilyWall / agenda partagé : le manque précis qu'on couvre mieux reste à nommer.
- **Coût de saisie du cycle** — Saisir un cycle multi-semaines est lourd ; à valider seulement si le cycle est stable dans le temps. Le transfert dérivé automatiquement réduit ce coût pour le cas nominal.
- **Refonte calendrier transverse** — Le passage du tableau au calendrier navigable touche slots, responsabilité (code couleur) et transferts ; risque de bloc indivisible, à découper en incréments adoptables.
- **Identité visuelle** — Un thème en accord avec le domaine (garde d'enfants) est attendu ; ergonomie de surface, subordonnée à l'usage par l'arbitre.
