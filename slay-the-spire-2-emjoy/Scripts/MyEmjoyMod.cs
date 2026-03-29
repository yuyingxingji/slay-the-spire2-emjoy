using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Entities.Players;
using System.Reflection;

namespace EmjoyMod;

public partial class MyEmjoyMod : Node 
{
    public override void _Ready()
    {
        GD.Print("[Emjoy] 正在初始化...");
        var harmony = new Harmony("com.emjoy.mod");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
        DynamicEmoteManager.Init();
    }
}

[HarmonyPatch(typeof(FlavorSynchronizer))]
[HarmonyPatch(MethodType.Constructor)]
[HarmonyPatch(new System.Type[] { typeof(INetGameService), typeof(IPlayerCollection), typeof(ulong) })]
public class FlavorPatch {
    
    // 2. 使用 __instance 来获取对象实例，不需要手动传入参数
    public static void Postfix(FlavorSynchronizer __instance) {
        if (__instance == null) return;

        // 3. 穿透私有字段获取 _gameService
        var traverse = Traverse.Create(__instance);
        var gs = traverse.Field("_gameService").GetValue<INetGameService>();
        
        if (gs != null) {
            // 4. 注册处理器
            gs.RegisterMessageHandler<DynamicEmoteMessage>((msg, senderId) => {
                GD.Print($"[Emjoy] 收到来自 {senderId} 的表情消息");
                DynamicEmoteManager.HandleMessage(msg, senderId);
            });

            // 初始化目录
            DynamicEmoteManager.Init();
            GD.Print("[Emjoy] 联机消息处理器已在 FlavorSynchronizer 中就绪。");
        } else {
            GD.PrintErr("[Emjoy] 无法获取 _gameService，消息监听失败。");
        }
    }
}