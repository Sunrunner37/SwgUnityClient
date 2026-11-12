using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Swg.Client.Runtime.Localization
{
    public sealed class ProseFormatter
    {
        static readonly Regex placeholders = new Regex(@"%[A-Z]{2}", RegexOptions.Compiled);
        // SWG prose templates substitute user (%TU), target (%TT), object (%TO), integer (%DI), and float (%DF) parameters into localized text.
        static readonly Dictionary<string, Func<ProseParameters, string>> tokens = new Dictionary<string, Func<ProseParameters, string>>(StringComparer.Ordinal)
        {
            ["%TU"] = prose => prose.User ?? "",
            ["%TT"] = prose => prose.Target ?? "",
            ["%TO"] = prose => prose.Object ?? "",
            ["%DI"] = prose => prose.Integer.ToString(CultureInfo.InvariantCulture),
            ["%DF"] = prose => prose.Float.ToString(CultureInfo.InvariantCulture)
        };

        public string Format(string template, ProseParameters parameters)
            => placeholders.Replace(template, match => tokens.TryGetValue(match.Value, out var resolve) ? resolve(parameters) : match.Value);
    }
}
