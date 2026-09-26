using StationeryUI.Canvas;
using StationeryUI.Controls;
using StationeryUI.Input;
using StationeryUI.Text;
using StationeryUI.Theming;

var tests = new (string Name, Action Run)[]
{
    ("editor launch modes distinguish read, edit and legacy file arguments", EditorLaunchTests.Run),
    ("designer opens, previews and edits referenced viewport layouts", EditorReferenceTests.Run),
    ("dock layout preserves edge priority, repeats, center, clipping and grid composition", DockLayoutTests.Run),
    ("tree scrollbars handle both axes, resize, zoom and tiny viewports", TreeScrollLayoutTests.Run),
    ("inspector operation logs record clicks, state transitions and inactive input", DeveloperOperationLogTests.Run),
    ("style autosave debounce, exact savepoints, restore, retention and conflicts", StyleSaveSessionTests.Run),
    ("read-only style refresh detects content changes and retains the last valid design", ReadStyleSnapshotTests.Run),
    ("read-only JSON tree maps model properties and preserves selection on reload", ReadJsonTreeTests.Run),
    ("style blueprint exports new validated designs without overwriting files", StyleBlueprintTests.Run),
    ("page layouts reserve inspector, preserve identities and switch on reload", PageLayoutTests.Run),
    ("developer style external overrides and embedded distribution defaults", DeveloperStyleTests.Run),
    ("developer inspection preserves selection, expansion and live details", DeveloperInspectionTests.Run),
    ("split panes arrange, clamp, preserve state and validate style bindings", SplitPaneTests.Run),
    ("tree expansion, selection, keyboard navigation and scoped IDs", TreeViewTests.Run),
    ("grid layout rates, pixels, scoped bindings and reusable definitions", GridLayoutTests.Run),
    ("nested layouts, spans, paths, validation and editor roundtrip", NestedLayoutTests.Run),
    ("model hierarchy, layout reference and semantic reload validation", ModelLayoutTests.Run),
    ("stationery IDs resolve by ancestry and keep duplicate local IDs isolated", StationeryNodeTests.Run),
    ("style parsing, padding and reload preserve the last good snapshot", StationeryStyleTests.Run),
    ("ring layout stays square, separated and inside the viewport", RingMenuLayoutTests.Run),
    ("grapheme movement and deletion", () => {
        var e = new UnderlineTextEditor("A😀e\u0301B"); e.MoveTo(e.Text.Length); e.Move(-1, false);
        Equal(5, e.Caret); e.Delete(true); Equal("A😀B", e.Text); e.Delete(true); Equal("AB", e.Text);
    }),
    ("selection replacement undo redo", () => {
        var e = new UnderlineTextEditor("abc"); e.Insert("日本語"); Equal("日本語", e.Text);
        e.Undo(); Equal("abc", e.Text); e.Redo(); Equal("日本語", e.Text);
    }),
    ("undo backspace restores caret before deletion", () => {
        var e=new UnderlineTextEditor("ab"); e.MoveTo(2); e.Delete(true); e.Undo();
        Equal("ab",e.Text); Equal(2,e.Caret); Equal(0,e.SelectionLength);
    }),
    ("length limit preserves surrogate pairs", () => {
        var e = new UnderlineTextEditor("", 1); e.Insert("😀"); Equal("", e.Text);
        e.Insert("AB"); Equal("A", e.Text);
    }),
    ("move to middle snaps to boundary", () => {
        var e = new UnderlineTextEditor("😀e\u0301"); e.MoveTo(1); Equal(0,e.Caret); e.MoveTo(3); Equal(2,e.Caret);
    }),
    ("single line filters line breaks", () => {
        var e = new UnderlineTextEditor(""); e.Insert("a\r\nb\tc"); Equal("abc", e.Text);
    }),
    ("composition commit is one undo operation", () => {
        var input = new FakeInput(); var e = new UnderlineTextEditor("before"); using var s = new TextInputSession(input,e);
        s.Focus(); input.Updates.Add(new("にほん",true,3)); s.Update(false,false); Equal("before",e.Text);
        input.Updates.Add(new("日",false)); input.Updates.Add(new("本",false)); s.Update(true,false);
        Equal("日本",e.Text); Equal(true,s.SuppressConfirmation); e.Undo(); Equal("before",e.Text);
        s.Update(true,false); Equal(true,s.SuppressConfirmation); s.Update(false,false); Equal(false,s.SuppressConfirmation);
    }),
    ("blur cancels preedit and pending input", () => {
        var input = new FakeInput(); using var s = new TextInputSession(input,new("a")); s.Focus();
        input.Updates.Add(new("未確定",true)); s.Update(false,false); input.Updates.Add(new("late",false));
        s.Blur(); Equal("",s.Composition.Text); Equal("a",s.Editor.Text); Equal(0,input.Updates.Count);
    }),
    ("focus wraps and skips disabled controls", () => {
        var f=new FocusManager(); f.Register("a"); f.Register("b",enabled:false); f.Register("c");
        f.Move(); Equal("a",f.FocusedId); f.Move(); Equal("c",f.FocusedId); f.Move(); Equal("a",f.FocusedId);
        f.Move(true); Equal("c",f.FocusedId);
        f.CapturePointer("c", 1); f.SetVisible("c", false);
        Equal("a",f.FocusedId); Equal<string?>(null,f.CapturedId);
        Equal(false,f.Focus("c"));
        f.SetVisible("b", false); f.SetVisible("b", true);
        Equal(false,f.Focus("b")); // Layout visibility must preserve explicit disabling.
        f.SetVisible("c", true); Equal(true,f.Focus("c"));
    }),
    ("modal excludes background and restores focus", () => {
        var f=new FocusManager(); f.Register("background"); f.Register("ok","dialog"); f.Focus("background");
        f.PushModal("dialog"); Equal("ok",f.FocusedId); Equal(false,f.Focus("background"));
        f.PopModal(); Equal("background",f.FocusedId);
    }),
    ("capture release on deactivate", () => {
        var f=new FocusManager(); f.Register("drag"); Equal(true,f.CapturePointer("drag",42));
        Equal(false,f.CapturePointer("drag",43)); f.Deactivate(); Equal(null,f.CapturedId);
    }),
    ("button releases outside without click", () => {
        var b=new IconButtonModel(new(0,0,40,40),"Button"); Equal(true,b.Press(new(10,10)));
        Equal(false,b.Release(new(80,80))); Equal(false,b.IsPressed);
    }),
    ("button theme overrides app theme", () => {
        var b=new IconButtonModel(new(0,0,40,40),"Button") { Theme=StationeryTheme.Light };
        var fills=new List<ButtonColor>(); StationeryButtonRenderer.Draw(b,(_,c)=>fills.Add(c),(_,_,_)=>{},(_,_)=>{},StationeryTheme.Dark);
        Equal(StationeryTheme.Light.Surface,fills[1]);
    }),
    ("logical transform roundtrip at 150 percent", () => {
        var v=new UiViewport { Scale=1.5, Offset=new(12,30) }; var r=v.ToWindow(new(20,40,200,50));
        Equal(new ScreenPoint(20,40),v.ToLogical(new(r.X,r.Y))); Equal(300d,r.Width);
    }),
    ("zoom preserves grid anchor", () => {
        var v=new GridViewport(20); var point=new ScreenPoint(30,50); var cell=v.ScreenToCell(point);
        v.ZoomAt(point,2); Equal(cell,v.ScreenToCell(point));
    }),
    ("progress cancellation is idempotent", () => {
        var m=new ModalDialogModel(ModalDialogKind.Progress,"work",""); Equal(ModalDialogAction.Stop,m.Apply(ModalDialogAction.Cancel));
        Equal(ModalDialogAction.None,m.Apply(ModalDialogAction.Stop));
    })
};
var failures=0;
foreach(var (name,run) in tests)
{
    try { run(); Console.WriteLine($"PASS {name}"); }
    catch(Exception ex) { failures++; Console.Error.WriteLine($"FAIL {name}: {ex}"); }
}
Console.WriteLine($"{tests.Length-failures}/{tests.Length} passed");
return failures==0 ? 0 : 1;
static void Equal<T>(T expected,T actual) { if(!EqualityComparer<T>.Default.Equals(expected,actual)) throw new Exception($"Expected {expected}, got {actual}"); }
sealed class FakeInput : ITextInputService
{
    public List<TextInputUpdate> Updates { get; }=[];
    public void Start() { }
    public void Stop()=>Updates.Clear();
    public void SetInputArea(ScreenRectangle area) { }
    public IReadOnlyList<TextInputUpdate> DrainUpdates() { var copy=Updates.ToArray(); Updates.Clear(); return copy; }
    public string ReadClipboard()=>"";
    public void WriteClipboard(string text) { }
    public void Dispose()=>Stop();
}
