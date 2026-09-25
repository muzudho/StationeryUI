using Microsoft.Xna.Framework.Input;
using StationeryUI.MonoGame;
using StationeryUI.Windows;

internal sealed partial class DesignerGame
{
    private StationeryUiHost inspector = null!;
    private StationeryUiHost.Element toolHint = null!;
    private StationeryUiHost.Element saveLabel = null!, saveBar = null!, saveBarTrack = null!;
    private MouseState inspectorMouse;
    private const int InspectorHeight = 80;
    private double BodyScale => Math.Max(.1, Math.Min(GraphicsDevice.Viewport.Width / 1600.0,
        Math.Max(1, GraphicsDevice.Viewport.Height - InspectorHeight - BodyTop) / 850.0));

    private void BuildInspector()
    {
        inspector = new(GraphicsDevice, input, family => new WindowsTextRasterizer(family));
        toolHint = inspector.AddTextBlock(inspector.Root.AddChild("toolHint", "textBlock"), new(), "");
        status = inspector.AddTextBlock(inspector.Root.AddChild("status", "textBlock"), new(), message);
        saveLabel = inspector.AddTextBlock(inspector.Root.AddChild("autoSaveStatus", "textBlock"), new(), "");
        saveBarTrack = inspector.AddTextBlock(inspector.Root.AddChild("autoSaveTrack", "textBlock"), new(), "");
        saveBar = inspector.AddTextBlock(inspector.Root.AddChild("autoSaveTimer", "textBlock"), new(), "");
    }

    private string? DesignerToolHint(StationeryUiHost.Element element) => element.Id switch
    {
        "new" => "新しい設計を始めます。現在のプランは置き換わるため、必要な内容は先にエクスポートしてください。",
        "resume" => hasDraft ? "メモリー上に残っているプランの編集を再開します。" : "再開できる編集がありません。新規作成か既存ファイルの選択から始めてください。",
        "open" or "chooseFile" => "JSON を開き、連番の .bak を作成します。変更は最後の入力から1.5秒後に元ファイルへ自動保存します。",
        "back" => "1ページ目へ戻ります。現在のプランは「現在の編集を再開」で続けられます。",
        "columns" or "columnsLabel" => "列数（1～8）を入力すると自動反映します。縮小するときは確認画面が開きます。",
        "rows" or "rowsLabel" => "行数（1～8）を入力すると自動反映します。縮小するときは確認画面が開きます。",
        "confirmShrink" => "範囲外のセルを削除して列数・行数を反映します。閉じる／Esc では元の数に戻します。",
        "columnTracksTitle" => "列ごとの幅を数値と単位で指定します。px は固定幅、rate は残りの幅の配分比です。",
        "rowTracksTitle" => "行ごとの高さを数値と単位で指定します。px は固定高さ、rate は残りの高さの配分比です。",
        "theme" => "明るいテーマと暗いテーマを切り替えます。",
        "kind" => blueprint.IsImported ? "既存モデルの種類は保持します。" : "プレビューで選んだセルの文房具の種類を切り替えます。",
        "label" => blueprint.IsImported ? "このセルに配置されたモデルを表示しています。" : "選択したセルの表示名を入力します。右のプレビューへ反映されます。",
        "output" => "新しい JSON の出力先。末尾は .stationery-style.json にします。既存ファイルは上書きしません。",
        "export" => "出力先を設定するエクスポートダイアログを開きます。",
        "writeOutput" => "指定パスへ書き出し、以後のオートセーブ先にします。編集中の元ファイルと同じパスなら即時保存します。",
        "restore" => "バックアップのファイル名と変更日時を確認し、選んだセーブポイントへ戻します。最大20世代を保持します。",
        "chooseFolder" => "新規作成先のフォルダーを Windows のダイアログで選びます。",
        "createFile" => selectedOutputFolder is null ? "先にフォルダーを選択してください。" : "選択フォルダーへ現在の設計を作成します。同名があれば連番にして上書きを避けます。",
        "styleTree" => "＋／－で開閉。水色の枠が操作対象です。layouts 内の box-layout や表をクリックすると設定を編集できます。",
        "addChild" => "layouts、子のないボックス、グリッドを選んで子レイアウトを追加します。グリッド内では row・col・rowspan・colspan を指定します。",
        "deleteNode" => "layouts 内の操作対象を削除します。modelTree・controlTree は閲覧専用です。参照や必須設定を壊す削除も拒否します。",
        "renameId" => "layouts 内の Id を変更します。既存の controlTree の参照は自動で追従します。modelTree・controlTree は閲覧専用です。",
        "stationeryId" => "英字・数字・アンダースコアのみ。camelCase を推奨します。数字始まりなどの警告があっても確定できます。",
        "panel" => "指定した Id の box-layout を追加し、margin・padding の設定画面を開きます。border はレイアウトに影響しません。",
        "floating" => "指定した Id の grid-layout を追加し、列幅・行高を設定します。",
        "confirmId" => "入力した Id に変更します。既存の controlTree の参照も追従します。",
        "cancel" => utilityDialog is not null
            ? exportDialog ? "ダイアログを閉じます。書き出し済みのファイルは保持します。Esc キーでも閉じられます。"
                : "縮小を取り消して元の列数・行数に戻します。Esc キーでも閉じられます。"
            : "変更を確定せず、ダイアログを閉じます。Esc キーでも閉じられます。",
        _ when element.Id.StartsWith("edge", StringComparison.Ordinal) => element.AccessibleName + "：0以上の px 値。margin は外側、padding は内側の余白。border は全体のサイズ計算に含めません。",
        _ when element.Id.EndsWith("Unit", StringComparison.Ordinal) => "px は固定サイズ、rate は残りの領域を分け合う比です。クリックで単位を切り替えます。",
        _ when element.Editor is not null => element.AccessibleName + "。0以上の数値を入力してください。配置プレビューに即時反映します。",
        _ => null
    };

    private void DrawInspector()
    {
        var height = Math.Min(InspectorHeight, GraphicsDevice.Viewport.Height);
        var y = GraphicsDevice.Viewport.Height - height;
        inspector.Theme = theme with { FontSize = 14, Padding = 2 };
        toolHint.Bounds = new(0, y, GraphicsDevice.Viewport.Width, Math.Min(56, height));
        status.Bounds = new(0, y + Math.Min(56, height), GraphicsDevice.Viewport.Width * .5, Math.Max(0, height - 56));
        status.Theme = theme with { FontSize = 12, Padding = 1 };
        var saveX = GraphicsDevice.Viewport.Width * .5;
        saveLabel.Bounds = new(saveX, y + Math.Min(56, height), saveX, Math.Max(0, height - 62));
        saveLabel.Label = SaveState;
        saveLabel.Theme = status.Theme;
        saveBarTrack.Bounds = new(saveX, GraphicsDevice.Viewport.Height - 6, saveX, 6);
        saveBarTrack.Theme = theme with { Surface = theme.Border };
        saveBar.Bounds = saveBarTrack.Bounds with { Width = saveX * (saveSession?.Progress ?? 0) };
        saveBar.Theme = theme with { Surface = saveError is not null ? new(210, 65, 65) : invalidDraft ? new(210, 155, 45) : theme.Accent };
        string? hint = null;
        if ((IsActive || !string.IsNullOrEmpty(smokeOutput)) && inspectorMouse.Y < y)
        {
            if (utilityDialog is not null) hint = utilityDialog.HoveredToolHint;
            else if (restoreDialog is not null) hint = restoreDialog.HoveredToolHint;
            else if (layoutDialog is not null) hint = layoutDialog.HoveredToolHint;
            else if (editingPage && inspectorMouse.Y < ApplicationBarHeight) hint = applicationBar.HoveredToolHint;
            else if (editingPage && livePreview is not null && inspectorMouse.X >= previewWindow.X && inspectorMouse.X < previewWindow.X + previewWindow.Width
                && inspectorMouse.Y >= previewWindow.Y && inspectorMouse.Y < previewWindow.Y + previewWindow.Height)
                hint = "配置プレビュー：セルをクリックして編集対象を選びます。プレビュー領域をビューポートとして px と rate を計算します。";
            else hint = ui.HoveredToolHint ?? (editingPage ? sidebar?.HoveredToolHint : null);
        }
        toolHint.Label = hint is null ? "ツールヒント：コントロールにマウスを合わせると説明を表示します。" : "ツールヒント：" + hint;
        inspector.Draw();
    }
}
