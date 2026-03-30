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
    
    public static void Postfix(FlavorSynchronizer __instance) {
        if (__instance == null) return;

        var traverse = Traverse.Create(__instance);
        var gs = traverse.Field("_gameService").GetValue<INetGameService>();
        
        if (gs != null) {
            gs.RegisterMessageHandler<DynamicEmoteMessage>((msg, senderId) => {
                GD.Print($"[Emjoy] 收到来自 {senderId} 的表情消息");
                DynamicEmoteManager.HandleMessage(msg, senderId);
            });

            DynamicEmoteManager.Init();
            GD.Print("[Emjoy] 联机消息处理器已在 FlavorSynchronizer 中就绪。");
        } else {
            GD.PrintErr("[Emjoy] 无法获取 _gameService，消息监听失败。");
        }
    }
}