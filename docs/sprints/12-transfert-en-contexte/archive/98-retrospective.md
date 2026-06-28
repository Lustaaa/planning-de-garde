# Rétrospective — Sprint 12 (transfert en contexte)

> Rétro de la **méthode** (pipeline d'agents/skills/commands) · produite par `retro-sprint`
> (étape 1 de `/6-cloture-sprint`). Distincte de `99-sprint12-besoins-fin-itération.md`
> (rétro produit). Sprint 100 % IHM, 6/6 verts, suite complète **182/182**, épic É12
> « écriture en contexte » refermé (retrait du dernier écran de saisie dédié).

## Ce qui a bien marché

- **Cascade early-green IHM maîtrisée.** `tdd-analyse` a anticipé Sc.2/3/4/6 en
  caractérisations « câblage IHM partagé » ; `ihm-builder` les a batchés en **1 run + 1
  commit**. Aucun early-green inattendu, **0 escalade CP** sur ce front — la prédiction de
  cascade ajoutée en rétro s11 (`tdd-analyse` §136-147) a tenu en s12.
- **Routage 100 % IHM net.** Les 6 scénarios étiquetés `🖥️ IHM` dès l'analyse (étape 2bis
  de `tdd-analyse`) ; aucun re-dispatch mécanique `tdd-auto`→`ihm-builder`.
- **Acceptation runtime tenue (rempart anti vert-qui-ment).** Preuve sur front WASM réel +
  API distante + store réel (`00-sprint12-suivi.md`), témoins assertés ; bUnit jamais preuve
  seule.
- **CP autonome efficace.** 5 décisions journalisées, **0 G1** (cadrage des cases orphelines
  d'un acteur supprimé tranché par règle structurante en `/5`), aucune porte PO superflue
  ouverte.

## Ce qui a coincé

- **Retours produit VIDE au gate G3 (récurrent s11→s12).** `99-sprint12-retours.md` section
  `# Retours produit (PO)` = placeholders partout ; le journal CP acte « retours produit
  vide → pilotage au catalogue ». `/4-retours` s'est réduit à désigner le prochain sujet
  depuis le backlog. Le cas vide était traité comme une anomalie alors qu'il est devenu le
  régime nominal.
- **Flakes SignalR `*TempsReel*` au 1er run complet (récurrent s10→s12).** `ihm-builder.md`
  codifie déjà la convention anti-flake + un retrofit P2, mais le retrofit reste différé au
  backlog ; 1-2 flakes au 1er run full, verts en isolation/re-run. Bruit à chaque commit
  IHM ; aucune consigne au VERIFY/commit pour distinguer ce flake connu d'une vraie
  régression.
- **DLL verrouillées par hôtes résiduels avant rebuild.** `ihm-builder` a dû tuer
  manuellement l'hôte Api + le devserver Blazor d'un run précédent (MSB3027). `run.ps1`
  nettoie les zombies, mais seulement au lancement de l'app — `test-count.ps1` (VERIFY) et le
  `dotnet build` direct ne nettoyaient rien.
- **Double numérotation spec vs BACKLOG (récurrent).** Le journal CP (Q2, écriture v13) acte
  « réconciliation différée en `/6` » ; l'écart de numérotation des paliers (spec v13 =
  référence unique vs `docs/BACKLOG.md`) était re-reporté à chaque sprint au lieu d'être
  résorbé.

## Actions sur le pipeline

> Priorisation tranchée par le **CP** (4 tweaks à faible risque, non destructifs, aucune
> escalade G1). Aval PO explicite obtenu (AskUserQuestion → « Appliquer les 4 »). Les
> éditions `.claude/` ont été **appliquées par le thread principal** (le garde-fou
> anti-auto-modification bloque les subagents ; levé par aval PO).

| # | Cible (fichier) | Édition | Statut |
|---|---|---|---|
| A1 | `.claude/commands/4-retours.md` | Blockquote fast-path « pilotage au catalogue » : retours produit vides = régime nominal (pas une friction) ; le CP dérive le sujet du backlog sans rouvrir de porte. Borne : n'altère pas l'arbitrage G2 du PO. | ✅ appliquée |
| A2 | `.claude/agents/ihm-builder.md` (VERIFY) | Bullet « Flake CONNU `*TempsReel*` au 1er run ≠ régression » → re-run isolé avant de bloquer le commit ; ne basculer rouge que s'il persiste sous isolation. Borne : ne lève pas l'acceptation runtime ni le rempart anti vert-qui-ment ; vrai fix = retrofit P2 au backlog. | ✅ appliquée |
| A3 | `.claude/skills/tdd-implement/scripts/test-count.ps1` | Kill des hôtes `PlanningDeGarde.Api/.Web` résiduels avant `dotnet test` (snippet ciblé repris de `run.ps1`), anti MSB3027/DLL verrouillées. | ✅ appliquée |
| A4 | `.claude/commands/6-cloture-sprint.md` (étape 4bis) | Blockquote check obligatoire « spec vivante = référence unique de numérotation » : réconcilier l'écart de paliers `BACKLOG.md` ici, ne plus le re-différer. | ✅ appliquée |

## Questions ouvertes (méthode)

- **Retrofit P2 anti-flake `*TempsReel*` récurrent s10→s12.** A2 réduit le *bruit* au commit
  mais ne corrige pas la cause ; le vrai correctif (rétrofit de la famille
  `FrontWasmConfigCycle*TempsReel*` sur attente déterministe d'établissement + isolation
  d'état) reste une passe dev séquencée au backlog. Après 3 sprints de récurrence, à
  arbitrer (CP/PO) : le tirer devant le prochain scénario temps-réel, ou continuer à le
  différer ?
