# Sc.9 — Ajout impossible si le service de configuration est injoignable

`@erreur` `🖥️ IHM`

↩ Retour : [00-sprint09-suivi.md](00-sprint09-suivi.md)

**Routage** : **100 % runtime IHM** (`ihm-builder`, driver runtime) — **backend néant**. Le service
de configuration injoignable = **API distante injoignable** : l'échec de **transport** survient
**avant** que le handler ne s'exécute. C'est un fait d'usage **runtime** (front WASM, gestion
d'échec transport), **jamais** un test backend à doublures. Réutilise la gestion d'échec transport
livrée au s08 Sc.9 (`PoserSlot.MessageServiceInjoignable`, saisie conservée).

## Acceptation (BDD) — niveau **runtime/IHM** (routé `ihm-builder`)

> Un parent a saisi « Carla » (rose) dans l'écran de config ; il valide l'ajout **alors que le
> service de configuration est injoignable** ; un **message d'échec clair** s'affiche, la saisie
> « Carla / rose » **reste à l'écran** à resoumettre, et **aucun acteur n'est enregistré**. Sur
> l'app **réellement câblée** (front WASM + API distante) : si le canal HTTP d'ajout échoue au
> transport, l'observable reste « rien d'enregistré, saisie conservée » → rouge si la saisie est
> perdue ou un acteur fantôme créé.

`Should_Afficher_un_message_d_echec_clair_et_conserver_la_saisie_Carla_rose_a_resoumettre_sans_enregistrer_aucun_acteur_When_un_parent_valide_l_ajout_alors_que_le_service_de_configuration_est_injoignable` — ✅ GREEN (runtime)
*(driver runtime — `tests/PlanningDeGarde.Web.Tests`, réutilise l'échec transport s08 Sc.9)*

## Tests unitaires backend (boucle interne, `tdd-auto`)

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| — | *(aucun test backend)* | — | **Aucune contradiction backend** — l'échec est de **transport** (API injoignable) : le handler `AjouterActeur` ne s'exécute jamais. La gestion (message dédié, saisie conservée, aucune écriture) vit **dans l'écran de config front** (catch `HttpRequestException`), déjà éprouvée au s08 Sc.9. Rien à piloter par `tdd-auto`. | — |

## Fichiers à créer / modifier

- **Backend** : néant.
- **Volet runtime IHM (routé `ihm-builder`)** — l'écran de config `ConfigurationFoyer.razor`
  applique au **canal d'ajout** (`POST /api/canal/ajouter-acteur`) la même gestion d'échec transport
  que l'édition s08 : `catch (HttpRequestException)` → message dédié, **saisie conservée**, aucune
  écriture ni mise en file (règle 28).

## Design notes

- **Échec de transport ≠ refus métier.** Sc.8 = refus **métier** (handler, motif « nom vide ») ;
  Sc.9 = échec **transport** (API injoignable, le handler ne tourne pas). Deux chemins distincts —
  ne pas confondre.
- **Saisie conservée, rien d'enregistré** (règle 28) : pas de file ni rejeu à ce stade ; la saisie
  « Carla / rose » reste à l'écran à resoumettre.
- **Réutilisation s08 Sc.9** : même patron d'échec transport déjà livré pour l'édition — le scénario
  est une caractérisation runtime de ce patron appliqué à l'ajout.
- **Robustesse anti-flake Docker (mission s09).** Le driver runtime n'utilise plus un port loopback
  réellement libéré (`ConnectionRefused`), dont la sémantique est altérée par le proxy loopback de
  Docker Desktop (flake environnemental). Un handler de transport déterministe lève
  `HttpRequestException` sur le seul POST d'écriture ciblé (`ClientVersAvecEcritureInjoignable`), tandis
  que l'énumération en lecture transite vers l'API live. Déterministe que Docker tourne ou non —
  l'ancien test d'édition (`FrontWasmConfigApiInjoignableTempsReelTests`), qui flakait, est porté sur ce
  patron et prouvé stable ≥5× Docker en marche.
