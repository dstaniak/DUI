using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DUI.GameObjects;

/// <summary>Short-lived beer droplets and foam; owns its one-pixel drawing texture.</summary>
public sealed class BeerBurst : IDisposable
{
    private const int ParticlesPerBurst = 44;
    private const int MaximumParticles = ParticlesPerBurst * 4;
    private static readonly Color s_outline = new(42, 26, 30);

    private readonly Texture2D _pixel;
    private readonly List<Particle> _particles = new(MaximumParticles);

    public BeerBurst(GraphicsDevice graphicsDevice)
    {
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    public void Emit(Vector2 center)
    {
        int excess = _particles.Count + ParticlesPerBurst - MaximumParticles;
        if (excess > 0)
        {
            _particles.RemoveRange(0, excess);
        }

        for (int index = 0; index < ParticlesPerBurst; index++)
        {
            bool isFoam = index % 3 == 0;
            float angle = Random.Shared.NextSingle() * MathF.Tau;
            Vector2 direction = new(MathF.Cos(angle), MathF.Sin(angle));
            float speed = isFoam ? 55 + Random.Shared.NextSingle() * 100 : 110 + Random.Shared.NextSingle() * 190;
            _particles.Add(new Particle
            {
                Position = center + direction * 8,
                Velocity = direction * speed,
                Lifetime = isFoam ? 0.75f : 0.5f + Random.Shared.NextSingle() * 0.2f,
                Size = isFoam ? 12 + Random.Shared.NextSingle() * 10 : 4 + Random.Shared.NextSingle() * 5,
                Color = isFoam ? new Color(255, 248, 216) : new Color(255, 166 + Random.Shared.Next(65), 25),
                IsFoam = isFoam
            });
        }
    }

    public void Update(GameTime gameTime)
    {
        float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
        for (int index = _particles.Count - 1; index >= 0; index--)
        {
            Particle particle = _particles[index];
            particle.Age += elapsed;
            if (particle.Age >= particle.Lifetime)
            {
                _particles.RemoveAt(index);
                continue;
            }

            particle.Position += particle.Velocity * elapsed;
            particle.Velocity *= MathF.Exp(-2.0f * elapsed);
            particle.Velocity.Y += (particle.IsFoam ? -35 : 140) * elapsed;
            _particles[index] = particle;
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        foreach (Particle particle in _particles)
        {
            float progress = particle.Age / particle.Lifetime;
            float opacity = 1 - progress * progress;
            Color fill = WithOpacity(particle.Color, opacity);
            Color outline = WithOpacity(s_outline, opacity);
            float size = particle.Size * (particle.IsFoam ? 1 + progress * 0.5f : 1 - progress * 0.4f);

            if (particle.IsFoam)
            {
                // Overlapping pixel rectangles form round foam puffs. The dark
                // edge keeps white foam legible over the tutorial's pale floor.
                DrawFoam(spriteBatch, particle.Position, size + 3, outline);
                DrawFoam(spriteBatch, particle.Position, size, fill);
            }
            else
            {
                float rotation = MathF.Atan2(particle.Velocity.Y, particle.Velocity.X);
                DrawDrop(spriteBatch, particle.Position, new Vector2(size * 2 + 2, size + 2), rotation, outline);
                DrawDrop(spriteBatch, particle.Position, new Vector2(size * 2, size), rotation, fill);
            }
        }
    }

    public void Clear()
    {
        _particles.Clear();
    }

    public void Dispose()
    {
        _pixel.Dispose();
    }

    private void DrawFoam(SpriteBatch spriteBatch, Vector2 position, float size, Color color)
    {
        DrawDrop(spriteBatch, position, new Vector2(size, size * 0.6f), 0, color);
        DrawDrop(spriteBatch, position, new Vector2(size * 0.6f, size), 0, color);
    }

    private void DrawDrop(SpriteBatch spriteBatch, Vector2 position, Vector2 scale, float rotation, Color color)
    {
        spriteBatch.Draw(_pixel, position, null, color, rotation,
            new Vector2(0.5f), scale, SpriteEffects.None, 0);
    }

    private static Color WithOpacity(Color color, float opacity)
    {
        // The scene uses straight alpha, so fade alpha without darkening RGB.
        color.A = (byte)(255 * opacity);
        return color;
    }

    private struct Particle
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public Color Color;
        public float Size;
        public float Age;
        public float Lifetime;
        public bool IsFoam;
    }
}
