# Besoins priorisés — IHM Blazor du planning partagé (semaine de garde)

> Source : `13-retours.md` · produit par `/4-retours` (retours-challenge).
> Réamorce `/2-make-gherkin` sur le **sujet prioritaire** ci-dessous.

## Classification des retours

| # | Retour (résumé) | Type | Besoin sous-jacent | Zone IHM/Tech |
|---|---|---|---|---|
| 1 | « J'aime pas le thème » — un thème en accord avec le sujet (garde d'enfants) | évolution | Une identité visuelle cohérente avec le domaine, agréable à l'usage | IHM - général |
| 2 | Landing page + connexion email (Gmail / Apple / Microsoft) | nouveau besoin | S'authentifier et identifier les utilisateurs réels via fournisseurs OAuth, avec une page d'accueil | IHM - général |
| 3 | Le changement de rôle ne change rien en admin ; distinguer Admin / Parent / Autre (tester toujours en admin) | nouveau besoin | Un modèle de 3 types d'utilisateurs (Admin, Parent, Autre) avec un affichage adapté au type ; la spec ne connaît que Parent/Invité | IHM - /planning |
| 4 | Dropdown du tableau « Localisation — slots de Léa » incompris ; vision attendue = calendrier semaine + 4 semaines, navigation type agenda | évolution | Visualiser les slots dans un calendrier navigable (semaine + 4 semaines) plutôt qu'un tableau à dropdown | IHM - /planning |
| 5 | Tableau « Responsabilité — périodes de garde » incompris ; voudrait un code couleur dans le calendrier | évolution | Restituer la responsabilité par garde via un code couleur dans le calendrier, pas un tableau séparé | IHM - /planning |
| 6 | Boutons « Modifier » des périodes ne font rien | bug | Le bouton Modifier d'une période déclenche bien l'édition (ModifierPeriodeHandler câblé, scénarios 9/10 verts) — régression IHM | IHM - /planning |
| 7 | Ce tableau devrait donner une page de paramétrage des parents ; l'admin configure 2 parents (≥1 enfant, toujours 2 parents dont un saisi par l'autre, N acteurs « autres » éditables) | nouveau besoin | Un écran d'administration du foyer (enfants, 2 parents, N acteurs autres) définissant le modèle d'acteurs — pan structurant absent de la spec | IHM - /planning |
| 8 | Tableau « Transferts de bascule » à déplacer dans un dialog « événements à venir » avec cloche de notifications | évolution | Présenter les transferts comme des événements à venir dans un panneau/notifications, pas un tableau permanent | IHM - /planning |
| 9 | /planning/poser-slot : « Le lieu visé n'existe pas dans les lieux du foyer. » quoi qu'on saisisse | bug | Poser un slot depuis l'IHM réussit avec un lieu valide (scénario 1 vert) — les lieux du foyer ne sont pas exposés/sélectionnables à l'écran | IHM - /planning/poser-slot |
| 10 | /planning/poser-slot devrait être un composant dans une dialog de /planning | évolution | Poser un slot depuis une dialog du hub plutôt qu'une page dédiée | IHM - /planning/poser-slot |
| 11 | /planning/affecter-periode : « Un responsable est requis pour la période de garde. » quoi qu'on saisisse | bug | Affecter une période depuis l'IHM réussit avec un responsable valide (scénario 7 vert) — les responsables ne sont pas sélectionnables à l'écran | IHM - /planning/affecter-periode |
| 12 | /planning/affecter-periode devrait faire partie d'un workflow de config et d'info sur les acteurs | question ouverte | Faut-il fondre l'affectation de période dans le workflow de configuration des acteurs ? — dépend du modèle d'acteurs | IHM - /planning/affecter-periode |
| 13 | /planning/definir-transfert : « Transfert incomplet : la récupération et l'heure sont requises. » quoi qu'on saisisse | bug | Définir un transfert depuis l'IHM réussit avec des champs valides (scénario 11 vert) — les champs récupération/heure ne sont pas saisissables/transmis | IHM - /planning/definir-transfert |
| 14 | Les transferts devraient être ponctuels (urgence, changement) et calculés automatiquement dans la majorité des cas ; sinon composant d'une dialog de /planning | question ouverte | Le transfert doit-il être dérivé automatiquement du planning (cas par défaut) et n'être saisi qu'à l'exception ? — remet en cause le modèle de transfert explicite (règle 6) | IHM - /planning/definir-transfert |

## Arbitrage

- **Objectif de l'itération** — Rendre le hub `/planning` réellement utilisable à l'usage : d'abord débloquer les trois actions d'écriture qui échouent quoi qu'on saisisse, puis enrichir le modèle d'acteurs/foyer, habiller le hub en calendrier, et enfin ouvrir l'accès aux utilisateurs réels.
- **Arbitre (départage)** — **L'usage réel tranche** (aligné sur l'arbitre de la spec). Quand deux besoins s'opposent, gagne celui qui rend le hub utilisable au quotidien : les données et le câblage qui débloquent l'usage priment sur l'ergonomie de surface, qui prime elle-même sur l'ouverture de l'accès. Application directe : modèle d'acteurs/config foyer (usage réel) gagne contre la refonte calendrier (ergonomie) au rang 2.

## Séquence de livraison

| Rang | Besoin | Type | Sujet make-gherkin | Dépend de |
|---|---|---|---|---|
| 1 | Réparer les 3 bugs de câblage IHM : poser-slot, affecter-periode, definir-transfert échouent invariablement (lieux/responsables non exposés, champs récupération/heure non transmis) alors que les scénarios 1/7/11 sont verts | bug | `reparer-cablage-ihm-actions` | — |
| 2 | Modèle d'acteurs (Admin / Parent / Autre) + écran de config du foyer (≥1 enfant, toujours 2 parents dont un saisissable par l'autre, N acteurs autres éditables) ; affichage adapté au type ; test toujours en admin | nouveau besoin | `modele-acteurs-config-foyer` | rang 1 |
| 3 | Refonte du hub /planning en calendrier navigable (semaine + 4 semaines, navigation type agenda), responsabilité par code couleur, slots et transferts en dialogs / panneau « événements à venir » avec cloche de notifications | évolution | `hub-calendrier-navigable` | rang 2 |
| 4 | Auth réelle : landing page + connexion email via OAuth (Gmail / Apple / Microsoft) pour identifier et connecter les utilisateurs réels — lève le risque mortel d'adoption de l'autre parent | nouveau besoin | `auth-landing-oauth` | rang 2 |

## Prochain sujet → make-gherkin

- **Sujet** : `reparer-cablage-ihm-actions` — Réparer le câblage IHM des trois actions d'écriture du planning
- **Périmètre** : Faire réussir depuis l'écran les trois actions déjà couvertes en backend (scénarios verts 1, 7, 11) :
  - (1) **poser-slot** — exposer les lieux du foyer dans un sélecteur pour qu'un lieu valide soit transmis, au lieu de l'erreur « Le lieu visé n'existe pas dans les lieux du foyer. » ;
  - (2) **affecter-periode** — exposer les responsables disponibles dans un sélecteur pour qu'un responsable valide soit transmis, au lieu de « Un responsable est requis pour la période de garde. » ;
  - (3) **definir-transfert** — saisir et transmettre la récupération et l'heure pour éviter « Transfert incomplet : la récupération et l'heure sont requises. ».
  - Vérifier aussi que le bouton « Modifier » des périodes déclenche bien l'édition (`ModifierPeriodeHandler`, scénarios 9/10).
  - Périmètre = correction de câblage/transmission IHM vers des handlers existants, **sans nouvelle règle métier**.
- **Hors périmètre (reporté)** : Modèle d'acteurs Admin/Parent/Autre et config foyer (rang 2) ; refonte calendrier, code couleur, dialogs, panneau événements/cloche (rang 3) ; auth landing + OAuth (rang 4) ; nouveau thème graphique (retour 1) ; transfert calculé automatiquement (question produit reportée, retour 14).

## Risques & questions encore ouvertes

- **Réparation rang 1 minimale tant que rang 2 n'est pas fait** — les sélecteurs de lieux et de responsables s'appuieront sur le référentiel actuel (Parent/Invité, lieux du foyer existants) ; quand le modèle d'acteurs riche arrivera (rang 2), ces sélecteurs devront être recâblés.
- **Adoption de l'autre parent (risque mortel de la spec) repoussée au rang 4** — acceptable seulement parce que la phase de test se fait toujours en admin ; ne pas laisser glisser indéfiniment.
- **Question ouverte (retour 12)** — faut-il fondre l'affectation de période dans le workflow de configuration des acteurs ? À reposer au rang 2.
- **Question ouverte (retour 14)** — le transfert doit-il être dérivé automatiquement du planning et n'être saisi qu'à l'exception ? Cela contredit la règle 6 (transferts explicites) et devra être arbitré avant d'implémenter une évolution du transfert.
- **Refonte calendrier (rang 3) transverse à presque tous les retours IHM** — risque de bloc indivisible si elle absorbe slots + responsabilité + transferts d'un coup ; à découper en incréments adoptables au moment du make-gherkin.
- **Bypass Tech assumé** — aucune contrainte technique injectée pour cette revue (l'utilisateur a fait uniquement de l'IHM ; le code et les tests seront revus séparément sur GitHub).
