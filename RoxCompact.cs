using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

public class RoxCompact : Form
{
    static readonly Color BgDeep = Color.FromArgb(245, 246, 248);
    static readonly Color BgCard = Color.White;
    static readonly Color BgField = Color.FromArgb(250, 251, 252);
    static readonly Color Line = Color.FromArgb(222, 226, 231);
    static readonly Color TextMain = Color.FromArgb(33, 37, 43);
    static readonly Color TextDim = Color.FromArgb(96, 105, 118);
    static readonly Color Accent = Color.FromArgb(30, 123, 224);
    static readonly Color BtnDark = Color.FromArgb(75, 85, 99);
    static readonly Color Warn = Color.FromArgb(176, 106, 0);
    static readonly Color WarnBg = Color.FromArgb(255, 246, 229);
    static readonly Color Ok = Color.FromArgb(22, 141, 104);

    const string GameDefault = @"C:\RO_SEA\RO_SEAGame\rox.exe";
    const string LauncherDefault = @"C:\RO_SEA\ROX.exe";
    const string Marker = "ScreenType_h2100444305";
    const string ScannedHash = "21D79B4BB41D0CB0E67B9E57F96BD6E4CB881D7719342E02C7DD8D84CFC95C18";

    RegistryKey key;
    bool gameRunning;

    Panel pageSize, pageArrange, pageAbout;
    Button tabSize, tabArrange, tabAbout;
    ComboBox cmbPreset;
    NumericUpDown numW, numH;

    Label lblPreview, lblSumTarget, lblSumCalc, lblSumFull, lblSumPreset;
    Label lblCount, lblArrangeResult;
    Panel preview;
    int selectedCount = 4;
    bool stacked2;
    Action refreshOrient;
    Button optSide, optStack;



    [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc cb, IntPtr p);
    delegate bool EnumProc(IntPtr h, IntPtr p);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetClassNameW(IntPtr h, System.Text.StringBuilder s, int n);
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] static extern bool MoveWindow(IntPtr h, int x, int y, int w, int ht, bool rp);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")] static extern bool IsIconic(IntPtr h);
    [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr h, int c);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] static extern IntPtr GetWindowLongPtr(IntPtr h, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] static extern IntPtr SetWindowLongPtr(IntPtr h, int index, IntPtr value);
    [DllImport("user32.dll")] static extern bool AdjustWindowRectEx(ref RECT r, uint style, bool menu, uint exStyle);
    [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags);
    [StructLayout(LayoutKind.Sequential)] struct RECT { public int L, T, R, B; }

    [STAThread]
    public static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new RoxCompact());
    }

    public RoxCompact()
    {
        key = FindRoxKey();
        gameRunning = Process.GetProcessesByName("rox").Length > 0 || Process.GetProcessesByName("ROX").Length > 0;

        Text = "ROX Window Tool";
        ClientSize = new Size(755, 421);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = BgDeep;
        Font = new Font("Segoe UI", 8F);

        BuildHeader();
        BuildTabs();

        pageSize = new Panel { Location = new Point(10, 90), Size = new Size(735, 300), BackColor = BgDeep };
        pageArrange = new Panel { Location = new Point(10, 90), Size = new Size(735, 300), BackColor = BgDeep };
        pageAbout = new Panel { Location = new Point(10, 90), Size = new Size(735, 300), BackColor = BgDeep };
        BuildSizePage();
        BuildArrangePage();
        BuildAboutPage();
        Controls.Add(pageSize);
        Controls.Add(pageArrange);
        Controls.Add(pageAbout);
        ShowPage(0);
        var footer = new Label
        {
            Text = "2026 - Kenshin Himura | Darkside TH",
            AutoSize = true,
            ForeColor = TextDim,
            BackColor = BgDeep,
            Font = new Font("Segoe UI", 7.5F)
        };
        footer.Location = new Point(ClientSize.Width - footer.PreferredWidth - 14, ClientSize.Height - footer.PreferredHeight - 8);
        footer.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        Controls.Add(footer);        foreach (string a in Environment.GetCommandLineArgs())
            if (a.Length == 6 && a.StartsWith("--tab=")) { int n; if (int.TryParse(a.Substring(6), out n)) ShowPage(n); }

        if (key != null)
        {
            int curW = GetValue("Screenmanager Resolution Width", "Screenmanager Resolution Width_h182942802", 1290);
            int curH = GetValue("Screenmanager Resolution Height", "Screenmanager Resolution Height_h2627697771", 714);
            int curMode = GetValue("Screenmanager Fullscreen mode", "Screenmanager Fullscreen mode_h3630240806", 3);
            cmbPreset.Items.Add(new Preset("Current (" + curW + " x " + curH + ")", curW, curH));
            cmbPreset.Items.Add(new Preset("1 window  — full screen (3046 x 1753)", 3046, 1753));
            cmbPreset.Items.Add(new Preset("2 windows — side by side (1510 x 1753)", 1510, 1753));
            cmbPreset.Items.Add(new Preset("3 windows — 2 x 2 grid (1510 x 841)", 1510, 841));
            cmbPreset.Items.Add(new Preset("4 windows — 2 x 2 grid (1510 x 841)", 1510, 841));
            cmbPreset.Items.Add(new Preset("5 windows — 3 x 2 grid (998 x 841)", 998, 841));
            cmbPreset.Items.Add(new Preset("6 windows — 3 x 2 grid (998 x 841)", 998, 841));
            cmbPreset.Items.Add(new Preset("Quarter screen (1510 x 841)", 1510, 841));
            cmbPreset.Items.Add(new Preset("HD (1280 x 720)", 1280, 720));
            cmbPreset.Items.Add(new Preset("HD+ (1600 x 900)", 1600, 900));
            cmbPreset.Items.Add(new Preset("Full HD (1920 x 1080)", 1920, 1080));
            numW.Value = Math.Max(numW.Minimum, Math.Min(numW.Maximum, curW));
            numH.Value = Math.Max(numH.Minimum, Math.Min(numH.Maximum, curH));
            cmbPreset.SelectedIndex = 0;
            UpdatePreview();
        }
    }

    void BuildHeader()
    {
        var head = new Panel { Location = new Point(0, 0), Size = new Size(755, 50), BackColor = BgDeep };
        head.Paint += (s, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var b = new SolidBrush(Accent))
            {
                g.FillRectangle(b, 14, 14, 9, 9);
                g.FillRectangle(b, 25, 14, 9, 9);
                g.FillRectangle(b, 14, 25, 9, 9);
                g.FillRectangle(b, 25, 25, 9, 9);
            }
            using (var f = new Font("Segoe UI", 11F, FontStyle.Bold))
                g.DrawString("ROX Window Tool", f, new SolidBrush(TextMain), 44, 9);
            using (var f = new Font("Segoe UI", 6.5F, FontStyle.Bold))
                g.DrawString("GAME WINDOW MANAGER", f, new SolidBrush(TextDim), 45, 31);
        };
        Controls.Add(head);
    }

    void BuildTabs()
    {
        tabSize = TabButton("Window Size", 0, 10, 110, true);
        tabArrange = TabButton("Arrange Windows", 1, 122, 128, false);
        tabAbout = TabButton("About Me", 2, 252, 92, false);
        Controls.Add(tabSize); Controls.Add(tabArrange); Controls.Add(tabAbout);
        tabSize.BringToFront(); tabArrange.BringToFront(); tabAbout.BringToFront();
    }

    Button TabButton(string text, int index, int left, int width, bool bold)
    {
        var b = new Button
        {
            Text = text, Location = new Point(left, 54), Size = new Size(width, 28),
            FlatStyle = FlatStyle.Flat, BackColor = BgDeep, ForeColor = TextDim,
            Font = new Font("Segoe UI", 8F, bold ? FontStyle.Bold : FontStyle.Regular),
            Cursor = Cursors.Hand
        };
        b.FlatAppearance.BorderColor = BgDeep;
        b.FlatAppearance.BorderSize = 1;
        b.Click += (s, e) => ShowPage(index);
        return b;
    }

    int current = 0;
    void ShowPage(int index)
    {
        current = index;
        pageSize.Visible = index == 0;
        pageArrange.Visible = index == 1;
        pageAbout.Visible = index == 2;
        var tabs = new Button[] { tabSize, tabArrange, tabAbout };
        for (int i = 0; i < tabs.Length; i++)
        {
            tabs[i].BackColor = (i == index) ? Color.White : BgDeep;
            tabs[i].ForeColor = (i == index) ? TextMain : TextDim;
            tabs[i].FlatAppearance.BorderColor = (i == index) ? Line : BgDeep;
        }
    }

    // ---------------------------------------------------------------- size tab
    void BuildSizePage()
    {
        if (gameRunning)
        {
            var warn = Card(new Point(0, 0), new Size(735, 24), WarnBg, Warn);
            warn.Paint += (s, e) =>
            {
                using (var b = new SolidBrush(Warn)) e.Graphics.FillEllipse(b, 8, 6, 12, 12);
                TextRenderer.DrawText(e.Graphics, "!", new Font("Segoe UI", 7F, FontStyle.Bold),
                    new Rectangle(8, 6, 12, 12), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                TextRenderer.DrawText(e.Graphics, "ROX is running — close all clients before saving, or the size will be overwritten on exit.",
                    new Font("Segoe UI", 7.5F), new Rectangle(26, 0, 700, 24), Warn, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            };
            pageSize.Controls.Add(warn);
        }

        int top = gameRunning ? 30 : 0;

        // kartu pengaturan: 120 px
        var card = Card(new Point(0, top), new Size(735, 120), BgCard, Line);
        card.Controls.Add(Lbl("Preset", 10, 9, 50));
        cmbPreset = new ComboBox
        {
            Location = new Point(1, 1), Size = new Size(344, 21),
            DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat,
            BackColor = BgField, ForeColor = TextMain, DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = 17
        };
        cmbPreset.DrawItem += (s, e) =>
        {
            if (e.Index < 0) return;
            bool sel = (e.State & DrawItemState.Selected) != 0;
            using (var b = new SolidBrush(sel ? Accent : BgField)) e.Graphics.FillRectangle(b, e.Bounds);
            using (var b = new SolidBrush(sel ? Color.White : TextMain))
                e.Graphics.DrawString(cmbPreset.Items[e.Index].ToString(), cmbPreset.Font, b, e.Bounds.X + 4, e.Bounds.Y + 2);
        };
        var comboFrame = new Panel { Location = new Point(66, 6), Size = new Size(348, 24), BackColor = Color.White };
        comboFrame.Paint += (s, e) => { using (var pen = new Pen(Line)) e.Graphics.DrawRectangle(pen, 0, 0, comboFrame.Width - 1, comboFrame.Height - 1); };
        comboFrame.Controls.Add(cmbPreset);
        card.Controls.Add(comboFrame);

        card.Controls.Add(Lbl("Width", 10, 44, 50));
        numW = Num(66, 41, 80);
        card.Controls.Add(numW);
        card.Controls.Add(Lbl("px", 150, 44, 20));
        card.Controls.Add(Lbl("Height", 190, 44, 45));
        numH = Num(244, 41, 80);
        card.Controls.Add(numH);
        card.Controls.Add(Lbl("px", 328, 44, 20));

        lblPreview = Lbl("", 66, 96, 650);
        lblPreview.ForeColor = TextDim;
        card.Controls.Add(lblPreview);
        pageSize.Controls.Add(card);

        // kartu actions: 56 px - posisi tombol absolut, vertikal dipusatkan saat layout
        var actions = Card(new Point(0, top + 128), new Size(735, 56), BgCard, Line);
        var btnSave = Primary("Launch Game", new Point(10, 12), new Size(210, 32));
        btnSave.Click += SaveAndLaunch;
        var btnCancel = Ghost("Cancel", new Point(230, 12), new Size(210, 32));
        btnCancel.DialogResult = DialogResult.Cancel;
        CancelButton = btnCancel;
        actions.Controls.Add(btnSave);
        actions.Controls.Add(btnCancel);
        actions.Layout += (s2, e2) =>
        {
            int cy = (actions.ClientSize.Height - btnSave.Height) / 2;
            if (cy < 0) cy = 0;
            btnSave.Top = cy; btnCancel.Top = cy;
        };
        pageSize.Controls.Add(actions);
        // kartu ringkasan: 62 px
        var summary = Card(new Point(0, top + 192), new Size(735, 62), BgCard, Line);
        Sum(summary, "Target:", 10, 8, out lblSumTarget);
        Sum(summary, "Calculated:", 370, 8, out lblSumCalc);
        Sum(summary, "Fullscreen:", 10, 32, out lblSumFull);
        Sum(summary, "Preset:", 370, 32, out lblSumPreset);
        pageSize.Controls.Add(summary);

        cmbPreset.SelectedIndexChanged += (s, e) =>
        {
            var p = cmbPreset.SelectedItem as Preset;
            if (p == null) return;
            numW.Value = Math.Max(numW.Minimum, Math.Min(numW.Maximum, p.W));
            numH.Value = Math.Max(numH.Minimum, Math.Min(numH.Maximum, p.H));
            UpdatePreview();
        };
        numW.ValueChanged += (s, e) => UpdatePreview();
        numH.ValueChanged += (s, e) => UpdatePreview();
    }
    NumericUpDown Num(int x, int y, int w)
    {
        var n = new NumericUpDown
        {
            Location = new Point(x, y), Size = new Size(w, 21),
            Minimum = 640, Maximum = 3840, Increment = 10,
            BackColor = BgField, ForeColor = TextMain, BorderStyle = BorderStyle.FixedSingle
        };
        foreach (var c in n.Controls) if (c is TextBox) { ((TextBox)c).BackColor = BgField; ((TextBox)c).ForeColor = TextMain; }
        return n;
    }

    void Sum(Panel parent, string label, int x, int y, out Label value)
    {
        parent.Controls.Add(new Label { Text = label, Location = new Point(x, y), Size = new Size(70, 18), ForeColor = TextDim });
        value = new Label { Text = "-", Location = new Point(x + 72, y), Size = new Size(260, 18), ForeColor = TextMain };
        parent.Controls.Add(value);
    }

    void UpdatePreview()
    {
        if (lblPreview == null || numW == null) return;
        int w = (int)numW.Value, h = (int)numH.Value;
        lblPreview.Text = "Window size including border: " + (w + 26) + " x " + (h + 71) + " px";
        lblSumTarget.Text = w + " x " + h;
        lblSumCalc.Text = (w + 26) + " x " + (h + 71);
        lblSumFull.Text = "No";
        var p = cmbPreset != null ? cmbPreset.SelectedItem as Preset : null;
        lblSumPreset.Text = p != null ? p.Name : "-";
    }
    // ---------------------------------------------------------------- arrange tab
    void BuildArrangePage()
    {
        var card = Card(new Point(0, 0), new Size(735, 140), BgCard, Line);
        card.Paint += (s, e) =>
        {
            using (var f = new Font("Segoe UI", 10F, FontStyle.Bold))
                e.Graphics.DrawString("Arrange Windows", f, new SolidBrush(TextMain), 12, 8);
            TextRenderer.DrawText(e.Graphics, "Choose how many windows to arrange. Preview the layout before applying it.",
                new Font("Segoe UI", 7.5F), new Rectangle(12, 26, 700, 18), TextDim, TextFormatFlags.Left);
        };

        var thumbs = new Panel { Location = new Point(10, 46), Size = new Size(715, 86), BackColor = BgCard };
        thumbs.Paint += (s, e) =>
        {
            for (int n = 1; n <= 6; n++)
            {
                var box = new Rectangle(2 + (n - 1) * 118, 2, 112, 68);
                bool sel = n == selectedCount;
                using (var b = new SolidBrush(sel ? Color.FromArgb(233, 236, 239) : BgField)) e.Graphics.FillRectangle(b, box);
                using (var pen = new Pen(sel ? Color.FromArgb(120, 128, 140) : Line, sel ? 2 : 1)) e.Graphics.DrawRectangle(pen, box);
                DrawLayout(e.Graphics, new Rectangle(box.X + 8, box.Y + 8, box.Width - 16, 36), n, sel);
                TextRenderer.DrawText(e.Graphics, n.ToString(), new Font("Segoe UI", 7.5F, FontStyle.Bold),
                    new Rectangle(box.X, box.Bottom - 20, box.Width, 16), sel ? TextMain : TextDim, TextFormatFlags.HorizontalCenter);
            }
        };
        thumbs.MouseClick += (s, e) =>
        {
            int n = (e.X - 2) / 118 + 1;
            if (n < 1 || n > 6) return;
            selectedCount = n; lblCount.Text = n.ToString(); thumbs.Invalidate(); preview.Invalidate();
        };
        card.Controls.Add(thumbs);
        pageSize.Controls.Add(card);
        pageArrange.Controls.Add(card);
        pageSize.Controls.Remove(card);

        var row = new Panel { Location = new Point(0, 144), Size = new Size(735, 96), BackColor = BgDeep };

        var left = Card(new Point(0, 0), new Size(330, 96), BgCard, Line);
        left.Paint += (s, e) =>
        {
            using (var f = new Font("Segoe UI", 9F, FontStyle.Bold))
                e.Graphics.DrawString("Windows", f, new SolidBrush(TextMain), 10, 8);
            TextRenderer.DrawText(e.Graphics, "Number of windows to arrange.", new Font("Segoe UI", 7F),
                new Rectangle(10, 26, 300, 16), TextDim, TextFormatFlags.Left);
        };
        var minus = Ghost("-", new Point(10, 40), new Size(34, 24));
        minus.Click += (s, e) => { selectedCount = Math.Max(1, selectedCount - 1); lblCount.Text = selectedCount.ToString(); if (refreshOrient != null) refreshOrient(); };
        left.Controls.Add(minus);
        lblCount = new Label { Text = "4", Location = new Point(50, 40), Size = new Size(48, 24), TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = TextMain };
        left.Controls.Add(lblCount);
        lblCount.TextChanged += (s, e) => { if (refreshOrient != null) refreshOrient(); };
        var plus = Ghost("+", new Point(104, 40), new Size(34, 24));
        plus.Click += (s, e) => { selectedCount = Math.Min(9, selectedCount + 1); lblCount.Text = selectedCount.ToString(); if (refreshOrient != null) refreshOrient(); };
        left.Controls.Add(plus);

        optSide = Ghost("Side by Side", new Point(10, 68), new Size(106, 22));
        optStack = Ghost("Stacked", new Point(122, 68), new Size(80, 22));
        refreshOrient = () =>
        {
            bool show = selectedCount == 2;
            optSide.Visible = show;
            optStack.Visible = show;
            if (!show) stacked2 = false;
            optSide.BackColor = stacked2 ? BgField : BtnDark;
            optSide.ForeColor = stacked2 ? TextMain : Color.White;
            optStack.BackColor = stacked2 ? BtnDark : BgField;
            optStack.ForeColor = stacked2 ? Color.White : TextMain;
            if (thumbs != null) thumbs.Invalidate();
            if (preview != null) preview.Invalidate();
        };
        optSide.Click += (s, e) => { stacked2 = false; refreshOrient(); };
        optStack.Click += (s, e) => { stacked2 = true; refreshOrient(); };
        left.Controls.Add(optSide);
        left.Controls.Add(optStack);
        optSide.BringToFront();
        optStack.BringToFront();
        refreshOrient();        row.Controls.Add(left);

        var right = Card(new Point(338, 0), new Size(397, 96), BgCard, Line);
        right.Paint += (s, e) =>
        {
            using (var f = new Font("Segoe UI", 9F, FontStyle.Bold))
                e.Graphics.DrawString("Layout Preview", f, new SolidBrush(TextMain), 10, 8);
        };
        preview = new Panel { Location = new Point(10, 26), Size = new Size(377, 62), BackColor = BgCard };
        preview.Paint += (s, e) => DrawLayout(e.Graphics, new Rectangle(0, 0, preview.Width, preview.Height), selectedCount, true);
        right.Controls.Add(preview);
        row.Controls.Add(right);
        pageArrange.Controls.Add(row);

        var act = Card(new Point(0, 246), new Size(735, 44), BgCard, Line);
        var btn = Primary("Arrange Now", new Point(10, 6), new Size(180, 32));
        btn.Click += ArrangeWindows;
        act.Controls.Add(btn);
        lblArrangeResult = new Label { Location = new Point(200, 12), Size = new Size(520, 22), ForeColor = Ok, Font = new Font("Segoe UI", 8.5F) };
        act.Controls.Add(lblArrangeResult);
        pageArrange.Controls.Add(act);

        var tip = Card(new Point(0, 294), new Size(735, 26), BgCard, Line);
        tip.Paint += (s, e) =>
        {
            TextRenderer.DrawText(e.Graphics, "Tip", new Font("Segoe UI", 7.5F, FontStyle.Bold), new Rectangle(10, 0, 30, 26), Accent, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            TextRenderer.DrawText(e.Graphics, "Keep the game visible; minimised windows are restored first.",
                new Font("Segoe UI", 7.5F), new Rectangle(44, 0, 680, 26), TextDim, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        };
        pageArrange.Controls.Add(tip);
    }

    void DrawLayout(Graphics g, Rectangle area, int n, bool accent)
    {
        if (n == 2 && stacked2)
        {
            int bw = area.Width / 2 - 3, bh = area.Height / 2 - 3;
            int bx = area.X + (area.Width - bw) / 2;
            Color f2 = accent ? Color.FromArgb(233, 236, 239) : Color.FromArgb(243, 244, 246);
            Color l2 = accent ? Color.FromArgb(120, 128, 140) : Color.FromArgb(205, 210, 218);
            for (int i = 0; i < 2; i++)
            {
                var cell = new Rectangle(bx, area.Y + i * (bh + 3), bw, bh);
                using (var b = new SolidBrush(f2)) g.FillRectangle(b, cell);
                using (var p = new Pen(l2)) g.DrawRectangle(p, cell);
            }
            return;
        }
        int cols, rows;
        LayoutFor(n, out cols, out rows);
        int gap = 3;
        int cw = (area.Width - (cols + 1) * gap) / cols;
        int ch = (area.Height - (rows + 1) * gap) / rows;
        if (cw < 2 || ch < 2) return;
        Color fill = accent ? Color.FromArgb(233, 236, 239) : Color.FromArgb(243, 244, 246);
        Color line = accent ? Color.FromArgb(120, 128, 140) : Color.FromArgb(205, 210, 218);
        int index = 0;
        for (int r = 0; r < rows && index < n; r++)
        {
            int inRow = Math.Min(cols, n - r * cols);
            int rowW = inRow * cw + (inRow - 1) * gap;
            int offsetX = area.X + (area.Width - rowW) / 2;
            for (int c = 0; c < inRow; c++)
            {
                var cell = new Rectangle(offsetX + c * (cw + gap), area.Y + gap + r * (ch + gap), cw, ch);
                using (var b = new SolidBrush(fill)) g.FillRectangle(b, cell);
                using (var p = new Pen(line)) g.DrawRectangle(p, cell);
                index++;
            }
        }
    }

    void LayoutFor(int n, out int cols, out int rows)
    {
        if (n == 2 && stacked2) { cols = 1; rows = 2; return; }
        if (n <= 1) { cols = 1; rows = 1; }
        else if (n == 2) { cols = 2; rows = 1; }
        else if (n <= 4) { cols = 2; rows = 2; }
        else if (n <= 6) { cols = 3; rows = 2; }
        else if (n <= 8) { cols = 4; rows = 2; }
        else { cols = 3; rows = 3; }
    }

    // ---------------------------------------------------------------- about tab
    void BuildAboutPage()
    {
        var prof = Card(new Point(0, 0), new Size(735, 60), BgCard, Line);
        prof.Paint += (s, e) =>
        {
            using (var b = new SolidBrush(Accent)) e.Graphics.FillEllipse(b, 12, 12, 36, 36);
            TextRenderer.DrawText(e.Graphics, "KH", new Font("Segoe UI", 10F, FontStyle.Bold), new Rectangle(12, 12, 36, 36),
                Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            using (var f = new Font("Segoe UI", 10F, FontStyle.Bold))
                e.Graphics.DrawString("Kenshin Himura", f, new SolidBrush(TextMain), 58, 12);
            TextRenderer.DrawText(e.Graphics, "Ragnarok X player on the Srikandi server, member of the Darkside TH guild.",
                new Font("Segoe UI", 7.5F), new Rectangle(58, 32, 660, 18), TextDim, TextFormatFlags.Left);
        };
        pageAbout.Controls.Add(prof);

        string Sha = SelfHash();
        var sec = Card(new Point(0, 66), new Size(735, 256), BgCard, Line);
        sec.Paint += (s, e) =>
        {
            var g = e.Graphics;
            using (var f = new Font("Segoe UI", 10F, FontStyle.Bold))
                g.DrawString("Security & Build", f, new SolidBrush(TextMain), 12, 6);

            TextRenderer.DrawText(g, "Unsigned build — verify this hash on VirusTotal before running.",
                new Font("Segoe UI", 7.5F), new Rectangle(12, 26, 700, 16), TextDim, TextFormatFlags.Left);

            using (var b = new SolidBrush(BgField)) g.FillRectangle(b, 12, 46, 710, 18);
            TextRenderer.DrawText(g, Sha, new Font("Consolas", 7F), new Rectangle(18, 46, 700, 18), TextMain,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

            var body = new Font("Segoe UI", 7.5F);
            int y = 100;
            TextRenderer.DrawText(g, "Built with C# and .NET Framework 4.8 (WinForms), compiled with the Microsoft C# compiler (csc.exe).",
                body, new Rectangle(12, y, 710, 16), TextDim, TextFormatFlags.Left); y += 18;
            TextRenderer.DrawText(g, "No installer, no background service, no network access. It only writes the window size into the game's own registry entry and starts rox.exe.",
                body, new Rectangle(12, y, 710, 16), TextDim, TextFormatFlags.Left); y += 18;
            TextRenderer.DrawText(g, "Source code is available on request — contact Kenshin Himura (Darkside TH).",
                body, new Rectangle(12, y, 710, 16), TextMain, TextFormatFlags.Left); y += 22;
            TextRenderer.DrawText(g, "Some engines may flag this file heuristically because it is unsigned, built with a small compiler, and writes to the registry. " +
                                   "That is a false positive: the hash above is the only file this build produces, and the source code is available for review.",
                body, new Rectangle(12, y, 710, 44), TextDim, TextFormatFlags.Left | TextFormatFlags.WordBreak);
        };

        var vt = Primary("Upload to VirusTotal", new Point(12, 66), new Size(160, 24));
        vt.Click += (s, e) => Process.Start("https://www.virustotal.com/gui/home/upload");
        sec.Controls.Add(vt);

        var cp = Ghost("Copy Hash", new Point(180, 66), new Size(90, 24));
        cp.Click += (s, e) => { try { Clipboard.SetText(SelfHash()); } catch { } };
        sec.Controls.Add(cp);

        pageAbout.Controls.Add(sec);
    }
    static string SelfHash()
    {
        try
        {
            using (var sha = SHA256.Create())
            using (var fs = System.IO.File.OpenRead(Application.ExecutablePath))
                return BitConverter.ToString(sha.ComputeHash(fs)).Replace("-", "");
        }
        catch { return ScannedHash; }
    }

    // ---------------------------------------------------------------- actions
    void SaveAndLaunch(object sender, EventArgs e)
    {
        if (key == null)
        {
            MessageBox.Show("ROX settings were not found in the registry.\n\nRun the game once, then open this tool again.",
                "ROX Window Tool", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        blMode = 0;
        int w = (int)numW.Value, h = (int)numH.Value;
        if (gameRunning)
        {
            var a = MessageBox.Show("ROX is still running.\n\nOpen windows keep their old size and can overwrite this setting when they close.\n\nSave anyway and launch the game?",
                "ROX is running", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (a != DialogResult.Yes) return;
        }
        try
        {
            SetValue("Screenmanager Resolution Width", "Screenmanager Resolution Width_h182942802", w);
            SetValue("Screenmanager Resolution Height", "Screenmanager Resolution Height_h2627697771", h);
            SetValue("Screenmanager Fullscreen mode", "Screenmanager Fullscreen mode_h3630240806", 3);
            SetValue("Screenmanager Resolution Use Native", "Screenmanager Resolution Use Native_h1405027254", 0);
        }
        catch (Exception ex)
        {
            MessageBox.Show("The settings could not be saved:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        MessageBox.Show("Window size saved: " + w + " x " + h + " (windowed).\n\nThe game will now start.",
            "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);

        string exe = FindGameClient();
        if (exe == null)
        {
            MessageBox.Show("The game client was not found. Start it manually:\n" + GameDefault, "Game client", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        try
        {
            var si = new ProcessStartInfo(exe);
            si.WorkingDirectory = System.IO.Path.GetDirectoryName(exe);
            Process.Start(si);
        }
        catch (Exception ex)
        {
            MessageBox.Show("The settings were saved, but the game could not start:\n" + ex.Message, "Game client", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
    void ArrangeWindows(object sender, EventArgs e)
    {
        var wins = FindGameWindows();
        int open = wins.Count;
        if (open == 0) { lblArrangeResult.Text = "No ROX window is open."; lblArrangeResult.ForeColor = Warn; return; }
        int n = Math.Min(selectedCount, open);
        int cols, rows;
        LayoutFor(selectedCount, out cols, out rows);
        Rectangle work = Screen.PrimaryScreen.WorkingArea;
        if (selectedCount == 2 && stacked2)
        {
            int cw = work.Width / 2, chh = work.Height / 2;
            int x = work.Left + (work.Width - cw) / 2;
            for (int i = 0; i < n; i++)
            {
                IntPtr hh = wins[i];
                if (IsIconic(hh)) { ShowWindow(hh, 9); System.Threading.Thread.Sleep(100); }
                MoveWindow(hh, x, work.Top + i * chh, cw, chh, true);
            }
            lblArrangeResult.ForeColor = Ok;
            lblArrangeResult.Text = n + " windows arranged (stacked, " + cw + " x " + chh + " each)";
            return;
        }
        int cellW = work.Width / cols, cellH = work.Height / rows;
        int index = 0;
        for (int r = 0; r < rows && index < n; r++)
        {
            int inRow = Math.Min(cols, n - r * cols);
            int offsetX = (work.Width - inRow * cellW) / 2;
            for (int c = 0; c < inRow; c++)
            {
                IntPtr hh = wins[index++];
                if (IsIconic(hh)) { ShowWindow(hh, 9); System.Threading.Thread.Sleep(100); }
                MoveWindow(hh, work.Left + offsetX + c * cellW, work.Top + r * cellH, cellW, cellH, true);
            }
        }
        lblArrangeResult.ForeColor = Ok;
        lblArrangeResult.Text = n + (n == 1 ? " window arranged" : " windows arranged") + " (" + cols + " x " + rows + ")" +
            (open < selectedCount ? "  — only " + open + " of " + selectedCount + " open" : "");
    }

    List<IntPtr> FindGameWindows()
    {
        var pids = new HashSet<uint>();
        foreach (var p in Process.GetProcessesByName("rox")) pids.Add((uint)p.Id);
        var list = new List<IntPtr>();
        EnumWindows((h, l) =>
        {
            uint pid; GetWindowThreadProcessId(h, out pid);
            if (!pids.Contains(pid)) return true;
            var cls = new System.Text.StringBuilder(64);
            GetClassNameW(h, cls, 64);
            if (cls.ToString() != "UnityWndClass") return true;
            RECT r; GetWindowRect(h, out r);
            if (r.R - r.L < 400 || r.B - r.T < 300) return true;
            if (!IsWindowVisible(h)) return true;
            list.Add(h);
            return true;
        }, IntPtr.Zero);
        return list;
    }

    // ---------------------------------------------------------------- helpers
    Panel Card(Point loc, Size size, Color bg, Color border)
    {
        var p = new Panel { Location = loc, Size = size, BackColor = bg };
        p.Paint += (s, e) => { using (var pen = new Pen(border)) e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1); };
        return p;
    }

    Label Lbl(string text, int x, int y, int w)
    {
        return new Label { Text = text, Location = new Point(x, y), Size = new Size(w, 18), ForeColor = TextDim };
    }

    Button Primary(string text, Point loc, Size size)
    {
        var b = new Button { Text = text, Location = loc, Size = size, FlatStyle = FlatStyle.Flat, BackColor = BtnDark, ForeColor = Color.White, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
        b.FlatAppearance.BorderSize = 0;
        return b;
    }

    Button Ghost(string text, Point loc, Size size)
    {
        var b = new Button { Text = text, Location = loc, Size = size, FlatStyle = FlatStyle.Flat, BackColor = BgField, ForeColor = TextMain };
        b.FlatAppearance.BorderColor = Line;
        return b;
    }

    const int GWL_STYLE = -16;
    const long WS_CAPTION = 0x00C00000L, WS_THICKFRAME = 0x00040000L, WS_SYSMENU = 0x00080000L,
               WS_MINIMIZEBOX = 0x00020000L, WS_MAXIMIZEBOX = 0x00010000L;
    const uint SWP_FRAMECHANGED = 0x0020, SWP_SHOWWINDOW = 0x0040, SWP_NOZORDER = 0x0004;

    // 0 = tidak ada, 1 = paksa borderless, 2 = paksa berbingkai
    static volatile int blMode;

    void SaveWindowedRegistry(int w, int h)
    {
        if (key == null) return;
        try
        {
            SetValue("Screenmanager Resolution Width", "Screenmanager Resolution Width_h182942802", w);
            SetValue("Screenmanager Resolution Height", "Screenmanager Resolution Height_h2627697771", h);
            SetValue("Screenmanager Fullscreen mode", "Screenmanager Fullscreen mode_h3630240806", 3);
            SetValue("Screenmanager Resolution Use Native", "Screenmanager Resolution Use Native_h1405027254", 0);
        }
        catch { }
    }
    void StartEnforcer()
    {
        var th = new System.Threading.Thread(() =>
        {
            while (true)
            {
                int m = blMode;
                if (m != 0)
                {
                    var bounds = Screen.PrimaryScreen.Bounds;
                    int w = 1510, h = 841;
                    try { Invoke((Action)(() => { w = (int)numW.Value; h = (int)numH.Value; })); } catch { }
                    foreach (IntPtr hw in GameWindows())
                    {
                        if (m == 1)
                        {
                            long style = GetWindowLongPtr(hw, GWL_STYLE).ToInt64();
                            long cleaned = style & ~(WS_CAPTION | WS_THICKFRAME | WS_SYSMENU | WS_MINIMIZEBOX | WS_MAXIMIZEBOX);
                            if (cleaned != style) SetWindowLongPtr(hw, GWL_STYLE, new IntPtr(cleaned));
                            SetWindowPos(hw, IntPtr.Zero, bounds.Left, bounds.Top, bounds.Width, bounds.Height,
                                SWP_FRAMECHANGED | SWP_SHOWWINDOW | SWP_NOZORDER);
                        }
                        else
                        {
                            long style = GetWindowLongPtr(hw, GWL_STYLE).ToInt64();
                            long back = style | WS_CAPTION | WS_THICKFRAME | WS_SYSMENU | WS_MINIMIZEBOX | WS_MAXIMIZEBOX;
                            if (back != style) SetWindowLongPtr(hw, GWL_STYLE, new IntPtr(back));
                            SetWindowPos(hw, IntPtr.Zero, 40, 40, w + 26, h + 71,
                                SWP_FRAMECHANGED | SWP_SHOWWINDOW | SWP_NOZORDER);
                        }
                    }
                }
                System.Threading.Thread.Sleep(150);
            }
        });
        th.IsBackground = true;
        th.Start();
    }

    static List<IntPtr> GameWindows()
    {
        var pids = new HashSet<uint>();
        foreach (var p in Process.GetProcessesByName("rox")) pids.Add((uint)p.Id);
        var list = new List<IntPtr>();
        EnumWindows((h, l) =>
        {
            uint pid; GetWindowThreadProcessId(h, out pid);
            if (!pids.Contains(pid)) return true;
            var cls = new System.Text.StringBuilder(64);
            GetClassNameW(h, cls, 64);
            if (cls.ToString() == "UnityWndClass" && IsWindowVisible(h)) list.Add(h);
            return true;
        }, IntPtr.Zero);
        return list;
    }
    static string FindGameClient()
    {
        string l = FindLauncher();
        if (l != null)
        {
            string p = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(l), @"RO_SEAGame\rox.exe");
            if (System.IO.File.Exists(p)) return p;
        }
        if (System.IO.File.Exists(GameDefault)) return GameDefault;
        return null;
    }

    static string FindLauncher()
    {
        foreach (string hive in new string[] { "HKCU", "HKLM" })
        {
            using (RegistryKey rk = hive == "HKCU"
                ? Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall")
                : Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall"))
            {
                if (rk == null) continue;
                foreach (string sub in rk.GetSubKeyNames())
                {
                    using (RegistryKey sk = rk.OpenSubKey(sub))
                    {
                        if (sk == null) continue;
                        string name = sk.GetValue("DisplayName") as string;
                        if (name == null || !(name.StartsWith("ROX") || name.StartsWith("Ragnarok"))) continue;
                        string loc = sk.GetValue("InstallLocation") as string;
                        if (!string.IsNullOrEmpty(loc))
                        {
                            string p = System.IO.Path.Combine(loc, "ROX.exe");
                            if (System.IO.File.Exists(p)) return p;
                        }
                        string icon = sk.GetValue("DisplayIcon") as string;
                        if (!string.IsNullOrEmpty(icon))
                        {
                            string p = icon.Split(',')[0].Trim('"');
                            if (System.IO.File.Exists(p)) return p;
                        }
                    }
                }
            }
        }
        if (System.IO.File.Exists(LauncherDefault)) return LauncherDefault;
        return null;
    }

    static RegistryKey FindRoxKey()
    {
        using (RegistryKey software = Registry.CurrentUser.OpenSubKey("Software"))
        {
            if (software == null) return null;
            RegistryKey fallback = null;
            foreach (string vendor in software.GetSubKeyNames())
            {
                using (RegistryKey vk = software.OpenSubKey(vendor))
                {
                    if (vk == null) continue;
                    foreach (string product in vk.GetSubKeyNames())
                    {
                        RegistryKey pk = null;
                        try { pk = vk.OpenSubKey(product, true); } catch { continue; }
                        if (pk == null) continue;
                        string[] names = pk.GetValueNames();
                        if (Array.IndexOf(names, Marker) >= 0) return pk;
                        if (fallback == null && Array.Exists(names, n => n.StartsWith("Screenmanager Resolution Width")))
                            fallback = pk;
                        else pk.Close();
                    }
                }
            }
            return fallback;
        }
    }

    int GetValue(string prefix, string fallbackName, int fallback)
    {
        string name = null;
        foreach (string n in key.GetValueNames())
            if (n.StartsWith(prefix) && n.IndexOf("Default", StringComparison.Ordinal) < 0) { name = n; break; }
        if (name == null) name = fallbackName;
        object v = key.GetValue(name);
        return v == null ? fallback : Convert.ToInt32(v);
    }

    void SetValue(string prefix, string fallbackName, int value)
    {
        string name = null;
        foreach (string n in key.GetValueNames())
            if (n.StartsWith(prefix) && n.IndexOf("Default", StringComparison.Ordinal) < 0) { name = n; break; }
        if (name == null) name = fallbackName;
        key.SetValue(name, value, RegistryValueKind.DWord);
    }

    class Preset
    {
        public string Name; public int W, H;
        public Preset(string name, int w, int h) { Name = name; W = w; H = h; }
        public override string ToString() { return Name; }
    }
}
