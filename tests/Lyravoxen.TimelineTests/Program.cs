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

Console.WriteLine("Todos os testes da linha do tempo, projeção, referências bíblicas, voz e marcação por toque passaram.");