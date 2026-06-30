# Fondations — back découplé en API

> Sujet **migré** depuis `docs/15-specification.md` (palier 1 + règles 28/29/30) à la migration
> complète des specs. Source de vérité pour le **socle technique** : canal requête/réponse, hôte d'API
> détaché, front WASM, CORS, OpenAPI. Édité en diff, jamais réécrit en bloc.

## Contexte

Le hub repose sur un **back découplé du front** : l'application expose ses commandes et ses lectures à
travers une **API**, ce qui en fait à la fois un produit utilisable et une **vitrine** ouverte à
d'autres clients (front navigateur, IHM tierce, agents). Le découplage va jusqu'à un **hôte d'API
détachable** : le back **démarre seul**, sans le front, et expose son canal d'écriture à n'importe quel
client. **Fondation posée** : l'hôte d'API tourne détaché, le front s'exécute **dans le navigateur**
(WebAssembly) et consomme l'API comme une **API distante**, ouverte et explorable.

## Objectif & arbitrage

**Exception bornée de fondation — refermée.** Ce socle a primé **ponctuellement** sur l'usage immédiat
en début de projet (coût minimal alors, qui explose une fois l'app grosse). La fenêtre est **close** :
l'arbitre d'usage a repris la main dès « saisie visible » et ne la rend plus ; toute nouvelle fondation
technique passe désormais **derrière l'usage**. Détail :
[`objectif-et-arbitrage.md`](objectif-et-arbitrage.md).

## Séquence

**Palier 1 — REFERMÉ.** Les commandes d'écriture (poser un slot, affecter / supprimer une période,
ajuster un transfert) sont confiées à un **canal requête/réponse** côté serveur. L'**hôte d'API est
détaché** (le back démarre seul, sans référencer le front) et le front WASM le consomme comme une **API
distante**. L'API est **explorable** (OpenAPI + UI interactive) et autorise l'**origine du front**
(CORS) ; une API injoignable produit un **échec clair** (message à l'écran, saisie non appliquée, sans
file ni rejeu). Texte complet : [`sequence-de-livraison.md` § palier 1](sequence-de-livraison.md).

## Mécaniques

- **Front découplé du back par une API** : le front n'appelle jamais le domaine en direct ; il émet ses
  commandes et lit ses données à travers l'API. Hôte d'API détaché, front WASM, API distante.
- **Toute écriture passe par le canal requête/réponse** : aucune vue n'écrit le domaine en direct.
- **Deux canaux distincts, jamais confondus** : **écriture** = canal requête/réponse (la réponse porte
  l'issue de sa propre écriture, p. ex. avertissement de chevauchement) ; **diffusion temps réel** =
  canal de diffusion **lecture seule** (SignalR). On **n'écrit jamais** par la diffusion : une écriture
  aboutie **déclenche** la diffusion.
- **Échec clair si l'API distante est injoignable** : message à l'écran, saisie non appliquée et
  conservée à resoumettre ; aucune mise en file ni rejeu à ce stade.
- **API explorable** : document OpenAPI + UI interactive (Swagger-UI / Scalar), origine du front
  autorisée par CORS.

*Texte complet des mécaniques transverses :* [`mecaniques-de-base.md`](mecaniques-de-base.md).

## Règles de gestion (catalogue : `regles-de-gestion.md`)

- **R28 — Écriture par le canal, échec clair si l'API est injoignable** (écriture sous identité réelle,
  réponse porte l'issue, dialog reste ouverte sur échec ; vaut aussi pour la suppression d'acteur).
- **R29 — API explorable et origine du front autorisée** (OpenAPI + UI, CORS ; garde-fou d'outillage
  sans observable métier).
- **R30 — Données derrière les ports, durables — exception bornée pour la config foyer** (durabilité,
  borne anti-cliquet ; distincte de l'édition R5).

## Risques

- **Contraintes du découplage front/API distant** (échanges inter-domaines, sérialisation, URL d'API,
  future auth), accentuées par l'hôte détaché et le front WASM.
- **Empaquetage en conteneurs** (hôte API + front WASM + store, façon compose) = garde-fou d'outillage
  sans observable métier (cf. [`sequence-de-livraison.md` § Garde-fous hors-spec](sequence-de-livraison.md)).

Cf. [`risques-et-questions-ouvertes.md`](risques-et-questions-ouvertes.md).
