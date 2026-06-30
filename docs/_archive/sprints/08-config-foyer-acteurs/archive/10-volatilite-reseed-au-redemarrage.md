# Sc.10 — Volatilité : après redémarrage, le seed d'origine réapparaît

`@limite`

↩ Retour : [00-sprint08-suivi.md](00-sprint08-suivi.md)

**Routage** : **caractérisation backend** (`tdd-auto`, ⚠️ early green attendu sur le store
re-seedé). Niveau **store / construction** : « le serveur redémarre » = la mémoire partagée
est **reconstruite**, donc **re-seedée** depuis `Foyer`. Pas de driver runtime (le redémarrage
n'est pas un geste d'IHM) ; dette volatile **assumée** (à éteindre au palier 13).

## Acceptation (BDD) — niveau **store / construction** (boucle interne)

> L'observable de volatilité se mesure à la **(re)construction** du store : un store
> fraîchement construit restitue le **seed d'origine** (`Foyer`), perdant les éditions de la
> session précédente. Inutile (et coûteux) de simuler un vrai redémarrage serveur en runtime :
> reconstruire le store **est** le redémarrage.

`Should_Restituer_le_seed_d_origine_Bruno_en_orange_en_perdant_les_editions_de_session_When_le_store_de_configuration_est_reconstruit_apres_avoir_ete_edite`

- **Niveau** : unitaire backend (store en mémoire). Édite `parent-b` (« Bruno » → « Bruno
  Martin », orange → violet), **reconstruit** le store, vérifie le retour au seed.
- **Observable** : après reconstruction, `NomDe("parent-b") == "Bruno"` et
  `CouleurDe("parent-b") == orange` (seed `Foyer`) — l'édition volatile est perdue.

## Tests unitaires backend (boucle interne, `tdd-auto`)

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| 1 | `Should_Restituer_le_nom_et_la_couleur_d_origine_du_seed_en_perdant_les_editions_de_la_session_precedente_When_le_store_de_configuration_est_reconstruit` | — (caractérisation du seed-à-la-construction) | ⚠️ **probablement early green — couvert par Sc.1 #1 (caractérisation, pas driver)** : le store **seede depuis `Foyer` à la construction** (nécessaire pour lire « Alice » au départ, Sc.1) ; une nouvelle instance **ne porte aucune édition** → seed d'origine **par construction**. Aucun rouge — filet documentant la **volatilité assumée**. | ✅ GREEN (caractérisation) |

## Fichiers à créer / modifier

- *(Aucun fichier backend nouveau.)* Caractérisation : (re)construction du store → seed
  `Foyer`, éditions perdues.

## Design notes

- **Volatilité = re-seed à la (re)construction** (décision CP) : le singleton est reconstruit
  au redémarrage du serveur → seed `Foyer`. Aucune persistance n'est tirée en avant
  (corollaire « éditable ≠ durable »).
- **Dette assumée et transitoire** : à éteindre au **palier 13** (persistance réelle), où le
  store mutable sera remplacé par un adaptateur durable **sans toucher au domaine** (litmus
  infra remplaçable).
- **Scénario sans driver réel** : la volatilité tombe de la construction seedée du store.
  Vert au 1er passage = **attendu** (`✅ GREEN (caractérisation)`).
