using Godot;
using System;
using System.IO;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Runs;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;

namespace EmjoyMod;

public struct DynamicEmoteMessage : INetMessage 
{
    public byte[] Data;
    public string ImgId;
    
    public bool ShouldBroadcast => true;
    public NetTransferMode Mode => NetTransferMode.Reliable;
    public LogLevel LogLevel => LogLevel.Info; 

    public void Serialize(PacketWriter writer) {
        writer.WriteString(ImgId ?? "emote");
        byte[] data = Data ?? Array.Empty<byte>();
        writer.WriteInt(data.Length);
        writer.WriteBytes(data, data.Length);
    }

    public void Deserialize(PacketReader reader) {
        ImgId = reader.ReadString();
        int length = reader.ReadInt();
        Data = new byte[length];
        reader.ReadBytes(Data, length);
    }
}
public static class DynamicEmoteManager 
{
    public static readonly string TempDir = "user://Emjoy_Temp/";

    // 修复：补回 Init 方法
    public static void Init() {
        if (!DirAccess.DirExistsAbsolute(TempDir)) {
            DirAccess.MakeDirRecursiveAbsolute(TempDir);
        }
        GD.Print("[Emjoy] 临时目录已初始化: " + TempDir);
    }

public static Image? SafeLoadImage(string path) {
    if (string.IsNullOrEmpty(path)) return null;
    
    string absolutePath = ProjectSettings.GlobalizePath(path);
    
    if (!System.IO.File.Exists(absolutePath)) {
        GD.PrintErr($"[Emjoy] 文件不存在: {absolutePath}");
        return null;
    }

    try {
        var img = new Image();
        Error err = img.Load(absolutePath);
        
        if (err != Error.Ok) {
            GD.PrintErr($"[Emjoy] 图片加载失败 (Error: {err}) 路径: {absolutePath}");
            return null;
        }
        
        if (img.IsCompressed() || img.IsEmpty()) {
            GD.PrintErr($"[Emjoy] 图片数据无效或为空");
            return null;
        }

        return img;
    } catch (Exception e) {
        GD.PrintErr($"[Emjoy] 加载异常: {e.Message}");
        return null;
    }
}

    public static void SendEmote(string localPath)
{
    var img = SafeLoadImage(localPath);
    if (img == null) return;

    // 等比例缩放，最大边长不超过 256
    int maxSize = 256;
    int width = img.GetWidth();
    int height = img.GetHeight();
    if (width > maxSize || height > maxSize)
    {
        float ratio = (float)width / height;
        if (width > height)
        {
            width = maxSize;
            height = (int)(width / ratio);
        }
        else
        {
            height = maxSize;
            width = (int)(height * ratio);
        }
        img.Resize(width, height, Image.Interpolation.Lanczos);
    }

    byte[] buffer = img.SavePngToBuffer();
    // 其余代码...


        var msg = new DynamicEmoteMessage { 
            Data = buffer, 
            ImgId = Guid.NewGuid().ToString().Substring(0, 8) 
        };

        try {
            var sync = RunManager.Instance?.FlavorSynchronizer;
            var gs = Traverse.Create(sync).Field("_gameService").GetValue<INetGameService>();
            gs?.SendMessage(msg);
        } catch (Exception e) {
            GD.Print($"[Emjoy] 网络发送失败: {e.Message}");
        }

        ShowBubble(localPath, LocalContext.NetId ?? 0, true);
    }

    public static void HandleMessage(DynamicEmoteMessage msg, ulong senderId) {
        string savePath = TempDir + msg.ImgId + ".png";
        using var file = Godot.FileAccess.Open(savePath, Godot.FileAccess.ModeFlags.Write);
        file?.StoreBuffer(msg.Data);
        file?.Close();
        ShowBubble(savePath, senderId, false);
    }

private static void ShowBubble(string path, ulong senderId, bool isLocalPath) {
    Callable.From(() => {
        var state = CombatManager.Instance?.DebugOnlyGetState();
        var player = state?.GetPlayer(senderId);
        var room = MegaCrit.Sts2.Core.Nodes.Rooms.NCombatRoom.Instance;
        if (player?.Creature == null || room == null) return;
        var playerNode = room.GetCreatureNode(player.Creature);
        if (playerNode == null) return;

        var img = SafeLoadImage(path);
        if (img == null) return;
        var tex = ImageTexture.CreateFromImage(img);

        float targetHeight = 220f; 
        float aspectRatio = (float)tex.GetWidth() / tex.GetHeight();
        float targetWidth = targetHeight * aspectRatio;

        var rect = new TextureRect {
            Texture = tex,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, 
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            Size = new Vector2(targetWidth, targetHeight), 
            Modulate = new Color(1, 1, 1, 0),
            ZIndex = 110 
        };

        float charWidth = playerNode.Hitbox.Size.X;
        float charHeight = playerNode.Hitbox.Size.Y;

        float posX = (charWidth * 0.4f) + 20f; 
        float posY = -charHeight * 1.2f;

        rect.Position = new Vector2(posX, posY);


        playerNode.AddChild(rect);

        var tween = rect.CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(rect, "modulate:a", 1.0f, 0.2f); 
        

        var seq = rect.CreateTween();
        seq.TweenInterval(2.0f);
        seq.TweenProperty(rect, "modulate:a", 0.0f, 0.5f); 
        seq.SetParallel(true);
        seq.TweenProperty(rect, "position:y", rect.Position.Y - 50f, 0.5f); 
        seq.Chain().TweenCallback(Callable.From(() => rect.QueueFree()));
        
    }).CallDeferred();
}
}