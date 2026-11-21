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

6. **Pacote "máximo" para teste único** (mesmo ZIP, cumulativo — ainda **sem teste do usuário**):
   - **Relatório de uso e erros**: botão **📋 Relatório** (aba Bíblia e painel de toque) copia para a área de transferência tudo que o app fez e todos os erros, mais dados do PC (Windows, núcleos, **AVX2 sim/não**). Fica em `%LocalAppData%\Lyravoxen\logs\lyravoxen-AAAAMMDD.log` (guarda 14 dias). Todas as mensagens de status do app entram sozinhas; erros inesperados são registrados e o app **não fecha de repente** (mostra aviso e continua). Arquivos: `Services/AppLog.cs`, `App.xaml.cs`, `MainWindow.Diagnostics.cs`.
   - **Comandos de voz** na aba Bíblia (`VoiceBibleCommandInterpreter`): "próximo", "anterior", "projetar", "limpar o telão", "cancelar", "sim/ok" (só confirma se houver referência esperando) e "versículo dezessete" / "versículos 16 ao 18" dentro do capítulo aberto (pede confirmação).
   - **Correção de nomes mal ouvidos** (`CorrectBookTypos`): "Romano" → Romanos, "Efesio" → Efésios etc. (1–2 letras de diferença, candidato único, só quando há número na frase).
   - **Modo clique** (caixa ao lado do medidor): clicar começa a gravar e clicar de novo envia (alternativa a segurar o botão).
   - **Recentes**: lista das últimas 12 referências projetadas; clicar reabre a referência.

7. **Mais ajustes no mesmo pacote** (ainda **sem teste do usuário**):
   - **Busca de letras mais esperta** (`LyricsSearchQueryBuilder`): além do texto original, procura sem "(Ao Vivo)", "Clipe Oficial", "Playback", "HD" etc. e sem acentos (ajuda músicas como "Reconstrução").
   - **Dica automática para playback**: se em 45 s o reconhecimento não identificou nenhuma frase, o app sugere a Marcação por toque.
   - **Controles manuais**: botões **Tudo −0,5 s / +0,5 s** (desloca todos os tempos e salva) e **Exportar .LRC** (`CueTools`).
   - **Bíblia**: **F2** anterior / **F3** próximo versículo (projetam).
   - O relatório agora mostra a **data de compilação** do app, para saber qual versão está instalada.
   - `docs/GUIA_RAPIDO.md`: guia de operação de uma página.

8. **Bíblia do Holyrics com texto fiel na tela própria** (pedido do usuário: o texto será conferido pela igreja, não pode ter erro; ainda **sem teste**):
   - **Regra**: para versões do Holyrics, o Lyravoxen **nunca** usa Bíblia gratuita. O Holyrics exibe e o Lyravoxen **lê de volta** o texto exato (API `GetCurrentQuickPresentation`, depois `GetCurrentPresentation`, com `include_slides`) e o repete na prévia e na **tela própria** (caixa "Mostrar também na tela própria", marcada por padrão).
   - **Plano B (callback fiel)**: se a leitura falhar, usa o texto que o próprio Holyrics já entregou antes para o mesmo versículo (cache `%LocalAppData%\Lyravoxen\BibleCache\holyrics-<versão>.json`). Se não houver, a tela própria **não mostra nada** e o app explica a permissão que falta (melhor vazio que errado).
   - **⇩ Guardar capítulo do Holyrics**: faz o Holyrics exibir cada versículo do capítulo uma vez e guarda o texto fiel (usar fora do culto; avisa antes). Depois a prévia mostra o texto fiel sem projetar.
   - **Permissões no Holyrics** (Configurações > API Server > Gerenciar permissões do token): habilitar `GetCurrentQuickPresentation` e `GetCurrentPresentation` (além de `ShowVerse`, `GetBibleVersionsV2`, `CloseCurrentQuickPresentation`, `CloseCurrentPresentation` que já eram usadas).
   - Segurança: texto de `GetCurrentPresentation` só é aceito se a resposta citar o livro pedido (evita projetar letra de música como versículo). Faixas de versículos (ex.: 4-7) não entram no cache, só versículos únicos.
   - **Incerteza**: o formato exato da resposta do Holyrics não pôde ser conferido (documentação oficial bloqueada para o Claude; só um resumo de terceiros). O app grava a resposta bruta no relatório (`[holyrics] GetCurrent… → HTTP …`) para o Claude ajustar o leitor no próximo ciclo se necessário.
   - Arquivos: `Models/HolyricsSlideTextExtractor.cs`, `Services/HolyricsBibleCache.cs`, `Integration/HolyricsClient.cs` (`ReadCurrentVerseTextAsync`), `MainWindow.HolyricsBible.cs`.

9. **Diagnóstico de um clique** (ainda **sem teste**): botão **🩺 Diagnóstico** (barra do topo, ao lado de *Conectar Holyrics*) confere de uma vez: conexão com o Holyrics, **cada permissão do token** (inclui `GetCurrentQuickPresentation` e `GetCurrentPresentation`, necessárias para a Bíblia fiel), se a ARC está instalada, ações opcionais `SetF8/SetF9/SetF10` (para decidir como ocultar a tela do Holyrics), microfones, modelo de voz, monitores e acesso ao catálogo de letras. O resultado abre numa janela, é **copiado** e gravado no relatório. Também: a aba Bíblia lembra as caixas "Mostrar também na tela própria" e "Modo clique" (`%LocalAppData%\Lyravoxen\preferencias-biblia.txt`).

10. **Reconstrução e correção de build** (ZIP `Lyravoxen-correcao-build-e-diagnostico`, **sem compilar nem testar**; arquivos: `.github`, `src`):
   - **Erro do Actions corrigido**: no `HolyricsClient.CheckPermissionAsync` (ZIP `correcao-voz-holyrics`) faltava o `return DeniedActions(responseBody);` (erro CS0161). Adicionado.
   - **Workflow**: copia `vcruntime140.dll`, `vcruntime140_1.dll` e `msvcp140.dll` do runner para a pasta do app (instalador e ZIP portátil herdam). Só avisa se faltar, não quebra o build.
   - **Recriados**: `Services/AppLog.cs`, `App.xaml.cs` (erros inesperados não fecham o app), `MainWindow.Diagnostics.cs` (botões **📋 Relatório** e **🩺 Diagnóstico**, na barra do topo), `Models/VoiceBibleCommandInterpreter.cs` + execução em `MainWindow.Voice.cs` (próximo, anterior, projetar, limpar o telão, cancelar, sim/ok, "versículo N [a M]"), **F2/F3** (anterior/próximo versículo), `HolyricsClient.DiagnoseAsync`. O status da aba Bíblia agora entra no log.
   - **Ainda NÃO recriados** (descritos nos itens 6–9 acima, o código foi perdido): correção de nomes mal ouvidos (`CorrectBookTypos`), Modo clique, lista Recentes, `LyricsSearchQueryBuilder`, dica de playback em 45 s, Tudo ±0,5 s / Exportar .LRC (`CueTools`), `docs/GUIA_RAPIDO.md` e **toda a Bíblia fiel do Holyrics** (item 8: leitura de volta, cache, ⇩ Guardar capítulo). O `Diagnóstico` já mostra se o token tem `GetCurrentQuickPresentation`/`GetCurrentPresentation`.
   - Os arquivos `HISTORICO_LYRAVOXEN_4/5/6.md` eram idênticos entre si; este é o `_7` (6 + item 10).

11. **Digitação rápida da Bíblia e foco** (ZIP `Lyravoxen-biblia-digitacao-rapida`, **sem compilar**; arquivos: `src`):
   - Estando na aba Bíblia, **qualquer letra/número digitado vai para o campo de referência**; o livro é destacado na lista enquanto digita (`o` → Oseias, `sl` → Salmos, `1co`). **Enter projeta** (antes só localizava; Ctrl+Enter deixou de ser necessário). Ex.: `jo 3 16` + Enter. Se o nome está incompleto (`ger 3 4`), o app completa com o primeiro livro que começa assim. Arquivo: `MainWindow.QuickBible.cs`.
   - Depois de projetar no Holyrics, o Lyravoxen volta para a frente (a janela "Apresentação Rápida" é do próprio Holyrics e a API não permite escondê-la) e o foco volta ao campo de referência.
   - Voz: teste do usuário (Actions já compila) → "A biblioteca existe, mas o Windows recusou carregá-la [AVX sim; whisper.dll presente]". O erro completo agora vai para o log (📋 Relatório). Plano B se persistir: reconhecedor de voz do próprio Windows com gramática fixa (livros e números), sem DLL nativa nem download.
   - O usuário aceita abandonar a voz se for inviável; prioridade é o atalho rápido por teclado como no Holyrics.

12. **Voz pelo Windows + fixar à frente** (ZIP `Lyravoxen-voz-windows`, **sem compilar**; arquivos: `src`):
   - Relatório do usuário: PC Windows 10 (19045), 4 núcleos, **sem AVX2**; `whisper.dll` presente mas "Cannot load the library… PInvokeError: A operação foi concluída com êxito" (o Windows recusa carregar mesmo com o VC++). Digitação rápida da Bíblia **funcionou**; permissões do Holyrics todas OK no último Diagnóstico.
   - Novo `Services/WindowsSpeechReferenceRecognizer.cs` (pacote `System.Speech`): se o Whisper não carrega, a voz da Bíblia usa o reconhecedor do Windows em pt-BR com gramática fechada (livros, números até 176, "capítulo/versículo", a/ao/até, comandos). Precisa do recurso "Reconhecimento de fala" do Português (Brasil) instalado; senão o app explica como instalar.
   - **A sincronização automática de letras (loopback) também usa Whisper e continua indisponível neste PC**; usar Marcação por toque.
   - Botão **📌 Fixar à frente** mantém o Lyravoxen sobre a "Apresentação Rápida" do Holyrics. Dica: arrastar essa janela do Holyrics para outro monitor (ele costuma lembrar a posição).
   - Onde ficam as músicas: `%LocalAppData%\Lyravoxen\library.json` (+ `.bak`), histórico em `video-history.json`, mídia em `Media\`. Não há backup automático.

13. **Janela do Holyrics para trás** (ZIP `Lyravoxen-janela-holyrics`, **sem compilar**): o botão Fixar à frente não adiantou (a janela "Apresentação Rápida" do Holyrics é de outro programa e também fica por cima). Agora, após projetar, o app manda essa janela para o fundo via Windows (`Services/NativeWindows.cs`) e registra no log "Janelas do Holyrics enviadas para trás: N". Se N=0, o título não bate; se N>0 e nada muda, o Holyrics provavelmente roda como administrador (rodar o Lyravoxen como administrador).
   - **Voz**: no teste, o Windows do usuário não tem nenhum reconhecedor de fala instalado ("instalados: nenhum") e o Whisper não carrega (sem AVX2). Voz inviável neste PC; recomendado abandonar (a digitação rápida cobre o uso).

14. **Backup em pasta (☁ Backup)** (ZIP `Lyravoxen-backup`, **sem compilar**): `Services/BackupService.cs` + `MainWindow.Backup.cs`. Botão ☁ Backup na barra do topo: escolher pasta (sugere a do OneDrive; serve Google Drive/pendrive), fazer backup agora, restaurar, abrir pasta. Gera `Lyravoxen-backup.zip` (library.json, video-history.json, preferences.json, Media, ProjectionBackgrounds; **não** inclui o token do Holyrics). Automático: ao abrir, a cada 10 min e ao fechar, só se algo mudou. Restaurar guarda a biblioteca atual como `.antes-da-restauracao` e pede para reabrir o app.
   - **Roteiro combinado**: (1) backup em pasta [feito]; (2) "Entrar com conta" Google ou Microsoft guardando o backup numa pasta privada do Drive/OneDrive (precisa de cadastro gratuito do app, escolher Google ou Microsoft); (3) controle remoto pelo celular via página na rede local + QR + PIN, voz pelo microfone do teclado do celular; (4) app Android para botão de volume com tela desligada (projeto à parte). Outras ideias: lista do culto, favoritos/recentes, funcionar sem Holyrics, tecla de pânico, histórico do culto, avisos/contagem regressiva, aparência/logo.

15. **Atalhos de navegação na Bíblia** (ZIP `Lyravoxen-atalhos-biblia`, **sem compilar**): `MainWindow.BibleKeys.cs`. ←/→ versículo anterior/próximo (projeta, atravessa capítulos); Ctrl+←/→ ou PageUp/PageDown capítulo; Ctrl+Shift+←/→ livro; Esc limpa o telão. Em campos de texto as setas continuam do campo (exceto o campo de referência vazio/todo selecionado, que fica assim após projetar). F2/F3 e F4 continuam.
   - Teste do usuário (Relatório): **backup funcionou** (zip criado em OneDrive\Lyravoxen); **janela do Holyrics vai para trás** (log "enviadas para trás: 1"); digitação rápida OK.
   - Respostas do usuário: usa **Google** e **Android** (iPhone se não complicar). Próximo: controle remoto pelo celular (página na rede local + QR + PIN, voz pelo microfone do teclado). "Entrar com Google" dispensável: pasta do Google Drive para computador já sincroniza o backup.

16. **Pacote único: celular + ajuda por aba** (ZIP `Lyravoxen-completo`, **sem compilar**; cumulativo, contém tudo dos itens 10–16; novo pacote NuGet `QRCoder` 1.6.0):
   - **📱 Celular** (`Services/RemoteControlServer.cs`, `MainWindow.Remote.cs`): servidor HTTP próprio (TcpListener, porta 5280+, sem precisar de administrador; o Windows pede liberação do firewall em redes privadas). Página para celular (Android/iPhone) com PIN (arquivo `remote-pin.txt`, botão Trocar PIN), campo de referência (voz = microfone do teclado do celular), Projetar, versículo ◀▶, capítulo ◀▶, Limpar telão. Botão 📱 Celular mostra QR code, endereço e PIN. Só controla a aba Bíblia.
   - **❓ Ajuda** (`MainWindow.Help.cs`): última aba "❓ Ajuda" (ou tecla **F1**) abre janelinha com a explicação da aba atual e de cada botão (itens que abrem/fecham; "Abrir tudo"). Textos em `LiveHelp`, `RegisterHelp`, `NextSongHelp`, `BibleHelp` — atualizar quando botões mudarem.
   - Ainda não feito: app Android (volume + tela desligada), lista do culto, favoritos/recentes, funcionar sem Holyrics, avisos/contagem regressiva.

17. **📥 Pedidos de músicas** (no mesmo `Lyravoxen-completo.zip`, **sem compilar**; `MainWindow.Requests.cs`, `Services/SongRequestStore.cs`, rota `/pedido` em `RemoteControlServer.cs`): página pública na rede local (`http://IP:5280/pedido`, sem PIN) onde a pessoa informa o nome do grupo (opcional) e até 10 músicas (nome opcional + link). Os pedidos chegam na lista de espera (botão **📥 Pedidos (N)**, salvo em `pedidos.json`, incluído no backup) com quem enviou e horário. Ações: **Preparar como próxima** (só links YouTube; pede confirmação se já houver próxima), Abrir (na aba Próxima música), Copiar link, Descartar. Proteções: links inválidos/repetidos ignorados, máx. 6 envios/min por aparelho, nada toca sozinho. Só funciona com quem está na mesma rede (Wi-Fi ou ponto de acesso), sem internet; envio de fora da rede exigiria um serviço na internet (não feito).

18. **Pedidos por WhatsApp (sem rede local)** (no `Lyravoxen-completo.zip`, **sem compilar**; `Services/OfflineRequestForm.cs`, `SongRequestStore.MessageToJson`, botões na janela 📥 Pedidos): **📄 Gerar formulário para enviar por WhatsApp** salva `Pedido-de-musicas.html` (arquivo único, abre offline no navegador do celular; número de WhatsApp do operador opcional, lembrado em `pedidos-whatsapp.txt`); a pessoa preenche, toca em Gerar mensagem e envia pelo WhatsApp (link wa.me) — a mensagem traz a lista legível e a linha `#LYRA:<base64>`. **📋 Importar pedido** cola a mensagem e a lê (aceita também texto livre com links: "Conjunto: X" e "nome - link" por linha). Limite: no iPhone o HTML recebido pode não abrir; nesse caso a pessoa manda texto livre.
   - **Opção por internet (pendente, aguardando decisão do usuário)**: um serviço gratuito na nuvem (ex.: Cloudflare Worker + KV) receberia os pedidos em um link público e o Lyravoxen os buscaria periodicamente; exige conta gratuita do usuário e publicação do código (passo a passo a fornecer).

## 5. Feedback do usuário (último teste)
- A lista de versões apareceu e projetou.
- "Limpar telão" deixava o fundo do telão; ele quer ver a área de trabalho.
- O texto local de domínio público tinha palavras estranhas (grafia antiga). Quer usar a Bíblia do Holyrics como servidor e operar tudo dentro do Lyravoxen.

## 6. Pendências / próximos passos
- **Teste único (lista para o usuário)**: ver seção 9. Nada do que foi entregue depois do "Salvar e sincronizar" foi testado; o Actions falhou no ZIP `correcao-voz-holyrics` (corrigido no item 10).
- **Etapa 3 (Vosk com vocabulário restrito)**: só se o relatório mostrar que o Whisper erra muito os nomes dos livros mesmo com a correção automática.
- **Limpar telão no Holyrics**: hoje fecha apresentação rápida e atual. Se o fundo do Holyrics continuar, o Diagnóstico mostra se `SetF8/SetF9/SetF10` existem para este token; então liga-se um botão "Tela preta".
- **Bíblia fiel**: formato real da resposta `GetCurrent…Presentation` ainda não confirmado; ajustar `HolyricsSlideTextExtractor` com a resposta bruta do relatório.
- Reconstrução (Eliã Oliveira): testar busca nova, colar letra + Salvar e sincronizar e marcação por toque.

## 7. Mapa do código (src/Lyravoxen.Desktop)
- `MainWindow.xaml(.cs)`: tela principal; sincronização automática (`StartAutoRecognitionAsync`, `BeginAutomaticSyncAfterSaveAsync`); Central Bíblica (`LocateBibleReferenceAsync`, `ReloadBibleChapterTextAsync`, `ProjectBibleSelectionAsync`).
- `Models/`: `CueTimeline`, `LiveLyricsMatcher`, `AutoSyncPlanner`, `BibleReferenceParser`.
- `Services/`: `LiveAudioTranscriptionService` (loopback + Whisper), `BibleReaderService` (midvash + cache).
- `MainWindow.Voice.cs`: microfone da Bíblia (push-to-talk, F4, confirmação, atalhos). `MainWindow.TapSync.cs`: marcação por toque.
- `Models/CueTools.cs` (deslocar/exportar LRC), `Models/LyricsSearchQueryBuilder.cs` (consultas ao catálogo), `MainWindow.Extras.cs` (botões novos e dica de playback).
- `Services/AppLog.cs` + `MainWindow.Diagnostics.cs` + `App.xaml.cs`: relatório e tratamento de erros. `Models/VoiceBibleCommandInterpreter.cs`: comandos de voz.
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
0. Clicar em **🩺 Diagnóstico** logo ao abrir (confere Holyrics, permissões, microfone, monitores e internet; o resultado já fica no relatório).
1. Subir o ZIP (pastas `src` e `tests`) → aguardar o **Actions** ficar verde. Se falhar, copiar o texto do erro **inteiro**.
2. Instalar a versão nova **uma vez** e abrir com a internet ligada (a 1ª voz baixa o modelo, ~150 MB).
3. **Bíblia por voz**: aba Bíblia → escolher o microfone/interface → o medidor deve se mexer ao falar → segurar o botão (ou F4) e dizer "João três dezesseis" → soltar. Esperado: "Entendi: João 3:16" → Enter projeta. Testar também "Salmos vinte e três", "Romanos oito vinte e oito", "1 Coríntios 13 versículo 4 ao 7". Testar o **Modo clique**.
4. **Comandos de voz** (com um versículo aberto): "próximo", "anterior", "versículo dezessete" (pede confirmação), "projetar", "limpar o telão", "cancelar". Conferir a lista **Recentes**.
5. **Marcação por toque**: música playback → Cadastrar letra → letra com linha em branco entre estrofes → play → Iniciar marcação por toque → Espaço a cada estrofe → Espaço extra ao fim. Voltar a música ao início e conferir se projeta sozinha nos tempos marcados (entra 1,5 s antes).
6. **Letra sem tempos**: música cujo catálogo só tenha texto → Procurar letra → Sim → a caixa deve encher e a sincronização começar.
7. **Limpar telão**: projetar um versículo → Limpar telão → anotar se a tela própria some e o que o Holyrics mostra.
8. **Ajustes**: depois da marcação por toque, testar *Tudo −0,5 s / +0,5 s* e *Exportar .LRC*; na aba Bíblia, **F2/F3**. Com uma música playback e a letra salva, iniciar o reconhecimento automático e ver se, em 45 s, aparece a sugestão da marcação por toque.
9. **Bíblia do Holyrics fiel** (o teste mais importante): no Holyrics, habilitar as permissões `GetCurrentQuickPresentation` e `GetCurrentPresentation` do token → no Lyravoxen escolher a versão do Holyrics (ARC) → Projetar João 3:16 → conferir: (a) projeta no Holyrics, (b) a tela própria mostra o MESMO texto, palavra por palavra, (c) a prévia mostra o texto. Depois testar ⇩ **Guardar capítulo do Holyrics** num capítulo curto (ex.: Salmos 23) fora do culto e conferir a prévia de cada versículo. Se a tela própria ficar sem texto, não precisa fazer nada além de mandar o relatório (ele traz a resposta bruta do Holyrics).
10. **No final, UM só envio**: clicar **📋 Relatório**, colar (Ctrl+V) na conversa e acrescentar: o que errou na voz, o que estranhou, erros do Actions (se houver). O relatório já traz as mensagens do app, o que foi ouvido e os erros.

## 11. Pedidos por grupo do WhatsApp + correção do formulário no celular + controle Ao vivo (SEM compilar, sem teste)
- **Formulário HTML**: o arquivo recebido pelo WhatsApp abre em visualizadores sem JavaScript (a tela mostrava só o nome e os botões mortos). Agora há um formulário simples SEM JS (textarea + botão que envia a `wa.me`) e, se o JS rodar, ele é trocado pelo formulário com cartões. Qualquer erro de script volta ao formulário simples.
- **Grupo**: `Services/WhatsAppSettings.cs` (número, nome e link do grupo, importação automática; arquivo `pedidos-whatsapp.txt`, incluído no backup). Janela Pedidos → ⚙ WhatsApp. Botões "Enviar para o grupo" abrem a lista de conversas do WhatsApp com a mensagem pronta (o WhatsApp não permite preencher texto direto num grupo); "Copiar e abrir o grupo" copia e abre o link de convite. 💬 "Copiar texto de convite" gera a instrução para colar no grupo.
- **Receber do grupo**: o Lyravoxen não lê o WhatsApp sozinho. Copie as mensagens no WhatsApp Web/Desktop (Ctrl+C) e ele importa sozinho (`ClipboardTick`, só textos que parecem pedido); 🗂 Importar conversa exportada (.txt); 📋 Importar (aceita várias mensagens, o nome de quem enviou vira o conjunto). Links já recebidos há menos de 2 dias são ignorados (`pedidos-vistos.txt`).
- **Celular, aba Ao vivo**: Tocar próxima, pausar/continuar, encerrar música, preparar o pedido mais antigo como próxima/trocar (`MainWindow.Remote.cs`, página em `RemoteControlServer.cs`). Tocar/encerrar pedem confirmação no celular.
- Testes novos no `Program.cs` (conversa Android/iPhone/Web, link de grupo).
- Relatório de 19/11: Whisper não carrega neste PC (sem AVX2) mesmo com o pacote NoAvx; usa o reconhecimento do Windows. Pendente investigar com o erro bruto do Actions/pacote.

## 12. Prévia de letra da próxima música e dos pedidos (SEM compilar, sem teste)
- `Services/LyricsPreviewService.cs` (sem janelas): dado o videoId, procura (1) a letra na biblioteca (mesmo vídeo ou a mesma música em outro vídeo), (2) senão pega o título no oEmbed do YouTube, limpa ("Clipe Oficial", "Ao Vivo"...) e consulta o catálogo lrclib. Resultado: ✅ sincronizada / 🟡 só texto ou artista não confere / ❌ não encontrada / ⚠ sem internet, com as 5 primeiras linhas para conferir se é o mesmo louvor. Cache em memória; erro vale só 2 min.
- `MainWindow.NextLyrics.cs`: caixa de prévia no painel PRÓXIMA MÚSICA (aba de pesquisa) atualizada ao preparar a próxima (pelo botão, por pedido ou pelo celular) e ao abrir o app; botão 🔎 Verificar letra. Cada pedido na janela 📥 mostra a verificação (feita em fila, uma por vez, assim que o pedido chega). O celular mostra o resumo na aba Ao vivo.
- Não altera nem salva nada; só informa. Testes novos no `Program.cs`.

## 13. Celular (aba Ao vivo): música e autor da próxima + verificar e confirmar a letra (SEM compilar, sem teste)
- Página do celular: campos Música/Autor (microfone do teclado), 🔎 Verificar letra (`nextcheck`), ✅ Confirmar letra (`nextconfirm`), ⏭ Tocar próxima. O resultado fica na tela por 90 s (a atualização automática de 4 s não apaga mais).
- `LyricsPreviewService.SearchByNamesAsync` procura pelo nome/autor digitados (ou pelo título do vídeo se vazio); `ParseLrc` guarda as frases com tempo. Confirmar grava/atualiza o vídeo na biblioteca (pasta "Músicas sincronizadas") com TrackName, Artist, FullLyrics e Cues; ao tocar a próxima, `LoadVideoAsync` já carrega essas frases e a projeção segue o player.
- Só texto (sem tempos) é salvo mas não roda sozinho; "Tocar próxima" avisa quando não há letra com tempos. Tempos do catálogo são da gravação de estúdio: playback/ao vivo pode desalinhar (usar Tudo ±0,5 s ou marcação por toque).
- Consultas ao catálogo agora em paralelo; timeout do servidor do celular subiu para 45 s.

## 14. Pedidos pela internet, de qualquer rede (Cloudflare Worker + KV, grátis) (SEM compilar, sem teste)
- Pedido do usuário: o HTML enviado pelo WhatsApp deve mandar o pedido direto ao programa, sem exigir a mesma rede.
- Janela 📥 Pedidos → ☁ Pedidos pela internet: endereço do serviço + senha (gerada, mín. 12) + busca automática a cada 20 s. **📄 Gerar código do serviço** salva `worker.js` (traz a página /pedido) e `LEIA-ME-nuvem.txt` (passo a passo no painel do Cloudflare, sem linha de comando). 🧪 Testar conexão (`/ping`).
- Serviço (`Services/CloudWorker.cs`): POST `/enviar` (público, aceita formulário simples e JSON; exige link; texto ≤4000; fila ≤150; guarda 2 dias), GET `/pedidos?v=` e POST `/confirmar` e GET `/ping` (exigem `Authorization: Bearer <senha>` = ADMIN_TOKEN). A busca usa a chave `ultimo` para quase não gastar o limite grátis do KV (≈1000 gravações/listagens por dia, 100 mil leituras).
- `Services/CloudRequestClient.cs` busca/confirma; o app importa com `ImportMessageText` (repetidos são ignorados) e só então apaga do serviço. Pode levar até ~1 min (KV é eventualmente consistente).
- Formulário HTML (`OfflineRequestForm.Build(settings, sendUrl)`): com nuvem, o formulário simples (sem JS) faz POST ao serviço e o completo faz XMLHttpRequest; WhatsApp/grupo viram alternativas. A senha NUNCA vai no HTML. Arquivo `pedidos-whatsapp.txt` (agora com nuvem/senha) segue no backup.
- Risco conhecido: quem tem o link público pode enviar pedidos (só texto com link); não consegue ler a fila sem a senha.

## 15. Vários computadores recebendo os mesmos pedidos da nuvem (SEM compilar, sem teste)
- Problema: o primeiro Lyravoxen a buscar apagava os pedidos do serviço; o outro (igreja) nunca os via.
- Agora o app não apaga nada: cada computador guarda os ids já importados (`_cloudSeenIds`) e o serviço descarta sozinho após 12 h (TTL 43200). `/confirmar` continua existindo no serviço, sem uso. Quem já tem o worker antigo publicado continua funcionando (TTL de 2 dias); regenerar o `worker.js` só muda o TTL.

## 16. Aba Ao vivo do celular "como no computador": sugestão de letra, miniatura/prévia e escolha de músicas (SEM compilar, sem teste)
- Causa do "Confirmar letra não funciona": o celular só confirmava depois de tocar em Verificar (exigia `_remoteLyricsCheck`). Agora há um único candidato (`_nextLyrics`/`_nextLyricsVideoId`) que o app acha sozinho ao preparar a próxima; "Confirmar letra" usa ele (`ConfirmNextLyrics`, também botão ✅ no painel PRÓXIMA MÚSICA do computador).
- Novo comando `livejson` (a página consulta a cada 3 s): o que está no telão (`_currentProjectedLyric`, próxima frase, estado), próxima música (miniatura do YouTube `i.ytimg.com`, status da letra, 5 primeiras linhas num "slide" preto, caixa verde "Encontrei esta letra… Confirmar", campos Música/Autor já preenchidos), pedidos (com status da letra e botão Preparar por pedido: `reqpick`) e busca na biblioteca (`libsearch`/`libpick`: Próxima ou ▶ Tocar já).
- A página monta tudo com `textContent` (títulos/pedidos vêm de terceiros); "Tocar" avisa se não há letra com tempos e se vai trocar a música atual. A miniatura precisa de internet no celular (se faltar, some sozinha).
- `PrepareNextForRemote` centraliza "deixar como próxima" para pedidos, biblioteca e "pedido mais antigo".

## 17. Aba Ao vivo: testes automáticos, correções e novos controles (C# AINDA NÃO COMPILADO)
- Testes rodados (em `tests/js`, Node + Chromium): serviço da nuvem com KV falso (30), formulário com e sem JavaScript e envio ao serviço (23), página do celular contra um "computador" falso com a mesma estrutura JSON do `BuildLiveJson` (45), regex reais do C# aplicadas a conversas Android/iPhone/Web, títulos de vídeo e .lrc (41). Também: XAML bem formado, chaves/parênteses balanceados, símbolos usados existem. NÃO foi compilado (sem .NET no ambiente): rodar o Actions.
- Bug achado e corrigido: nomes que começam com número ("10.000 Razões") perdiam o nome ao importar (a limpeza de numeração era gananciosa). Agora só tira "1.", "2)", "3 -", "•".
- Mensagens de resposta do celular agora ficam fixas no pé da tela (antes ficavam fora da vista).
- Novos controles na aba Ao vivo: ◀ Frase / Frase ▶ (projeção manual), 🔄 Voltar ao automático (aparece no modo manual), ⏪ −10 s / +10 s ⏩, 🔉 − / 🔇 Mudo / 🔊 + (só YouTube). Comandos `cueprev`, `cuenext`, `auto`, `seek`, `vol`.
