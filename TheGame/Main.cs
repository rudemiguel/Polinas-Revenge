using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using BloomPostprocess;
using Microsoft.Xna.Framework.Input.Touch;

namespace TheGame;

/// <summary>
/// Игра
/// </summary>
public class Main : Game
{
    private GraphicsDeviceManager m_graphicsDeviceManager;
    private MapGameElement m_mapGameElement;
    private FinalScreenGameElement m_finalScreenGameElement;
    private BloomComponent m_bloom;
    private Texture2D m_fadeTexture;
    private SpriteBatch m_spriteBatch;
    private SoundManager m_soundManager;
    private MusicManager m_musicManager;
    private RenderTarget2D m_renderTarget;
    private bool m_bFade;
    private int m_fadeOpacity;
    private bool m_bFadeDirection;

    /// <summary>
    /// Список путей до ресурсов
    /// </summary>
    public IList<string> Assets { get; set; }

    /// <summary>
    /// Разрешение для рендеренга
    /// </summary>
    public Rectangle GameResolution => new Rectangle(0, 0, 800, 480);

    /// <summary>
    /// Физическое разрешение устройства
    /// </summary>
    public Rectangle ScreenViewport { get; private set; }

    public Main()
        : base()
    {
        m_bFade = false;
        m_graphicsDeviceManager = new GraphicsDeviceManager(this);
        m_graphicsDeviceManager.IsFullScreen = true;
        m_graphicsDeviceManager.SynchronizeWithVerticalRetrace = true;
        m_graphicsDeviceManager.PreferHalfPixelOffset = false;
        Content.RootDirectory = "Content";
    }

    /// <summary>
    /// Инициализация
    /// </summary>
    protected override void Initialize()
    {
        m_soundManager = new SoundManager();
        m_musicManager = new MusicManager();
        m_mapGameElement = new MapGameElement(this, m_soundManager, m_musicManager);
        m_finalScreenGameElement = new FinalScreenGameElement(this, m_mapGameElement.PanelManager, m_soundManager, m_musicManager);
        m_finalScreenGameElement.Visible = false;
        m_finalScreenGameElement.Enabled = false;
        Components.Add(m_mapGameElement);
        Components.Add(m_finalScreenGameElement);

        // bloom
        m_bloom = new BloomComponent(this);
        m_bloom.Settings = new BloomSettings(null, 0.95f, 1f, 1.5f, 1f, 1f, 1f);
        Components.Add(m_bloom);

        m_graphicsDeviceManager.PreferredBackBufferWidth = GraphicsDevice.Adapter.CurrentDisplayMode.Width;
        m_graphicsDeviceManager.PreferredBackBufferHeight = GraphicsDevice.Adapter.CurrentDisplayMode.Height;
        m_graphicsDeviceManager.ApplyChanges();

        // Расчет вьюпорта
        var desiredAspect = (float)GameResolution.Width / (float)GameResolution.Height;
        var actualAspect = (float)m_graphicsDeviceManager.PreferredBackBufferWidth / (float)GraphicsDevice.Adapter.CurrentDisplayMode.Height;
        if (actualAspect <= desiredAspect)
        {
            // output is taller than it is wider, bars on top/bottom
            int presentHeight = (int)((m_graphicsDeviceManager.PreferredBackBufferWidth / desiredAspect) + 0.5f);
            int barHeight = (m_graphicsDeviceManager.PreferredBackBufferHeight - presentHeight) / 2;
            ScreenViewport = new Rectangle(0, barHeight, m_graphicsDeviceManager.PreferredBackBufferWidth, presentHeight);
        }
        else
        {
            // output is wider than it is tall, bars left/right
            int presentWidth = (int)((m_graphicsDeviceManager.PreferredBackBufferHeight * desiredAspect) + 0.5f);
            int barWidth = (m_graphicsDeviceManager.PreferredBackBufferWidth - presentWidth) / 2;
            ScreenViewport = new Rectangle(barWidth, 0, presentWidth, m_graphicsDeviceManager.PreferredBackBufferHeight);
        }

        // Бэкбуфер для рендеринга в уменьшеном разрешении
        m_renderTarget = new RenderTarget2D(GraphicsDevice, GameResolution.Width, GameResolution.Height, false, SurfaceFormat.Color, DepthFormat.None, 1, RenderTargetUsage.DiscardContents);

        base.Initialize();
    }

    /// <summary>
    /// Загрузка
    /// </summary>
    protected override void LoadContent()
    {
        base.LoadContent();
        m_soundManager.LoadContent(Content, Assets);
        m_musicManager.LoadContent(Content, Assets);
        m_fadeTexture = new Texture2D(GraphicsDevice, 1, 1);
        m_fadeTexture.SetData<Color>(new Color[] { Color.White });
        m_spriteBatch = new SpriteBatch(GraphicsDevice);
        m_musicManager.PlayCategory("map");        
    }

    /// <summary>
    /// Выгрузка
    /// </summary>
    protected override void UnloadContent()
    {
        base.UnloadContent();
    }

    /// <summary>
    /// Отрисовка
    /// </summary>
    protected override void Draw(GameTime gameTime)
    {
        // Редерим в уменьшеном разрешении в бэкбуфер
        GraphicsDevice.SetRenderTarget(m_renderTarget);

        base.Draw(gameTime);

        if (m_bFade)
        {
            m_spriteBatch.Begin();
            m_spriteBatch.Draw(m_fadeTexture, new Vector2(0, 0), null, new Color(0, 0, 0, m_fadeOpacity), 0f, Vector2.Zero, new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height), SpriteEffects.None, 0);
            m_spriteBatch.End();
        }

        GraphicsDevice.SetRenderTarget (null);

        // Копируем бэкбуфер в устройство со скейлингом
        m_spriteBatch.Begin();
        m_spriteBatch.Draw(m_renderTarget, ScreenViewport, GameResolution, Color.White);
        m_spriteBatch.End();
    }

    /// <summary>
    /// Обновление
    /// </summary>
    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (m_mapGameElement.IsMapEnded)
        {
            m_bFade = true;
            m_bFadeDirection = true;
            m_fadeOpacity = 0;
        }

        if (m_bFade)
        {
            m_fadeOpacity += (int)(gameTime.ElapsedGameTime.Milliseconds * 0.1) * (m_bFadeDirection ? 1 : -1);
            if (m_bFadeDirection)
            {
                if (m_fadeOpacity > 255)
                {
                    Components.Remove(m_mapGameElement);
                    m_finalScreenGameElement.Visible = true;
                    m_finalScreenGameElement.Enabled = true;
                    m_bFadeDirection = false;
                    if (m_bloom != null)
                    {
                        m_bloom.Settings.BloomIntensity = 2;
                        m_bloom.Settings.BlurAmount = 2;
                        m_bloom.Settings.BloomThreshold = 0.7f;
                    }
                    m_musicManager.PlayCategory("final");
                }
            }
            else
            {
                if (m_fadeOpacity <= 0)
                    m_bFade = false;
            }
        }
    }
}
