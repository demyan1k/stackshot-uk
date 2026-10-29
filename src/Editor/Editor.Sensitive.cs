// Stackshot - Editor: "Datos sensibles" finds personal data in the image (Windows' OCR, on this computer) and covers it
// with Redact marks the user can remove; a capture covered automatically opens with its covers as such marks.
// MIT License - https://github.com/rubenitx/stackshot
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace Stackshot
{
    public partial class Editor
    {
        Bar.Item sensitiveItem;
        bool sensitiveBusy;
        System.Windows.Forms.Timer sensitiveWait, sensitiveFlash;

        // The button (images only, called from the constructor), and the covers of an automatic capture brought back once
        // the window is up.
        void SensitiveSetup()
        {
            sensitiveItem = Button(L.T("Datos sensibles"), "shield", L.T("Buscar y tapar DNI, NIE, n.\u00BA de la Seguridad Social, IBAN, tarjetas, correos y tel\u00E9fonos. Se leen en tu equipo y puedes quitar cualquier marca  (Ctrl+May\u00FAs+X)"), FindSensitive, true);
            sensitiveItem.Alt = L.T("Buscando\u2026");
            sensitiveItem.Optional = true; // Ctrl+Shift+X
            Shown += delegate { RestoreCovers(); };
            FormClosed += delegate
            {
                if (sensitiveWait != null) sensitiveWait.Dispose();
                if (sensitiveFlash != null) sensitiveFlash.Dispose();
            };
        }

        // Reads the image as it is now (crop and marks, without the backdrop, and under the Redact marks too) on a worker
        // thread and covers what it finds that is not covered yet.
        void FindSensitive()
        {
            if (IsVideo || sensitiveBusy || Exporting || sensitiveItem == null) return;
            canvas.CommitText();
            Bitmap plain;
            Point origin;
            try { plain = canvas.RenderPlain(out origin); }
            catch (Exception ex)
            {
                ShotStack.Log("Datos sensibles (editor): " + ex.Message);
                SensitiveSay(L.T("No se ha podido leer la imagen."));
                return;
            }
            SetSensitiveBusy(true);
            Flash(L.T("Buscando datos sensibles\u2026"), false);
            Thread t = new Thread(delegate()
            {
                List<SensitiveFinding> found = null;
                string error = null;
                try
                {
                    using (plain)
                    {
                        List<Ocr.Word> words = Ocr.ReadWords(plain, out error);
                        if (words != null) found = Sensitive.Find(words, plain.Size);
                    }
                }
                catch (Exception ex)
                {
                    ShotStack.Log("Datos sensibles (editor): " + ex);
                    error = ex is OutOfMemoryException ? L.T("No hay memoria suficiente para una imagen tan grande.") : L.T("No se ha podido leer la imagen.");
                    found = null;
                }
                try { BeginInvoke((Action)delegate { SensitiveFound(found, error, origin); }); }
                catch (Exception) { } // closed meanwhile
            });
            t.IsBackground = true;
            t.SetApartmentState(ApartmentState.MTA);
            try { t.Start(); }
            catch (Exception ex)
            {
                ShotStack.Log("Datos sensibles (editor): " + ex.Message);
                plain.Dispose();
                SetSensitiveBusy(false);
                SensitiveSay(L.T("No se han podido buscar los datos sensibles. Vuelve a intentarlo."));
            }
        }

        void SetSensitiveBusy(bool on)
        {
            sensitiveBusy = on;
            sensitiveItem.Enabled = !on;
            sensitiveItem.ShowAlt = on;
            bar.Sync();
        }

        // Back on the UI thread. A mark being drawn or moved, or a text being typed, is finished first: the covers wait.
        void SensitiveFound(List<SensitiveFinding> found, string error, Point origin)
        {
            if (IsDisposed) return;
            if (canvas.Busy || canvas.Typing)
            {
                if (sensitiveWait == null)
                {
                    sensitiveWait = new System.Windows.Forms.Timer();
                    sensitiveWait.Interval = 150;
                }
                EventHandler tick = null;
                tick = delegate
                {
                    if (canvas.Busy || canvas.Typing) return;
                    sensitiveWait.Stop();
                    sensitiveWait.Tick -= tick;
                    SensitiveFound(found, error, origin);
                };
                sensitiveWait.Tick += tick;
                sensitiveWait.Start();
                return;
            }
            SetSensitiveBusy(false);
            if (found == null)
            {
                SensitiveSay(L.F("No se han podido buscar los datos sensibles. {0}", error ?? L.T("No se ha podido leer la imagen.")));
                return;
            }
            List<Rectangle> boxes = new List<Rectangle>();
            foreach (SensitiveFinding f in found)
            {
                Rectangle r = f.Box;
                r.Offset(origin);
                boxes.Add(r);
            }
            int already = boxes.Count > 0 ? canvas.CoveredAlready(boxes) : 0;
            int added = boxes.Count > 0 ? canvas.AddCovers(boxes) : 0;
            // Counted by data found, not by marks (two in one word share a mark).
            SensitiveSay(SensitiveResult(found, added > 0 ? found.Count - already : 0));
        }

        // What the hint strip says after a search. added: how many of found this search covered (the rest already were).
        internal static string SensitiveResult(IList<SensitiveFinding> found, int added)
        {
            if (found.Count == 0) return L.T("No he encontrado datos sensibles.");
            if (added == 0) return L.T("Los datos sensibles ya estaban tapados.");
            int n = found.Count;
            if (added < n)
            {
                string rest = n - added == 1 ? L.T("el otro ya lo estaba.") : L.F("los otros {0} ya lo estaban.", n - added);
                return (added == 1 ? L.T("He tapado 1 dato sensible m\u00E1s; ") : L.F("He tapado {0} datos sensibles m\u00E1s; ", added)) + rest;
            }
            return L.F("{0}: {1}{2}", n == 1 ? L.T("1 dato sensible tapado") : L.F("{0} datos sensibles tapados", n), Sensitive.Summary(found),
                       n == 1 ? L.T(". Puedes quitarlo.") : L.T(". Puedes quitar cualquiera."));
        }

        // A longer message than Flash's usual: it stays a few seconds, unless something else is shown meanwhile.
        void SensitiveSay(string text)
        {
            Flash(text, false);
            if (sensitiveFlash == null)
            {
                sensitiveFlash = new System.Windows.Forms.Timer();
                sensitiveFlash.Interval = 6000;
                sensitiveFlash.Tick += delegate
                {
                    sensitiveFlash.Stop();
                    if (flashText == (string)sensitiveFlash.Tag) Flash(null, false);
                };
            }
            sensitiveFlash.Stop();
            sensitiveFlash.Tag = text;
            sensitiveFlash.Start();
        }

        // A capture covered when it was taken opens with its original pixels and the covers as marks that can be removed,
        // when the file is still exactly that capture. Read off the UI thread; nothing changes if the user started editing.
        void RestoreCovers()
        {
            if (owner == null || !InTemp || !CoverStore.Has(path)) return;
            string file = path;
            ThreadPool.QueueUserWorkItem(delegate
            {
                Bitmap original = null;
                List<Rectangle> boxes = null;
                try { original = CoverStore.Restore(file, out boxes); }
                catch (Exception ex) { ShotStack.Log("Datos sensibles (editor): " + ex.Message); }
                if (original == null) return;
                try
                {
                    BeginInvoke((Action)delegate
                    {
                        if (IsDisposed || dirty) { original.Dispose(); return; }
                        if (canvas.ShowCovers(original, boxes))
                            SensitiveSay(boxes.Count == 1 ? L.T("El dato sensible se tap\u00F3 al capturar: es una marca y puedes quitarla.") : L.T("Los datos sensibles se taparon al capturar: son marcas y puedes quitar cualquiera."));
                    });
                }
                catch (Exception) { original.Dispose(); }
            });
        }
    }
}
