# Lyravoxen — histórico e estado do projeto

> Gerado automaticamente para você não perder o contexto se os tokens acabarem.
> Cole este arquivo numa conversa nova e peça para continuar de onde parou.

## 1. Projeto
- App Windows (.NET 8, WPF) **Lyravoxen**: toca YouTube, sincroniza letras, projeta (tela própria e Holyrics) e tem a aba Bíblia.
- Repositório: https://github.com/jabesguitar/Lyravoxen — trabalho na branch `feature/integrated-bible-microphone-holyrics` (PR #9). A `main` está desatualizada.
- Reconhecimento de voz: Whisper.net (runtime sem AVX, para PCs antigos). Letras: LRCLIB. Bíblia: API do Holyrics (`pt_arc`) + leitor local (midvash).
- Quem compila e testa: GitHub Actions (aba **Actions**). No ambiente do Claude **não há .NET**: nada foi compilado lá.

## 2. Regras do usuário
- Economizar tokens; ele não envia imagens. Respostas curtas.
- Quando os tokens estiverem acabando: reunir tudo em Markdown (este arquivo).
- Quer qualidade profissional: nada com cara de rascunho.
- **Teste único**: ele não quer instalar a cada etapa. O Claude faz todas as etapas, revisa o código por leitura e entrega **uma lista só** do que ele precisa fazer; os erros do teste voltam todos de uma vez.
- Sempre atualizar este arquivo com o relatório de tudo que foi feito.

## 3. Como aplicar um ZIP de alterações
1. Descompactar. 2. GitHub → branch acima → **Add file → Upload files**. 3. Arrastar as pastas `src` e `tests` (substitui só os arquivos do ZIP). 4. **Commit changes**. 5. Aba **Actions**: verde = compilou e testes passaram. Se der erro, copiar o texto do erro.

## 4. O que já foi feito
1. **Salvar e sincronizar** (ZIP `Lyravoxen-salvar-e-sincronizar`): ao salvar a letra colada, a sincronização automática começa sozinha (ou no primeiro play); pergunta se pode reiniciar a música se já passou de 15 s; projeta na prévia, na tela própria e no Holyrics. Correções: frases puladas não somem mais da letra salva; busca se alarga quando trava; novo `AutoSyncPlanner` e testes.
2. **Correção do XAML**: havia uma tag `<Button ` solta na linha 88 (herdada de edição anterior) que quebrava o build. Corrigida.
3. **Central Bíblica** (ZIP `Lyravoxen-central-biblica`): aba refeita (versão, campo de referência, livros/capítulos/versículos, prévia, Projetar/Anterior/Próximo/Limpar). Novo `BibleReferenceParser` com 66 livros, intervalos (`1Co 13:4-7`), "Jo" = João (antes virava Jó), "1 João" ≠ João. Cache local de livros e capítulos.
4. **Ajustes pós-teste** (ZIP `Lyravoxen-biblia-ajustes`): com a Bíblia do Holyrics não aparece mais o texto local de grafia antiga (só a numeração; o texto é do Holyrics); **Limpar telão** agora fecha a janela de projeção própria (some o fundo e aparece a área de trabalho).

5. **Etapa 2 + marcação por toque + letra sem tempos** (ZIP `Lyravoxen-etapa2-voz-toque`, ainda **sem teste do usuário**):
   - **Microfone na Central Bíblica** (aba Bíblia, nova linha abaixo da referência): lista de dispositivos (inclui interface de áudio externa; botão ↻ procura de novo; lembra a escolha em `%LocalAppData%\Lyravoxen\microfone.txt`), botão **Segure para falar** (ou tecla **F4** segurada), medidor de nível, limite de 12 s. Fala é transcrita localmente pelo mesmo modelo Whisper (sem internet depois do download, sem API paga) com uma dica de vocabulário bíblico.
   - **Confirmação antes de projetar**: aparece "Entendi: João 3:16 (ouvi: …)" com **Projetar (Enter)** e **Descartar (Esc)**. A referência já é localizada na lista; nada vai ao telão sem confirmar.
   - Novo `VoiceBibleReferenceParser`: converte números por extenso ("João três dezesseis", "Salmos vinte e três", "cento e dezenove") e reaproveita o `BibleReferenceParser`. Só o nome do livro dentro de frase longa e sem número é recusado (evita "os", "is" virarem livro).
   - **Marcação por toque** (aba Cadastrar letra, painel novo): estrofes separadas por linha em branco (sem linha em branco: grupos de 4). Com a música tocando: **Espaço** ou botão TOQUE = começou a estrofe; **Backspace** desfaz; um toque a mais depois da última (ou *Concluir e salvar*) salva. A estrofe é projetada na hora (prévia, tela própria e Holyrics); as linhas internas recebem tempo proporcional ao tamanho do texto. Novo `TapSyncPlanner` (com testes).
   - **Letra do catálogo sem tempos**: se o LRCLIB só tiver texto, o app agora mostra a letra encontrada e, em **Sim**, preenche a caixa, salva e já sincroniza; **Criar** só preenche a caixa para revisar. Antes descartava.
   - Arquivos novos: `MainWindow.Voice.cs`, `MainWindow.TapSync.cs`, `Services/MicrophoneReferenceService.cs`, `Models/VoiceBibleReferenceParser.cs`, `Models/TapSyncPlanner.cs`. Alterados: `MainWindow.xaml`, `MainWindow.xaml.cs` (3 trechos), `LiveAudioTranscriptionService.cs` (`ModelPath` ficou `internal`), testes.
   - Verificação feita sem .NET: XAML bem formado, chaves balanceadas, todos os eventos do XAML têm método, sem nomes duplicados. **Falta o Actions compilar.**

6. **Correção pós-teste da etapa 2** (ZIP `Lyravoxen-correcao-voz-holyrics`, **sem teste**; arquivos: `.github`, `src`):
   - Teste do usuário: (a) voz → "Failed to load native whisper library… PInvokeError: A operação foi concluída com êxito"; (b) Bíblia do Holyrics → HTTP 401 `{"status":"error","error":"unauthorized action"}`; por isso a lista caiu no texto local ALM1911.
   - Causa provável (a): `whisper.dll` precisa do VC++ Redistributable (vcruntime140/msvcp140), ausente no PC. Correção: o workflow copia essas DLLs para a pasta do app (instalador e ZIP portátil herdam) e falha o build se faltarem; novo `Services/WhisperDiagnostics.cs` troca a mensagem técnica por explicação em português (diz se falta whisper.dll, VC++ e se há AVX).
   - Causa (b): o token do Holyrics não tem permissão para `GetBibleVersionsV2`/`ShowVerse`. O cliente agora lê o retorno de `CheckPermissions`, avisa quais ações estão negadas ao "Testar e salvar" e traduz o 401 em instrução clara. **O usuário precisa marcar essas permissões no token, dentro do Holyrics.**
   - Verificação: sem .NET aqui; revisão por leitura. O repositório é público: o código real foi baixado por git (o ZIP anexado do repositório estava desatualizado, sem os arquivos da etapa 2).

## 5. Feedback do usuário (teste da etapa 2: voz e Bíblia falharam; ver item 6 acima)
- A lista de versões apareceu e projetou.
- "Limpar telão" deixava o fundo do telão; ele quer ver a área de trabalho.
- O texto local de domínio público tinha palavras estranhas (grafia antiga). Quer usar a Bíblia do Holyrics como servidor e operar tudo dentro do Lyravoxen.

## 6. Pendências / próximos passos
- **Teste único (lista para o usuário)**: ver seção 9.
- **Etapa 3** (só se o Whisper errar muito nos nomes): trocar por Vosk com vocabulário restrito.
- Música "Reconstrução (Eliã Oliveira)" nunca funcionou na busca; testar pelo caminho "colar letra + Salvar e sincronizar" e pela marcação por toque.
- Limpar telão **no Holyrics**: o Holyrics mantém o fundo dele; a documentação da API estava bloqueada para o Claude. Confirmar no teste se o fundo permanece e anotar o que aparece.
- Possível melhoria: mostrar a marcação por toque também com a música tocando por arquivo local (já usa o relógio do player local; não testado).

## 7. Mapa do código (src/Lyravoxen.Desktop)
- `MainWindow.xaml(.cs)`: tela principal; sincronização automática (`StartAutoRecognitionAsync`, `BeginAutomaticSyncAfterSaveAsync`); Central Bíblica (`LocateBibleReferenceAsync`, `ReloadBibleChapterTextAsync`, `ProjectBibleSelectionAsync`).
- `Models/`: `CueTimeline`, `LiveLyricsMatcher`, `AutoSyncPlanner`, `BibleReferenceParser`.
- `Services/`: `LiveAudioTranscriptionService` (loopback + Whisper), `BibleReaderService` (midvash + cache).
- `MainWindow.Voice.cs`: microfone da Bíblia (push-to-talk, F4, confirmação, atalhos). `MainWindow.TapSync.cs`: marcação por toque.
- `Services/MicrophoneReferenceService.cs`: gravação do microfone (NAudio WaveInEvent, 16/48/44,1 kHz com reamostragem, ganho automático) + Whisper local.
- `Models/VoiceBibleReferenceParser.cs`, `Models/TapSyncPlanner.cs`: lógica pura, coberta por testes.
- `Integration/HolyricsClient.cs`: `GetBibleVersionsAsync`, `ShowBibleVerseAsync`, fila de envio.
- `ProjectionWindow`: tela própria (tela cheia, fundo escuro; `Close()` some com ela).
- `tests/Lyravoxen.TimelineTests/Program.cs`: testes que rodam no Actions.

## 8. Cuidados
- Tudo que o Claude entrega vem **sem compilar**: se o Actions falhar, mande o texto do erro.
- A primeira sincronização baixa o modelo de voz; faça-a antes de usar ao vivo.
- Tempo das frases vem de blocos de 5 s: a projeção ao vivo pode atrasar alguns segundos.
- Atalhos novos: **F4** (segurar) fala na aba Bíblia; **Enter/Esc** confirmam/descartam a referência ouvida; **Espaço/Backspace** marcam/desfazem durante a marcação por toque. Não clique no vídeo do YouTube durante a marcação (o Espaço iria para o player).
- O `MEMORIA_CONTINUA.md` do repositório descreve um microfone antigo que já não existe no código desta branch; vale o que está aqui.

## 9. Teste único — o que o usuário faz
1. Subir o ZIP (pastas `src` e `tests`) → aguardar o **Actions** ficar verde. Se falhar, copiar o texto do erro **inteiro**.
2. Instalar a versão nova **uma vez** e abrir com a internet ligada (a 1ª voz baixa o modelo, ~150 MB).
3. **Bíblia por voz**: aba Bíblia → escolher o microfone/interface → falar perto e ver o medidor se mexer → segurar o botão (ou F4) e dizer "João três dezesseis" → soltar. Esperado: "Entendi: João 3:16" → Enter projeta. Testar também "Salmos vinte e três", "Romanos oito vinte e oito", "1 Coríntios 13 versículo 4 ao 7". Anotar o que o app "ouviu" nos que errarem.
4. **Marcação por toque**: carregar uma música playback → Cadastrar letra → colar a letra com linha em branco entre estrofes → dar play → Iniciar marcação por toque → Espaço a cada estrofe → Espaço extra ao fim. Depois voltar a música ao início e conferir se a letra projeta sozinha nos tempos marcados (a projeção entra 1,5 s antes).
5. **Letra sem tempos**: carregar uma música que só tenha letra sem tempo no catálogo e usar "Procurar letra"; confirmar com Sim e ver se a caixa enche e a sincronização começa.
6. **Limpar telão**: projetar um versículo → Limpar telão → anotar se a tela própria some e o que o Holyrics mostra.
7. Mandar de volta: erros do Actions (se houver), o que o app ouviu de errado, e qualquer comportamento estranho, tudo de uma vez.
