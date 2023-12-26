using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Supernova.Particles2D.Modifiers.Alpha
{
 /// <summary>
 /// Modifier to fade particles based on particle age
 /// </summary>
 public class RotateAgeTransform : IModifier
 {
  private static readonly Random Random = new Random();
  private float m_minSpeed;
  private float m_maxSpeed;
  
  public RotateAgeTransform(float minSpeed, float maxSpeed)
  {
   m_minSpeed = minSpeed;
   m_maxSpeed = maxSpeed;
  }

  public void Update(float particleAge, double totalMilliseconds, double elapsedSeconds, Particle2D particle)
  {
   if (particle.AngularVelocity == 0f)
    particle.AngularVelocity = (float)(Random.NextDouble() * (m_maxSpeed - m_minSpeed) + m_minSpeed);
   particle.Rotation += (float)elapsedSeconds * particle.AngularVelocity;
  }
 }
}
