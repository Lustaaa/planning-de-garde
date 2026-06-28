# Scenario 7 — Slot chevauchant accepté avec avertissement non bloquant `@limite 🖥️ IHM`

[← Retour au suivi](00-sprint11-suivi.md)

> **Routé vers `ihm-builder`.** Niveau d'acceptation = **E2E/runtime**.
> **⚠️ Caractérisation (filet) — règle métier probablement early-green.** La **règle 16**
> (chevauchement **accepté + averti**, « ni refusé ni dédoublonné ») est **déjà verte depuis
> s01** (`Scenario5_Chevauchement`, `AvertissementChevauchement`). Le **seul neuf est
> l'habillage IHM** : sur un chevauchement, l'écriture **aboutit** donc la dialog **se ferme**
> (comme tout succès, décision CP), le slot **réapparaît**, et l'avertissement s'affiche
> **à part** (toast/bandeau) **sans bloquer**.

## Acceptation (BDD) — test de NIVEAU RUNTIME

**Should_Fermer_la_dialog_faire_reapparaitre_le_slot_chevauchant_et_signaler_l_avertissement_a_part_sans_bloquer_When_un_parent_pose_un_slot_qui_en_chevauche_un_autre** — ✅ GREEN (`FrontWasmChevauchementAccepteAvertiTests`, 2 Facts : chevauchement → bandeau / sans chevauchement → pas de bandeau)

> **Cycle RED→GREEN réel (test #2, habillage IHM neuf)** — exception de scope bornée (décision CP) :
> le canal `POST /api/canal/poser-slot` cesse de renvoyer `Ok()` nu ; il porte dans son **corps de
> succès** un `PoserSlotReponse(bool Chevauchement)`, lu depuis le read model **existant**
> `JourneeEnfantQuery.Chevauchements` (règle 16, verte s01) — **aucune règle, handler, ni recalcul
> neuf** ; **`GrilleAgendaQuery`/Domain intacts** ; pas de nouvel endpoint. La dialog (issue succès)
> remonte le drapeau ; `PlanningPartage` affiche un **bandeau à part, non bloquant, refermable** après
> fermeture. CQRS préservé : l'avertissement est un attribut de l'outcome de la commande (réponse du
> canal requête/réponse), distinct de la diffusion SignalR et de la projection de lecture. Test #1
> (fermeture) reste une caractérisation (issue succès du Sc.1). Acceptation **non-vacuous** : avec
> chevauchement → bandeau + slots conservés (école + nounou) ; sans chevauchement → **pas** de bandeau.

Sur l'app **réellement câblée**, la case du **lundi 22/06/2026** contient déjà un slot
« École » 08:00→12:00. Un Parent ouvre la dialog depuis cette case, choisit « Nounou »
10:00→14:00 et **valide**. **Observable runtime** : l'écriture **aboutit** → la dialog **se
ferme** ; un slot « Nounou » 10:00→14:00 **réellement enregistré** réapparaît dans la case du
lundi 22/06/2026 ; un **avertissement de chevauchement s'affiche à part** (toast/bandeau),
**sans bloquer** ; le slot « École » 08:00→12:00 **reste présent** dans la case (ni refusé ni
dédoublonné).

## Tests bUnit composant (optionnels — pilotés par `ihm-builder`)

| # | Test composant (FLFI) | TPP | Contradiction | Status |
|---|------------------------|-----|---------------|--------|
| 1 | Should_Fermer_la_dialog_When_la_pose_chevauchante_aboutit | (conditional) issue succès même sur averti | ⚠️ probablement early green — l'issue succès du Sc.1 ferme déjà la dialog ; le chevauchement reste un succès (règle 16) | ⏳ Pending |
| 2 | Should_Afficher_l_avertissement_de_chevauchement_a_part_sans_bloquer_When_la_pose_chevauche_un_slot_existant | (conditional) habillage avertissement non bloquant | Un avertissement **dans** la dialog (traité comme un échec) bloquerait → contredit « accepté + non bloquant » (le neuf à driver) | ⏳ Pending |

## Fichiers à créer / modifier (par `ihm-builder`)

- **`PlanningPartage.razor` / `.razor.cs`** — afficher l'avertissement de chevauchement
  renvoyé par le retour de commande dans un **bandeau/toast à part** (`data-testid` dédié),
  **non bloquant**, **après** fermeture de la dialog.
- **`PoserSlotDialog.razor.cs`** — l'avertissement n'empêche **pas** la fermeture (issue
  succès, distincte de l'issue échec du Sc.4).
- **`Web.Tests`** — bUnit (canal renvoyant l'avertissement) + acceptation runtime.

## Design notes

- **⚠️ Couverte par l'acquis** : la règle 16 (accepté + averti) et l'`AvertissementChevauchement`
  côté Application sont **verts s01** — ce scénario **ne re-spécifie aucune règle métier**. Le
  seul **driver réel** est l'**habillage IHM non bloquant** (test #2). Le test #1 (fermeture)
  est une **caractérisation** : l'issue succès du Sc.1 le couvre déjà → `tdd-auto`/`ihm-builder`
  doit l'attendre **vert d'emblée** (pas un défaut).
- **Distinction d'issue** (décision CP) : chevauchement = **succès** (dialog fermée +
  avertissement à part), à **ne pas** confondre avec l'issue échec du Sc.4 (dialog ouverte +
  message dans la dialog). C'est le point de conception que ce scénario protège.
