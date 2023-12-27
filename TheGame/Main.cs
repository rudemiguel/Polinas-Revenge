using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using BloomPostprocess;

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
    private Texture2D m_pixelTexture;
    private SpriteBatch m_spriteBatch;
    private SoundManager m_soundManager;
    private MusicManager m_musicManager;
    private bool m_bFade;
    private int m_fadeOpacity;
    private bool m_bFadeDirection;

    public IList<string> Assets { get; set; }

    public Main()
        : base()
    {
        m_bFade = false;
        m_graphicsDeviceManager = new GraphicsDeviceManager(this);
        m_graphicsDeviceManager.PreferredBackBufferWidth = 640;
        m_graphicsDeviceManager.PreferredBackBufferHeight = 480;
        m_graphicsDeviceManager.IsFullScreen = true;
        m_graphicsDeviceManager.SynchronizeWithVerticalRetrace = true;
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
        //Components.Add(m_bloom);

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
        m_pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
        m_pixelTexture.SetData<Color>(new Color[] { Color.White });
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
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    protected override void Draw(GameTime gameTime)
    {
        base.Draw(gameTime);

        if (m_bFade)
        {
            m_spriteBatch.Begin();
            m_spriteBatch.Draw(m_pixelTexture, new Vector2(0, 0), null, new Color(0, 0, 0, m_fadeOpacity), 0f, Vector2.Zero, new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height), SpriteEffects.None, 0);
            m_spriteBatch.End();
        }
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
