# Product backlog — planning-de-garde

> **Backlog produit permanent** (artefact SCRUM). Vue cumulée **fait / en cours / à faire**
> avec le sprint de rattachement, pour planifier les prochains sprints d'un coup d'œil.
> Source de vérité du *quoi/quand* ; le *pourquoi* vit dans la spec vivante
> [`docs/04-specification.md`](04-specification.md) (séquence de livraison en paliers).
>
> **Tenue à jour par le pipeline** : `/4-retours` y **ajoute** les besoins issus du
> challenge ; `/6-cloture-sprint` y passe à **✅ fait** ce qui a été livré (gate visuel
> passé), en renseignant le sprint. Statuts : ✅ fait · 🟡 en cours · ⬜ à faire.

## Sprints livrés

| Sprint | Sujet | Statut | Livré |
|-------:|-------|:------:|-------|
| 01 | Semaine de garde (grille agenda, cycle récurrent, slots/périodes) | ✅ fait | Modèle de garde + grille initiale |
| 02 | Réparer le câblage IHM ↔ actions (render mode interactif) | ✅ fait | Actions d'écriture câblées au front |
| 03 | Calendrier — grille de lecture (5 semaines, lecture seule, 2 niveaux de couleur) | ✅ fait | Projection `GrilleAgendaQuery` + grille 5×7 lecture seule |

## En cours

| Sprint | Sujet | Palier (spec v04) | Statut |
|-------:|-------|-------------------|:------:|
| 04 | `controllers-wasm-fondation` — adaptateur de gauche (écriture via canal requête/réponse) + migration front côté client (WASM), SignalR conservé en diffusion lecture seule | 1 — Fondations | 🟡 en cours |

## À faire (paliers de la spec vivante v04)

| Palier | Besoin | Origine | Statut |
|-------:|--------|---------|:------:|
| 2 | Lisibilité des périodes/responsable (libellé/nom + légende, pas seulement la teinte) **+** thème en accord avec le domaine | retours sprint 03 (G1) | ⬜ à faire |
| 3 | Écriture en contexte — les saisies réapparaissent dans la grille (bug runtime requalifié, recâblé via le canal d'écriture, repro runtime) | retours sprint 03 (G2) | ⬜ à faire |
| 4 | Alimentation & saisie des utilisateurs — **persistance de la config foyer** (sortir les données en dur de `Foyer.cs`) | retours sprint 03 (#11, dette signalée) | ⬜ à faire |
| 5 | Modèle d'acteurs & foyer — déclaration des acteurs réels, responsabilité récurrente de fond, set de couleurs par défaut | spec (incrément historique 6) | ⬜ à faire |
| 6 | Immédiat & événements à venir — panneau cloche exposant **transferts** et changements à venir | spec (règle 20, transferts non exposés) | ⬜ à faire |
| 7 | Imprévu & échange | spec (incrément historique) | ⬜ à faire |
| 8 | Ouverture de l'accès (authentification, personnalisation des couleurs par utilisateur) | spec (incrément historique 7) | ⬜ à faire |

## Garde-fous structurels (non-paliers, hors observable métier)

> Invariants de structure portés au fil de l'eau, sans scénario codant dédié.

- Convention code-behind systématique (`.razor` + `.razor.cs`, pas de `@code` inline) — sprint 04+.
- API explorable (swagger/OpenAPI) — accompagne les controllers du palier 1.
- Séparation des canaux : écriture = requête/réponse ; diffusion temps réel = lecture seule (jamais d'écriture par la diffusion).
