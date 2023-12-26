using Microsoft.Xna.Framework;

namespace Supernova.Particles2D.Modifiers.Alpha
{
    /// <summary>
    /// Modifier to fade particles based on particle age
    /// </summary>
    public class AlphaAgeTransform : IModifier
    {
       float min;
       float max;
       float alphaInterval;

       public AlphaAgeTransform()
       {
        min = 0;
        max = 1;
        alphaInterval = 0;
        FadeIn = false;
       }
     
       public AlphaAgeTransform(float m1, float m2, float a)
       {
        min = m1;
        max = m2;
        alphaInterval = a;
        FadeIn = false;
       }

        public void Update(float particleAge, double totalMilliseconds, double elapsedSeconds, Particle2D particle)
        {
         if (particleAge < alphaInterval)
          particle.Alpha = 1f;
         else
          particle.Alpha = FadeIn ? MathHelper.Lerp(min, max, (particleAge - alphaInterval) / (1-alphaInterval)) : MathHelper.Lerp( max, min, (particleAge - alphaInterval) / (1-alphaInterval) );
        }

        /// <summary>
        /// Gets or sets whether the modifier should fade in or out
        /// </summary>
        public bool FadeIn { get; set; }
    }
}
