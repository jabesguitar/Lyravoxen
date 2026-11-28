using Lyravoxen.Models;
using Lyravoxen.Services;

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
Check(Math.Abs(CueTimeline.ClampForwardOffset(new double?[] { 10, 20, 30 }, 1, -0.5) + 0.5) < 0.001,
    "ajuste fino desloca a frase e as posteriores pela mesma diferença");
Check(Math.Abs(CueTimeline.ClampForwardOffset(new double?[] { 10, 10.2, 30 }, 1, -1) + 0.15) < 0.001,
    "ajuste fino não deixa uma frase ultrapassar a anterior");
Check(Math.Abs(CueTimeline.ClampForwardOffset(new double?[] { 10, null, 30 }, 1, -0.3) + 0.3) < 0.001,
    "ajuste pendente funciona em cartão ainda sem tempo");
var originalPreviewTimes = new double?[] { 10, 20, 30, null };
var previewShiftedTimes = CueTimeline.PreviewShiftAfterAnchor(originalPreviewTimes, 1, 20.4);
Check(Math.Abs(previewShiftedTimes[1]!.Value - 20.4) < 0.001 &&
      Math.Abs(previewShiftedTimes[2]!.Value - 30.4) < 0.001 &&
      !previewShiftedTimes[3].HasValue,
    "prévia da linha do tempo desloca a frase selecionada e os cues seguintes");
Check(Math.Abs(originalPreviewTimes[1]!.Value - 20) < 0.001 &&
      Math.Abs(originalPreviewTimes[2]!.Value - 30) < 0.001,
    "prévia da linha do tempo não altera os dados originais antes de aplicar");
var boundedPreviewTimes = CueTimeline.PreviewShiftAfterAnchor(new double?[] { 10, 10.2, 20 }, 1, 9);
Check(Math.Abs(boundedPreviewTimes[1]!.Value - 10.05) < 0.001,
    "prévia impede o cartão selecionado de ultrapassar a frase anterior");
var acceleratedTimeline = CueTimeline.ScaleAfterAnchor(new double?[] { 10, 20, 30 }, 0, 0.99);
Check(Math.Abs(acceleratedTimeline[0]!.Value - 10) < 0.001 &&
      Math.Abs(acceleratedTimeline[1]!.Value - 19.9) < 0.001 &&
      Math.Abs(acceleratedTimeline[2]!.Value - 29.8) < 0.001,
    "acelerar 1% preserva a âncora e aproxima os tempos posteriores");
var deceleratedTimeline = CueTimeline.ScaleAfterAnchor(new double?[] { 10, 20, 30 }, 0, 1.01);
Check(Math.Abs(deceleratedTimeline[0]!.Value - 10) < 0.001 &&
      Math.Abs(deceleratedTimeline[1]!.Value - 20.1) < 0.001 &&
      Math.Abs(deceleratedTimeline[2]!.Value - 30.2) < 0.001,
    "desacelerar 1% preserva a âncora e amplia os tempos posteriores");
var clampedRateTimeline = CueTimeline.ScaleAfterAnchor(new double?[] { 10, 10.01, 20 }, 0, 0.5);
Check(clampedRateTimeline[1]!.Value >= 10.05 &&
      clampedRateTimeline[2]!.Value >= clampedRateTimeline[1]!.Value + 0.05,
    "ajuste de ritmo mantém intervalo mínimo entre frases");
Check(LyricsCatalogRanking.ScoreBrazilianPortuguese("Você chegou pra mudar a minha vida, tá tudo bem") >= 70,
    "catálogo prioriza letra provavelmente brasileira");
Check(LyricsCatalogRanking.ScoreBrazilianPortuguese("Es un martirio, verte una y otra vez, los latidos de mi piel") < 0,
    "catálogo não confunde espanhol com PT-BR");
Check(LyricsCatalogRanking.GetNameMatchScore("Martirio Carmen Lillo", "Martirio") > 0,
    "catálogo mantém relevância do nome da música na classificação");

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

var sectionBoundaries = new[]
{
    new LyricCue { Seconds = 1, Text = "verso anterior 1", SectionId = "verse-a", OccurrenceIndex = 1 },
    new LyricCue { Seconds = 2, Text = "verso anterior 2", SectionId = "verse-a", OccurrenceIndex = 1 },
    new LyricCue { Seconds = 3, Text = "coro linha 1", SectionId = "chorus", OccurrenceIndex = 1 },
    new LyricCue { Seconds = 4, Text = "coro linha 2", SectionId = "chorus", OccurrenceIndex = 1 },
    new LyricCue { Seconds = 5, Text = "verso seguinte 1", SectionId = "verse-b", OccurrenceIndex = 1 }
};
Check(CueTimeline.BuildProjectionBlock(sectionBoundaries, 2, 4) ==
    string.Join(Environment.NewLine, new[] { "coro linha 1", "coro linha 2" }),
    "projeção não mistura linhas da seção anterior ou seguinte");
var repeatedChorus = new[]
{
    new LyricCue { Seconds = 10, Text = "coro A", SectionId = "chorus", OccurrenceIndex = 1 },
    new LyricCue { Seconds = 11, Text = "coro B", SectionId = "chorus", OccurrenceIndex = 1 },
    new LyricCue { Seconds = 40, Text = "coro A", SectionId = "chorus", OccurrenceIndex = 2 },
    new LyricCue { Seconds = 41, Text = "coro B", SectionId = "chorus", OccurrenceIndex = 2 }
};
Check(CueTimeline.BuildProjectionBlock(repeatedChorus, 2, 4) ==
    string.Join(Environment.NewLine, new[] { "coro A", "coro B" }),
    "cada repetição do mesmo coro forma uma ocorrência isolada");
var cueMetadata = CueTimeline.Normalize(new[]
{
    new LyricCue { Seconds = 8, Text = "linha marcada", SectionId = "sec-1", OccurrenceIndex = 3,
        SourceLineIndex = 7, Confidence = 0.92, NeedsReview = false, IsManual = true }
}).Single();
Check(cueMetadata.SectionId == "sec-1" && cueMetadata.OccurrenceIndex == 3 &&
      cueMetadata.SourceLineIndex == 7 && cueMetadata.Confidence == 0.92 && cueMetadata.IsManual,
    "normalização preserva origem, repetição, confiança e marcação manual");


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

// Referências bíblicas ditas ao microfone (números por extenso).
Check(VoiceBibleReferenceParser.TryParse("João três dezesseis")?.ToString() == "João 3:16", "voz: João três dezesseis");
Check(VoiceBibleReferenceParser.TryParse("Abra em Salmos vinte e três")?.ToString() == "Salmos 23", "voz: Salmos vinte e três (só capítulo)");
Check(VoiceBibleReferenceParser.TryParse("Romanos oito vinte e oito")?.ToString() == "Romanos 8:28", "voz: Romanos oito vinte e oito");
Check(VoiceBibleReferenceParser.TryParse("Gênesis um um")?.ToString() == "Gênesis 1:1", "voz: Gênesis um um");
Check(VoiceBibleReferenceParser.TryParse("1 Coríntios 13 versículo 4 ao 7")?.ToString() == "1 Coríntios 13:4-7", "voz: intervalo com versículo");
Check(VoiceBibleReferenceParser.TryParse("João 3:16.")?.ToString() == "João 3:16", "voz: texto já em dígitos com ponto final");
Check(VoiceBibleReferenceParser.TryParse("Salmos cento e dezenove cento e cinco")?.ToString() == "Salmos 119:105", "voz: cento e dezenove / cento e cinco");
Check(VoiceBibleReferenceParser.TryParse("Obrigado por assistir") is null, "voz: frase sem livro é recusada");
Check(VoiceBibleReferenceParser.TryParse("   ") is null, "voz: silêncio é recusado");
Check(VoiceBibleReferenceParser.ConvertNumberWords("vinte e um") == "21", "números por extenso: vinte e um");

// Marcação manual por toque (uma marca por estrofe).
var stanzas = TapSyncPlanner.SplitStanzas("a1\na2\n\nb1\nb2\nb3\n\n\nc1");
Check(stanzas.Count == 3 && stanzas[0].Count == 2 && stanzas[1].Count == 3 && stanzas[2].Count == 1, "estrofes separadas por linha em branco");
var noBlank = TapSyncPlanner.SplitStanzas(string.Join("\n", Enumerable.Range(1, 9).Select(i => "linha " + i)));
Check(noBlank.Count == 3 && noBlank[0].Count == 4 && noBlank[2].Count == 1, "sem linha em branco: grupos de 4 linhas");
Check(TapSyncPlanner.SplitStanzas("  \n ").Count == 0, "letra vazia não gera estrofes");

var tapCues = TapSyncPlanner.BuildCues(stanzas, new[] { 10d, 20d, 40d });
Check(tapCues.Count == 6, "uma frase por linha da letra");
Check(tapCues[0].Seconds == 10d && tapCues[2].Seconds == 20d && tapCues[5].Seconds == 40d, "primeira linha de cada estrofe usa o instante do toque");
Check(tapCues.Zip(tapCues.Skip(1), (x, y) => y.Seconds > x.Seconds).All(ok => ok), "tempos estritamente crescentes");
Check(tapCues[1].Seconds > 10d && tapCues[1].Seconds < 20d, "linha interna fica entre dois toques");
var partialTap = TapSyncPlanner.BuildCues(stanzas, new[] { 5d, 15d });
Check(partialTap.Count == 5 && partialTap[^1].Text == "b3", "estrofes sem toque ficam de fora");
Check(TapSyncPlanner.LinesBefore(stanzas, 2) == 5, "linhas antes da estrofe 3");
Check(CueTimeline.BuildProjectionBlock(tapCues, TapSyncPlanner.LinesBefore(stanzas, 1), 4) == string.Join(Environment.NewLine, new[] { "a1", "a2", "b1", "b2" }), "projeção da estrofe usa o bloco rolante já existente");

// Pedidos por WhatsApp (pessoa, grupo, exportação e cópia do WhatsApp Web).
var android = "19/11/2026 20:15 - Maria: Conjunto: Jovens\n1. Santo - https://youtu.be/abcdefghijk\n19/11/2026 20:16 - Joao: Grande é o Senhor https://www.youtube.com/watch?v=12345678901\n19/11/2026 20:17 - Ana: oi pessoal";
var chat = SongRequestStore.MessagesToJson(android);
Check(chat.Count == 2, "grupo: só mensagens com link viram pedido");
Check(chat[0].Contains("Jovens") && chat[1].Contains("Joao"), "grupo: 'Conjunto:' vale; sem ele o remetente vira o grupo");
var ios = "[19/11/2026 20:15:10] Pedro: 1. Aleluia - https://youtu.be/abcdefghijk";
Check(SongRequestStore.MessagesToJson(ios).Count == 1, "grupo: formato do iPhone");
var web = "[20:15, 19/11/2026] Lia: Hosana https://youtu.be/abcdefghijk";
Check(SongRequestStore.MessagesToJson(web).Count == 1, "grupo: formato do WhatsApp Web");
Check(SongRequestStore.LooksLikeRequest(web) && SongRequestStore.LooksLikeRequest("Conjunto: X\nhttps://youtu.be/abcdefghijk"), "área de transferência: reconhece pedido");
Check(!SongRequestStore.LooksLikeRequest("https://youtu.be/abcdefghijk") && !SongRequestStore.LooksLikeRequest("bom dia"), "área de transferência: link solto é ignorado");
var token = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("{\"grupo\":\"\",\"itens\":[{\"nome\":\"A\",\"link\":\"https://youtu.be/abcdefghijk\"}]}"));
Check(SongRequestStore.MessagesToJson("[19/11/2026 20:15:10] Rui: 🎵 Pedido\n#LYRA:" + token)[0].Contains("Rui"), "formulário: remetente preenche o grupo vazio");
Check(WhatsAppSettings.NormalizeGroupLink("veja https://chat.whatsapp.com/AbCdEfGhIjKlMn?x=1") == "https://chat.whatsapp.com/AbCdEfGhIjKlMn" && WhatsAppSettings.NormalizeGroupLink("https://exemplo.com/x") == "", "link de convite do grupo");

// Prévia de letra da próxima música.
Check(LyricsPreviewService.CleanTitle("Eliã Oliveira - Reconstrução (Clipe Oficial) [HD]") == "Eliã Oliveira - Reconstrução", "prévia: título limpo");
Check(LyricsPreviewService.BuildQueries("Reconstrução - Eliã Oliveira (Ao Vivo)").SequenceEqual(new[] { "Reconstrução - Eliã Oliveira", "Reconstrução", "Eliã Oliveira" }), "prévia: consultas com cada parte do título");
Check(LyricsPreviewService.Normalize("Reconstrução!") == "reconstrucao", "prévia: normalização sem acentos");
Check(LyricsPreviewService.LrcTextLines("[00:12.30] Primeira linha\n[00:20.00][00:40.00]Refrão\n[00:50.00]").SequenceEqual(new[] { "Primeira linha", "Refrão" }), "prévia: tira tempos do .lrc");
var catalog = System.Text.Json.JsonDocument.Parse("""
[{"trackName":"Deus","artistName":"Outro","plainLyrics":"x\ny\nz\nw"},
 {"trackName":"Reconstrução","artistName":"Eliã Oliveira","plainLyrics":"texto\nsem tempos\na\nb"},
 {"trackName":"Reconstrução","artistName":"Eliã Oliveira","syncedLyrics":"[00:10.00] Nada mais\n[00:15.00] Resta"}]
""").RootElement.EnumerateArray().Select(e => e.Clone()).ToList();
var picked = LyricsPreviewService.Pick(catalog, "Reconstrução - Eliã Oliveira (Clipe Oficial)", "Eliã Oliveira");
Check(picked.Availability == LyricsAvailability.Synced && picked.ArtistMatches && picked.Preview.StartsWith("Nada mais"), "prévia: prefere letra sincronizada e mostra as primeiras linhas");
Check(LyricsPreviewService.Pick(catalog, "Deus é fiel - Banda", "Canal").Availability == LyricsAvailability.NotFound, "prévia: nome curto dentro de outro título não conta");
Check(LyricsPreviewService.Pick(new System.Text.Json.JsonElement[0], "Qualquer", "x").Availability == LyricsAvailability.NotFound, "prévia: sem resultados = não encontrada");

var lrcCues = LyricsPreviewService.ParseLrc("[ti:x]\n[00:12.30] A\n[01:05.5][02:00.0] B\n[03:00.00]");
Check(lrcCues.Count == 3 && lrcCues[0].Text == "A" && Math.Abs(lrcCues[1].Seconds - 65.5) < 0.001 && lrcCues[2].Seconds == 120, "prévia: .lrc vira frases com tempo (várias marcas por linha)");
Check(picked.Cues.Count == 2 && picked.PlainText == "Nada mais\nResta", "prévia: escolha guarda frases e texto completo para confirmar");
var byNames = LyricsPreviewService.Pick(catalog, "Reconstrução - Eliã Oliveira", "Eliã Oliveira");
Check(byNames.Availability == LyricsAvailability.Synced && byNames.ArtistMatches, "prévia: busca por música e autor digitados");

// Pedidos pela internet (Cloudflare).
Check(WhatsAppSettings.NormalizeCloudUrl("x.workers.dev/pedido?a=1") == "https://x.workers.dev", "nuvem: endereço sem caminho");
Check(WhatsAppSettings.NormalizeCloudUrl("http://x.workers.dev") == "" && WhatsAppSettings.NormalizeCloudUrl("https://x\"y.dev") == "", "nuvem: só https e nome simples");
Check(WhatsAppSettings.NewToken().Length == 24, "nuvem: senha gerada");
var cloudCfg = new WhatsAppSettings { CloudUrl = "https://x.workers.dev", CloudToken = "SEGREDOSEGREDO1234", Number = "5582999999999" };
var cloudForm = OfflineRequestForm.Build(cloudCfg, cloudCfg.SendUrl);
Check(cloudForm.Contains("method=\"post\" action=\"https://x.workers.dev/enviar\"") && cloudForm.Contains("SEND='https://x.workers.dev/enviar'"), "nuvem: formulário envia ao serviço, com e sem JavaScript");
Check(!cloudForm.Contains("SEGREDO"), "nuvem: a senha nunca vai no formulário");
Check(OfflineRequestForm.Build(cloudCfg).Contains("method=\"get\""), "formulário sem nuvem continua indo ao WhatsApp");
var workerCode = CloudWorker.Build(cloudCfg);
Check(workerCode.Contains("env.PEDIDOS") && workerCode.Contains("ADMIN_TOKEN") && workerCode.Contains("const PAGE = \"") && !workerCode.Contains("SEGREDO"), "nuvem: código do serviço traz a página e não traz a senha");

var numbered = SongRequestStore.MessagesToJson("Conjunto: J\n1. 10.000 Razões - https://youtu.be/abcdefghijk\n2) Santo https://youtu.be/bbbbbbbbbbb\n- 40 dias https://youtu.be/ccccccccccc");
Check(numbered.Count == 1 && numbered[0].Contains("10.000 Raz") && numbered[0].Contains("\"Santo\"") && numbered[0].Contains("40 dias"), "pedido: tira só a numeração da lista, não números que fazem parte do nome");

// Intérprete e título da música a partir do título/canal do vídeo; limpeza de letra copiada.
var nameA = TrackTitleParser.Parse("Eliã Oliveira - Reconstrução (Clipe Oficial)", "Eliã Oliveira");
Check(nameA.Track == "Reconstrução" && nameA.Artist == "Eliã Oliveira" && nameA.Confident, "nomes: intérprete = canal");
var nameB = TrackTitleParser.Parse("Fernandinho - Santo (Ao Vivo)", "Som Gospel Playback");
Check(nameB.Track == "Santo" && nameB.Artist == "Fernandinho" && !nameB.Confident && nameB.PartA == "Fernandinho", "nomes: canal de playback não vira intérprete; ordem a confirmar");
var nameC = TrackTitleParser.Parse("Santo - Fernandinho", "Canal X", new[] { "Fernandinho" });
Check(nameC.Track == "Santo" && nameC.Artist == "Fernandinho" && nameC.Confident, "nomes: intérprete conhecido resolve a ordem invertida");
var nameD = TrackTitleParser.Parse("Grande é o Senhor (Legendado)", "Adhemar de Campos - Topic");
Check(nameD.Track == "Grande é o Senhor" && nameD.Artist == "Adhemar de Campos", "nomes: título único + canal Topic");
Check(TrackTitleParser.LooksUncorrected("Fernandinho - Santo (Ao Vivo)", "x") && !TrackTitleParser.LooksUncorrected("Rei dos Reis", "Aline - Rei dos Reis"), "nomes: só corrige o que ainda é o título inteiro");
Check(LyricsTextCleaner.Clean("[Refrão]\nC G Am\nSanto, santo\n\n\nSenhor (2x)\nCompositor: X\n© 2020").Replace("\r", "") == "Santo, santo\n\nSenhor", "letra: tira rótulos, cifras, créditos e marcas de repetição");
Check(!LyricsTextCleaner.LooksLikeLyrics("https://youtu.be/abcdefghijk") && LyricsTextCleaner.LooksLikeLyrics("Santo, santo, santo\nSenhor Deus todo-poderoso\nA terra e o céu cheios estão\nDa tua glória, Senhor"), "letra: link não parece letra");

// Rascunho de tempos, nomes com "tom abaixo"/feat. e busca tolerante por nomes.
var estimated = TapSyncPlanner.EstimateStarts(new List<List<string>>
{
    new() { new string('a', 40), new string('b', 40) },
    new() { new string('c', 40), new string('d', 40) },
    new() { new string('e', 80), new string('f', 80) }
}, 240);
Check(estimated.Count == 3 && Math.Abs(estimated[0] - 14.4) < 1e-9 && estimated[0] < estimated[1] && estimated[1] < estimated[2] && estimated[2] < 228, "estimativa: 1ª estrofe após 6% e estrofes crescentes dentro da música");
Check(TapSyncPlanner.EstimateStarts(new List<List<string>> { new() { "x" } }, 200, 30)[0] == 30, "estimativa: usa o instante informado como começo da 1ª estrofe");
Check(TrackTitleParser.Parse("Santo - Um Tom Abaixo", "Fernandinho").Track == "Santo" && TrackTitleParser.Parse("Santo - Dois Tons Acima", "Fernandinho").Track == "Santo", "nomes: tira \"um tom abaixo\" e \"dois tons acima\"");
Check(TrackTitleParser.Parse("Fernandinho - Santo feat. Aline Barros (Ao Vivo)", "Fernandinho").Track == "Santo", "nomes: feat. sai do título da música");
Check(TrackTitleParser.Parse("Rei dos Reis", "Hinos Cantados TV").Artist == "", "nomes: canal de hinos não vira intérprete");
var catalogItems = System.Text.Json.JsonDocument.Parse("[{\"trackName\":\"Reconstrução - Ao Vivo\",\"artistName\":\"Eliã Oliveira\",\"syncedLyrics\":\"[00:10.00] a\\n[00:20.00] b\"},{\"trackName\":\"Deus é fiel\",\"artistName\":\"Banda\",\"plainLyrics\":\"x\\ny\"}]")
    .RootElement.EnumerateArray().Select(e => e.Clone()).ToList();
Check(LyricsPreviewService.PickByNames(catalogItems, "Reconstrução", "Eliã Oliveira").Availability == LyricsAvailability.Synced, "busca por nomes: aceita sufixo \"Ao Vivo\" do catálogo");
Check(LyricsPreviewService.PickByNames(catalogItems, "Eliã Oliveira", "Reconstrução").Availability == LyricsAvailability.Synced, "busca por nomes: campos trocados também acham");
var shortName = LyricsPreviewService.PickByNames(catalogItems, "Deus", "");
Check(shortName.Availability == LyricsAvailability.NotFound && shortName.CatalogResults == 2, "busca por nomes: \"Deus\" não casa com \"Deus é fiel\" e informa os resultados");

// Texto da letra pelo Vagalume.
const string lyricsBody = "Estátuas e cofres\\ne paredes pintadas\\nNinguém sabe o que aconteceu\\nÉ só uma criança\\nque quer ser feliz e não sabe\\nde onde vem a dor";
var vagalumeOk = VagalumeLyricsService.Parse("{\"type\":\"exact\",\"art\":{\"name\":\"Legião Urbana\"},\"mus\":[{\"name\":\"Pais e Filhos\",\"text\":\"" + lyricsBody + "\"}]}", "Pais e Filhos");
Check(vagalumeOk.Found && vagalumeOk.Artist == "Legião Urbana" && vagalumeOk.Text.Contains("Estátuas e cofres"), "vagalume: resposta exata vira texto limpo");
Check(!VagalumeLyricsService.Parse("{\"type\":\"notfound\"}", "Santo").Found && VagalumeLyricsService.Parse("{\"type\":\"song_notfound\"}", "Santo").Message.Contains("não tem"), "vagalume: música inexistente");
Check(!VagalumeLyricsService.Parse("{\"type\":\"aprox\",\"mus\":[{\"name\":\"Outra Coisa\",\"text\":\"" + lyricsBody + "\"}]}", "Pais e Filhos").Found, "vagalume: música aproximada com outro nome é recusada");
Check(VagalumeLyricsService.Parse("{\"type\":\"aprox\",\"mus\":[{\"name\":\"Pais e Filhos - Ao Vivo\",\"text\":\"" + lyricsBody + "\"}]}", "Pais e Filhos").Found, "vagalume: aceita o mesmo nome com sufixo");
Check(!VagalumeLyricsService.Parse("não é json", "x").Found && !VagalumeLyricsService.Parse("{\"type\":\"exact\",\"mus\":[{\"name\":\"X\",\"text\":\"curto\"}]}", "X").Found, "vagalume: resposta inválida ou texto curto");
Check(LyricsSourceSettings.SafeKey("abc-123_DEF ;'\"") == "abc-123_DEF", "vagalume: chave só com letras, números, - e _");


// Análise inicial da estrutura da letra: texto-fonte separado das ocorrências de execução.
var structuredLyrics = LyricsStructureAnalyzer.Analyze(
    "Estrofe 1\nQuando eu clamo, tu me ouves\nE me sustentas em amor\n\n[Coro]\nSanto, santo é o Senhor\nDigno de todo louvor\n\nEstrofe 2\nMesmo na noite eu cantarei\nTua promessa guardarei\n\n[Coro]\nSanto, santo é o Senhor\nDigno de todo louvor\n\nEstrofe 3\nTua luz me conduzirá\nE tua graça bastará\n\n[Coro 2x]\nSanto, santo é o Senhor\nDigno de todo louvor");
var chorusSection = structuredLyrics.Sections.Single(section => section.Kind == LyricsSectionKind.Chorus);
Check(structuredLyrics.ExecutionOrder.Count == 7, "estrutura: cria 3 estrofes + 2 entradas do coro + coro final 2x (7 ocorrências)");
Check(chorusSection.OccurrenceCount == 4, "estrutura: mesmo coro explícito aponta para quatro ocorrências");
Check(structuredLyrics.Sections.Count == 4, "estrutura: preserva três estrofes e um único bloco-fonte de coro");
Check(structuredLyrics.ExecutionOrder.Where(item => item.Kind == LyricsSectionKind.Chorus).Select(item => item.OccurrenceIndex).SequenceEqual(new[] { 1, 2, 3, 4 }),
    "estrutura: ocorrências do coro têm índices independentes");
Check(structuredLyrics.ExecutionOrder[0].Kind == LyricsSectionKind.Verse &&
      structuredLyrics.ExecutionOrder[1].Kind == LyricsSectionKind.Chorus &&
      structuredLyrics.ExecutionOrder[2].Kind == LyricsSectionKind.Verse &&
      structuredLyrics.ExecutionOrder[3].Kind == LyricsSectionKind.Chorus,
    "estrutura: mantém sequência estrofe-coro-estrofe-coro");
var inferredLyrics = LyricsStructureAnalyzer.Analyze(
    "Quando eu clamo tu me ouves\nE me sustentas em amor\n\nSanto santo é o Senhor\nDigno de todo louvor\n\nMesmo na noite eu cantarei\nTua promessa guardarei\n\nSanto, santo é o Senhor!\nDigno de todo louvor.");
Check(inferredLyrics.Sections.Count == 3, "estrutura: blocos repetidos compartilham um bloco-fonte");
Check(inferredLyrics.Sections.Count(section => section.Kind == LyricsSectionKind.Chorus) == 1,
    "estrutura: identifica bloco repetido sem rótulo como provável coro");
Check(inferredLyrics.ExecutionOrder.Count(item => item.Kind == LyricsSectionKind.Chorus) == 2,
    "estrutura: mantém duas ocorrências do coro repetido");
Check(inferredLyrics.Sections.Any(section => section.NeedsReview),
    "estrutura: partes únicas sem rótulo ficam sinalizadas para revisão, não são classificadas por palpite");
var plainVerse = LyricsStructureAnalyzer.Analyze("Uma parte sem rótulo\nOutra linha");
Check(plainVerse.ExecutionOrder.Count == 1 && plainVerse.ExecutionOrder[0].Kind == LyricsSectionKind.Unknown,
    "estrutura: não inventa que um bloco único é estrofe sem evidência");
var explicitRepeat = LyricsStructureAnalyzer.Analyze("[Refrão 2x]\nCanta ao Senhor\nToda a terra");
Check(explicitRepeat.ExecutionOrder.Count == 2 &&
      explicitRepeat.ExecutionOrder.All(item => item.Kind == LyricsSectionKind.Chorus),
    "estrutura: interpreta indicação explícita de refrão duas vezes");
var accentedRepeat = LyricsStructureAnalyzer.Analyze(
    "[Refrão]\nGlória ao Rei\n\n[Refrão]\nGlória ao Rei");
Check(accentedRepeat.Sections.Count == 1 && accentedRepeat.Sections[0].OccurrenceCount == 2,
    "estrutura: rótulos e textos iguais reutilizam identidade de bloco");


// Arquivo permanente mantém estrutura musical e ocorrências após reabrir.
var archivePath = Path.Combine(Path.GetTempPath(), "lyravoxen-structure-" + Guid.NewGuid().ToString("N") + ".json");
try
{
    var archiveAnalysis = LyricsStructureAnalyzer.Analyze(
        "[Estrofe 1]\nPrimeira parte\n\n[Coro]\nCanta ao Senhor\n\n[Estrofe 2]\nSegunda parte\n\n[Coro]\nCanta ao Senhor");
    var archiveVideo = new LibraryVideo
    {
        VideoId = "structure-test-video",
        Url = "https://youtu.be/structure-test",
        TrackName = "Música de teste",
        Artist = "Intérprete de teste",
        FullLyrics = "[Estrofe 1]\nPrimeira parte\n\n[Coro]\nCanta ao Senhor\n\n[Estrofe 2]\nSegunda parte\n\n[Coro]\nCanta ao Senhor",
        Sections = archiveAnalysis.Sections,
        ExecutionOrder = archiveAnalysis.ExecutionOrder.Select(item => new LyricsExecutionOccurrence
        {
            SectionId = item.SectionId, Label = item.Label, Kind = item.Kind,
            OccurrenceIndex = item.OccurrenceIndex, SourceIndex = item.SourceIndex,
            LineOrder = item.Kind == LyricsSectionKind.Chorus && item.OccurrenceIndex == 1
                ? new List<int> { 0, 4 }
                : new List<int>()
        }).ToList()
    };
    new PermanentLyricsArchive(archivePath).Save(archiveVideo);
    var restored = new PermanentLyricsArchive(archivePath).Find("structure-test-video", archiveVideo.Url);
    Check(restored is not null && restored.Sections.Count == 3 && restored.ExecutionOrder.Count == 4,
        "arquivo permanente: recupera seções e ocorrências");
    Check(restored!.ExecutionOrder.Count(item => item.Kind == LyricsSectionKind.Chorus) == 2,
        "arquivo permanente: preserva duas entradas do coro");
    Check(restored.ExecutionOrder.First(item => item.Kind == LyricsSectionKind.Chorus && item.OccurrenceIndex == 1)
        .LineOrder.SequenceEqual(new[] { 0, 4 }),
        "arquivo permanente: preserva ordem personalizada dos slides");
}
finally
{
    if (File.Exists(archivePath)) File.Delete(archivePath);
    if (File.Exists(archivePath + ".bak")) File.Delete(archivePath + ".bak");
    if (File.Exists(archivePath + ".tmp")) File.Delete(archivePath + ".tmp");
}


 // Biblioteca offline: associação segura por ID ou por nome único e retomada do último ponto.
var offlineTestPath = Path.Combine(Path.GetTempPath(), "lyravoxen-offline-match-" + Guid.NewGuid().ToString("N") + ".mp3");
File.WriteAllBytes(offlineTestPath, new byte[] { 1, 2, 3, 4 });
try
{
    var offlineByName = new LibraryVideo
    {
        IsOffline = true, LocalPath = offlineTestPath, TrackName = "Te Ver", Artist = "",
        SavedAt = DateTime.Now, OfflineExpiresAt = DateTime.Now.AddDays(7), LastPlaybackPositionSeconds = 51
    };
    var onlineCurrent = new LibraryVideo
    {
        VideoId = "video-te-ver", TrackName = "Té Vêr", Title = "Trio R3 - Te ver (Play Back)", Artist = "Trio R3"
    };
    var nameMatch = OfflineLibraryMatcher.FindMatchingBackup(new[] { offlineByName }, onlineCurrent, DateTime.Now);
    Check(nameMatch is not null && !nameMatch.MatchedByVideoId && ReferenceEquals(nameMatch.Video, offlineByName),
        "failover: encontra cópia importada por título exato único mesmo sem ID do YouTube");
    Check(OfflineLibraryMatcher.AreSameRecording(onlineCurrent, offlineByName),
        "biblioteca: reconhece mesma gravação por título normalizado quando intérprete do arquivo não foi preenchido");
    Check(Math.Abs(OfflineLibraryMatcher.ResolveResumePosition(null, 51) - 51) < 0.001 &&
          Math.Abs(OfflineLibraryMatcher.ResolveResumePosition(23, 51) - 23) < 0.001,
        "retomada: posição explícita do failover prevalece e seleção manual usa a posição salva");

    var wrongArtist = new LibraryVideo
    {
        IsOffline = true, LocalPath = offlineTestPath, TrackName = "Te Ver", Artist = "Outro Trio",
        SavedAt = DateTime.Now
    };
    Check(OfflineLibraryMatcher.FindMatchingBackup(new[] { wrongArtist }, onlineCurrent, DateTime.Now) is null,
        "failover: não associa arquivo com mesmo título e intérprete diferente");

    var secondCopyPath = offlineTestPath + ".second.mp3";
    File.WriteAllBytes(secondCopyPath, new byte[] { 5, 6, 7 });
    try
    {
        var duplicate = new LibraryVideo
        {
            IsOffline = true, LocalPath = secondCopyPath, TrackName = "Te Ver", Artist = "",
            SavedAt = DateTime.Now.AddMinutes(-1)
        };
        Check(OfflineLibraryMatcher.FindMatchingBackup(new[] { offlineByName, duplicate }, onlineCurrent, DateTime.Now) is null,
            "failover: não escolhe arbitrariamente entre duas cópias do mesmo título sem intérprete");
    }
    finally { if (File.Exists(secondCopyPath)) File.Delete(secondCopyPath); }

    var expired = new LibraryVideo
    {
        IsOffline = true, LocalPath = offlineTestPath, TrackName = "Te Ver", Artist = "Trio R3",
        OfflineExpiresAt = DateTime.Now.AddMinutes(-1)
    };
    Check(OfflineLibraryMatcher.FindMatchingBackup(new[] { expired }, onlineCurrent, DateTime.Now) is null,
        "failover: ignora cópia offline expirada");
    var exactId = new LibraryVideo
    {
        VideoId = "video-te-ver", IsOffline = true, LocalPath = offlineTestPath,
        TrackName = "Título diferente", Artist = "Artista diferente", SavedAt = DateTime.Now.AddDays(-1)
    };
    Check(OfflineLibraryMatcher.FindMatchingBackup(new[] { offlineByName, exactId }, onlineCurrent, DateTime.Now)?.MatchedByVideoId == true,
        "failover: ID exato tem prioridade sobre correspondência de título");
}
finally { if (File.Exists(offlineTestPath)) File.Delete(offlineTestPath); }

Console.WriteLine("Todos os testes de timeline, projeção, referências bíblicas, voz, toque e biblioteca offline passaram.");