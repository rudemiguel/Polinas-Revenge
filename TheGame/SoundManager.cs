using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Audio;
using System;
using System.Linq;

namespace TheGame;

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

    public void LoadContent(ContentManager content, IList<string> assets)
    {
        foreach (var asset in assets)
        {
            if (!asset.Split(Path.DirectorySeparatorChar).Contains("Sound"))
                continue;
            
            string key = Path.GetFileNameWithoutExtension(asset);
            string strFile = Path.Combine("Sound", key);
            var soundEffectInstance = content.Load<SoundEffect>(strFile).CreateInstance();
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
