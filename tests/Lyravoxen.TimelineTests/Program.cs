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

var tenLines = Enumerable.Range(1, 10)
    .Select(index => new LyricCue { Seconds = index * 2, Text = "linha " + index })
    .ToList();
Check(CueTimeline.BuildProjectionBlock(tenLines, 0, 4) == string.Join(Environment.NewLine, new[] { "linha 1", "linha 2", "linha 3", "linha 4" }), "primeiro bloco mostra quatro linhas");
Check(CueTimeline.BuildProjectionBlock(tenLines, 3, 4) == string.Join(Environment.NewLine, new[] { "linha 1", "linha 2", "linha 3", "linha 4" }), "mantém o mesmo bloco até a quarta linha");
Check(CueTimeline.BuildProjectionBlock(tenLines, 4, 4) == string.Join(Environment.NewLine, new[] { "linha 5", "linha 6", "linha 7", "linha 8" }), "troca para as quatro linhas seguintes");
Check(CueTimeline.BuildProjectionBlock(tenLines, 8, 4) == string.Join(Environment.NewLine, new[] { "linha 9", "linha 10" }), "último bloco aceita menos linhas restantes");
Check(CueTimeline.BuildProjectionBlock(tenLines, 2, 2) == string.Join(Environment.NewLine, new[] { "linha 3", "linha 4" }), "duas linhas formam blocos consecutivos");
Check(CueTimeline.BuildProjectionBlock(tenLines, 0, 1) == "linha 1", "uma linha por bloco");
Check(CueTimeline.BuildProjectionBlock(tenLines, 0, 20) == string.Join(Environment.NewLine, tenLines.Select(cue => cue.Text)), "limite máximo de dez linhas");
Check(CueTimeline.BuildProjectionBlock(tenLines, -1, 4) == string.Empty, "sem frase ativa mantém projeção vazia");
var multiLine = new[]
{
    new LyricCue { Seconds = 1, Text = "primeira\nsegunda" },
    new LyricCue { Seconds = 2, Text = "terceira" },
    new LyricCue { Seconds = 3, Text = "quarta" },
    new LyricCue { Seconds = 4, Text = "quinta" }
};
Check(CueTimeline.BuildProjectionBlock(multiLine, 1, 3) == string.Join(Environment.NewLine, new[] { "primeira", "segunda", "terceira" }), "bloco conta linhas visuais em frases multilinha");

var expectedLiveLyrics = new[]
{
    "No vigésimo ano do mês de Quisleu",
    "Eu, Neemias, estando em Susã",
    "E os muros, comecei a levantar",
    "E os meus planos comecei a detalhar"
};
var liveMatch = LiveLyricsMatcher.FindNextLine(expectedLiveLyrics, 0,
    "No vigésimo ano do mês de Quisleu", out var liveConfidence);
Check(liveMatch == 0 && liveConfidence >= 0.78, "reconhece frase completa com acentos");
liveMatch = LiveLyricsMatcher.FindNextLine(expectedLiveLyrics, 1,
    "Eu Neemias estando em Susa", out liveConfidence);
Check(liveMatch == 1 && liveConfidence >= 0.78, "reconhece frase sem acentos");
liveMatch = LiveLyricsMatcher.FindNextLine(expectedLiveLyrics, 1,
    "No vigesimo ano do mes de Quisleu", out liveConfidence);
Check(liveMatch == -1, "não volta para frase anterior já reconhecida");
liveMatch = LiveLyricsMatcher.FindNextLine(expectedLiveLyrics, 0,
    "uma conversa qualquer sem relação", out liveConfidence);
Check(liveMatch == -1, "rejeita transcrição sem correspondência");

Console.WriteLine("Todos os testes da linha do tempo e dos blocos de projeção passaram.");