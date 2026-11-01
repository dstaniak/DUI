using System;

namespace DUI.GameObjects;

/// <summary>Spawn probabilities and gameplay effects of the three collectibles.</summary>
public static class PickupRules
{
    public static PickupType ChooseType(Random random)
    {
        int roll = random.Next(100);
        if (roll < 60)
        {
            return PickupType.Beer;
        }

        return roll < 80 ? PickupType.Vodka : PickupType.Sausage;
    }

    public static decimal ApplyScore(decimal scorePermille, PickupType type)
    {
        decimal change = type switch
        {
            PickupType.Beer => 0.2m,
            PickupType.Vodka => 0.4m,
            PickupType.Sausage => -0.4m,
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
        return Math.Max(0m, scorePermille + change);
    }

    public static bool GrowsConvoy(PickupType type)
    {
        return type switch
        {
            PickupType.Beer or PickupType.Vodka => true,
            PickupType.Sausage => false,
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }
}
