using UnityEngine;
using UnityEngine.Rendering;

namespace ArcaneOnyx.MeshGizmos
{
    public abstract class MGizmoBaseDrawCall
    {
        public abstract float RemainingTime { get; }
        public bool KeepOneFrame { get; set; }
        public bool AddThisFrame { get; set; }

        public abstract MaterialPropertyBlock MaterialPropertyBlock { get; }

        public abstract MGizmoBaseDrawCall SetMaterial(Material material);
        public abstract MGizmoBaseDrawCall SetColor(Color color);
        public abstract MGizmoBaseDrawCall SetDuration(float duration);
        public abstract MGizmoBaseDrawCall SetShadowCastingMode(ShadowCastingMode shadowCastingMode);
        public abstract MGizmoBaseDrawCall SetReceiveShadows(bool value);
        public abstract void Draw(Camera camera, float deltaTime);
        public abstract MGizmoBaseDrawCall Clone();

        //guards pooled instances against being released twice; managed by the Release overrides
        protected bool pooled;

        //called by MGizmos when it discards a clone it created, so implementations can recycle
        //themselves; the originals returned by the MGizmos.Render* API are caller-owned and never released
        internal virtual void Release() { }
    }
}