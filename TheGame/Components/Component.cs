using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Input;

namespace TheGame;

/// <summary>
/// Абстрактный компонент
/// </summary>
public abstract class Component
{
    /// <summary>
    /// Отрисовка
    /// </summary>
    public virtual void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
    }

    /// <summary>
    /// Расчет
    /// </summary>
    public virtual void Update(GameTime gameTime, MouseState mouseState, TouchCollection touchPanelState)
    {
    }
}