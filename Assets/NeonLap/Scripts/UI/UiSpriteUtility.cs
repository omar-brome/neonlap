using UnityEngine;

namespace NeonLap.UI
{
    public static class UiSpriteUtility
    {
        static Sprite whiteSprite;

        public static Sprite White =>
            whiteSprite != null ? whiteSprite : whiteSprite = CreateWhiteSprite();

        static Sprite CreateWhiteSprite()
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            texture.hideFlags = HideFlags.HideAndDontSave;

            return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        }
    }
}
