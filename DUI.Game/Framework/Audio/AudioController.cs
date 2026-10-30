using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;

namespace MonoGameLibrary.Audio;

public class AudioController : IDisposable
{
    public bool IsMuted { get; private set; }

    public float SongVolume
    {
        get => IsMuted ? 0.0f : MediaPlayer.Volume;
        set
        {
            if (IsMuted)
                return;
            MediaPlayer.Volume = Math.Clamp(value, 0.0f, 1.0f);
        }
    }

    public float SoundEffectVolume
    {
        get => IsMuted ? 0.0f : SoundEffect.MasterVolume;
        set
        {
            if (IsMuted)
                return;
            SoundEffect.MasterVolume = Math.Clamp(value, 0.0f, 1.0f);
        }
    }

    public bool IsDisposed { get; private set; }

    private readonly List<SoundEffectInstance> _activeEffects;
    private float _previousSongVolume;
    private float _previousSoundEffectVolume;

    public AudioController()
    {
        _activeEffects = [];
    }

    ~AudioController() => Dispose(false);

    public void Update()
    {
        for (int i = _activeEffects.Count - 1; i >= 0; --i)
        {
            var instance = _activeEffects[i];

            if (instance.State == SoundState.Stopped)
            {
                if (!instance.IsDisposed)
                {
                    instance.Dispose();
                }
                _activeEffects.RemoveAt(i);
            }
        }
    }

    public SoundEffectInstance PlaySoundEffect(SoundEffect soundEffect)
    {
        return PlaySoundEffect(soundEffect, 1.0f, 0.0f, 0.0f, false);
    }

    public SoundEffectInstance PlaySoundEffect(SoundEffect soundEffect, float volume, float pitch, float pan, bool isLooped)
    {
        var soundEffectInstance = soundEffect.CreateInstance();

        soundEffectInstance.Volume = volume;
        soundEffectInstance.Pitch = pitch;
        soundEffectInstance.Pan = pan;
        soundEffectInstance.IsLooped = isLooped;

        soundEffectInstance.Play();

        _activeEffects.Add(soundEffectInstance);

        return soundEffectInstance;
    }

    public void PlaySong(Song song, bool isRepeating = true)
    {
        if (MediaPlayer.State == MediaState.Playing)
        {
            MediaPlayer.Stop();
        }

        MediaPlayer.Play(song);
        MediaPlayer.IsRepeating = isRepeating;
    }

    public void PauseAudio()
    {
        MediaPlayer.Pause();

        foreach (var effect in _activeEffects)
        {
            effect.Pause();
        }
    }

    public void ResumeAudio()
    {
        MediaPlayer.Resume();

        foreach (var effect in _activeEffects)
        {
            effect.Resume();
        }
    }

    public void MuteAudio()
    {
        if (IsMuted)
            return;

        _previousSongVolume = MediaPlayer.Volume;
        _previousSoundEffectVolume = SoundEffect.MasterVolume;

        MediaPlayer.Volume = 0.0f;
        SoundEffect.MasterVolume = 0.0f;

        IsMuted = true;
    }

    public void UnmuteAudio()
    {
        if (!IsMuted)
            return;

        MediaPlayer.Volume = _previousSongVolume;
        SoundEffect.MasterVolume = _previousSoundEffectVolume;

        IsMuted = false;
    }

    public void ToggleMute()
    {
        if (IsMuted)
        {
            UnmuteAudio();
        }
        else
        {
            MuteAudio();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected void Dispose(bool disposing)
    {
        if (IsDisposed)
            return;

        if (disposing) // Explicit dispose of managed resources on user's Dispose() call.
        {
            foreach (var effect in _activeEffects)
            {
                effect.Dispose();
            }
            _activeEffects.Clear();
        }

        IsDisposed = true;
    }
}
