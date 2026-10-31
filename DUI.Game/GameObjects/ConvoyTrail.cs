using System.Collections.Generic;

namespace DUI.GameObjects;

/// <summary>Distance-indexed head poses, retaining only the convoy and one spare car length.</summary>
internal sealed class ConvoyTrail
{
    private readonly List<CarPose> _samples = [];
    private int _first;

    internal int Count => _samples.Count - _first;

    public ConvoyTrail(CarPose start)
    {
        _samples.Add(start);
    }

    public void Append(CarPose pose)
    {
        _samples.Add(pose);
    }

    public CarPose Sample(double distance)
    {
        CarPose oldest = _samples[_first];
        if (distance <= oldest.Distance)
        {
            // Before enough driving history exists, grow backwards along the oldest tangent.
            return new CarPose(oldest.Center + oldest.Direction * (float)(distance - oldest.Distance),
                oldest.Rotation, distance);
        }

        int left = _first;
        int right = _samples.Count - 1;
        if (distance >= _samples[right].Distance)
        {
            return _samples[right];
        }

        while (right - left > 1)
        {
            int middle = left + (right - left) / 2;
            if (_samples[middle].Distance <= distance)
            {
                left = middle;
            }
            else
            {
                right = middle;
            }
        }

        return CarPose.Interpolate(_samples[left], _samples[right], distance);
    }

    public void TrimBefore(double distance)
    {
        // Retain a bracketing sample for interpolation. Compact in batches, not every tick.
        while (_first + 1 < _samples.Count && _samples[_first + 1].Distance < distance)
        {
            _first++;
        }

        if (_first >= 256 && _first >= _samples.Count / 2)
        {
            _samples.RemoveRange(0, _first);
            _first = 0;
        }
    }
}
