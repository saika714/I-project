using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SoftballConnect.Tests
{
    internal static class UnityFrameCapture
    {
        public static void Save(Camera camera, Canvas canvas, string path, int width, int height)
        {
            var previousMode = canvas != null ? canvas.renderMode : RenderMode.ScreenSpaceOverlay;
            var previousCamera = canvas != null ? canvas.worldCamera : null;
            float previousPlane = canvas != null ? canvas.planeDistance : 0;
            var previousTarget = camera.targetTexture; var previousActive = RenderTexture.active;
            var target = new RenderTexture(width, height, 24);
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                if (canvas != null)
                {
                    canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera;
                    canvas.planeDistance = camera.nearClipPlane + 0.01f;
                }
                camera.targetTexture = target; Canvas.ForceUpdateCanvases();
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                if (canvas != null)
                { canvas.renderMode = previousMode; canvas.worldCamera = previousCamera; canvas.planeDistance = previousPlane; }
                camera.targetTexture = previousTarget; RenderTexture.active = previousActive;
                target.Release(); Object.Destroy(target); Object.Destroy(texture);
            }
        }
    }
}
