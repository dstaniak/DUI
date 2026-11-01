using System;
using DUI.GameObjects;
using Assert = DUI.Tests.MovementTests;

namespace DUI.Tests;

internal static class PickupTests
{
    public static readonly (string Name, Action Run)[] Cases =
    [
        ("Pickup rolls cover exactly 50/20/5/5/20 percent", Probabilities),
        ("Mixed pickups preserve scores and grow only after drinks or powder", MixedPickups),
        ("Sausage clamps the score to zero without shrinking the convoy", SausageFloor)
    ];

    private static void Probabilities()
    {
        int beer = 0;
        int vodka = 0;
        int powder = 0;
        int mushroom = 0;
        int sausage = 0;
        for (int roll = 0; roll < 100; roll++)
        {
            switch (PickupRules.ChooseType(new FixedRoll(roll)))
            {
                case PickupType.Beer:
                    beer++;
                    break;
                case PickupType.Vodka:
                    vodka++;
                    break;
                case PickupType.Sausage:
                    sausage++;
                    break;
                case PickupType.Powder:
                    powder++;
                    break;
                case PickupType.Mushroom:
                    mushroom++;
                    break;
            }
        }
        Assert.Check(beer == 50 && vodka == 20 && powder == 5 && mushroom == 5 && sausage == 20);
    }

    private static void MixedPickups()
    {
        decimal score = 0m;
        int cars = 1;
        var sequence = new[]
        {
            (PickupType.Beer, 0.2m, 2),
            (PickupType.Vodka, 0.6m, 3),
            (PickupType.Sausage, 0.2m, 3),
            (PickupType.Beer, 0.4m, 4),
            (PickupType.Sausage, 0m, 4),
            (PickupType.Powder, 0m, 5),
            (PickupType.Mushroom, 0m, 5),
            (PickupType.Vodka, 0.4m, 6),
            (PickupType.Powder, 0.4m, 7),
            (PickupType.Mushroom, 0.4m, 7)
        };

        foreach (var (type, expectedScore, expectedCars) in sequence)
        {
            score = PickupRules.ApplyScore(score, type);
            if (PickupRules.GrowsConvoy(type))
            {
                cars++;
            }
            Assert.Check(score == expectedScore && cars == expectedCars);
        }
    }

    private static void SausageFloor()
    {
        foreach (decimal score in new[] { 0m, 0.1m, 0.2m, 0.4m })
        {
            Assert.Check(PickupRules.ApplyScore(score, PickupType.Sausage) == 0m);
        }
        Assert.Check(PickupRules.ApplyScore(1m, PickupType.Sausage) == 0.6m);
        Assert.Check(!PickupRules.GrowsConvoy(PickupType.Sausage));
    }

    private sealed class FixedRoll(int roll) : Random
    {
        public override int Next(int maxValue)
        {
            Assert.Check(maxValue == 100);
            return roll;
        }
    }
}
