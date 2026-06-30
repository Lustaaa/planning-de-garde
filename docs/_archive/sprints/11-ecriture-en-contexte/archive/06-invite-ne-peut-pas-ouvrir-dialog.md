# Scenario 6 — Un Invité ne peut pas ouvrir la dialog depuis une case `@erreur 🖥️ IHM`

[← Retour au suivi](00-sprint11-suivi.md)

> **Routé vers `ihm-builder`.** Niveau d'acceptation = **E2E/runtime**. **Driver neuf** : le
> déclencheur d'écriture **migre** de l'écran dédié **vers la case** (palier 7) ; le **gater**
> en consultation seule (règle 9) est du **rendu conditionnel IHM neuf** réutilisant le
> **contexte rôle existant** (`SessionPlanning`, acquis s01). **Ni auth ni impersonation**
> tirées (paliers 8/15 intacts).

## Acceptation (BDD) — test de NIVEAU RUNTIME

**Should_N_ouvrir_aucune_dialog_d_ecriture_et_garder_la_grille_en_lecture_seule_When_un_invite_en_consultation_seule_clique_une_case** — ✅ GREEN (`FrontWasmInviteNePeutPasOuvrirDialogTests`)

> **Caractérisation early-green (décision CP)** : l'acceptation passe **sans fix** — le gating Invité a
> été **mutualisé dès le Sc.1/Sc.2** dans `OuvrirMenu` (`if (!Session.EstParent) return;`) + classe
> `grille-jour-cliquable` conditionnelle. Test runtime **non-vacuous avec contrôle positif** (garde-fou
> CP) : en **Parent** le clic ouvre bien le menu (déclencheur actif), PUIS bascule en **Invité** via le
> sélecteur de rôle réel → le clic n'ouvre **ni menu ni dialog**, le déclencheur est désactivé (classe
> cliquable retirée), consultation seule signalée. Sans le contrôle positif, un clic cassé pour tous
> passerait vacuously. Pas de cycle RED→GREEN factice.

Sur l'app **réellement câblée**, le planning est affiché pour un **Invité en consultation
seule** ; la case du **mardi 16/06/2026** est visible. L'Invité **clique la case** :
**aucune dialog d'écriture ne s'ouvre**, le **déclencheur d'écriture de la case est
désactivé** (rendu conditionnel sur le rôle), et la **grille reste en lecture seule**.

## Tests bUnit composant (optionnels — pilotés par `ihm-builder`)

| # | Test composant (FLFI) | TPP | Contradiction | Status |
|---|------------------------|-----|---------------|--------|
| 1 | Should_Ne_pas_ouvrir_de_dialog_d_ecriture_When_un_invite_clique_une_case | (conditional sur rôle) garde Invité | Si le clic-case ouvre la dialog sans gater le rôle, l'Invité écrirait → contradiction avec règle 9 | ⏳ Pending |
| 2 | Should_Desactiver_le_declencheur_d_ecriture_de_la_case_When_la_session_est_en_consultation_seule | (conditional) rendu conditionnel du déclencheur | Un déclencheur toujours actif (Parent par défaut) violerait la consultation seule | ⏳ Pending |

## Fichiers à créer / modifier (par `ihm-builder`)

- **`PlanningPartage.razor` / `.razor.cs`** — le déclencheur d'écriture de la case
  (`@onclick` ouvrant la dialog) est **rendu conditionnel** sur `Session.EstParent` ;
  désactivé/non câblé pour l'Invité (réutilise le `Desactive` / `EstParent` déjà présents
  pour les liens-barre).
- **`Web.Tests`** — bUnit (session Invité : clic sans dialog) + acceptation runtime.

## Design notes

- **Le point d'application du droit Invité migre avec le déclencheur** (écran → case) : c'est
  un **observable IHM neuf**, donc un driver, mais **borné** — il réutilise l'acquis d'accès
  (`SessionPlanning`, règle 9, archive s01 `06-invite-edition-refusee.md`) **sans** tirer la
  fondation auth/impersonation devant l'usage.
- Les anciennes pages dédiées gataient déjà l'Invité (`PoserSlot.razor` / `AffecterPeriode`,
  « Action réservée aux Parents ») ; ce sprint **transpose** cette garde sur la **case** et
  retire les écrans dédiés.
- **Acceptation runtime** : prouver sur l'app câblée que le clic Invité **n'ouvre rien** —
  un bUnit composant pourrait masquer un câblage de DI réel du rôle.
