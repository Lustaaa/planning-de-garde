---
description: Cadre un produit ou une feature de bout en bout — challenge le PO puis rédige/maj la spec au format maison.
argument-hint: "[sujet ou feature à cadrer] (optionnel)"
---

# /spec — Cadrage produit

Orchestration de bout en bout : challenger l'idée, puis la coucher en spec.

Sujet (optionnel) : $ARGUMENTS

## Déroulé

1. **Explorer le contexte.** Lis la spec existante, `docs/`, et les derniers commits avant toute chose.

2. **Passe de challenge.** Invoque le skill `challenge-po` et exécute-le entièrement :
   - nomme les tensions / angles morts à voix haute,
   - pose les questions une à une (objectif réel, arbitre, vraie douleur), avec hypothèse par défaut,
   - refuse les réponses « tous / les trois à la fois » → force le séquencement,
   - termine par une synthèse : objectif + arbitre, séquence, risques ouverts.

3. **Validation PO.** Présente la synthèse et attends l'accord avant de rédiger.

4. **Rédaction.** Invoque le skill `redaction-spec` et produis/mets à jour la spec canonique au format Contexte / Objectif & arbitrage / Séquence / Mécaniques de base / Règles de gestion / Risques.

5. **Propagation.** Mets à jour les docs qui référencent la spec (README, roadmap) pour rester cohérents ; garde une seule source de vérité et pointe les brouillons obsolètes vers elle.

6. **Commit.** Propose un commit (sans pousser sauf demande explicite).

## Notes

- Fonctionnel uniquement : aucun choix technique dans la spec.
- Une question à la fois pendant le challenge — pas de rafale.
- Le challenge n'est pas une formalité : pas de complaisance, on tranche.
