using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;


namespace TheGame
{
 /// <summary>
 /// Менеджер воды
 /// </summary>
 class WaterManager
 {
  private List<Rectangle?> m_waterRegionList;
  
  public WaterManager(xTile.Layers.Layer layer)
  {
   m_waterRegionList = new List<Rectangle?>();
   for (int x = 0; x < layer.LayerWidth; x++)
   {
    Rectangle? r = null;
    for (int y = 0; y < layer.LayerHeight; y++)
    {
     xTile.Tiles.Tile tile = layer.Tiles[x, y];
     if (tile != null)
     {
      if (r==null)
       r = new Rectangle(x*layer.TileWidth,y*layer.TileHeight, layer.TileWidth, layer.TileHeight);
      else
       r = new Rectangle(r.Value.X, r.Value.Y, r.Value.Width, r.Value.Height + layer.TileHeight);
     }
     else
     {
      if (r!=null)
       m_waterRegionList.Add(r);
      r = null;
     }
    }
    if ( r!=null)
     m_waterRegionList.Add(r);
   }
  }

  public bool Check(GameObject gameObject)
  {
   Vector2 pos = gameObject.Position;
   Rectangle bounds = gameObject.Bounds;
   bounds.X = (int)pos.X - bounds.Width/2;
   bounds.Y = (int)pos.Y - bounds.Height/2;
   foreach (Rectangle? r in m_waterRegionList)
   {
    if (r.Value.Intersects(bounds))
     return true;
   }
   return false;
  }
 }

}
