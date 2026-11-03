using System;
using System.Collections.Generic;
using System.Diagnostics;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Content;

namespace MonoGameLibrary.Graphics;

/// <summary>
/// Owns a watched shader, its parameter lookup, and optional ImGui inspector.
/// All operations, including disposal and reload, run on the game thread.
/// </summary>
public sealed class Material : IDisposable
{
    private static readonly HashSet<Material> s_materials = new();
    private static readonly HashSet<Material> s_debugMaterials = new();
    private readonly Dictionary<string, EffectParameter> _parameters = new(StringComparer.Ordinal);
    private bool _disposed;

    public WatchedAsset<Effect> Asset { get; }
    public Effect Effect => Asset.Asset;
    public IReadOnlyDictionary<string, EffectParameter> ParameterMap => _parameters;
    public bool DebugOverride { get; set; }
    public bool IsDebugVisible
    {
        get => s_debugMaterials.Contains(this);
        set
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (value)
            {
                s_debugMaterials.Add(this);
            }
            else
            {
                s_debugMaterials.Remove(this);
            }
        }
    }

    public Material(WatchedAsset<Effect> asset)
    {
        ArgumentNullException.ThrowIfNull(asset);
        Asset = asset;
        UpdateParameterCache();
        s_materials.Add(this);
    }

    [Conditional("DEBUG")]
    public void Update()
    {
        if (!Asset.TryRefresh(out Effect previous))
        {
            return;
        }

        try
        {
            UpdateParameterCache();
            foreach (EffectParameter source in previous.Parameters)
            {
                if (TryGetParameter(source.Name, out EffectParameter target))
                {
                    CopyParameter(source, target);
                }
            }
        }
        finally
        {
            Asset.ReleasePreviousAsset();
        }
    }

    public void UpdateParameterCache()
    {
        _parameters.Clear();
        foreach (EffectParameter parameter in Effect.Parameters)
        {
            _parameters.Add(parameter.Name, parameter);
        }
    }

    public bool TryGetParameter(string name, out EffectParameter parameter)
    {
        return _parameters.TryGetValue(name, out parameter);
    }

    public void SetParameter(string name, float value)
    {
        if (!DebugOverride && TryGetParameter(name, out EffectParameter parameter))
        {
            parameter.SetValue(value);
        }
    }

    public void SetParameter(string name, Vector2 value)
    {
        if (!DebugOverride && TryGetParameter(name, out EffectParameter parameter))
        {
            parameter.SetValue(value);
        }
    }

    public void SetParameter(string name, Vector3 value)
    {
        if (!DebugOverride && TryGetParameter(name, out EffectParameter parameter))
        {
            parameter.SetValue(value);
        }
    }

    public void SetParameter(string name, Vector4 value)
    {
        if (!DebugOverride && TryGetParameter(name, out EffectParameter parameter))
        {
            parameter.SetValue(value);
        }
    }

    public void SetParameter(string name, Matrix value)
    {
        if (!DebugOverride && TryGetParameter(name, out EffectParameter parameter))
        {
            parameter.SetValue(value);
        }
    }

    public void SetParameter(string name, Texture2D value)
    {
        // Resource bindings must remain live even when numeric values are frozen.
        if (TryGetParameter(name, out EffectParameter parameter))
        {
            parameter.SetValue(value);
        }
    }

    [Conditional("DEBUG")]
    public static void UpdateWatchedMaterials()
    {
        foreach (Material material in s_materials)
        {
            material.Update();
        }
    }

    [Conditional("DEBUG")]
    public static void DrawVisibleDebugUi()
    {
        int columns = Math.Max(1, (int)((ImGui.GetIO().DisplaySize.X - 40) / 390));
        int index = 0;
        foreach (Material material in s_debugMaterials)
        {
            // Give each inspector a visible initial position; later positions
            // belong to the developer and are not reset on subsequent frames.
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(
                20 + index % columns * 390, 60 + index / columns * 320), ImGuiCond.FirstUseEver);
            material.DrawDebug();
            index++;
        }
    }

    [Conditional("DEBUG")]
    public void DrawDebug()
    {
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(370, 300), ImGuiCond.FirstUseEver);
        bool expanded = ImGui.Begin(Asset.AssetName);
        if (expanded)
        {
            ImGui.TextUnformatted($"Last updated: {Asset.UpdatedAt.LocalDateTime:T}");
            if (Asset.LastReloadError != null)
            {
                ImGui.TextWrapped(Asset.LastReloadError);
            }

            bool debugOverride = DebugOverride;
            if (ImGui.Checkbox("Override values", ref debugOverride))
            {
                DebugOverride = debugOverride;
            }

            ImGui.TextWrapped("Override values freezes game-driven numeric parameters. F12 hides the inspector.");
            foreach (EffectParameter parameter in _parameters.Values)
            {
                DrawParameter(parameter);
            }
        }

        ImGui.End();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        s_debugMaterials.Remove(this);
        s_materials.Remove(this);
        Asset.Dispose();
        _disposed = true;
    }

    private static void CopyParameter(EffectParameter source, EffectParameter target)
    {
        // A shader edit may change a parameter's shape or type. In that case
        // retain the new shader's default instead of writing incompatible data.
        if (source.ParameterType != target.ParameterType || source.ParameterClass != target.ParameterClass ||
            source.RowCount != target.RowCount || source.ColumnCount != target.ColumnCount ||
            source.Elements.Count != target.Elements.Count)
        {
            return;
        }

        if (source.Elements.Count > 0)
        {
            for (int i = 0; i < source.Elements.Count; i++)
            {
                CopyParameter(source.Elements[i], target.Elements[i]);
            }

            return;
        }

        switch (source.ParameterType, source.ParameterClass)
        {
            case (EffectParameterType.Bool, EffectParameterClass.Scalar):
                target.SetValue(source.GetValueBoolean());
                break;
            case (EffectParameterType.Int32, EffectParameterClass.Scalar):
                target.SetValue(source.GetValueInt32());
                break;
            case (EffectParameterType.Single, EffectParameterClass.Scalar):
                target.SetValue(source.GetValueSingle());
                break;
            case (EffectParameterType.Single, EffectParameterClass.Vector) when source.ColumnCount == 2:
                target.SetValue(source.GetValueVector2());
                break;
            case (EffectParameterType.Single, EffectParameterClass.Vector) when source.ColumnCount == 3:
                target.SetValue(source.GetValueVector3());
                break;
            case (EffectParameterType.Single, EffectParameterClass.Vector) when source.ColumnCount == 4:
                target.SetValue(source.GetValueVector4());
                break;
            case (EffectParameterType.Single, EffectParameterClass.Matrix)
                when source.RowCount == 4 && source.ColumnCount == 4:
                target.SetValue(source.GetValueMatrix());
                break;
            case (EffectParameterType.Texture2D, EffectParameterClass.Object):
                target.SetValue(source.GetValueTexture2D());
                break;
        }
    }

    private static void DrawParameter(EffectParameter parameter)
    {
        string name = parameter.Name;
        if (parameter.Elements.Count > 0)
        {
            ImGui.TextUnformatted($"{name}: array ({parameter.Elements.Count} elements)");
            return;
        }

        switch (parameter.ParameterType, parameter.ParameterClass)
        {
            case (EffectParameterType.Single, EffectParameterClass.Scalar):
                float scalar = parameter.GetValueSingle();
                if (ImGui.DragFloat(name, ref scalar, 0.005f))
                {
                    parameter.SetValue(scalar);
                }

                break;
            case (EffectParameterType.Single, EffectParameterClass.Vector):
                DrawVector(parameter);
                break;
            case (EffectParameterType.Bool, EffectParameterClass.Scalar):
                bool boolean = parameter.GetValueBoolean();
                if (ImGui.Checkbox(name, ref boolean))
                {
                    parameter.SetValue(boolean);
                }

                break;
            case (EffectParameterType.Int32, EffectParameterClass.Scalar):
                int integer = parameter.GetValueInt32();
                if (ImGui.InputInt(name, ref integer))
                {
                    parameter.SetValue(integer);
                }

                break;
            case (EffectParameterType.Texture2D, EffectParameterClass.Object):
                Texture2D texture = parameter.GetValueTexture2D();
                ImGui.TextUnformatted(name);
                if (texture != null && !texture.IsDisposed)
                {
                    float scale = Math.Min(1f, 300f / Math.Max(texture.Width, texture.Height));
                    ImGui.Image(Core.ImGuiRenderer.BindTexture(texture),
                        new System.Numerics.Vector2(texture.Width * scale, texture.Height * scale));
                }

                break;
            default:
                ImGui.TextUnformatted($"{name}: {parameter.ParameterType} {parameter.RowCount}×{parameter.ColumnCount}");
                break;
        }
    }

    private static void DrawVector(EffectParameter parameter)
    {
        switch (parameter.ColumnCount)
        {
            case 2:
                Vector2 value2 = parameter.GetValueVector2();
                var vector2 = new System.Numerics.Vector2(value2.X, value2.Y);
                if (ImGui.DragFloat2(parameter.Name, ref vector2, 0.005f))
                {
                    parameter.SetValue(new Vector2(vector2.X, vector2.Y));
                }

                break;
            case 3:
                Vector3 value3 = parameter.GetValueVector3();
                var vector3 = new System.Numerics.Vector3(value3.X, value3.Y, value3.Z);
                if (ImGui.DragFloat3(parameter.Name, ref vector3, 0.005f))
                {
                    parameter.SetValue(new Vector3(vector3.X, vector3.Y, vector3.Z));
                }

                break;
            case 4:
                Vector4 value4 = parameter.GetValueVector4();
                var vector4 = new System.Numerics.Vector4(value4.X, value4.Y, value4.Z, value4.W);
                if (ImGui.DragFloat4(parameter.Name, ref vector4, 0.005f))
                {
                    parameter.SetValue(new Vector4(vector4.X, vector4.Y, vector4.Z, vector4.W));
                }

                break;
        }
    }
}
