using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcaneOnyx
{
    public class MGizmoCompositeDrawCall : MGizmoBaseDrawCall
    {
        private List<MGizmoBaseDrawCall> drawCalls = new();
        
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

        public override MaterialPropertyBlock MaterialPropertyBlock { get; }

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
                    drawCalls.RemoveAt(i);
                }
            }
        }

        public override MGizmoBaseDrawCall Clone()
        {
            return new MGizmoCompositeDrawCall(drawCalls.ToList());
        }
    }
}