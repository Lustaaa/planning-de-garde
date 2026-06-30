# Scenario 5 — Annuler la dialog ne modifie pas le planning `@limite 🖥️ IHM`

[← Retour au suivi](00-sprint11-suivi.md)

> **Routé vers `ihm-builder`.** Niveau d'acceptation = **E2E/runtime**. Comportement IHM :
> annuler **n'émet aucune écriture** et laisse la grille intacte (règle 14). Contredit le
> chemin de validation (Sc.2) : ici la fermeture ne doit **pas** émettre de commande.

## Acceptation (BDD) — test de NIVEAU RUNTIME

**Should_N_emettre_aucune_ecriture_et_garder_le_responsable_de_fond_Bruno_When_un_parent_annule_la_dialog_sans_valider** — ✅ GREEN (`FrontWasmAnnulerDialogSansEcrireTests`)

> **Caractérisation early-green (décision CP)** : l'acceptation passe **sans fix** — `OnAnnule` /
> `FermerDialog` (fermeture sans appel canal, sans relecture) a atterri dès les fix Sc.1/Sc.2. Test
> runtime **non-vacuous** conforme au garde-fou CP : un **spy de canal** compte les `POST /api/canal/…`
> et exige **0** écriture émise à l'annulation (pas seulement « la case n'a pas changé »), la case reste
> sur « Bruno », et le store distant ne reçoit **aucune** affectation d'Alice. Si l'annulation
> réutilisait le chemin de validation, le spy compterait ≥ 1 → rouge. *(Bruno semé via période ; le
> « de fond » vs « période » est immatériel à la sémantique d'annulation — seul compte « inchangé ».)*
> Pas de cycle RED→GREEN factice.

Sur l'app **réellement câblée**, la case du **samedi 20/06/2026** affiche le responsable de
fond « Bruno ». Un Parent **clique la case** → la dialog « Affecter une période » s'ouvre →
choisit « Alice » comme responsable → **annule sans valider**. **Observable runtime** : la
dialog se ferme, **aucune écriture n'est émise** (aucune commande ne transite vers l'API
distante — le store reste inchangé), et la case du samedi 20/06/2026 **affiche toujours le
responsable de fond « Bruno »**.

## Tests bUnit composant (optionnels — pilotés par `ihm-builder`)

| # | Test composant (FLFI) | TPP | Contradiction | Status |
|---|------------------------|-----|---------------|--------|
| 1 | Should_Fermer_la_dialog_sans_emettre_de_commande_When_un_parent_annule_la_dialog | (conditional) annulation ≠ validation | Un câblage qui émet à la fermeture (réutilisant le chemin valider) émettrait à tort → contradiction | ⏳ Pending |
| 2 | Should_Laisser_la_case_afficher_le_responsable_de_fond_When_un_parent_annule_apres_avoir_change_le_choix | (conditional) pas d'effet de bord | Une mutation locale de la case sur changement de choix violerait « grille intacte » | ⏳ Pending |

## Fichiers à créer / modifier (par `ihm-builder`)

- **`AffecterPeriodeDialog.razor` / `.razor.cs`** — `OnAnnule` ferme la dialog **sans**
  appeler le canal ; le choix saisi reste local et est **abandonné**.
- **`PlanningPartage.razor.cs`** — fermeture sur annulation **sans** relecture ni mutation de
  la grille.
- **`Web.Tests`** — bUnit (canal espionné : 0 requête sur annulation) + acceptation runtime
  (store distant inchangé).

## Design notes

- **Règle 14** (grille lecture seule) : ni l'ouverture ni l'annulation n'écrivent. Le seul
  chemin d'écriture est la **validation** (Sc.1/Sc.2).
- **Spy sur le canal** : vérifier **0** requête sortante à l'annulation (et non un simple
  « la case n'a pas changé », qui pourrait masquer une écriture annulée côté store).
