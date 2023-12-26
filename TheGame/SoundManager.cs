using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Audio;

namespace TheGame
{
 /// <summary>
 /// Менеджер звуков
 /// </summary>
 class SoundManager
 {
  Dictionary<string, SoundEffectInstance> m_soundList;

  public SoundManager()
  {
   m_soundList = new Dictionary<string, SoundEffectInstance>();
  }

  public void LoadContent(ContentManager content)
  {
   DirectoryInfo dir = new DirectoryInfo(System.IO.Path.Combine(content.RootDirectory, "Sound"));
   FileInfo[] files = dir.GetFiles("*.*");
   foreach (FileInfo file in files)
   {
    string key = Path.GetFileNameWithoutExtension(file.Name);
    SoundEffectInstance soundEffectInstance = content.Load<SoundEffect>(Path.Combine("Sound", key)).CreateInstance();
    m_soundList.Add(key, soundEffectInstance);
   }
  }

  public void Play(string strSoundName)
  {
   if (strSoundName != null)
   {
    SoundEffectInstance soundEffectInstance = null;
    m_soundList.TryGetValue(strSoundName, out soundEffectInstance);
    if (soundEffectInstance != null)
    {
     if (soundEffectInstance.State != SoundState.Playing)
      soundEffectInstance.Play();
    }
   }
  }

  public void Stop(string strSoundName)
  {
   if (strSoundName != null)
   {
    SoundEffectInstance soundEffectInstance = null;
    m_soundList.TryGetValue(strSoundName, out soundEffectInstance);
    if (soundEffectInstance != null)
     soundEffectInstance.Stop();
   }
  }  

  public bool IsPlaying(string strSoundName)
  {
   if (strSoundName != null)
   {
    SoundEffectInstance soundEffectInstance = null;
    m_soundList.TryGetValue(strSoundName, out soundEffectInstance);
    if (soundEffectInstance != null)
     return soundEffectInstance.State == SoundState.Playing;
   }
   return false;
  }
 }
}
