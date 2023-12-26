using System;
using Microsoft.Xna.Framework;

namespace Supernova.Particles2D.Patterns
{
 public class RectangleAreaEmissionPattern : IEmissionPattern
 {
  private readonly int width;
  private readonly int height;

  public RectangleAreaEmissionPattern(int width, int height)
  {
   this.width = width;
   this.height = height;
  }

  public Vector2 CalculateParticlePosition(Random random, Vector2 emitPosition)
  {
   Vector2 offset = Vector2.Zero;
   offset.X = (float)(width * random.NextDouble());
   offset.Y = (float)(height * random.NextDouble());
   return Vector2.Add(emitPosition, offset);
  }
 }
}