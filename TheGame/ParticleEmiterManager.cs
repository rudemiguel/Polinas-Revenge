using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Supernova.Particles2D;
using Supernova.Particles2D.Modifiers.Alpha;
using Supernova.Particles2D.Modifiers.Scale;
using Supernova.Particles2D.Modifiers.Movement.Gravity;
using Supernova.Particles2D.Patterns;

namespace TheGame
{
 
 /// <summary>
 /// Излучатель частиц
 /// </summary>
 class ParticleEmitterManager
 {
  /// <summary>
  /// Элемент списка эмитеров
  /// </summary>
  private class EmitterHolder
  {
   private ParticleEffect2D m_effect;
   private TimeSpan m_lifeTime;
   private Vector2 m_pos;
   private bool m_bNeedEmitt;
   private bool m_bViewportRelative;

   public EmitterHolder(ParticleEffect2D effect, Vector2 pos, TimeSpan lifeTime, bool bViewportRelative)
   {
    m_effect = effect;
    m_lifeTime = lifeTime;
    m_pos = pos;
    m_bNeedEmitt = true;
    m_bViewportRelative = bViewportRelative;
   }

   public ParticleEffect2D Effect
   {
    get
    {
     return m_effect;
    }
   }

   public TimeSpan LifeTime
   {
    get
    {
     return m_lifeTime;
    }
    set
    {
     m_lifeTime = value;
    }
   }

   public Vector2 Position
   {
    get
    {
     return m_pos;
    }
   }

   public bool IsNeedEmitt
   {
    get
    {
     return m_bNeedEmitt;
    }
    set
    {
     m_bNeedEmitt = value;
    }
   }

   public bool IsViewportRelative
   {
    get
    {
     return m_bViewportRelative;
    }
   }
  }

  private List<EmitterHolder> m_emitterList;
  private List<Texture2D> m_heartTextureList;
  private List<Texture2D> m_waterTextureList;
  private List<Texture2D> m_leafTextureList;

  public ParticleEmitterManager()
  {
   m_emitterList = new List<EmitterHolder>();
  }

  public void LoadContent(Microsoft.Xna.Framework.Content.ContentManager contentManager)
  {
   m_heartTextureList = new List<Texture2D>()
   {
    contentManager.Load<Texture2D>("Graphics\\heart1"),
			 contentManager.Load<Texture2D>("Graphics\\heart2"),
			 contentManager.Load<Texture2D>("Graphics\\heart3"),
			 contentManager.Load<Texture2D>("Graphics\\heart4")
   };
   m_waterTextureList = new List<Texture2D>()
   {
    contentManager.Load<Texture2D>("Graphics\\particle")
   };
   m_leafTextureList = new List<Texture2D>()
   {
    contentManager.Load<Texture2D>("Graphics\\Leaf")
   };
  }

  public void CreateGemEmiter(GameObject gemGameObject)
  {
   Vector2 pos = gemGameObject.Position;
   ParticleEffect2D effect = ParticleEffect2DFactory.Initialize(3000, 1000)
                                                    .SetMaxParticleSpeed(2f)
                                                    .SetEmitAmount(200)
                                                    .AddTexture(m_heartTextureList[0])
                                                    .AddModifier(new AlphaAgeTransform())
                                                    .AddModifier(new ScaleAgeTransform(new Vector2(0.3f, 0.3f), new Vector2(0.8f, 0.8f), new Vector2(1f, 1f)))
                                                    .SetEmissionPattern(new CircleEmissionPattern(50f))
                                                    .Create();
   m_emitterList.Add(new EmitterHolder(effect, pos, TimeSpan.FromSeconds(5), true));
  }

  public void CreateWaterEmiter(ActorGameObject playerGameObject)
  {
   int nLifeSpan = (int)(playerGameObject.FallSpeed * 200);
   int nCount = (int)(playerGameObject.FallSpeed * 50);
   Vector2 pos = playerGameObject.Position + new Vector2(0, (float)playerGameObject.Bounds.Height*0.6f);
   ParticleEffect2D effect = ParticleEffect2DFactory.Initialize(3000, nLifeSpan)
                                                    .SetMaxParticleSpeed(2f)
                                                    .SetEmitAmount(nCount)
                                                    .AddTexture(m_waterTextureList[0])
                                                    .AddModifier(new AlphaAgeTransform())
                                                    .AddModifier(new ScaleAgeTransform(new Vector2(0.02f, 0.02f), new Vector2(0.01f, 0.01f), new Vector2(1f, 1f)))
                                                    .AddModifier(new DirectionalPull(new Vector2(0, -3f)))
                                                    .Create();
   m_emitterList.Add(new EmitterHolder(effect, pos, TimeSpan.FromSeconds(5), true));
  }

  public void CreatePanelGemEmitter(Vector2 pos)
  {
   ParticleEffect2D effect = ParticleEffect2DFactory.Initialize(3000, 500)
                                                    .SetMaxParticleSpeed(2f)
                                                    .SetEmitAmount(100)
                                                    .AddTexture(m_heartTextureList[1])
                                                    .AddModifier(new AlphaAgeTransform())
                                                    .AddModifier(new ScaleAgeTransform(new Vector2(0.1f, 0.1f), new Vector2(0.2f, 0.2f), new Vector2(1f, 1f)))
                                                    .Create();
   m_emitterList.Add(new EmitterHolder(effect, pos, TimeSpan.FromSeconds(5), false));
  }

  public void CreateLeafEmitter(Rectangle viewPort)
  {
   ParticleEffect2D effect = ParticleEffect2DFactory.Initialize(60, 50000)
                                                    .SetMaxParticleSpeed(0.5f)
                                                    .SetEmitAmount(60)
                                                    .AddTexture(m_leafTextureList[0])
                                                    .AddModifier(new AlphaAgeTransform())
                                                    .SetEmissionPattern(new RectangleEmissionPattern(viewPort.Width, viewPort.Height))
                                                    .AddModifier(new RotateAgeTransform(-4f, 4f))
                                                    .AddModifier(new DirectionalPull(new Vector2(0.1f, 0.1f)))
                                                    .Create();
   m_emitterList.Add(new EmitterHolder(effect, new Vector2(viewPort.X, viewPort.Y), TimeSpan.FromSeconds(5), true));
  }

  public void Update(GameTime gameTime)
  {
   List<EmitterHolder> removeEmmitterList = null; ;
   foreach (EmitterHolder emitterHolder in m_emitterList)
   {
    if (emitterHolder.IsNeedEmitt)
    {
     emitterHolder.Effect.Emit( (float)gameTime.TotalGameTime.TotalMilliseconds );
     emitterHolder.Effect.Emit( (float)gameTime.TotalGameTime.TotalMilliseconds );
     emitterHolder.IsNeedEmitt = false;
    }
    emitterHolder.Effect.Update( (float)gameTime.TotalGameTime.TotalMilliseconds, (float)gameTime.ElapsedGameTime.TotalSeconds );
    if (!emitterHolder.Effect.IsActive)
    {
     if (removeEmmitterList == null)
      removeEmmitterList = new List<EmitterHolder>();
     removeEmmitterList.Add(emitterHolder);
    }
   }
   if (removeEmmitterList != null)
   {
    foreach (EmitterHolder emitterHolder in removeEmmitterList)
     m_emitterList.Remove(emitterHolder);
   }
  }

  public void Draw(SpriteBatch spriteBatch, xTile.Dimensions.Rectangle viewPort)
  {
   foreach (EmitterHolder emitterHolder in m_emitterList)
   {
    Vector2 position = emitterHolder.Position;
    if (emitterHolder.IsViewportRelative)
     position -= new Vector2(viewPort.X, viewPort.Y);
    emitterHolder.Effect.Draw(spriteBatch, position);
   }
  }
 }

}
