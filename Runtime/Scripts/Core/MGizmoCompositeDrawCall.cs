using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcaneOnyx.MeshGizmos
{
    public class MGizmoCompositeDrawCall : MGizmoBaseDrawCall
    {
        private List<MGizmoBaseDrawCall> drawCalls = new();

        //recycles the per-camera clones MGizmos creates and destroys - see Clone/Release. Only instances
        //created by Clone are ever pooled, so a pooled composite always owns its list
        private static readonly Stack<MGizmoCompositeDrawCall> pool = new();
        private const int MaxPoolSize = 256;
        
        public override float RemainingTime 
        {
            get
            {
                float duration = float.MinValue;

                foreach (var dc in drawCalls)
                {
                    if (dc.RemainingTime > duration) duration = dc.RemainingTime;
                }
                
                return duration;
            }
        }

        public override MaterialPropertyBlock MaterialPropertyBlock => null;

        public MGizmoCompositeDrawCall() { }

        public MGizmoCompositeDrawCall(List<MGizmoBaseDrawCall> dc)
        {
            drawCalls = dc;
        }

        public void AddDrawCall(MGizmoBaseDrawCall drawCall)
        {
            drawCalls.Add(drawCall);
        }

        public override MGizmoBaseDrawCall SetColor(Color color)
        {
            foreach (var dc in drawCalls)
            {
                dc.SetColor(color);
            }

            return this;
        }

        public override MGizmoBaseDrawCall SetDuration(float duration)
        {
            foreach (var dc in drawCalls)
            {
                dc.SetDuration(duration);
            }

            return this;
        }

        public override MGizmoBaseDrawCall SetShadowCastingMode(ShadowCastingMode shadowCastingMode)
        {
            foreach (var dc in drawCalls)
            {
                dc.SetShadowCastingMode(shadowCastingMode);
            }

            return this;
        }

        public override MGizmoBaseDrawCall SetReceiveShadows(bool value)
        {
            foreach (var dc in drawCalls)
            {
                dc.SetReceiveShadows(value);
            }

            return this;
        }

        public override MGizmoBaseDrawCall SetMaterial(Material material)
        {
            foreach (var dc in drawCalls)
            {
                dc.SetMaterial(material);
            }

            return this;
        }
        
        public override void Draw(Camera camera, float deltaTime)
        {
            for (int i = drawCalls.Count - 1; i >= 0; i--)
            {
                drawCalls[i].Draw(camera, deltaTime);

                if (drawCalls[i].RemainingTime < 0)
                {
                    drawCalls[i].Release();
                    drawCalls.RemoveAt(i);
                }
            }
        }

        public override MGizmoBaseDrawCall Clone()
        {
            var composite = pool.Count > 0 ? pool.Pop() : new MGizmoCompositeDrawCall();
            composite.pooled = false;
            composite.KeepOneFrame = false;
            composite.AddThisFrame = false;

            foreach (var dc in drawCalls)
            {
                composite.drawCalls.Add(dc.Clone());
            }

            return composite;
        }

        internal override void Release()
        {
            if (pooled) return;

            foreach (var dc in drawCalls)
            {
                dc.Release();
            }

            drawCalls.Clear();

            //only pool exact instances so a user subclass never lands in this pool
            if (GetType() != typeof(MGizmoCompositeDrawCall) || pool.Count >= MaxPoolSize) return;

            pooled = true;
            pool.Push(this);
        }
    }
}