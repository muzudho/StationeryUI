namespace StationeryUI.Windows;

using StationeryUI.Inspection;
using System.Globalization;

/// <summary>A modeless inspector with its own STA message loop. Pass snapshots from the game thread.</summary>
public sealed class StationeryDeveloperWindow : IDisposable
{
    private readonly object gate = new();
    private StationeryInspectionEntry[] snapshot = [];
    private Thread? thread;
    private bool requested;
    private bool visible;
    private bool disposed;
    public bool IsOpen { get { lock (gate) return visible || requested; } }

    public void Show(IReadOnlyList<StationeryInspectionEntry> entries)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            snapshot = entries.ToArray();
            requested = true;
            if (thread is not null) return;
            thread = new Thread(Run) { IsBackground = true, Name = "StationeryUI F12 developer window" };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }
    }

    public void Update(IReadOnlyList<StationeryInspectionEntry> entries)
    {
        lock (gate)
        {
            if (!disposed) snapshot = entries.ToArray();
        }
    }

    private void Run()
    {
        using var form = new InspectorForm();
        using var timer = new System.Windows.Forms.Timer { Interval = 150 };
        var stopping = false;
        form.FormClosing += (_, e) =>
        {
            if (stopping) return;
            e.Cancel = true;
            form.Hide();
            lock (gate) visible = false;
        };
        form.VisibleChanged += (_, _) => { lock (gate) visible = form.Visible; };
        timer.Tick += (_, _) =>
        {
            StationeryInspectionEntry[] current;
            bool show;
            lock (gate)
            {
                stopping = disposed;
                current = snapshot;
                show = requested;
                requested = false;
            }
            if (stopping)
            {
                timer.Stop();
                form.Close();
                Application.ExitThread();
                return;
            }
            if (show)
            {
                form.Show();
                if (form.WindowState == FormWindowState.Minimized) form.WindowState = FormWindowState.Normal;
                form.Activate();
            }
            if (form.Visible) form.RefreshSnapshot(current);
        };
        timer.Start();
        Application.Run();
    }

    public void Dispose()
    {
        Thread? current;
        lock (gate)
        {
            if (disposed) return;
            disposed = true;
            requested = visible = false;
            current = thread;
        }
        if (current is not null && current != Thread.CurrentThread) current.Join(2000);
    }

    private sealed class InspectorForm : Form
    {
        private readonly TreeView tree = new() { Dock = DockStyle.Fill, HideSelection = false, AccessibleName = "文房具の階層" };
        private readonly TextBox details = new() { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true,
            ScrollBars = ScrollBars.Both, WordWrap = false, AccessibleName = "選択した文房具の情報" };
        private readonly Button copy = new() { Text = "パスをコピー", Dock = DockStyle.Bottom, Height = 40, Enabled = false };
        private readonly Dictionary<string, TreeNode> nodes = new(StringComparer.Ordinal);
        private Dictionary<string, StationeryInspectionEntry> entries = new(StringComparer.Ordinal);

        public InspectorForm()
        {
            Text = "F12 開発者ウィンドウ — 文房具UI";
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new(900, 520);
            MinimumSize = new(640, 360);
            StartPosition = FormStartPosition.CenterScreen;
            KeyPreview = true;
            Font = new System.Drawing.Font("Yu Gothic UI", 10);
            var split = new SplitContainer { Dock = DockStyle.Fill, Size = ClientSize, SplitterDistance = 350 };
            split.Panel1.Controls.Add(tree);
            split.Panel2.Controls.Add(details);
            split.Panel2.Controls.Add(copy);
            Controls.Add(split);
            Controls.Add(new Label { Dock = DockStyle.Top, Height = 52, Padding = new(12, 8, 12, 4),
                Text = "文房具を選んで Id と完全パスを確認できます。F12 / Esc で閉じます。\nId は C# コードで付ける名前です。ここでは編集しません。" });
            tree.AfterSelect += (_, _) => UpdateDetails();
            copy.Click += (_, _) =>
            {
                if (tree.SelectedNode?.Name is not { } path) return;
                try { Clipboard.SetText(path); }
                catch (System.Runtime.InteropServices.ExternalException)
                {
                    MessageBox.Show(this, "クリップボードを使用できません。もう一度お試しください。", Text);
                }
            };
            KeyDown += (_, e) =>
            {
                if (e.KeyCode is Keys.F12 or Keys.Escape) { e.SuppressKeyPress = true; Close(); }
            };
        }

        public void RefreshSnapshot(StationeryInspectionEntry[] snapshot)
        {
            entries = snapshot.ToDictionary(entry => entry.Path, StringComparer.Ordinal);
            if (!nodes.Keys.SequenceEqual(entries.Keys))
            {
                var selected = tree.SelectedNode?.Name;
                tree.BeginUpdate();
                tree.Nodes.Clear();
                nodes.Clear();
                foreach (var entry in snapshot) nodes.Add(entry.Path, new TreeNode { Name = entry.Path });
                foreach (var entry in snapshot)
                {
                    var node = nodes[entry.Path];
                    if (entry.ParentPath is not null && nodes.TryGetValue(entry.ParentPath, out var parent)) parent.Nodes.Add(node);
                    else tree.Nodes.Add(node);
                }
                tree.ExpandAll();
                tree.SelectedNode = selected is not null && nodes.TryGetValue(selected, out var previous)
                    ? previous : tree.Nodes.Cast<TreeNode>().FirstOrDefault();
                tree.EndUpdate();
            }
            foreach (var entry in snapshot)
            {
                var node = nodes[entry.Path];
                node.Text = $"{entry.Id}  [{entry.Kind}]" + (entry.Visible ? "" : "  （非表示）");
                node.ForeColor = entry.Visible ? System.Drawing.SystemColors.WindowText : System.Drawing.SystemColors.GrayText;
            }
            UpdateDetails();
        }

        private void UpdateDetails()
        {
            if (tree.SelectedNode?.Name is not { } path || !entries.TryGetValue(path, out var entry))
            {
                copy.Enabled = false;
                details.Text = "文房具を選択してください。";
                return;
            }
            copy.Enabled = true;
            var bounds = entry.WindowBounds is { } b
                ? string.Create(CultureInfo.InvariantCulture, $"X={b.X:0.##}, Y={b.Y:0.##}, 幅={b.Width:0.##}, 高さ={b.Height:0.##}") : "—";
            var text = $"文房具 Id: {entry.Id}\r\n\r\n完全パス: {entry.Path}\r\n\r\n種類: {entry.Kind}\r\n名前: {entry.Label}\r\n表示: {(entry.Visible ? "表示中" : "非表示")}\r\n\r\nウィンドウ内の位置（px）:\r\n{bounds}";
            if (details.Text != text) details.Text = text;
        }
    }
}
