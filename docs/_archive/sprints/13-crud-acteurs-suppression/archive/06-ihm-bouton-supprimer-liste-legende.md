# Sc.6 — Depuis l'écran de config : bouton supprimer → liste, légende, accusé `@nominal` 🖥️ scénario IHM `@caractérisation`

← [Retour au suivi](00-sprint13-suivi.md)

> **Routé vers `ihm-builder`** — **niveau d'acceptation E2E / runtime** (app réellement câblée : DI
> réelle, front WASM + API distante + Mongo réel + SignalR). **PAS** un test backend bUnit-à-doublures
> destiné à `tdd-auto`. Caractérisation : compose des mécaniques déjà livrées (canal d'écriture, liste
> relue, registre accusé, légende dédoublonnée) + le **driver de câblage IHM** (bouton supprimer).

## Acceptation (BDD)

Test **runtime** reproduisant le geste PO : sur l'écran de configuration du foyer affiché pour un
**Parent** (foyer Parent A / Parent B / Nounou, légende faisant apparaître Nounou), un clic sur le
**bouton supprimer** de Nounou →
- Nounou **disparaît de la liste relue** des acteurs ;
- un accusé **« Acteur supprimé »** s'affiche **à part, sans bloquer** (registre avertissement, aligné
  sur « Transfert défini » — D5) ;
- la **légende dédoublonnée** du planning ne fait **plus apparaître** Nounou ;
- Nounou est **absent du store Mongo relu**.

Prouvé sur l'app réellement câblée (front WASM + API distante + Mongo réel), **pas** par bUnit seul
(render mode, DI, transport HTTP, store durable hors de portée de bUnit).

**✅ GREEN** — `FrontWasmConfigSupprimerActeurTempsReelTests` vert sur l'app réellement câblée
(API distante réelle `ApiDistanteFactory`, store réel, diffusion SignalR réelle). **RED → GREEN** :
le RED a échoué nommément (`ArgumentNullException`, `bouton-supprimer` introuvable = symptôme PO « pas
de bouton supprimer / @onclick mort »), puis l'IHM posée (bouton supprimer par acteur + `@onclick` →
`POST /api/canal/supprimer-acteur` + accusé « Acteur supprimé » non bloquant + ré-énumération) repasse
au vert. Suite complète **193/193** (Docker actif, sans `--no-build` ni filtre). Balayage runtime
`Web.Tests` après touche du composant partagé (liste d'acteurs) : 3 assertions runtime préexistantes
rendues précises (`.acteur-nom` au lieu du `TextContent` du `li`, désormais pollué par le libellé du
bouton) — `FrontWasmConfigAjouterActeurTempsReelTests`, `…AjouterServiceInjoignable…`,
`…AjouterSansNomRefuse…`.

## Tests unitaires (ordonnés)

_Détail RED→GREEN sur le `.razor` + câblage piloté par `ihm-builder`._ Driver de câblage : le bouton
supprimer émet la commande sur le canal et déclenche la relecture ; les autres observables (liste,
accusé, légende, store) réutilisent des mécaniques acquises.

1. **RED** (runtime) — `Should_Retirer_grand_pere_de_la_liste_et_de_la_legende_avec_un_accuse_Acteur_supprime_sans_recharger_la_page_When_un_parent_clique_le_bouton_supprimer_depuis_l_ecran_de_configuration`
   échoue : aucun `[data-testid='bouton-supprimer']` à cliquer (écran statique faute de bouton/@onclick).
2. **GREEN** — bouton supprimer gating-ready dans la liste, `@onclick="() => Supprimer(a.Id)"`, émission
   canal `POST /api/canal/supprimer-acteur` (`SupprimerActeurRequete`), accusé non bloquant + relecture
   du store ; la légende du planning dédoublonnée perd grand-père via diffusion temps réel (filtre
   d'existence `Resolvable` côté projection, Sc.2/Sc.4).

**Câblage partagé posé (cascade)** : bouton + émission canal + issue accusé/échec
(`HttpRequestException` → `MessagesEcriture.ServiceInjoignable`, Sc.8) + relecture liste/légende →
réutilisés par Sc.7 (gating Invité), Sc.8 (API injoignable), Sc.9 (temps réel).

## Fichiers à créer

- `src/PlanningDeGarde.Web/Components/Pages/ConfigurationFoyer.razor` (+ `.razor.cs`) — bouton supprimer par acteur + émission sur le canal + accusé non bloquant
- `src/PlanningDeGarde.Web/CanalEcriture.cs` — méthode de suppression (`POST /api/canal/supprimer-acteur`)
- `tests/PlanningDeGarde.Web.Tests/FrontWasmConfigSupprimerActeurTempsReelTests.cs` (acceptation runtime / intégration)

## Design notes

- **Accusé « Acteur supprimé »** tranché par **D5** (libellé + registre avertissement-à-part, non
  bloquant). Pas d'escalade nécessaire sur le libellé.
- **Câblage partagé** posé ici (bouton, émission canal, issue accusé, relecture liste/légende) : il est
  **réutilisé** par les Sc.7 (gating), Sc.8 (échec), Sc.9 (temps réel) → ceux-ci tomberont
  **early-green** une fois ce câblage en place (cf. leur annotation).
- L'écran énumère les acteurs **depuis le store** (acquis s09) ; la légende est **dédoublonnée par id**
  (acquis s07). La suppression réutilise ces mécaniques, ne les redrive pas.
