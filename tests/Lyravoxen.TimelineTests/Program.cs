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
    "Eu Neemias estando em Susa", out liveConfidence);
Check(liveMatch == 1, "aceita a próxima frase quando a transcrição avança uma linha");
liveMatch = LiveLyricsMatcher.FindNextLine(expectedLiveLyrics, 0,
    "E os muros comecei a levantar", out liveConfidence);
Check(liveMatch == 2, "aceita transcrição agrupada que corresponde a até duas linhas adiante");
liveMatch = LiveLyricsMatcher.FindNextLine(expectedLiveLyrics, 0,
    "uma conversa qualquer sem relação", out liveConfidence);
Check(liveMatch == -1, "rejeita transcrição sem correspondência");


Check(BibleReferenceParser.TryParse("João 3:16") == "João 3:16", "referência numérica João 3:16");
Check(BibleReferenceParser.TryParse("João capítulo três, versículo dezesseis") == "João 3:16", "referência falada por extenso");
Check(BibleReferenceParser.TryParse("João capítulo 3 versículo 16") == "João 3:16", "referência falada com números");
Check(BibleReferenceParser.TryParse("João capítulo 3 versículo dezesseis") == "João 3:16", "capítulo numérico e versículo falado");
Check(BibleReferenceParser.TryParse("primeiro Samuel capítulo dois versículo quatro") == "1 Samuel 2:4", "livro com ordinal falado");
Check(BibleReferenceParser.TryParse("Salmos vinte e três") == "Salmos 23", "capítulo com número composto");
Check(BibleReferenceParser.TryParse("uma conversa sem referência") is null, "rejeita fala sem livro bíblico");
Check(BibleReferenceParser.TryParse("João 0:16") is null, "rejeita capítulo inválido");

// Recuperação quando o reconhecimento perde frases e planejamento dos tempos salvos.
var longLyrics = new[]
{
    "Grandes coisas fez o Senhor por nós", "Estamos alegres com a sua bondade",
    "Meu coração canta louvores ao Rei", "Rios de paz inundam a minha alma",
    "Sobre a rocha firmei os meus passos", "Nenhuma sombra pode me separar",
    "Aleluia aleluia ao Cordeiro de Deus", "Em toda a terra ressoa seu nome"
};
liveMatch = LiveLyricsMatcher.FindNextLine(longLyrics, 0, "Sobre a rocha firmei os meus passos", out liveConfidence);
Check(liveMatch == -1, "janela normal não salta para uma frase distante");
liveMatch = LiveLyricsMatcher.FindNextLine(longLyrics, 0, "Sobre a rocha firmei os meus passos", out liveConfidence, 0.78, 12);
Check(liveMatch == 4 && liveConfidence >= 0.85, "janela ampliada recupera frase distante com alta confiança");
liveMatch = LiveLyricsMatcher.FindNextLine(longLyrics, 0, "conversa sem relação nenhuma", out liveConfidence, 0.78, 12);
Check(liveMatch == -1, "janela ampliada ainda rejeita ruído");

var planSingle = AutoSyncPlanner.CreateCuesForMatch(longLyrics, 0, 0, null, 12.5);
Check(planSingle.Count == 1 && planSingle[0].Text == longLyrics[0] && Math.Abs(planSingle[0].Seconds - 12.5) < 1e-9,
    "primeira frase reconhecida gera uma marca no tempo reconhecido");

var planSkip = AutoSyncPlanner.CreateCuesForMatch(longLyrics, 1, 3, 10.0, 20.0);
Check(planSkip.Count == 3 && planSkip[0].Text == longLyrics[1] && planSkip[1].Text == longLyrics[2] && planSkip[2].Text == longLyrics[3],
    "frases puladas continuam na letra sincronizada, em ordem");
Check(planSkip[0].Seconds > 10.0 && planSkip[0].Seconds < planSkip[1].Seconds && planSkip[1].Seconds < planSkip[2].Seconds && Math.Abs(planSkip[2].Seconds - 20.0) < 1e-9,
    "tempos das frases puladas ficam entre a marca anterior e a reconhecida");

var planBehind = AutoSyncPlanner.CreateCuesForMatch(longLyrics, 1, 1, 30.0, 5.0);
Check(planBehind.Count == 1 && planBehind[0].Seconds > 30.0, "tempo nunca recua em relação à última marca");

var planMidSong = AutoSyncPlanner.CreateCuesForMatch(longLyrics, 0, 5, null, 80.0);
Check(planMidSong.Count == 1 && planMidSong[0].Text == longLyrics[5],
    "música iniciada no meio não inventa tempos para frases não ouvidas");

var planLeading = AutoSyncPlanner.CreateCuesForMatch(longLyrics, 0, 2, null, 8.0);
Check(planLeading.Count == 3 && planLeading[0].Seconds >= 0 && planLeading[0].Seconds < planLeading[1].Seconds && planLeading[1].Seconds < planLeading[2].Seconds,
    "poucas frases iniciais perdidas recebem tempos estimados antes da reconhecida");

Check(AutoSyncPlanner.CreateCuesForMatch(longLyrics, 3, 1, null, 5.0).Count == 0, "índice anterior ao esperado não gera marcas");
Check(AutoSyncPlanner.CreateCuesForMatch(longLyrics, 0, 99, null, 5.0).Count == 0, "índice inexistente não gera marcas");

// Referências bíblicas digitadas na Central Bíblica.
Check(BibleReferenceParser.TryParse("Jo 3:16") == "João 3:16", "Jo (sem acento) é a abreviação de João, não de Jó");
Check(BibleReferenceParser.TryParse("Jó 1:1") == "Jó 1:1", "Jó com acento continua sendo o livro de Jó");
Check(BibleReferenceParser.TryParse("1 jo 3:16") == "1 João 3:16", "1 Jo com espaço é 1 João");
Check(BibleReferenceParser.TryParse("1 João 4:8") == "1 João 4:8", "1 João não é confundido com João");
Check(BibleReferenceParser.TryParse("primeiro joão 4:8") == "1 João 4:8", "primeiro João falado");
Check(BibleReferenceParser.TryParse("1Co 13:4-7") == "1 Coríntios 13:4-7", "intervalo de versículos com abreviação");
Check(BibleReferenceParser.TryParse("João 3:16 ao 18") == "João 3:16-18", "intervalo falado com 'ao'");
Check(BibleReferenceParser.TryParse("Sl 23") == "Salmos 23", "abreviação Sl");
Check(BibleReferenceParser.TryParse("Cânticos 2") == "Cantares 2", "Cânticos é Cantares");
Check(BibleReferenceParser.TryParse("abram na bíblia em joão capítulo 3 versículo 16") == "João 3:16", "frase falada com palavras antes do livro");
Check(BibleReferenceParser.TryParse("Obadias 2") is null, "capítulo inexistente no livro é recusado");
Check(BibleReferenceParser.TryParse("Salmos 151") is null, "Salmos tem 150 capítulos");
var bookOnly = BibleReferenceParser.TryParseReference("Gênesis", allowBookOnly: true);
Check(bookOnly is not null && bookOnly.Chapter == 1 && bookOnly.VerseStart is null, "livro sem capítulo abre o capítulo 1");
Check(BibleReferenceParser.TryParseReference("Gênesis") is null, "sem permissão, livro sem capítulo não é referência");
Check(BibleReferenceParser.Books.Count == 66 && BibleReferenceParser.Books.Sum(b => b.Chapters) == 1189, "66 livros e 1189 capítulos");

Console.WriteLine("Todos os testes da linha do tempo, projeção e referências bíblicas passaram.");