# Sc.9 — API distante injoignable : échec clair, édition non appliquée

`@erreur` `🖥️ IHM`

↩ Retour : [00-sprint08-suivi.md](00-sprint08-suivi.md)

**Routage** : **100 % runtime IHM** (`ihm-builder`). **Aucun driver backend** : le symptôme est
un **échec de transport** (API injoignable) qui n'atteint **jamais** le handler — il vit dans
le **client d'écriture front** (`ClientCanalEcriture`). Pas de mise en file ni de rejeu.

## Acceptation (BDD) — niveau **runtime/IHM** (routé `ihm-builder`)

> Symptôme runtime **transport** : l'API distante étant injoignable, l'enregistrement échoue
> **clairement** (message à l'écran), l'édition **n'est pas appliquée** (case + légende gardent
> l'ancienne valeur) et reste **à resoumettre**. bUnit seul ne prouve pas le chemin HTTP réel
> ni la gestion de l'échec transport — acceptation sur l'app câblée avec API **down**.

`Should_Afficher_un_echec_clair_et_laisser_Alice_inchangee_dans_la_case_du_14_07_2026_et_en_legende_a_resoumettre_When_on_renomme_parent_a_en_Alicia_alors_que_l_API_distante_est_injoignable`

- **Niveau** : E2E/runtime sur l'app câblée avec **API distante injoignable** (pattern
  `ApiDistanteFactory` / `FakeCanalHttpHandler` côté échec transport). Store réel : `parent-a`
  (Alice).
- **Observable** : message d'échec clair à l'écran ; case du 14/07 et légende restent
  « Alice » ; l'édition reste à resoumettre (aucune file, aucun rejeu automatique).
- **Statut** : ✅ GREEN (runtime, `@vert`) — `FrontWasmConfigApiInjoignableTempsReelTests`
  (grille sur API live affichant « Alice » ; écran de config câblé à une API réellement arrêtée —
  port TCP libéré → vrai `ConnectionRefused` ⇒ `HttpRequestException`, pas un stub 4xx ; message
  « service injoignable » surfacé, grille inchangée, formulaire à resoumettre). **Zéro code de
  production** : réutilise la gestion d'échec transport déjà câblée (`catch HttpRequestException`
  → `PoserSlot.MessageServiceInjoignable`).

## Tests unitaires backend

*(Néant.)* L'échec est **transport** (l'API n'est pas atteinte) : ni le handler ni le store ne
sont sollicités. La gestion (message + non-application + resoumission) vit dans le **client
front** → **aucun rouge backend**, aucun test unitaire backend.

## Fichiers à créer / modifier

- *(Aucun fichier backend.)*
- **Câblage IHM** (routé `ihm-builder`) — `ClientCanalEcriture` (Web) surface l'échec
  transport ; l'écran de config affiche le message et **ne mute pas** l'état local. Réutilise
  le pattern d'échec HTTP existant (`FakeCanalHttpHandler`).

## Design notes

- **Échec transport ≠ refus métier** : le refus métier (Sc.8) traverse le handler et renvoie
  un motif ; ici l'API est **injoignable**, rien n'est appliqué côté serveur. Deux chemins
  d'échec distincts à ne pas confondre.
- **Pas de file ni de rejeu** (analyse technique) : l'édition reste simplement **à
  resoumettre** par l'utilisateur — pas d'orchestration de retry dans ce sprint (YAGNI).
- **Édition non appliquée** : l'état local de la grille ne doit pas « optimistiquement »
  refléter une écriture qui n'a pas abouti (anti « vert qui ment »).
