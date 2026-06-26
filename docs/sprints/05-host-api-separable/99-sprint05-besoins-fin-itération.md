# Besoins fin d'itération — Sprint 05 (host-api-separable)

> Sortie de `/4-retours` (agent `retours-challenge`). Backlog priorisé qui **réamorce
> `/2-make-gherkin`** sur **un** sujet. Scope produit uniquement : la partie méthode du
> `99-sprint05-retours.md` (`# Méthode (agents)`, `## IA`, `## Notes de contexte`) relève
> de `retro-sprint` et n'est pas traitée ici.

## État du sprint — rien à corriger côté produit

**Aucun retour produit humain.** La section `# Retours produit (PO)` du
`99-sprint05-retours.md` est restée **vide** : sous-sections `## IHM - général`,
`## IHM - /planning/poser-slot`, `## IHM - /planning` aux placeholders intacts, et
sous-section `## Tech` vide → **bypass Tech tranché par le PO**.

Sprint **structurel**, entièrement **vert** (suivi : 6/6 GREEN, 96 tests, smoke runtime
OK). Le **palier 1 — « Fermeture de la fondation : hôte d'API détachable + UI
d'exploration »** est **CLOS** :

| Acquis | Preuve |
|--------|--------|
| Back démarrable seul (API détachée, ne référence pas le front) | Sc.1 — test d'architecture sur `ProjectReference` + intégration `WebApplicationFactory<ApiProgram>` |
| Front **WASM réel** consommant l'API **distante** | Sc.2 — runtime sur front WASM + API distante (conversion `Sdk.Web → Sdk.BlazorWebAssembly` réalisée) |
| API explorable (OpenAPI natif + Scalar) | Sc.3 / Sc.4 — servabilité HTTP |
| CORS autorisant l'origine du front | Sc.5 |
| Échec clair si API injoignable (message + saisie non appliquée, **sans file ni rejeu**) | Sc.6 — runtime |

**Conséquence directe.** Aucun symptôme rapporté → aucune confrontation au code courant
(HEAD) requise → **AUCUN `bug` ordonné**, **aucune réparation à l'aveugle**. Rien à
corriger ni à faire évoluer sur le **livré** du sprint 05.

## Besoins priorisés

> Un seul devient le prochain `/2-make-gherkin` ; le reste est **séquencé derrière**.
> **Règle d'arbitrage (héritée de la spec) : l'usage réel tranche** — « on garde ce qui
> sert l'usage quotidien et on coupe le reste ». Les sujets techniques restent **sous**
> l'usage.

| Rang | Besoin | Type | Origine | Statut |
|-----:|--------|------|---------|:------:|
| **1 (prochain sujet)** | **Saisie visible** — la saisie réapparaît à la **bonne date** (défaut = aujourd'hui) **et** en **couleur du parent** (identité acteur ↔ palette) | évolution d'usage + défaut confirmé | backlog palier 2 / rang +2 · défaut couleur confirmé s04 | ⬜ prochain `/2` |
| 2 | **PWA — saisie hors-ligne** — cache / file d'écritures côté navigateur, **rejouée** au retour de connexion (au-delà de l'échec clair du Sc.6) | technique différé | backlog hors-séquence v05 · besoins s04 | ⬜ derrière |
| 3 | **Persistance réelle — adaptateurs de droite** — store durable derrière les ports de droite, en remplacement des `InMemory*Repository` (recoupe la config foyer du palier 5) | technique différé | backlog hors-séquence v05 · PO post-s05 | ⬜ derrière |
| 4 | **Conteneurisation Docker** — packager hôte API + front WASM (+ base) en conteneurs montables ensemble (compose) | technique différé | backlog hors-séquence v05 · PO post-s05 | ⬜ derrière |

**Pourquoi « Saisie visible » gagne le rang 1.** C'est le **premier sujet d'usage réel**
après **deux sprints structurels d'affilée** (s04 fondation, s05 hôte API) ; il éteint
directement la dette « faux sentiment de progrès ». L'arbitre spec tranche en sa faveur.

**Pourquoi les rangs 2-4 restent derrière.** PWA, persistance réelle et Docker sont
**débloqués** par la fermeture du palier 1 (hôte API détaché, front WASM autonome) — mais
ils étaient **explicitement « hors séquence v05 »** et seront **positionnés comme paliers
au prochain `/5-consolidation`**. Aucun ne court-circuite la valeur d'usage ; aucun ne lève
le risque mortel d'adoption (cf. risques).

## Prochain sujet pour `/2-make-gherkin`

**Palier 2 — « Saisie visible ».** La saisie d'un slot / d'une période **réapparaît à la
bonne date et dans la bonne couleur** dans la grille.

Deux choses **distinctes** à dérouler (à ne pas confondre au moment du make-gherkin) :

1. **Date par défaut = aujourd'hui** — symptôme rapporté en s03 comme « saisies
   invisibles ». **Faux bug** : la saisie n'est pas perdue, elle tombe **hors de la
   fenêtre stricte 35 jours** faute de date par défaut → besoin d'**ergonomie de saisie**
   (proposer aujourd'hui), pas une réparation de comportement vert cassé.
2. **Couleur du parent (identité acteur ↔ palette)** — **vrai défaut confirmé au sprint
   04** : les affectations s'affichent en **gris** au lieu de la teinte du responsable. Au
   moment du `/2` puis `/3`, ce point devra être **confronté au code courant (HEAD)** et
   le défaut **localisé** (`fichier:lignes`) avant toute réparation — c'est le **seul** des
   deux qui relève d'un défaut à localiser.

## Risques à porter

1. **Faux bug « saisies invisibles » vs vrai défaut « gris » (confirmé s04).** Le palier 2
   mêle un besoin d'ergonomie (date par défaut) et un défaut runtime confirmé (couleur
   acteur). Le prochain `/2-make-gherkin` doit les **séparer** : seul le défaut couleur est
   à confronter à HEAD et à localiser ; la date par défaut est une évolution d'usage.
2. **Pression technique récurrente.** Persistance réelle (recoupe le palier 5) et Docker
   sont tentants maintenant que le palier 1 les débloque et que la base est testable
   bout-en-bout. À **tenir derrière l'usage** : les laisser passer devant ferait un **3e
   sprint sans valeur produit observable**, à l'encontre de l'arbitre « l'usage réel
   tranche ».
3. **Risque mortel — adoption du second parent.** La spec le qualifie de risque mortel « à
   traiter dès le socle », mais l'auth (landing + OAuth) est repoussée au **palier 9**.
   Aucun des sujets ci-dessus ne le lève. À **ne pas laisser glisser** indéfiniment
   derrière la technique.

## Note de cadrage (non-décision, pour `/5-consolidation`)

Une **piste technique** consignée au backlog pour la PWA (rang 2) : *outbox pattern* (file
côté client IndexedDB) comme socle minimal d'une file d'écritures rejouable « exactement
une fois » ; *event sourcing* seulement si offline/rejeu/audit le justifient. **À trancher
au palier PWA**, pas un prérequis du prochain sujet.
