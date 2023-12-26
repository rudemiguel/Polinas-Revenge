using System;
using System.Collections.Generic;
using FarseerPhysics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Dynamics.Contacts;
using nkast.Aether.Physics2D.Dynamics.Joints;

namespace TheGame
{
 /// <summary>
 /// стадия анимации
 /// </summary>
 class AnimationStage
 {
  private int m_nCurFrame;
  private int m_nBeginFrame;
  private int m_nEndFrame;
  private string m_strName;
  private bool m_bLooped;
  private long m_nFrameIntervalMillisec;
  private long m_nFrameTimeMillisec;

  public AnimationStage(int nBeginFrame, int nEndFrame, String strName)
  {
   m_nBeginFrame = nBeginFrame;
   m_nEndFrame = nEndFrame;
   m_nCurFrame = m_nBeginFrame; 
   m_strName = strName;
   m_nFrameIntervalMillisec = 800;
  }
  
  public String Name
  {
   get
   {
    return m_strName;
   }
  }

  public bool IsLooped
  {
   get
   {
    return m_bLooped;
   }
   set
   {
    m_bLooped = value;
   }
  }

  public int CurFrame
  {
   get
   {
    return m_nCurFrame;
   }
  }

  public bool IsEnd
  {
   get
   {
    return (!m_bLooped) ? m_nCurFrame == m_nEndFrame : false;
   }
  }

  public long FrameInterval
  {
   get
   {
    return m_nFrameIntervalMillisec;
   }
   set
   {
    m_nFrameIntervalMillisec = value;
   }
  }

  public void Begin()
  {
   m_nFrameTimeMillisec = 0;
   m_nCurFrame = m_nBeginFrame;
  }

  public void Next()
  {
   m_nFrameTimeMillisec = 0;
   m_nCurFrame++;
   if (m_nCurFrame>m_nEndFrame)
   {
    if (m_bLooped)
     m_nCurFrame = m_nBeginFrame;
    else
     m_nCurFrame = m_nEndFrame;
   }
  }

  public void Update(long nMillisec)
  {
   m_nFrameTimeMillisec += nMillisec;
   if (m_nFrameTimeMillisec >= m_nFrameIntervalMillisec)
    Next();
  }
 }

 /// <summary>
 /// список стадий анимации
 /// </summary>
 class AnimationStageList
 {
  private xTile.Tiles.TileSheet m_tileSheet;
  private Dictionary<string,AnimationStage> m_animationStageList;
  private AnimationStage m_curAnimationStage;

  public AnimationStageList(xTile.Tiles.TileSheet tileSheet)
  {
   m_tileSheet = tileSheet;
   m_animationStageList = new Dictionary<string,AnimationStage>();
   foreach(var v in tileSheet.Properties)
   {
    String[] s1 = v.Key.Split('_');
    if (s1.Length > 1)
    {
     if (s1[1] == "frames")
     {
      // <начальный кадр>,<конечный кадр>,<зацикленно 1|0>,<интервал мсек>
      String strName = s1[0];
      String val = v.Value.ToString();
      String[] s2 = val.Split(',');
      if (s2.Length != 4)
       throw new Exception("frames property value invalid format");
      int nBeginFrame = Convert.ToInt32(s2[0]) - 1;
      int nEndFrame = Convert.ToInt32(s2[1]) - 1;
      int nLooped = Convert.ToInt32(s2[2]);
      long nFrameInterval = Convert.ToInt64(s2[3]);
      AnimationStage animationStage = new AnimationStage(nBeginFrame, nEndFrame, strName);
      animationStage.FrameInterval = nFrameInterval;
      animationStage.IsLooped = nLooped == 1;
      m_animationStageList.Add(strName, animationStage);
     }
    }
   }
  }

  public AnimationStage AnimationStage
  {
   get
   {
    return m_curAnimationStage;
   }
  }

  public String AnimationStageName
  {
   get
   {
    return (m_curAnimationStage != null) ? m_curAnimationStage.Name : null;
   }
   set
   {
    AnimationStage animationStage = GetAnimationStageByName(value);
    if (animationStage != m_curAnimationStage)
    {
     m_curAnimationStage = GetAnimationStageByName(value);
     if (animationStage == null)
      throw new Exception(String.Format("Animation '{0}' not found", value));
     m_curAnimationStage.Begin();
    }
   }
  }

  public AnimationStage GetAnimationStageByName(String strName)
  {
   AnimationStage v = null;
   m_animationStageList.TryGetValue(strName, out v);
   return v;
  }
 }

 /// <summary>
 /// Игровой объект
 /// </summary>
 abstract class GameObject
 {
  protected World m_world;
  protected xTile.Tiles.TileSheet m_tileSheet;
  protected Texture2D m_tileSheetTexture;
  protected AnimationStageList m_animationStageList;
  protected List<Body> m_bodyList;
  private Dictionary<string, string> m_soundList;
  protected float m_fDensity;

  public GameObject(World world, xTile.Tiles.TileSheet tileSheet, Texture2D tileSheetTexture)
  {
   m_world = world;
   m_tileSheet = tileSheet;
   m_tileSheetTexture = tileSheetTexture;
   m_bodyList = new List<Body>();
   m_soundList = new Dictionary<string, string>();
   m_animationStageList = new AnimationStageList(tileSheet);
   m_fDensity = 0.1f;
   LoadSounds();
  }

  public virtual Vector2 Position
  {
   get;
   set;
  }

  public virtual Rectangle Bounds
  {
   get
   {
    return new Rectangle(0, 0, 0, 0);
   }
  }

  public xTile.Tiles.TileSheet TileSheet
  {
   get
   {
    return m_tileSheet;
   }
  }

  public Texture2D Texture
  {
   get
   {
    return m_tileSheetTexture;
   }
  }

  public string GetSound(string strSoundName)
  {
   string strSoundFile = null;
   m_soundList.TryGetValue(strSoundName, out strSoundFile);
   return strSoundFile;
  }

  public abstract void Update(GameTime gameTime);

  public abstract void Draw(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch, xTile.Dimensions.Rectangle viewPort);

  private void LoadSounds()
  {
   foreach (var v in m_tileSheet.Properties)
   {
    String[] s1 = v.Key.Split('_');
    if (s1.Length > 1)
    {
     if (s1[1] == "sound")
     {
      String strSoundName = s1[0];
      String strSoundFileName = v.Value.ToString();
      m_soundList.Add(strSoundName, strSoundFileName);
     }
    }
   }
  }
 }

 /// <summary>
 /// Объект игрок
 /// </summary>
 class ActorGameObject : GameObject
 {
  private Body m_bodyRect;
  private Body m_bodyCircle;
  private RevoluteJoint m_joint;
  private Vector2 m_posAddition;
  private float m_fCircleRadius;
  private float m_fRectangleWidth;
  private float m_fRectangleHeight;
  private long m_nFireTimeoutMilliseconds;
  private bool m_bPlatfromEdge;
  private bool m_bGround;
  private bool m_bLanded;
  private bool m_bDirection;
  private bool m_bAllowFire;
  private bool m_bInWater;
  private bool m_bEnterWater;
  protected float m_fSpeed;
  private Rectangle m_bounds;
  protected WaterManager m_waterManager;
  
  public ActorGameObject(World world, xTile.Tiles.TileSheet tileSheet, Texture2D tileSheetTexture):
   base(world, tileSheet, tileSheetTexture)
  {
   m_bDirection = true;
   m_bAllowFire = true;
   m_bInWater = false;
   m_bEnterWater = false;
   m_waterManager = null;
   m_nFireTimeoutMilliseconds = 0;
   m_fSpeed = 15f;
   m_fDensity = 0.1f;
   CreateBody();
   m_bounds = new Rectangle(0, 0, (int)Math.Max(m_fRectangleWidth, m_fCircleRadius * 2), (int)(m_fCircleRadius + Math.Max(m_fCircleRadius, m_fRectangleHeight)));
   m_animationStageList.AnimationStageName = "idle";
  }

  public bool IsOnGround
  {
   get
   {
    return m_bGround;
   }
  }

  public bool PlatformEdge
  {
   get
   {
    return m_bPlatfromEdge;
   }
  }

  public bool IsLanded
  {
   get
   {
    return m_bLanded;
   }
  }

  public bool Direction
  {
   get
   {
    return m_bDirection;
   }
   set
   {
    m_bDirection = value;
   }
  }

  public bool IsInWater
  {
   get
   {
    return m_bInWater;
   }
  }

  public bool IsEnterWater
  {
   get
   {
    return m_bEnterWater;
   }
  }

  public float WalkSpeed
  {
   get
   {
    return Math.Abs(m_bodyRect.LinearVelocity.X);
   }
  }

  public float FallSpeed
  {
   get
   {
    return m_bodyRect.LinearVelocity.Y > 0 ? Math.Abs(m_bodyRect.LinearVelocity.Y) : 0;
   }
  }

  public override Rectangle Bounds
  {
   get
   {
    return m_bounds;
   }
  }

  public void ApplyLeft()
  {
   if (IsOnGround)
    m_joint.MotorSpeed = -m_fSpeed;
   else
   {
    ClearMotion();
    m_bodyRect.ApplyLinearImpulse(new Vector2(-0.0003f*m_fDensity*m_fSpeed, 0));
   }
   m_bDirection = false;
  }

  public void ApplyRight()
  {
   if (IsOnGround)
    m_joint.MotorSpeed = m_fSpeed;
   else
   {
    ClearMotion();
    m_bodyRect.ApplyLinearImpulse(new Vector2(0.0003f*m_fDensity*m_fSpeed, 0));
   }
   m_bDirection = true;
  }

  public void ClearMotion()
  {
   m_joint.MotorSpeed = 0f;
  }

  public bool Jump()
  {
   bool bAllowJump = IsOnGround && (Math.Abs(m_bodyRect.LinearVelocity.Y) < 0.01f);
   if (bAllowJump)
   {
    Vector2 v = new Vector2(m_bodyCircle.LinearVelocity.X*-0.02f, (m_bodyCircle.Mass + m_bodyCircle.Mass) * -7f);
    m_bodyCircle.ApplyLinearImpulse(ref v);
   }
   return bAllowJump;
  }

  public bool Fire()
  {
   if (m_bAllowFire)
   {
    m_nFireTimeoutMilliseconds = 1000;
    m_bAllowFire = false;
    return true;
   }
   return false;
  }

  public WaterManager WaterManager
  {
   get
   {
    return m_waterManager;
   }
   set
   {
    m_waterManager = value;
   }
  }

  private void CreateBody()
  {
   m_fCircleRadius = m_tileSheet.TileWidth / 2f;
   m_fRectangleWidth = m_tileSheet.TileWidth * 0.8f;
   m_fRectangleHeight = m_tileSheet.TileHeight - m_fCircleRadius;   
   // создаем тело
   m_bodyRect = m_world.CreateRectangle((float)ConvertUnits.ToSimUnits(m_fRectangleWidth), (float)ConvertUnits.ToSimUnits(m_fRectangleHeight), m_fDensity);
   m_bodyRect.BodyType = BodyType.Dynamic;
   m_bodyRect.SetRestitution(0f);
   m_bodyRect.SetFriction(0.5f);
   m_bodyRect.FixedRotation = true;
   m_bodyRect.Tag = this;
   m_bodyRect.SetCollisionCategories(Category.Cat3);
   m_bodyCircle = m_world.CreateCircle(ConvertUnits.ToSimUnits(m_fCircleRadius), m_fDensity);
   m_bodyCircle.BodyType = BodyType.Dynamic;
   m_bodyCircle.SetRestitution(0f);
   m_bodyCircle.SetFriction(0.8f);
   m_bodyCircle.Tag = this;
   m_bodyRect.SetCollisionCategories(Category.Cat4);
   m_bodyCircle.Position = ConvertUnits.ToSimUnits(0, m_fRectangleHeight/2);
   m_joint = JointFactory.CreateRevoluteJoint(m_world, m_bodyRect, m_bodyCircle, Vector2.Zero);
   m_joint.MotorEnabled = true;
   m_joint.MaxMotorTorque = 0.1f;
   m_bodyList.Add(m_bodyCircle);
   m_bodyList.Add(m_bodyRect);
   // Добавка к положению
   m_posAddition = ConvertUnits.ToSimUnits(0, m_tileSheet.TileHeight / 2 - m_fRectangleHeight / 2);
  }

  private void CalcOnGround()
  {
   bool bGround = false;
   Func<Fixture, Vector2, Vector2, float, float> callback = (f, v1, v2, f1) =>
   {
     bGround = true;
     return 1.0f;
   };
   float fCircleRadius = ConvertUnits.ToSimUnits(m_fCircleRadius-1);
   Vector2 pos = m_bodyCircle.Position;
   Vector2[] spList = new Vector2[] { new Vector2(pos.X - fCircleRadius, pos.Y), new Vector2(pos.X, pos.Y), new Vector2(pos.X + fCircleRadius, pos.Y) };
   Vector2 epAddition = ConvertUnits.ToSimUnits(0, 5 + m_fCircleRadius);
   foreach (Vector2 sp in spList)
   {
    if (!bGround)
     m_world.RayCast(new(callback), sp, sp + epAddition);
   }
   m_bGround = bGround;
  }

  private void CalcPlatformEdge()
  {
   bool bGround = false;
   Func<Fixture, Vector2, Vector2, float, float> callback = (f, v1, v2, f1) =>
   {
    if (f.Body.Tag is string)
    {
     if ((f.Body.Tag as string) == "ground")
     {
      bGround = true;
      return 1.0f;
     }
    }
    return f1;
   };
   Vector2 p = m_bodyCircle.Position;
   Vector2 p1 = ConvertUnits.ToSimUnits(m_fCircleRadius/2 * ((m_bDirection) ? 1 : -1), m_fCircleRadius + 10);
   m_world.RayCast(new(callback), p, p+p1 );
   m_bPlatfromEdge = !bGround;
  }

  private void CalcWater()
  {
   if (m_waterManager != null)
   {
    bool bInWater = m_bInWater;
    m_bInWater = m_waterManager.Check(this);
    m_bEnterWater = !bInWater && m_bInWater;
   }
  }

  protected virtual void CalcAnimation(GameTime gameTime)
  {
   // Смена кадров при ходьбе
   if (Math.Abs(m_bodyCircle.AngularVelocity) > 0.000001)
   {
    m_animationStageList.AnimationStageName = "walk";
    m_animationStageList.AnimationStage.Update((long)(WalkSpeed * 5f * (float)gameTime.ElapsedGameTime.TotalMilliseconds));
   }
   else
   {
    m_animationStageList.AnimationStageName = "idle";
   }
  }

  public override Vector2 Position
  {
   get
   {
    return ConvertUnits.ToDisplayUnits(m_bodyRect.Position + m_posAddition);
   }
   set
   {
    m_bodyRect.Position = ConvertUnits.ToSimUnits(value) - m_posAddition;
    m_bodyCircle.Position = m_bodyRect.Position + ConvertUnits.ToSimUnits(0, m_fRectangleHeight / 2);
   }
  }

  public override void Update(GameTime gameTime)
  {
   bool bPrevIsOnGround = IsOnGround;
   // Вычисляем конец платформы
   CalcPlatformEdge();
   // Вычисляем на земле или в воздухе
   CalcOnGround();
   // Вычисляем воду
   CalcWater();
   // Вычисляем анимацию
   CalcAnimation(gameTime);
   // Вычисляем приземлился ли
   m_bLanded = (!bPrevIsOnGround && IsOnGround) && (Math.Abs(m_bodyRect.LinearVelocity.Y) > 2);
   // Разрешение на стрельбу
   if (!m_bAllowFire)
   {
    m_nFireTimeoutMilliseconds -= (long)gameTime.ElapsedGameTime.TotalMilliseconds;
    if (m_nFireTimeoutMilliseconds <= 0)
     m_bAllowFire = true;
   }
  }

  public override void Draw(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch, xTile.Dimensions.Rectangle viewPort)
  {
   Vector2 screenPos = Position - new Vector2(viewPort.X, viewPort.Y);
   Vector2 originPos = new Vector2(m_tileSheet.TileWidth / 2, m_tileSheet.TileHeight / 2);
   xTile.Dimensions.Rectangle r = m_tileSheet.GetTileImageBounds( m_animationStageList.AnimationStage.CurFrame );
   Rectangle sourceRectangle = new Rectangle(r.X, r.Y, r.Width, r.Height);
   SpriteEffects spriteEffects = (m_bDirection) ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
   spriteBatch.Draw(m_tileSheetTexture, screenPos, sourceRectangle, Color.White, m_bodyRect.Rotation, originPos, 1f, spriteEffects, 0);   
  }
 }

 /// <summary>
 /// Игровой объект кристал
 /// </summary>
 class GemGameObject : GameObject
 {
  private Body m_bodyRect;
  private float m_fRectangleWidth;
  private float m_fRectangleHeight;
  private bool m_bPickedUp;
  private Rectangle m_bounds;

  public GemGameObject(World world, xTile.Tiles.TileSheet tileSheet, Texture2D texture)
   : base(world, tileSheet, texture)
  {
   m_bPickedUp = false;
   CreateBody();
   m_bounds = new Rectangle(0, 0, (int)m_fRectangleWidth, (int)m_fRectangleHeight);
   m_animationStageList.AnimationStageName = "idle";
  }

  private void CreateBody()
  {
   m_fRectangleWidth = m_tileSheet.TileWidth;
   m_fRectangleHeight = m_tileSheet.TileHeight;
   m_bodyRect = m_world.CreateRectangle((float)ConvertUnits.ToSimUnits(m_fRectangleWidth), (float)ConvertUnits.ToSimUnits(m_fRectangleHeight), m_fDensity);
   m_bodyRect.BodyType = BodyType.Dynamic;
   m_bodyRect.SetFriction(0.5f);
   m_bodyRect.FixedRotation = true;
   m_bodyRect.Tag = this;
   m_bodyList.Add(m_bodyRect);
  }

  public override Vector2 Position
  {
   get
   {
    return ConvertUnits.ToDisplayUnits(m_bodyRect.Position);
   }
   set
   {
    m_bodyRect.Position = ConvertUnits.ToSimUnits(value);
   }
  }

  public bool PickedUp
  {
   get
   {
    return m_bPickedUp;
   }
  }

  public override Rectangle Bounds
  {
   get
   {
    return m_bounds;
   }
  }

  public void Push(Vector2 v)
  {
   m_bodyRect.ApplyLinearImpulse(v);
  }

  public override void Update(GameTime gameTime)
  {
   // Анимация
   m_animationStageList.AnimationStage.Update((long)gameTime.ElapsedGameTime.TotalMilliseconds);
   // Подобрали ли кристал
   for (ContactEdge edge = m_bodyRect.ContactList; edge != null; edge = edge.Next)
   {
    if (edge.Contact.FixtureB.Body.Tag is PlayerGameObject)
    {
     m_bPickedUp = true;
    }
   }
   // Левитация
   Levitate();
  }

  public override void Draw(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch, xTile.Dimensions.Rectangle viewPort)
  {
   Vector2 screenPos = Position - new Vector2(viewPort.X, viewPort.Y);
   Vector2 originPos = new Vector2(m_tileSheet.TileWidth / 2, m_tileSheet.TileHeight / 2);
   xTile.Dimensions.Rectangle r = m_tileSheet.GetTileImageBounds(m_animationStageList.AnimationStage.CurFrame);
   var sourceRectangle = new Microsoft.Xna.Framework.Rectangle(r.X, r.Y, r.Width, r.Height);
   spriteBatch.Draw(m_tileSheetTexture, screenPos, sourceRectangle, Color.White, m_bodyRect.Rotation, originPos, 1f, SpriteEffects.None, 0);  
  }

  private void Levitate()
  {
   Vector2 p1 = m_bodyRect.Position;
   Vector2 p2 = p1 + ConvertUnits.ToSimUnits(0, m_fRectangleHeight);
   float fSelfHeight = ConvertUnits.ToSimUnits( m_fRectangleHeight/2 );
   float fDistance = 100f;
   Func<Fixture, Vector2, Vector2, float, float> callback = (f, v1, v2, f1) =>
   {
    if (f.Body.Tag is string)
    {
     if ((f.Body.Tag as string) == "ground")
     {
      float fLocalDistance = Vector2.Distance(v1, p1);
      if (fLocalDistance < fDistance)
       fDistance = fLocalDistance;       
     }
    }
    return 1f;
   };
   m_world.RayCast(new(callback), p1, p2);
   float fMaxDistance = Vector2.Distance(p1, p2);
   if (fDistance > fMaxDistance)
    fDistance = fMaxDistance;
   fDistance -= fSelfHeight;
   fMaxDistance -= fSelfHeight;
   float fDistanceRatio = fDistance / fMaxDistance;
   m_bodyRect.ApplyForce(new Vector2(0, (1f/fDistanceRatio) * -0.08f), m_bodyRect.WorldCenter);
  }
 }

 /// <summary>
 /// Пуля
 /// </summary>
 class BulletGameObject : GameObject
 {
  private Body m_body;
  private float m_fCircleRadius;
  private long m_nTTLMilliseconds;
  private bool m_bNeedRemove;
  private bool m_bBlowFlagRead;
  
  public BulletGameObject(World world, ActorGameObject playerGameObject, xTile.Tiles.TileSheet tileSheet, Texture2D texture) :
   base(world, tileSheet, texture)
  {
   CreateBody();
   m_nTTLMilliseconds = 1000;
   m_bNeedRemove = false;
   m_bBlowFlagRead = false;
   Position = playerGameObject.Position + new Vector2(playerGameObject.Bounds.Width / 2 * ((playerGameObject.Direction) ? 1f : -1f) ,0f);
   Vector2 imp = new Vector2( 0.008f * ((playerGameObject.Direction) ? 1f : -1f), -0.001f);
   m_body.ApplyLinearImpulse(ref imp);
  }

  public bool NeedRemove
  {
   get
   {
    return m_bNeedRemove;
   }
  }

  public bool IsBlow
  {
   get
   {
    if (m_animationStageList.AnimationStageName=="blow" && !m_bBlowFlagRead)
    {
     m_bBlowFlagRead = true;
     return true;
    }
    return false;
   }
  }

  private void CreateBody()
  {
   m_fCircleRadius = m_tileSheet.TileWidth / 2f;
   m_body = m_world.CreateCircle(ConvertUnits.ToSimUnits(m_fCircleRadius), 0.1f);
   m_body.BodyType = BodyType.Dynamic;
   m_body.SetRestitution(0f);
   m_body.Tag = this;
   m_body.SetCollisionCategories(Category.Cat2);
   m_bodyList.Add(m_body);
  }

  public override Vector2 Position
  {
   get
   {
    return ConvertUnits.ToDisplayUnits(m_body.Position);
   }
   set
   {
    m_body.Position = ConvertUnits.ToSimUnits(value);
   }
  }

  public override void Update(GameTime gameTime)
  {
   m_nTTLMilliseconds -= (int)gameTime.ElapsedGameTime.TotalMilliseconds;
   // Анимация расстворения
   if (m_animationStageList.AnimationStageName == "blow")
   {
    if (m_animationStageList.AnimationStage.IsEnd)
     m_bNeedRemove = true;
   }
   // Определяем начало фазы растворения
   else
   {
    if (m_nTTLMilliseconds <= 0)
    {
     m_nTTLMilliseconds = 0;
     m_animationStageList.AnimationStageName = "blow";
    }
    else
     m_animationStageList.AnimationStageName = "run";
   }
   // Попадание в врага   
   for (ContactEdge edge = m_body.ContactList; edge != null; edge = edge.Next)
   {
    if (edge.Contact.FixtureA.Body.Tag is EnemyGameObject)
    {
     EnemyGameObject enemyGameObject = edge.Contact.FixtureA.Body.Tag as EnemyGameObject;
     // Начинаем расстворение врага
     enemyGameObject.Blow();
     // Начинаем анимацию растворения
     m_animationStageList.AnimationStageName = "blow";
     break;
    }
   }
   // Анимация
   m_animationStageList.AnimationStage.Update((long)gameTime.ElapsedGameTime.TotalMilliseconds);
  }

  public override void Draw(SpriteBatch spriteBatch, xTile.Dimensions.Rectangle viewPort)
  {
   Vector2 screenPos = Position - new Vector2(viewPort.X, viewPort.Y);
   Vector2 originPos = new Vector2(m_tileSheet.TileWidth / 2, m_tileSheet.TileHeight / 2);
   xTile.Dimensions.Rectangle r = m_tileSheet.GetTileImageBounds( m_animationStageList.AnimationStage.CurFrame );
   Rectangle sourceRectangle = new Rectangle(r.X, r.Y, r.Width, r.Height);
   spriteBatch.Draw(m_tileSheetTexture, screenPos, sourceRectangle, Color.White, m_body.Rotation, originPos, 1f, SpriteEffects.None, 0);   
  }
 }

 /// <summary>
 /// Игрок
 /// </summary>
 class PlayerGameObject : ActorGameObject
 {
  public PlayerGameObject(World wordld, xTile.Tiles.TileSheet tileSheet, Texture2D texture) :
   base(wordld, tileSheet, texture)
  {
   m_fDensity = 0.5f;
   m_fSpeed = 7f;
  }

  public override void Update(GameTime gameTime)
  {
   base.Update(gameTime);
  }
 }
 
 /// <summary>
 /// Враг
 /// </summary>
 class EnemyGameObject : ActorGameObject
 {
  private bool m_bNeedRemove;
  private long m_nWalkTime;
  private bool m_bPlayerInSight;
  private bool m_bAllowDropGem;
  private bool m_bDropGem;
  private bool m_bDropGemFlagRead;
  private long m_nDropGemDelay;
  private long m_nDropGemRemainMilliseconds;
  private long m_nBlowPendingDelay;
  private long m_nBlowPendingRemainMilliseconds;
  private bool m_bBlowPending;
  private bool m_bBlowFlagRead;
  private long m_nVelocityStuckMilliseconds;

  private static readonly Random Random = new Random();

  public EnemyGameObject(World wordld, xTile.Tiles.TileSheet tileSheet, Texture2D texture) :
   base(wordld, tileSheet, texture)
  {
   m_bBlowFlagRead = false;
   m_bNeedRemove = false;
   m_bAllowDropGem = false;
   m_bDropGem = false;
   m_bDropGemFlagRead = false;
   m_bPlayerInSight = false;
   m_fSpeed = 1;
   m_fDensity = 0.1f;
   m_nWalkTime = 0;
   m_nDropGemDelay = 2000;
   m_nDropGemRemainMilliseconds = m_nDropGemDelay;
   m_bBlowPending = false;
   m_nBlowPendingDelay = 50;
   m_nBlowPendingRemainMilliseconds = m_nBlowPendingDelay;
   m_nVelocityStuckMilliseconds = 0;
   // Свойства
   foreach (var v in tileSheet.Properties)
   {
    if (v.Key == "is_drop_gem")
     m_bAllowDropGem = Convert.ToInt32(v.Value.ToString()) == 1;
   }
  }

  public void Blow()
  {
   // Начинаем отсчет до анимации расттворени
   m_bBlowPending = true;
   // Враг соприкасается только с полом
   foreach (Body body in m_bodyList)
    body.SetCollidesWith(Category.Cat1);
  }

  public bool NeedRemove
  {
   get
   {
    return m_bNeedRemove;
   }
  }

  public bool IsBlow
  {
   get
   {
    if (m_animationStageList.AnimationStageName == "blow" && !m_bBlowFlagRead)
    {
     m_bBlowFlagRead = true;
     return true;
    }
    return false;
   }
  }

  public bool IsPlayerInSight
  {
   get
   {
    return m_bPlayerInSight;
   }
  }

  public bool IsDropGem
  {
   get
   {
    if (m_bDropGem && !m_bDropGemFlagRead)
    {
     m_bDropGemFlagRead = true;
     return true;
    }
    return false;
   }
  }

  public bool IsAllowDropGem
  {
   get
   {
    return m_bAllowDropGem;
   }
  }

  public void DropGem(GemGameObject gemGameObject)
  {
   var bound = Bounds;
   var gemBound = gemGameObject.Bounds;
   var pos = Position;
   gemGameObject.Position = new Vector2(pos.X + (gemBound.Width/2*(Direction ? -1 : 1)), pos.Y - bound.Height / 2 - gemBound.Height / 2);
   gemGameObject.Push(new Vector2(0.008f, -0.015f));
  }

  private void CalcPlayerInSight()
  {
   bool bPlayerInSight = false;
   Func<Fixture, Vector2, Vector2, float, float> callback = (f, v1, v2, f1) =>
   {
    if (f.Body.Tag is PlayerGameObject)
    {
     bPlayerInSight = true;
     return 1.0f;
    }
    return f1;
   };
   Vector2 p = ConvertUnits.ToSimUnits(Position);
   Vector2 p1 = ConvertUnits.ToSimUnits((Bounds.Width / 2 + 10) * ((Direction) ? 1 : -1), 0);
   m_world.RayCast( new(callback), p, p + p1);
   m_bPlayerInSight = bPlayerInSight;
  }

  protected override void CalcAnimation(GameTime gameTime)
  {
   // Анимация
   if (m_animationStageList.AnimationStageName == "blow")
   {
    if (m_animationStageList.AnimationStage.IsEnd)
     m_bNeedRemove = true;
   }
   else
   {
    if (m_bPlayerInSight && !m_bBlowPending)
     m_animationStageList.AnimationStageName = "attack";
    else
     m_animationStageList.AnimationStageName = "walk";
   }
   if (m_animationStageList.AnimationStageName == "walk")
    m_animationStageList.AnimationStage.Update((long)(WalkSpeed * 5f * (float)gameTime.ElapsedGameTime.TotalMilliseconds));
   else
    m_animationStageList.AnimationStage.Update((long)gameTime.ElapsedGameTime.TotalMilliseconds);
  }

  public override void Update(GameTime gameTime)
  {
   base.Update(gameTime);
   // Игрок в поле зрения
   CalcPlayerInSight();
   // Дроп кристала
   if (m_bPlayerInSight)
   {
    m_nDropGemRemainMilliseconds -= (long)gameTime.ElapsedGameTime.TotalMilliseconds;
    if (m_nDropGemRemainMilliseconds <= 0)
    {
     m_bDropGem = m_bAllowDropGem;
     m_bAllowDropGem = false;
    }
   }
   else
   {
    m_nDropGemRemainMilliseconds = m_nDropGemDelay;
   }
   // Смена направления
   if (PlatformEdge)
    m_nWalkTime = 0;
   m_nWalkTime -= (long)gameTime.ElapsedGameTime.TotalMilliseconds;
   if (m_nWalkTime <= 0 && !IsPlayerInSight)
   {
    m_nWalkTime = (long)(Random.NextDouble() * 10000.0) + 5000;
    Direction = !Direction;
   }
   // Передвижение
   if (m_animationStageList.AnimationStageName != "blow")
   {
    if (Direction)
     ApplyRight();
    else
     ApplyLeft();
   }
   else
    ClearMotion();
   // Если застрял
   float fLinearVecity = m_bodyList[0].LinearVelocity.X;
   if (Math.Abs(fLinearVecity) < 0.2)
    m_nVelocityStuckMilliseconds += (long)gameTime.ElapsedGameTime.TotalMilliseconds;
   else
    m_nVelocityStuckMilliseconds = 0;
   if (m_nVelocityStuckMilliseconds > 300 && !IsPlayerInSight)
   {
    m_nWalkTime = 0;
    m_nVelocityStuckMilliseconds = 0;
   }
   // Пауза перед взрывом
   if (m_bBlowPending)
   {
    m_nBlowPendingRemainMilliseconds -= (long)gameTime.ElapsedGameTime.TotalMilliseconds;
    if (m_nBlowPendingRemainMilliseconds <= 0)
    {
     m_animationStageList.AnimationStageName = "blow";
    }
   }
  }
 }
}
