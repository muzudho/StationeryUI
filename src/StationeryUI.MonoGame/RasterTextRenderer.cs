namespace StationeryUI.MonoGame;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StationeryUI.Platform;

/// <summary>Bounded text texture cache for a host-owned SpriteBatch using premultiplied alpha.</summary>
public class RasterTextRenderer(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, ITextRasterizer rasterizer) : IDisposable
{
    private readonly Dictionary<(string Text, int Height, bool Bold), Texture2D> textures = [];
    private readonly List<Texture2D> retired = [];
    private bool disposed;
    public void Draw(string text, Rectangle bounds, Color color, int pixelHeight = 18, bool bold = false)
    {
        if (string.IsNullOrEmpty(text) || bounds.Width <= 0 || bounds.Height <= 0) return;
        spriteBatch.Draw(GetTexture(text,pixelHeight,bold), GetDrawBounds(text,bounds,pixelHeight,bold), color);
    }
    public Point Measure(string text, int pixelHeight = 18, bool bold = false) =>
        string.IsNullOrEmpty(text) ? Point.Zero : GetTexture(text,pixelHeight,bold).Bounds.Size;
    public Rectangle GetDrawBounds(string text, Rectangle bounds, int pixelHeight = 18, bool bold = false)
    {
        var measured = Measure(text,pixelHeight,bold);
        var scale = MathF.Min(1f,MathF.Min(bounds.Width/(float)Math.Max(1,measured.X),bounds.Height/(float)Math.Max(1,measured.Y)));
        var width = Math.Max(1,(int)MathF.Round(measured.X*scale));
        var height = Math.Max(1,(int)MathF.Round(measured.Y*scale));
        return new(bounds.X,bounds.Y+(bounds.Height-height)/2,width,height);
    }
    private Texture2D GetTexture(string text,int height,bool bold)
    {
        ObjectDisposedException.ThrowIf(disposed,this);
        var key=(text,height,bold);
        if (textures.TryGetValue(key,out var texture)) return texture;
        if(textures.Count>=128)
        {
            var oldest=textures.First(); retired.Add(oldest.Value); textures.Remove(oldest.Key);
        }
        using var stream=new MemoryStream(rasterizer.RasterizePng(text,height,bold));
        texture=Texture2D.FromStream(graphicsDevice,stream,Premultiply);
        textures.Add(key,texture);
        return texture;
    }
    internal static void Premultiply(byte[] rgba)
    {
        for(var i=0;i<rgba.Length;i+=4)
        {
            var alpha=rgba[i+3];
            rgba[i]=(byte)(rgba[i]*alpha/255);
            rgba[i+1]=(byte)(rgba[i+1]*alpha/255);
            rgba[i+2]=(byte)(rgba[i+2]*alpha/255);
        }
    }
    /// <summary>Call after SpriteBatch.End, once all queued text draws have been submitted.</summary>
    public void EndFrame()
    {
        foreach(var texture in retired) texture.Dispose();
        retired.Clear();
    }
    public void Dispose()
    {
        if(disposed) return;
        foreach(var texture in textures.Values) texture.Dispose();
        EndFrame();
        textures.Clear(); disposed=true;
    }
}
