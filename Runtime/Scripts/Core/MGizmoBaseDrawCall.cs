using UnityEngine;
using UnityEngine.Rendering;

namespace ArcaneOnyx
{
    public abstract class MGizmoBaseDrawCall
    {
        public abstract float RemainingTime { get; }

        public abstract MaterialPropertyBlock MaterialPropertyBlock { get; }

        public abstract MGizmoBaseDrawCall SetMaterial(Material material);
        public abstract MGizmoBaseDrawCall SetColor(Color color);
        public abstract MGizmoBaseDrawCall SetDuration(float duration);
        public abstract MGizmoBaseDrawCall SetShadowCastingMode(ShadowCastingMode shadowCastingMode);
        public abstract MGizmoBaseDrawCall SetReceiveShadows(bool value);
        public abstract void Draw(Camera camera, float deltaTime);
        public abstract MGizmoBaseDrawCall Clone();
    }
}