using Lyravoxen.Models;

static void Check(bool condition, string name)
{
    if (!condition) throw new Exception("Falha: " + name);
    Console.WriteLine("OK: " + name);
}

var normalized = CueTimeline.Normalize(new[]
{
    new LyricCue { Seconds = 12, Text = "segunda" },
    new LyricCue { Seconds = 3, Text = "primeira" },
    new LyricCue { Seconds = 7, Text = "meio" }
});
Check(normalized.Select(c => c.Seconds).SequenceEqual(new[] { 3d, 7d, 12d }), "ordenação cronológica");
Check(CueTimeline.FindActiveIndex(normalized, 0, 1.5) == -1, "antes da primeira frase");
Check(CueTimeline.FindActiveIndex(normalized, 1.5, 1.5) == 0, "antecipação de 1,5 segundo");
Check(CueTimeline.FindActiveIndex(normalized, 5.5, 1.5) == 1, "troca para frase seguinte");
Check(CueTimeline.FindActiveIndex(normalized, 20, 1.5) == 2, "depois da última frase");
Check(CueTimeline.FindActiveIndex(normalized, 5.5, 1.5) == 1, "seek para trás recalcula frase sem estado residual");
Check(CueTimeline.FindActiveIndex(normalized, double.NaN, 1.5) == -1, "tempo inválido não projeta frase");
var blockCues = Enumerable.Range(1, 7)
    .Select(index => new LyricCue { Seconds = index * 2, Text = "linha " + index })
    .ToList();
Check(CueTimeline.BuildProjectionBlock(blockCues, 0, 4) == "linha 1", "bloco de quatro começa com uma linha");
Check(CueTimeline.BuildProjectionBlock(blockCues, 3, 4) == string.Join(Environment.NewLine, "linha 1", "linha 2", "linha 3", "linha 4"), "bloco de quatro acumula quatro linhas");
Check(CueTimeline.BuildProjectionBlock(blockCues, 4, 4) == "linha 5", "novo bloco substitui o bloco anterior");
Check(CueTimeline.BuildProjectionBlock(blockCues, 6, 5) == string.Join(Environment.NewLine, "linha 6", "linha 7"), "último bloco parcial mantém apenas linhas restantes");
Check(CueTimeline.BuildProjectionBlock(blockCues, 6, 0) == "linha 7", "limite inferior de linhas é um");
Check(CueTimeline.BuildProjectionBlock(blockCues, 6, 99) == string.Join(Environment.NewLine, blockCues.Select(cue => cue.Text)), "limite superior de linhas é dez");
Check(CueTimeline.BuildProjectionBlock(blockCues, -1, 4) == "", "índice inválido limpa projeção");

Console.WriteLine("Todos os testes da linha do tempo passaram.");