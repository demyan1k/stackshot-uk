// Stackshot - Settings sections and their rows (switches, segmented controls, hotkeys, folder).
// MIT License - https://github.com/rubenitx/stackshot
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using D = System.Drawing;
using WF = System.Windows.Forms;

namespace Stackshot
{
    public partial class HomeWindow
    {
        static string[] actionNames;
        static string[] ActionNames { get { return actionNames ?? (actionNames = new string[] { L.T("Capturar un \u00E1rea"), L.T("Repetir la \u00FAltima \u00E1rea"), L.T("Capturar la pantalla"), L.T("Capturar la ventana activa"), L.T("Captura con desplazamiento"), L.T("Copiar texto de la pantalla"), L.T("Grabar v\u00EDdeo"), L.T("Grabar GIF"), L.T("Ver y ordenar Markdown"), L.T("Capturar con temporizador") }); } }
        static string[] actionShort;
        static string[] ActionShort { get { return actionShort ?? (actionShort = new string[] { L.T("Un \u00E1rea"), L.T("La \u00FAltima \u00E1rea"), L.T("Pantalla completa"), L.T("Una ventana"), L.T("Con desplazamiento"), L.T("Copiar texto"), L.T("V\u00EDdeo"), L.T("GIF animado"), "Markdown", L.T("Con temporizador") }); } }
        static readonly string[] ActionIcons = { "area", "update", "screen", "window", "scroll", "textscan", "video", "gif", "markdown", "clock" };
        static readonly D.Color[,] ActionColors =
        {
            { D.Color.FromArgb(64, 156, 255), D.Color.FromArgb(10, 110, 230) },
            { D.Color.FromArgb(100, 180, 255), D.Color.FromArgb(48, 118, 236) },
            { D.Color.FromArgb(90, 200, 250), D.Color.FromArgb(0, 122, 255) },
            { D.Color.FromArgb(191, 90, 242), D.Color.FromArgb(120, 70, 220) },
            { D.Color.FromArgb(52, 199, 89), D.Color.FromArgb(0, 160, 120) },
            { D.Color.FromArgb(100, 210, 200), D.Color.FromArgb(20, 150, 170) },
            { D.Color.FromArgb(255, 85, 100), D.Color.FromArgb(230, 40, 40) },
            { D.Color.FromArgb(255, 179, 64), D.Color.FromArgb(245, 110, 40) },
            { D.Color.FromArgb(140, 150, 255), D.Color.FromArgb(88, 86, 214) },
            { D.Color.FromArgb(255, 159, 10), D.Color.FromArgb(235, 100, 20) }
        };
        static readonly D.Color Gray1 = D.Color.FromArgb(152, 152, 157), Gray2 = D.Color.FromArgb(110, 110, 115);
        static string[] profileNames;
        static string[] ProfileNames { get { return profileNames ?? (profileNames = new string[] { L.T("Rendimiento"), L.T("Equilibrado"), L.T("Completo") }); } }

        string keysMoved;   // one-off note after a combo moved from one action to another

        // ---- Page building blocks

        // Builds one part of a page; if it fails it is logged and left out whole (no caption without its card), and what
        // follows still shows.
        void Section(string what, Action build)
        {
            int had = body.Children.Count;
            try { build(); }
            catch (Exception ex)
            {
                ShotStack.Log("P\u00E1gina " + page + ", " + what + ": " + ex.Message);
                while (body.Children.Count > had) body.Children.RemoveAt(body.Children.Count - 1);
            }
        }

        void Header(string title, string sub)
        {
            TextBlock t = Label(title, Ds.Title, 26, Ds.Brushes.Label, new Thickness(0, 58, 0, 0));
            body.Children.Add(t);
            if (sub != null)
            {
                TextBlock d = Paragraph(sub, 13, Ds.Brushes.Label2);
                d.Margin = new Thickness(0, 6, 0, 0);
                body.Children.Add(d);
            }
            body.Children.Add(new Border { Height = 24 });
        }

        // macOS Settings-style group: a small caption above, rows inside a rounded card.
        void Group(string caption, params Row[] rows)
        {
            Palette pal = Ds.Brushes;
            if (caption != null) body.Children.Add(Label(caption, Ds.Semibold, 12.5, pal.Label2, new Thickness(8, 0, 0, 8)));
            StackPanel list = new StackPanel();
            bool first = true;
            foreach (Row r in rows)
            {
                if (r == null) continue;
                r.W = this;
                r.Build();
                if (!first) list.Children.Add(new Border { Height = 1, Background = Ds.Brush(pal.Separator), Margin = new Thickness(r.Icon != null ? 54 : 14, 0, 0, 0) });
                list.Children.Add(r);
                first = false;
            }
            Border card = new Border
            {
                CornerRadius = new CornerRadius(10),
                Background = Ds.Brush(pal.Group),
                BorderBrush = Ds.Brush(pal.High ? pal.Hairline : pal.Dark ? Ds.Argb(0.07, 255, 255, 255) : Ds.Argb(0.07, 0, 0, 0)),
                BorderThickness = new Thickness(1),
                Child = list
            };
            Grid holder = new Grid { Margin = new Thickness(0, 0, 0, 24) };
            if (!pal.Dark && !pal.High) holder.Children.Add(new SoftShadow(10, 3, 0.5, 0.06));
            holder.Children.Add(card);
            body.Children.Add(holder);
            SearchGroup(caption, holder, rows);
        }

        void Note(string text)
        {
            TextBlock n = Paragraph(text, 12, Ds.Brushes.Label2);
            n.Margin = new Thickness(8, -14, 8, 24);
            body.Children.Add(n);
        }

        // A note that ends in a quiet link (an action that is rarely needed, so no button for it).
        void NoteLink(string text, string link, Action act)
        {
            TextBlock n = Paragraph(text.Length > 0 ? text + " " : "", 12, Ds.Brushes.Label2);
            n.Margin = new Thickness(8, -14, 8, 24);
            n.Inlines.Add(InlineLink(link, act));
            body.Children.Add(n);
        }

        static System.Windows.Documents.Hyperlink InlineLink(string text, Action act)
        {
            System.Windows.Documents.Hyperlink h = new System.Windows.Documents.Hyperlink(new System.Windows.Documents.Run(text));
            h.Foreground = Ds.Brush(Ds.Brushes.Link);
            h.TextDecorations = null;
            h.Cursor = Cursors.Hand;
            h.Focusable = false;
            AccessLink(h);
            h.MouseEnter += delegate { h.TextDecorations = TextDecorations.Underline; };
            h.MouseLeave += delegate { h.TextDecorations = null; };
            // Run after the click: the action may repaint the very text this link lives in.
            h.Click += delegate { h.Dispatcher.BeginInvoke(act); };
            return h;
        }

        void Add(FrameworkElement e) { body.Children.Add(e); }

        // After a switch or a choice, rows that depend on it fade in or out (no rebuild, so the switch keeps sliding).
        void SyncRows()
        {
            if (body == null) return;
            List<Row> rows = new List<Row>();
            FindRows(body, rows);
            foreach (Row r in rows) r.Sync(true);
            if (profileNote != null && profileNote.Parent == body) PaintProfileNote();
            if (styleNote != null && styleNote.Parent == body) PaintStyleNote();
        }

        static void FindRows(DependencyObject o, List<Row> into)
        {
            Row r = o as Row;
            if (r != null) { into.Add(r); return; }
            foreach (object c in LogicalTreeHelper.GetChildren(o))
            {
                DependencyObject d = c as DependencyObject;
                if (d != null) FindRows(d, into);
            }
        }

        // ---- Rows

        // Setting row: colored icon, title and description, and the control on the right.
        public abstract class Row : Grid
        {
            public HomeWindow W;
            public string Title, Sub, Icon;
            public D.Color C1, C2;
            // Only applies while When holds; otherwise it dims, and WhenOff (if any) explains why.
            public Func<bool> When;
            public string WhenOff;
            // A description that follows the setting (null hides it), used instead of Sub.
            public Func<string> SubNow;
            protected TextBlock SubText;
            bool greyed;
            internal string SubShown { get { return SubText != null && SubText.Visibility == Visibility.Visible ? SubText.Text : null; } }

            string CurrentSub()
            {
                if (When != null && WhenOff != null && !When()) return WhenOff;
                return SubNow != null ? SubNow() : Sub;
            }

            protected Row(string title, string sub, string icon, D.Color c1, D.Color c2) { Title = title; Sub = sub; Icon = icon; C1 = c1; C2 = c2; }

            protected abstract FrameworkElement Control();

            public virtual void Build()
            {
                Children.Clear();
                ColumnDefinitions.Clear();
                Palette pal = Ds.Brushes;
                string sub = CurrentSub();
                MinHeight = sub != null ? 58 : 48;
                Margin = new Thickness(14, 0, 14, 0);
                ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                ColumnDefinitions.Add(new ColumnDefinition());
                ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                if (Icon != null)
                {
                    IconTile t = new IconTile(Icon, 28, HomeWindow.W(C1), HomeWindow.W(C2));
                    t.Margin = new Thickness(0, 0, 12, 0);
                    Children.Add(t);
                }
                StackPanel text = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 9, 14, 9) };
                Grid.SetColumn(text, 1);
                // A contrast theme shows a row that does not apply in GrayText instead of fading it.
                greyed = pal.High && When != null && !When();
                TextBlock title = Label(Title, Ds.Regular, 13.5, greyed ? pal.Label3 : pal.Label);
                title.TextTrimming = TextTrimming.CharacterEllipsis;
                text.Children.Add(title);
                SubText = null;
                if (sub != null || SubNow != null || (When != null && WhenOff != null))
                {
                    SubText = Label(sub ?? "", Ds.Regular, 12, greyed ? pal.Label3 : pal.Label2);
                    SubText.TextWrapping = TextWrapping.Wrap;
                    SubText.Margin = new Thickness(0, 2, 0, 0);
                    SubText.Visibility = sub != null ? Visibility.Visible : Visibility.Collapsed;
                    text.Children.Add(SubText);
                }
                Children.Add(text);
                FrameworkElement c = Control();
                if (c != null)
                {
                    c.VerticalAlignment = VerticalAlignment.Center;
                    Grid.SetColumn(c, 2);
                    Children.Add(c);
                    AccessRow(this, sub);
                }
                Sync(false);
            }

            // Follows the settings it depends on without being rebuilt.
            public virtual void Sync(bool animate)
            {
                if (When != null)
                {
                    bool live = When();
                    IsEnabled = live;
                    if (Ds.Brushes.High && greyed == live) { Build(); return; }
                    double to = live || Ds.Brushes.High ? 1 : 0.42;
                    // Only a change fades (every switch on the page syncs all its rows).
                    if (animate) { if (Math.Abs(Opacity - to) > 0.001) HomeWindow.Glide(this, OpacityProperty, to, 220); }
                    else { BeginAnimation(OpacityProperty, null); Opacity = to; }
                }
                if (SubText != null && (SubNow != null || WhenOff != null))
                {
                    string sub = CurrentSub();
                    SubText.Text = sub ?? "";
                    SubText.Visibility = sub != null ? Visibility.Visible : Visibility.Collapsed;
                    MinHeight = sub != null ? 58 : 48;
                    AccessRow(this, sub);
                }
            }
        }

        public class ToggleRow : Row
        {
            readonly Func<bool> get;
            readonly Action<bool> set;
            public ToggleRow(string title, string sub, string icon, D.Color c1, D.Color c2, Func<bool> get, Action<bool> set) : base(title, sub, icon, c1, c2)
            {
                this.get = get;
                this.set = set;
            }
            MacSwitch sw;
            protected override FrameworkElement Control()
            {
                sw = new MacSwitch(get());
                sw.Toggled += delegate(bool v) { set(v); W.Changed(); W.SyncRows(); };
                return sw;
            }
            // Changed elsewhere (a profile, a reset): the knob slides over.
            public override void Sync(bool animate)
            {
                base.Sync(animate);
                if (sw != null && sw.IsOn != get()) sw.IsOn = get();
            }
        }

        public class SegRow : Row
        {
            readonly string[] options;
            readonly Func<int> get;
            readonly Action<int> set;
            public SegRow(string title, string sub, string icon, D.Color c1, D.Color c2, string[] options, Func<int> get, Action<int> set) : base(title, sub, icon, c1, c2)
            {
                this.options = options;
                this.get = get;
                this.set = set;
            }
            // True while the value sits between two options (tuned elsewhere): the nearest one shows, and clicking it
            // sets it exactly.
            public Func<bool> Loose;
            MacSegmented seg;
            protected override FrameworkElement Control()
            {
                seg = new MacSegmented(options, get());
                seg.Changed += delegate(int i) { Picked(i); };
                seg.PreviewMouseLeftButtonUp += delegate(object o, MouseButtonEventArgs e)
                {
                    if (Loose == null || !seg.IsMouseCaptured || !Loose()) return;
                    Point p = e.GetPosition(seg);
                    if (!new Rect(seg.RenderSize).Contains(p)) return;
                    int i = Math.Max(0, Math.Min(options.Length - 1, (int)((p.X - 2) / Math.Max(1, (seg.ActualWidth - 4) / options.Length))));
                    if (i == seg.Selected) Picked(i); // the segment's own click ignores the option already shown
                };
                return seg;
            }
            void Picked(int i) { set(i); W.Changed(); W.SyncRows(); }
            public override void Sync(bool animate)
            {
                base.Sync(animate);
                if (seg != null) seg.Selected = get();
            }
        }

        // Row with a button on the right (none if the label is null).
        public class ButtonRow : Row
        {
            readonly string label;
            readonly Action act;
            public ButtonRow(string title, string sub, string icon, D.Color c1, D.Color c2, string label, Action act) : base(title, sub, icon, c1, c2)
            {
                this.label = label;
                this.act = act;
            }
            protected override FrameworkElement Control()
            {
                if (label == null) return null;
                MacButton b = new MacButton(label, ButtonKind.Secondary);
                b.Click += delegate { if (act != null) act(); };
                return b;
            }
        }

        // Hotkey field drawn as keycaps; click it to record a new combination. While recording it shows the modifiers
        // being held and, in orange, why a combination can't be used.
        public class HotkeyRow : Row
        {
            readonly string action;
            bool on;
            string hint;
            Border field;
            SolidColorBrush pulse;
            Color pulseColor;
            KeyEventHandler live;
            string liveShown;

            public HotkeyRow(string action, int i) : base(ActionNames[i], null, ActionIcons[i], ActionColors[i, 0], ActionColors[i, 1])
            {
                this.action = action;
                // Rebuilt away (a theme change) while recording: stop cleanly.
                Unloaded += delegate { if (on && W != null) Stop(W); };
            }

            public override void Build()
            {
                Sub = null;
                string taken = null;
                if (!on)
                    foreach (string c in Hotkeys.Split(W.settings.HotkeysFor(action)))
                        if (Hotkeys.IsBusy(action, c)) { taken = c; break; }
                if (taken != null)
                {
                    // Refused because another action of Stackshot has it (a settings file edited by hand), or another program.
                    string other = null;
                    for (int i = 0; i < Settings.Actions.Length && other == null; i++)
                    {
                        if (Settings.Actions[i] == action) continue;
                        foreach (string c in Hotkeys.Split(W.settings.HotkeysFor(Settings.Actions[i])))
                            if (Hotkeys.Same(c, taken)) { other = ActionNames[i]; break; }
                    }
                    Sub = other != null ? L.F("{0} tambi\u00E9n es de \u00AB{1}\u00BB", Hotkeys.DisplayOne(taken), other) : L.F("{0} lo est\u00E1 usando otro programa", Hotkeys.DisplayOne(taken));
                }
                base.Build();
                if (taken != null && SubText != null) SubText.Foreground = Ds.Brush(Ds.Brushes.Warn);
            }

            protected override FrameworkElement Control()
            {
                Palette pal = Ds.Brushes;
                StopPulse();
                field = new TapBorder { Width = 236, Height = 32, CornerRadius = new CornerRadius(8), Cursor = Cursors.Hand, Margin = new Thickness(0, 8, 0, 8),
                                        Do = Click, Help = on ? KeysPrompt : Hotkeys.Display(W.settings.HotkeysFor(action)) };
                if (on)
                {
                    field.BorderThickness = new Thickness(2);
                    ShowLive();
                }
                else
                {
                    field.BorderThickness = new Thickness(1);
                    field.BorderBrush = Ds.Brush(pal.Hairline);
                    field.Background = Ds.Brush(pal.Control);
                    List<string> combos = Hotkeys.Split(W.settings.HotkeysFor(action));
                    if (combos.Count == 0) field.Child = Center(Label(L.T("Sin atajo"), Ds.Regular, 12.5, pal.Label3));
                    else
                    {
                        Grid g = new Grid();
                        g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                        g.ColumnDefinitions.Add(new ColumnDefinition());
                        FrameworkElement k = Fit(new Keycaps(Hotkeys.DisplayOne(combos[0]), 20), HorizontalAlignment.Right);
                        k.Margin = new Thickness(0, 0, 6, 0);
                        Grid.SetColumn(k, 1);
                        g.Children.Add(k);
                        // Extra combos written in the settings file are kept; say so, and list them on hover.
                        if (combos.Count > 1)
                        {
                            g.Children.Add(Label(L.F("y {0} m\u00E1s", combos.Count - 1), Ds.Regular, 11.5, pal.Label2, new Thickness(11, 0, 8, 0)));
                            List<string> more = new List<string>();
                            for (int i = 1; i < combos.Count; i++) more.Add(Hotkeys.DisplayOne(combos[i]));
                            field.ToolTip = L.F("Tambi\u00E9n: {0}", string.Join(", ", more.ToArray()));
                        }
                        field.Child = g;
                    }
                    field.MouseEnter += delegate { field.Background = Ds.Brush(pal.ControlHover); };
                    field.MouseLeave += delegate { field.Background = Ds.Brush(pal.Control); };
                }
                field.MouseLeftButtonUp += delegate { Click(); };
                return field;
            }

            static FrameworkElement Center(TextBlock t)
            {
                t.HorizontalAlignment = HorizontalAlignment.Center;
                t.TextTrimming = TextTrimming.CharacterEllipsis;
                t.Margin = new Thickness(6, 0, 6, 0);
                return t;
            }

            // Keycaps at their size, or a little smaller when a long combination would not fit the field.
            static FrameworkElement Fit(Keycaps k, HorizontalAlignment h)
            {
                return new Viewbox { Child = k, Stretch = Stretch.Uniform, StretchDirection = StretchDirection.DownOnly, HorizontalAlignment = h, VerticalAlignment = VerticalAlignment.Center };
            }

            // The field's content while recording: the reason a combo was refused, the modifiers held, or the prompt.
            void ShowLive()
            {
                if (field == null || !on) return;
                Palette pal = Ds.Brushes;
                ModifierKeys m = Keyboard.Modifiers;
                // A pulsing ring in the accent while waiting, in orange while it shows why a combo was refused.
                Color ring = hint != null ? pal.Orange : pal.Accent;
                if (pulse == null || pulseColor != ring)
                {
                    StopPulse();
                    pulseColor = ring;
                    pulse = new SolidColorBrush(ring);
                    field.BorderBrush = pulse;
                    field.Background = Ds.Brush(Ds.WithAlpha(ring, 0.12));
                    ColorAnimation a = new ColorAnimation(Ds.WithAlpha(ring, 0.35), TimeSpan.FromMilliseconds(650));
                    a.AutoReverse = true;
                    // A few breaths, then a steady ring: a field left waiting doesn't keep redrawing the window.
                    a.RepeatBehavior = new RepeatBehavior(4);
                    pulse.BeginAnimation(SolidColorBrush.ColorProperty, a);
                }
                // Held modifiers repeat their key-down: only a change redraws the field.
                string state = hint != null ? "!" + hint : m.ToString();
                if (field.Child != null && state == liveShown) return;
                liveShown = state;
                if (hint != null)
                {
                    TextBlock t = Label(hint, Ds.Semibold, 12.5, pal.Warn);
                    field.ToolTip = hint;
                    field.Child = Center(t);
                    return;
                }
                field.ToolTip = null;
                if (m == ModifierKeys.None) { field.Child = Center(Label(L.T("Pulsa la combinaci\u00F3n\u2026"), Ds.Semibold, 12.5, pal.Link)); return; }
                string held = Hotkeys.DisplayMods((m & ModifierKeys.Control) != 0, (m & ModifierKeys.Shift) != 0, (m & ModifierKeys.Alt) != 0, (m & ModifierKeys.Windows) != 0);
                FrameworkElement k = Fit(new Keycaps(held + " + \u2026", 20), HorizontalAlignment.Center);
                k.Margin = new Thickness(6, 0, 6, 0);
                field.Child = k;
            }

            void Listen(bool attach)
            {
                if (attach && live == null)
                {
                    live = delegate(object o, KeyEventArgs e)
                    {
                        Key k = e.Key == Key.System ? e.SystemKey : e.Key;
                        if (!on || !IsModifier(k)) return; // other keys reach Take()
                        if (e.IsDown) hint = null;
                        ShowLive();
                    };
                    // The window marks every key handled while recording, so listen to handled ones too.
                    W.AddHandler(Keyboard.PreviewKeyDownEvent, live, true);
                    W.AddHandler(Keyboard.PreviewKeyUpEvent, live, true);
                }
                else if (!attach && live != null)
                {
                    W.RemoveHandler(Keyboard.PreviewKeyDownEvent, live);
                    W.RemoveHandler(Keyboard.PreviewKeyUpEvent, live);
                    live = null;
                }
            }

            void StopPulse()
            {
                if (pulse == null) return;
                pulse.BeginAnimation(SolidColorBrush.ColorProperty, null);
                pulse = null;
            }

            void Click()
            {
                if (on) { Stop(W); return; }
                if (W.listening != null) W.listening.Stop(W);
                on = true;
                hint = null;
                W.listening = this;
                W.owner.SuspendHotkeys();
                Build();
                Listen(true);
                W.Wake();
            }

            public void Stop(HomeWindow w)
            {
                on = false;
                hint = null;
                Listen(false);
                StopPulse();
                if (w.listening == this) w.listening = null;
                w.owner.ResumeHotkeys();
                Build();
            }

            // Like a classic hotkey box: Delete or Backspace clears it, Esc cancels; extra combos from the settings file
            // are kept. Combos that would break typing or a system shortcut everywhere are refused with the reason.
            public void Take(HomeWindow w, WF.Keys keyData)
            {
                WF.Keys key = keyData & WF.Keys.KeyCode, mods = keyData & WF.Keys.Modifiers;
                bool win = (Keyboard.Modifiers & ModifierKeys.Windows) != 0;
                // Dead keys (the accent keys) and IME keys arrive without their code: ask Windows which one is down.
                if (key == WF.Keys.None || key == WF.Keys.ProcessKey || key == WF.Keys.Packet) key = Hotkeys.HeldKey();
                if (key == WF.Keys.None || Hotkeys.IsModifierKey(key)) return;
                if (key == WF.Keys.Escape) { Stop(w); return; }
                bool clear = mods == 0 && !win && (key == WF.Keys.Delete || key == WF.Keys.Back);
                string combo = "";
                if (!clear)
                {
                    string why = Hotkeys.Problem(key, mods, win);
                    // Refused: the ring breathes again, so even the same refusal twice is seen to have been heard.
                    if (why != null) { hint = why; StopPulse(); ShowLive(); return; }
                    combo = Hotkeys.ToSetting(key | mods, win);
                }
                List<string> list = Hotkeys.Split(w.settings.HotkeysFor(action));
                if (list.Count == 0 && clear) { Stop(w); return; }
                if (!clear && list.Count > 0 && Hotkeys.Same(list[0], combo)) { Stop(w); return; } // unchanged
                if (list.Count > 0) list.RemoveAt(0);
                if (!clear)
                {
                    list.RemoveAll(delegate(string c) { return Hotkeys.Same(c, combo); });
                    list.Insert(0, combo);
                    // A combo maps to one action: take it from any other, and say so under the list.
                    for (int i = 0; i < Settings.Actions.Length; i++)
                    {
                        string a = Settings.Actions[i];
                        if (a == action) continue;
                        List<string> others = Hotkeys.Split(w.settings.HotkeysFor(a));
                        if (others.RemoveAll(delegate(string o) { return Hotkeys.Same(o, combo); }) == 0) continue;
                        w.settings.SetHotkeys(a, string.Join(", ", others.ToArray()));
                        w.keysMoved = L.F(others.Count == 0 ? "{0} era de \u00AB{1}\u00BB, que se ha quedado sin atajo." : "{0} era de \u00AB{1}\u00BB; ahora es de \u00AB{2}\u00BB.", Hotkeys.DisplayOne(combo), ActionNames[i], Title);
                    }
                }
                w.settings.SetHotkeys(action, string.Join(", ", list.ToArray()));
                w.settings.Save();
                // Choosing Print Screen means wanting it: take it back from the Snipping Tool right away.
                if (Hotkeys.Same(combo, "PrintScreen") && Installer.SnippingOwnsPrintScreen)
                {
                    Installer.FreePrintScreen();
                    w.owner.Notify(L.T("Impr Pant ya es de Stackshot"),
                                   L.T("Windows la ten\u00EDa reservada para Recortes; ya est\u00E1 liberada en tu usuario. Si a\u00FAn se abre Recortes, cierra sesi\u00F3n y vuelve a entrar."));
                }
                Stop(w);
                w.Rebuild();
            }
        }

        public class FolderRow : Row
        {
            public FolderRow() : base("", L.T("Donde van las capturas que guardas."), "folder", D.Color.FromArgb(90, 200, 250), D.Color.FromArgb(0, 122, 255)) { }
            public override void Build()
            {
                Title = ShortPath(W.settings.SaveFolder);
                base.Build();
                ToolTip = W.settings.SaveFolder;
                ToolTipService.SetInitialShowDelay(this, 700);
            }
            protected override FrameworkElement Control()
            {
                StackPanel p = new StackPanel { Orientation = Orientation.Horizontal };
                MacButton open = new MacButton(L.T("Abrir"), ButtonKind.Secondary);
                open.Click += delegate { W.owner.OpenFolder(); };
                MacButton change = new MacButton(L.T("Cambiar\u2026"), ButtonKind.Secondary);
                change.Margin = new Thickness(8, 0, 0, 0);
                change.Click += delegate
                {
                    using (WF.FolderBrowserDialog d = new WF.FolderBrowserDialog())
                    {
                        d.Description = L.T("\u00BFD\u00F3nde quieres guardar las capturas que conserves?");
                        d.ShowNewFolderButton = true;
                        try { Directory.CreateDirectory(W.settings.SaveFolder); d.SelectedPath = W.settings.SaveFolder; } catch { }
                        if (d.ShowDialog(W.Win32) == WF.DialogResult.OK) { W.settings.SaveFolder = d.SelectedPath; W.Changed(); Build(); }
                    }
                };
                p.Children.Add(open);
                p.Children.Add(change);
                return p;
            }
        }

        static int Nearest(int[] options, int v)
        {
            int best = 0;
            for (int i = 1; i < options.Length; i++) if (Math.Abs(options[i] - v) < Math.Abs(options[best] - v)) best = i;
            return best;
        }

        static string ShortPath(string p)
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).TrimEnd('\\');
            bool inside = home.Length > 0 && p.StartsWith(home, StringComparison.OrdinalIgnoreCase) && (p.Length == home.Length || p[home.Length] == '\\');
            return inside ? "~" + p.Substring(home.Length) : p;
        }

        // "a", "a y b", "a, b y c".
        static string Listed(List<string> items)
        {
            if (items.Count == 0) return "";
            if (items.Count == 1) return items[0];
            return L.F("{0} y {1}", string.Join(", ", items.GetRange(0, items.Count - 1).ToArray()), items[items.Count - 1]);
        }

        // ---- Atajos

        void BuildKeys()
        {
            Header(L.T("Atajos"), L.T("Haz clic en un atajo y pulsa la combinaci\u00F3n que quieras, con Ctrl, Alt, May\u00FAs o Win. Supr lo quita y Esc cancela."));
            if (PrintScreenToSnipping())
                Group(null, new ButtonRow(L.T("Impr Pant abre Recortes de Windows"), L.T("Para que capture con Stackshot hay que liberarla. Solo cambia en tu usuario."),
                                          "keyboard", Mac.Orange, Mac.Red, L.T("Liberar"), FreePrintScreen));
            Row[] rows = new Row[Settings.Actions.Length];
            for (int i = 0; i < rows.Length; i++) rows[i] = new HotkeyRow(Settings.Actions[i], i);
            Group(null, rows);
            if (keysMoved != null) { Note(keysMoved); keysMoved = null; }
            Note(L.T("Si otro programa (Recortes, ShareX, Lightshot\u2026) ya usa alguno, aparecer\u00E1 marcado en naranja y Stackshot te avisar\u00E1 junto al reloj."));
            if (!DefaultHotkeys())
                NoteLink("", L.T("Restaurar los atajos de serie"), delegate
                {
                    CancelListening();
                    Settings d = new Settings();
                    foreach (string a in Settings.Actions) settings.SetHotkeys(a, d.HotkeysFor(a));
                    Changed();
                    owner.ResumeHotkeys();
                    Rebuild();
                });
        }

        bool DefaultHotkeys()
        {
            Settings d = new Settings();
            foreach (string a in Settings.Actions)
            {
                List<string> mine = Hotkeys.Split(settings.HotkeysFor(a)), def = Hotkeys.Split(d.HotkeysFor(a));
                if (mine.Count != def.Count) return false;
                for (int i = 0; i < mine.Count; i++) if (!Hotkeys.Same(mine[i], def[i])) return false;
            }
            return true;
        }

        // Windows 11 opens the Snipping Tool with Print Screen, and some shortcut here uses it on its own.
        bool PrintScreenToSnipping()
        {
            bool uses = false;
            foreach (string a in Settings.Actions)
                foreach (string c in Hotkeys.Split(settings.HotkeysFor(a)))
                    if (Hotkeys.Same(c, "PrintScreen")) uses = true;
            return uses && Installer.SnippingOwnsPrintScreen;
        }

        void FreePrintScreen()
        {
            Installer.FreePrintScreen();
            owner.ResumeHotkeys();
            Rebuild();
            owner.Notify(L.T("Impr Pant ya es de Stackshot"),
                         L.T("Windows la ten\u00EDa reservada para Recortes; ya est\u00E1 liberada en tu usuario. Si a\u00FAn se abre Recortes, cierra sesi\u00F3n y vuelve a entrar."));
        }

        // ---- General

        // Presets for the settings that weigh on performance; everything else stays as the user left it. Rendimiento has no
        // mascot at all (Settings.MascotOn), Equilibrado keeps it inside the window, Completo also on the desktop.
        void ApplyProfile(int p)
        {
            settings.Profile = p;
            settings.ShowIntro = p > 0;
            settings.MascotTalks = p > 0;
            if (p > 0) settings.MascotOn = true;
            settings.MascotDesktop = p == 2;
            // Rendimiento keeps everything inside the window: nothing pops up outside it. Video is never touched.
            if (p == 0) { settings.WellToasts = false; settings.HealthTalk = false; ReleaseMascot(); }
            owner.MascotChanged();
            owner.ApplySettings();
            RefreshNav();
            BuildWidget();
        }

        TextBlock profileNote;

        // Under the profile: what it is for or, once something it sets has been changed by hand, what and a link to apply
        // it again.
        void PaintProfileNote()
        {
            if (profileNote == null) return;
            int p = Math.Max(0, Math.Min(2, settings.Profile));
            List<string> changed = ProfileChanges(p);
            profileNote.Inlines.Clear();
            if (changed.Count == 0)
            {
                profileNote.Inlines.Add(new System.Windows.Documents.Run(L.T("Un perfil solo cambia unos pocos ajustes de golpe; despu\u00E9s puedes retocar cualquiera.")));
                return;
            }
            // "Respecto a este perfil": most people never chose it, it came selected.
            profileNote.Inlines.Add(new System.Windows.Documents.Run(L.F("Has cambiado {0} respecto a este perfil. ", Listed(changed))));
            profileNote.Inlines.Add(InlineLink(L.F("Volver a aplicar \u00AB{0}\u00BB", ProfileNames[p]), delegate
            {
                ApplyProfile(p);
                Changed();
                SyncRows();
            }));
        }

        // What has been changed by hand since the profile was chosen.
        List<string> ProfileChanges(int p)
        {
            List<string> l = new List<string>();
            if (settings.ShowIntro != (p > 0)) l.Add(L.T("la animaci\u00F3n al abrir"));
            if (settings.MascotTalks != (p > 0) && !Policy.MascotForced) l.Add(L.T("los saludos de la mascota"));
            if (settings.MascotDesktop != (p == 2) && !Policy.MascotForced) l.Add(L.T("la mascota en el escritorio"));
            if (p == 0 && settings.WellToasts) l.Add(L.T("los avisos de pausas"));
            if (p == 0 && settings.HealthTalk) l.Add(L.T("los comentarios de actividad"));
            return l;
        }

        void BuildGeneral()
        {
            Header(L.T("General"), L.T("C\u00F3mo arranca Stackshot y qu\u00E9 hace cada vez que capturas."));
            string[] pdesc =
            {
                L.T("Lo justo para capturar: sin mascota (ni en la ventana ni en el escritorio, y sin gastar nada en ella), sin animaci\u00F3n al abrir y sin avisos por fuera. Ideal para equipos justos."),
                L.T("El equilibrio de siempre: animaciones suaves y la mascota solo dentro de Stackshot."),
                L.T("Todo activado: la mascota tambi\u00E9n paseando por el escritorio, sus saludos y los avisos de salud.")
            };
            // Choosing a profile moves the switches it touches on this page too (no rebuild: the choice keeps sliding).
            Group(L.T("Perfil"),
                new SegRow(L.T("Perfil r\u00E1pido"), null, "gear", Gray1, Gray2, ProfileNames,
                           delegate { return settings.Profile; }, delegate(int i) { ApplyProfile(i); })
                    { SubNow = delegate { return pdesc[Math.Max(0, Math.Min(2, settings.Profile))]; } });
            profileNote = Paragraph("", 12, Ds.Brushes.Label2);
            profileNote.Margin = new Thickness(8, -14, 8, 24);
            Add(profileNote);
            PaintProfileNote();
            Group(L.T("Apariencia"),
                new SegRow(L.T("Tema"), L.T("Claro u oscuro, o el mismo que tenga Windows en cada momento."), "sparkle", Mac.Indigo, Mac.Blue,
                           new string[] { L.T("Como Windows"), L.T("Claro"), L.T("Oscuro") },
                           delegate { return settings.Appearance; }, delegate(int i) { settings.Appearance = i; }),
                new SegRow(L.T("Vista de Inicio"), L.T("C\u00F3mo se ve la p\u00E1gina principal."), "home", Mac.Blue, Mac.Indigo,
                           HomeStyles, delegate { return settings.HomeStyle; }, delegate(int i) { settings.HomeStyle = i; }),
                new SegRow(L.T("Idioma"), L.T("Se aplica al reiniciar Stackshot."), "sparkle", Mac.Teal, Mac.Blue,
                           new string[] { L.T("Automático"), "Українська", "English" },
                           delegate { return settings.Language == L.Ukrainian ? 1 : settings.Language == L.English ? 2 : 0; },
                           delegate(int i) { settings.Language = i == 1 ? L.Ukrainian : i == 2 ? L.English : L.Auto; }));
            Group(L.T("Arranque"),
                new ToggleRow(L.T("Iniciar con Windows"), L.T("Arranca en segundo plano al encender el equipo."), "power", Mac.Green, D.Color.FromArgb(0, 160, 90),
                              delegate { return Installer.StartupEnabled; }, delegate(bool v) { Installer.SetStartup(v); }),
                new ToggleRow(L.T("Seguir en la bandeja al cerrar"), L.T("Al cerrar esta ventana, Stackshot sigue funcionando junto al reloj."), "stack", Mac.Blue, Mac.Indigo,
                              delegate { return settings.CloseToTray; }, delegate(bool v) { settings.CloseToTray = v; }),
                new ToggleRow(L.T("Animaci\u00F3n al abrir"), L.T("La bienvenida con el logo cuando abres Stackshot."), "sparkle", Mac.Purple, Mac.Indigo,
                              delegate { return settings.ShowIntro; }, delegate(bool v) { settings.ShowIntro = v; }));
            Group(L.T("Actualizaciones"), UpdateRows());
            Group(L.T("Archivos"),
                new ToggleRow(L.T("Abrir .md con Stackshot"), null, "markdown", Mac.Indigo, Mac.Purple,
                              delegate { return FileAssoc.Registered(FileAssoc.Markdown); }, delegate(bool v) { SetAssoc(FileAssoc.Markdown, v); })
                    { SubNow = delegate { return AssocNote(FileAssoc.Markdown, ".md"); } },
                new ToggleRow(L.T("Abrir .xml con Stackshot"), null, "doc", Mac.Teal, Mac.Blue,
                              delegate { return FileAssoc.Registered(FileAssoc.Xml); }, delegate(bool v) { SetAssoc(FileAssoc.Xml, v); })
                    { SubNow = delegate { return AssocNote(FileAssoc.Xml, ".xml"); } },
                new ToggleRow(L.T("Abrir .mkv con Stackshot"), null, "video", D.Color.FromArgb(64, 156, 255), D.Color.FromArgb(10, 110, 230),
                              delegate { return FileAssoc.Registered(FileAssoc.Video); }, delegate(bool v) { SetAssoc(FileAssoc.Video, v); })
                    { SubNow = delegate { return AssocNote(FileAssoc.Video, ".mkv"); } },
                new ButtonRow(L.T("Aplicaciones predeterminadas de Windows"), L.T("Ah\u00ED eliges qu\u00E9 programa abre cada tipo de archivo; Stackshot nunca lo cambia por su cuenta."), "gear", Gray1, Gray2, L.T("Abrir"), FileAssoc.OpenDefaultApps));
            // Shown only while there is something in it: recordings saved from a crash or a failed final pass.
            ButtonRow recovered = null;
            long recoveredBytes = WorkFiles.Bytes(ShotStack.RecoveredDir);
            if (recoveredBytes > 0)
                recovered = new ButtonRow(L.T("Grabaciones recuperadas"), L.F("{0} de grabaciones que no llegaron a terminar. Stackshot nunca las borra: \u00E1brelas, gu\u00E1rdalas o b\u00F3rralas t\u00FA.", Recents.Size(recoveredBytes)),
                                          "folder", Mac.Orange, Mac.Red, L.T("Abrir"), OpenRecovered);
            ButtonRow print = null;
            if (PrintScreenToSnipping())
                print = new ButtonRow(L.T("Usar la tecla Impr Pant"), L.T("Ahora la usa Recortes de Windows. Se cambia solo en tu usuario."), "keyboard", Mac.Orange, Mac.Red, L.T("Usar"), FreePrintScreen);
            Group(L.T("Recientes"),
                new ToggleRow(L.T("Mostrar capturas recientes en Inicio"), L.T("Desact\u00EDvalo y Inicio no muestra ni carga miniaturas."), "photo", Mac.Blue, Mac.Indigo,
                              delegate { return settings.ShowRecent; }, delegate(bool v) { settings.ShowRecent = v; Recents.Forget(); }),
                new ToggleRow(L.T("Buscar por el texto de las capturas"), L.T("Stackshot lee el texto de cada captura en tu equipo para que el Historial la encuentre por lo que pon\u00EDa."), "search", Mac.Indigo, Mac.Blue,
                              delegate { return settings.HistorySearch; }, delegate(bool v) { settings.HistorySearch = v; HistoryIndex.Apply(settings); }),
                HistoryIndexRowIfAny(),
                new SegRow(L.T("Guardar las temporales"), L.T("Cu\u00E1nto duran las capturas que no guardas en tu carpeta."), "stack", Mac.Teal, Mac.Blue, new string[] { L.T("1 hora"), L.T("Al cerrar"), L.T("1 d\u00EDa"), L.T("7 d\u00EDas"), L.T("30 d\u00EDas") },
                           delegate { return settings.TempKeep; }, delegate(int i) { settings.TempKeep = i; owner.Sweep(); }),
                new ButtonRow(L.T("Borrar recientes"), L.T("Quita las capturas temporales; las de tu carpeta no se tocan."), "trash", Mac.Red, Mac.Orange, L.T("Borrar"), ClearRecent),
                recovered);
            Group(L.T("Al capturar"),
                new SegRow(L.T("Despu\u00E9s de seleccionar un \u00E1rea"), L.T("Directo la env\u00EDa a la pila; Ajustar antes te deja moverla y redimensionarla viendo c\u00F3mo quedar\u00E1, y la capturas con Enter; con opciones adem\u00E1s eliges captura, v\u00EDdeo, GIF o desplazamiento."), "area", Mac.Blue, Mac.Indigo,
                           new string[] { L.T("Directo a la pila"), L.T("Ajustar antes"), L.T("Mostrar opciones") },
                           delegate { return settings.AllInOne ? 2 : settings.AdjustFirst ? 1 : 0; },
                           delegate(int i) { settings.AllInOne = i == 2; settings.AdjustFirst = i == 1; }),
                new ToggleRow(L.T("Sonido de c\u00E1mara"), L.T("Un clic suave cada vez que capturas."), "sound", Mac.Pink, Mac.Red,
                              delegate { return settings.Sound; }, delegate(bool v) { settings.Sound = v; if (v) Shutter.Play(); }),
                new ToggleRow(L.T("Copiar cada captura"), L.T("Lista para pegar con Ctrl+V nada m\u00E1s hacerla; los v\u00EDdeos y GIF, como archivo."), "copy", Mac.Teal, Mac.Blue,
                              delegate { return settings.CopyToClipboard; }, delegate(bool v) { settings.CopyToClipboard = v; }),
                new SegRow(L.T("Esconder las miniaturas"), L.T("Si no las usas, se apartan al borde y solo asoma un trocito; al pasar el rat\u00F3n por encima vuelven."), "stack", Mac.Purple, Mac.Indigo,
                           new string[] { L.T("No"), L.T("A los 3 s"), L.T("A los 5 s"), L.T("A los 10 s") },
                           delegate { int i = Array.IndexOf(Settings.TuckSeconds, settings.StackTuck); return i < 0 ? 2 : i; },
                           delegate(int i) { settings.StackTuck = Settings.TuckSeconds[Math.Max(0, Math.Min(Settings.TuckSeconds.Length - 1, i))]; owner.Untuck(); }),
                new SegRow(L.T("Captura con temporizador"), L.T("Eliges el \u00E1rea y cuenta atr\u00E1s antes de capturar: da tiempo a abrir un men\u00FA o un aviso. Esc la cancela."), "clock", Mac.Orange, Mac.Red,
                           new string[] { L.T("3 s"), L.T("5 s"), L.T("10 s") },
                           delegate { return settings.CaptureDelay == 10 ? 2 : settings.CaptureDelay == 5 ? 1 : 0; },
                           delegate(int i) { settings.CaptureDelay = i == 2 ? 10 : i == 1 ? 5 : 3; }),
                new ToggleRow(L.T("Seguir al rat\u00F3n entre pantallas"), L.T("Con varias pantallas, las miniaturas van a la del rat\u00F3n."), "screen", Mac.Indigo, Mac.Purple,
                              delegate { return settings.FollowMouse; }, delegate(bool v) { settings.FollowMouse = v; }),
                new ToggleRow(L.T("Incluir el puntero"), L.T("El puntero del rat\u00F3n aparece en la captura, donde estaba al empezarla."), "cursor", Mac.Blue, Mac.Indigo,
                              delegate { return settings.CaptureCursor; }, delegate(bool v) { settings.CaptureCursor = v; }),
                print);
            Group(L.T("Datos sensibles"),
                new ToggleRow(L.T("Tapar los datos sensibles al capturar"), L.T("DNI, NIE, n\u00FAmero de la Seguridad Social, IBAN, tarjetas, correos y tel\u00E9fonos se tapan solos en cada captura. Se leen en tu equipo; nada sale de \u00E9l."), "redact!", Mac.Red, Mac.Orange,
                              delegate { return settings.SensitiveAuto; }, delegate(bool v) { settings.SensitiveAuto = v; }));
            Note(L.T("Apagado, decides t\u00FA: en el editor, \u00ABDatos sensibles\u00BB los tapa con marcas que puedes quitar."));
            Group(L.T("Capturas guardadas"),
                new ToggleRow(L.T("Guardar todas las capturas"), L.T("Cada captura y grabaci\u00F3n va tambi\u00E9n a tu carpeta. Apagado, solo las que guardas con Guardar."), "folder", Mac.Blue, Mac.Teal,
                              delegate { return settings.SaveAll; }, delegate(bool v) { settings.SaveAll = v; }),
                new FolderRow(), new NameTemplateRow(), SubfolderRow());
            Note(L.T("Lo que no est\u00E1 en tu carpeta se borra solo seg\u00FAn lo elegido en Recientes (nunca mientras tenga miniatura o est\u00E9 en el editor)."));
            BuildAdvanced();
        }

        // The folder watch: only where it is already set up or the organization sets its folder; elsewhere nothing at all.
        void BuildAdvanced()
        {
            if (!XmlWatch.Offered(settings)) return;
            Group(L.T("Avanzado"),
                new ToggleRow(L.T("Abrir los XML nuevos de una carpeta"), null, "doc", Mac.Teal, Mac.Blue,
                              delegate { return settings.XmlWatch; }, delegate(bool v) { settings.XmlWatch = v; })
                    { SubNow = XmlWatchNote },
                new XmlFolderRow { When = delegate { return settings.XmlWatch; } });
            Note(L.T("Pensado para programas que dejan cada mensaje SOAP como un archivo XML en una carpeta. En el visor, [ y ] pasan al XML anterior o al siguiente, y \u00ABM\u00E1s opciones\u00BB lista los \u00FAltimos que han llegado."));
        }

        string XmlWatchNote()
        {
            if (!settings.XmlWatch) return L.T("Cada XML que aparezca en esa carpeta se abre solo en el visor de Stackshot, delante de todo.");
            string dir = XmlWatch.FolderFor(settings);
            if (dir == null) return L.T("Elige abajo la carpeta que vigilar.");
            bool exists = false;
            try { exists = Directory.Exists(dir); } catch { }
            if (!exists) return L.T("La carpeta no existe todav\u00EDa; Stackshot la vuelve a buscar cada minuto.");
            return L.T("Activo: los XML nuevos se abren solos en el visor, delante de todo.");
        }

        // The folder whose new XML files open in the viewer: the organization's, or the one chosen here.
        public class XmlFolderRow : Row
        {
            public XmlFolderRow() : base("", null, "folder", D.Color.FromArgb(90, 200, 250), D.Color.FromArgb(0, 122, 255)) { }
            public override void Build()
            {
                string dir = XmlWatch.FolderFor(W.settings);
                Title = dir != null ? ShortPath(dir) : L.T("Ninguna carpeta");
                Sub = XmlWatch.Preset() != null ? Policy.Managed + "." : dir != null ? L.T("Carpeta elegida por ti.") : L.T("Elige la carpeta donde aparecen los XML.");
                base.Build();
                if (dir != null) ToolTip = dir;
                ToolTipService.SetInitialShowDelay(this, 700);
            }
            protected override FrameworkElement Control()
            {
                StackPanel p = new StackPanel { Orientation = Orientation.Horizontal };
                if (XmlWatch.Preset() != null) return p;
                MacButton change = new MacButton(XmlWatch.FolderFor(W.settings) != null ? L.T("Cambiar\u2026") : L.T("Elegir\u2026"), ButtonKind.Secondary);
                change.Click += delegate
                {
                    using (WF.FolderBrowserDialog d = new WF.FolderBrowserDialog())
                    {
                        d.Description = L.T("\u00BFQu\u00E9 carpeta vigilo? Cada XML nuevo que aparezca en ella se abrir\u00E1 en el visor.");
                        d.ShowNewFolderButton = false;
                        try { string cur = XmlWatch.FolderFor(W.settings); if (cur != null && Directory.Exists(cur)) d.SelectedPath = cur; } catch { }
                        if (d.ShowDialog(W.Win32) != WF.DialogResult.OK) return;
                        W.settings.XmlWatchFolder = d.SelectedPath;
                        W.Changed();
                        W.Rebuild();
                    }
                };
                p.Children.Add(change);
                return p;
            }
        }

        // Registers or removes the association. Windows keeps the final choice to the user, so after registering its
        // default-apps page opens on Stackshot.
        void SetAssoc(AssocKind k, bool on)
        {
            if (!on) { FileAssoc.Disable(k); return; }
            if (!FileAssoc.Enable(k)) return;
            if (FileAssoc.IsDefault(k)) return;
            string ext = k.Exts[0];
            DocAlert.Ask(this, L.F("Stackshot ya aparece entre las aplicaciones que pueden abrir {0}.", ext),
                         L.F("Elige Stackshot para {0} en la ventana de Windows que se abre ahora: Windows pide que esa decisi\u00F3n la tomes t\u00FA.", ext),
                         new string[] { L.T("Abrir esa ventana") }, 0, 0);
            FileAssoc.OpenDefaultApps();
        }

        static string AssocNote(AssocKind k, string ext)
        {
            if (!FileAssoc.Registered(k))
                return k == FileAssoc.Video ? L.F("Doble clic en un {0} (los m\u00E1sters Cine, que el reproductor de Windows no abre) y se ve en el reproductor de Stackshot, a tama\u00F1o real.", ext)
                    : L.F("Doble clic en un {0} y se abre en el visor de Stackshot.", ext);
            if (FileAssoc.IsDefault(k)) return L.F("Stackshot es la aplicaci\u00F3n predeterminada para {0}.", ext);
            return L.F("Registrado, pero Windows abre los {0} con otra aplicaci\u00F3n. Elige Stackshot en Aplicaciones predeterminadas.", ext);
        }

        static void OpenRecovered()
        {
            try { System.Diagnostics.Process.Start(Native.Explorer, "\"" + ShotStack.RecoveredDir + "\""); }
            catch (Exception ex)
            {
                ShotStack.Log("Recuperadas: " + ex.Message);
                if (ShotStack.Running != null) ShotStack.Running.Notify(L.T("No se pudo abrir la carpeta"), ShotStack.RecoveredDir);
            }
        }

        void ClearRecent()
        {
            if (DocAlert.Ask(this, L.T("\u00BFBorrar las capturas temporales?"), L.T("Las guardadas en tu carpeta no se tocan."), new string[] { L.T("Borrar"), L.T("Cancelar") }, 0, 1) != 0) return;
            owner.ClearTemp();
            HistoryIndex.RequestScan();
            Rebuild();
            owner.Notify(L.T("Recientes borradas"), L.T("Se han borrado las capturas temporales. Las de tu carpeta siguen ah\u00ED."));
        }

        // Looking for updates, and installing them on their own (which needs the looking, and an installed copy that is
        // not managed by IT).
        Row[] UpdateRows()
        {
            bool msi = Installer.ManagedByMsi, installed = Installer.RunningInstalled;
            ToggleRow auto = new ToggleRow(L.T("Instalar actualizaciones autom\u00E1ticamente"),
                                           L.T("Se descargan, se comprueba su firma y se instalan solas cuando no est\u00E9s usando el equipo."),
                                           "save", Mac.Green, D.Color.FromArgb(0, 160, 90),
                                           delegate { return settings.AutoUpdate; }, delegate(bool v) { settings.AutoUpdate = v; Updater.SettingsChanged(); });
            auto.When = delegate { return !msi && installed && settings.CheckUpdates; };
            auto.WhenOff = msi ? L.T("En este equipo las instala inform\u00E1tica: Stackshot solo te avisar\u00E1 de cada versi\u00F3n nueva.")
                         : !installed ? L.T("Solo con Stackshot instalado; esta copia no puede instalarse sola.")
                         : L.T("Necesita \u00ABBuscar actualizaciones\u00BB: sin buscarlas no puede instalarlas.");
            return new Row[]
            {
                new ToggleRow(L.T("Buscar actualizaciones"), L.T("Al abrir Stackshot y cada 3 horas pregunta a GitHub si hay una versi\u00F3n nueva. No env\u00EDa nada tuyo."), "update", Mac.Teal, Mac.Blue,
                              delegate { return settings.CheckUpdates; }, delegate(bool v) { settings.CheckUpdates = v; Updater.SettingsChanged(); }),
                auto
            };
        }

        // ---- Ajustes de la mascota (reached from the Ajustes button on its card)

        void BuildMascotSettings()
        {
            Header(L.T("Ajustes de la mascota"), L.T("Su forma de ser, qu\u00E9 te dice y si te acompa\u00F1a tambi\u00E9n por el escritorio."));
            Func<bool> shown = delegate { return settings.MascotOn; };
            Group(L.T("Car\u00E1cter"),
                new ToggleRow(L.T("Mostrar la mascota"), L.T("En Inicio y abajo en la barra lateral."), "bot", Mac.Blue, Mac.Purple,
                              delegate { return settings.MascotOn; },
                              delegate(bool v)
                              {
                                  settings.MascotOn = v;
                                  if (v) { mascot.PopIn(); if (settings.MascotTalks) mascot.Greet(L.T("\u00A1He vuelto!")); }
                                  owner.MascotChanged();
                              }),
                new SegRow(L.T("Personalidad"), L.T("Lo que dice y c\u00F3mo se mueve."), "bot", Mac.Green, D.Color.FromArgb(0, 160, 90), MascotParts.Personalities,
                           delegate { return settings.MascotPersonality; },
                           delegate(int i)
                           {
                               MascotLook l = MascotLook.From(settings);
                               l.Personality = i;
                               LookChanged(l, null);
                               if (settings.MascotTalks) mascot.Say(MascotTalk.Hello(settings), 3000);
                           }) { When = shown },
                new ToggleRow(L.T("Saludos y consejos"), L.T("De vez en cuando te cuenta trucos de Stackshot."), "sparkle", Mac.Orange, Mac.Pink,
                              delegate { return settings.MascotTalks; }, delegate(bool v) { settings.MascotTalks = v; if (!v) mascot.Bubble = null; }) { When = shown },
                new ToggleRow(L.T("Disfraz de temporada"), L.T("Sin gorro, se pone la calabaza en Halloween y el de Pap\u00E1 Noel en Navidad."), "sparkle", Mac.Orange, Mac.Red,
                              delegate { return settings.MascotSeasonal; },
                              delegate(bool v) { MascotLook l = MascotLook.From(settings); l.Seasonal = v; LookChanged(l, null); }) { When = shown });
            // Each part on its own: one that fails is noted and the rest of the page (the desktop switch included) still shows.
            Section("comportamiento", BuildMascotBehavior);
            Section("bienestar", BuildWellness);
            Section("concentraci\u00F3n", BuildFocus);
            Section("salud", BuildHealth);
            Section("asistente", BuildAssist);
            Group(L.T("En el escritorio"),
                new ToggleRow(L.T("Mascota en el escritorio"), L.T("Pasea junto a la barra de tareas, te mira, juega y se echa la siesta."), "screen", Mac.Indigo, Mac.Purple,
                              delegate { return settings.MascotDesktop; }, delegate(bool v) { settings.MascotDesktop = v; owner.MascotChanged(); })
                    {
                        // Completo's alone: below it the switch is off and says where to get it (the profile is never changed here).
                        When = delegate { return settings.MascotOn && settings.Profile == 2; },
                        WhenOff = settings.Profile != 2 ? L.T("Solo con el perfil Completo (en General \u203A Perfil).") : L.T("Activa antes \u00ABMostrar la mascota\u00BB.")
                    },
                new ToggleRow(L.T("Sube a las ventanas"), L.T("De vez en cuando salta a la ventana que tienes delante y pasea por encima. Si la mueves, viaja con ella."), "window", Mac.Teal, Mac.Blue,
                              delegate { return settings.MascotClimb; }, delegate(bool v) { settings.MascotClimb = v; owner.MascotChanged(); })
                    { When = delegate { return settings.MascotOn && settings.MascotDesktop; } });
            Note(L.T("En el escritorio consume algo m\u00E1s: unos 10 MB de memoria y en torno al 1 % de CPU mientras se mueve (casi nada cuando duerme). Se esconde sola con juegos o presentaciones a pantalla completa y nunca sale en tus capturas ni al compartir pantalla. Un clic abre su men\u00FA y varios seguidos le hacen cosquillas; puedes arrastrarla a donde quieras."));
        }

        // Called after any look change: the live mascot, the desktop pet and every preview follow.
        void LookChanged(MascotLook l, string reaction)
        {
            l.ApplyTo(settings);
            mascot.Look = MascotLook.From(settings);
            owner.MascotChanged();
            if (reaction != null && settings.MascotTalks) mascot.Celebrate(reaction);
            else mascot.Celebrate(null);
            // Saved; the sidebar card is only rebuilt where it shows the mascot (here it shows the capture card, and the
            // next page builds it afresh), so a click never pays for rebuilding it.
            owner.ApplySettings();
            if (MiniMascot) BuildWidget();
            Wake();
        }

        // Shows a look on the big mascot without saving it (hovering a picker); null goes back to the saved one.
        void TryOn(MascotLook l)
        {
            mascot.Look = l ?? MascotLook.From(settings);
            RefreshStagePlate(l);
            if (l != null && settings.MascotOn) mascot.Hop(0.45);
            Wake();
        }
    }
}
