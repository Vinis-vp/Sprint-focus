using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SprintFocus.Services;

public enum SoundCue
{
    /// <summary>Fim de uma sessão de foco: três notas subindo.</summary>
    FocusEnded,

    /// <summary>Fim de uma pausa: duas notas, chamando de volta ao foco.</summary>
    BreakEnded,
}

/// <summary>
/// Toca sons curtos usando o player de áudio do próprio sistema.
/// Os sons são sintetizados em WAV na primeira vez e guardados numa pasta temporária.
/// </summary>
public class SoundPlayer
{
    private const int SampleRate = 44100;

    private readonly string _cacheDir = Path.Combine(Path.GetTempPath(), "SprintFocus", "sounds");
    private readonly (string Command, string ArgsFormat)? _player = FindPlayer();

    public void Play(SoundCue cue)
    {
        if (_player is not { } player)
            return;

        try
        {
            var file = EnsureSoundFile(cue);
            // Dispose só libera o handle; o player continua tocando em segundo plano.
            Process.Start(new ProcessStartInfo
            {
                FileName = player.Command,
                Arguments = string.Format(player.ArgsFormat, file),
                UseShellExecute = false,
                CreateNoWindow = true,
            })?.Dispose();
        }
        catch (Exception ex)
        {
            // Som é um extra: se falhar, o timer continua funcionando normalmente.
            Trace.WriteLine($"[SoundPlayer] Falha ao tocar {cue}: {ex.Message}");
        }
    }

    private string EnsureSoundFile(SoundCue cue)
    {
        var path = Path.Combine(_cacheDir, $"{cue}.wav");
        if (File.Exists(path))
            return path;

        Directory.CreateDirectory(_cacheDir);

        // Frequências em Hz: C6, E6, G6 (acorde maior subindo) e G5, C6.
        double[] notes = cue == SoundCue.FocusEnded
            ? [1046.5, 1318.5, 1568.0]
            : [784.0, 1046.5];

        File.WriteAllBytes(path, SynthesizeChime(notes));
        return path;
    }

    /// <summary>
    /// Gera um "sininho": cada nota é uma senoide com um harmônico suave e decaimento
    /// exponencial, começando um pouco depois da anterior para soar como arpejo.
    /// </summary>
    private static byte[] SynthesizeChime(double[] notes)
    {
        const double noteSpacing = 0.16; // segundos entre o início de cada nota
        const double noteLength = 0.9;   // duração do decaimento de cada nota
        const double volume = 0.35;

        var totalSeconds = noteSpacing * (notes.Length - 1) + noteLength;
        var samples = new short[(int)(totalSeconds * SampleRate)];

        for (var n = 0; n < notes.Length; n++)
        {
            var start = (int)(n * noteSpacing * SampleRate);
            for (var i = 0; i < noteLength * SampleRate && start + i < samples.Length; i++)
            {
                var t = (double)i / SampleRate;
                var envelope = Math.Exp(-5 * t) * Math.Min(1, t / 0.005); // ataque de 5 ms evita "clique"
                var wave = Math.Sin(2 * Math.PI * notes[n] * t)
                         + 0.3 * Math.Sin(2 * Math.PI * notes[n] * 2 * t);
                var value = samples[start + i] + volume / notes.Length * envelope * wave * short.MaxValue;
                samples[start + i] = (short)Math.Clamp(value, short.MinValue, short.MaxValue);
            }
        }

        return ToWav(samples);
    }

    /// <summary>Monta um WAV PCM 16 bits mono (cabeçalho RIFF de 44 bytes + amostras).</summary>
    private static byte[] ToWav(short[] samples)
    {
        const short channels = 1;
        const short bitsPerSample = 16;
        var dataSize = samples.Length * sizeof(short);

        using var stream = new MemoryStream(44 + dataSize);
        using var writer = new BinaryWriter(stream);

        writer.Write("RIFF"u8);
        writer.Write(36 + dataSize);
        writer.Write("WAVE"u8);

        writer.Write("fmt "u8);
        writer.Write(16);                                         // tamanho do bloco fmt
        writer.Write((short)1);                                   // PCM
        writer.Write(channels);
        writer.Write(SampleRate);
        writer.Write(SampleRate * channels * bitsPerSample / 8); // bytes por segundo
        writer.Write((short)(channels * bitsPerSample / 8));     // bytes por amostra
        writer.Write(bitsPerSample);

        writer.Write("data"u8);
        writer.Write(dataSize);
        foreach (var sample in samples)
            writer.Write(sample);

        return stream.ToArray();
    }

    private static (string, string)? FindPlayer()
    {
        if (OperatingSystem.IsWindows())
            return ("powershell", "-NoProfile -Command \"(New-Object Media.SoundPlayer '{0}').PlaySync()\"");

        if (OperatingSystem.IsMacOS())
            return ("afplay", "\"{0}\"");

        // Linux: PipeWire, PulseAudio ou ALSA, nesta ordem de preferência.
        string[] candidates = ["pw-play", "paplay", "aplay"];
        var found = candidates.FirstOrDefault(IsOnPath);
        return found is null ? null : (found, found == "aplay" ? "-q \"{0}\"" : "\"{0}\"");
    }

    private static bool IsOnPath(string command) =>
        (Environment.GetEnvironmentVariable("PATH") ?? "")
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Any(dir => File.Exists(Path.Combine(dir, command)));
}
