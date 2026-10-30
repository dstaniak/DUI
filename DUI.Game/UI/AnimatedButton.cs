using System;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Graphics.Animation;
using Gum.Managers;
using Microsoft.Xna.Framework.Input;
using Gum.GueDeriving;
using MonoGameLibrary.Graphics;

namespace DUI.UI;

internal class AnimatedButton : Button
{
    public AnimatedButton(TextureAtlas atlas)
    {
        var buttonVisual = (ButtonVisual)Visual;
        buttonVisual.Height = 14f;
        buttonVisual.HeightUnits = DimensionUnitType.Absolute;
        buttonVisual.Width = 21f;
        buttonVisual.WidthUnits = DimensionUnitType.RelativeToChildren;

        NineSliceRuntime background = buttonVisual.Background;
        background.Texture = atlas.Texture;
        background.TextureAddress = TextureAddress.Custom;
        background.Color = Microsoft.Xna.Framework.Color.White;

        TextRuntime textInstance = buttonVisual.TextInstance;
        textInstance.Text = "START";
        textInstance.Blue = 130;
        textInstance.Green = 86;
        textInstance.Red = 70;
        textInstance.UseCustomFont = true;
        textInstance.CustomFontFile = "Fonts/04b_30.fnt";
        textInstance.FontScale = 0.25f;
        textInstance.Anchor(Gum.Wireframe.Anchor.Center);
        textInstance.Width = 0;
        textInstance.WidthUnits = DimensionUnitType.RelativeToChildren;

        TextureRegion unfocusedTextureRegion = atlas.GetRegion("unfocused-button");

        AnimationChain unfocusedAnimation = [];
        unfocusedAnimation.Name = nameof(unfocusedAnimation);
        AnimationFrame unfocusedFrame = new()
        {
            TopCoordinate = unfocusedTextureRegion.TopTextureCoordinate,
            BottomCoordinate = unfocusedTextureRegion.BottomTextureCoordinate,
            LeftCoordinate = unfocusedTextureRegion.LeftTextureCoordinate,
            RightCoordinate = unfocusedTextureRegion.RightTextureCoordinate,
            FrameLength = 0.3f,
            Texture = unfocusedTextureRegion.Texture
        };
        unfocusedAnimation.Add(unfocusedFrame);

        Animation focusedAtlasAtnimation = atlas.GetAnimation("focused-button-animation");
        AnimationChain focusedAnimation = [];
        focusedAnimation.Name = nameof(focusedAnimation);
        foreach (var region in focusedAtlasAtnimation.Frames)
        {
            AnimationFrame frame = new()
            {
                TopCoordinate = region.TopTextureCoordinate,
                BottomCoordinate = region.BottomTextureCoordinate,
                LeftCoordinate = region.LeftTextureCoordinate,
                RightCoordinate = region.RightTextureCoordinate,
                FrameLength = (float)focusedAtlasAtnimation.Delay.TotalSeconds,
                Texture = region.Texture
            };
            focusedAnimation.Add(frame);
        }

        background.AnimationChains = [unfocusedAnimation, focusedAnimation];

        buttonVisual.ButtonCategory.ResetAllStates();

        StateSave enabledState = buttonVisual.States.Enabled;
        enabledState.Apply = () =>
        {
            background.CurrentChainName = unfocusedAnimation.Name;
            background.Animate = false;
        };

        StateSave focusedState = buttonVisual.States.Focused;
        focusedState.Apply = () =>
        {
            background.CurrentChainName = focusedAnimation.Name;
            background.Animate = true;
        };

        StateSave highlightedFocusedState = buttonVisual.States.HighlightedFocused;
        highlightedFocusedState.Apply = focusedState.Apply;

        StateSave highlightedState = buttonVisual.States.Highlighted;
        highlightedState.Apply = enabledState.Apply;

        KeyDown += HandleKeyDown;
        buttonVisual.RollOn += HandleRollOn;
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
