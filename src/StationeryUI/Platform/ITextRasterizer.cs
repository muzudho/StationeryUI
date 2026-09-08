namespace StationeryUI.Platform;

/// <summary>
/// OS のフォント描画機能を使い、文字列を透明背景の PNG へ変換します。
/// </summary>
public interface ITextRasterizer
{
    byte[] RasterizePng(string text, int pixelHeight, bool bold);

    /// <summary>ラスタライズ本文と同じフォント設定で、1 行テキストの幅を測ります。</summary>
    float MeasureTextWidth(string text, int pixelHeight, bool bold);

    /// <summary>折返し本文で使用する、追加行間を含む実際の1行の高さを返します。</summary>
    int MeasureLineHeight(int pixelHeight, int extraLineSpacing);

    /// <summary>行の上端から文字ベースラインまでの距離を返します。</summary>
    int MeasureBaselineOffset(int pixelHeight);

    int GetWrappedPageCount(
        string text,
        int width,
        int height,
        int pixelHeight,
        int extraLineSpacing);

    byte[] RasterizeWrappedPagePng(
        string text,
        int width,
        int height,
        int pixelHeight,
        int extraLineSpacing,
        int requestedPage);
}
