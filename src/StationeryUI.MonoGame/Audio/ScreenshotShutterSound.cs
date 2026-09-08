namespace StationeryUI.MonoGame.Audio;

using Microsoft.Xna.Framework.Audio;
using System;

/// <summary>GUIとインストーラーで共有する、動的生成のスクリーンショット用シャッター音です。</summary>
public static class ScreenshotShutterSound
{
    public static SoundEffect Create()
    {
        const int sampleRate = 44100;
        const float duration = 0.19f;
        var sampleCount = (int)(sampleRate * duration);
        var buffer = new byte[sampleCount * sizeof(short)];
        uint noiseState = 0x4B1D5EED;

        for (var i = 0; i < sampleCount; i++)
        {
            var t = i / (float)sampleRate;
            noiseState = noiseState * 1664525u + 1013904223u;
            var noise = ((noiseState >> 8) / 8388607.5f) - 1f;
            var firstClick = ShutterPulse(t, 0f, 0.034f, 68f, noise);
            var secondClick = ShutterPulse(t, 0.072f, 0.052f, 52f, -noise);
            var mechanism = t >= 0.02f
                ? MathF.Sin(MathF.Tau * (118f - 240f * (t - 0.02f)) * (t - 0.02f)) * MathF.Exp(-24f * (t - 0.02f)) * 0.22f
                : 0f;
            var wave = Math.Clamp(firstClick + secondClick + mechanism, -1f, 1f);
            var sample = (short)(wave * short.MaxValue * 0.78f);
            buffer[i * 2] = (byte)(sample & 0xff);
            buffer[i * 2 + 1] = (byte)((sample >> 8) & 0xff);
        }

        return new SoundEffect(buffer, sampleRate, AudioChannels.Mono);
    }

    private static float ShutterPulse(float time, float start, float duration, float decay, float noise)
    {
        var localTime = time - start;
        if (localTime < 0f || localTime >= duration) return 0f;
        var attack = Math.Clamp(localTime / 0.0015f, 0f, 1f);
        var envelope = attack * MathF.Exp(-decay * localTime);
        var metal = MathF.Sin(MathF.Tau * 1850f * localTime) * 0.34f +
                    MathF.Sin(MathF.Tau * 2730f * localTime) * 0.16f;
        return (noise * 0.72f + metal) * envelope;
    }
}
