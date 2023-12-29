using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

namespace TheGame;

public class Button : Component
{
    #region Fields

    private MouseState _currentMouse;
    private TouchCollection _currentTouch;

    private SpriteFont _font;

    private bool _isHovering;

    private MouseState _previousMouse;

    private Texture2D _texture;

    private Game _game;

    #endregion

    #region Properties

    public event EventHandler Click;

    public Color PenColour { get; set; }

    public Vector2 Position { get; set; }

    public bool IsPressed { get; private set; }

    public Rectangle Rectangle { get; set; }

    public string Text { get; set; }

    #endregion

    #region Methods

    public Button(Game game, Texture2D texture, SpriteFont font, Vector2 position)
    {
        _game = game;
        _texture = texture;
        _font = font;        
        Position = position;
        PenColour = Color.Black;
        Rectangle = new ((int)Position.X, (int)Position.Y, _texture.Width, _texture.Height);
    }

    /// <inheritdoc/>
    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var colour = Color.White;

        if (_isHovering)
            colour = Color.Red;

        spriteBatch.Draw(_texture, Rectangle, colour);

        if (!string.IsNullOrEmpty(Text))
        {
            var x = (Rectangle.X + (Rectangle.Width / 2)) - (_font.MeasureString(Text).X / 2);
            var y = (Rectangle.Y + (Rectangle.Height / 2)) - (_font.MeasureString(Text).Y / 2);

            spriteBatch.DrawString(_font, Text, new Vector2(x, y), PenColour);
        }
    }

    /// <inheritdoc/>
    public override void Update(GameTime gameTime, MouseState mouseState, TouchCollection touchPanelState)
    {
        IsPressed = false;
        _previousMouse = _currentMouse;
        _currentMouse = mouseState;
        _currentTouch = touchPanelState;

        var mouseRectangle = new Rectangle(_currentMouse.X, _currentMouse.Y, 1, 1);

        _isHovering = false;

        if (mouseRectangle.Intersects(Rectangle))
        {
            _isHovering = true;

            if (_currentMouse.LeftButton == ButtonState.Released && _previousMouse.LeftButton == ButtonState.Pressed)
            {
                IsPressed = true;
                Click?.Invoke(this, new EventArgs());
            }
        }

        foreach (var touchState in touchPanelState)
        {
            var touchRectangle = new Rectangle((int)touchState.Position.X, (int)touchState.Position.Y, 1, 1);
            if (touchRectangle.Intersects(Rectangle))
            {
                _isHovering = true;
                if (touchState.State == TouchLocationState.Pressed)
                {
                    IsPressed = true;
                    Click?.Invoke(this, new EventArgs());
                }
            }

        }
    }

    #endregion
}