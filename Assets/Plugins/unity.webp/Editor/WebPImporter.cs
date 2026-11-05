using System.IO;
using UnityEditor;
using UnityEngine;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif

namespace WebP
{
    [ScriptedImporter(2, "webp")] // Увеличили версию импортера, чтобы Unity поняла, что нужно пересобрать файлы
    public class WebPImporter : ScriptedImporter
    {
        const TextureFormat WEBP_LOAD_DEFAULT_TEXTURE_FORMAT = TextureFormat.RGBA32;

        [Tooltip("Is Generate Mipmap")]
        public bool Mipmap = false;

        [Tooltip("Is Linear ColorSpace")]
        public bool Linear = false;

        [Tooltip("Should it generate a sprite, alongside the base texture?")]
        public bool ShouldGenerateSprite = false;
        
        [Tooltip("Converted Texture Format")]
        public TextureFormat TextureFormat = WEBP_LOAD_DEFAULT_TEXTURE_FORMAT;

        [Header("Tile Settings (Anti-Seam)")]
        [Tooltip("Wrap mode for textures. Use Clamp for tiles to avoid seams.")]
        public TextureWrapMode WrapMode = TextureWrapMode.Clamp;

        [Tooltip("Filter mode for textures. Use Point for pixel-perfect tile snapping.")]
        public FilterMode FilterMode = FilterMode.Point;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            byte[] bytes = File.ReadAllBytes(ctx.assetPath);
            Texture2D webpTexture = Texture2DExt.CreateTexture2DFromWebP(bytes, lMipmaps: false, lLinear: Linear, lError: out Error lError, makeNoLongerReadable: false);
            if (lError != Error.Success)
            {
                Debug.LogError("Error decoding WebP texture: " + lError);
                Texture2D tex = new Texture2D(1, 1);
                tex.SetPixel(1, 1, Color.red);
                ctx.AddObjectToAsset("main", tex);
                ctx.SetMainObject(tex);
                return;
            }

            Texture2D targetTexture;
            if (Mipmap)
            {
                int width = webpTexture.width;
                int height = webpTexture.height;
                UnityEngine.Experimental.Rendering.GraphicsFormat graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormatUtility.GetGraphicsFormat(TextureFormat.RGBA32, false);
                uint mipmapSize = UnityEngine.Experimental.Rendering.GraphicsFormatUtility.ComputeMipmapSize(width, height, graphicsFormat);
                int mipmapCount = (int)mipmapSize / (width * height);
                targetTexture = new Texture2D(webpTexture.width, webpTexture.height, TextureFormat, mipCount: mipmapCount, linear: Linear);
            }
            else
            {
                targetTexture = new Texture2D(webpTexture.width, webpTexture.height, TextureFormat, mipCount: -1, linear: Linear);
            }

            bool isSuccess = targetTexture.LoadImage(webpTexture.EncodeToPNG());
            if (!isSuccess)
            {
                Debug.LogError($"Error convert target texture format : {WEBP_LOAD_DEFAULT_TEXTURE_FORMAT} => {TextureFormat}");
                Texture2D tex = new Texture2D(1, 1);
                tex.SetPixel(1, 1, Color.red);
                ctx.AddObjectToAsset("main", tex);
                ctx.SetMainObject(tex);
                return;
            }

            // --- НАШИ ИСПРАВЛЕНИЯ ТУТ ---
            targetTexture.wrapMode = WrapMode;
            targetTexture.filterMode = FilterMode;
            // ----------------------------

            EditorUtility.CompressTexture(targetTexture, TextureFormat, TextureCompressionQuality.Normal);
            ctx.AddObjectToAsset("main", targetTexture);
            ctx.SetMainObject(targetTexture);
            
            if (ShouldGenerateSprite) {
                Rect spriteRect = new Rect(Vector2.zero, new Vector2(webpTexture.width, webpTexture.height));
                Sprite sprite = Sprite.Create(targetTexture, spriteRect, Vector2.zero);
                sprite.name = Path.GetFileNameWithoutExtension(ctx.assetPath);
                ctx.AddObjectToAsset("sprite", sprite);
            }
        }
    }
}
