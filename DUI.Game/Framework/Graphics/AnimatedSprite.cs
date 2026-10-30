using System;
using Microsoft.Xna.Framework;

namespace MonoGameLibrary.Graphics;

public class AnimatedSprite : Sprite
{
    private int _currentFrame;
    private TimeSpan _elapsed;
    private Animation _animation;

    public AnimatedSprite()
    {
    }

    public AnimatedSprite(Animation animation)
    {
        _animation = animation;
        Region = _animation.Frames[0];
    }

    public void Update(GameTime gameTime)
    {
        if (_animation.Delay.Milliseconds == 0)
            return;

        _elapsed += gameTime.ElapsedGameTime;
        int totalFrames = _elapsed.Milliseconds / _animation.Delay.Milliseconds;

        if (totalFrames > 0)
        {
            _elapsed -= TimeSpan.FromMilliseconds(_animation.Delay.Milliseconds * totalFrames);
            _currentFrame = (_currentFrame + totalFrames) % _animation.Frames.Count;
            Region = _animation.Frames[_currentFrame];
        }
        else if (Region == null)
        {
            Region = _animation.Frames[_currentFrame];
        }

        /* 
        Loop version, more accurate than milliseconds.

        bool updateRegion = false;
        while (_elapsed >= _animation.Delay)
        {
            _currentFrame = (_currentFrame + 1) % _animation.Frames.Count;
            _elapsed -= _animation.Delay;
            updateRegion = true;
        } 

        if (updateRegion)
            Region = _animation.Frames[_currentFrame];
        */
    }
}
