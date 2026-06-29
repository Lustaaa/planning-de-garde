# Scénario 2 — Basculer entre les vues prédéfinies

`@nominal` · **backend** (read model vue/span sur `GrilleAgendaQuery`, frontière Application — testable
sans Blazor, même registre que les `Scenario_Grille*` existants).

[← Retour au suivi](00-sprint15-suivi.md)

Changer de vue **redimensionne** la fenêtre projetée en gardant l'ancre lundi (Semaine / 4 semaines), ou
en s'ancrant au mois calendaire (Mois). C'est le **seul vrai driver backend** du bloc A : la dimension
**vue → span**. (La re-résolution du fond par date est déjà acquise — cf. design notes.)

**Ancrage** : aujourd'hui = mercredi 10/06/2026 → semaine en cours lundi 08/06/2026. Cycle N=2, index 0 →
Alice (ISO paire), index 1 → Bruno (ISO impaire).

## Acceptation (BDD) — ✅ GREEN

`Should_Redimensionner_la_fenetre_selon_la_vue_choisie_Semaine_7j_QuatreSemaines_28j_Mois_semaines_ISO_du_mois_When_la_grille_est_projetee_a_l_ancre_du_08_06_2026`
— sur `GrilleAgendaQuery.Projeter(ancre, vue)` : `Semaine` → 7 jours / 1 ligne (08→14/06) ; `QuatreSemaines`
→ 28 jours / 4 lignes (08/06→05/07) ; `Mois` → semaines ISO entières recouvrant juin 2026 → 5 lignes
(01/06→05/07). Chaque case reste résolue `surcharge > fond > neutre` à sa propre date.

## Tests unitaires (boucle interne, TDD)

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| 1 | `Should_Projeter_7_jours_du_lundi_08_06_au_dimanche_14_06_2026_en_une_seule_ligne_de_semaine_When_la_vue_choisie_est_Semaine_a_l_ancre_du_08_06_2026` | constant → calcul (span paramétré) | Le span figé « 35 j / 5 lignes » ne sait pas rétrécir à 7 j / 1 ligne ; force le paramètre **vue → span**. **Driver.** | ✅ GREEN |
| 2 | `Should_Projeter_28_jours_du_lundi_08_06_au_dimanche_05_07_2026_en_4_lignes_de_semaine_When_la_vue_choisie_est_4_semaines_glissantes_a_l_ancre_du_08_06_2026` | calcul → table à 3 valeurs | Un span binaire « 7 si Semaine sinon 35 » échoue sur 28 j / 4 lignes ; force la 3ᵉ taille (et fixe le futur défaut, Sc.3). **Driver.** | ✅ GREEN |
| 3 | `Should_Projeter_les_semaines_ISO_entieres_recouvrant_juin_2026_du_lundi_01_06_au_dimanche_05_07_en_5_lignes_When_la_vue_choisie_est_Mois_a_l_ancre_du_10_06_2026` | table → ancrage mensuel | Un span ancré sur le lundi de la **semaine** de l'ancre ne couvre pas le **mois** (01/06 précède l'ancre 08/06) ; force l'ancrage « lundi de la semaine du 1ᵉʳ → dimanche de la semaine du dernier jour du mois ». **Driver** (le plus complexe). | ✅ GREEN |
| 4 | `Should_Resoudre_chaque_case_par_priorite_surcharge_puis_fond_puis_neutre_a_sa_propre_date_When_la_vue_change_sans_modifier_les_periodes_ni_le_cycle` | (aucune — caractérisation) | ⚠️ probablement early green — couvert par la résolution **par date** déjà acquise (`CaseJourAu` / `ResponsableDeFond(date)` inchangés) ; filet de non-régression, pas un driver. | ✅ GREEN (caractérisation) |

## Fichiers à créer / modifier

- `src/PlanningDeGarde.Application/Classes/GrilleAgendaQuery.cs` — nouvelle surface `Projeter(DateOnly ancre, VuePlanning vue)` calculant le span (Semaine 7 j / 4 sem. 28 j / Mois = semaines ISO du mois). Le défaut (sans vue) = **4 semaines** (porté par Sc.3).
- `src/PlanningDeGarde.Application/…/VuePlanning` (enum/VO : `Semaine`, `QuatreSemaines`, `Mois`) — type framework-free de l'Application.
- `tests/PlanningDeGarde.Tests/Scenario_BasculerVuesPlanning.cs` (ou nom équivalent) — projection doublée à la main (Fakes existants), date d'ancre injectée (jamais `DateTime.Now`).
- *(wiring, hors unit)* `src/PlanningDeGarde.Api/Controllers/CanalLecture.cs` — endpoint `GET /api/grille/{annee}/{mois}/{jour}` étendu d'un **paramètre vue** (query/segment), passé à `Projeter`. Acceptation HTTP dans `Api.Tests/CanalLectureApiTests` (couche Api, hors liste unit).

## Design notes

- **Le seul backend neuf du bloc A = la dimension vue/span.** La re-résolution du fond à la date naviguée
  est **déjà** assurée par `CaseJourAu` (résolution `ResponsableDeFond(date)` par date) : décaler l'ancre
  re-résout le fond mécaniquement. Ne pas re-tester la re-résolution comme un driver (caractérisation #4).
- **« Mois » est ancré au mois, pas à la semaine de l'ancre** : pour juin 2026, fenêtre 01/06→05/07 (le
  01/06 est un lundi, le 30/06 tombe dans la semaine finissant le 05/07). L'ancre lundi vaut pour Semaine /
  4 semaines ; Mois recadre sur le mois calendaire de l'ancre. **→ remonter au CP si ambigu** (convention
  « semaines ISO entières recouvrant le mois » vs « jours du mois seuls »).
- **Bascule du défaut 5 → 4 semaines** : les tests structurels existants figeant 35 cases / 5 lignes
  migrent vers 28 cases / 4 lignes (re-pointage mécanique dicté par Sc.3) — re-pointage attendu, **pas une
  régression**. Vérifier `Scenario_GrilleStructure5Semaines` et `Scenario_SlotBorneHauteFenetre`.
- **Endpoint** : le paramètre vue voyage sur le canal de **lecture** (CQRS) — ne déclenche **jamais** la
  diffusion. Compatibilité ascendante : sans vue → défaut 4 semaines.
