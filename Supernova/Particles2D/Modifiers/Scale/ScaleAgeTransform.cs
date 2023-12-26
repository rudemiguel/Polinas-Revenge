using System;
using Microsoft.Xna.Framework;

namespace Supernova.Particles2D.Modifiers.Scale
{
    /// <summary>
    /// Modifier to scale particles based on age
    /// </summary>
    public class ScaleAgeTransform : IModifier
    {
        private static readonly Random Random = new Random();
        private Vector2 currentScale;

        public ScaleAgeTransform(Vector2 startScale, Vector2 endScale, Vector2 randomScaleFactor)
        {
         StartScale = startScale;
         EndScale = endScale;
         RandomScaleFactor = RandomScaleFactor;
        }

        public void Update(float particleAge, double totalMilliseconds, double elapsedSeconds, Particle2D particle)
        {
            float age = (float)(totalMilliseconds - particle.InceptionTime);

            if (age == 0)
            {
             StartScale = StartScale + RandomScaleFactor * new Vector2( (float)Random.NextDouble()*2f - 0.5f, (float)Random.Next()*2f - 0.5f );
            }

            if (age < particle.Lifespan)
            {
                currentScale.X = MathHelper.Lerp(StartScale.X, EndScale.X, age / particle.Lifespan);
                currentScale.Y = MathHelper.Lerp(StartScale.Y, EndScale.Y, age / particle.Lifespan);
            }
            else
                currentScale = EndScale;

            particle.Scale = currentScale;
        }

        /// <summary>
        /// Gets or sets the particle starting scale
        /// </summary>
        public Vector2 StartScale { get; set; }

        /// <summary>
        /// Gets or sets the particle end scale
        /// </summary>
        public Vector2 EndScale { get; set; }

        /// <summary>
        /// Gets or sets the particle random factor
        /// </summary>
        public Vector2 RandomScaleFactor { get; set; }
    }
}
