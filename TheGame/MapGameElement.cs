using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using xTile;
using xTile.Dimensions;
using xTile.Display;
using xTile.Tiles;
using xTile.Layers;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Common;
using nkast.Aether.Physics2D.Common.Decomposition;
using FarseerPhysics;

namespace TheGame
{
 /// <summary>
 /// Карта
 /// </summary>
 class MapGameElement : DrawableGameComponent 
 {
  private static readonly Random Random = new Random();
  // map and viewport
  private XnaDisplayDevice m_xnaDisplayDevice;
  private Map m_map;
  private xTile.Dimensions.Rectangle m_viewPort;
  // Панель
  private PanelManager m_panelManager;
  // game objects
  private List<GameObject> m_gameObjectList;
  private List<xTile.Tiles.TileSheet> m_gemTileSheetList;
  // physics
  private World m_world;
  // sound
  private SoundManager m_soundManager;
  private MusicManager m_musicManager;
  // water
  private WaterManager m_waterManager;
  // waypoints
  private WaypointManager m_waypointManager;
  // particle
  private ParticleEmitterManager m_particleEmitterManager;
  //
  private bool m_bMapEnded;
  private bool m_bMapEndedFlagRead;
  //
  private long m_leafRemainMilliseconds;
  
  public MapGameElement(Game game, SoundManager soundManager, MusicManager musicManager) 
   : base (game)
  {
   m_bMapEnded = false;
   m_bMapEndedFlagRead = false;
   m_leafRemainMilliseconds = 30000;
   m_soundManager = soundManager;
   m_musicManager = musicManager;
   m_gameObjectList = new List<GameObject>();
   m_world = new World(new Vector2(0f, 10.0f));
   m_particleEmitterManager = new ParticleEmitterManager();
   m_panelManager = new PanelManager();
   m_waypointManager = new WaypointManager();
  }

  /// <summary>
  /// Вышел ли игрок
  /// </summary>  
  public bool IsMapEnded
  {
   get
   {
    bool bIsMapEnded = m_bMapEnded && !m_bMapEndedFlagRead;
    if (bIsMapEnded)
     m_bMapEndedFlagRead = true;
    return bIsMapEnded;
   }
  }

  /// <summary>
  /// Менеджер панели
  /// </summary>
  public PanelManager PanelManager
  {
   get
   {
    return m_panelManager;
   }
  }

  #region DrawableGameComponent Members

  /// <summary>
  /// Загрузка контента
  /// </summary>
  /// <param name="content"></param>
  protected override void LoadContent()
  {
   var content = Game.Content;
   content.RootDirectory = ".";
   GraphicsDevice graphicsDevice = Game.GraphicsDevice;
   m_viewPort = new xTile.Dimensions.Rectangle(0, 0, graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height);
   // Create a new SpriteBatch, which can be used to draw textures.
   m_xnaDisplayDevice = new XnaDisplayDevice(content, graphicsDevice);   
   // панель
   m_panelManager.LoadContent(graphicsDevice, content);
   // Частици
   m_particleEmitterManager.LoadContent(content);
   // load map from content pipeline and initialise it
   m_map = Game.Content.Load<Map>("Maps\\Map01");
   m_map.LoadTileSheets(m_xnaDisplayDevice);
   // Вода
   Layer waterLayer = m_map.GetLayer("water");
   m_waterManager = new WaterManager(waterLayer);
   // Пол
   Layer foregroundLayer = m_map.GetLayer("foreground");
   Layer groundLayer = m_map.GetLayer("ground");
   uint[] data = new uint[foregroundLayer.TileWidth * foregroundLayer.TileHeight];
   Dictionary<int, Vertices> tileVertices = new Dictionary<int, Vertices>();
   for (int x = 0; x < groundLayer.LayerWidth; x++)
   {
    for (int y = 0; y < groundLayer.LayerHeight; y++)
    {
     Tile tile = groundLayer.Tiles[x, y];
     Tile foregroundTile = foregroundLayer.Tiles[x, y];
     if (tile != null && foregroundTile != null)
     {      
      Vertices vertices = null;
      tileVertices.TryGetValue(foregroundTile.TileIndex, out vertices);
      if (vertices == null)
      {
       xTile.Dimensions.Rectangle r = foregroundTile.TileSheet.GetTileImageBounds(foregroundTile.TileIndex);
       var tileSheetTexture = m_xnaDisplayDevice.GetTileSheetTexture(tile.TileSheet);
       Microsoft.Xna.Framework.Rectangle rect = new Microsoft.Xna.Framework.Rectangle(r.X, r.Y, r.Width, r.Height);
       tileSheetTexture.GetData<uint>( 0, rect, data, 0, rect.Width*rect.Height );
       vertices = PolygonTools.CreatePolygon(data, foregroundLayer.TileWidth, true);
       var scaleVector = ConvertUnits.ToSimUnits(1f, 1f);
       vertices.Scale(ref scaleVector);
       tileVertices.Add(foregroundTile.TileIndex, vertices);
      }
      List<Vertices> _list = Triangulate.ConvexPartition(vertices, TriangulationAlgorithm.Bayazit);
      Body body = m_world.CreateCompoundPolygon(_list, 1);
      body.Tag = "ground";
      body.BodyType = BodyType.Static;
      body.SetFriction(2f);
      body.SetCollidesWith(Category.All);
      body.SetCollisionCategories(Category.Cat1);
      body.Position = ConvertUnits.ToSimUnits((float)(x * foregroundLayer.TileWidth),
                                               (float)(y * foregroundLayer.TileHeight));
      groundLayer.Tiles[x, y] = null;
     }
    }
   }
   var leftWall = m_world.CreateRectangle((float)ConvertUnits.ToSimUnits(10), (float)ConvertUnits.ToSimUnits(foregroundLayer.DisplayHeight), 0.1f);
   leftWall.Position = ConvertUnits.ToSimUnits(-5, foregroundLayer.DisplayHeight / 2);
   leftWall.BodyType = BodyType.Static;
   var rightWall = m_world.CreateRectangle((float)ConvertUnits.ToSimUnits(10), (float)ConvertUnits.ToSimUnits(foregroundLayer.DisplayHeight), 0.1f);
   rightWall.Position = ConvertUnits.ToSimUnits(foregroundLayer.DisplayWidth + 5, foregroundLayer.DisplayHeight / 2);
   rightWall.BodyType = BodyType.Static;
   // актеры
   var enemiesGameObjectList = new List<GameObject>();
   var enemiesLayer = m_map.GetLayer("actors");
   for (int x = 0; x < enemiesLayer.LayerWidth; x++)
   {
    for (int y = 0; y < enemiesLayer.LayerHeight; y++)
    {
     var tile = enemiesLayer.Tiles[x, y];
     if (tile != null)
     {
      var pos = new Vector2((x + 1) * enemiesLayer.TileWidth, (y + 1) * enemiesLayer.TileHeight);
      String strTileSheetName = "";
      xTile.ObjectModel.PropertyValue t = null;
      tile.Properties.TryGetValue("type", out t);
      if (t != null)
       strTileSheetName = t.ToString();
      var tileSheet = m_map.GetTileSheet(strTileSheetName);
      if (tileSheet == null)
       throw new Exception(string.Format("Tile sheet not found by name: {0}", strTileSheetName));
      Texture2D texture = m_xnaDisplayDevice.GetTileSheetTexture(tileSheet);
      ActorGameObject actorGameObject = null;
      if (strTileSheetName == "player")
      {
       actorGameObject = new PlayerGameObject(m_world, tileSheet, texture);
       m_gameObjectList.Add(actorGameObject);
      }
      else
      {
       actorGameObject = new EnemyGameObject(m_world, tileSheet, texture);
       // Добавляем в панель место для кристала если враг бросает
       if ((actorGameObject as EnemyGameObject).IsAllowDropGem)
        m_panelManager.GemCount++;
       enemiesGameObjectList.Add(actorGameObject);
      }
      actorGameObject.Position = pos - new Vector2(tileSheet.TileWidth / 2, tileSheet.TileHeight / 2);
      actorGameObject.WaterManager = m_waterManager;
      enemiesLayer.Tiles[x, y] = null;
     }
    }
   }
   m_gameObjectList.AddRange(enemiesGameObjectList);
   // Кристаллы
   m_gemTileSheetList = new List<TileSheet>();
   Layer gemsLayer = m_map.GetLayer("gems");
   for (int x = 0; x < gemsLayer.LayerWidth; x++)
   {
    for (int y = 0; y < gemsLayer.LayerHeight; y++)
    {
     Tile tile = gemsLayer.Tiles[x, y];
     if (tile != null)
     {
      Vector2 pos = new Vector2(x * gemsLayer.TileWidth + gemsLayer.TileWidth / 2, (y * gemsLayer.TileHeight + gemsLayer.TileHeight / 2)-50);
      String strTileSheetName = "";
      xTile.ObjectModel.PropertyValue t = null;
      tile.Properties.TryGetValue("type", out t);
      if (t != null)
       strTileSheetName = t.ToString();
      xTile.Tiles.TileSheet tileSheet = m_map.GetTileSheet(strTileSheetName);
      if (tileSheet == null)
       throw new Exception(string.Format("Tile sheet not found by name: {0}", strTileSheetName));
      m_gemTileSheetList.Add(tileSheet);
      Texture2D texture = m_xnaDisplayDevice.GetTileSheetTexture(tileSheet);
      GemGameObject gem = new GemGameObject(m_world, tileSheet, texture);
      gem.Position = pos;
      m_gameObjectList.Add(gem);
      gemsLayer.Tiles[x, y] = null;
      m_panelManager.GemCount++;
     }
    }
   }
   // метки
   Layer waypointsLayer = m_map.GetLayer("waypoints");
   if (waypointsLayer != null)
   {
    for (int x = 0; x < waypointsLayer.LayerWidth; x++)
    {
     for (int y = 0; y < waypointsLayer.LayerHeight; y++)
     {
      Tile tile = waypointsLayer.Tiles[x, y];
      if (tile != null)
      {
       Vector2 pos = new Vector2(x * waypointsLayer.TileWidth + waypointsLayer.TileWidth / 2, y * waypointsLayer.TileHeight + waypointsLayer.TileHeight / 2);
       String strName = "";
       xTile.ObjectModel.PropertyValue v = null;
       tile.Properties.TryGetValue("name", out v);
       if (v != null)
        strName = v.ToString();
       m_waypointManager.AddWaypoint(strName, pos);
       waypointsLayer.Tiles[x, y] = null;
      }
     }
    }
   }
   // Событие отрисовки переднего слоя карты
   foregroundLayer.AfterDraw += new LayerEventHandler(foregroundLayer_AfterDraw);
   waterLayer.AfterDraw += new LayerEventHandler(waterLayer_AfterDraw);   
  }

  /// <summary>
  /// Выгружаем контент
  /// </summary>
  protected override void UnloadContent()
  {
   m_map.DisposeTileSheets(m_xnaDisplayDevice);
   m_map = null;
  }

  /// <summary>
  /// Отрисовка
  /// </summary>
  /// <param name="gameTime"></param>
  public override void Draw(Microsoft.Xna.Framework.GameTime gameTime)
  {  
   // карта
   m_map.Draw(m_xnaDisplayDevice, m_viewPort, Location.Origin, true);
   // debug view
   var projection = Matrix.CreateOrthographicOffCenter(
             ConvertUnits.ToSimUnits(m_viewPort.X),
             ConvertUnits.ToSimUnits(m_viewPort.X + m_viewPort.Width),
             ConvertUnits.ToSimUnits(m_viewPort.Y + m_viewPort.Height),
             ConvertUnits.ToSimUnits(m_viewPort.Y),
             0f,
             1f);
  }

  /// <summary>
  /// Обновление
  /// </summary>
  /// <param name="gameTime"></param>
  public override void Update(Microsoft.Xna.Framework.GameTime gameTime)
  {
   bool bJump = false;
   bool bFire = false;
   // get keyboard and gamepad states  
   var keyboardState = Keyboard.GetState(PlayerIndex.One);
   Nullable<GamePadState> gamePadState = null;
   if (GamePad.GetState(PlayerIndex.One).IsConnected)
    gamePadState = GamePad.GetState(PlayerIndex.One);
   // movement via keyboard
   ActorGameObject playerObject = m_gameObjectList[0] as ActorGameObject;
   if (keyboardState.IsKeyDown(Keys.Left))
    playerObject.ApplyLeft();
   else if (keyboardState.IsKeyDown(Keys.Right))
    playerObject.ApplyRight();
   else
    playerObject.ClearMotion();
   if (keyboardState.IsKeyDown(Keys.Space))
    bJump = true;
   if (keyboardState.IsKeyDown(Keys.RightShift))
    bFire = true;
   // joystic movement
   if (gamePadState != null)
   {
    if (gamePadState.Value.DPad.Left == ButtonState.Pressed)
     playerObject.ApplyLeft();
    else if (gamePadState.Value.DPad.Right == ButtonState.Pressed)
     playerObject.ApplyRight();
    else
     playerObject.ClearMotion();
    if (gamePadState.Value.Buttons.A == ButtonState.Pressed)
     bJump = true;
    if (gamePadState.Value.Buttons.B == ButtonState.Pressed)
     bFire = true;
   }
   // Прыжок
   if (bJump)
   {
    if (playerObject.Jump())
     m_soundManager.Play("jump");
   }
   // Стрельба
   if (bFire)
   {
    if (playerObject.Fire())
    {
     TileSheet ballTileSheet = m_map.GetTileSheet("ball");
     Texture2D ballTexture = m_xnaDisplayDevice.GetTileSheetTexture(ballTileSheet);
     m_gameObjectList.Add(new BulletGameObject(m_world, playerObject, ballTileSheet, ballTexture));
     m_soundManager.Play("throw");
    }
   }
   // Приземление
   if (playerObject.IsLanded)
    m_soundManager.Play("jumpland");
   // limit viewport to map if wraparound disabled
   Vector2 playerPosition = playerObject.Position;
   m_viewPort.Location.X = (int)playerPosition.X - m_viewPort.Width / 2;
   m_viewPort.Location.Y = (int)playerPosition.Y - m_viewPort.Height / 2;
   m_viewPort.Location.X = Math.Max(0, m_viewPort.X);
   m_viewPort.Location.Y = Math.Max(0, m_viewPort.Y);
   m_viewPort.Location.X = Math.Min(m_map.DisplayWidth - m_viewPort.Width, m_viewPort.X);
   m_viewPort.Location.Y = Math.Min(m_map.DisplayHeight - m_viewPort.Height, m_viewPort.Y);
   // Физика
   m_world.Step(Math.Min((float)gameTime.ElapsedGameTime.TotalMilliseconds * 0.001f, (1f / 30f)));
   // Карта
   m_map.Update(gameTime.ElapsedGameTime.Milliseconds);
   // Игровые объекты
   foreach (GameObject gameObject in m_gameObjectList)
    gameObject.Update(gameTime);
   // Излучатели частиц
   m_particleEmitterManager.Update(gameTime);
   // Панель
   m_panelManager.Update(gameTime);
   // Подбираем кристалы
   List<GameObject> removeGameObjectList = null;
   List<GameObject> addGameObjectList = null;
   foreach (GameObject gameObject in m_gameObjectList)
   {
    if (gameObject is GemGameObject)
    {
     if ((gameObject as GemGameObject).PickedUp)
     {
      if (removeGameObjectList == null)
       removeGameObjectList = new List<GameObject>();
      removeGameObjectList.Add(gameObject);
      m_particleEmitterManager.CreateGemEmiter(gameObject);
      m_soundManager.Play("pick");
      int nPanelGemSlot = m_panelManager.AddGem(gameObject as GemGameObject);
      Point p = m_panelManager.PlaceholderBoundsList[nPanelGemSlot].Center;
      m_particleEmitterManager.CreatePanelGemEmitter(new Vector2(p.X, p.Y));
     }
    }
   }
   // Удалеем мячики
   foreach (GameObject gameObject in m_gameObjectList)
   {
    if (gameObject is BulletGameObject)
    {
     BulletGameObject bulletGameObject = gameObject as BulletGameObject;
     if (bulletGameObject.IsBlow)
      m_soundManager.Play("bullet_blow");
     if (bulletGameObject.NeedRemove)
     {
      if (removeGameObjectList == null)
       removeGameObjectList = new List<GameObject>();
      removeGameObjectList.Add(gameObject);
     }
    }
   }
   // Убиваем врагов
   foreach (GameObject gameObject in m_gameObjectList)
   {
    if (gameObject is EnemyGameObject)
    {
     EnemyGameObject enemyGameObject = gameObject as EnemyGameObject;
     if (enemyGameObject.IsBlow)
     {
      m_soundManager.Stop(enemyGameObject.GetSound("player"));
      m_soundManager.Play(enemyGameObject.GetSound("blow"));
     }
     if (enemyGameObject.IsPlayerInSight)
      m_soundManager.Play(enemyGameObject.GetSound("player"));
     if (enemyGameObject.NeedRemove)
     {
      if (removeGameObjectList == null)
       removeGameObjectList = new List<GameObject>();
      removeGameObjectList.Add(gameObject);
     }
    }
   }
   // Выбрасываем кристалы
   foreach (GameObject gameObject in m_gameObjectList)
   {
    if (gameObject is EnemyGameObject)
    {
     EnemyGameObject enemyGameObject = gameObject as EnemyGameObject;
     if (enemyGameObject.IsDropGem)
     {
      if (m_gemTileSheetList.Count > 0)
      {
       xTile.Tiles.TileSheet gemTilesheet = m_gemTileSheetList[Random.Next(m_gemTileSheetList.Count - 1)];
       GemGameObject gemGameObject = new GemGameObject(m_world, gemTilesheet, m_xnaDisplayDevice.GetTileSheetTexture(gemTilesheet));
       enemyGameObject.DropGem(gemGameObject);
       if (addGameObjectList == null)
        addGameObjectList = new List<GameObject>();
       addGameObjectList.Add(gemGameObject);
       m_soundManager.Play("drop_gem");
      }
     }
    }
   }
   // Вода
   if (playerObject.IsEnterWater)
   {
    m_soundManager.Play("water");
    m_particleEmitterManager.CreateWaterEmiter(playerObject);
   }
   // Выход
   if (m_waypointManager.Check(playerObject, "exit"))
   {
    if (!m_bMapEndedFlagRead)
     m_soundManager.Play("exit");
    m_bMapEnded = true;
   }
   // Удаляем объекты
   if (removeGameObjectList != null)
   {
    foreach (var gameObject in removeGameObjectList)
    {
     m_gameObjectList.Remove(gameObject);
    }
   }
   // Добавляем объекты
   if (addGameObjectList != null)
   {
    foreach (GameObject gameObject in addGameObjectList)
    {
     m_gameObjectList.Add(gameObject);
    }
   }
   // Листочки
   m_leafRemainMilliseconds -= (long)gameTime.ElapsedGameTime.TotalMilliseconds;
   if (m_leafRemainMilliseconds <= 0)
   {
    m_leafRemainMilliseconds = Random.Next(10000) + 10000;
    m_particleEmitterManager.CreateLeafEmitter(new Microsoft.Xna.Framework.Rectangle(m_viewPort.X - m_viewPort.Width*2, m_viewPort.Y - m_viewPort.Height*2, m_viewPort.Width, m_viewPort.Height));
   }
  }

  #endregion

  /// <summary>
  /// После отрисовка переднего слоя
  /// </summary>
  private void foregroundLayer_AfterDraw(object sender, LayerEventArgs layerEventArgs)
  {
   foreach (GameObject gameObject in m_gameObjectList)
    gameObject.Draw(m_xnaDisplayDevice.SpriteBatchAlpha, layerEventArgs.Viewport);
  }

  /// <summary>
  /// После отрисовки воды
  /// </summary>
  /// <param name="sender"></param>
  /// <param name="layerEventArgs"></param>
  private void waterLayer_AfterDraw(object sender, LayerEventArgs layerEventArgs)
  {
   m_panelManager.Draw(m_xnaDisplayDevice.SpriteBatchAlpha, layerEventArgs.Viewport);
   m_particleEmitterManager.Draw(m_xnaDisplayDevice.SpriteBatchAlpha, layerEventArgs.Viewport);
  }

 }
}
