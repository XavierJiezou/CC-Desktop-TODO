using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace DesktopTodo
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TodoForm());
        }
    }

    // 单条任务
    public class TodoItem
    {
        public string Text;
        public bool Completed;
        public TodoItem(string text, bool completed) { Text = text; Completed = completed; }
    }

    public class TodoForm : Form
    {
        // 圆角窗口
        [DllImport("gdi32.dll")]
        static extern IntPtr CreateRoundRectRgn(int x1, int y1, int x2, int y2, int cx, int cy);

        // 深色主题配色
        static readonly Color PanelBg = Color.FromArgb(24, 26, 32);
        static readonly Color FgColor = Color.FromArgb(240, 241, 245);
        static readonly Color MutedColor = Color.FromArgb(154, 160, 173);
        static readonly Color AccentColor = Color.FromArgb(91, 157, 255);
        static readonly Color RowHover = Color.FromArgb(40, 43, 52);
        static readonly Color InputBg = Color.FromArgb(38, 41, 50);

        const string RunKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
        const string AppRegName = "CC-Desktop-TODO";

        readonly string dataDir;
        readonly string todosFile;
        readonly string configFile;

        List<TodoItem> todos = new List<TodoItem>();

        Panel titleBar;
        Label titleLabel;
        TrackBar opacityBar;
        Button autostartBtn;
        Button minBtn;
        Button closeBtn;
        TextBox inputBox;
        Button addBtn;
        FlowLayoutPanel listPanel;
        Label emptyLabel;

        bool dragging;
        Point dragStart;

        public TodoForm()
        {
            dataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "CC-Desktop-TODO");
            Directory.CreateDirectory(dataDir);
            todosFile = Path.Combine(dataDir, "todos.txt");
            configFile = Path.Combine(dataDir, "config.txt");

            InitForm();
            BuildUi();
            LoadConfig();   // 恢复位置/尺寸/透明度
            LoadTodos();
            RenderList();
            UpdateAutostartButton();
        }

        // ---- 窗体基础设置 ----
        void InitForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            TopMost = true;
            ShowInTaskbar = true;       // 保留任务栏按钮，方便最小化后恢复
            StartPosition = FormStartPosition.Manual;
            BackColor = PanelBg;
            ForeColor = FgColor;
            Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
            MinimumSize = new Size(220, 180);
            ClientSize = new Size(320, 420);
            Text = "桌面TODO";
            KeyPreview = true;
        }

        void BuildUi()
        {
            // 顶栏
            titleBar = new Panel();
            titleBar.Dock = DockStyle.Top;
            titleBar.Height = 38;
            titleBar.BackColor = PanelBg;
            Controls.Add(titleBar);

            titleLabel = new Label();
            titleLabel.Text = "待办";
            titleLabel.ForeColor = MutedColor;
            titleLabel.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
            titleLabel.AutoSize = true;
            titleLabel.Location = new Point(12, 10);
            titleBar.Controls.Add(titleLabel);

            MakeDraggable(titleBar);
            MakeDraggable(titleLabel);

            closeBtn = MakeIconButton("✕");   // ✕
            closeBtn.Click += delegate { Close(); };
            closeBtn.MouseEnter += delegate { closeBtn.BackColor = Color.FromArgb(224, 68, 62); closeBtn.ForeColor = Color.White; };
            closeBtn.MouseLeave += delegate { closeBtn.BackColor = PanelBg; closeBtn.ForeColor = MutedColor; };
            titleBar.Controls.Add(closeBtn);

            minBtn = MakeIconButton("—");      // —
            minBtn.Click += delegate { WindowState = FormWindowState.Minimized; };
            titleBar.Controls.Add(minBtn);

            autostartBtn = MakeIconButton("⏻"); // ⏻
            autostartBtn.Click += delegate { ToggleAutostart(); };
            titleBar.Controls.Add(autostartBtn);

            opacityBar = new TrackBar();
            opacityBar.Minimum = 25;
            opacityBar.Maximum = 100;
            opacityBar.Value = 85;
            opacityBar.TickStyle = TickStyle.None;
            opacityBar.Width = 80;
            opacityBar.Height = 26;
            opacityBar.BackColor = PanelBg;
            opacityBar.Scroll += delegate { Opacity = opacityBar.Value / 100.0; };
            opacityBar.MouseUp += delegate { SaveConfig(); };
            titleBar.Controls.Add(opacityBar);

            titleBar.Resize += delegate { LayoutTitleBar(); };
            LayoutTitleBar();

            // 输入行
            Panel addRow = new Panel();
            addRow.Dock = DockStyle.Top;
            addRow.Height = 42;
            addRow.BackColor = PanelBg;
            Controls.Add(addRow);

            inputBox = new TextBox();
            inputBox.BorderStyle = BorderStyle.FixedSingle;
            inputBox.BackColor = InputBg;
            inputBox.ForeColor = FgColor;
            inputBox.Font = new Font("Microsoft YaHei UI", 10F);
            inputBox.Location = new Point(10, 8);
            inputBox.Width = 250;
            inputBox.MaxLength = 200;
            inputBox.KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; AddTask(); }
            };
            addRow.Controls.Add(inputBox);

            addBtn = new Button();
            addBtn.Text = "＋"; // ＋
            addBtn.FlatStyle = FlatStyle.Flat;
            addBtn.FlatAppearance.BorderSize = 0;
            addBtn.BackColor = AccentColor;
            addBtn.ForeColor = Color.White;
            addBtn.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            addBtn.Size = new Size(34, 26);
            addBtn.Click += delegate { AddTask(); };
            addRow.Controls.Add(addBtn);

            addRow.Resize += delegate
            {
                inputBox.Width = addRow.ClientSize.Width - 10 - 34 - 8;
                addBtn.Location = new Point(addRow.ClientSize.Width - 34 - 8, 8);
            };

            // 列表区
            listPanel = new FlowLayoutPanel();
            listPanel.Dock = DockStyle.Fill;
            listPanel.FlowDirection = FlowDirection.TopDown;
            listPanel.WrapContents = false;
            listPanel.AutoScroll = true;
            listPanel.BackColor = PanelBg;
            listPanel.Padding = new Padding(6, 0, 6, 6);
            Controls.Add(listPanel);
            listPanel.BringToFront();

            emptyLabel = new Label();
            emptyLabel.Text = "还没有任务，加一条吧 ✦";
            emptyLabel.ForeColor = MutedColor;
            emptyLabel.AutoSize = false;
            emptyLabel.TextAlign = ContentAlignment.MiddleCenter;

            listPanel.SizeChanged += delegate { FitRows(); };

            // 修正停靠顺序：索引越高越靠上。titleBar 最上，addRow 居中，listPanel 填充
            Controls.SetChildIndex(listPanel, 0);
            Controls.SetChildIndex(addRow, 1);
            Controls.SetChildIndex(titleBar, 2);
        }

        void LayoutTitleBar()
        {
            int right = titleBar.ClientSize.Width - 6;
            closeBtn.Location = new Point(right - 26, 8);
            minBtn.Location = new Point(right - 52, 8);
            autostartBtn.Location = new Point(right - 78, 8);
            opacityBar.Location = new Point(right - 78 - 86, 9);
        }

        Button MakeIconButton(string glyph)
        {
            Button b = new Button();
            b.Text = glyph;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.BackColor = PanelBg;
            b.ForeColor = MutedColor;
            b.Font = new Font("Segoe UI", 9F);
            b.Size = new Size(24, 22);
            b.TabStop = false;
            return b;
        }

        // ---- 拖动 ----
        void MakeDraggable(Control c)
        {
            c.MouseDown += delegate(object s, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left) { dragging = true; dragStart = e.Location; }
            };
            c.MouseMove += delegate(object s, MouseEventArgs e)
            {
                if (dragging) { Left += e.X - dragStart.X; Top += e.Y - dragStart.Y; }
            };
            c.MouseUp += delegate { if (dragging) { dragging = false; SaveConfig(); } };
        }

        // ---- 任务渲染 ----
        void RenderList()
        {
            listPanel.SuspendLayout();
            listPanel.Controls.Clear();

            if (todos.Count == 0)
            {
                listPanel.Controls.Add(emptyLabel);
                emptyLabel.Width = listPanel.ClientSize.Width - 12;
                emptyLabel.Height = 120;
            }
            else
            {
                foreach (TodoItem item in todos)
                    listPanel.Controls.Add(CreateRow(item));
                FitRows();
            }
            listPanel.ResumeLayout();
        }

        Panel CreateRow(TodoItem item)
        {
            Panel row = new Panel();
            row.Height = 32;
            row.Margin = new Padding(0, 2, 0, 2);
            row.BackColor = PanelBg;

            CheckBox cb = new CheckBox();
            cb.Checked = item.Completed;
            cb.AutoSize = false;
            cb.Size = new Size(18, 18);
            cb.Location = new Point(6, 7);
            cb.FlatStyle = FlatStyle.Standard;

            Label txt = new Label();
            txt.Text = item.Text;
            txt.AutoSize = false;
            txt.Location = new Point(30, 0);
            txt.Height = 32;
            txt.TextAlign = ContentAlignment.MiddleLeft;
            txt.ForeColor = item.Completed ? MutedColor : FgColor;
            txt.Font = MakeRowFont(item.Completed);

            Button del = new Button();
            del.Text = "✕";
            del.FlatStyle = FlatStyle.Flat;
            del.FlatAppearance.BorderSize = 0;
            del.BackColor = PanelBg;
            del.ForeColor = MutedColor;
            del.Size = new Size(22, 22);
            del.Visible = false;
            del.TabStop = false;

            cb.CheckedChanged += delegate
            {
                item.Completed = cb.Checked;
                txt.ForeColor = item.Completed ? MutedColor : FgColor;
                txt.Font = MakeRowFont(item.Completed);
                SaveTodos();
            };

            del.Click += delegate
            {
                todos.Remove(item);
                SaveTodos();
                RenderList();
            };

            txt.DoubleClick += delegate { StartEdit(row, txt, item); };

            // hover 显示删除按钮 + 行高亮
            EventHandler enter = delegate { row.BackColor = RowHover; del.Visible = true; };
            EventHandler leave = delegate
            {
                Point p = row.PointToClient(Cursor.Position);
                if (!row.ClientRectangle.Contains(p)) { row.BackColor = PanelBg; del.Visible = false; }
            };
            row.MouseEnter += enter; row.MouseLeave += leave;
            txt.MouseEnter += enter; txt.MouseLeave += leave;
            cb.MouseEnter += enter;
            del.MouseEnter += enter;
            del.MouseLeave += leave;

            row.Controls.Add(cb);
            row.Controls.Add(txt);
            row.Controls.Add(del);
            row.Tag = del; // 方便 FitRows 定位
            return row;
        }

        Font MakeRowFont(bool completed)
        {
            return new Font("Microsoft YaHei UI", 9.5F,
                completed ? FontStyle.Strikeout : FontStyle.Regular);
        }

        void FitRows()
        {
            int w = listPanel.ClientSize.Width - listPanel.Padding.Horizontal;
            foreach (Control c in listPanel.Controls)
            {
                Panel row = c as Panel;
                if (row == null) continue;
                row.Width = w;
                Button del = row.Tag as Button;
                if (del != null) del.Location = new Point(row.Width - 26, 5);
                foreach (Control rc in row.Controls)
                {
                    Label lbl = rc as Label;
                    if (lbl != null) lbl.Width = row.Width - 30 - 28;
                }
            }
        }

        void StartEdit(Panel row, Label txt, TodoItem item)
        {
            TextBox tb = new TextBox();
            tb.Text = item.Text;
            tb.BorderStyle = BorderStyle.FixedSingle;
            tb.BackColor = InputBg;
            tb.ForeColor = FgColor;
            tb.Font = new Font("Microsoft YaHei UI", 9.5F);
            tb.Bounds = new Rectangle(txt.Left, 5, txt.Width, 22);
            txt.Visible = false;
            row.Controls.Add(tb);
            tb.BringToFront();
            tb.Focus();
            tb.SelectAll();

            bool done = false;
            EventHandler commit = delegate
            {
                if (done) return;
                done = true;
                string v = tb.Text.Trim();
                if (v.Length > 0) item.Text = v;
                row.Controls.Remove(tb);
                tb.Dispose();
                txt.Text = item.Text;
                txt.Visible = true;
                SaveTodos();
            };

            tb.KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; commit(null, null); }
                else if (e.KeyCode == Keys.Escape)
                {
                    done = true;
                    row.Controls.Remove(tb);
                    tb.Dispose();
                    txt.Visible = true;
                }
            };
            tb.Leave += commit;
        }

        // ---- 操作 ----
        void AddTask()
        {
            string v = inputBox.Text.Trim();
            if (v.Length == 0) return;
            todos.Add(new TodoItem(v, false));
            inputBox.Clear();
            SaveTodos();
            RenderList();
        }

        // ---- 持久化 ----
        void LoadTodos()
        {
            todos = new List<TodoItem>();
            try
            {
                if (!File.Exists(todosFile)) return;
                foreach (string line in File.ReadAllLines(todosFile))
                {
                    if (line.Length == 0) continue;
                    int sep = line.IndexOf('|');
                    if (sep < 0) { todos.Add(new TodoItem(line, false)); continue; }
                    bool done = line.Substring(0, sep) == "1";
                    string text = line.Substring(sep + 1);
                    todos.Add(new TodoItem(text, done));
                }
            }
            catch { }
        }

        void SaveTodos()
        {
            try
            {
                List<string> lines = new List<string>();
                foreach (TodoItem t in todos)
                    lines.Add((t.Completed ? "1" : "0") + "|" + t.Text.Replace("\r", " ").Replace("\n", " "));
                File.WriteAllLines(todosFile, lines.ToArray());
            }
            catch { }
        }

        void LoadConfig()
        {
            int x = -1, y = -1, w = ClientSize.Width, h = ClientSize.Height, op = 85;
            try
            {
                if (File.Exists(configFile))
                {
                    foreach (string line in File.ReadAllLines(configFile))
                    {
                        int eq = line.IndexOf('=');
                        if (eq < 0) continue;
                        string key = line.Substring(0, eq);
                        string val = line.Substring(eq + 1);
                        int n;
                        if (!int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out n)) continue;
                        if (key == "x") x = n;
                        else if (key == "y") y = n;
                        else if (key == "w") w = n;
                        else if (key == "h") h = n;
                        else if (key == "opacity") op = n;
                    }
                }
            }
            catch { }

            if (op < 25) op = 25; if (op > 100) op = 100;
            opacityBar.Value = op;
            Opacity = op / 100.0;
            ClientSize = new Size(Math.Max(w, MinimumSize.Width), Math.Max(h, MinimumSize.Height));

            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            if (x < 0 || y < 0)
            {
                x = wa.Right - Width - 40;
                y = wa.Top + 60;
            }
            // 防止窗口跑到屏幕外
            if (x > wa.Right - 40) x = wa.Right - Width - 40;
            if (y > wa.Bottom - 40) y = wa.Bottom - Height - 40;
            if (x < wa.Left) x = wa.Left;
            if (y < wa.Top) y = wa.Top;
            Location = new Point(x, y);
        }

        void SaveConfig()
        {
            try
            {
                string[] lines = new string[]
                {
                    "x=" + Location.X.ToString(CultureInfo.InvariantCulture),
                    "y=" + Location.Y.ToString(CultureInfo.InvariantCulture),
                    "w=" + ClientSize.Width.ToString(CultureInfo.InvariantCulture),
                    "h=" + ClientSize.Height.ToString(CultureInfo.InvariantCulture),
                    "opacity=" + opacityBar.Value.ToString(CultureInfo.InvariantCulture)
                };
                File.WriteAllLines(configFile, lines);
            }
            catch { }
        }

        // ---- 开机自启 ----
        bool IsAutostartOn()
        {
            try
            {
                RegistryKey rk = Registry.CurrentUser.OpenSubKey(RunKey, false);
                if (rk == null) return false;
                object v = rk.GetValue(AppRegName);
                rk.Close();
                return v != null;
            }
            catch { return false; }
        }

        void ToggleAutostart()
        {
            try
            {
                RegistryKey rk = Registry.CurrentUser.OpenSubKey(RunKey, true);
                if (rk == null) rk = Registry.CurrentUser.CreateSubKey(RunKey);
                if (IsAutostartOn()) rk.DeleteValue(AppRegName, false);
                else rk.SetValue(AppRegName, "\"" + Application.ExecutablePath + "\"");
                rk.Close();
            }
            catch { }
            UpdateAutostartButton();
        }

        void UpdateAutostartButton()
        {
            bool on = IsAutostartOn();
            autostartBtn.ForeColor = on ? AccentColor : MutedColor;
            // tooltip
            ToolTip tt = new ToolTip();
            tt.SetToolTip(autostartBtn, on ? "开机自启动：开" : "开机自启动：关");
        }

        // ---- 圆角 + 边缘缩放 + 关闭保存 ----
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (Width > 0 && Height > 0)
                Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, Width + 1, Height + 1, 14, 14));
        }

        protected override void OnResizeEnd(EventArgs e)
        {
            base.OnResizeEnd(e);
            SaveConfig();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            SaveConfig();
            SaveTodos();
            base.OnFormClosing(e);
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x84;
            const int HTRIGHT = 11, HTBOTTOM = 15, HTBOTTOMRIGHT = 17;
            if (m.Msg == WM_NCHITTEST)
            {
                base.WndProc(ref m);
                int lp = m.LParam.ToInt32();
                int sx = (short)(lp & 0xFFFF);
                int sy = (short)((lp >> 16) & 0xFFFF);
                Point p = PointToClient(new Point(sx, sy));
                int grip = 14;
                bool right = p.X >= ClientSize.Width - grip;
                bool bottom = p.Y >= ClientSize.Height - grip;
                if (right && bottom) m.Result = (IntPtr)HTBOTTOMRIGHT;
                else if (right) m.Result = (IntPtr)HTRIGHT;
                else if (bottom) m.Result = (IntPtr)HTBOTTOM;
                return;
            }
            base.WndProc(ref m);
        }
    }
}
