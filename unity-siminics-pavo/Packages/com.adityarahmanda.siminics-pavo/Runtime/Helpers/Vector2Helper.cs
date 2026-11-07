using System;
using UnityEngine;

namespace Rahmanda.PavoSDK
{
    public class Vector2Helper
    {
        public static Vector2 ToVector2(Vector2 origin, float rotation, float angle, float distance)
        {
            double angleRadian = AngleHelper.DegreeToRadian(angle + rotation);
            double dblX = origin.x + Math.Cos(angleRadian) * distance;
            double dblY = origin.y + Math.Sin(angleRadian) * distance;

            float x = Convert.ToSingle(dblX);
            float y = Convert.ToSingle(dblY);
            Vector2 vector2 = new Vector2(x, y);
            return vector2;
        }

        public static Vector2 ToVector2(Vector2 origin, float rotation, PavoScanPoint point)
        {
            Vector2 pointF = ToVector2(origin, rotation, point.angle, point.distance);
            return pointF;
        }
    }
}
