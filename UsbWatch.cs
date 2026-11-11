// UsbWatch - live USB arrival/removal monitor (WinForms, single exe, no dependencies)
// Dracula-themed. Build: run Build.bat (uses the C# compiler that ships with Windows).
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

static class Theme
{
    // Dracula palette
    public static readonly Color Bg       = ColorTranslator.FromHtml("#282a36");
    public static readonly Color Panel    = ColorTranslator.FromHtml("#21222c");
    public static readonly Color Line     = ColorTranslator.FromHtml("#44475a");
    public static readonly Color Comment  = ColorTranslator.FromHtml("#6272a4");
    public static readonly Color Dim      = ColorTranslator.FromHtml("#8f9bc7");
    public static readonly Color Fg       = ColorTranslator.FromHtml("#f8f8f2");
    public static readonly Color Cyan     = ColorTranslator.FromHtml("#8be9fd");
    public static readonly Color Green    = ColorTranslator.FromHtml("#50fa7b");
    public static readonly Color Orange   = ColorTranslator.FromHtml("#ffb86c");
    public static readonly Color Pink     = ColorTranslator.FromHtml("#ff79c6");
    public static readonly Color Purple   = ColorTranslator.FromHtml("#bd93f9");
    public static readonly Color Red      = ColorTranslator.FromHtml("#ff5555");
    public static readonly Color PurpleHi = ColorTranslator.FromHtml("#d3b8fb");
}

class Dev
{
    public string Id, Name, Class;
    public int Code;
}

class RowInfo
{
    public string Id, Event;
    public int Code;
}

class DarkColors : ProfessionalColorTable
{
    public override Color MenuItemSelected { get { return Theme.Line; } }
    public override Color MenuItemSelectedGradientBegin { get { return Theme.Line; } }
    public override Color MenuItemSelectedGradientEnd { get { return Theme.Line; } }
    public override Color MenuItemBorder { get { return Theme.Line; } }
    public override Color MenuBorder { get { return Theme.Line; } }
    public override Color ToolStripDropDownBackground { get { return Theme.Panel; } }
    public override Color ImageMarginGradientBegin { get { return Theme.Panel; } }
    public override Color ImageMarginGradientMiddle { get { return Theme.Panel; } }
    public override Color ImageMarginGradientEnd { get { return Theme.Panel; } }
    public override Color SeparatorDark { get { return Theme.Line; } }
    public override Color SeparatorLight { get { return Theme.Line; } }
}

class DarkRenderer : ToolStripProfessionalRenderer
{
    public DarkRenderer() : base(new DarkColors()) { RoundedEdges = false; }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        using (SolidBrush b = new SolidBrush(e.BackColor)) e.Graphics.FillRectangle(b, e.AffectedBounds);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        if (!(e.ToolStrip is StatusStrip)) base.OnRenderToolStripBorder(e);
    }

    protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
    {
        e.ArrowColor = Theme.Dim;
        base.OnRenderArrow(e);
    }

    // Custom check mark so toggled menu items read clearly on the dark background.
    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
    {
        Rectangle r = e.ImageRectangle;
        Graphics g = e.Graphics;
        SmoothingMode old = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (Pen p = new Pen(Theme.Green, 2f))
        {
            p.StartCap = LineCap.Round;
            p.EndCap = LineCap.Round;
            p.LineJoin = LineJoin.Round;
            g.DrawLines(p, new Point[] {
                new Point(r.X + 5, r.Y + r.Height / 2),
                new Point(r.X + r.Width / 2 - 1, r.Y + r.Height - 6),
                new Point(r.X + r.Width - 5, r.Y + 5)
            });
        }
        g.SmoothingMode = old;
    }
}

class DarkList : ListView
{
    public DarkList()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
    }
}

class AboutForm : Form
{
    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    static extern int SetWindowTheme(IntPtr hWnd, string appName, string idList);

    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    RichTextBox rt;

    public AboutForm()
    {
        Text = "About UsbWatch";
        Font = new Font("Segoe UI", 9f);
        BackColor = Theme.Bg;
        ForeColor = Theme.Fg;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(560, 520);

        Panel head = new Panel();
        head.Dock = DockStyle.Top;
        head.Height = 76;
        head.BackColor = Theme.Panel;
        head.Paint += delegate(object s, PaintEventArgs e)
        {
            using (Pen p = new Pen(Theme.Line)) e.Graphics.DrawLine(p, 0, head.Height - 1, head.Width, head.Height - 1);
        };

        Label title = new Label();
        title.Text = "UsbWatch";
        title.AutoSize = true;
        title.Font = new Font("Segoe UI Semibold", 16f);
        title.ForeColor = Theme.Purple;
        title.Location = new Point(20, 8);
        head.Controls.Add(title);

        Label ver = new Label();
        ver.Text = "Live USB device monitor   ·   version v" + BuildInfo.Stamp;
        ver.AutoSize = true;
        ver.ForeColor = Theme.Cyan;
        ver.Location = new Point(23, 46);
        head.Controls.Add(ver);

        Panel foot = new Panel();
        foot.Dock = DockStyle.Bottom;
        foot.Height = 48;
        foot.BackColor = Theme.Panel;

        Button close = new Button();
        close.Text = "Close";
        close.FlatStyle = FlatStyle.Flat;
        close.FlatAppearance.BorderSize = 0;
        close.FlatAppearance.MouseOverBackColor = Theme.Comment;
        close.FlatAppearance.MouseDownBackColor = Theme.Purple;
        close.BackColor = Theme.Line;
        close.ForeColor = Theme.Fg;
        close.Size = new Size(88, 28);
        close.Location = new Point(foot.Width - close.Width - 16, 10);
        close.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        close.Cursor = Cursors.Hand;
        close.DialogResult = DialogResult.OK;
        foot.Controls.Add(close);
        AcceptButton = close;
        CancelButton = close;

        Panel body = new Panel();
        body.Dock = DockStyle.Fill;
        body.BackColor = Theme.Bg;
        body.Padding = new Padding(20, 14, 10, 10);

        rt = new RichTextBox();
        rt.Dock = DockStyle.Fill;
        rt.ReadOnly = true;
        rt.BorderStyle = BorderStyle.None;
        rt.BackColor = Theme.Bg;
        rt.ForeColor = Theme.Fg;
        rt.Font = new Font("Segoe UI", 9.5f);
        rt.ScrollBars = RichTextBoxScrollBars.Vertical;
        rt.DetectUrls = false;
        rt.TabStop = false;
        rt.HandleCreated += delegate
        {
            try { SetWindowTheme(rt.Handle, "DarkMode_Explorer", null); } catch { }
        };
        body.Controls.Add(rt);

        // Dock order: Fill first, then the edges.
        Controls.Add(body);
        Controls.Add(head);
        Controls.Add(foot);

        FillChangelog();
    }

    void AppendRun(string text, Color color, Font font, int indent, int hanging)
    {
        rt.SelectionStart = rt.TextLength;
        rt.SelectionLength = 0;
        rt.SelectionColor = color;
        rt.SelectionFont = font;
        rt.SelectionIndent = indent;
        rt.SelectionHangingIndent = hanging;
        rt.SelectedText = text;
    }

    // CHANGELOG.md is embedded into the exe by Build.bat (resource name UsbWatch.CHANGELOG.md).
    static string[] ReadChangelogLines()
    {
        try
        {
            using (Stream s = System.Reflection.Assembly.GetExecutingAssembly()
                       .GetManifestResourceStream("UsbWatch.CHANGELOG.md"))
            {
                if (s == null) return null;
                using (StreamReader r = new StreamReader(s, Encoding.UTF8))
                    return r.ReadToEnd().Replace("\r", "").Split('\n');
            }
        }
        catch { return null; }
    }

    // Format: "## <version>" starts an entry and "- " lines are its bullets. Anything else in the
    // file (the title, intro text, blank lines) is for GitHub readers and is skipped here.
    void FillChangelog()
    {
        Font head = new Font("Segoe UI Semibold", 10.5f);
        Font body = new Font("Segoe UI", 9.5f);
        AppendRun("Changelog\n\n", Theme.Pink, new Font("Segoe UI Semibold", 12f), 0, 0);

        string[] lines = ReadChangelogLines();
        if (lines == null)
        {
            AppendRun("The changelog is not available in this build.\n", Theme.Dim, body, 0, 0);
            rt.Select(0, 0);
            return;
        }

        bool inEntry = false;
        foreach (string raw in lines)
        {
            string line = raw.TrimEnd();
            if (line.StartsWith("## "))
            {
                if (inEntry) AppendRun("\n", Theme.Fg, body, 0, 0);   // gap between entries
                AppendRun(line.Substring(3).Trim() + "\n", Theme.Purple, head, 0, 0);
                inEntry = true;
            }
            else if (line.StartsWith("- ") || line.StartsWith("* "))
            {
                AppendRun("•  " + line.Substring(2).Trim() + "\n", Theme.Fg, body, 6, 16);
            }
        }
        rt.Select(0, 0);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        try
        {
            int on = 1;
            if (DwmSetWindowAttribute(Handle, 20, ref on, 4) != 0) DwmSetWindowAttribute(Handle, 19, ref on, 4);
            int cap = 0x002C2221;   // #21222c (Windows 11 only; ignored elsewhere)
            DwmSetWindowAttribute(Handle, 35, ref cap, 4);
            int txt = 0x00F2F8F8;   // #f8f8f2
            DwmSetWindowAttribute(Handle, 36, ref txt, 4);
        }
        catch { }
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        if (Owner != null) Icon = Owner.Icon;
        rt.Select(0, 0);
        ActiveControl = null;
    }
}

class MainForm : Form
{
    const int WM_DEVICECHANGE = 0x0219;
    const int MaxRows = 2000;

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    static extern int SetWindowTheme(IntPtr hWnd, string appName, string idList);

    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    [DllImport("user32.dll")]
    static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    static readonly Dictionary<int, string> Problems = new Dictionary<int, string> {
        {1, "not configured"}, {10, "cannot start"}, {12, "resource conflict"},
        {14, "restart required"}, {18, "reinstall drivers"}, {22, "disabled"},
        {24, "not present or drivers missing"}, {41, "driver loaded but device not found"},
        {48, "blocked from starting"}, {51, "waiting on another device"},
        {28, "drivers not installed"}, {31, "not working properly"},
        {37, "driver returned failure"},
        {43, "reported problems (enumeration failure)"},
        {52, "driver signature problem"}
    };

    ListView list;
    Label hint, lblDot;
    ToolStripStatusLabel lblCount, lblProblems, lblLast;
    // Settings-menu toggles (saved to the ini). Pause stays a toolbar chip: it's used live.
    ToolStripMenuItem cbUsbOnly, cbHideIf, cbNotify, cbAuto, cbLog, cbTop, cbTray;
    CheckBox cbPause;
    ContextMenuStrip settingsMenu;
    ToolStripMenuItem colMenuItem;
    ToolStripDropDownMenu colSub;
    ToolStripMenuItem[] colItems;

    // Column model. List column 0 is a slim gutter that carries the colored accent bar; columns
    // 1..7 are the data columns below, each of which can be shown or hidden from Settings > Columns.
    static readonly string[] ColNames = { "Time", "Event", "Device", "Class", "VID:PID", "Port", "Status" };
    static readonly int[] ColDefaultW = { 100, 110, 330, 95, 135, 70, 260 };
    static readonly bool[] ColDefaultOn = { true, true, true, false, false, true, true };
    const int GutterW = 16;
    int[] colW = (int[])ColDefaultW.Clone();
    bool[] colOn = (bool[])ColDefaultOn.Clone();
    bool applyingCols;
    bool wasMaximized;
    NotifyIcon tray;
    bool exiting;          // true only when the user picks Exit from the tray menu
    bool trayHintShown;    // the "still running" balloon is shown once per session
    System.Windows.Forms.Timer debounce, poll;
    Dictionary<string, Dev> prev;

    // Notification throttle: the first change notifies immediately and starts a holdoff. Changes
    // that arrive during the holdoff are collected and sent as ONE summary toast when it ends
    // (which starts another holdoff, so a long burst keeps collapsing). A dock or hub can emit
    // events for ~4 s with ~2 s gaps, so keep this above that. List rows are never suppressed.
    const int NotifyHoldoffMs = 5000;
    bool inHoldoff;
    List<Dev> pArrived = new List<Dev>(), pRemoved = new List<Dev>(), pChanged = new List<Dev>();
    System.Windows.Forms.Timer holdoffTimer;

    int busy;
    bool pending;
    Font uiFont = new Font("Segoe UI", 9f);
    Font listFont = new Font("Consolas", 10f);
    Font listBold = new Font("Consolas", 10f, FontStyle.Bold);
    Font headFont = new Font("Segoe UI", 8.25f, FontStyle.Bold);
    string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UsbWatch.log");

    public MainForm()
    {
        Text = "UsbWatch";
        Font = uiFont;
        BackColor = Theme.Bg;
        ForeColor = Theme.Fg;
        Size = new Size(1120, 560);
        MinimumSize = new Size(520, 260);
        StartPosition = FormStartPosition.CenterScreen;
        Icon = MakeIcon();

        // ---- list ----
        list = new DarkList();
        list.Dock = DockStyle.Fill;
        list.View = View.Details;
        list.FullRowSelect = true;
        list.GridLines = false;
        list.HideSelection = false;
        list.BorderStyle = BorderStyle.None;
        list.BackColor = Theme.Bg;
        list.ForeColor = Theme.Fg;
        list.Font = listFont;
        list.OwnerDraw = true;
        // A 1x26 image list is the classic trick for taller rows.
        ImageList rowHeight = new ImageList();
        rowHeight.ImageSize = new Size(1, 26);
        list.SmallImageList = rowHeight;
        list.Columns.Add("", GutterW);
        for (int ci = 0; ci < ColNames.Length; ci++) list.Columns.Add(ColNames[ci], ColDefaultW[ci]);
        list.ColumnWidthChanging += OnColWidthChanging;
        list.ColumnWidthChanged += OnColWidthChanged;
        list.DrawColumnHeader += DrawHeader;
        list.DrawItem += delegate(object s, DrawListViewItemEventArgs e) { };
        list.DrawSubItem += DrawCell;
        list.Resize += delegate { FitColumns(); PlaceHint(); };
        list.HandleCreated += delegate
        {
            try { SetWindowTheme(list.Handle, "DarkMode_Explorer", null); } catch { }
            // LVM_SETEXTENDEDLISTVIEWSTYLE: explicitly clear LVS_EX_GRIDLINES (0x1).
            try { SendMessage(list.Handle, 0x1036, (IntPtr)0x1, IntPtr.Zero); } catch { }
        };
        list.KeyDown += delegate(object s, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.C) { CopyRows(false); e.Handled = true; }
            if (e.Control && e.KeyCode == Keys.A) { foreach (ListViewItem i in list.Items) i.Selected = true; }
        };

        ContextMenuStrip menu = new ContextMenuStrip();
        menu.Renderer = new DarkRenderer();
        menu.BackColor = Theme.Panel;
        menu.ForeColor = Theme.Fg;
        menu.ShowImageMargin = false;
        menu.Items.Add("Copy row(s)", null, delegate { CopyRows(false); });
        menu.Items.Add("Copy instance ID(s)", null, delegate { CopyRows(true); });
        foreach (ToolStripItem mi in menu.Items) { mi.BackColor = Theme.Panel; mi.ForeColor = Theme.Fg; }
        list.ContextMenuStrip = menu;

        hint = new Label();
        hint.Text = "Waiting for USB activity. Plug something in.";
        hint.AutoSize = true;
        hint.Font = new Font("Segoe UI", 11f);
        hint.ForeColor = Theme.Comment;
        hint.BackColor = Theme.Bg;
        list.Controls.Add(hint);

        // ---- header bar ----
        // Fixed-height bar (no wrapping, so buttons can never overlap): the live controls sit on
        // the left and a gear on the right opens the settings menu.
        Panel top = new Panel();
        top.Dock = DockStyle.Top;
        top.Height = 44;
        top.BackColor = Theme.Panel;
        top.Padding = new Padding(0, 0, 0, 1);   // leaves room for the bottom hairline
        top.Paint += delegate(object s, PaintEventArgs e)
        {
            using (Pen p = new Pen(Theme.Line)) e.Graphics.DrawLine(p, 0, top.Height - 1, top.Width, top.Height - 1);
        };

        FlowLayoutPanel left = new FlowLayoutPanel();
        left.Dock = DockStyle.Fill;
        left.WrapContents = false;
        left.BackColor = Theme.Panel;
        left.Padding = new Padding(14, 9, 0, 0);

        lblDot = new Label();
        lblDot.Text = "\u25CF";
        lblDot.AutoSize = true;
        lblDot.Font = new Font("Segoe UI", 12f);
        lblDot.ForeColor = Theme.Green;
        lblDot.Margin = new Padding(0, 1, 2, 0);
        left.Controls.Add(lblDot);

        Label brand = new Label();
        brand.Text = "UsbWatch";
        brand.AutoSize = true;
        brand.Font = new Font("Segoe UI Semibold", 12f);
        brand.ForeColor = Theme.Purple;
        brand.Margin = new Padding(0, 1, 18, 0);
        left.Controls.Add(brand);

        cbPause = MakeChip(left, "Pause", false);
        Button btnCurrent = MakeButton(left, "List current");
        Button btnClear = MakeButton(left, "Clear");
        btnCurrent.Click += delegate { ListCurrent(); };
        btnClear.Click += delegate { list.Items.Clear(); UpdateHint(); };

        Button btnGear = new Button();
        btnGear.Dock = DockStyle.Right;
        btnGear.Width = 48;
        btnGear.FlatStyle = FlatStyle.Flat;
        btnGear.FlatAppearance.BorderSize = 0;
        btnGear.FlatAppearance.MouseOverBackColor = Theme.Line;
        btnGear.FlatAppearance.MouseDownBackColor = Theme.Comment;
        btnGear.BackColor = Theme.Panel;
        btnGear.ForeColor = Theme.Fg;
        btnGear.Font = new Font("Segoe UI Symbol", 14f);
        btnGear.Text = "\u2699";
        btnGear.Cursor = Cursors.Hand;
        btnGear.TabStop = false;
        new ToolTip().SetToolTip(btnGear, "Settings");
        btnGear.Click += delegate
        {
            settingsMenu.Show(btnGear, new Point(btnGear.Width, btnGear.Height), ToolStripDropDownDirection.BelowLeft);
        };

        // Dock order: the Fill panel is added first so the gear claims its edge before it.
        top.Controls.Add(left);
        top.Controls.Add(btnGear);

        // ---- settings menu (gear) ----
        settingsMenu = new ContextMenuStrip();
        settingsMenu.Renderer = new DarkRenderer();
        settingsMenu.BackColor = Theme.Panel;
        settingsMenu.ForeColor = Theme.Fg;
        settingsMenu.ShowImageMargin = false;
        settingsMenu.ShowCheckMargin = true;
        cbUsbOnly = AddToggle(settingsMenu.Items, "USB devices only", true);
        cbHideIf = AddToggle(settingsMenu.Items, "Hide sub-interfaces", false);
        cbNotify = AddToggle(settingsMenu.Items, "Notifications", true);
        cbAuto = AddToggle(settingsMenu.Items, "Auto-scroll", true);
        cbLog = AddToggle(settingsMenu.Items, "Log to file", false);
        cbTop = AddToggle(settingsMenu.Items, "Always on top", false);
        cbTray = AddToggle(settingsMenu.Items, "Close to tray", true);
        settingsMenu.Items.Add(new ToolStripSeparator());

        colMenuItem = new ToolStripMenuItem("Columns");
        colMenuItem.BackColor = Theme.Panel;
        colMenuItem.ForeColor = Theme.Fg;
        colSub = (ToolStripDropDownMenu)colMenuItem.DropDown;
        colSub.Renderer = new DarkRenderer();
        colSub.BackColor = Theme.Panel;
        colSub.ForeColor = Theme.Fg;
        colSub.ShowImageMargin = false;
        colSub.ShowCheckMargin = true;
        colItems = new ToolStripMenuItem[ColNames.Length];
        for (int ci = 0; ci < ColNames.Length; ci++)
        {
            int idx = ci;
            colItems[ci] = AddToggle(colSub.Items, ColNames[ci], ColDefaultOn[ci]);
            colItems[ci].CheckedChanged += delegate { OnColumnToggled(idx); };
        }
        settingsMenu.Items.Add(colMenuItem);
        settingsMenu.Items.Add(new ToolStripSeparator());

        ToolStripMenuItem miCopyAll = new ToolStripMenuItem("Copy all rows");
        miCopyAll.BackColor = Theme.Panel;
        miCopyAll.ForeColor = Theme.Fg;
        miCopyAll.Click += delegate { CopyRows(false, true); };
        settingsMenu.Items.Add(miCopyAll);
        settingsMenu.Items.Add(new ToolStripSeparator());

        ToolStripMenuItem miAbout = new ToolStripMenuItem("About UsbWatch...");
        miAbout.BackColor = Theme.Panel;
        miAbout.ForeColor = Theme.Fg;
        miAbout.Click += delegate { ShowAbout(); };
        settingsMenu.Items.Add(miAbout);

        // Keep the menu open while toggling checkable items so several can be changed in one visit.
        settingsMenu.Closing += KeepMenuOpen;
        colSub.Closing += KeepMenuOpen;

        cbUsbOnly.CheckedChanged += delegate { prev = null; RequestRefresh(); };
        cbTop.CheckedChanged += delegate { TopMost = cbTop.Checked; };
        cbAuto.CheckedChanged += delegate { if (cbAuto.Checked && list.Items.Count > 0) list.EnsureVisible(0); };
        cbPause.CheckedChanged += delegate { if (!cbPause.Checked) RequestRefresh(); SetPausedState(); };

        // ---- status bar ----
        StatusStrip status = new StatusStrip();
        status.Renderer = new DarkRenderer();
        status.BackColor = Theme.Panel;
        status.SizingGrip = false;
        status.Padding = new Padding(8, 0, 8, 0);
        lblCount = new ToolStripStatusLabel("Starting...");
        lblCount.ForeColor = Theme.Cyan;
        lblProblems = new ToolStripStatusLabel("");
        lblProblems.ForeColor = Theme.Orange;
        lblProblems.Margin = new Padding(16, 3, 0, 2);
        lblLast = new ToolStripStatusLabel("");
        lblLast.ForeColor = Theme.Dim;
        lblLast.Spring = true;
        lblLast.TextAlign = ContentAlignment.MiddleRight;
        status.Items.Add(lblCount);
        status.Items.Add(lblProblems);
        status.Items.Add(lblLast);

        // Version = build date/time, stamped into BuildInfo.cs by Build.bat on every build.
        ToolStripStatusLabel lblVer = new ToolStripStatusLabel("v" + BuildInfo.Stamp);
        lblVer.ForeColor = Theme.Comment;
        lblVer.Margin = new Padding(16, 3, 0, 2);
        status.Items.Add(lblVer);

        // Dock order: status (bottom), header bar (top), then the list fills the rest.
        Controls.Add(list);
        Controls.Add(top);
        Controls.Add(status);

        tray = new NotifyIcon();
        tray.Icon = Icon;
        tray.Text = "UsbWatch";
        tray.Visible = true;
        tray.DoubleClick += delegate { BringToView(); };
        tray.BalloonTipClicked += delegate { BringToView(); };

        // Right-click menu: reopen the window, or really quit (the X button only hides to the
        // tray while "Close to tray" is on).
        ContextMenuStrip trayMenu = new ContextMenuStrip();
        trayMenu.Renderer = new DarkRenderer();
        trayMenu.BackColor = Theme.Panel;
        trayMenu.ForeColor = Theme.Fg;
        trayMenu.ShowImageMargin = false;
        trayMenu.Items.Add("Show UsbWatch", null, delegate { BringToView(); });
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add("Exit", null, delegate { ExitApp(); });
        foreach (ToolStripItem ti in trayMenu.Items) { ti.BackColor = Theme.Panel; ti.ForeColor = Theme.Fg; }
        tray.ContextMenuStrip = trayMenu;

        debounce = new System.Windows.Forms.Timer();
        debounce.Interval = 300;
        debounce.Tick += delegate { debounce.Stop(); RequestRefresh(); };

        holdoffTimer = new System.Windows.Forms.Timer();
        holdoffTimer.Interval = NotifyHoldoffMs;
        holdoffTimer.Tick += delegate { OnHoldoffElapsed(); };


        // Safety net: catches status changes (e.g. a driver finishing install) after the event.
        poll = new System.Windows.Forms.Timer();
        poll.Interval = 2000;
        poll.Tick += delegate { RequestRefresh(); };
        poll.Start();

        // Restore saved toggles, columns and window geometry first (the toggles' handlers apply
        // their effects), then start saving changes.
        LoadSettings();
        ApplyColumns();
        foreach (KeyValuePair<string, ToolStripMenuItem> kv in SavedItems())
            kv.Value.CheckedChanged += delegate { SaveSettings(); };
        Resize += delegate
        {
            if (WindowState == FormWindowState.Minimized) return;
            bool max = WindowState == FormWindowState.Maximized;
            if (max != wasMaximized) { wasMaximized = max; SaveSettings(); }
        };
        ResizeEnd += delegate { SaveSettings(); };   // fires after a resize or a move drag

        Shown += delegate { FitColumns(); PlaceHint(); };
        RequestRefresh();
    }

    // ---- settings (UsbWatch.ini next to the exe) ----

    string iniPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UsbWatch.ini");

    // The checkable items that persist across runs: the settings toggles and the column choices.
    // Pause is deliberately left out: starting a fresh session already paused would look like a bug.
    KeyValuePair<string, ToolStripMenuItem>[] SavedItems()
    {
        List<KeyValuePair<string, ToolStripMenuItem>> l = new List<KeyValuePair<string, ToolStripMenuItem>>();
        l.Add(new KeyValuePair<string, ToolStripMenuItem>("UsbOnly", cbUsbOnly));
        l.Add(new KeyValuePair<string, ToolStripMenuItem>("HideSubInterfaces", cbHideIf));
        l.Add(new KeyValuePair<string, ToolStripMenuItem>("Notifications", cbNotify));
        l.Add(new KeyValuePair<string, ToolStripMenuItem>("AutoScroll", cbAuto));
        l.Add(new KeyValuePair<string, ToolStripMenuItem>("LogToFile", cbLog));
        l.Add(new KeyValuePair<string, ToolStripMenuItem>("AlwaysOnTop", cbTop));
        l.Add(new KeyValuePair<string, ToolStripMenuItem>("CloseToTray", cbTray));
        for (int i = 0; i < ColNames.Length; i++)
            l.Add(new KeyValuePair<string, ToolStripMenuItem>("Col." + ColNames[i].Replace(":", ""), colItems[i]));
        return l.ToArray();
    }

    static bool TryInt(Dictionary<string, string> kv, string key, out int v)
    {
        v = 0;
        string s;
        return kv.TryGetValue(key, out s) && int.TryParse(s, out v);
    }

    // Restore the saved window rectangle, but only if enough of its title bar is on a screen that
    // is still connected (otherwise keep the default centered placement).
    void RestoreGeometry(Dictionary<string, string> kv)
    {
        int x, y, w, h;
        if (!TryInt(kv, "WindowLeft", out x) || !TryInt(kv, "WindowTop", out y) ||
            !TryInt(kv, "WindowWidth", out w) || !TryInt(kv, "WindowHeight", out h)) return;
        w = Math.Max(w, MinimumSize.Width);
        h = Math.Max(h, MinimumSize.Height);
        Rectangle r = new Rectangle(x, y, w, h);
        Rectangle titleBar = new Rectangle(r.X, r.Y, r.Width, 40);
        bool onScreen = false;
        foreach (Screen sc in Screen.AllScreens)
        {
            Rectangle inter = Rectangle.Intersect(sc.WorkingArea, titleBar);
            if (inter.Width >= 100 && inter.Height >= 20) onScreen = true;
        }
        if (!onScreen) return;
        StartPosition = FormStartPosition.Manual;
        Bounds = r;
        string m;
        if (kv.TryGetValue("WindowMaximized", out m) && m == "1")
        {
            WindowState = FormWindowState.Maximized;
            wasMaximized = true;
        }
    }

    void LoadSettings()
    {
        try
        {
            if (!File.Exists(iniPath)) return;   // first run: keep the built-in defaults
            Dictionary<string, string> vals = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string raw in File.ReadAllLines(iniPath))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#") || line.StartsWith(";")) continue;
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                vals[line.Substring(0, eq).Trim()] = line.Substring(eq + 1).Trim();
            }
            foreach (KeyValuePair<string, ToolStripMenuItem> kv in SavedItems())
            {
                string v;
                if (!vals.TryGetValue(kv.Key, out v)) continue;
                v = v.ToLowerInvariant();
                if (v == "1" || v == "true") kv.Value.Checked = true;        // handlers apply the effect
                else if (v == "0" || v == "false") kv.Value.Checked = false;
            }
            RestoreGeometry(vals);
        }
        catch { }   // unreadable or garbled file: fall back to defaults
    }

    void SaveSettings()
    {
        try
        {
            List<string> lines = new List<string>();
            lines.Add("# UsbWatch settings (1 = on, 0 = off). Safe to edit while the app is closed.");
            foreach (KeyValuePair<string, ToolStripMenuItem> kv in SavedItems())
                lines.Add(kv.Key + "=" + (kv.Value.Checked ? "1" : "0"));
            // While maximized, remember the normal (restored) rectangle plus a maximized flag.
            Rectangle b = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
            lines.Add("WindowLeft=" + b.X);
            lines.Add("WindowTop=" + b.Y);
            lines.Add("WindowWidth=" + b.Width);
            lines.Add("WindowHeight=" + b.Height);
            lines.Add("WindowMaximized=" + (wasMaximized ? "1" : "0"));
            File.WriteAllLines(iniPath, lines.ToArray());
        }
        catch { }   // read-only folder etc.: settings just won't persist
    }

    // ---- theming helpers ----

    static Icon MakeIcon()
    {
        using (Bitmap bmp = new Bitmap(32, 32))
        {
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                using (SolidBrush b = new SolidBrush(Theme.Purple)) g.FillEllipse(b, 1, 1, 30, 30);
                using (SolidBrush b = new SolidBrush(Theme.Bg)) g.FillEllipse(b, 9, 9, 14, 14);
                using (SolidBrush b = new SolidBrush(Theme.Green)) g.FillEllipse(b, 12, 12, 8, 8);
            }
            return Icon.FromHandle(bmp.GetHicon());
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        try
        {
            int on = 1;
            if (DwmSetWindowAttribute(Handle, 20, ref on, 4) != 0) DwmSetWindowAttribute(Handle, 19, ref on, 4);
            int cap = 0x002C2221;   // #21222c as COLORREF (Windows 11 only; ignored elsewhere)
            DwmSetWindowAttribute(Handle, 35, ref cap, 4);
            int txt = 0x00F2F8F8;   // #f8f8f2
            DwmSetWindowAttribute(Handle, 36, ref txt, 4);
        }
        catch { }
    }

    CheckBox MakeChip(Control parent, string text, bool isChecked)
    {
        CheckBox c = new CheckBox();
        c.Appearance = Appearance.Button;
        c.FlatStyle = FlatStyle.Flat;
        c.FlatAppearance.BorderSize = 0;
        c.Text = text;
        // Fixed size from measured text: AutoSize clips text on flat toggle buttons.
        c.AutoSize = false;
        c.Size = new Size(TextRenderer.MeasureText(text, uiFont).Width + 24, 26);
        c.Margin = new Padding(0, 0, 8, 0);
        c.TextAlign = ContentAlignment.MiddleCenter;
        c.Cursor = Cursors.Hand;
        c.Checked = isChecked;
        ApplyChip(c);
        c.CheckedChanged += delegate { ApplyChip(c); };
        parent.Controls.Add(c);
        return c;
    }

    static void ApplyChip(CheckBox c)
    {
        bool on = c.Checked;
        c.BackColor = on ? Theme.Purple : Theme.Line;
        c.ForeColor = on ? Theme.Bg : Theme.Fg;
        c.FlatAppearance.CheckedBackColor = Theme.Purple;
        c.FlatAppearance.MouseOverBackColor = on ? Theme.PurpleHi : Theme.Comment;
        c.FlatAppearance.MouseDownBackColor = on ? Theme.PurpleHi : Theme.Comment;
    }

    Button MakeButton(Control parent, string text)
    {
        Button b = new Button();
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderSize = 0;
        b.FlatAppearance.MouseOverBackColor = Theme.Comment;
        b.FlatAppearance.MouseDownBackColor = Theme.Purple;
        b.BackColor = Theme.Line;
        b.ForeColor = Theme.Fg;
        b.Text = text;
        b.AutoSize = false;
        b.Size = new Size(TextRenderer.MeasureText(text, uiFont).Width + 24, 26);
        b.Margin = new Padding(0, 0, 8, 0);
        b.Cursor = Cursors.Hand;
        parent.Controls.Add(b);
        return b;
    }

    ToolStripMenuItem AddToggle(ToolStripItemCollection items, string text, bool isOn)
    {
        ToolStripMenuItem m = new ToolStripMenuItem(text);
        m.CheckOnClick = true;
        m.Checked = isOn;
        m.BackColor = Theme.Panel;
        m.ForeColor = Theme.Fg;
        items.Add(m);
        return m;
    }

    // Which settings item is under the mouse (checks the Columns submenu too).
    ToolStripItem ItemUnderCursor()
    {
        Point p = Cursor.Position;
        ToolStripItem it = settingsMenu.GetItemAt(settingsMenu.PointToClient(p));
        if ((it == null || it == colMenuItem) && colSub.Visible)
            it = colSub.GetItemAt(colSub.PointToClient(p));
        return it;
    }

    void KeepMenuOpen(object sender, ToolStripDropDownClosingEventArgs e)
    {
        if (e.CloseReason != ToolStripDropDownCloseReason.ItemClicked) return;
        ToolStripMenuItem m = ItemUnderCursor() as ToolStripMenuItem;
        if (m != null && m.CheckOnClick) e.Cancel = true;
    }

    // ---- columns ----

    void OnColumnToggled(int idx)
    {
        int n = 0;
        foreach (ToolStripMenuItem m in colItems) if (m.Checked) n++;
        if (n == 0) { colItems[idx].Checked = true; return; }   // always keep one column visible
        colOn[idx] = colItems[idx].Checked;
        ApplyColumns();
    }

    // Hidden columns get width 0; list column 0 is the fixed accent-bar gutter.
    void ApplyColumns()
    {
        if (list == null || list.Columns.Count != ColNames.Length + 1) return;
        applyingCols = true;
        try
        {
            list.BeginUpdate();
            list.Columns[0].Width = GutterW;
            for (int i = 0; i < ColNames.Length; i++)
                list.Columns[i + 1].Width = colOn[i] ? colW[i] : 0;
            FitColumns();
            list.EndUpdate();
        }
        finally { applyingCols = false; }
        list.Invalidate();
    }

    void OnColWidthChanging(object sender, ColumnWidthChangingEventArgs e)
    {
        if (applyingCols) return;
        if (e.ColumnIndex == 0) { e.NewWidth = GutterW; e.Cancel = true; return; }
        if (!colOn[e.ColumnIndex - 1]) { e.NewWidth = 0; e.Cancel = true; }
    }

    void OnColWidthChanged(object sender, ColumnWidthChangedEventArgs e)
    {
        if (applyingCols || e.ColumnIndex == 0) return;
        int i = e.ColumnIndex - 1;
        if (colOn[i]) colW[i] = Math.Max(40, list.Columns[e.ColumnIndex].Width);
        FitColumns();
    }

    // Restore the window if minimized and pull it to the front (used by notification click
    // and tray-icon double-click).
    void BringToView()
    {
        if (!Visible) Show();
        if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
        bool wasTop = TopMost;
        TopMost = true;          // momentary topmost toggle forces it above other windows
        TopMost = wasTop;
        Activate();
        list.Focus();
    }

    void ShowAbout()
    {
        using (AboutForm f = new AboutForm()) f.ShowDialog(this);
    }

    void SetPausedState()
    {
        Text = cbPause.Checked ? "UsbWatch (paused)" : "UsbWatch";
        lblDot.ForeColor = cbPause.Checked ? Theme.Orange : Theme.Green;
    }

    void FitColumns()
    {
        if (list == null || list.Columns.Count != ColNames.Length + 1) return;
        // The last visible column stretches to fill whatever width is left over.
        int last = -1;
        for (int i = 0; i < ColNames.Length; i++) if (colOn[i]) last = i;
        if (last < 0) return;
        int used = GutterW;
        for (int i = 0; i < ColNames.Length; i++) if (colOn[i] && i != last) used += colW[i];
        bool was = applyingCols;
        applyingCols = true;
        list.Columns[last + 1].Width = Math.Max(100, list.ClientSize.Width - used);
        applyingCols = was;
    }

    void PlaceHint()
    {
        hint.Location = new Point(Math.Max(10, (list.ClientSize.Width - hint.Width) / 2), 70);
    }

    void UpdateHint()
    {
        hint.Visible = list.Items.Count == 0;
        PlaceHint();
    }

    // ---- owner drawing ----

    static Color EventColor(RowInfo r)
    {
        if (r.Event == "PRESENT") return Theme.Comment;
        if (r.Event == "REMOVED") return Theme.Red;
        if (r.Code != 0) return Theme.Orange;
        if (r.Event == "ARRIVED") return Theme.Green;
        return Theme.Cyan;
    }

    static string Glyph(string ev)
    {
        switch (ev)
        {
            case "ARRIVED": return "+";
            case "REMOVED": return "-";
            case "CHANGED": return "~";
            default: return "\u00B7";
        }
    }

    void DrawHeader(object sender, DrawListViewColumnHeaderEventArgs e)
    {
        using (SolidBrush b = new SolidBrush(Theme.Panel)) e.Graphics.FillRectangle(b, e.Bounds);
        using (Pen p = new Pen(Theme.Line)) e.Graphics.DrawLine(p, e.Bounds.X, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
        if (e.ColumnIndex == 0 || e.Bounds.Width <= 0) return;   // gutter and hidden columns: no label
        Rectangle tb = new Rectangle(e.Bounds.X + 6, e.Bounds.Y, e.Bounds.Width - 8, e.Bounds.Height);
        TextRenderer.DrawText(e.Graphics, e.Header.Text.ToUpper(), headFont, tb, Theme.Purple,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
    }

    void DrawCell(object sender, DrawListViewSubItemEventArgs e)
    {
        if (e.ColumnIndex > 0 && e.Bounds.Width <= 0) return;   // hidden column
        RowInfo r = (RowInfo)e.Item.Tag;
        Rectangle b = e.Bounds;
        if (e.ColumnIndex == 0) b = new Rectangle(b.X, b.Y, list.Columns[0].Width, b.Height);

        using (SolidBrush bg = new SolidBrush(e.Item.Selected ? Theme.Line : Theme.Bg))
            e.Graphics.FillRectangle(bg, b);

        Color ec = EventColor(r);
        if (e.ColumnIndex == 0)
        {
            using (SolidBrush ab = new SolidBrush(ec)) e.Graphics.FillRectangle(ab, b.X, b.Y + 3, 3, b.Height - 6);
        }

        if (e.ColumnIndex == 0)
        {
            using (Pen gp = new Pen(Color.FromArgb(70, Theme.Line)))
                e.Graphics.DrawLine(gp, b.X, b.Bottom - 1, b.Right, b.Bottom - 1);
            return;   // the gutter only carries the accent bar
        }

        Color fg = Theme.Fg;
        Font f = listFont;
        string text = e.SubItem.Text;
        switch (e.ColumnIndex)   // 1 = Time ... 7 = Status (0 is the gutter)
        {
            case 1: fg = Theme.Dim; break;
            case 2: fg = ec; f = listBold; text = Glyph(r.Event) + " " + text; break;
            case 3: fg = r.Event == "REMOVED" ? Theme.Dim : Theme.Fg; break;
            case 4: fg = Theme.Dim; break;
            case 5: fg = Theme.Purple; break;
            case 6: fg = Theme.Pink; f = listBold; break;
            case 7:
                if (r.Code != 0) { fg = Theme.Orange; f = listBold; }
                else fg = r.Event == "REMOVED" ? Theme.Dim : Theme.Green;
                break;
        }

        Rectangle tb = new Rectangle(b.X + 6, b.Y, b.Width - 10, b.Height);
        TextRenderer.DrawText(e.Graphics, text, f, tb, fg,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);

        using (Pen p = new Pen(Color.FromArgb(70, Theme.Line)))
            e.Graphics.DrawLine(p, b.X, b.Bottom - 1, b.Right, b.Bottom - 1);
    }

    // ---- window plumbing ----

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        if (m.Msg == WM_DEVICECHANGE && debounce != null)
        {
            debounce.Stop();
            debounce.Start();
        }
    }

    void ExitApp()
    {
        exiting = true;
        Close();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // Only intercept the user clicking X / Alt+F4. Windows shutdown, task-manager close and
        // the tray "Exit" item all go through untouched.
        if (!exiting && cbTray.Checked && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();   // the window and its taskbar button disappear; monitoring and toasts keep running
            if (!trayHintShown)
            {
                trayHintShown = true;
                tray.ShowBalloonTip(2500, "UsbWatch is still running",
                    "Double-click the tray icon to reopen it, or right-click it and choose Exit.",
                    ToolTipIcon.Info);
            }
            return;
        }
        SaveSettings();   // really exiting: record the final window geometry
        base.OnFormClosing(e);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        tray.Visible = false;
        tray.Dispose();
        base.OnFormClosed(e);
    }

    // ---- device querying (background thread) ----

    static Dictionary<string, Dev> Query(bool all)
    {
        Dictionary<string, Dev> result = new Dictionary<string, Dev>(StringComparer.OrdinalIgnoreCase);
        string q = "SELECT Name, PNPClass, PNPDeviceID, ConfigManagerErrorCode FROM Win32_PnPEntity";
        // Keep the WQL simple (no backslashes to escape); the exact prefix check happens below.
        if (!all) q += " WHERE PNPDeviceID LIKE 'USB%' OR PNPDeviceID LIKE 'FTDIBUS%'";
        using (ManagementObjectSearcher s = new ManagementObjectSearcher(q))
        {
            foreach (ManagementBaseObject o in s.Get())
            {
                object id = o["PNPDeviceID"];
                if (id == null) continue;
                string sid = id.ToString();
                if (!all && !(sid.StartsWith("USB\\", StringComparison.OrdinalIgnoreCase)
                           || sid.StartsWith("FTDIBUS\\", StringComparison.OrdinalIgnoreCase))) continue;
                Dev d = new Dev();
                d.Id = sid;
                d.Name = o["Name"] != null ? o["Name"].ToString() : "(unnamed)";
                d.Class = o["PNPClass"] != null ? o["PNPClass"].ToString() : "-";
                d.Code = o["ConfigManagerErrorCode"] != null ? Convert.ToInt32(o["ConfigManagerErrorCode"]) : 0;
                // Code 45 = phantom ("hardware device is not connected") and 21 = Windows is removing
                // the device. Both mean it is already unplugged, so treat it as gone (a clean REMOVED
                // event) instead of reporting a bogus error.
                if (d.Code == 45 || d.Code == 21) continue;
                result[d.Id] = d;
            }
        }
        return result;
    }

    void RequestRefresh()
    {
        if (cbPause.Checked) return;
        if (Interlocked.CompareExchange(ref busy, 1, 0) != 0) { pending = true; return; }
        bool all = !cbUsbOnly.Checked;
        ThreadPool.QueueUserWorkItem(delegate
        {
            Dictionary<string, Dev> snap = null;
            try { snap = Query(all); } catch { }
            try
            {
                BeginInvoke(new Action(delegate
                {
                    busy = 0;
                    if (snap != null && all == !cbUsbOnly.Checked) ApplySnapshot(snap);
                    if (pending) { pending = false; RequestRefresh(); }
                }));
            }
            catch { } // form already closed
        });
    }

    // ---- diffing and display (UI thread) ----

    void ApplySnapshot(Dictionary<string, Dev> cur)
    {
        int problems = 0;
        foreach (Dev d in cur.Values) if (d.Code != 0) problems++;

        if (prev == null)
        {
            prev = cur;
            SetStatus(cur.Count, problems, null);
            return;
        }

        List<KeyValuePair<string, Dev>> events = new List<KeyValuePair<string, Dev>>();
        List<Dev> removed = new List<Dev>(), arrived = new List<Dev>(), changed = new List<Dev>();

        foreach (KeyValuePair<string, Dev> kv in prev)
            if (!cur.ContainsKey(kv.Key)) removed.Add(kv.Value);
        foreach (KeyValuePair<string, Dev> kv in cur)
        {
            Dev old;
            if (!prev.TryGetValue(kv.Key, out old)) arrived.Add(kv.Value);
            else if (old.Code != kv.Value.Code || old.Name != kv.Value.Name) changed.Add(kv.Value);
        }
        Comparison<Dev> byId = delegate(Dev a, Dev b) { return string.Compare(a.Id, b.Id, StringComparison.OrdinalIgnoreCase); };
        removed.Sort(byId); arrived.Sort(byId); changed.Sort(byId);

        foreach (Dev d in removed) if (Show(d)) events.Add(new KeyValuePair<string, Dev>("REMOVED", d));
        foreach (Dev d in arrived) if (Show(d)) events.Add(new KeyValuePair<string, Dev>("ARRIVED", d));
        foreach (Dev d in changed) if (Show(d)) events.Add(new KeyValuePair<string, Dev>("CHANGED", d));

        prev = cur;

        // Insert in reverse so the parent device ends up above its sub-interfaces, newest batch on top.
        int anchor = TopIndex();
        list.BeginUpdate();
        for (int i = events.Count - 1; i >= 0; i--) AddRow(events[i].Key, events[i].Value, true);
        while (list.Items.Count > MaxRows) list.Items.RemoveAt(list.Items.Count - 1);
        list.EndUpdate();
        KeepView(anchor, events.Count);
        UpdateHint();

        string note = null;
        if (events.Count > 0)
        {
            note = "last change " + DateTime.Now.ToString("HH:mm:ss") + ": " + Summarize(removed, arrived, changed);
            TryNotify(removed, arrived, changed);
        }
        SetStatus(cur.Count, problems, note);
    }

    bool Show(Dev d)
    {
        if (cbHideIf.Checked && d.Code == 0 && Regex.IsMatch(d.Id, "&MI_\\d\\d")) return false;
        return true;
    }

    string Summarize(List<Dev> removed, List<Dev> arrived, List<Dev> changed)
    {
        List<string> parts = new List<string>();
        if (arrived.Count > 0) parts.Add(arrived.Count + " arrived");
        if (removed.Count > 0) parts.Add(removed.Count + " removed");
        if (changed.Count > 0) parts.Add(changed.Count + " changed");
        return string.Join(", ", parts.ToArray());
    }

    void TryNotify(List<Dev> removed, List<Dev> arrived, List<Dev> changed)
    {
        if (!cbNotify.Checked) return;

        List<Dev> r = new List<Dev>(), a = new List<Dev>(), c = new List<Dev>();
        foreach (Dev d in removed) if (Show(d)) r.Add(d);
        foreach (Dev d in arrived) if (Show(d)) a.Add(d);
        foreach (Dev d in changed) if (Show(d)) c.Add(d);
        if (r.Count + a.Count + c.Count == 0) return;

        if (inHoldoff)
        {
            // Collect for the summary toast (the rows still show up in the list right away).
            pRemoved.AddRange(r);
            pArrived.AddRange(a);
            pChanged.AddRange(c);
            return;
        }

        inHoldoff = true;
        holdoffTimer.Stop();
        holdoffTimer.Start();
        Notify(r, a, c, false);
    }

    void OnHoldoffElapsed()
    {
        holdoffTimer.Stop();
        List<Dev> r = pRemoved, a = pArrived, c = pChanged;
        pRemoved = new List<Dev>();
        pArrived = new List<Dev>();
        pChanged = new List<Dev>();

        if (r.Count + a.Count + c.Count == 0 || !cbNotify.Checked)
        {
            inHoldoff = false;   // quiet period over; the next change notifies immediately
            return;
        }

        Notify(r, a, c, true);
        holdoffTimer.Start();    // stay in holdoff so a continuing burst keeps collapsing
    }

    void Notify(List<Dev> removed, List<Dev> arrived, List<Dev> changed, bool summary)
    {
        int total = removed.Count + arrived.Count + changed.Count;

        // A device that left and came back inside the same batch is a reconnect, not two events.
        HashSet<string> removedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        HashSet<string> arrivedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Dev d in removed) removedIds.Add(d.Id);
        foreach (Dev d in arrived) arrivedIds.Add(d.Id);

        List<Dev> back = new List<Dev>(), newOnes = new List<Dev>(), gone = new List<Dev>();
        foreach (Dev d in arrived)
        {
            if (removedIds.Contains(d.Id)) back.Add(d); else newOnes.Add(d);
        }
        foreach (Dev d in removed)
            if (!arrivedIds.Contains(d.Id)) gone.Add(d);

        bool bad = false;
        foreach (Dev d in arrived) if (d.Code != 0) bad = true;
        foreach (Dev d in changed) if (d.Code != 0) bad = true;

        StringBuilder sb = new StringBuilder();
        AppendNames(sb, "Connected", newOnes);
        AppendNames(sb, "Reconnected", back);
        AppendNames(sb, "Disconnected", gone);
        AppendNames(sb, "Changed", changed);
        string title = bad ? "USB device problem"
            : summary ? "USB: " + total + " more change" + (total == 1 ? "" : "s")
            : "USB change";
        tray.ShowBalloonTip(3000, title, sb.ToString().Trim(),
            bad ? ToolTipIcon.Warning : ToolTipIcon.Info);
    }

    void AppendNames(StringBuilder sb, string label, List<Dev> devs)
    {
        if (devs.Count == 0) return;
        sb.Append(label).Append(": ");
        for (int i = 0; i < devs.Count && i < 2; i++)
        {
            if (i > 0) sb.Append("; ");
            sb.Append(devs[i].Name);
        }
        if (devs.Count > 2) sb.Append(" (+" + (devs.Count - 2) + " more)");
        sb.AppendLine();
    }

    void SetStatus(int count, int problems, string note)
    {
        lblCount.Text = count + " devices present";
        lblProblems.Text = problems > 0 ? "!  " + problems + " with problems" : "";
        if (note != null) lblLast.Text = note;
    }

    string[] Fields(string tag, Dev d)
    {
        string vidpid = "-";
        Match m = Regex.Match(d.Id, "VID_([0-9A-Fa-f]{4})&PID_([0-9A-Fa-f]{4})");
        if (m.Success) vidpid = (m.Groups[1].Value + ":" + m.Groups[2].Value).ToUpper();
        Match mi = Regex.Match(d.Id, "&MI_(\\d\\d)");
        if (mi.Success) vidpid += "  MI_" + mi.Groups[1].Value;

        Match com = Regex.Match(d.Name, "\\((COM\\d+)\\)");
        string port = com.Success ? com.Groups[1].Value : "";

        string state = tag == "REMOVED" ? "disconnected" : "OK";
        if (d.Code != 0 && tag != "REMOVED")
        {
            string txt;
            state = "ERROR " + d.Code + (Problems.TryGetValue(d.Code, out txt) ? ": " + txt : "");
        }
        // The leading empty string is the gutter column.
        return new string[] { "", DateTime.Now.ToString("HH:mm:ss"), tag, d.Name, d.Class, vidpid, port, state };
    }

    void AddRow(string tag, Dev d, bool log)
    {
        string[] f = Fields(tag, d);
        ListViewItem item = new ListViewItem(f);
        RowInfo info = new RowInfo();
        info.Id = d.Id;
        info.Event = tag;
        info.Code = d.Code;
        item.Tag = info;
        list.Items.Insert(0, item);

        if (log && cbLog.Checked)
        {
            try
            {
                File.AppendAllText(logPath, DateTime.Now.ToString("yyyy-MM-dd ") + string.Join("\t", f, 1, f.Length - 1) + "\t" + d.Id + Environment.NewLine);
            }
            catch { }
        }
    }

    void ListCurrent()
    {
        if (prev == null) return;
        List<Dev> devs = new List<Dev>(prev.Values);
        devs.Sort(delegate(Dev a, Dev b) { return string.Compare(b.Id, a.Id, StringComparison.OrdinalIgnoreCase); });
        int anchor = TopIndex();
        list.BeginUpdate();
        foreach (Dev d in devs) AddRow("PRESENT", d, true);   // snapshots are logged too (tagged PRESENT)
        list.EndUpdate();
        KeepView(anchor, devs.Count);
        UpdateHint();
    }

    // Newest rows are inserted at the top. With auto-scroll on, jump back to the top so the
    // latest event is always visible. With it off, hold the view on whatever the user was
    // reading (if they were already at the top, new rows simply appear there).
    int TopIndex()
    {
        ListViewItem t = list.TopItem;
        return t == null ? 0 : t.Index;
    }

    void KeepView(int anchor, int added)
    {
        if (list.Items.Count == 0 || added == 0) return;
        if (cbAuto.Checked)
        {
            list.EnsureVisible(0);
        }
        else if (anchor > 0)
        {
            int idx = Math.Min(anchor + added, list.Items.Count - 1);
            list.TopItem = list.Items[idx];
        }
    }

    void CopyRows(bool idsOnly) { CopyRows(idsOnly, false); }

    void CopyRows(bool idsOnly, bool everything)
    {
        StringBuilder sb = new StringBuilder();
        List<ListViewItem> items = everything
            ? new List<ListViewItem>(Cast(list.Items))
            : new List<ListViewItem>(Cast(list.SelectedItems));
        foreach (ListViewItem i in items)
        {
            if (idsOnly) sb.AppendLine(((RowInfo)i.Tag).Id);
            else
            {
                string[] cols = new string[i.SubItems.Count - 1];   // skip the gutter column
                for (int k = 0; k < cols.Length; k++) cols[k] = i.SubItems[k + 1].Text;
                sb.AppendLine(string.Join("\t", cols));
            }
        }
        if (sb.Length > 0) Clipboard.SetText(sb.ToString());
    }

    static IEnumerable<ListViewItem> Cast(System.Collections.IEnumerable src)
    {
        foreach (object o in src) yield return (ListViewItem)o;
    }

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.Run(new MainForm());
    }
}
