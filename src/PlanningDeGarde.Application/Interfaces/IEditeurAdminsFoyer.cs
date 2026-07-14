namespace PlanningDeGarde.Application;

/// <summary>
/// Port d'<b>écriture</b> des admins du foyer (miroir écriture d'<see cref="IEnumerationAdminsFoyer"/>) :
/// persiste la désignation d'un acteur comme admin, sur son id stable. Réalisé par le store mutable
/// en Infrastructure (InMemory tests / Mongo runtime, bornés à la config foyer), consommé par le
/// handler de désignation d'admin APRÈS validation de l'invariant admin=parent par l'agrégat
/// <c>AdministrationFoyer</c> (aucune écriture d'un admin non-Parent).
/// </summary>
public interface IEditeurAdminsFoyer
{
    /// <summary>Persiste la désignation de l'acteur comme admin du foyer (idempotent : un acteur déjà
    /// admin reste un unique admin).</summary>
    void DesignerAdmin(string acteurId);

    /// <summary>Retire la désignation d'admin de l'acteur identifié (sens OFF, s41) : il cesse d'être
    /// admin du foyer. Miroir écriture de <see cref="DesignerAdmin"/>. Tolérant à l'absence / à un
    /// acteur déjà non-admin (no-op qui réussit — idempotence). La borne « dernier admin » et le refus
    /// d'un acteur inconnu sont portés par le handler / l'agrégat AVANT écriture (le store ne fait que
    /// persister une dé-désignation déjà validée).</summary>
    void DeDesignerAdmin(string acteurId);
}
