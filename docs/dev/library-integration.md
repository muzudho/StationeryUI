# 繝ｩ繧､繝悶Λ繝ｪ繝ｼ縺ｮ邨・∩霎ｼ縺ｿ

MonoGame 繧｢繝励Μ縺ｸ譁・袷蜈ｷ UI 繧呈磁邯壹☆繧矩幕逋ｺ閠・髄縺代・雉・侭縺ｧ縺吶・
## 繝代ャ繧ｱ繝ｼ繧ｸ

| 繝代ャ繧ｱ繝ｼ繧ｸ | 蟇ｾ雎｡ | 蜀・ｮｹ |
|---|---|---|
| `StationeryUI` | .NET 8莉･髯・| OS繝ｻMonoGame髱樔ｾ晏ｭ倥・邱ｨ髮・・驛ｨ蜩√・蜈･蜉帙・繝・・繝槭・蠎ｧ讓吶Δ繝・Ν |
| `StationeryUI.MonoGame` | .NET 8莉･髯・| DesktopGL 3.8.5.1縺ｸ縺ｮ謠冗判繝ｻ蜈･蜉帶磁邯・|
| `StationeryUI.Windows` | .NET 8 Windows莉･髯・| Windows譁・ｭ玲緒逕ｻ縲√け繝ｪ繝・・繝懊・繝峨ヾDL2蜈･蜉帙・蜷域・逶｣隕・|

v0.3.0 縺ｯ NuGet.org 縺九ｉ蜿門ｾ励〒縺阪∪縺呻ｼ喙StationeryUI](https://www.nuget.org/packages/StationeryUI/0.3.0)縲ーStationeryUI.MonoGame](https://www.nuget.org/packages/StationeryUI.MonoGame/0.3.0)縲ーStationeryUI.Windows](https://www.nuget.org/packages/StationeryUI.Windows/0.3.0)縲ょ・髢玖・・ [Muzudho](https://www.nuget.org/profiles/Muzudho) 縺ｧ縺吶・
NuGet.org 縺梧怏蜉ｹ縺ｪ繝励Ο繧ｸ繧ｧ繧ｯ繝医〒縺ｯ縲∽ｻ･荳九・蜿ら・繧定ｿｽ蜉縺励※ `dotnet restore` 繧貞ｮ溯｡後＠縺ｾ縺吶ＡStationeryUI` 縺ｯ萓晏ｭ倥ヱ繝・こ繝ｼ繧ｸ縺ｨ縺励※蠕ｩ蜈・＆繧後∪縺吶・uGet.org 縺ｮ繧ｽ繝ｼ繧ｹ URL 縺ｯ `https://api.nuget.org/v3/index.json` 縺ｧ縺吶ゅた繝ｼ繧ｹ繧貞宛髯舌☆繧・`NuGet.Config` 繧・Package Source Mapping 縺後≠繧句ｴ蜷医・縲√％縺ｮ・薙ヱ繝・こ繝ｼ繧ｸ繧・NuGet.org 縺九ｉ蜿門ｾ励〒縺阪ｋ險ｭ螳壹↓縺励※縺上□縺輔＞縲・
謇句虚驟榊ｸ・畑縺ｮ `.nupkg` 縺ｯ蠑輔″邯壹″ [GitHub Release v0.3.0](https://github.com/muzudho/StationeryUI/releases/tag/v0.3.0) 縺九ｉ蜈･謇九〒縺阪∪縺吶よ里蟄倥い繝励Μ縺ｮ `LocalPackages/StationeryUI` 繧剃ｽｿ縺・°逕ｨ繧らｶ咏ｶ壹〒縺阪∪縺吶・
```xml
<ItemGroup>
  <PackageReference Include="StationeryUI.MonoGame" Version="0.3.0" />
  <PackageReference Include="StationeryUI.Windows" Version="0.3.0" />
</ItemGroup>
```

Windows縺ｮ繧｢繝励Μ縺ｯ `net8.0-windows` 莉･髯阪ｒ蟇ｾ雎｡縺ｨ縺励∪縺吶ＡStationeryUI` 閾ｪ菴薙ｒ蜿ら・縺吶ｋ縺溘ａ縺ｫWindows縺ｯ蠢・ｦ√≠繧翫∪縺帙ｓ縲・
## 繝ｪ繝ｳ繧ｰ繝｡繝九Η繝ｼ縺ｮ驟咲ｽｮ・・.1.1・・
CircleSpaceCoordinator 縺九ｉ遘ｻ讀阪＠縺・`StationeryUI.Controls.RingMenuLayout` 縺ｯ縲∵緒逕ｻ繧ｨ繝ｳ繧ｸ繝ｳ縺ｫ萓晏ｭ倥○縺壹∵ｭ｣譁ｹ蠖｢縺ｮ繝懊ち繝ｳ繧貞・蜻ｨ荳翫↓遲蛾俣髫斐〒驟咲ｽｮ縺励∪縺吶ょ・鬆ｭ縺ｯ荳翫∽ｻ･髯阪・譎りｨ亥屓繧翫〒縺吶ら判髱｢遶ｯ縺ｧ縺ｯ荳ｭ蠢・ｒ陬懈ｭ｣縺励∝ｰ上＆縺・判髱｢縺ｧ縺ｯ蜈ｨ菴薙ｒ邵ｮ蟆上＠縺ｦ縲√・繧ｿ繝ｳ縺ｮ驥阪↑繧翫→縺ｯ縺ｿ蜃ｺ縺励ｒ髦ｲ縺弱∪縺吶・
```csharp
using StationeryUI.Canvas;
using StationeryUI.Controls;

var layout = RingMenuLayout.Create(
    new ScreenRectangle(100, 100, 44, 44), // 襍ｷ轤ｹ繝懊ち繝ｳ縺ｮ鬆伜沺
    1280, 720,                           // 菴ｿ逕ｨ蜿ｯ閭ｽ縺ｪ逕ｻ髱｢縺ｮ蟷・→鬮倥＆
    5);                                  // 繧ｭ繝｣繝ｳ繧ｻ繝ｫ繧貞性繧鬆・岼謨ｰ
var buttons = layout.Buttons.Select((bounds, index) =>
    new IconButtonModel(bounds, $"謫堺ｽ・{index + 1}")).ToArray();
// layout.Center 縺ｨ layout.Radius 縺ｯ繝ｪ繝ｳ繧ｰ縺ｮ蟶ｯ繧呈緒縺城圀縺ｫ繧ゆｽｿ逕ｨ縺ｧ縺阪∪縺吶・```

蟷・→鬮倥＆縺ｯ譛蛾剞縺ｮ豁｣謨ｰ縲・・岼謨ｰ縺ｯ1莉･荳翫ｒ謖・ｮ壹＠縺ｦ縺上□縺輔＞縲ゅΓ繝九Η繝ｼ縺ｮ髢矩哩縲∵桃菴懷ｮ溯｡後∝・蜉幃・譁ｭ縲∵緒逕ｻ縺ｯ繝帙せ繝亥・縺梧球蠖薙＠縺ｾ縺吶ょ推繝懊ち繝ｳ縺ｫ謫堺ｽ懷・螳ｹ繧堤､ｺ縺吶い繧ｯ繧ｻ繧ｷ繝悶Ν蜷阪ｒ莉倥￠縲ゝab繝ｻ遏｢蜊ｰ繧ｭ繝ｼ縺ｧ遘ｻ蜍輔・nter繝ｻSpace縺ｧ螳溯｡後・sc縺ｧ髢峨§繧区桃菴懊ｒ謗･邯壹＠縺ｦ縺上□縺輔＞縲り牡縺ｯ `StationeryButtonRenderer` 縺ｨ `StationeryTheme` 縺ｧ驕ｩ逕ｨ縺ｧ縺阪∪縺吶ょ峇遒√・莨壼ｴ縺ｪ縺ｩ縺ｮ繧｢繝励Μ蝗ｺ譛峨Δ繝・Ν繧・MonoGame繝ｻWindows 縺ｸ縺ｮ萓晏ｭ倥・縺ゅｊ縺ｾ縺帙ｓ縲・
譌｢蟄倥・ CircleSpaceCoordinator 縺九ｉ遘ｻ陦後☆繧句ｴ蜷医～CircleSpaceCoordinator.ReusableControls.RingMenuLayout` 繧・`StationeryUI.Controls.RingMenuLayout` 縺ｫ鄂ｮ縺肴鋤縺医∪縺吶ＡCreate` 縺ｮ蠑墓焚縺ｨ謌ｻ繧雁､縺ｮ讒矩縺ｯ蜷後§縺ｧ縺吶・
## 繧ｵ繝ｳ繝励Ν繧貞虚縺九☆

Windows荳翫〒.NET SDK 10繧剃ｽｿ逕ｨ縺吶ｋ謇矩・〒縺呻ｼ医Λ繧､繝悶Λ繝ｪ繝ｼ縺ｮ蟇ｾ雎｡縺ｯ.NET 8・峨４DL2縺ｪ縺ｩ縺ｮ繝阪う繝・ぅ繝紋ｾ晏ｭ倥・MonoGame縺ｮNuGet繝代ャ繧ｱ繝ｼ繧ｸ縺九ｉ蠕ｩ蜈・＠縺ｾ縺吶・
```powershell
dotnet build StationeryUI.slnx -c Release
dotnet run --project samples/StationeryUI.Demo -c Release --no-build
```

繧ｵ繝ｳ繝励Ν縺ｮ謫堺ｽ懊・ [蛻ｩ逕ｨ閠・髄縺代・譯亥・](../user/demo.md)繧貞盾辣ｧ縺励※縺上□縺輔＞縲・
## MonoGame縺ｸ縺ｮ邨・∩霎ｼ縺ｿ

谺｡縺ｯ `Game` 豢ｾ逕溘け繝ｩ繧ｹ蜀・〒縺ｮ菴ｿ逕ｨ萓九〒縺吶・
```csharp
using StationeryUI.MonoGame;
using StationeryUI.Windows;

private WindowsTextInputService input = null!;
private StationeryUiHost ui = null!;

protected override void LoadContent()
{
    input = new WindowsTextInputService(Window.Handle);
    ui = new StationeryUiHost(GraphicsDevice, input,
        family => new WindowsTextRasterizer(family));
    var name = ui.AddTextBox("name", new(24, 24, 600, 64), "蜷榊燕", "縺薙ｓ縺ｫ縺｡縺ｯ");
    ui.AddButton("apply", new(24, 120, 320, 64), "蜷榊燕繧貞渚譏",
        () => Window.Title = name.Editor!.Text);
    ui.Focus.Focus(name.Path);
}

protected override void Update(GameTime gameTime)
{
    ui.Update(gameTime, IsActive, Keyboard.GetState(), Mouse.GetState());
    // 繧ｲ繝ｼ繝蛛ｴ縺ｮ蜈･蜉帛・逅・〒縺ｯ ui.KeyboardConsumed / PointerConsumed 繧堤｢ｺ隱阪☆繧九・    base.Update(gameTime);
}

protected override void Draw(GameTime gameTime)
{
    GraphicsDevice.Clear(StationeryUiHost.Convert(ui.Theme.Background));
    ui.Draw();
    base.Draw(gameTime);
}

protected override void UnloadContent()
{
    ui.Dispose(); // UI縺ｯ蛟溘ｊ縺溷・蜉帙し繝ｼ繝薙せ繧堤ｴ譽・＠縺ｪ縺・・    input.Dispose();
    base.UnloadContent();
}
```

荳願ｨ倥〒縺ｯ騾壼ｸｸ縺ｮMonoGame蜷榊燕遨ｺ髢・`Microsoft.Xna.Framework`縲～Microsoft.Xna.Framework.Input` 繧ょｿ・ｦ√〒縺吶ゅお繝ｳ繝医Μ繝ｼ繝昴う繝ｳ繝医↓ `[STAThread]` 繧剃ｻ倥￠縺ｦ縺上□縺輔＞縲・
## 髱槭い繧ｯ繝・ぅ繝悶↑繧ｦ繧｣繝ｳ繝峨え縺ｮ繝槭え繧ｹ蜈･蜉・
蛻･繧ｦ繧｣繝ｳ繝峨え繧・F12 髢狗匱閠・え繧｣繝ｳ繝峨え縺後い繧ｯ繝・ぅ繝悶↑髢薙・縲∬レ蠕後・繧ｲ繝ｼ繝逕ｻ髱｢縺ｸ縺ｮ繝槭え繧ｹ蜈･蜉帙ｒ辟｡隕悶＠縺ｦ縺上□縺輔＞縲ＡStationeryUiHost.Update` 縺ｮ `active` 縺ｫ縺ｯ謇譛峨☆繧・`Game.IsActive` 繧呈ｸ｡縺励∪縺吶る撼繧｢繧ｯ繝・ぅ繝紋ｸｭ繧・`Update(..., false, ...)` 繧貞他縺ｶ縺薙→縺ｧ縲√ラ繝ｩ繝・げ繝ｻ繝昴う繝ｳ繧ｿ繝ｼ繧ｭ繝｣繝励メ繝｣繝ｼ縺ｮ隗｣髯､縺ｨ蜈･蜉帛ｱ･豁ｴ縺ｮ蜷梧悄縺瑚｡後ｏ繧後∪縺吶ＡStationeryUiHost` 縺ｯ繧ｦ繧｣繝ｳ繝峨え縺ｮ繧｢繧ｯ繝・ぅ繝也憾諷九ｒ閾ｪ蜍募叙蠕励＠縺ｪ縺・◆繧√～true` 蝗ｺ螳壹〒縺ｯ縺薙・謚第ｭ｢縺悟ロ縺阪∪縺帙ｓ縲・
**繧ｲ繝ｼ繝蛛ｴ縺ｮ迢ｬ閾ｪ蜈･蜉帙ｂ蛻･騾疲椛豁｢縺悟ｿ・ｦ√〒縺吶・* `PointerConsumed` 縺ｯ髱槭い繧ｯ繝・ぅ繝匁凾縺ｫ `false` 縺ｸ謌ｻ繧九◆繧√√◎繧後□縺代ｒ隕九※繧ｲ繝ｼ繝縺ｮ繧ｯ繝ｪ繝・け蜃ｦ逅・∈騾ｲ縺ｾ縺ｪ縺・〒縺上□縺輔＞縲ら峡閾ｪ縺ｮ繝励Ξ繝薙Η繝ｼ驕ｸ謚槭√・繧､繝ｼ繝ｫ蜃ｦ逅・∝承繧ｯ繝ｪ繝・け縲√ラ繝ｩ繝・げ縲～GameComponent` 繧・う繝吶Φ繝育ｵ檎罰縺ｮ蜈･蜉帙↓繧ょ酔縺俶擅莉ｶ繧帝←逕ｨ縺励∪縺吶・
莉･荳九・迢ｬ閾ｪ蜈･蜉帙∈騾ｲ繧譚｡莉ｶ縺ｨ螻･豁ｴ蜷梧悄縺ｮ萓九〒縺吶ょ・縺ｮ蝓ｺ譛ｬ萓九・ `Update` 縺ｫ邨・∩霎ｼ縺ｿ縺ｾ縺吶ＡgameDragging` 縺ｯ蛻ｩ逕ｨ繧｢繝励Μ縺梧戟縺､繝峨Λ繝・げ迥ｶ諷九・萓九〒縺吶・
```csharp
private MouseState previousGameMouse;
private bool gameWasActive;
private bool gameDragging;

protected override void Update(GameTime gameTime)
{
    var keyboard = Keyboard.GetState();
    var mouse = Mouse.GetState();
    var active = IsActive;
    ui.Update(gameTime, active, keyboard, mouse);

    if (!active || !gameWasActive)
    {
        gameDragging = false;
        previousGameMouse = mouse;
    }
    else if (!ui.PointerConsumed)
    {
        var clicked = mouse.LeftButton == ButtonState.Pressed
            && previousGameMouse.LeftButton == ButtonState.Released;
        var wheelDelta = mouse.ScrollWheelValue - previousGameMouse.ScrollWheelValue;
        // 縺薙％縺ｧ繧ｲ繝ｼ繝蛛ｴ縺ｮ繧ｯ繝ｪ繝・け繝ｻ繝帙う繝ｼ繝ｫ縺ｪ縺ｩ繧貞・逅・☆繧九・    }

    previousGameMouse = mouse;
    gameWasActive = active;
    // GameComponent 蛛ｴ縺ｫ繧・IsActive 縺ｮ遒ｺ隱阪ｒ蜈･繧後ｋ縲・    base.Update(gameTime);
}
```

髱槭い繧ｯ繝・ぅ繝紋ｸｭ縺ｮ螻･豁ｴ繧呈峩譁ｰ縺励∝ｾｩ蟶ｰ縺励◆譛蛻昴・繝輔Ξ繝ｼ繝繧ょｷｮ蛻・ｒ蝓ｺ貅門喧縺吶ｋ縺薙→縺ｧ縲∝挨繧ｦ繧｣繝ｳ繝峨え縺ｧ縺ｮ繧ｯ繝ｪ繝・け繧・・繧､繝ｼ繝ｫ遘ｻ蜍輔ｒ蠕ｩ蟶ｰ蠕後・謫堺ｽ懊→縺励※謇ｱ繧上↑縺・ｈ縺・↓縺励∪縺吶ゅラ繝ｩ繝・げ縺ｮ髢句ｧ九↓縺ｯ謚ｼ荳九・迸ｬ髢薙ｒ菴ｿ縺・∵款縺輔ｌ縺ｦ縺・ｋ縺縺代〒蜀埼幕縺励↑縺・〒縺上□縺輔＞縲・12 縺ｮ繧ｭ繝｣繝励メ繝｣繝ｼ荳ｭ繧・Δ繝ｼ繝繝ｫ陦ｨ遉ｺ荳ｭ繧ゅ√ご繝ｼ繝蛛ｴ縺ｮ蜈･蜉帙∈騾ｲ縺ｾ縺ｪ縺・ｈ縺・擅莉ｶ繧堤ｵ・∩蜷医ｏ縺帙∪縺吶ょ・蜉帑ｻ･螟悶・譖ｴ譁ｰ繝ｻ謠冗判繧呈ｭ｢繧√ｋ蠢・ｦ√・縺ゅｊ縺ｾ縺帙ｓ縲・
蟆主・蠕後・蛻･繧ｦ繧｣繝ｳ繝峨え繧偵ご繝ｼ繝逕ｻ髱｢縺ｫ驥阪・縲√け繝ｪ繝・け繝ｻ繝帙う繝ｼ繝ｫ繝ｻ繝峨Λ繝・げ縺ｧ閭悟ｾ後・ UI 縺悟､牙喧縺励↑縺・％縺ｨ縲√ラ繝ｩ繝・げ騾比ｸｭ縺ｮ蛻・ｊ譖ｿ縺医〒謫堺ｽ懊′繧ｭ繝｣繝ｳ繧ｻ繝ｫ縺輔ｌ繧九％縺ｨ縲∝ｾｩ蟶ｰ逶ｴ蠕後↓隱､謫堺ｽ懊○縺壽ｬ｡縺ｮ繧ｯ繝ｪ繝・け縺九ｉ騾壼ｸｸ謫堺ｽ懊〒縺阪ｋ縺薙→繧堤｢ｺ隱阪＠縺ｦ縺上□縺輔＞縲・
## F12 髢狗匱閠・え繧｣繝ｳ繝峨え縺ｨ・ｻ謖・〒縺､縺ｾ繧・ｽ讖溯・縺ｮ邨・∩霎ｼ縺ｿ

NuGet 繝代ャ繧ｱ繝ｼ繧ｸ繧貞ｰ主・縺吶ｋ髫帙・縲：12 髢狗匱閠・え繧｣繝ｳ繝峨え縺ｮ・ｻ謖・〒縺､縺ｾ繧・ｽ・医く繝｣繝励メ繝｣繝ｼ・画ｩ溯・繧ょ茜逕ｨ繧｢繝励Μ縺ｸ謗･邯壹＠縺ｦ縺上□縺輔＞縲・*髢狗匱閠・え繧｣繝ｳ繝峨え繧定｡ｨ遉ｺ縺吶ｋ縺縺代〒縺ｯ縲∝・縺ｮ逕ｻ髱｢縺ｮ繧ｯ繝ｪ繝・け蛻､螳壹→譯・牡縺ｮ譫縺ｮ謠冗判縺ｯ蜍輔″縺ｾ縺帙ｓ縲・* 繧｢繧､繧ｳ繝ｳ縺ｯ繧ｭ繝｣繝励メ繝｣繝ｼ繝｢繝ｼ繝峨ｒ蛻・ｊ譖ｿ縺医∝茜逕ｨ繧｢繝励Μ縺悟ｯｾ雎｡繧貞愛螳壹＠縺ｦ驕ｸ謚槭ｒ騾夂衍縺励∵棧繧呈緒逕ｻ縺励∪縺吶・
縺ｾ縺・[髢狗匱閠・え繧｣繝ｳ繝峨え縺ｮ繝帙せ繝育ｵ・∩霎ｼ縺ｿ](developer-window.md#繝帙せ繝医・邨・∩霎ｼ縺ｿ)縺ｫ蠕薙＞縲√お繝ｳ繝医Μ繝ｼ繝昴う繝ｳ繝医・ `--stationery-inspector <繝代う繝怜錐>` 蛻・ｲ舌→ `InspectorGame` 逶ｸ蠖薙・繝帙せ繝医ｒ逕ｨ諢上＠縺ｾ縺吶・uGet 縺ｮ蜿ら・縺縺代〒縺ｯ縲√％縺ｮ繧｢繝励Μ蛛ｴ縺ｮ襍ｷ蜍募・逅・・霑ｽ蜉縺輔ｌ縺ｾ縺帙ｓ縲ゅ☆縺ｧ縺ｫ F12 縺ｧ髢狗匱閠・え繧｣繝ｳ繝峨え縺碁幕縺上い繝励Μ縺ｧ縺ｯ縲∵ｬ｡縺ｮ蜈･蜉帙・謠冗判縺ｮ謗･邯壹ｒ遒ｺ隱阪＠縺ｦ縺上□縺輔＞縲・
莉･荳九・縲∽ｸ翫・萓九・ `ui` 荳縺､繧呈､懈渊縺吶ｋ蝣ｴ蜷医〒縺吶Ａusing StationeryUI.Inspection;` 縺ｨ `using System.Linq;` 繧定ｿｽ蜉縺励√ヵ繧｣繝ｼ繝ｫ繝峨→ `Update` / `Draw` 繧呈ｬ｡縺ｮ繧医≧縺ｫ邨・∩霎ｼ縺ｿ縺ｾ縺吶ゅ☆縺ｧ縺ｫ `StationeryDeveloperWindow` 繧呈戟縺､蝣ｴ蜷医・縲√◎縺ｮ繧､繝ｳ繧ｹ繧ｿ繝ｳ繧ｹ繧剃ｽｿ縺・∪縺吶・
```csharp
private readonly StationeryDeveloperWindow developerWindow = new();
private bool previousDeveloperKey;
private bool captureMouseDown;
private double inspectionElapsed;

protected override void Update(GameTime gameTime)
{
    var keyboard = Keyboard.GetState();
    var mouse = Mouse.GetState();
    var developerKey = keyboard.IsKeyDown(Keys.F12);
    if (IsActive && developerKey && !previousDeveloperKey)
        developerWindow.Show(ui.Inspect());
    previousDeveloperKey = developerKey;

    inspectionElapsed += gameTime.ElapsedGameTime.TotalSeconds;
    if (developerWindow.IsOpen && inspectionElapsed >= .25)
    {
        developerWindow.Update(ui.Inspect());
        inspectionElapsed = 0;
    }

    var captureDown = mouse.LeftButton == ButtonState.Pressed;
    if (developerWindow.CaptureEnabled && IsActive)
    {
        if (captureDown && !captureMouseDown)
        {
            var hit = DeveloperCapture.HitTest(ui.Inspect(), mouse.X, mouse.Y);
            if (hit is not null) developerWindow.SelectCaptured(hit.Path);
        }
        captureMouseDown = captureDown;
        // 繧ｭ繝｣繝励メ繝｣繝ｼ荳ｭ縺ｯ騾壼ｸｸ縺ｮ UI 謫堺ｽ懊∈蜈･蜉帙ｒ貂｡縺輔↑縺・・        ui.Update(gameTime, false, keyboard, mouse);
        base.Update(gameTime);
        return;
    }
    captureMouseDown = captureDown;

    ui.Update(gameTime, IsActive, keyboard, mouse);
    // 繧ｲ繝ｼ繝蛛ｴ縺ｮ騾壼ｸｸ蜈･蜉帛・逅・ｂ繧ｭ繝｣繝励メ繝｣繝ｼ蛻・ｲ舌ｈ繧雁ｾ後↓鄂ｮ縺上・    base.Update(gameTime);
}

protected override void Draw(GameTime gameTime)
{
    GraphicsDevice.Clear(StationeryUiHost.Convert(ui.Theme.Background));
    ui.Draw();
    base.Draw(gameTime);

    // 逕ｻ髱｢縺ｮ謠冗判繧堤ｵゅ∴縺溷ｾ後・∈謚樔ｸｭ縺ｮ驛ｨ蜩√↓譯・牡縺ｮ譫繧帝㍾縺ｭ繧九・    if (developerWindow.IsOpen && developerWindow.SelectedPath is { } selected)
    {
        var entry = ui.Inspect().FirstOrDefault(e => e.Path == selected && e.Visible);
        if (entry?.WindowBounds is { } bounds)
            ui.DrawInspectionOutline(bounds);
    }
}

protected override void Dispose(bool disposing)
{
    if (disposing) developerWindow.Dispose();
    base.Dispose(disposing);
}
```

`LoadContent` / `UnloadContent` 縺ｯ荳翫・萓九→蜈ｱ騾壹〒縺吶ＡInspect()` 縺ｯ繧ｲ繝ｼ繝繧ｹ繝ｬ繝・ラ縺ｧ蜻ｼ縺ｳ縺ｾ縺吶ＡDrawInspectionOutline` 縺ｯ蜀・Κ縺ｧ `SpriteBatch.Begin` / `End` 繧貞他縺ｶ縺溘ａ縲√い繝励Μ蛛ｴ縺ｮ `SpriteBatch.End` 繧呈ｸ医∪縺帙※縺九ｉ蜻ｼ繧薙〒縺上□縺輔＞縲Ａbase.Update` 縺九ｉ蜈･蜉帙ｒ蜃ｦ逅・☆繧・`GameComponent` 縺後≠繧九い繝励Μ縺ｧ縺ｯ縲√◎縺ｮ蜈･蜉帛・逅・ｂ繧ｭ繝｣繝励メ繝｣繝ｼ荳ｭ縺ｯ謚第ｭ｢縺励∪縺吶・
隍・焚縺ｮ `StationeryUiHost` 繧・Δ繝ｼ繝繝ｫ逕ｻ髱｢繧呈戟縺､蝣ｴ蜷医・縲∬｡ｨ遉ｺ繝ｻ繧ｯ繝ｪ繝・け蛻､螳壹・譫縺ｮ謠冗判縺ｫ蜷後§讀懈渊繝・Μ繝ｼ繧剃ｽｿ縺・∪縺吶・繝・Δ縺ｮ螳溯｣・(../../samples/StationeryUI.Demo/Program.cs)縺ｮ `InspectStationery()` 縺梧磁邯壻ｾ九〒縺吶ゅΔ繝ｼ繝繝ｫ陦ｨ遉ｺ荳ｭ縺ｯ `DeveloperCapture.HitTest(entries, mouse.X, mouse.Y, dialog.Path)` 縺ｮ繧医≧縺ｫ `scope` 縺ｸ繝繧､繧｢繝ｭ繧ｰ縺ｮ螳悟・繝代せ繧呈ｸ｡縺励∬レ蠕後・驛ｨ蜩√ｒ驕ｸ縺ｰ縺ｪ縺・ｈ縺・↓縺励∪縺吶・
### 譯・牡縺ｮ譫縺悟・縺ｪ縺・ｴ蜷・
- ・ｻ謖・〒縺､縺ｾ繧・ｽ繧呈怏蜉ｹ縺ｫ縺励∝・縺ｮ繧｢繝励Μ逕ｻ髱｢繧偵い繧ｯ繝・ぅ繝悶↓縺励※蟇ｾ雎｡繧偵け繝ｪ繝・け縺励∪縺吶よ棧縺ｯ驕ｸ謚槭＠縺滄Κ蜩√・遽・峇縺ｫ陦ｨ遉ｺ縺輔ｌ縺ｾ縺吶ゅ・繧､繝ｳ繧ｿ繝ｼ縺ｫ霑ｽ蠕薙☆繧九き繝ｼ繧ｽ繝ｫ縺ｧ縺ｯ縺ゅｊ縺ｾ縺帙ｓ縲・- `CaptureEnabled` 繧定ｪｭ縺ｿ縲√け繝ｪ繝・け譎ゅ↓ `DeveloperCapture.HitTest` 竊・`SelectCaptured` 繧貞他繧薙〒縺・ｋ縺狗｢ｺ隱阪＠縺ｾ縺吶・- 蟇ｾ雎｡縺梧､懈渊繧ｹ繝翫ャ繝励す繝ｧ繝・ヨ縺ｫ縺ゅｊ縲～Visible` 縺・`true`縲～WindowBounds` 縺梧ｭ｣縺ｮ蟷・・鬮倥＆繧呈戟縺､縺狗｢ｺ隱阪＠縺ｾ縺吶り・菴懈緒逕ｻ縺ｮ驛ｨ蜩√・ [繝｢繝・Ν縺ｨ讀懈渊諠・ｱ縺ｮ謗･邯咯(model-inspection.md)縺ｫ蠕薙▲縺ｦ逋ｻ骭ｲ縺励∪縺吶・- 繝槭え繧ｹ蠎ｧ讓吶→ `WindowBounds` 縺ｯ蜷後§繧ｦ繧｣繝ｳ繝峨え蜀・・繝斐け繧ｻ繝ｫ蠎ｧ讓吶〒辣ｧ蜷医＠縺ｾ縺吶６I 縺ｮ隲也炊蠎ｧ讓吶∈螟画鋤縺励※貂｡縺励◆繧翫∵棧縺ｸ `Viewport.Scale` 繧剃ｺ碁㍾縺ｫ驕ｩ逕ｨ縺励◆繧翫＠縺ｪ縺・〒縺上□縺輔＞縲・- `SelectedPath` 縺ｫ蟇ｾ蠢懊☆繧矩Κ蜩√∈ `DrawInspectionOutline` 繧貞他縺ｳ縲√◎縺ｮ蠕後・逕ｻ髱｢繧ｯ繝ｪ繧｢繧・緒逕ｻ縺ｧ譫繧定ｦ・▲縺ｦ縺・↑縺・°遒ｺ隱阪＠縺ｾ縺吶・
蟆主・蠕後・縲熊12 竊・・ｻ謖・〒縺､縺ｾ繧・ｽ 竊・蜈・・逕ｻ髱｢縺ｮ驛ｨ蜩√ｒ繧ｯ繝ｪ繝・け縲阪〒縲∵｡・牡縺ｮ譫縺ｨ繝・Μ繝ｼ縺ｮ驕ｸ謚槭′荳閾ｴ縺励√◎縺ｮ繧ｯ繝ｪ繝・け縺ｧ騾壼ｸｸ縺ｮ繝懊ち繝ｳ謫堺ｽ懊′螳溯｡後＆繧後↑縺・％縺ｨ繧堤｢ｺ隱阪＠縺ｾ縺吶ゅい繧､繧ｳ繝ｳ繧偵ｂ縺・ｸ蠎ｦ謚ｼ縺吶→騾壼ｸｸ謫堺ｽ懊↓謌ｻ繧翫∪縺吶・
## 繝峨ャ繧ｯ驟咲ｽｮ

`dock-layout` 縺ｯ `top` / `right` / `bottom` / `left` 繧帝・蛻鈴・↓遒ｺ菫昴＠縲～center` 縺梧ｮ九ｊ繧剃ｽｿ縺・・鄂ｮ縺ｧ縺吶よ婿蜷代・驥崎､・→莉ｻ諢丞区焚縺ｮ隕∫ｴ繧呈桶縺医∪縺吶ょ推隕∫ｴ縺ｮ `size` 繧呈欠螳壹＠縲∬｡後・蛻怜ｮ夂ｾｩ縺ｯ菴ｿ縺・∪縺帙ｓ縲・險ｭ螳壻ｾ九→繧ｨ繝ｩ繝ｼ陦ｨ遉ｺ縺ｮ謗･邯咯(dock-layout.md)繧貞盾辣ｧ縺励※縺上□縺輔＞縲・
## 螟冶ｦｳ縺ｮ螟画峩

```csharp
ui.Theme = StationeryUI.Theming.StationeryTheme.Light with
{
    Accent = new(30, 110, 190),
    FontFamily = "Meiryo",
    FontSize = 24,
    Padding = 14
};
ui.Viewport.Scale = 1.5;
```

蛟句挨縺ｮ `Element.Theme`縲～IconButtonModel.Theme`縲｀onoGame縺ｮ `Button.Theme` 縺ｧ荳頑嶌縺阪〒縺阪∪縺吶よ里蟄倥・ `StationeryDrawingTools` 縺ｫ繧・`Theme` 縺後≠繧翫∪縺吶ゅユ繝ｼ繝樣・岼縺ｮ縺・■隗剃ｸｸ繝ｻ蠖ｱ縺ｯ謠冗判譁ｹ蠑上＃縺ｨ縺ｮ蟇ｾ蠢懊〒縺吶ＡStationeryUiHost` 縺ｮ蜈･蜉帶ｬ・・繝懊ち繝ｳ縺ｯ荳狗ｷ壻ｸｻ菴薙・陦ｨ遉ｺ縺ｧ縲∬ｧ剃ｸｸ繝ｻ蠖ｱ繧呈緒逕ｻ縺励∪縺帙ｓ縲・
## 蜈･蜉帙・謇譛画ｨｩ縺ｨ蟇ｾ蠢懃ｯ・峇

`WindowsTextInputService` 縺ｮ蠑墓焚縺ｯ **DesktopGL縺ｮ `SDL_Window*`** 縺ｧ縺吶８indowsDX縺ｮHWND繧呈ｸ｡縺輔↑縺・〒縺上□縺輔＞縲０S縺ｨ繝舌ャ繧ｯ繧ｨ繝ｳ繝峨・蟇ｾ蠢懊・蛻･縲・〒縺吶・
- 譁ｰ隕上い繝励Μ縺ｯ遒ｺ螳壽枚蟄励ｂ譛ｪ遒ｺ螳壽枚蟄励ｂ `ITextInputService` 縺九ｉ蜿励￠蜿悶ｊ縺ｾ縺吶ょ酔縺伜・蜉帶ｬ・∈ `GameWindow.TextInput` 繧帝㍾縺ｭ縺ｦ謗･邯壹＠縺ｪ縺・〒縺上□縺輔＞縲・- `WindowsCompositionObserver` 縺ｯ譌｢蟄倥い繝励Μ蜷代￠縺ｮ蜷域・迥ｶ諷句ｰら畑謗･邯壹〒縺吶ら｢ｺ螳壽枚蟄励ｒMonoGame縺九ｉ蜿励￠蜿悶ｋ譌｢蟄俶婿蠑上ｒ菫昴▽縺溘ａ縺ｫ谿九＠縺ｦ縺翫ｊ縲∵眠隕上・繧ｹ繝医→縺ｯ蜷梧凾菴ｿ逕ｨ縺励∪縺帙ｓ縲・- SDL2縺ｮ繝・く繧ｹ繝亥・蜉帙・繝励Ο繧ｻ繧ｹ蜀・〒1繧ｻ繝・す繝ｧ繝ｳ繧剃ｽｿ逕ｨ縺励∪縺吶６I繧ｹ繝ｬ繝・ラ縺ｧ髢句ｧ九・邨ゆｺ・・遐ｴ譽・＠縺ｦ縺上□縺輔＞縲・- `SetInputArea` 縺ｯ繧ｦ繧｣繝ｳ繝峨え蠎ｧ讓吶〒縺吶よ緒逕ｻ縺ｨ蜷後§ `UiViewport` 縺ｧ螟画鋤縺励・ｫ魯PI縺ｧ繝舌ャ繧ｯ繝舌ャ繝輔ぃ繝ｼ蠎ｧ讓吶→繧ｦ繧｣繝ｳ繝峨え蠎ｧ讓吶′逡ｰ縺ｪ繧九・繧ｹ繝医〒縺ｯ縺昴・蛟咲紫繧よ磁邯壼・縺ｧ螟画鋤縺励※縺上□縺輔＞縲・- 譌･譛ｬ隱槭・繧､繝ｳ繧ｹ繝医・繝ｫ貂医∩縺ｮ繝輔か繝ｳ繝医〒謠冗判縺励∪縺吶らｵｵ譁・ｭ励・謠冗判縺ｯ繝輔か繝ｳ繝医→Windows謠冗判譁ｹ蠑上↓萓晏ｭ倥＠縲√き繝ｩ繝ｼ邨ｵ譁・ｭ励・縺吶∋縺ｦ縺ｮ繧ｰ繝ｪ繝輔ｒ菫晁ｨｼ縺励∪縺帙ｓ縲・- Windows + DesktopGL縺ｧ繝薙Ν繝峨・謠冗判繝ｻ繝阪う繝・ぅ繝亡DL繧､繝吶Φ繝域､懈渊繧貞ｯｾ雎｡縺ｨ縺励∪縺吶ょｮ櫑ME縺ｮ蛟呵｣憺∈謚槭・蜈ｨDPI繝ｻ繧ｿ繝・メ縺ｮ螳滓ｩ滓､懆ｨｼ縺ｯ [讀懆ｨｼ險倬鹸](validation.md) 繧貞盾辣ｧ縺励※縺上□縺輔＞縲・- Linux/macOS縺ｮIME縲仝indowsDX縲＾S逕ｻ髱｢隱ｭ縺ｿ荳翫￡縲∬､・焚繧ｦ繧｣繝ｳ繝峨え縲；UI繝・じ繧､繝翫・縺ｯ譛ｪ蟇ｾ蠢懊〒縺吶ゅさ繧｢縺ｮ髱杆indows繝・せ繝医→UI蜈ｨ菴薙・髱杆indows蟇ｾ蠢懊・蛹ｺ蛻･縺励∪縺吶・
## 髢｢騾｣雉・侭

- [莉悶い繝励Μ縺ｸ縺ｮ繧ｹ繧ｿ繧､繝ｫ險ｭ螳夂ｵ・∩霎ｼ縺ｿ繧ｬ繧､繝云(style-settings/README.md)・哂I 蜷代￠縺ｮ謗･邯壻ｾ九√が繝ｼ繝医Μ繝ｭ繝ｼ繝峨゛SON 縺ｮ隱ｭ縺ｿ譁ｹ縲∵里蟄・C# 縺九ｉ縺ｮ谿ｵ髫守噪縺ｪ遘ｻ陦後・
- [繧､繝ｳ繧ｹ繝壹け繧ｿ繝ｼ繝代ロ繝ｫ縺ｨ繝・・繝ｫ繝偵Φ繝域ｬЬ(../user/inspector-panel-guide.md)・夂判髱｢荳九・80px縺ｫ謫堺ｽ懆ｪｬ譏弱ｒ髮・ｴ・☆繧九√♀縺吶☆繧√・繧ｹ繧ｿ繧､繝ｫ繧ｬ繧､繝峨Λ繧､繝ｳ・亥ｿ・医〒縺ｯ縺ゅｊ縺ｾ縺帙ｓ・峨・
- [荳狗ｷ壻ｻ倥″繝・く繧ｹ繝医・隕句・縺第婿](../user/underline-guide.md)
- [繝ｪ繧ｹ繝・I險ｭ險医・逶ｮ螳云(list-ui-guidelines.md)
- [驟榊ｸ・ヱ繝・こ繝ｼ繧ｸ縺ｮ繝√ぉ繝・け繧ｵ繝](../user/package-checksums.txt)
- [謚ｽ蜃ｺ蜈・・繝ｩ繧､繧ｻ繝ｳ繧ｹ](../user/CircleSpaceCoordinator-LICENSE.txt)
- [髢狗匱閠・髄縺代ラ繧ｭ繝･繝｡繝ｳ繝・(README.md)・壹Λ繧､繝悶Λ繝ｪ繝ｼ閾ｪ菴薙・髢狗匱縺ｨ讀懆ｨｼ縲・
縺阪・繧上ｉ縺ｹ縺ｮ遒・026縺ｨ繧ｵ繝ｼ繧ｯ繝ｫ繧ｹ繝壹・繧ｹ繧ｳ繝ｼ繝・ぅ繝阪・繧ｿ繝ｼ縺ｮ螳溯｣・°繧画歓蜃ｺ繝ｻ謨ｴ逅・＠縺ｦ縺・∪縺吶ょ・縺ｮ繝ｩ繧､繧ｻ繝ｳ繧ｹ陦ｨ遉ｺ縺ｨ萓晏ｭ倡黄縺ｫ縺､縺・※縺ｯ [THIRD-PARTY-NOTICES.md](../../THIRD-PARTY-NOTICES.md) 繧貞盾辣ｧ縺励※縺上□縺輔＞縲・
## 繧ｳ繝ｼ繝峨ｒ隱ｭ繧

[繧ｳ繝ｳ繝医Ο繝ｼ繝ｫ縺ｮ繝励Ο繧ｰ繝ｩ繝隗｣隱ｬ](control-guide.md) 縺ｫ縲∵緒逕ｻ蠅・阜縺ｨ驛ｨ蜩∝挨縺ｮ隱ｬ譏弱ｒ縺ｾ縺ｨ繧√※縺・∪縺吶・

### 繝ｬ繧､繧｢繧ｦ繝医ヤ繝ｪ繝ｼ縺ｮ驕ｸ謚樊棧

驟咲ｽｮ縺ｫ菴ｿ縺｣縺滓怙譁ｰ縺ｮ險育ｮ礼ｵ先棡繧呈ｸ｡縺励√Ξ繧､繧｢繧ｦ繝亥ｮ夂ｾｩ縺ｮ鬆伜沺繧よ､懃ｴ｢蟇ｾ雎｡縺ｫ縺吶ｋ縲・
```csharp
var entries = ui.Inspect(settings, arranged); // arranged 縺ｯ繧ｦ繧｣繝ｳ繝峨え縺ｮ繝斐け繧ｻ繝ｫ蜊倅ｽ・if (developerWindow.IsOpen && developerWindow.SelectedPath is { } selected)
{
    var entry = DeveloperInspectionLayout.FindVisibleEntry(entries, selected);
    if (entry?.WindowBounds is { } bounds)
        ui.DrawInspectionOutline(bounds);
}
```

隍・焚繝帙せ繝医・蝣ｴ蜷医・縲∫ｵ仙粋縺励◆繝｢繝・Ν驟榊・縺ｫ `DeveloperInspectionLayout.Apply(entries, settings, arranged)` 繧帝←逕ｨ縺吶ｋ縲る撼陦ｨ遉ｺ繝壹・繧ｸ縺ｮ繝｢繝・Ν縺ｯ `Visible = false` 縺ｫ縺吶ｋ縲ゅΞ繧､繧｢繧ｦ繝医ヮ繝ｼ繝峨ｒ繧ｭ繝｣繝励メ繝｣繝ｼ蛻､螳壹・繝｢繝・Ν驟榊・縺ｸ霑ｽ蜉縺吶ｋ蠢・ｦ√・縺ｪ縺・・

蛻・牡蠅・阜繧よ緒縺丞ｴ蜷医・縲∵､懃ｴ｢縺励◆ entry 繧・`ui.DrawInspectionSelection(entry)` 縺ｫ貂｡縺吶・螟門捉縺ｯ螳溽ｷ壹～PartitionLines` 縺ｯ譯・牡縺ｮ轤ｹ邱壹〒謠冗判縺輔ｌ繧九Ｈrid 縺ｯ邨仙粋繧ｻ繝ｫ縺ｮ蜀・Κ繧堤怐縺阪‥ock 縺ｯ驟榊・鬆・↓蛻・ｊ蜿悶▲縺溷｢・阜繧定｡ｨ遉ｺ縺吶ｋ縲ＡDrawInspectionOutline(bounds)` 縺ｯ蠑輔″邯壹″螟門捉縺縺代ｒ謠上￥縲・
