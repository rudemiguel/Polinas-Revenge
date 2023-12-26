using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;


namespace TheGame
{
 /// <summary>
 /// Менеджер панели
 /// </summary>
 class PanelManager
 {
  private Texture2D m_texturePanel;
  private Texture2D m_texturePlaceholder;
  private SpriteFont m_spriteFont;
  private List<GemGameObject> m_gemsList;
  private List<Rectangle> m_gemPlaceholderList;
  private int m_nGemCount;
  
  public PanelManager()
  {
   m_gemsList = new List<GemGameObject>();
   m_gemPlaceholderList = null;
   m_nGemCount = 0;
  }

  public int GemCount
  {
   get
   {
    return m_nGemCount;
   }
   set
   {
    m_nGemCount = value;
   }
  }

  public List<Rectangle> PlaceholderBoundsList
  {
   get
   {
    return m_gemPlaceholderList;
   }
  }

  public List<GemGameObject> GemList
  {
   get
   {
    return m_gemsList;
   }
  }

  public void LoadContent(GraphicsDevice graphicsDevice, ContentManager content)
  {
   m_spriteFont = content.Load<SpriteFont>("Fonts\\Font");
   m_texturePanel = new Texture2D(graphicsDevice, 1, 1);
   m_texturePanel.SetData<Color>(new Color[] { new Color(0, 0, 0, 128) });
   m_texturePlaceholder = content.Load<Texture2D>("Graphics\\frame");
  }

  public int AddGem(GemGameObject gemGameObject)
  {
   m_gemsList.Add(gemGameObject);
   if (m_gemsList.Count > m_nGemCount)
    throw new SystemException("Gem count error");
   return m_gemsList.Count-1;
  }

  public void Update(GameTime gameTime)
  {
  }

  public void Draw(SpriteBatch spriteBatch, xTile.Dimensions.Rectangle viewPort)
  {
   // Надо ли заполнить список слотов
   bool bNeedFillPlaceholderList = false;
   if (m_gemPlaceholderList == null)
   {
    bNeedFillPlaceholderList = true;
    m_gemPlaceholderList = new List<Microsoft.Xna.Framework.Rectangle>();
   }
   //
   int h = viewPort.Height / 10;
   Microsoft.Xna.Framework.Rectangle panelRectangle = new Microsoft.Xna.Framework.Rectangle(0, viewPort.Height - h, viewPort.Width, h);
   //spriteBatch.Draw(m_texturePanel, panelRectangle, Color.White);
   int nPaddingVectical = 5;
   int nPlaceHolderWidth = panelRectangle.Width / (m_nGemCount+1);
   int nPlaceHolderHeight = panelRectangle.Height - nPaddingVectical * 2;
   if ((float)nPlaceHolderWidth / (float)nPlaceHolderHeight > 2)
    nPlaceHolderWidth = (int)((float)nPlaceHolderHeight * 1.5);
   int nPaddingHorizontal = nPlaceHolderWidth / (m_nGemCount + 1);
   int x = 0;
   for (int i = 0; i < m_nGemCount; ++i)
   {
    // Рисуем слот
    x += nPaddingHorizontal;
    Microsoft.Xna.Framework.Rectangle placeholderRectangle = new Microsoft.Xna.Framework.Rectangle(x, panelRectangle.Y + nPaddingVectical, nPlaceHolderWidth, nPlaceHolderHeight);
    //spriteBatch.Draw(m_texturePlaceholder, placeholderRectangle, Color.White);
    if (bNeedFillPlaceholderList)
     m_gemPlaceholderList.Add(placeholderRectangle);
    // Рисуем кристалл
    if (i < m_gemsList.Count)
    {
     GemGameObject gemGameObject = m_gemsList[i];
     Texture2D texture = gemGameObject.Texture;
     int nGemWidth=0, nGemHeight=0;
     if (texture.Width >= texture.Height)
     {
      int nGemHorizontalPadding = 5;
      nGemWidth = placeholderRectangle.Width - nGemHorizontalPadding * 2;
      if (nGemWidth > texture.Width)
       nGemWidth = texture.Width;
      nGemHeight = (int)(((float)texture.Height / (float)texture.Width) * (float)nGemWidth);
     }
     else
     {
      int nGemVerticalPadding = 5;
      nGemHeight = placeholderRectangle.Height - nGemVerticalPadding * 2;
      if (nGemHeight > texture.Height)
       nGemHeight = texture.Height;
      nGemWidth = (int)(((float)texture.Width / (float)texture.Height) * (float)nGemHeight);
     }
     Microsoft.Xna.Framework.Rectangle gemRectangle = new Microsoft.Xna.Framework.Rectangle( 
      (placeholderRectangle.X + placeholderRectangle.Width/2) - nGemWidth/2,
      (placeholderRectangle.Y + placeholderRectangle.Height/2) - nGemHeight/2,
      nGemWidth,
      nGemHeight
     );
     if (m_texturePlaceholder!=null)
      spriteBatch.Draw(m_texturePlaceholder, placeholderRectangle, Color.White);
     spriteBatch.Draw(texture, gemRectangle, Color.White);
    }
    x += nPlaceHolderWidth;
   }
  }
 }
}
