# Besoins priorisés — saisie visible (palier 2)

> Source : `99-sprint06-retours.md` (section `# Retours produit (PO)`) · produit par `/4-retours` (retours-challenge).
> Réamorce `/2-make-gherkin` sur le **sujet prioritaire** ci-dessous.

## Classification des retours

> La section `# Retours produit (PO)` du sprint 06 est **entièrement vide/template** :
> aucune puce IHM remplie (général, `/planning/poser-slot`, `/planning/affecter-periode`,
> `/planning/definir-transfert`, `/planning`), `## Notes de contexte` vide, `## Tech` =
> ligne template seule. Le PO **confirme n'avoir aucun retour** sur le livré : sprint 06
> (saisie visible) clos à **8/8 scénarios verts**, palier 2 refermé. **Aucun symptôme
> rapporté → aucun `bug` à confronter au code courant (HEAD), aucune réparation à
> ordonner.** Le prochain sujet sort donc de la **séquence v06**, pas d'un défaut.

| # | Retour (résumé) | Type | Besoin sous-jacent | Zone IHM/Tech |
|---|---|---|---|---|
| 1 | Aucun retour produit déposé (toutes les puces IHM vides, Notes de contexte vide) | *aucun retour* | Aucun défaut ni évolution sur le livré ; le séquencement spec reprend la main | IHM (toutes zones) |
| 2 | `## Tech` non remplie (ligne template seule) | *tech vide* | Aucune contrainte technique remontée par le PO | Tech |

> **Note `bug` (anti-règle confrontation HEAD)** : aucun item classé `bug`, donc aucune
> confrontation au code courant requise — il n'y a pas de symptôme observé à localiser.
> `hasTech=true` mais le contenu Tech est vide → traité comme **bypass de fait** (aucune
> contrainte à injecter).

## Arbitrage

- **Objectif de l'itération** — Le sprint 06 (saisie visible) n'appelle aucune réparation
  ni évolution corrective. L'objectif est de **désigner le prochain incrément d'usage** de
  la séquence v06, en gardant la main à l'usage après deux sprints structurels (s04, s05)
  refermés par le palier 2.
- **Arbitre (départage)** — **L'usage réel tranche** (arbitre permanent, spec v06). Entre
  un palier d'usage et un palier technique débloqué, **l'usage gagne** : remonter la
  technique (persistance 10, PWA 11, Docker) devant l'usage ferait un **3ᵉ sprint sans
  valeur produit observable**, contre l'arbitre. Au sein du palier 3, le PO a tranché
  **« en bloc »** (lisibilité + thème indissociables) plutôt que « lisibilité d'abord ».
  Garde-fou de secours conservé : si le périmètre déborde au make-gherkin, le **corollaire
  de découpe v06** reprend la main (couper au plus petit incrément lisible, séquencer le
  thème derrière) — la lisibilité porte l'observable d'usage (règle 16), le thème reste
  ergonomie de surface subordonnée à l'usage.

## Séquence de livraison

| Rang | Besoin | Type | Sujet make-gherkin | Dépend de |
|---|---|---|---|---|
| 1 | **Lisibilité des périodes/responsable + thème** — nom du responsable + légende couleur dans les cases (la teinte seule ne dit pas qui garde, règle 16) **et** thème en accord avec le domaine (garde d'enfants) | évolution | `lisibilite-theme` | palier 2 (livré ✅) |
| 2 | Calendrier navigable (passé/futur, vues prédéfinies) + écriture en contexte (dialogs depuis les cases) | évolution | `calendrier-navigable` | rang 1 |
| 3 | Alimentation & saisie — persistance config foyer (sortir le dur de `Foyer.cs`) + cycle de fond + lieux/couleurs | nouveau besoin | `alimentation-config-foyer` | rang 2 |
| … | *(paliers techniques en queue)* Persistance réelle (10), PWA (11), garde-fou Docker | nouveau besoin | — | tout l'usage |

> Les paliers techniques **10 (persistance réelle)**, **11 (PWA)** et le garde-fou
> **Docker** restent **en queue de séquence, derrière tout l'usage** : l'arbitre interdit
> de les remonter.

## Prochain sujet → make-gherkin

- **Sujet** : `lisibilite-theme` — Lisibilité des périodes/responsable + thème métier (palier 3, **en bloc**)
- **Périmètre** : rendre la **responsabilité de période explicite** dans la grille — afficher
  le **nom du responsable** et une **légende couleur**, pas seulement la teinte de fond
  (règle 16 : la couleur seule ne dit pas qui garde) — **et** habiller l'app d'un **thème en
  accord avec le domaine** (garde d'enfants). Les deux traités comme **un seul sujet**
  make-gherkin (choix PO « en bloc »).
- **Hors périmètre (reporté)** : navigation passé/futur et vues prédéfinies (palier 4) ;
  écriture en contexte par dialogs depuis les cases (palier 4) ; persistance config foyer
  (palier 5) ; paliers techniques 10/11 et Docker. Les deux **révisions de règle en
  attente** (workflow demande/accord = révision règle 22 ; interdiction/dédoublonnage de
  slot = révision règle 14) restent **hors boucle**, rattachées à leur palier porteur.

## Risques & questions encore ouvertes

- **Débordement de périmètre du choix « en bloc »** — nom + légende + thème dans un seul
  sujet peut gonfler. Activer le **corollaire de découpe v06** au make-gherkin si besoin :
  couper au plus petit incrément lisible, thème séquencé derrière. Arbitrage de découpe à
  rendre au PO si elle survient.
- **Thème = ergonomie de surface, pas une régression** — « le thème est dégueulasse » est
  une **absence de feature** (palier non encore fait), à traiter comme évolution, jamais
  comme bug.
- **Lisibilité ≠ couleur déjà livrée** — la couleur (identifiant stable → palette) **fonctionne**
  depuis le palier 2 ; ce qui manque c'est le **nom + la légende** qui disent qui garde.
  Ne pas confondre.
- **Paliers techniques en queue à ne PAS remonter** — persistance (10), PWA (11), Docker
  restent derrière l'usage. Les laisser passer devant = 3ᵉ sprint sans valeur observable,
  contre l'arbitre.
- **Révisions de règle hors boucle** — workflow demande/accord (révision règle 22 → palier
  imprévu/échange) et interdiction/dédoublonnage de slot (révision règle 14) : ne pas les
  injecter dans le sujet palier 3.
- **Risque d'adoption du second parent (mortel)** — repoussé au palier 9 (auth) ; aucun
  palier technique ne le lève. À ne pas laisser glisser indéfiniment.
