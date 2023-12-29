using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using xTile;
using xTile.Dimensions;
using xTile.Display;
using xTile.Tiles;
using xTile.Layers;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Common;
using nkast.Aether.Physics2D.Common.Decomposition;
using FarseerPhysics;
using Studies.Joystick.Input;
using System.Linq;

namespace TheGame;

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
    private List<TileSheet> m_gemTileSheetList;
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
    
    // компоненты
    private DualStick _dualStick;
    private Button _jumpButton;
    private Button _fireButton;
    private List<Component> _components = new ();
    private SpriteBatch _componentsSpriteBatch;    

    //
    private bool m_bMapEnded;
    private bool m_bMapEndedFlagRead;
    //
    private long m_leafRemainMilliseconds;

    public MapGameElement(Game game, SoundManager soundManager, MusicManager musicManager)
     : base(game)
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
    public PanelManager PanelManager => m_panelManager;

    #region DrawableGameComponent Members

    /// <summary>
    /// Загрузка контента
    /// </summary>        
    protected override void LoadContent()
    {
        var game = Game as Main;
        var content = game.Content;
        content.RootDirectory = "Content";
        var graphicsDevice = Game.GraphicsDevice;
        m_viewPort = new xTile.Dimensions.Rectangle(0, 0, game.GameResolution.Width, game.GameResolution.Height);
        
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
        var waterLayer = m_map.GetLayer("water");
        m_waterManager = new WaterManager(waterLayer);
        
        // Пол
        var foregroundLayer = m_map.GetLayer("foreground");
        var groundLayer = m_map.GetLayer("ground");
        uint[] data = new uint[foregroundLayer.TileWidth * foregroundLayer.TileHeight];
        var tileVertices = new Dictionary<int, Vertices>();
        for (int x = 0; x < groundLayer.LayerWidth; x++)
        {
            for (int y = 0; y < groundLayer.LayerHeight; y++)
            {
                var tile = groundLayer.Tiles[x, y];
                var foregroundTile = foregroundLayer.Tiles[x, y];
                if (tile != null && foregroundTile != null)
                {
                    Vertices vertices = null;
                    tileVertices.TryGetValue(foregroundTile.TileIndex, out vertices);
                    if (vertices == null)
                    {
                        var r = foregroundTile.TileSheet.GetTileImageBounds(foregroundTile.TileIndex);
                        var tileSheetTexture = m_xnaDisplayDevice.GetTileSheetTexture(tile.TileSheet);
                        var rect = new Microsoft.Xna.Framework.Rectangle(r.X, r.Y, r.Width, r.Height);
                        tileSheetTexture.GetData<uint>(0, rect, data, 0, rect.Width * rect.Height);
                        vertices = PolygonTools.CreatePolygon(data, foregroundLayer.TileWidth, true);
                        var scaleVector = ConvertUnits.ToSimUnits(1f, 1f);
                        vertices.Scale(ref scaleVector);
                        tileVertices.Add(foregroundTile.TileIndex, vertices);
                    }
                    var _list = Triangulate.ConvexPartition(vertices, TriangulationAlgorithm.Bayazit);
                    var body = m_world.CreateCompoundPolygon(_list, 1);
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
                    var strTileSheetName = "";
                    xTile.ObjectModel.PropertyValue t = null;
                    tile.Properties.TryGetValue("type", out t);
                    if (t != null)
                        strTileSheetName = t.ToString();
                    var tileSheet = m_map.GetTileSheet(strTileSheetName);
                    if (tileSheet == null)
                        throw new Exception(string.Format("Tile sheet not found by name: {0}", strTileSheetName));
                    var texture = m_xnaDisplayDevice.GetTileSheetTexture(tileSheet);
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
                            m_panelManager.ReserveGem();
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
        var gemsLayer = m_map.GetLayer("gems");
        for (int x = 0; x < gemsLayer.LayerWidth; x++)
        {
            for (int y = 0; y < gemsLayer.LayerHeight; y++)
            {
                var tile = gemsLayer.Tiles[x, y];
                if (tile != null)
                {
                    var pos = new Vector2(x * gemsLayer.TileWidth + gemsLayer.TileWidth / 2, (y * gemsLayer.TileHeight + gemsLayer.TileHeight / 2) - 50);
                    var strTileSheetName = "";
                    xTile.ObjectModel.PropertyValue t = null;
                    tile.Properties.TryGetValue("type", out t);
                    if (t != null)
                        strTileSheetName = t.ToString();
                    var tileSheet = m_map.GetTileSheet(strTileSheetName);
                    if (tileSheet == null)
                        throw new Exception(string.Format("Tile sheet not found by name: {0}", strTileSheetName));
                    m_gemTileSheetList.Add(tileSheet);
                    var texture = m_xnaDisplayDevice.GetTileSheetTexture(tileSheet);
                    var gem = new GemGameObject(m_world, tileSheet, texture);
                    gem.Position = pos;
                    m_gameObjectList.Add(gem);
                    gemsLayer.Tiles[x, y] = null;
                    m_panelManager.ReserveGem();
                }
            }
        }

        // метки
        var waypointsLayer = m_map.GetLayer("waypoints");
        if (waypointsLayer != null)
        {
            for (int x = 0; x < waypointsLayer.LayerWidth; x++)
            {
                for (int y = 0; y < waypointsLayer.LayerHeight; y++)
                {
                    var tile = waypointsLayer.Tiles[x, y];
                    if (tile != null)
                    {
                        var pos = new Vector2(x * waypointsLayer.TileWidth + waypointsLayer.TileWidth / 2, y * waypointsLayer.TileHeight + waypointsLayer.TileHeight / 2);
                        var strName = "";
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

        // Компоненты
        _componentsSpriteBatch = new SpriteBatch(GraphicsDevice);

        // Джойстик
        var font = content.Load<SpriteFont>("Fonts\\font");
        _dualStick = new DualStick(font);
        _dualStick.LeftStick.SetAsFree();
        _dualStick.RightStick.SetAsFree();
        _components.Add(_dualStick);

        // Кнопки
        var jumpButtonImage = content.Load<Texture2D>("Graphics\\jump_button");
        var jumpButtonImageSize = new Microsoft.Xna.Framework.Rectangle(0, 0, jumpButtonImage.Width / 2, jumpButtonImage.Width / 2);
        var jumpPosition = new Vector2(
            game.GameResolution.Width - jumpButtonImageSize.Width - game.GameResolution.Width / 20,
            game.GameResolution.Height - jumpButtonImageSize.Height - game.GameResolution.Height / 20);
        _jumpButton = new Button(game, jumpButtonImage, font, jumpPosition);
        _jumpButton.Rectangle = new (
            (int)jumpPosition.X,
            (int)jumpPosition.Y,
            jumpButtonImageSize.Width,
            jumpButtonImageSize.Height
        );
        _jumpButton.TouchRectangle = new (
            _jumpButton.Rectangle.X - game.GameResolution.Width / 40,
            _jumpButton.Rectangle.Y - game.GameResolution.Height / 40,
            _jumpButton.Rectangle.Width + game.GameResolution.Width * 2 / 40,
            _jumpButton.Rectangle.Height + game.GameResolution.Height * 2 / 40
        );
        _components.Add(_jumpButton);

        var fireButtonImage = content.Load<Texture2D>("Graphics\\fire_button");
        var fireButtonImageSize = new Microsoft.Xna.Framework.Rectangle(0, 0, fireButtonImage.Width / 2, fireButtonImage.Width / 2);
        var firePosition = new Vector2(
            jumpPosition.X - fireButtonImageSize.Width - game.GameResolution.Width / 20,
            game.GameResolution.Height - fireButtonImageSize.Height - game.GameResolution.Height / 20);
        _fireButton = new Button(game, fireButtonImage, font, firePosition);
        _fireButton.Rectangle = new Microsoft.Xna.Framework.Rectangle (
            (int)firePosition.X,
            (int)firePosition.Y,
            fireButtonImageSize.Width,
            fireButtonImageSize.Height
        );
        _fireButton.TouchRectangle = new (
            _fireButton.Rectangle.X - game.GameResolution.Width / 40,
            _fireButton.Rectangle.Y - game.GameResolution.Height / 40,
            _fireButton.Rectangle.Width + game.GameResolution.Width * 2 / 40,
            _fireButton.Rectangle.Height + game.GameResolution.Height * 2 / 40
        );

        _components.Add(_fireButton);        
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
    /// Обновление
    /// </summary>
    /// <param name="gameTime"></param>
    public override void Update(GameTime gameTime)
    {
        var game = Game as Main;
        bool isJump = false;
        bool isFire = false;        

        // Ввод
        var mouseState = Mouse.GetState();
        var touchPanelState = TouchPanel.GetState();
        var keyboardState = Keyboard.GetState();
        GamePadState? gamePadState = GamePad.GetState(PlayerIndex.One).IsConnected
            ? GamePad.GetState(PlayerIndex.One)
            : null;

        var viewport = game.ScreenViewport;
        var gameResolution = game.GameResolution;
        var touchLocations = touchPanelState
            .Select(state =>
            {
                var x = ((state.Position.X - viewport.X) / viewport.Width) * gameResolution.Width;
                var y = ((state.Position.Y - viewport.Y) / viewport.Height) * gameResolution.Height;
                return new TouchLocation(state.Id, state.State, new Vector2(x, y));
            })
            .ToArray();        
        touchPanelState = new TouchCollection(touchLocations);
        
        // Компоненты
        foreach (var component in _components)
            component.Update(gameTime, mouseState, touchPanelState);

        // Клавиатура
        var playerObject = m_gameObjectList[0] as ActorGameObject;
        if (keyboardState.IsKeyDown(Keys.Left))
            playerObject.ApplyLeft();
        else if (keyboardState.IsKeyDown(Keys.Right))
            playerObject.ApplyRight();
        else
            playerObject.ClearMotion();
        if (keyboardState.IsKeyDown(Keys.Space))
            isJump = true;
        if (keyboardState.IsKeyDown(Keys.RightShift))
            isFire = true;

        // Геймпад
        if (gamePadState != null)
        {
            if (gamePadState.Value.DPad.Left == ButtonState.Pressed)
                playerObject.ApplyLeft();
            else if (gamePadState.Value.DPad.Right == ButtonState.Pressed)
                playerObject.ApplyRight();
            else
                playerObject.ClearMotion();
            if (gamePadState.Value.Buttons.A == ButtonState.Pressed)
                isJump = true;
            if (gamePadState.Value.Buttons.B == ButtonState.Pressed)
                isFire = true;
        }

        // Экранный джойстик        
        if (TouchPanel.GetCapabilities().IsConnected)
        {
            var stickHorizontalAxis = _dualStick.LeftStick.GetRelativeVector(_dualStick.aliveZoneSize).X;
            if (stickHorizontalAxis < 0)
                playerObject.ApplyLeft();
            else if (stickHorizontalAxis > 0)
                playerObject.ApplyRight();
            else
                playerObject.ClearMotion();
        }

        // Экранные кнопки
        if (_jumpButton.IsPressed)
            isJump = true;
        if (_fireButton.IsPressed)
            isFire = true;

        // Прыжок
        if (isJump)
        {
            if (playerObject.Jump())
                m_soundManager.Play("jump");
        }

        // Стрельба
        if (isFire)
        {
            if (playerObject.Fire())
            {
                var ballTileSheet = m_map.GetTileSheet("ball");
                var ballTexture = m_xnaDisplayDevice.GetTileSheetTexture(ballTileSheet);
                m_gameObjectList.Add(new BulletGameObject(m_world, playerObject, ballTileSheet, ballTexture));
                m_soundManager.Play("throw");
            }
        }

        // Приземление
        if (playerObject.IsLanded)
            m_soundManager.Play("jumpland");
        
        // limit viewport to map if wraparound disabled
        var playerPosition = playerObject.Position;
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
        foreach (var gameObject in m_gameObjectList)
            gameObject.Update(gameTime);

        // Излучатели частиц
        m_particleEmitterManager.Update(gameTime);

        // Панель
        m_panelManager.Update(gameTime);

        // Подбираем кристалы
        List<GameObject> removeGameObjectList = null;
        List<GameObject> addGameObjectList = null;
        foreach (var gameObject in m_gameObjectList)
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
                    var p = m_panelManager.PlaceholderBoundsList[nPanelGemSlot].Center;
                    m_particleEmitterManager.CreatePanelGemEmitter(new Vector2(p.X, p.Y));
                }
            }
        }

        // Удалеем мячики
        foreach (var gameObject in m_gameObjectList)
        {
            if (gameObject is BulletGameObject)
            {
                var bulletGameObject = gameObject as BulletGameObject;
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
        foreach (var gameObject in m_gameObjectList)
        {
            if (gameObject is EnemyGameObject)
            {
                var enemyGameObject = gameObject as EnemyGameObject;
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
        foreach (var gameObject in m_gameObjectList)
        {
            if (gameObject is EnemyGameObject)
            {
                var enemyGameObject = gameObject as EnemyGameObject;
                if (enemyGameObject.IsDropGem)
                {
                    if (m_gemTileSheetList.Count > 0)
                    {
                        var gemTilesheet = m_gemTileSheetList[Random.Next(m_gemTileSheetList.Count - 1)];
                        var gemGameObject = new GemGameObject(m_world, gemTilesheet, m_xnaDisplayDevice.GetTileSheetTexture(gemTilesheet));
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
                gameObject.Dispose();
            }
        }

        // Добавляем объекты
        if (addGameObjectList != null)
        {
            foreach (var gameObject in addGameObjectList)
            {
                m_gameObjectList.Add(gameObject);
            }
        }

        // Листочки
        m_leafRemainMilliseconds -= (long)gameTime.ElapsedGameTime.TotalMilliseconds;
        if (m_leafRemainMilliseconds <= 0)
        {
            m_leafRemainMilliseconds = Random.Next(10000) + 10000;
            m_particleEmitterManager.CreateLeafEmitter(new Microsoft.Xna.Framework.Rectangle(m_viewPort.X - m_viewPort.Width * 2, m_viewPort.Y - m_viewPort.Height * 2, m_viewPort.Width, m_viewPort.Height));
        }
    }

    /// <summary>
    /// Отрисовка
    /// </summary>
    /// <param name="gameTime"></param>
    public override void Draw(GameTime gameTime)
    {
        // карта
        m_map.Draw(m_xnaDisplayDevice, m_viewPort, Location.Origin, false);

        // компоненты
        _componentsSpriteBatch.Begin();
        foreach (var component in _components)
            component.Draw(gameTime, _componentsSpriteBatch);        
        _componentsSpriteBatch.End();
    }

    #endregion

    /// <summary>
    /// После отрисовка переднего слоя
    /// </summary>
    private void foregroundLayer_AfterDraw(object sender, LayerEventArgs layerEventArgs)
    {
        foreach (var gameObject in m_gameObjectList)
            gameObject.Draw(m_xnaDisplayDevice.SpriteBatchAlpha, layerEventArgs.Viewport);
    }

    /// <summary>
    /// После отрисовки воды
    /// </summary>
    private void waterLayer_AfterDraw(object sender, LayerEventArgs layerEventArgs)
    {
        m_panelManager.Draw(m_xnaDisplayDevice.SpriteBatchAlpha, layerEventArgs.Viewport);
        m_particleEmitterManager.Draw(m_xnaDisplayDevice.SpriteBatchAlpha, layerEventArgs.Viewport);
    }

}
