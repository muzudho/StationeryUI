using System.Text.Json;

internal sealed partial class DesignerGame
{
    private void ValidateGridCounts()
    {
        if (!editingPage || !blueprint.CanEditGrid) return;
        if (!int.TryParse(columns.Editor!.Text, out var c) || !int.TryParse(rows.Editor!.Text, out var r)
            || c is < 1 or > 8 || r is < 1 or > 8)
            throw new JsonException("列数・行数は1～8の整数を入力してください。");
        if (c != blueprint.Columns.Count || r != blueprint.Rows.Count)
            throw new JsonException("列数・行数の変更を確定してください。");
    }

    private void ResetGridCounts()
    {
        if (!blueprint.CanEditGrid) return;
        SetText(columns, blueprint.Columns.Count.ToString());
        SetText(rows, blueprint.Rows.Count.ToString());
    }

    private bool ApplyGridCounts()
    {
        if (ui.IsComposing) return true;
        if (!int.TryParse(columns.Editor!.Text, out var c) || !int.TryParse(rows.Editor!.Text, out var r)
            || c is < 1 or > 8 || r is < 1 or > 8)
            throw new JsonException("列数・行数は1～8の整数を入力してください。");
        if (c == blueprint.Columns.Count && r == blueprint.Rows.Count) return true;
        if (c < blueprint.Columns.Count || r < blueprint.Rows.Count)
        {
            StartUtilityDialog($"列数 {c} ／ 行数 {r} に縮小します。範囲外のセルを削除してよいですか？", false);
            utilityDialog!.AddButton("confirmShrink", new(204, 368, 440, 44), "縮小する", () => utilityAction = () =>
            {
                Guard(() => ResizeGrid(c, r));
                CloseUtilityDialog();
            });
            return false;
        }
        ResizeGrid(c, r);
        return true;
    }

    private void ResizeGrid(int c, int r)
    {
        var focus = ui.Focus.FocusedId;
        try { blueprint.Resize(c, r); }
        catch { ResetGridCounts(); throw; }
        selectedRow = Math.Min(selectedRow, r - 1); selectedColumn = Math.Min(selectedColumn, c - 1);
        BuildUi();
        ui.Viewport.Scale = BodyScale; ui.Viewport.Offset = new(320 * BodyScale, BodyTop);
        if (focus == columns.Path || focus == rows.Path) ui.Focus.Focus(focus);
        message = "列数・行数を反映しました。";
    }
}
