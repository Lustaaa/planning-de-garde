using PlanningDeGarde.Application;

namespace PlanningDeGarde.Api;

/// <summary>
/// Amorce un jeu de données de démonstration cohérent (centré sur aujourd'hui) pour que la
/// grille lue par le front WASM ne s'ouvre pas vide. Passe par les use cases réels — mêmes
/// validations qu'une saisie utilisateur. Persistance en mémoire : reparti à zéro à chaque
/// redémarrage. Porté sur l'hôte d'API détaché (le front WASM n'amorce plus rien : il consomme
/// l'API distante, qui détient le store réel).
/// </summary>
public static class SeedDonneesDemo
{
    public static WebApplication AmorcerDonneesDemo(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var sp = scope.ServiceProvider;

        var poserSlot = sp.GetRequiredService<PoserSlotHandler>();
        var affecterPeriode = sp.GetRequiredService<AffecterPeriodeHandler>();
        var definirTransfert = sp.GetRequiredService<DefinirTransfertHandler>();

        const string enfant = "Léa";
        const string parentA = "Parent A";
        const string parentB = "Parent B";

        var aujourdHui = DateTime.Today;
        var demain = aujourdHui.AddDays(1);

        // Responsabilité — Parent A jusqu'à demain, puis Parent B (responsable « maintenant » = Parent A).
        affecterPeriode.Handle(new AffecterPeriodeCommand(parentA, aujourdHui.AddDays(-2), demain));
        affecterPeriode.Handle(new AffecterPeriodeCommand(parentB, demain, aujourdHui.AddDays(4)));

        // Localisation — journée d'aujourd'hui et de demain pour Léa.
        poserSlot.Handle(new PoserSlotCommand(enfant, "école", aujourdHui.AddHours(8.5), aujourdHui.AddHours(16.5)));
        poserSlot.Handle(new PoserSlotCommand(enfant, "domicile A", aujourdHui.AddHours(16.5), aujourdHui.AddHours(20)));
        poserSlot.Handle(new PoserSlotCommand(enfant, "nounou", demain.AddHours(8.5), demain.AddHours(12)));
        poserSlot.Handle(new PoserSlotCommand(enfant, "domicile B", demain.AddHours(13), demain.AddHours(18)));

        // Transfert de bascule — Parent A dépose, Parent B récupère, à l'école en fin de journée.
        definirTransfert.Handle(new DefinirTransfertCommand(parentA, parentB, "école", TimeSpan.FromHours(16.5), aujourdHui));

        return app;
    }
}
