# Sc.3 — Renommer vers un nom long : tronqué dans la case, complet au survol et en légende

`@limite` `🖥️ IHM`

↩ Retour : [00-sprint08-suivi.md](00-sprint08-suivi.md)

**Routage** : **100 % runtime IHM** (`ihm-builder`). **Aucun driver backend** : le read model
porte **toujours le nom complet** ; la troncature + le survol vivent dans le `.razor` (livré
**s07 Sc.6**). Ici on **assert** que ce comportement s'applique à un nom **édité** long.

## Acceptation (BDD) — niveau **runtime/IHM** (routé `ihm-builder`)

> Symptôme runtime de **présentation** : après un renommage vers un nom long, la case tronque
> visuellement, le **survol** (attribut natif `title`) restitue le nom complet, et la légende
> affiche le nom complet. bUnit seul ne prouve pas le rendu réel (CSS de troncature, attribut
> natif) — acceptation sur l'app câblée.

`Should_Tronquer_Marie_Helene_Grand_Dubois_dans_la_case_du_16_07_2026_avec_le_nom_complet_au_survol_et_en_legende_When_l_acteur_parent_c_est_renomme_de_Marie_en_un_nom_long_de_25_caracteres` — ✅ GREEN (caractérisation)
(`tests/PlanningDeGarde.Web.Tests/FrontWasmConfigNomLongEditeTempsReelTests.cs`)

- **Niveau** : E2E/runtime sur l'app câblée (réutilise le composant troncature + survol
  livré s07 Sc.6). Store réel : renommage `parent-c` « Marie » → « Marie-Hélène Grand-Dubois ».
- **GREEN sans code neuf** (caractérisation anticipée) : compose la chaîne d'édition (Sc.1,
  store bindé sur `IReferentielResponsables`) et le rendu troncature/survol (s07 Sc.6). Le
  baseline court « Marie » est posé via le **canal d'écriture réel** (Given) — `Foyer` seed
  parent-c **inchangé** (le test s07 `FrontWasmGrilleNomLongLisibleTests` reste vert) ; le test
  observe la transition court→long, donc rouge si l'édition ne propageait pas (pas un faux vert).
- **Observable** : la case du 16/07/2026 affiche le nom tronqué (ex. « Marie-Hél… »), son
  `title` natif porte « Marie-Hélène Grand-Dubois », l'entrée de légende affiche le nom
  **complet**.

## Tests unitaires backend

*(Néant.)* Le read model porte le **nom complet** (champ déjà présent depuis s07) ; renommer
ne fait que muter cette valeur (caractérisation de Sc.1 #1). La troncature/survol est un fait
de **rendu** déjà couvert par le composant s07 Sc.6 → **aucun rouge backend** à attendre,
aucun nouveau test unitaire.

## Fichiers à créer / modifier

- *(Aucun fichier backend.)*
- **Câblage IHM** (routé `ihm-builder`) — le composant case (troncature + `title`) et la
  légende (nom complet) **existent** (s07 Sc.6) ; `ihm-builder` **assert** qu'ils s'appliquent
  à un nom **édité** long, il ne reconstruit pas la troncature.

## Design notes

- **Largement caractérisation au runtime** : le driver troncature+survol est **déjà vert**
  depuis s07. La valeur ajoutée du sprint 08 est seulement que la source du nom long est
  désormais une **édition** (store mutable), pas le seed figé. Si la chaîne d'édition (Sc.1)
  est verte, ce scénario suit mécaniquement.
- **Nom complet jamais tronqué côté donnée** : la troncature est **présentation** ; le read
  model et la légende conservent le nom intégral (anti perte d'information).
