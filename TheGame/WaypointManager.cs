using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;


namespace TheGame
{
 /// <summary>
 /// Менеджер точек
 /// </summary>
 class WaypointManager
 {
  Dictionary<string,Vector2> m_pointList;

  public WaypointManager()
  {
   m_pointList = new Dictionary<string, Vector2>();
  }

  public void AddWaypoint(String strName, Vector2 pos)
  {
   m_pointList.Add(strName, pos);
  }

  public void Update(GameTime gameTime)
  {
  }

  public bool Check(GameObject gameObject, String strWaypointName)
  {
   Rectangle r = gameObject.Bounds;
   Vector2 pos = gameObject.Position;
   Vector2 v;
   m_pointList.TryGetValue(strWaypointName, out v);
   if (v != null)
   {
    if ((v.X > pos.X) && (v.X < pos.X + r.Width) && (v.Y > pos.Y) && (v.Y < pos.Y + r.Height))
     return true;
   }
   return false;
  }


 }
}
