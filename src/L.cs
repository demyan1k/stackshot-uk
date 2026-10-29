// Stackshot - Localization: the Spanish text in the code is the key; lang\*.tsv holds the English and Ukrainian versions.
// MIT License - https://github.com/rubenitx/stackshot
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace Stackshot
{
    public static class L
    {
        public const string Auto = "auto", English = "en", Ukrainian = "uk";

        static Dictionary<string, string> map = new Dictionary<string, string>(StringComparer.Ordinal);
        static string lang = English;
        static CultureInfo culture = CultureInfo.GetCultureInfo("en-US");

        public static string Lang { get { return lang; } }
        public static bool IsUkrainian { get { return lang == Ukrainian; } }
        public static CultureInfo Culture { get { return culture; } }

        // pref: "auto" (follow Windows: Ukrainian if the display language is Ukrainian, else English), "uk" or "en".
        public static void Init(string pref)
        {
            string l = (pref ?? Auto).Trim().ToLowerInvariant();
            if (l != English && l != Ukrainian)
            {
                string sys = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                l = sys == "uk" ? Ukrainian : English;
            }
            lang = l;
            try { culture = CultureInfo.GetCultureInfo(l == Ukrainian ? "uk-UA" : "en-US"); } catch { culture = CultureInfo.InvariantCulture; }
            map = Load(l == Ukrainian ? 2 : 1);
        }

        // Each .tsv line: spanish <TAB> english <TAB> ukrainian. \n, \t and \ are escaped; lines starting with # are comments.
        static Dictionary<string, string> Load(int column)
        {
            Dictionary<string, string> d = new Dictionary<string, string>(StringComparer.Ordinal);
            try
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                foreach (string name in asm.GetManifestResourceNames())
                {
                    if (!name.EndsWith(".tsv", StringComparison.OrdinalIgnoreCase)) continue;
                    using (Stream s = asm.GetManifestResourceStream(name))
                    using (StreamReader r = new StreamReader(s, Encoding.UTF8))
                    {
                        string line;
                        while ((line = r.ReadLine()) != null)
                        {
                            if (line.Length == 0 || line[0] == '#') continue;
                            string[] p = line.Split('\t');
                            if (p.Length <= column) continue;
                            string k = Unescape(p[0]), v = Unescape(p[column]);
                            if (k.Length == 0 || v.Length == 0 || d.ContainsKey(k)) continue;
                            d[k] = v;
                        }
                    }
                }
            }
            catch (Exception ex) { try { ShotStack.Log("Idioma: " + ex.Message); } catch { } }
            return d;
        }

        static string Unescape(string s)
        {
            if (s.IndexOf((char)92) < 0) return s;
            StringBuilder b = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == (char)92 && i + 1 < s.Length)
                {
                    char n = s[++i];
                    b.Append(n == 'n' ? '\n' : n == 't' ? '\t' : n == 'r' ? '\r' : n);
                }
                else b.Append(c);
            }
            return b.ToString();
        }

        // Translates a literal. An unknown text comes back unchanged, so a missing entry never breaks the interface.
        public static string T(string es)
        {
            if (es == null) return null;
            string v;
            return map.TryGetValue(es, out v) ? v : es;
        }

        // Translates a format string and fills it in: L.F("Área de {0} × {1} px", w, h).
        public static string F(string es, params object[] args)
        {
            try { return string.Format(culture, T(es), args); }
            catch (FormatException) { return string.Format(culture, es, args); }
        }

        // For plural nouns: picks by count. Ukrainian has three forms (1 / 2-4 / 5+), English two. forms: es-key of the one/few/many sets.
        public static string Plural(int n, string one, string few, string many)
        {
            int a = Math.Abs(n);
            if (lang == Ukrainian)
            {
                int m10 = a % 10, m100 = a % 100;
                if (m10 == 1 && m100 != 11) return T(one);
                if (m10 >= 2 && m10 <= 4 && (m100 < 12 || m100 > 14)) return T(few);
                return T(many);
            }
            return a == 1 ? T(one) : T(many);
        }
    }
}
