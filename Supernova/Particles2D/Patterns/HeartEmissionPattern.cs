using System;
using Microsoft.Xna.Framework;

namespace Supernova.Particles2D.Patterns
{
 /// <summary>
 /// 
 /// </summary>
 public class HeartEmissionPattern : IEmissionPattern
 {
  private readonly float radius;

  public HeartEmissionPattern(float radius)
  {
   this.radius = radius;
  }

  public Vector2 CalculateParticlePosition(Random random, Vector2 emitPosition)
  {
   double t = random.NextDouble()*10 - 5;
   double x = 16 * Math.Pow(Math.Sin(t), 3);
   double y = 5 - (13 * Math.Cos(t) - 5 * Math.Cos(2 * t) - 2 * Math.Cos(3 * t) - Math.Cos(4 * t));
   Vector2 offset = new Vector2((float)x * radius, (float)y * radius);
   return Vector2.Add(emitPosition, offset);
  }
 }
}
