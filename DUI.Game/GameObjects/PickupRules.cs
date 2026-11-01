using System;

namespace DUI.GameObjects;

/// <summary>Spawn probabilities, score changes and growth for all collectibles.</summary>
public static class PickupRules
{
    public static PickupType ChooseType(Random random)
    {
        int roll = random.Next(100);
        if (roll < 50)
        {
            return PickupType.Beer;
        }

        if (roll < 70)
        {
            return PickupType.Vodka;
        }

        if (roll < 75)
        {
            return PickupType.Powder;
        }

        return roll < 80 ? PickupType.Mushroom : PickupType.Sausage;
    }

    public static decimal ApplyScore(decimal scorePermille, PickupType type)
    {
        decimal change = type switch
        {
            PickupType.Beer => 0.2m,
            PickupType.Vodka => 0.4m,
            PickupType.Sausage => -0.4m,
            PickupType.Powder or PickupType.Mushroom => 0m,
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
        return Math.Max(0m, scorePermille + change);
    }

    public static bool GrowsConvoy(PickupType type)
    {
        return type switch
        {
            PickupType.Beer or PickupType.Vodka or PickupType.Powder => true,
            PickupType.Sausage or PickupType.Mushroom => false,
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }
}
