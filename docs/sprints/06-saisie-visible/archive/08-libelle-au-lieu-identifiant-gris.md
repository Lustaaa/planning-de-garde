# Scénario 8 — Libellé fourni à la place de l'identifiant fait retomber sur gris

`@erreur` `@vert` · 🖥️ **scénario IHM** — **Routé vers `ihm-builder`**

[← Retour au suivi](00-sprint06-suivi.md)

> **Axe : IHM / runtime (diagnostic du défaut B).** Ce scénario **prouve la cause** du gris-bug :
> quand la **source** (sélecteur + seed) fournit le **libellé** « Parent A » comme
> `ResponsableId` au lieu de l'**identifiant stable** `parent-a`, la case retombe sur **gris**
> **alors qu'un responsable y est affecté**. C'est un **fait d'usage runtime** localisé dans
> l'adaptateur de gauche (la source des responsables), **pas** dans la projection : au niveau
> `IPaletteCouleurs.CouleurDe`, un id absent renvoie **déjà** gris (contrat correct). Le driver
> réel est donc la **source qui envoie le mauvais identifiant** au canal. **JAMAIS** un bUnit à
> doublures comme preuve : il stub le transport et ne verrait pas que le canal réel reçoit
> « Parent A ».
>
> **Niveau d'acceptation : E2E / runtime** sur l'app réellement câblée (front WASM réel + API
> distante, palette réelle). C'est le **pendant négatif** du Sc.6 : avant la correction de la
> source, l'affectation retombe au gris ; après (Sc.6), elle est colorée. RED→GREEN piloté par
> `ihm-builder`.

## Acceptation (BDD)

`Should_Laisser_grise_la_case_du_24_06_2026_alors_qu_un_responsable_y_est_affecte_When_la_source_du_front_envoie_le_libelle_Parent_A_au_lieu_de_l_identifiant_stable_parent_a` — ✅ Passing
(`tests/PlanningDeGarde.Web.Tests/FrontWasmLibelleAuLieuIdGrisTests.cs` — caractérisation runtime / diagnostic, pendant négatif du Sc.6)

**Test de NIVEAU RUNTIME** sur l'app réellement câblée (palette réelle `parent-a→bleu` ; source
fournissant le **libellé** « Parent A ») — caractérisation **diagnostic** du défaut B :
- **Given** le set de couleurs associe **parent-a au bleu** ; une période est **réellement
  affectée** au 24/06/2026 avec le responsable « Parent A » ; « Parent A » est le **libellé
  d'affichage**, **non** l'identifiant stable `parent-a` ;
- **When** la grille est projetée ;
- **Then** la **case du 24/06/2026 est grise** **alors qu'un responsable y est affecté** ; **et**
  ce gris **trahit un libellé fourni à la place de l'identifiant stable** (la période existe, mais
  son `ResponsableId` n'est pas une clé du set).

> Lien avec Sc.6 : c'est exactement ce gris-bug que la **correction de la source** (Sc.6) élimine
> en bindant/semant `parent-a`. Le Sc.8 le **caractérise** pour qu'il ne réapparaisse pas.

## Tests

> Détail RED→GREEN piloté par `ihm-builder` (la même correction de source que Sc.6 supprime ce
> gris-bug). Boucle externe = acceptation runtime ci-dessus. **Aucune table de tests unitaires
> backend** : la résolution gris d'un id absent est **déjà verte** (contrat
> `IPaletteCouleurs.CouleurDe`) ; ce scénario prouve, **au niveau runtime / source**, que c'est
> le **libellé fourni** qui provoque le gris — pas un défaut de projection.

## Fichiers à créer / modifier

- Même périmètre que **Sc.6** : `src/PlanningDeGarde.Web/Foyer.cs`,
  `src/PlanningDeGarde.Infrastructure/Foyer.cs`, `AffecterPeriode.razor`,
  `src/PlanningDeGarde.Api/SeedDonneesDemo.cs` (source → identifiant stable).
- Le test runtime de ce scénario **caractérise l'état avant correction** (libellé → gris) et
  documente l'invariant « la source doit fournir l'id stable » — réutilise le harnais runtime du
  Sc.6 (front WASM réel + API distante).

## Design notes

- **Diagnostic, pas règle neuve** : la projection est correcte (gris sur id absent). Le défaut
  est la **source** ; sa correction est portée par Sc.6. Ce scénario **distingue** le gris-bug
  (libellé fourni) du **gris assumé** (acteur légitimement hors set, Sc.7).
- **Anti « vert qui ment »** : observer le **canal réel** (qui reçoit `ResponsableId`) et la
  **palette réelle**, pas une doublure — un bUnit ne verrait pas l'identifiant transmis.
- **Gris assumé ≠ gris-bug** : même couleur (`gris`), causes opposées. Sc.7 = conforme (règle
  17) ; Sc.8 = défaut de source corrigé par Sc.6.
- **Projection inchangée** : aucun changement dans `GrilleAgendaQuery` / `IPaletteCouleurs` /
  `CouleursParActeur`.
