using UnityEngine;
using UnityEngine.Rendering;

namespace ArcaneOnyx
{
    public abstract class BaseMeshDrawCall
    {
        public abstract float RemainingTime { get; }

        public abstract MaterialPropertyBlock MaterialPropertyBlock { get; }

        public abstract BaseMeshDrawCall SetMaterial(Material material);
        public abstract BaseMeshDrawCall SetColor(Color color);
        public abstract BaseMeshDrawCall SetDuration(float duration);
        public abstract BaseMeshDrawCall SetShadowCastingMode(ShadowCastingMode shadowCastingMode);
        public abstract BaseMeshDrawCall SetReceiveShadows(bool value);
        public abstract void Draw(Camera camera, float deltaTime);
    }
}