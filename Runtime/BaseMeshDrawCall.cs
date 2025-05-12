using UnityEngine;

namespace ArcaneOnyx
{
    public abstract class BaseMeshDrawCall
    {
        public abstract float RemainingTime { get; }
        
        public abstract BaseMeshDrawCall SetMaterial(Material material);
        public abstract BaseMeshDrawCall SetColor(Color color);
        public abstract BaseMeshDrawCall SetDuration(float duration);
        public abstract void Draw(Camera camera, float deltaTime);
    }
}