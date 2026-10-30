using UnityEngine;

namespace Farm.View
{
    /// <summary>Colours a placeholder renderer through a property block, so no material per object is needed.</summary>
    public static class PlaceholderTint
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static MaterialPropertyBlock block;

        public static void Apply(Renderer renderer, Color color)
        {
            block ??= new MaterialPropertyBlock();
            block.SetColor(BaseColor, color);
            renderer.SetPropertyBlock(block);
        }
    }
}
