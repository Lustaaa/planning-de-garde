# Scénario 6 — API distante injoignable pendant la navigation

`@erreur` · **🖥️ scénario IHM** — **Routé vers `ihm-builder`** · **acceptation RUNTIME**. **Vrai driver
front** (l'actuel `ChargerAsync` vide la grille sur échec — comportement à durcir pour la navigation).

[← Retour au suivi](00-sprint15-suivi.md)

Avec l'API distante injoignable, cliquer « Semaine suivante » **laisse la fenêtre courante inchangée**
(lundi 08/06), affiche un **message d'échec clair**, et **ne met aucune navigation en file ni ne la rejoue**
(règle 28 — échec clair, sans file ni rejeu).

## Acceptation (BDD) — niveau RUNTIME — ✅ GREEN

`Should_Conserver_la_fenetre_courante_du_lundi_08_06_2026_et_afficher_un_echec_clair_sans_mise_en_file_ni_rejeu_When_une_navigation_echoue_parce_que_l_API_distante_est_injoignable_sur_l_app_reellement_cablee`
(`tests/PlanningDeGarde.Web.Tests/FrontWasmNavigationEchecTempsReelTests.cs`)
— sur l'app réellement câblée avec la re-requête de la date naviguée (`GET /api/grille/2026/6/15`) coupée
(échec de transport déterministe) : la fenêtre reste celle du 08/06 (pas de grille vide), un message
d'échec clair s'affiche, aucune re-tentative automatique. Anti vert-qui-ment : un « Semaine précédente »
qui transite ramène au **01/06** (et non 08/06) → preuve runtime que l'ancre n'a PAS avancé pendant l'échec
(aucun rejeu, aucune file). bUnit seul ne reproduit pas le défaut runtime (transport HTTP réel).

## Inner-loop (boucle rapide `ihm-builder`)

| # | Test inner-loop (échec de navigation) | Contradiction | Status |
|---|---------------------------------------|---------------|--------|
| 1 | `Should_Preserver_la_fenetre_affichee_et_signaler_l_echec_sans_rejouer_When_la_re_requete_de_navigation_echoue` | L'actuel `ChargerAsync` **vide** la grille sur `HttpRequestException` ; pour la navigation il faut **préserver** la fenêtre courante + signaler l'échec, sans re-projeter à vide ni rejouer. **Driver front.** | ✅ GREEN (couvert par l'acceptation runtime) |

> Driver porté directement par l'acceptation RUNTIME ci-dessus (le symptôme vit dans le composant câblé,
> pas dans un état isolable). `ChargerAsync` renvoie désormais un booléen de succès ; le pivot de navigation
> `NaviguerAsync` **restaure l'ancre** + lève le bandeau d'échec si la re-requête échoue, sans rejeu.

## Fichiers à créer / modifier

- `src/PlanningDeGarde.Web/Components/Pages/PlanningPartage.razor` (+ `.razor.cs`) — gestion d'échec de
  **navigation** : conserver la fenêtre affichée (et l'ancre courante), bandeau d'échec clair refermable,
  **aucun** rejeu/file.
- `src/PlanningDeGarde.Web/MessagesEcriture.cs` (ou équivalent) — libellé du message d'échec de navigation.

## Design notes

- **Différence clé avec le chargement initial** : à l'ouverture, une grille vide sur échec est tolérée
  (vue consultable) ; en **navigation**, on ne doit **pas** perdre la fenêtre déjà affichée — c'est le vrai
  rouge. Ne pas confondre avec un early-green de la plomberie de Sc.1.
- **Cohérence règle 28** : échec clair, saisie/navigation non appliquée, **sans mise en file ni rejeu** (le
  hors-ligne rejouable est un palier technique ultérieur, hors scope).
- L'ancre de navigation **ne doit pas** avancer si la re-requête échoue (sinon l'état et l'affichage
  divergent). → remonter au CP si le libellé/forme du message d'échec doit être harmonisé avec l'échec
  d'écriture existant.
