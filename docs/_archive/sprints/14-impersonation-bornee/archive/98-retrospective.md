# Rétrospective méthode — sprint 14 (impersonation bornée)

> 6/6 verts, 214/214. 5 actions retenues (CP, palier 0), **appliquées** par le thread
> principal après autorisation PO directe (garde Self-Modification sur `.claude/`).

## Bien
- **Prédiction early-green juste** : Sc.2/3/4 early-green attendus, zéro escalade CP parasite — le correctif s13 A2 (vérifier le câblage prérequis sur l'écran ciblé) a payé.
- **Cadrage CP** : D1→D4 ont tranché périmètre/concurrence/type/plan sans G1 parasite, routage 100% IHM runtime tenu.

## À améliorer (→ actions)
1. Réentrance renderer bUnit (stack overflow Sc.5, clic réentrant pendant la pompe) — non documentée.
2. Dette P2 ambiguë : le garde `WaitForState` (s13 A1) ne couvre pas la convergence SignalR multi-clients.
3. Garde Self-Modification redécouvert 2 sprints de suite (édits `.claude/` exigent l'aval PO direct, pas la délégation CP).
4. (positif à acter) cascade early-green s13 A2 confirmée.
5. **Spec bloat** (retour PO) : v15 = 665 lignes, trop — `spec-consolidation` accrète sans élaguer.

## Actions appliquées
| # | Cible | Édition |
|---|-------|---------|
| A1 | `ihm-builder.md` + `tdd-implement/SKILL.md` | Règle « pas d'interaction réentrante pendant la pompe de diffusion » (stack overflow). |
| A2 | `ihm-builder.md` | Rétrofit P2 = convergence SignalR multi-clients, distincte de la course d'énumération gardée (s13 A1). |
| A3 | `6-cloture-sprint.md` + `retro-sprint/SKILL.md` | Protocole Self-Modification : édits `.claude/` appliqués par le thread principal après aval PO direct. |
| A4 | `tdd-analyse.md` | Acte le positif : contrôle préalable early-green confirmé s14. |
| A5 | `spec-consolidation.md` | **Concision impérative** : élaguer autant qu'ajouter, règle = 1–3 phrases, cible < ~300 lignes (retour PO sur v15=665 lignes). |

## Suite (hors rétro, demandé PO)
- Skill de **rédaction** concise + **agent de nettoyage** (pour résorber le bloat des specs existantes) — à construire ultérieurement.
