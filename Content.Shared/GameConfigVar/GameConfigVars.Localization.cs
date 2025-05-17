using Robust.Shared.Configuration;

namespace Content.Shared.GameConfigVar;

public sealed partial class GameConfigVars
{
    /// <summary>
    ///     Language used for the in-game localization.
    /// </summary>
    public static readonly CVarDef<string> ServerLanguage =
        CVarDef.Create("loc.server_language", "pt-BR", CVar.SERVER | CVar.REPLICATED);
}