using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;


namespace EmjoyMod;

[HarmonyPatch(typeof(NCombatUi), "_Ready")]
public class CombatUiPatch {
    private static PanelContainer? _pickerPanel;
    private static GridContainer? _emoteGrid;

    public static void Postfix(NCombatUi __instance) {
        Callable.From(() => SetupEmoteUi(__instance)).CallDeferred();
    }

    private static void SetupEmoteUi(NCombatUi ui) {
        GD.Print("[Emjoy] 正在手动构建 UI 面板...");

        var toggleBtn = new Button();
        toggleBtn.Text = "EMOTE";
        toggleBtn.Modulate = new Color(0, 1, 0); // 亮绿色
        toggleBtn.CustomMinimumSize = new Vector2(100, 45);
        ui.AddChild(toggleBtn);

        toggleBtn.AnchorLeft = 1.0f;
        toggleBtn.AnchorTop = 1.0f;
        toggleBtn.AnchorRight = 1.0f;
        toggleBtn.AnchorBottom = 1.0f;
        toggleBtn.OffsetLeft = -450;
        toggleBtn.OffsetTop = -130;
        toggleBtn.OffsetRight = -350;
        toggleBtn.OffsetBottom = -80;

        _pickerPanel = new PanelContainer();
        _pickerPanel.CustomMinimumSize = new Vector2(400, 300);
        _pickerPanel.TopLevel = true;
        _pickerPanel.ZIndex = 2000;
        _pickerPanel.Hide();
        ui.AddChild(_pickerPanel);

        var style = new StyleBoxFlat();
        style.BgColor = new Color(0, 0, 0, 0.9f);
        style.SetBorderWidthAll(2);
        style.BorderColor = new Color(1, 1, 1, 0.5f);
        _pickerPanel.AddThemeStyleboxOverride("panel", style);

        var vbox = new VBoxContainer();
        _pickerPanel.AddChild(vbox);

        var title = new Label();
        title.Text = " 选择表情 ";
        title.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(title);

        _emoteGrid = new GridContainer();
        _emoteGrid.Columns = 5;
        _emoteGrid.AddThemeConstantOverride("h_separation", 10);
        _emoteGrid.AddThemeConstantOverride("v_separation", 10);
        
        var scroll = new ScrollContainer();
        scroll.CustomMinimumSize = new Vector2(0, 250);
        scroll.AddChild(_emoteGrid);
        vbox.AddChild(scroll);

        RefreshEmotes();

        toggleBtn.Pressed += () => {
            _pickerPanel.Visible = !_pickerPanel.Visible;
            if (_pickerPanel.Visible) {
                _pickerPanel.GlobalPosition = toggleBtn.GlobalPosition + new Vector2(-200, -350);
                GD.Print("[Emjoy] 面板已打开，位置: " + _pickerPanel.GlobalPosition);
            }
        };

        GD.Print("[Emjoy] UI 手动构建完成。");
    }

  private static void RefreshEmotes() {
    if (_emoteGrid == null) return;
    foreach (Node child in _emoteGrid.GetChildren()) child.QueueFree();

    string dllPath = typeof(CombatUiPatch).Assembly.Location;
    string assetsPath = Path.Combine(Path.GetDirectoryName(dllPath) ?? "", "Assets");

    if (!Directory.Exists(assetsPath)) {
        GD.PrintErr($"[Emjoy] 找不到 Assets 文件夹: {assetsPath}");
        return;
    }

    var files = Directory.EnumerateFiles(assetsPath)
        .Where(f => f.EndsWith(".png") || f.EndsWith(".jpg") || f.EndsWith(".jpeg"));

    foreach (string filePath in files) {
        var img = DynamicEmoteManager.SafeLoadImage(filePath);
        
        if (img == null) {
            GD.PrintErr($"[Emjoy] 跳过损坏的表情文件: {Path.GetFileName(filePath)}");
            continue; 
        }

        var tex = ImageTexture.CreateFromImage(img);
        var btn = new Button { 
            CustomMinimumSize = new Vector2(80, 80), 
            Icon = tex, 
            ExpandIcon = true,
            TooltipText = Path.GetFileName(filePath)
        };

        btn.Pressed += () => {
            GD.Print($"[Emjoy] 点击了: {Path.GetFileName(filePath)}");
            DynamicEmoteManager.SendEmote(filePath);
            _pickerPanel?.Hide();
        };

        _emoteGrid.AddChild(btn);
    }
    GD.Print($"[Emjoy] 成功加载 {_emoteGrid.GetChildCount()} 个表情。");
}
}


