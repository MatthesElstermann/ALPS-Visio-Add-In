using System;
using System.Drawing;
using System.Windows.Forms;

namespace ALPS_Visio_AddIn_rewrite.UI
{
    /// <summary>Ampel-Status eines Ergebnis-Dialogs.</summary>
    public enum ResultStatus { Success, Warning, Error, Info }

    /// <summary>
    /// Einheitlicher, benutzerfreundlicher Ergebnis-/Meldungsdialog fuer alle Add-In-Funktionen
    /// (BPMN-Export, ALPS-Verifikation, NL-Checker). Ersetzt die frueheren rohen Text-Dumps und
    /// nackten <see cref="MessageBox"/>-Aufrufe durch einen farbigen Status-Header mit Symbol,
    /// Titel und optionalem Detail-/Report-Bereich.
    ///
    /// Zwei Darstellungsformen, je nach Inhalt:
    /// - Kurzmeldung (z. B. Erfolg): Header + kurze, umbrechende Nachricht.
    /// - Report (z. B. Verifikations-Ausgabe): Header + scrollbarer Monospace-Bereich.
    /// Zusaetzliche Aktions-Buttons (z. B. „Ordner oeffnen") lassen sich vor dem Anzeigen ergaenzen.
    ///
    /// Layout-Regeln: Die Fensterbreite steht je Darstellungsform fest; Titel (eine Zeile,
    /// Ellipsis) und Untertitel (umbrechend, Hoehe wird gemessen) richten sich danach — so
    /// laeuft kein Text mehr aus dem Fenster (frueher schnitt der AutoSize-Untertitel rechts ab).
    /// </summary>
    public class ResultDialog : Form
    {
        private readonly FlowLayoutPanel _buttonRow;

        public ResultDialog(ResultStatus status, string title, string subtitle = null,
            string body = null, bool bodyIsReport = false)
        {
            Color accent, headerBack;
            string symbol;
            SymbolAndColors(status, out symbol, out accent, out headerBack);

            bool hasBody = !string.IsNullOrEmpty(body);
            // Feste Breite je Darstellungsform — Grundlage fuer Umbruch/Ellipsis im Header.
            int clientWidth = hasBody ? (bodyIsReport ? 860 : 580) : 480;
            const int textLeft = 70;
            int textWidth = clientWidth - textLeft - 20;

            SuspendLayout();

            // --- Kopf-Bereich: Statusfarbe + Symbol + Titel/Untertitel -------------------------
            var symbolLabel = new Label
            {
                Text = symbol,
                Font = new Font("Segoe UI Symbol", 22F, FontStyle.Regular),
                ForeColor = accent,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(16, 10),
                Size = new Size(44, 44)
            };
            var titleLabel = new Label
            {
                Text = title ?? "",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 33, 33),
                AutoSize = false,
                AutoEllipsis = true,
                Bounds = new Rectangle(textLeft, 14, textWidth, 24)
            };

            // Untertitel bricht um; seine gemessene Hoehe bestimmt die Header-Hoehe.
            int headerHeight = 62;
            Label subtitleLabel = null;
            if (!string.IsNullOrEmpty(subtitle))
            {
                subtitleLabel = new Label
                {
                    Text = subtitle,
                    Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                    ForeColor = Color.FromArgb(97, 97, 97),
                    AutoSize = false,
                    Location = new Point(textLeft + 1, 42)
                };
                int subtitleHeight = TextRenderer.MeasureText(subtitle, subtitleLabel.Font,
                    new Size(textWidth, int.MaxValue), TextFormatFlags.WordBreak).Height;
                subtitleLabel.Size = new Size(textWidth, subtitleHeight);
                headerHeight = Math.Max(headerHeight, 42 + subtitleHeight + 12);
            }

            var header = new Panel { Dock = DockStyle.Top, Height = headerHeight, BackColor = headerBack };
            header.Controls.Add(titleLabel);
            header.Controls.Add(symbolLabel);
            if (subtitleLabel != null)
                header.Controls.Add(subtitleLabel);

            // --- Button-Zeile unten ------------------------------------------------------------
            var buttonBar = new Panel { Dock = DockStyle.Bottom, Height = 52, BackColor = SystemColors.Control };
            _buttonRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 10, 12, 10),
                WrapContents = false
            };
            var closeButton = new Button
            {
                Text = "Schließen",
                DialogResult = DialogResult.OK,
                AutoSize = true,
                Padding = new Padding(10, 2, 10, 2),
                Margin = new Padding(6, 0, 0, 0)
            };
            _buttonRow.Controls.Add(closeButton);
            buttonBar.Controls.Add(_buttonRow);

            // --- Inhalts-Bereich (nur wenn body vorhanden) -------------------------------------
            int bodyHeight = 0;
            if (hasBody)
            {
                var content = new RichTextBox
                {
                    Dock = DockStyle.Fill,
                    ReadOnly = true,
                    BorderStyle = BorderStyle.None,
                    BackColor = Color.White,
                    Text = body,
                    Font = bodyIsReport
                        ? new Font("Consolas", 9.5F)
                        : new Font("Segoe UI", 10F),
                    WordWrap = !bodyIsReport
                };
                // Ein leichtes Innenabstands-Panel, damit der Text nicht am Rand klebt.
                var pad = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 12, 12, 4), BackColor = Color.White };
                pad.Controls.Add(content);
                Controls.Add(pad);      // Fill zuerst hinzufuegen (innerster Docking-Bereich)

                if (bodyIsReport)
                {
                    bodyHeight = 520;
                }
                else
                {
                    // Kurztext: Hoehe am Inhalt ausrichten (mit Ober-/Untergrenze), damit
                    // weder Leerraum bleibt noch unnoetig gescrollt werden muss.
                    int measured = TextRenderer.MeasureText(body, content.Font,
                        new Size(clientWidth - 16 - 12 - 8, int.MaxValue), TextFormatFlags.WordBreak).Height;
                    bodyHeight = Math.Max(90, Math.Min(measured + 28, 420));
                }
            }
            Controls.Add(header);
            Controls.Add(buttonBar);

            // --- Fenster-Rahmen ----------------------------------------------------------------
            AcceptButton = closeButton;
            CancelButton = closeButton;
            AutoScaleMode = AutoScaleMode.Font;
            AutoScaleDimensions = new SizeF(6F, 13F);
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = false;
            MinimizeBox = false;
            Text = title ?? "ALPS Visio";

            if (hasBody && bodyIsReport)
            {
                MaximizeBox = true;
                FormBorderStyle = FormBorderStyle.Sizable;
                ClientSize = new Size(clientWidth, headerHeight + bodyHeight + buttonBar.Height);
                MinimumSize = new Size(520, 320);
            }
            else
            {
                MaximizeBox = false;
                FormBorderStyle = FormBorderStyle.FixedDialog;
                ClientSize = new Size(clientWidth, headerHeight + bodyHeight + buttonBar.Height + (hasBody ? 0 : 12));
            }

            ResumeLayout(false);
        }

        /// <summary>Fuegt links neben „Schließen" einen weiteren Aktions-Button ein.</summary>
        public ResultDialog AddActionButton(string text, Action onClick)
        {
            var button = new Button
            {
                Text = text,
                AutoSize = true,
                Padding = new Padding(10, 2, 10, 2),
                Margin = new Padding(6, 0, 0, 0)
            };
            button.Click += (s, e) => { try { onClick?.Invoke(); } catch { /* Aktion darf den Dialog nicht sprengen */ } };
            _buttonRow.Controls.Add(button);
            return this;
        }

        private static void SymbolAndColors(ResultStatus status, out string symbol, out Color accent, out Color headerBack)
        {
            switch (status)
            {
                case ResultStatus.Success:
                    symbol = "✔"; accent = Color.FromArgb(46, 125, 50); headerBack = Color.FromArgb(232, 245, 233); break;
                case ResultStatus.Warning:
                    symbol = "⚠"; accent = Color.FromArgb(230, 118, 0); headerBack = Color.FromArgb(255, 248, 225); break;
                case ResultStatus.Error:
                    symbol = "✖"; accent = Color.FromArgb(198, 40, 40); headerBack = Color.FromArgb(253, 236, 234); break;
                default:
                    symbol = "ℹ"; accent = Color.FromArgb(21, 101, 192); headerBack = Color.FromArgb(232, 240, 254); break;
            }
        }

        // --- Bequeme statische Helfer ---------------------------------------------------------

        public static void ShowSuccess(string title, string subtitle = null, string body = null)
            => new ResultDialog(ResultStatus.Success, title, subtitle, body, false).ShowDialog();

        public static void ShowWarning(string title, string subtitle = null, string body = null)
            => new ResultDialog(ResultStatus.Warning, title, subtitle, body, false).ShowDialog();

        public static void ShowError(string title, string subtitle = null, string body = null)
            => new ResultDialog(ResultStatus.Error, title, subtitle, body, true).ShowDialog();

        public static void ShowInfo(string title, string subtitle = null, string body = null)
            => new ResultDialog(ResultStatus.Info, title, subtitle, body, false).ShowDialog();
    }
}
