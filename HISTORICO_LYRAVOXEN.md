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

## 3. Como aplicar um ZIP de alterações
1. Descompactar. 2. GitHub → branch acima → **Add file → Upload files**. 3. Arrastar as pastas `src` e `tests` (substitui só os arquivos do ZIP). 4. **Commit changes**. 5. Aba **Actions**: verde = compilou e testes passaram. Se der erro, copiar o texto do erro.

## 4. O que já foi feito
1. **Salvar e sincronizar** (ZIP `Lyravoxen-salvar-e-sincronizar`): ao salvar a letra colada, a sincronização automática começa sozinha (ou no primeiro play); pergunta se pode reiniciar a música se já passou de 15 s; projeta na prévia, na tela própria e no Holyrics. Correções: frases puladas não somem mais da letra salva; busca se alarga quando trava; novo `AutoSyncPlanner` e testes.
2. **Correção do XAML**: havia uma tag `<Button ` solta na linha 88 (herdada de edição anterior) que quebrava o build. Corrigida.
3. **Central Bíblica** (ZIP `Lyravoxen-central-biblica`): aba refeita (versão, campo de referência, livros/capítulos/versículos, prévia, Projetar/Anterior/Próximo/Limpar). Novo `BibleReferenceParser` com 66 livros, intervalos (`1Co 13:4-7`), "Jo" = João (antes virava Jó), "1 João" ≠ João. Cache local de livros e capítulos.
4. **Ajustes pós-teste** (ZIP `Lyravoxen-biblia-ajustes`): com a Bíblia do Holyrics não aparece mais o texto local de grafia antiga (só a numeração; o texto é do Holyrics); **Limpar telão** agora fecha a janela de projeção própria (some o fundo e aparece a área de trabalho).

## 5. Feedback do usuário (último teste)
- A lista de versões apareceu e projetou.
- "Limpar telão" deixava o fundo do telão; ele quer ver a área de trabalho.
- O texto local de domínio público tinha palavras estranhas (grafia antiga). Quer usar a Bíblia do Holyrics como servidor e operar tudo dentro do Lyravoxen.

## 6. Pendências / próximos passos
- **Etapa 2 — microfone**: falar "João 3:16", apertar para falar (push-to-talk), escolher dispositivo (interface externa), medidor de nível, confirmação "Entendi: João 3:16" antes de projetar. Reaproveita o `BibleReferenceParser`.
- **Etapa 3** (só se o Whisper errar muito nos nomes): trocar por Vosk com vocabulário restrito.
- **Marcação manual por toque** (uma tecla por estrofe) para músicas **playback** (sem voz): o reconhecimento automático não tem o que ouvir.
- Quando o catálogo de letras só tem texto sem tempo, preencher a caixa automaticamente (hoje o app descarta).
- Música "Reconstrução (Eliã Oliveira)" nunca funcionou na busca; testar pelo caminho "colar letra + Salvar e sincronizar".
- Limpar telão **no Holyrics**: o Holyrics mantém o fundo dele; se for esse o problema, procurar na API/opções dele como ocultar a tela. A documentação completa da API do Holyrics estava bloqueada para o Claude: confirmar as ações disponíveis (texto de versículo, ocultar tela) antes de usar.

## 7. Mapa do código (src/Lyravoxen.Desktop)
- `MainWindow.xaml(.cs)`: tela principal; sincronização automática (`StartAutoRecognitionAsync`, `BeginAutomaticSyncAfterSaveAsync`); Central Bíblica (`LocateBibleReferenceAsync`, `ReloadBibleChapterTextAsync`, `ProjectBibleSelectionAsync`).
- `Models/`: `CueTimeline`, `LiveLyricsMatcher`, `AutoSyncPlanner`, `BibleReferenceParser`.
- `Services/`: `LiveAudioTranscriptionService` (loopback + Whisper), `BibleReaderService` (midvash + cache).
- `Integration/HolyricsClient.cs`: `GetBibleVersionsAsync`, `ShowBibleVerseAsync`, fila de envio.
- `ProjectionWindow`: tela própria (tela cheia, fundo escuro; `Close()` some com ela).
- `tests/Lyravoxen.TimelineTests/Program.cs`: testes que rodam no Actions.

## 8. Cuidados
- Tudo que o Claude entrega vem **sem compilar**: se o Actions falhar, mande o texto do erro.
- A primeira sincronização baixa o modelo de voz; faça-a antes de usar ao vivo.
- Tempo das frases vem de blocos de 5 s: a projeção ao vivo pode atrasar alguns segundos.
