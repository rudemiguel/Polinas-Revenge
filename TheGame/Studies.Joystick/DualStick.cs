﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Studies.Joystick.Abstract;
using TheGame;

namespace Studies.Joystick.Input;

public class DualStick : Component
{
    // How quickly the touch stick follows in FreeFollow mode
    public readonly float aliveZoneFollowSpeed;
    // How far from the alive zone we can get before the touch stick starts to follow in FreeFollow mode
    public readonly float aliveZoneFollowFactor;
    // If we let the touch origin get too close to the screen edge,
    // the direction is less accurate, so push it away from the edge.
    public readonly float edgeSpacing;
    // Where touches register, if they first land beyond this point,
    // the touch wont be registered as occuring inside the stick
    public readonly float aliveZoneSize;
    // Keeps information of last 4 taps
    public readonly TapStart[] tapStarts = new TapStart[4];
    private int tapStartCount = 0;
    // this keeps counting, no ideia why i cant reset it
    private double totalTime;

    private readonly SpriteFont font;
    public Stick RightStick { get; set; }
    public Stick LeftStick { get; set; }


    public DualStick(
        SpriteFont font,
        float aliveZoneFollowFactor = 1.3f,
        float aliveZoneFollowSpeed = 0.05f,
        float edgeSpacing = 25f,
        float aliveZoneSize = 65f,
        float deadZoneSize = 5f)
    {
        this.aliveZoneFollowFactor = aliveZoneFollowFactor;
        this.aliveZoneFollowSpeed = aliveZoneFollowSpeed;
        this.edgeSpacing = edgeSpacing;
        this.aliveZoneSize = aliveZoneSize;
        this.font = font;

        LeftStick = new Stick(deadZoneSize,
             new Rectangle(0, 100, (int)(TouchPanel.DisplayWidth * 0.3f), TouchPanel.DisplayHeight - 100),
             aliveZoneSize, aliveZoneFollowFactor, aliveZoneFollowSpeed, edgeSpacing)
        {
            FixedLocation = new Vector2(aliveZoneSize * aliveZoneFollowFactor, TouchPanel.DisplayHeight - aliveZoneSize * aliveZoneFollowFactor)
        };
        RightStick = new Stick(
            deadZoneSize,
            new Rectangle((int)(TouchPanel.DisplayWidth * 0.5f), 100, (int)(TouchPanel.DisplayWidth * 0.5f), TouchPanel.DisplayHeight - 100),
            aliveZoneSize,
            aliveZoneFollowFactor,
            aliveZoneFollowSpeed,
            edgeSpacing)
        {
            FixedLocation = new Vector2(TouchPanel.DisplayWidth - aliveZoneSize * aliveZoneFollowFactor, TouchPanel.DisplayHeight - aliveZoneSize * aliveZoneFollowFactor)
        };

        //TouchPanel.EnabledGestures = GestureType.None;
        //TouchPanel.DisplayOrientation = DisplayOrientation.LandscapeLeft;
    }

    /// <inheritdoc/>
    public override void Update(GameTime gameTime, MouseState mouseState, TouchCollection touchPanelState)
    {
        var dt = (float)gameTime.ElapsedGameTime.TotalMilliseconds;
        totalTime += dt;

        var state = touchPanelState;
        TouchLocation? leftTouch = null, rightTouch = null;

        if (tapStartCount > state.Count)
            tapStartCount = state.Count;

        foreach (var loc in state)
        {
            if (loc.State == TouchLocationState.Released)
            {
                int tapStartId = -1;
                for (int i = 0; i < tapStartCount; ++i)
                {
                    if (tapStarts[i].Id == loc.Id)
                    {
                        tapStartId = i;
                        break;
                    }
                }
                if (tapStartId >= 0)
                {
                    for (int i = tapStartId; i < tapStartCount - 1; ++i)
                        tapStarts[i] = tapStarts[i + 1];
                    tapStartCount--;
                }
                continue;
            }
            else if (loc.State == TouchLocationState.Pressed && tapStartCount < tapStarts.Length)
            {
                tapStarts[tapStartCount] = new TapStart(loc.Id, totalTime, loc.Position);
                tapStartCount++;
            }

            if (LeftStick.touchLocation.HasValue && loc.Id == LeftStick.touchLocation.Value.Id)
            {
                leftTouch = loc;
                continue;
            }
            if (RightStick.touchLocation.HasValue && loc.Id == RightStick.touchLocation.Value.Id)
            {
                rightTouch = loc;
                continue;
            }

            if (!loc.TryGetPreviousLocation(out TouchLocation locPrev))
                locPrev = loc;

            if (!LeftStick.touchLocation.HasValue)
            {
                if (LeftStick.StartRegion.Contains((int)locPrev.Position.X, (int)locPrev.Position.Y))
                {
                    if (LeftStick.Style == TouchStickStyle.Fixed)
                    {
                        if (Vector2.Distance(locPrev.Position, LeftStick.StartLocation) < aliveZoneSize)
                        {
                            leftTouch = locPrev;
                        }
                    }
                    else
                    {
                        leftTouch = locPrev;
                        LeftStick.StartLocation = leftTouch.Value.Position;
                        if (LeftStick.StartLocation.X < LeftStick.StartRegion.Left + edgeSpacing)
                            LeftStick.StartLocation.X = LeftStick.StartRegion.Left + edgeSpacing;
                        if (LeftStick.StartLocation.Y > LeftStick.StartRegion.Bottom - edgeSpacing)
                            LeftStick.StartLocation.Y = LeftStick.StartRegion.Bottom - edgeSpacing;
                    }
                    continue;
                }
            }

            if (!RightStick.touchLocation.HasValue && locPrev.Id != RightStick.lastExcludedRightTouchId)
            {
                if (RightStick.StartRegion.Contains((int)locPrev.Position.X, (int)locPrev.Position.Y))
                {
                    bool excluded = false;
                    foreach (Rectangle r in RightStick.startExcludeRegions)
                    {
                        if (r.Contains((int)locPrev.Position.X, (int)locPrev.Position.Y))
                        {
                            excluded = true;
                            RightStick.lastExcludedRightTouchId = locPrev.Id;
                            continue;
                        }
                    }
                    if (excluded)
                        continue;
                    RightStick.lastExcludedRightTouchId = -1;
                    if (RightStick.Style == TouchStickStyle.Fixed)
                    {
                        if (Vector2.Distance(locPrev.Position, RightStick.StartLocation) < aliveZoneSize)
                        {
                            rightTouch = locPrev;
                        }
                    }
                    else
                    {
                        rightTouch = locPrev;
                        RightStick.StartLocation = rightTouch.Value.Position;
                        if (RightStick.StartLocation.X > RightStick.StartRegion.Right - edgeSpacing)
                            RightStick.StartLocation.X = RightStick.StartRegion.Right - edgeSpacing;
                        if (RightStick.StartLocation.Y > RightStick.StartRegion.Bottom - edgeSpacing)
                            RightStick.StartLocation.Y = RightStick.StartRegion.Bottom - edgeSpacing;
                    }
                    continue;
                }
            }
        }

        LeftStick.Update(state, leftTouch, dt);
        RightStick.Update(state, rightTouch, dt);
    }

    /// <inheritdoc/>
    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        //DrawStringCentered($"L", LeftStick.StartLocation, Color.Black, spriteBatch);
        //DrawStringCentered($"L@L", LeftStick.GetPositionVector(aliveZoneSize), Color.Black, spriteBatch);
        //DrawStringCentered($"R", RightStick.StartLocation, Color.Black, spriteBatch);
        //DrawStringCentered($"R@R", RightStick.GetPositionVector(aliveZoneSize), Color.Black, spriteBatch);
    }

    private void DrawStringCentered(string text, Vector2 position, Color color, SpriteBatch spriteBatch)
    {
        var size = font.MeasureString(text);
        var origin = size * 0.5f;

        spriteBatch.DrawString(font, text, position, color, 0, origin, 1, SpriteEffects.None, 0);
    }
}