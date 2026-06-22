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
| 👤 | **2 rôles simples** — *Parent* (gère tout) et *Invité* (consultation seule) |
| 🧩 | **Créneaux de garde** — responsable, lieu, activité, et transferts (dépose / récupère) |
| 🔁 | **Récurrence** — cycle multi-semaines (semaine paire / impaire) qui se répète tout seul |
| ⚡ | **Exceptions ponctuelles** — surcharger un jour (vacances, imprévu) sans casser le cycle |
| ✏️ | **Modification directe** — un parent change le planning, les autres sont notifiés |
| 🔔 | **Notifications in-app** — changements récents et rappels de transfert |

### Les 3 vues

- **📅 Semaine** *(principale)* — tous les créneaux, responsables, lieux et transferts de la semaine.
- **📍 Aujourd'hui** — où est chaque enfant à l'instant, et les prochains transferts.
- **📥 À traiter** — changements récents à voir.

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

## 🚀 Roadmap

Le v1 reste volontairement simple. La suite, si le besoin se confirme :

- 🔐 **Permissions fines** (proposer / modifier / valider, par personne)
- 🤝 **Workflow d'échange** — proposer un changement, faire valider avant application
- 📧 **Notifications push & email** en plus de l'in-app
- ⏱️ **Suivi des heures** et paiement de la nounou

---

## 📌 Statut

🚧 **En conception.** La spécification fonctionnelle est figée ; le
développement n'a pas encore commencé.

📄 Spec consolidée : [`docs/init/01-specification.md`](docs/init/01-specification.md)
