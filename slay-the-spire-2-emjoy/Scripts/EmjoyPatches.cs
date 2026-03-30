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
        toggleBtn.Text = "表情";
        toggleBtn.Modulate = new Color(0, 1, 0); // 亮绿色
        toggleBtn.CustomMinimumSize = new Vector2(100, 45);
        ui.AddChild(toggleBtn);

        // 1. 设置按钮坐标（锚点设为右下角）
        toggleBtn.AnchorLeft = 1.0f;
        toggleBtn.AnchorTop = 1.0f;
        toggleBtn.AnchorRight = 1.0f;
        toggleBtn.AnchorBottom = 1.0f;

        // 这里的 Offset 控制按钮在“结束回合”上方的具体位置
        // 如果想更靠右，调大 OffsetLeft/Right 的负数（如-150），调小则靠左。
        toggleBtn.OffsetLeft = -250; 
        toggleBtn.OffsetTop = -300; // 调大这个负值（如-350）可以把按钮放得更高
        toggleBtn.OffsetRight = -150;
        toggleBtn.OffsetBottom = -255;

        // 2. 面板初始化
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
        _emoteGrid.Columns = 4;
        _emoteGrid.AddThemeConstantOverride("h_separation", 10);
        _emoteGrid.AddThemeConstantOverride("v_separation", 10);
        
        var scroll = new ScrollContainer();
        scroll.CustomMinimumSize = new Vector2(0, 250);
        scroll.AddChild(_emoteGrid);
        vbox.AddChild(scroll);

        RefreshEmotes();

        // 3. 实现向左弹出逻辑
        toggleBtn.Pressed += () => {
            _pickerPanel.Visible = !_pickerPanel.Visible;
            if (_pickerPanel.Visible) {
                // 向左弹出的核心计算：
                // X轴 = 按钮位置 - 面板宽度 - 间隙
                // Y轴 = 按钮位置 - 面板高度 + 按钮高度 (让底边对齐按钮)
                Vector2 btnPos = toggleBtn.GlobalPosition;
                float panelW = _pickerPanel.CustomMinimumSize.X;
                float panelH = _pickerPanel.CustomMinimumSize.Y;
                
                _pickerPanel.GlobalPosition = new Vector2(
                    btnPos.X - panelW - 20, // 减去宽度实现向左移，20是间隙
                    btnPos.Y - (panelH - toggleBtn.Size.Y) // 向上移动使面板与按钮底部大致对齐
                );
                
                GD.Print("[Emjoy] 面板已向左打开");
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

