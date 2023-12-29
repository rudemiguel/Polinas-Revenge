using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Input;

namespace TheGame;

/// <summary>
/// ����������� ���������
/// </summary>
public abstract class Component
{
    /// <summary>
    /// ���������
    /// </summary>
    public virtual void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
    }

    /// <summary>
    /// ������
    /// </summary>
    public virtual void Update(GameTime gameTime, MouseState mouseState, TouchCollection touchPanelState)
    {
    }
}