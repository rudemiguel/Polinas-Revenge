using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Content;
using System.Linq;

namespace TheGame;


/// <summary>
/// Менеджер музыки
/// </summary>
class MusicManager
{
    private static readonly Random Random = new Random();
    private Dictionary<string, List<Song>> m_musicCategoryList;
    private string m_strCurCategory;

    public MusicManager()
    {
        m_musicCategoryList = new Dictionary<string, List<Song>>();
        MediaPlayer.MediaStateChanged += new EventHandler<EventArgs>(MediaPlayer_MediaStateChanged);
    }

    void MediaPlayer_MediaStateChanged(object sender, EventArgs e)
    {
        if (MediaPlayer.State == MediaState.Stopped)
        {
            if (m_strCurCategory != null)
                PlayCategory(m_strCurCategory);
        }
    }

    public void LoadContent(ContentManager content, IList<string> assets)
    {
        foreach (var asset in assets)
        {
            if (!asset.Split(Path.DirectorySeparatorChar).Contains("Music"))
                continue;

            string key = Path.GetFileNameWithoutExtension(asset);
            string strFile = Path.Combine("Music", key);
            int n = key.IndexOfAny(new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' });
            string strCategory = key;
            if (n > -1)
                strCategory = key.Substring(0, n);

            List<Song> songList = null;
            m_musicCategoryList.TryGetValue(strCategory, out songList);
            if (songList == null)
            {
                songList = new List<Song>();
                m_musicCategoryList.Add(strCategory, songList);
            }
            try
            {
                Song song = content.Load<Song>(strFile);
                songList.Add(song);
            }
            catch (NotSupportedException)
            {
            }
        }
    }

    public void PlayCategory(String strCategory)
    {
        MediaPlayer.Stop();
        List<Song> songList = null;
        m_musicCategoryList.TryGetValue(strCategory, out songList);
        if (songList != null)
        {
            if (songList.Count > 0)
            {
                MediaPlayer.Play(songList[Random.Next(songList.Count - 1)]);
                MediaPlayer.Volume = 1f;
                m_strCurCategory = strCategory;
            }
        }
    }
}
