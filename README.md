# 🗓️ Planning de garde

> **Savoir où est l'enfant et qui en est responsable, à tout moment.**

Une app simple pour planifier et partager les semaines de garde entre parents
et intervenants (nounou, grands-parents…), sans tableur partagé ni groupe de
messages qui déborde.

---

## 😩 Le problème

En garde alternée — ou simplement quand plusieurs personnes s'occupent d'un
enfant — la même question revient sans cesse :

> *« C'est qui qui récupère ce soir ? Il est où là, à l'école ou chez la
> nounou ? »*

Les plannings vivent dans des têtes, des SMS et des tableurs jamais à jour.
Résultat : oublis, double-réservation, transferts ratés.

## ✅ La solution

Un planning **partagé et toujours à jour** où chacun voit, en un coup d'œil :

- **où** se trouve l'enfant maintenant,
- **qui** en est responsable,
- **quand** ont lieu les prochains transferts (qui dépose, qui récupère).

---

## ✨ Fonctionnalités (v1)

| | Fonctionnalité |
|---|---|
| 👨‍👩‍👧 | **2 rôles simples** — *Parent* (gère tout) et *Invité* (consultation seule) |
| 🧩 | **Créneaux de garde** — responsable, lieu, activité, et transferts (dépose / récupère) |
| 🔁 | **Récurrence** — cycle multi-semaines (semaine paire / impaire) qui se répète tout seul |
| ⚡ | **Exceptions ponctuelles** — surcharger un jour (vacances, imprévu) sans casser le cycle |
| ✏️ | **Modification directe** — un parent change le planning, les autres sont notifiés |
| 🔔 | **Notifications in-app** — changements récents et rappels de transfert |

### Les 3 vues

- **📅 Semaine** *(principale)* — tous les créneaux, responsables, lieux et transferts de la semaine.
- **📍 Aujourd'hui** — où est l'enfant à l'instant, et les prochains transferts.
- **📥 À traiter** — changements récents à voir.

---

## 📖 Concepts clés

- **Enfant** — un seul en v1 (le modèle est prévu pour en accueillir plusieurs plus tard).
- **Lieu** — domicile parent A / B, école, chez la nounou, activité… (liste éditable).
- **Créneau** — la brique de base : un horaire début → fin, avec un responsable, un lieu, une activité et ses transferts.
- **Journée** — une suite de créneaux enchaînés.
  *Ex. : dépose 7h15 chez la nounou → école 8h05 → nounou 11h45 → …*

---

## 🚀 Roadmap

Le v1 reste volontairement simple. La suite, si le besoin se confirme :

- 👧👦 **Plusieurs enfants** par foyer
- 🔐 **Permissions fines** (proposer / modifier / valider, par personne)
- 🤝 **Workflow d'échange** — proposer un changement, faire valider avant application
- 📧 **Notifications push & email** en plus de l'in-app
- ⏱️ **Suivi des heures** et paiement de la nounou

---

## 📌 Statut

🚧 **En conception.** La spécification fonctionnelle est figée ; le
développement n'a pas encore commencé.

📄 Spec détaillée : [`docs/superpowers/specs/2026-06-22-planning-de-garde-design.md`](docs/superpowers/specs/2026-06-22-planning-de-garde-design.md)
