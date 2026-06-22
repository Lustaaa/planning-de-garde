# Test sous pression — skills challenge-po & redaction-spec

Protocole : 8 subagents en parallèle, **baseline (sans skill) vs avec skill**,
sur 2 domaines neutres (clicker Paperclip, RDV médical Doctolib). PO simulé
répondant systématiquement « tous / à parts égales » aux questions de
priorisation.

## challenge-po (discipline : refuser « tous » → forcer le séquencement)

| Domaine | Baseline | Avec skill |
|---|---|---|
| Clicker | Refuse « tous », mais **2 questions d'un coup** | **1 seule** question (arbitre A/B/C) ✅ |
| Doctolib | Refuse « tous », question ouverte | **1 seule** question (arbitre A/B/C) + **hypothèse par défaut** challengée ✅ |

**Delta mesuré** : le skill impose le *un-à-la-fois* + format choix multiple +
hypothèse par défaut. Les deux versions nomment le pattern « tout = aucun » et
forcent le classement (modèle déjà capable, le skill structure la passe).

## redaction-spec (format : contrat de sortie)

| Domaine | Baseline | Avec skill |
|---|---|---|
| Clicker | Conforme **mais** a lu le repo et copié le format (contamination), + a écrit un fichier non demandé dans `docs/init/` | Conforme sans accès au repo — numérotation continue 1→25, sections dans l'ordre ✅ |
| Doctolib | Conforme (a lu le repo) | Conforme — numérotation continue 1→27 ✅ |

**Delta mesuré** : le skill produit le format (Contexte / Objectif & arbitrage
/ Séquence / Mécaniques / Règles numérotées en continu / Risques) **sans
dépendre du repo**. Fonctionnel only, `**Nom** — description` respecté.

## Verdict

Les 2 skills généralisent à des domaines nouveaux. Aucun correctif nécessaire.

**Effet de bord observé** : un agent baseline (sans consigne de périmètre) a
écrit dans `docs/init/`. Hors test, le skill `redaction-spec` rappelle de
garder une seule source de vérité — mais penser à cadrer le périmètre d'écriture
quand on délègue la rédaction à un subagent.

## Fichiers de sortie

- [`clicker-paperclip.md`](clicker-paperclip.md) — spec du jeu (version avec skill)
- [`rdv-medical-doctolib.md`](rdv-medical-doctolib.md) — spec RDV médical (version avec skill)
