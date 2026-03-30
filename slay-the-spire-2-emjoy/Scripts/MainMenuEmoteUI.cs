using Godot;
using HarmonyLib;
using System;
using System.IO;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace EmjoyMod
{
    [HarmonyPatch(typeof(NMainMenu), "_Ready")]
    public static class MainMenuEmoteUI
    {
        private static bool _buttonsAdded = false;

        public static void Postfix(NMainMenu __instance)
        {
            if (_buttonsAdded) return;
            _buttonsAdded = true;

            string assetsDir = Path.Combine(Path.GetDirectoryName(typeof(DynamicEmoteManager).Assembly.Location) ?? "", "Assets");
            if (!Directory.Exists(assetsDir)) Directory.CreateDirectory(assetsDir);

            float screenHeight = __instance.GetViewportRect().Size.Y;

            var btnManage = new Button
            {
                Text = " 📂 管理表情包 ",
                CustomMinimumSize = new Vector2(180, 50)
            };
            __instance.AddChild(btnManage);
            btnManage.Position = new Vector2(50, screenHeight - 210);
            btnManage.Pressed += () =>
            {
                string folder = ProjectSettings.GlobalizePath(assetsDir);
                OS.ShellOpen(folder);
                GD.Print($"[Emjoy] 打开文件夹: {folder}");
            };

            var btnImport = new Button
            {
                Text = " ➕ 导入图片 ",
                CustomMinimumSize = new Vector2(180, 50)
            };
            __instance.AddChild(btnImport);
            btnImport.Position = new Vector2(50, screenHeight - 150);
            btnImport.Pressed += () =>
            {
                var fd = new FileDialog
                {
                    Access = FileDialog.AccessEnum.Filesystem,
                    FileMode = FileDialog.FileModeEnum.OpenFile,
                    Filters = new string[] { "*.png,*.jpg,*.jpeg ; 图片文件" },
                    UseNativeDialog = true
                };
                fd.FileSelected += (path) =>
                {
                    var img = new Image();
                    if (img.Load(path) == Error.Ok)
                    {
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

                        string saveName = $"emote_{DateTime.Now:yyyyMMddHHmmss}.png";
                        string fullPath = Path.Combine(assetsDir, saveName);
                        img.SavePng(fullPath);
                        GD.Print($"[Emjoy] 已导入: {saveName} (尺寸: {width}x{height})");
                    }
                };
                __instance.AddChild(fd);
                fd.PopupCentered();
            };
        }
    }
}