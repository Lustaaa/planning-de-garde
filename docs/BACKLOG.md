# Product backlog — planning-de-garde

> **Backlog produit permanent** (artefact SCRUM). Deux lectures du même produit :
> une vue **par épic (fonctionnalité)** pour regrouper ce qui est lié et préparer le
> découpage des sprints, et une vue **par palier (séquence de livraison)** pour le
> calendrier d'un coup d'œil. Source de vérité du *quoi/quand* ; le *pourquoi* vit dans
> la spec vivante [`docs/04-specification.md`](04-specification.md).
>
> **Tenue à jour par le pipeline** : `/4-retours` y **ajoute** les besoins issus du
> challenge ; `/6-cloture-sprint` y passe à **✅ fait** ce qui a été livré (gate visuel
> passé), en renseignant le sprint. Statuts : ✅ fait · 🟡 en cours · ⬜ à faire.
> Origine tracée : `spec` (règle/palier v04), `retours sNN` (retours produit d'un sprint),
> `dette` (dette explicitement signalée).

## Sprints livrés

| Sprint | Sujet | Statut | Livré |
|-------:|-------|:------:|-------|
| 01 | Semaine de garde (grille agenda, cycle récurrent, slots/périodes/transferts) | ✅ fait | Modèle de garde + 12 scénarios domaine + grille initiale |
| 02 | Réparer le câblage IHM ↔ actions (render mode interactif) | ✅ fait | Actions d'écriture câblées au front |
| 03 | Calendrier — grille de lecture (5 semaines, lecture seule, 2 niveaux de couleur) | ✅ fait | Projection `GrilleAgendaQuery` + grille 5×7 lecture seule |

## En cours

| Sprint | Sujet | Palier (spec v04) | Statut |
|-------:|-------|-------------------|:------:|
| 04 | `controllers-wasm-fondation` — adaptateur de gauche (écriture via canal requête/réponse) + migration front côté client (WASM), SignalR conservé en diffusion lecture seule | 1 — Fondations | 🟡 en cours |

---

## Épics (par fonctionnalité)

> Regroupement transverse aux paliers : chaque épic réunit les besoins liés, avec leur
> statut, leur sprint/palier de rattachement et leur origine. Les dépendances entre épics
> sont en bas (« Dépendances »). Cette vue sert à **constituer les prochains sprints** ;
> la vue paliers ci-dessous donne l'ordre de livraison.

### Épic 1 — Fondation données & modèle foyer
*Déclarer et persister les données du foyer (acteurs, lieux, cycle, couleurs) au lieu de les figer dans `Foyer.cs`.*

| Besoin | Statut | Sprint/Palier | Origine |
|--------|:------:|---------------|---------|
| Extraire la config foyer de `Foyer.cs` vers persistance (base) | ⬜ | Palier 4 | retours s03 (#11, dette) · spec p4 |
| Déclaration des enfants du foyer (N enfants, ≥1, organisation propre) | 🟡 | s01 socle + Palier 4 | spec règle 1 |
| Familles recomposées (enfants de parents différents, même planning) | ⬜ | Palier 4-5 | spec règle 2 |
| Deux parents (toujours exactement 2 ; le 1er saisit l'autre) | ⬜ | Palier 5 | retours s01 · spec règle 3 |
| Acteurs « autres » éditables (nounou, grands-parents…) | ⬜ | Palier 5 | spec règle 4 · retours s01 |
| Lieux éditables et persistés (référentiel des sélecteurs) | 🟡 | Palier 4 | spec règle 11 |
| Cycle récurrent multi-semaines persisté et éditable | ⬜ | Palier 4 | spec règles 9/118 |
| Set de couleurs par défaut persisté (acteur → couleur) | 🟡 | s03 statique + Palier 4 | spec règle 15 |

### Épic 2 — Modèle & configuration d'acteurs
*Déclarer les acteurs réels (Admin / Parent / Autre) avec rôles, responsabilités et accès.*

| Besoin | Statut | Sprint/Palier | Origine |
|--------|:------:|---------------|---------|
| Trois types d'acteurs avec rôles distincts (Admin / Parent / Autre) | ⬜ | Palier 5 | retours s01 (#3) · spec règles 6-7 |
| Écran de configuration du foyer (acteurs + cycle de fond + couleurs) | ⬜ | Palier 5 | retours s01 (#7) · spec p5 |
| Édition des acteurs « autres » (ajout/édition/suppression) | ⬜ | Palier 5 | spec règle 4 |
| Affichage/actions adaptés au type d'acteur | ⬜ | Palier 5 | retours s01 (#3) · spec règles 6-7 |

### Épic 3 — Fondations techniques (architecture & API)
*Socle découplé : API exposée, front WASM, conventions de code, swagger.*

| Besoin | Statut | Sprint/Palier | Origine |
|--------|:------:|---------------|---------|
| Controllers HTTP exposant les commandes d'écriture (adaptateur de gauche) | 🟡 | s04 / Palier 1 | retours s03 (#9) · spec p1 |
| Migration front Blazor Server → WASM consommant l'API | 🟡 | s04 / Palier 1 | retours s03 (#6) · spec p1 |
| SignalR cantonné au push lecture seule (jamais d'écriture) | 🟡 | s04 / Palier 1 | retours s03 · spec p1 (séparation canaux) |
| Convention code-behind systématique (`.razor.cs`, pas de `@code` inline) | ⬜ | Palier 1+ (garde-fou) | retours s03 (#7, dette : 7 composants) |
| Swagger / OpenAPI sur le backend | ⬜ | Palier 1 (garde-fou) | retours s03 (#8) |
| Ports & adaptateurs visibles (hexagonal : gauche/droite/domaine) | 🟡 | Palier 1 | retours s03 (#10) |

### Épic 4 — Calendrier & grille de lecture
*Calendrier navigable (semaine + 4 semaines) lisible d'un coup d'œil.*

| Besoin | Statut | Sprint/Palier | Origine |
|--------|:------:|---------------|---------|
| Grille agenda 5 semaines (35 jours) en lecture seule | ✅ | s03 | spec p3 · retours s02 (#3-5) |
| Positionnement des slots dans les cases jour/horaire | ✅ | s03 | spec règles 12/114 |
| Code couleur par personne sur les cases-jour | ✅ | s03 | spec règles 14/158 |
| Slots empilés dans l'ordre horaire | ✅ | s03 | scénario 5 s03 |
| Fenêtre stricte 35 jours (bornes inf./sup.) | ✅ | s03 | scénarios 1/7 s03 |
| Navigation dans le mois (semaines précédente/suivante) | ⬜ | Palier 3 item 2 | spec p3 · retours s02 (#3) |

### Épic 5 — Lisibilité & identité visuelle
*Rendre la responsabilité explicite (pas seulement une teinte) et habiller l'app.*

| Besoin | Statut | Sprint/Palier | Origine |
|--------|:------:|---------------|---------|
| Libellé + nom du responsable dans les cases (pas que la teinte) | ⬜ | Palier 2 (G1) | retours s03 (#3) · spec règle 14 |
| Légende des couleurs persistante (mapping acteur → couleur) | ⬜ | Palier 2 (G1) | spec règle 14 · retours s03 (#3) |
| Thème visuel en accord avec le domaine (garde d'enfants) | ⬜ | Palier 2 (G1, transverse) | retours s01/s02/s03 (« j'aime pas le thème ») |

### Épic 6 — Créneaux & slots de localisation
*Poser et gérer les slots (où est l'enfant) : création, validation, affichage.*

| Besoin | Statut | Sprint/Palier | Origine |
|--------|:------:|---------------|---------|
| Poser un slot (enfant → lieu, début/fin, date) | ✅ | s01 + s02 + s04 (API) | spec règles 8/112 |
| Rejet : durée nulle interdite | ✅ | s01 | scénario 2 s01 |
| Slot franchissant minuit (rendu sur deux jours) | ✅ | s01 | scénario 3 s01 |
| Rejet : lieu inexistant | ✅ | s01 + s02 + s04 (API) | scénario 4 s01 |
| Signalement de chevauchement (création acceptée + avertissement) | ✅ | s01 | scénario 5 s01 |
| Droits : seul Parent crée/édite les slots | ✅ | s01 | spec règle 7 |
| Poser un slot en contexte via dialog (depuis une case) | ⬜ | Palier 3 item 3 | retours s02 (#10) · spec p3 |

### Épic 7 — Périodes de garde & responsabilité récurrente
*Modéliser la responsabilité de garde sur une période (distincte des slots).*

| Besoin | Statut | Sprint/Palier | Origine |
|--------|:------:|---------------|---------|
| Affecter une période à un responsable | ✅ | s01 + s02 + s04 (API) | spec règles 5/8/118 |
| Rejet : responsable requis | ✅ | s01 | scénario 8 s01 |
| Bornes de période paramétrables | ✅ | s01 | scénario 9 s01 |
| Édition concurrente — rejet sur état périmé | ✅ | s01 | scénario 10 s01 |
| Suppression de période (depuis dialog) | ⬜ | Palier 3 item 3 | retours s02 (#6) · retours s03 (trou) |
| Affecter période en contexte via dialog | ⬜ | Palier 3 item 3 | retours s02 (#7) · spec p3 |
| Responsabilité de fond déclarée en config foyer (le cycle) | ⬜ | Palier 4-5 | spec règles 5/118 |

### Épic 8 — Transferts & bascule de responsabilité
*Modéliser les transferts (qui dépose, qui récupère, où, quand) bornant les périodes.*

| Besoin | Statut | Sprint/Palier | Origine |
|--------|:------:|---------------|---------|
| Définir un transfert (date, dépositaire, récupérateur, lieu, heure) | ✅ | s01 + s02 + s04 (API) | spec règles 17-18 |
| Rejet : transfert incomplet | ✅ | s01 | scénario 12 s01 |
| Transfert dérivé automatiquement par défaut (saisie réservée au ponctuel) | ⬜ | Palier 5-6 | spec règle 17 · retours s02 (#14) |
| Transfert ponctuel & modifiable | 🟡 | s01 (modèle) + Palier 5+ | spec règle 18 |
| Transfert en contexte via dialog | ⬜ | Palier 3 item 3 | retours s02 (#8) · spec p3 |
| Transferts exposés dans le panneau cloche | ⬜ | Palier 4 item 6 | spec règle 20 · retours s02 (#8)/s03 (#4) |

### Épic 9 — Notifications & événements à venir
*Exposer transferts, changements et rappels comme événements (panneau cloche).*

| Besoin | Statut | Sprint/Palier | Origine |
|--------|:------:|---------------|---------|
| Panneau cloche listant les événements à venir | ⬜ | Palier 4 item 6 | spec règles 20/120 · retours s02 (#8)/s03 (#4) |
| Transferts listés comme événements (date, acteurs, lieu, heure) | ⬜ | Palier 4 item 6 | spec règle 20 |
| Changements de planning exposés comme événements | ⬜ | Palier 4 item 6 | spec règle 20 |
| Notifications in-app push temps réel (SignalR, lecture seule) | ✅ | s01 (infra) | spec règles 19-20 |
| « Qui récupère ce soir » — immédiat (qui-quand-où du jour) | ⬜ | Palier 4 item 6 | spec p4 · spec v03 incrément 2 |

### Épic 10 — Authentification & accès utilisateurs
*Authentifier les acteurs réels, lever le risque d'adoption du second parent, ouvrir l'accès.*

| Besoin | Statut | Sprint/Palier | Origine |
|--------|:------:|---------------|---------|
| Landing page (identifie le foyer, invite à s'authentifier) | ⬜ | Palier 8 | retours s01 (#2) · spec p8 |
| Authentification OAuth (Gmail / Apple / Microsoft) | ⬜ | Palier 8 | retours s01 (#2) · spec p8 |
| Gestion des sessions utilisateur (persistance, logout) | ⬜ | Palier 8 | spec p8 |
| Droits d'accès par utilisateur identifié (selon rôle) | ⬜ | Palier 5 + 8 | spec règles 6-7 |
| Personnalisation des couleurs par utilisateur authentifié | ⬜ | Palier 8 | spec règle 16 |

### Épic 11 — Imprévu & échange
*Gérer les exceptions : malade, retard, échange de dernière minute avec accord.*

| Besoin | Statut | Sprint/Palier | Origine |
|--------|:------:|---------------|---------|
| Signalement d'imprévu (malade, retard…) + notification immédiate | ⬜ | Palier 7 | spec p7 |
| Échange de dernière minute (proposition + accord requis) | ⬜ | Palier 7 | spec p7 |
| Transferts temporaires (exception, non récurrents) | ⬜ | Palier 7 | spec règles 17-18 |

### Épic 12 — Écriture en contexte (recâblage post-API)
*Faire passer les saisies par le canal requête/réponse et vérifier leur réapparition.*

| Besoin | Statut | Sprint/Palier | Origine |
|--------|:------:|---------------|---------|
| Dialogs d'écriture (poser/affecter/supprimer) depuis les cases | ⬜ | Palier 3 item 3 | retours s02 (#7/8/10)/s03 |
| Recâblage de l'écriture via API HTTP (au lieu du DI direct) | 🟡 | s04 / Palier 1 | retours s03 (#5) · spec p1 |
| Rafraîchissement immédiat : la saisie réapparaît dans la grille | ⬜ | Groupe 2 (dép. Palier 1) | retours s03 (#5, bug runtime) |

---

## À faire (paliers de la spec vivante v04)

> Vue de séquencement (ordre de livraison). Chaque palier agrège des besoins des épics.

| Palier | Besoin | Épics concernés | Origine | Statut |
|-------:|--------|-----------------|---------|:------:|
| 2 | Lisibilité des périodes/responsable (libellé/nom + légende) **+** thème en accord avec le domaine | É5 | retours s03 (G1) | ⬜ |
| 3.2 | Navigation calendrier (semaines précédente/suivante) | É4 | spec p3 · retours s02 | ⬜ |
| 3.3 | Écriture en contexte — dialogs depuis les cases + saisies réapparaissent dans la grille | É6, É7, É8, É12 | retours s03 (G2) | ⬜ |
| 4 | Persistance config foyer (sortir le dur de `Foyer.cs`) + cycle de fond + lieux/couleurs | É1 | retours s03 (#11, dette) | ⬜ |
| 4.6 | Immédiat & événements à venir — panneau cloche (transferts + changements) | É8, É9 | spec règle 20 · retours s02/s03 | ⬜ |
| 5 | Modèle d'acteurs & foyer — Admin/Parent/Autre, écran de config, responsabilité de fond, couleurs par défaut | É1, É2, É7 | spec (incrément 6) · retours s01 | ⬜ |
| 6 | Transferts dérivés automatiquement par défaut | É8 | spec règle 17 · retours s02 | ⬜ |
| 7 | Imprévu & échange | É11 | spec (incrément historique) | ⬜ |
| 8 | Ouverture de l'accès (auth OAuth, landing, personnalisation des couleurs) | É10, É5 | spec (incrément 7) · retours s01 | ⬜ |

## Dépendances entre épics (pour la découpe des sprints)

- **É3 (Fondations API) → É12 (Écriture en contexte)** : controllers opérationnels avant de recâbler les dialogs.
- **É1 (Config foyer) → É2 (Modèle d'acteurs)** : déclarer les acteurs requiert la persistance.
- **É4 (Calendrier) + É5 (Lisibilité)** traités ensemble (Groupe 1) : la lisibilité enrichit le calendrier déjà livré.
- **É12 (Écriture) → É9 (Cloche)** : les événements apparaissent après que les écritures soient observables.
- **É10 (Auth) → personnalisation couleurs d'É5** : requiert l'identification.
- **É7 (Périodes) + É8 (Transferts) + É9 (Cloche)** forment un bloc « responsabilité + événements ».
- **É11 (Imprévu)** vient en dernier, paliers 1-6 stabilisés.

## Garde-fous structurels (non-paliers, hors observable métier)

> Invariants de structure portés au fil de l'eau, sans scénario codant dédié.

- Convention code-behind systématique (`.razor` + `.razor.cs`, pas de `@code` inline) — sprint 04+.
- API explorable (swagger/OpenAPI) — accompagne les controllers du palier 1.
- Séparation des canaux : écriture = requête/réponse ; diffusion temps réel = lecture seule (jamais d'écriture par la diffusion).

## Dettes explicitement signalées

- Données en dur dans `Foyer.cs` (É1) — persister en base — retours s03 (#11).
- Aucune édition/suppression de période depuis l'IHM (É7) — « trou fonctionnel assumé » — retours s03.
- Saisies invisibles à l'écran (É12) — bug runtime à requalifier après recâblage — retours s03 (#5).
- Risque d'adoption du second parent (É10) — repoussé au palier 8, « ne pas laisser glisser ».
- Faux sentiment de progrès — sprints fondation/grille n'avancent aucun besoin produit observable.
- 7 composants encore en `@code` inline (É3) — retours s03 (#7).
- Cycle multi-semaines non affiché/éditable (É1) — modèle existe, IHM absente.
