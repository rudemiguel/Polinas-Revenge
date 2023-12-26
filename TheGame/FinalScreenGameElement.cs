using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Supernova.Particles2D;
using Supernova.Particles2D.Modifiers.Alpha;
using Supernova.Particles2D.Modifiers.Scale;
using Supernova.Particles2D.Patterns;

namespace TheGame
{

 /// <summary>
 /// Финальный экран
 /// </summary>
 class FinalScreenGameElement : DrawableGameComponent
 {
  class ParticleEmitter
  {
   public ParticleEffect2D effect;
   public Vector2 pos;

   public ParticleEmitter(ParticleEffect2D e, Vector2 p)
   {
    effect = e;
    pos = p;
   }
  }

  private static readonly Random Random = new Random();
  private PanelManager m_panelManager;
  private SoundManager m_soundManager;
  private MusicManager m_musicManager;
  private Texture2D m_backgroundTexture;
  private Texture2D m_heartTexture;
  private SpriteBatch m_spriteBatch;
  private ParticleEffect2D m_heartParticleEmitter;
  private List<ParticleEmitter> m_exposionEmitterList;
  private long m_nExposionEmitterRemainMsec;

  /// <summary>
  /// Конструктор
  /// </summary>
  /// <param name="game"></param>
  public FinalScreenGameElement(Game game, PanelManager panelManager, SoundManager soundManager, MusicManager musicManager) :
   base(game)
  {
   m_panelManager = panelManager;
   m_soundManager = soundManager;
   m_musicManager = musicManager;
   m_exposionEmitterList = new List<ParticleEmitter>();
   m_nExposionEmitterRemainMsec = 0;
  }

  #region DrawableGameComponent Members

  /// <summary>
  /// Загрузка контента
  /// </summary>
  /// <param name="content"></param>
  protected override void LoadContent()
  {
   ContentManager content = Game.Content;
   GraphicsDevice graphicsDevice = Game.GraphicsDevice;
   m_backgroundTexture = content.Load<Texture2D>("Graphics\\background");
   m_heartTexture = content.Load<Texture2D>("Graphics\\heart3");
   m_spriteBatch = new SpriteBatch(graphicsDevice);
   m_heartParticleEmitter = ParticleEffect2DFactory.Initialize(2000, 1000)
                                                   .SetMaxParticleSpeed(0.8f)
                                                   .SetEmitAmount(20)
                                                   .AddTexture(m_heartTexture)
                                                   .AddModifier(new AlphaAgeTransform())
                                                   .AddModifier(new ScaleAgeTransform(new Vector2(0.3f, 0.3f), new Vector2(0.8f, 0.8f), new Vector2(1f, 1f)))
                                                   .SetEmissionPattern(new HeartEmissionPattern(10f))
                                                   .Create();
  }

  /// <summary>
  /// Выгружаем контент
  /// </summary>
  protected override void UnloadContent()
  {
  }

  /// <summary>
  /// Отрисовка
  /// </summary>
  /// <param name="gameTime"></param>
  public override void Draw(Microsoft.Xna.Framework.GameTime gameTime)
  {
   m_spriteBatch.Begin();
   m_spriteBatch.Draw(m_backgroundTexture, new Rectangle(0, 0, Game.GraphicsDevice.Viewport.Width, Game.GraphicsDevice.Viewport.Height), Color.White);
   m_heartParticleEmitter.Draw( m_spriteBatch, new Vector2(Game.GraphicsDevice.Viewport.Width/2, Game.GraphicsDevice.Viewport.Height/2-80) );
   foreach (ParticleEmitter emitter in m_exposionEmitterList)
    emitter.effect.Draw(m_spriteBatch, emitter.pos);
   m_spriteBatch.End();
  }

  /// <summary>
  /// Обновление
  /// </summary>
  /// <param name="gameTime"></param>
  public override void Update(Microsoft.Xna.Framework.GameTime gameTime)
  {
   m_heartParticleEmitter.Emit( (float)gameTime.TotalGameTime.TotalMilliseconds );
   m_heartParticleEmitter.Update( (float)gameTime.TotalGameTime.TotalMilliseconds, (float)gameTime.ElapsedGameTime.TotalSeconds );
   //
   foreach (ParticleEmitter emitter in m_exposionEmitterList)
    emitter.effect.Update((float)gameTime.TotalGameTime.TotalMilliseconds, (float)gameTime.ElapsedGameTime.TotalSeconds);
   //
   m_nExposionEmitterRemainMsec -= (long)gameTime.ElapsedGameTime.TotalMilliseconds;
   if (m_nExposionEmitterRemainMsec <= 0)
   {
    if (m_panelManager.GemList.Count > 0)
    {
     Vector2 pos = new Vector2((float)Game.GraphicsDevice.Viewport.Width * (float)Random.NextDouble(),
                                (float)Game.GraphicsDevice.Viewport.Height * (float)Random.NextDouble());
     GemGameObject gemGameObject = m_panelManager.GemList[Random.Next(m_panelManager.GemList.Count)];
     ParticleEffect2D particleEffect = ParticleEffect2DFactory.Initialize(50, 2000)
                                                    .SetMaxParticleSpeed(1f)
                                                    .SetEmitAmount(Random.Next(20)+5)
                                                    .AddTexture(gemGameObject.Texture)
                                                    .AddModifier(new AlphaAgeTransform(0f,1f,0.8f))
                                                    .AddModifier(new ScaleAgeTransform(new Vector2(0.3f, 0.3f), new Vector2(1f, 1f), new Vector2(1f, 1f)))
                                                    .AddModifier(new RotateAgeTransform( -4f, 4f))
                                                    .Create();
     particleEffect.Emit((float)gameTime.TotalGameTime.TotalMilliseconds);
     m_exposionEmitterList.Add(new ParticleEmitter(particleEffect, pos));
     m_soundManager.Play("final_gem_blow");
    }
    m_nExposionEmitterRemainMsec = (long)(Random.NextDouble() * 10000.0 / (m_panelManager.GemList.Count/2));
   }
  }

  #endregion


 }
}
