using System.Collections.Generic;
using UnityEngine;

namespace ArcaneOnyx
{
    public class CompositeMeshDrawCall : BaseMeshDrawCall
    {
        private List<BaseMeshDrawCall> drawCalls = new();

        public override float RemainingTime 
        {
            get
            {
                float duration = 0;

                foreach (var dc in drawCalls)
                {
                    if (dc.RemainingTime > duration) duration = dc.RemainingTime;
                }

                return duration;
            }
        }
        
        public void AddDrawCall(BaseMeshDrawCall drawCall)
        {
            drawCalls.Add(drawCall);
        }

        public override BaseMeshDrawCall SetColor(Color color)
        {
            foreach (var dc in drawCalls)
            {
                dc.SetColor(color);
            }

            return this;
        }

        public override BaseMeshDrawCall SetDuration(float duration)
        {
            foreach (var dc in drawCalls)
            {
                dc.SetDuration(duration);
            }

            return this;
        }

        public override BaseMeshDrawCall SetMaterial(Material material)
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
                
                if (drawCalls[i].RemainingTime <= 0)
                {
                    drawCalls.RemoveAt(i);
                }
            }
        }
    }
}