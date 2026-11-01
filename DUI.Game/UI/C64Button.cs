using System;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.GueDeriving;
using Gum.Managers;
using MonoGameLibrary.Graphics;

namespace DUI.UI;

/// <summary>A square C64 button with inverse-video focus, hover and press states.</summary>
internal sealed class C64Button : Button
{
    public C64Button()
    {
        var visual = (ButtonVisual)Visual;
        TextRuntime label = visual.TextInstance;

        // Keep Gum's input and state handling, but replace every default visual.
        visual.Children.Clear();
        visual.HeightUnits = DimensionUnitType.Absolute;
        visual.Height = 14f;
        visual.WidthUnits = DimensionUnitType.RelativeToChildren;
        visual.Width = 12f;
        RectangleRuntime background = C64Theme.AddFrame(visual, 1f);
        C64Theme.ApplyFont(label);
        label.WidthUnits = DimensionUnitType.RelativeToChildren;
        label.Width = 0;
        label.Anchor(Gum.Wireframe.Anchor.Center);
        visual.AddChild(label);

        visual.ButtonCategory.ResetAllStates();
        foreach (StateSave state in visual.ButtonCategory.States)
        {
            bool disabled = state.Name.Contains("Disabled", StringComparison.Ordinal);
            bool pressed = state.Name.Contains("Pushed", StringComparison.Ordinal);
            bool selected = state.Name.Contains("Focused", StringComparison.Ordinal) ||
                state.Name.Contains("Highlighted", StringComparison.Ordinal);

            state.Apply = () =>
            {
                background.FillColor = pressed ? C64Palette.White :
                    selected && !disabled ? C64Palette.LightBlue : C64Palette.Blue;
                label.Color = disabled ? C64Palette.LightGray :
                    pressed || selected ? C64Palette.Blue : C64Palette.White;
            };
        }

        KeyDown += HandleKeyDown;
        visual.RollOn += HandleRollOn;
    }

    private void HandleKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Gum.Forms.Input.Keys.Left:
                HandleTab(TabDirection.Up, loop: true);
                break;
            case Gum.Forms.Input.Keys.Right:
                HandleTab(TabDirection.Down, loop: true);
                break;
        }
    }

    private void HandleRollOn(object sender, EventArgs e)
    {
        IsFocused = true;
    }
}
