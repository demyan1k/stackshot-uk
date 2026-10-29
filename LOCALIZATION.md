# Localization guide (Spanish source -> English + Ukrainian)

The UI text in the code is Spanish. It is the *key*. `src/L.cs` looks it up in `lang/*.tsv`.

* `L.T("Texto")` - translate a literal.
* `L.F("Área de {0} × {1} px", w, h)` - translate a format string and fill it in (never concatenate sentence parts with `+`).
* `L.Plural(n, "1 archivo", "archivos", "archivos")` - Ukrainian needs 3 forms (1 / 2-4 / 5+), English 2. Prefer `L.F` with a `L.Plural` result.
* Literals stay ASCII-escaped as in the repo (`ñ`); the key in the .tsv is the real UTF-8 text.
* `lang/<group>.tsv` lines: `spanish<TAB>english<TAB>ukrainian`. Escape newline as `\n`, tab as `\t`, backslash as `\`. Lines starting with `#` are comments. Use only your own group's file.
* Do NOT wrap: file paths, registry keys, settings.ini keys, log messages (`ShotStack.Log`), command-line flags, format codes, brand names (Stackshot, Pixel, Cine, FFmpeg), identifiers/icon names, regexes, anything not shown to the user.
* Ukrainian: natural modern Ukrainian (not Russian calques), informal-polite "ти"/imperative like the Spanish tone (e.g. "Скопіювати", "Зберегти"). Keep placeholders {0} identical in all three columns. Keep short labels short (buttons must still fit).
* Static fields/constants initialised before `L.Init` runs must not call `L.T` (use a property/lazy getter). Check `static readonly string X = "..."` and static arrays shown in the UI.
* Strings that hold a *key* compared elsewhere (e.g. `if (mode == "Cine")`, dictionary keys, `switch` cases on display text): do not translate the compared value; translate only at display time.
