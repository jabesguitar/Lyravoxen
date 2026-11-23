namespace Lyravoxen.Models;

/// <summary>
/// Marcação manual por toque: o operador toca uma vez por estrofe (útil em músicas playback,
/// sem voz para o reconhecimento automático). Este planejador separa a letra em estrofes e
/// distribui as linhas de cada estrofe entre um toque e o seguinte.
/// </summary>
public static class TapSyncPlanner
{
    public const int DefaultChunkLines = 4;

    /// <summary>
    /// Estrofes são separadas por linha em branco. Se a letra não tiver nenhuma linha em branco,
    /// ela é dividida em grupos de <paramref name="chunkLines"/> linhas.
    /// </summary>
    public static List<List<string>> SplitStanzas(string? lyrics, int chunkLines = DefaultChunkLines)
    {
        var stanzas = new List<List<string>>();
        if (string.IsNullOrWhiteSpace(lyrics)) return stanzas;

        var current = new List<string>();
        var normalized = lyrics.Replace("\r\n", "\n").Replace('\r', '\n');
        foreach (var raw in normalized.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0)
            {
                if (current.Count > 0)
                {
                    stanzas.Add(current);
                    current = new List<string>();
                }
                continue;
            }
            current.Add(line);
        }
        if (current.Count > 0) stanzas.Add(current);

        var size = Math.Max(1, chunkLines);
        if (stanzas.Count == 1 && stanzas[0].Count > size)
        {
            var all = stanzas[0];
            stanzas = new List<List<string>>();
            for (var i = 0; i < all.Count; i += size)
                stanzas.Add(all.Skip(i).Take(size).ToList());
        }
        return stanzas;
    }

    /// <summary>
    /// Gera uma frase por linha. A primeira linha de cada estrofe recebe o instante do toque;
    /// as demais dividem o intervalo até o toque seguinte proporcionalmente ao tamanho do texto.
    /// Só as estrofes que já têm toque entram (estrofes sem toque são ignoradas).
    /// Para a última estrofe marcada, a duração é estimada em <paramref name="secondsPerLineForLast"/> por linha.
    /// </summary>
    public static List<LyricCue> BuildCues(
        IReadOnlyList<IReadOnlyList<string>> stanzas,
        IReadOnlyList<double> starts,
        double secondsPerLineForLast = 3.0)
    {
        var cues = new List<LyricCue>();
        var count = Math.Min(stanzas.Count, starts.Count);
        for (var i = 0; i < count; i++)
        {
            var lines = stanzas[i];
            if (lines.Count == 0) continue;

            var start = Math.Max(0, starts[i]);
            var end = i + 1 < count
                ? starts[i + 1]
                : start + lines.Count * Math.Max(0.5, secondsPerLineForLast);
            var span = Math.Max(end - start, 0.05 * lines.Count);

            var weights = lines.Select(line => (double)Math.Max(1, line.Length)).ToList();
            var total = weights.Sum();
            var cumulative = 0d;
            for (var k = 0; k < lines.Count; k++)
            {
                cues.Add(new LyricCue { Seconds = start + span * cumulative / total, Text = lines[k] });
                cumulative += weights[k];
            }
        }
        return cues;
    }

    /// <summary>
    /// Rascunho de tempos SEM tocar: distribui o início de cada estrofe pela duração da música, proporcional ao tamanho do texto.
    /// <paramref name="firstStart"/> = instante em que a 1ª estrofe começa (negativo = estimar uma introdução curta).
    /// É aproximado (não sabe de solos nem de refrões repetidos): serve para começar rápido e ajustar depois.
    /// </summary>
    public static List<double> EstimateStarts(IReadOnlyList<IReadOnlyList<string>> stanzas, double durationSeconds, double firstStart = -1, double tailSeconds = -1)
    {
        var starts = new List<double>();
        if (stanzas.Count == 0 || durationSeconds <= 0) return starts;
        var first = firstStart >= 0 ? Math.Min(firstStart, durationSeconds * 0.5) : Math.Min(20, durationSeconds * 0.06);
        var tail = tailSeconds >= 0 ? tailSeconds : Math.Min(15, durationSeconds * 0.05);
        var span = Math.Max(1, durationSeconds - first - tail);
        // +25: cada estrofe "pesa" um pouco mais por causa da pausa entre elas.
        var weights = stanzas.Select(stanza => stanza.Sum(line => (double)Math.Max(1, line.Length)) + 25).ToList();
        var total = weights.Sum();
        var cumulative = 0d;
        foreach (var weight in weights)
        {
            starts.Add(first + span * cumulative / total);
            cumulative += weight;
        }
        return starts;
    }

    /// <summary>Quantidade de linhas das estrofes anteriores à de índice <paramref name="stanzaIndex"/>.</summary>
    public static int LinesBefore(IReadOnlyList<IReadOnlyList<string>> stanzas, int stanzaIndex)
        => stanzas.Take(Math.Max(0, stanzaIndex)).Sum(stanza => stanza.Count);
}
