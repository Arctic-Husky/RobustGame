using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Content.Shared.GameConfigVar;
using Robust.Shared.Configuration;

namespace Content.Shared.Localization
{
    public sealed class ContentLocalizationManager
    {
        [Dependency] private readonly ILocalizationManager _loc = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        /// <summary>
        /// Custom format strings used for parsing and displaying minutes:seconds timespans.
        /// </summary>
        public static readonly string[] TimeSpanMinutesFormats = {
            @"m\:ss",
            @"mm\:ss",
            @"%m",
            @"mm"
        };

        public void Initialize()
        {
            _loc.Initialize(); // Criminoso? Para usar ReloadLocalizations, pecisa estar inicializado
            
            var culture = new CultureInfo(_cfg.GetCVar(GameConfigVars.ServerLanguage));

            _loc.LoadCulture(culture);

            /*
             * The following language functions are specific to the english localization. When working on your own
             * localization you should NOT modify these, instead add new functions specific to your language/culture.
             * This ensures the english translations continue to work as expected when fallbacks are needed.
             */
            var cultureEn = new CultureInfo("en-US");
            _loc.LoadCulture(cultureEn);
            _loc.SetFallbackCluture(cultureEn);

            _loc.AddFunction(cultureEn, "MAKEPLURAL", FormatMakePlural);
            _loc.AddFunction(cultureEn, "MANY", args => FormatMany(args, true));
            
            _cfg.OnValueChanged(GameConfigVars.ServerLanguage, OnCultureUpdate, true);
        }
        
        private void OnCultureUpdate(string value)
        {
            var culture = new CultureInfo(value);
            if (!_loc.HasCulture(culture))
                _loc.LoadCulture(culture);

            _loc.AddFunction(culture, "MAKEPLURAL", FormatMakePluralCustom);
            _loc.AddFunction(culture, "MANY", args => FormatMany(args, false));
            _loc.AddFunction(culture, "TOSTRING", args => FormatToString(culture, args));
            _loc.AddFunction(culture, "LOC", FormatLoc);
            _loc.AddFunction(culture, "NATURALFIXED", FormatNaturalFixed);
            _loc.AddFunction(culture, "NATURALPERCENT", FormatNaturalPercent);

            _loc.DefaultCulture = culture;
            _loc.ReloadLocalizations();
        }
        
        private ILocValue FormatMany(LocArgs args, bool isFallback)
        {
            var count = ((LocValueNumber) args.Args[1]).Value;

            if (Math.Abs(count - 1) < 0.0001f)
            {
                return (LocValueString) args.Args[0];
            }
            else
            {
                if(isFallback) return (LocValueString) FormatMakePlural(args);
                return (LocValueString) FormatMakePluralCustom(args);
            }
        }
        
        private static readonly Regex PluralEsRule = new("^.*(s|sh|ch|x|z)$");

        private ILocValue FormatMakePlural(LocArgs args)
        {
            var text = ((LocValueString) args.Args[0]).Value;
            var split = text.Split(" ", 1);
            var firstWord = split[0];
            if (PluralEsRule.IsMatch(firstWord))
            {
                if (split.Length == 1)
                {
                    return new LocValueString($"{firstWord}es");
                }
                    
                return new LocValueString($"{firstWord}es {split[1]}");
            }

            if (split.Length == 1)
            {
                return new LocValueString($"{firstWord}s");
            }
            
            return new LocValueString($"{firstWord}s {split[1]}");
        }

        public string FormatList(List<string> list)
        {
            return list.Count switch
            {
                <= 0 => string.Empty,
                1 => list[0],
                2 => $"{list[0]} and {list[1]}",
                _ => $"{string.Join(", ", list.GetRange(0, list.Count - 1))}, and {list[^1]}"
            };
        }

        public string FormatListToOr(List<string> list)
        {
            return list.Count switch
            {
                <= 0 => string.Empty,
                1 => list[0],
                2 => $"{list[0]} or {list[1]}",
                _ => $"{string.Join(" or ", list)}"
            };
        }
        
        private static readonly Regex PluralRegraEs = new("^.*(s|sh|ch|x|z)$");
        private static readonly Regex PluralRegraOes = new("ão$");
        private static readonly Regex PluralRegraIs = new("[aeo]l$");
        private static readonly Regex PluralRegraNs = new("m$");

        private ILocValue FormatMakePluralCustom(LocArgs args)
        {
            var text = ((LocValueString) args.Args[0]).Value;
            var split = text.Split(" ", 2);
            var firstWord = split[0];
            string plural;

            if (PluralRegraOes.IsMatch(firstWord))
                plural = firstWord[..^2] + "oẽs";
            else if (PluralRegraIs.IsMatch(firstWord))
                plural = firstWord[..^1] + "is";
            else if (PluralRegraNs.IsMatch(firstWord))
                plural = firstWord[..^1] + "ns";
            else if (PluralRegraEs.IsMatch(firstWord))
                plural = firstWord + "es";
            else
                plural = firstWord + "s";

            return split.Length == 1 ? new LocValueString(plural) : new LocValueString($"{plural} {split[1]}");
        }

        public string FormatListCustom(List<string> list)
        {
            return list.Count switch
            {
                <= 0 => string.Empty,
                1 => list[0],
                2 => $"{list[0]} e {list[1]}",
                _ => $"{string.Join(", ", list.GetRange(0, list.Count - 1))}, e {list[^1]}"
            };
        }

        public string FormatListToOrCustom(List<string> list)
        {
            return list.Count switch
            {
                <= 0 => string.Empty,
                1 => list[0],
                2 => $"{list[0]} ou {list[1]}",
                _ => $"{string.Join(" ou ", list)}"
            };
        }

        private ILocValue FormatNaturalPercent(LocArgs args)
        {
            var number = ((LocValueNumber) args.Args[0]).Value * 100;
            var maxDecimals = (int)Math.Floor(((LocValueNumber) args.Args[1]).Value);
            var formatter = (NumberFormatInfo)NumberFormatInfo.GetInstance(CultureInfo.GetCultureInfo(_cfg.GetCVar(GameConfigVars.ServerLanguage))).Clone();
            formatter.NumberDecimalDigits = maxDecimals;
            return new LocValueString(string.Format(formatter, "{0:N}", number).TrimEnd('0').TrimEnd(char.Parse(formatter.NumberDecimalSeparator)) + "%");
        }

        private ILocValue FormatNaturalFixed(LocArgs args)
        {
            var number = ((LocValueNumber) args.Args[0]).Value;
            var maxDecimals = (int)Math.Floor(((LocValueNumber) args.Args[1]).Value);
            var formatter = (NumberFormatInfo)NumberFormatInfo.GetInstance(CultureInfo.GetCultureInfo(_cfg.GetCVar(GameConfigVars.ServerLanguage))).Clone();
            formatter.NumberDecimalDigits = maxDecimals;
            return new LocValueString(string.Format(formatter, "{0:N}", number).TrimEnd('0').TrimEnd(char.Parse(formatter.NumberDecimalSeparator)));
        }

        /// <summary>
        /// Formats a direction struct as a human-readable string.
        /// </summary>
        public static string FormatDirection(Direction dir)
        {
            return Loc.GetString($"zzzz-fmt-direction-{dir.ToString()}");
        }

        private static ILocValue FormatLoc(LocArgs args)
        {
            var id = ((LocValueString) args.Args[0]).Value;

            return new LocValueString(Loc.GetString(id, args.Options.Select(x => (x.Key, x.Value.Value!)).ToArray()));
        }

        private static ILocValue FormatToString(CultureInfo culture, LocArgs args)
        {
            var arg = args.Args[0];
            var fmt = ((LocValueString) args.Args[1]).Value;

            var obj = arg.Value;
            if (obj is IFormattable formattable)
                return new LocValueString(formattable.ToString(fmt, culture));

            return new LocValueString(obj?.ToString() ?? "");
        }
    }
}
