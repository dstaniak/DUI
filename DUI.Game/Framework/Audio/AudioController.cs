using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;

namespace MonoGameLibrary.Audio;

public class AudioController : IDisposable
{
    // Keep each effect's requested volume so channel changes preserve its mix.
    private readonly List<(SoundEffectInstance Instance, float Volume)> _activeEffects = [];
    private SoundEffectInstance _songInstance;
    private float _songVolume = 1.0f;
    private float _soundEffectVolume = 1.0f;
    private float _songPlaybackRate = 1.0f;

    public bool IsMuted { get; private set; }
    public bool IsDisposed { get; private set; }

    public float SongVolume
    {
        get => IsMuted ? 0.0f : _songVolume;
        set
        {
            if (IsMuted)
                return;

            _songVolume = Math.Clamp(value, 0.0f, 1.0f);
            ApplySongVolume();
        }
    }

    public float SoundEffectVolume
    {
        get => IsMuted ? 0.0f : _soundEffectVolume;
        set
        {
            if (IsMuted)
                return;

            _soundEffectVolume = Math.Clamp(value, 0.0f, 1.0f);
            ApplySoundEffectVolumes();
        }
    }

    /// <summary>Playback speed from 0.5 to 2.0, changing pitch without restarting the song.</summary>
    public float SongPlaybackRate
    {
        get => _songPlaybackRate;
        set
        {
            if (!float.IsFinite(value) || value < 0.5f || value > 2.0f)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (_songPlaybackRate == value)
                return;

            _songPlaybackRate = value;
            if (_songInstance != null)
            {
                // MonoGame pitch uses octaves: -1 halves speed, +1 doubles it.
                _songInstance.Pitch = MathF.Log2(value);
            }
        }
    }

    public void Update()
    {
        for (int i = _activeEffects.Count - 1; i >= 0; --i)
        {
            SoundEffectInstance instance = _activeEffects[i].Instance;
            if (instance.IsDisposed || instance.State == SoundState.Stopped)
            {
                instance.Dispose();
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
        var instance = soundEffect.CreateInstance();
        instance.Volume = volume * SoundEffectVolume;
        instance.Pitch = pitch;
        instance.Pan = pan;
        instance.IsLooped = isLooped;
        instance.Play();
        _activeEffects.Add((instance, volume));
        return instance;
    }

    /// <summary>Owns the playback instance; the caller retains ownership of the sound resource.</summary>
    public void PlaySong(SoundEffect song, bool isRepeating = true)
    {
        _songInstance?.Dispose();
        _songInstance = song.CreateInstance();
        _songInstance.Volume = SongVolume;
        _songInstance.Pitch = MathF.Log2(_songPlaybackRate);
        _songInstance.IsLooped = isRepeating;
        _songInstance.Play();
    }

    public void PauseAudio()
    {
        _songInstance?.Pause();
        foreach (var effect in _activeEffects)
        {
            effect.Instance.Pause();
        }
    }

    public void ResumeAudio()
    {
        if (_songInstance?.State == SoundState.Paused)
        {
            _songInstance.Resume();
        }

        foreach (var effect in _activeEffects)
        {
            if (effect.Instance.State == SoundState.Paused)
            {
                effect.Instance.Resume();
            }
        }
    }

    public void MuteAudio()
    {
        IsMuted = true;
        ApplySongVolume();
        ApplySoundEffectVolumes();
    }

    public void UnmuteAudio()
    {
        IsMuted = false;
        ApplySongVolume();
        ApplySoundEffectVolumes();
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

        if (disposing)
        {
            // Dispose playback before the ContentManager releases its buffers.
            _songInstance?.Dispose();
            _songInstance = null;
            foreach (var effect in _activeEffects)
            {
                effect.Instance.Dispose();
            }
            _activeEffects.Clear();
        }

        IsDisposed = true;
    }

    private void ApplySongVolume()
    {
        if (_songInstance != null)
        {
            _songInstance.Volume = SongVolume;
        }
    }

    private void ApplySoundEffectVolumes()
    {
        // MasterVolume would also change music now that both use SoundEffect.
        foreach (var effect in _activeEffects)
        {
            if (!effect.Instance.IsDisposed)
            {
                effect.Instance.Volume = effect.Volume * SoundEffectVolume;
            }
        }
    }
}
