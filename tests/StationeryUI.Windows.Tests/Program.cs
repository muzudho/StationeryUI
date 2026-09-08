using System.Runtime.InteropServices;
using System.Text;
using StationeryUI.Windows;

internal static class Program
{
    [STAThread]
    private static int Main()
    {
        nint window=0;
        try
        {
            if(SDL_Init(0x20)!=0) throw new Exception("SDL video initialization failed");
            window=SDL_CreateWindow("StationeryUI native tests",0,0,100,100,8);
            using var input=new WindowsTextInputService(window);
            input.Start(); input.Start();
            var id=SDL_GetWindowID(window);
            Push(0x302,id,"にほん",2,1);
            var updates=input.DrainUpdates();
            Require(updates.Count==1 && updates[0].Text=="にほん" && updates[0].IsComposition && updates[0].CaretIndex==2 && updates[0].SelectionLength==1,"composition fields");
            Push(0x303,id,"日本");
            updates=input.DrainUpdates();
            Require(updates.Count==1 && updates[0].Text=="日本" && !updates[0].IsComposition,"committed text");
            Push(0x302,id,"😀a",1,1);
            updates=input.DrainUpdates(); Require(updates[0].CaretIndex==2 && updates[0].SelectionLength==1,"Unicode offsets");
            Push(0x303,id+100,"other window"); Require(input.DrainUpdates().Count==0,"window isolation");
            using(var other=new WindowsTextInputService(window))
            {
                var rejected=false; try { other.Start(); } catch(InvalidOperationException) { rejected=true; }
                Require(rejected,"single active owner");
            }
            input.SetInputArea(new(12,18,100,30));
            input.Stop(); Push(0x303,id,"late"); Require(input.DrainUpdates().Count==0,"stop discards input");
            input.Start(); Push(0x303,id,"再開"); Require(input.DrainUpdates().Count==1,"restart has one watch");
            input.Dispose(); var disposed=false; try { input.Start(); } catch(ObjectDisposedException) { disposed=true; }
            Require(disposed,"disposed service rejects start");
            var rasterizer=new WindowsTextRasterizer();
            var png=rasterizer.RasterizePng("日本語 & e\u0301",22,false);
            Require(png.Length>100 && png[0]==137 && png[1]==80,"PNG rasterization");
            using var image=System.Drawing.Image.FromStream(new MemoryStream(png));
            Require(Math.Abs(image.Width-rasterizer.MeasureTextWidth("日本語 & e\u0301",22,false))<=1,"measurement matches raster width");
            Console.WriteLine("PASS: 10 native SDL lifecycle/event and Windows text rasterization checks.");
            return 0;
        }
        catch(Exception ex) { Console.Error.WriteLine(ex); return 1; }
        finally { if(window!=0) SDL_DestroyWindow(window); SDL_Quit(); }
    }
    private static void Require(bool ok,string name) { if(!ok) throw new Exception(name); }
    private static void Push(int type,uint id,string text,int caret=0,int length=0)
    {
        var buffer=Marshal.AllocHGlobal(56);
        try
        {
            Marshal.Copy(new byte[56],0,buffer,56);
            Marshal.WriteInt32(buffer,type); Marshal.WriteInt32(buffer,8,unchecked((int)id));
            var bytes=Encoding.UTF8.GetBytes(text); if(bytes.Length>=32) throw new Exception("fixture too long");
            Marshal.Copy(bytes,0,buffer+12,bytes.Length);
            if(type==0x302) { Marshal.WriteInt32(buffer,44,caret); Marshal.WriteInt32(buffer,48,length); }
            if(SDL_PushEvent(buffer)<0) throw new Exception("SDL_PushEvent failed");
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }
    [DllImport("SDL2.dll",CallingConvention=CallingConvention.Cdecl)] private static extern int SDL_Init(uint flags);
    [DllImport("SDL2.dll",CallingConvention=CallingConvention.Cdecl)] private static extern nint SDL_CreateWindow([MarshalAs(UnmanagedType.LPUTF8Str)]string title,int x,int y,int w,int h,uint flags);
    [DllImport("SDL2.dll",CallingConvention=CallingConvention.Cdecl)] private static extern uint SDL_GetWindowID(nint window);
    [DllImport("SDL2.dll",CallingConvention=CallingConvention.Cdecl)] private static extern int SDL_PushEvent(nint data);
    [DllImport("SDL2.dll",CallingConvention=CallingConvention.Cdecl)] private static extern void SDL_DestroyWindow(nint window);
    [DllImport("SDL2.dll",CallingConvention=CallingConvention.Cdecl)] private static extern void SDL_Quit();
}
