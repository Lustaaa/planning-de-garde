# 🗓️ Planning de garde

> **Organisez à l'avance les semaines de garde de vos enfants, ensemble.**

Une app simple pour planifier et partager les semaines de garde entre parents
et intervenants (nounou, grands-parents…), sans tableur partagé ni groupe de
messages qui déborde.

---

## 😩 Le problème

En garde alternée — ou simplement quand plusieurs personnes s'occupent des
enfants — tout se joue sur l'**anticipation** :

> *« Qui prend les enfants la semaine prochaine ? Qui récupère jeudi ?
> Et pendant les vacances, on s'organise comment ? »*

Les plannings vivent dans des têtes, des SMS et des tableurs jamais à jour.
Résultat : on s'y prend au dernier moment, on oublie, on se télescope.

## ✅ La solution

Un planning **partagé et préparé à l'avance** où chacun voit, en un coup d'œil :

- **qui** garde quel enfant, et **quand**,
- les **transferts** à venir (qui dépose, qui récupère),
- et, à l'instant T, **où** se trouve chaque enfant.

L'idée : poser l'organisation des semaines à venir une bonne fois, et que tout
le monde s'y retrouve sans avoir à demander.

---

## ✨ Fonctionnalités (v1)

| | Fonctionnalité |
|---|---|
| 👨‍👩‍👧‍👦 | **Plusieurs enfants** — pensé dès le départ pour les familles recomposées (chaque enfant a sa propre organisation) |
| 🏠 | **Le foyer, configuré** — un écran déclare les acteurs : toujours 2 parents (l'un peut saisir l'autre) et N intervenants « autres » |
| 👤 | **3 types d'acteurs** — *Admin* (configure le foyer), *Parent* (gère le planning) et *Autre* (nounou, grands-parents… accès limité à ses infos) |
| 🧩 | **Créneaux de garde** — responsable unique, lieu, activité ; saisis en contexte depuis le calendrier |
| 🔄 | **Transferts automatiques** — qui dépose / récupère est *déduit du planning*, modifiable et complétable à l'exception (urgence, imprévu) |
| 🔁 | **Récurrence** — cycle multi-semaines (semaine paire / impaire) qui se répète tout seul |
| ⚡ | **Exceptions ponctuelles** — surcharger un jour (vacances, imprévu) sans casser le cycle |
| ✏️ | **Modification directe** — un parent change le planning, les autres sont notifiés |
| 🔔 | **Notifications & événements à venir** — changements récents et transferts à venir, dans un panneau accessible via une cloche |

### Le hub : un calendrier partagé

Une vue **calendrier navigable** (semaine en cours + 4 semaines suivantes, façon agenda) est le cœur de l'app : la responsabilité de chaque garde s'y lit **par un code couleur**, les actions (poser un créneau, ajuster un transfert) se font **en contexte** depuis le calendrier, et les transferts à venir apparaissent dans un panneau d'événements.

---

## 👨‍👩‍👧‍👦 Familles recomposées

Le cas est central, pas une option : un foyer peut mélanger plusieurs enfants
de parents différents.

> *Ex. : Papa A vit avec son enfant A ; Maman B avec ses deux enfants B.
> Chaque enfant a son cycle de garde propre, mais tout le monde partage la même
> vue d'ensemble du foyer.*

C'est pourquoi le **multi-enfants est dans le v1**, et non repoussé plus tard.

---

## 📖 Concepts clés

- **Enfant** — chacun a sa propre organisation de garde (un ou plusieurs par foyer).
- **Lieu** — domicile parent A / B, école, chez la nounou, activité… (liste éditable).
- **Créneau** — la brique de base : un horaire début → fin, avec un responsable, un lieu, une activité et ses transferts.
- **Journée** — une suite de créneaux enchaînés.
  *Ex. : dépose 7h15 chez la nounou → école 8h05 → nounou 11h45 → …*

---

## 🚀 Séquence de livraison

Tous les besoins comptent, mais ils sont **livrés par étapes** : chaque phase
doit être adoptée avant la suivante. **L'usage réel tranche** : si ça n'aide
pas le quotidien, c'est coupé.

1. 🧱 **Mémoire partagée fiable** — une seule source de vérité commune, à la place des SMS éparpillés *(socle)*
2. ⏰ **Immédiat & rappels** — « qui récupère ce soir », où est l'enfant maintenant, rappels de transfert
3. 👪 **Modèle d'acteurs & foyer** — déclarer les vraies personnes du foyer (Admin, Parents, Autres), prérequis de l'ouverture de l'accès
4. 🔁 **Anticipation & cycle** — cycle multi-semaines récurrent, projeté sur un calendrier navigable
5. 🤝 **Imprévu & échange** — gérer malade / retard / échange de dernière minute, avec accord entre parents
6. 🔐 **Ouverture de l'accès** — landing page et connexion des acteurs réels (email via Gmail / Apple / Microsoft)

Plus tard, si le besoin se confirme : permissions fines, notifications push &
email, suivi des heures de la nounou.

---

## 📌 Statut

🚧 **En développement itératif.** Le 1ᵉʳ sprint (semaine de garde : créneaux,
périodes, transferts) est livré côté back + IHM ; les retours d'usage nourrissent
le sprint suivant. La spec est une **documentation vivante**, reversionnée à chaque
itération à partir des retours.

📄 Spec courante : [`docs/07-specification.md`](docs/07-specification.md)
*(les versions précédentes, ex. [`docs/06-specification.md`](docs/06-specification.md), restent figées en historique)*
