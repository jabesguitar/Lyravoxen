# Histórico de alterações — cadastro de letras Lyravoxen

Este arquivo registra decisões, código, testes e limitações por etapa. Não substitui os commits; complementa-os para retomada.

## 27/11/2026 — Especificação e pesquisa
- Requisitos confirmados: sincronização automática e manual; identificar estrofes/coro; preservar repetições na ordem cantada; salvamento permanente; aba equivalente no controle remoto; rapidez ao vivo; preservar layout moderno e funções úteis; remover apenas botões sem função.
- Pesquisa: Librosa recurrence/novelty para recorrência e fronteiras; WhisperX com modelo português para forced alignment; sherpa-onnx (modelo português de forced alignment não confirmado); Essentia (AGPLv3/comercial, revisar licença); keytool como referência estrutural; hookfinder não reutilizável devido à licença source-available.
- Criada branch isolada `feature/lyrics-structure-fast-sync` a partir de `feature/offline-library-on-redesign`.

## 27/11/2026 — Protótipo de estrutura textual
- Novo arquivo: `src/Lyravoxen.Desktop/Models/LyricsStructureAnalyzer.cs`.
- Parser de cabeçalhos de estrofe/verso, coro/refrão, ponte, intro e final; marcador de repetição `2x`/`x2`.
- Blocos textualmente idênticos compartilham identidade; execução preserva ocorrências separadas e ordem.
- Partes sem evidência permanecem incertas e exigem revisão.
- Testes adicionados a `tests/Lyravoxen.TimelineTests/Program.cs`; arquivo incluído no projeto de testes.
- Workflow atualizado para disparar na branch.
- **Não validado:** .NET SDK ausente no ambiente desta sessão; nenhuma execução Actions foi observada. Não declarar compilação ou testes aprovados.

## Próxima etapa
Executar build/testes no GitHub; corrigir falhas; então prototipar análise de áudio preparatória para encontrar seções musicais recorrentes e alinhar a letra em português. Depois integrar somente na aba Cadastro de letras e nos serviços mínimos, mantendo XAML/tema/layout moderno e recursos existentes fora do escopo.


## 27/11/2026 — Modelo e arquivo permanente
- `LibraryVideo` recebeu `Sections` e `ExecutionOrder`, com compatibilidade aditiva para registros antigos via inicializadores padrão.
- `PermanentLyricsArchive` agora clona e persiste esses dados; também permite salvar quando há ocorrências estruturais mesmo sem cues temporais.
- Teste de persistência acrescentado: salvar registro com estrofes/coro e reabrir, verificando que as ocorrências de coro continuam separadas.
- **CI pendente:** execuções Actions foram canceladas por concorrência de commits. O teste foi escrito, mas não executado/aprovado ainda.
- Nenhuma alteração visual foi feita; a interface moderna permanece intacta.


## 27/11/2026 — Correção do teste de contagem
- Log real do Actions identificou falha na asserção de `Program.cs:263`, que esperava 6 ocorrências.
- Caso contém 3 estrofes + 1 coro + 1 coro + coro final 2x = 7 ocorrências. O valor esperado no teste estava incorreto.
- Corrigida a expectativa para 7, sem alterar o algoritmo de produção.
- Aguardando CI completo para validar a correção.


## 27/11/2026 — Protocolo de continuidade e execução
- Usuário pediu continuidade sem interrupções por perguntas rotineiras: avançar até entregar versão testável; perguntar somente quando uma mudança puder afetar estrutura ou funções já funcionais.
- Acordado manter dois registros no GitHub: `MEMORIA_CONTINUA.md` (histórico cronológico) e `RETOMADA_ATUAL.md` (estado compacto para retomar).
- Ao iniciar nova conversa, consultar registros, branch, commits e Actions antes de agir.
- Ao concluir marcos, atualizar os arquivos e registrar testes/resultados reais. Se o GitHub não estiver acessível, preparar Markdown na conversa e informar que não foi publicado.
- Nenhuma autorização para merge na main, remover recursos funcionais ou modificar arquitetura fora do escopo foi concedida.


## 27/11/2026 — Liberdade de remodelagem da aba
- Usuário esclareceu que não considera úteis os comandos atuais da aba Cadastro de Letras, pois não funcionaram no uso esperado.
- Autorizou explicitamente remodelar a área, substituir controles e fluxo, criar uma experiência moderna e funcional e remover comandos antigos que não tenham utilidade comprovada.
- A preservação passa a priorizar o resultado do produto e os dados existentes, não a manutenção de botões antigos. Nenhuma função fora da aba Cadastro de Letras deve ser alterada sem necessidade e justificativa.
- Direção de produto: dois fluxos claros — Automático (análise/alinhamento do áudio real com confiança e revisão) e Manual (blocos/cartões grandes, marcação durante playback, navegação, desfazer, reordenação e repetição de ocorrências sem duplicar texto-fonte).
- Persistência obrigatória por música, com recuperação da letra, estrutura, ordem de execução e tempos. O acesso pelo controle remoto será desenvolvido após auditoria do protocolo atual, sem quebrar Ao Vivo/Bíblia.
- O usuário autorizou pesquisa técnica e execução de comandos disponíveis para trazer a solução integrada e pronta; não quer ser chamado para testar etapas intermediárias.
- Estado: decisão aprovada; implementação e validação ainda pendentes. Não declarar a aba pronta antes de build e testes.


## 27/11/2026 — CI Windows #931 concluído
- Run do GitHub Actions: `46067616951`, associado ao commit `6350d2819950907e7490422353cd1b6d52c7322f` na branch `feature/lyrics-structure-fast-sync`.
- Jobs `build` e `cloud-worker-tests`: ambos `completed/success`.
- O job Windows concluiu Restore, Build, `Test cue timeline`, pacote portátil, instalador Inno Setup e upload dos artefatos.
- O job cloud concluiu testes do serviço de mídia offline, Help coverage, verificações C#, Cloudflare Worker e página remota/mobile.
- Artefatos: `Lyravoxen-Windows-x64` (ID `15231637109`), `Lyravoxen-Windows-x64-portable` (ID `15231617268`), `Lyravoxen-Installer-Windows-x64` (ID `15230839460`). Integridade dos ZIPs baixados verificada.
- Resultado limitado ao código do commit acima e às verificações configuradas; não valida alinhamento de áudio cantado, a UI remodelada ou cadastro pelo controle remoto.


## 27/11/2026 — Auditoria funcional inicial do fluxo atual

### Interface WPF existente
- A aba atual ainda é composta por trilho de ferramentas, editor de texto completo e prévia.
- Os comandos expostos incluem limpeza/consulta de letra, estimativa, cadastro rápido, salvar/sincronizar, reconhecimento e marcação por toque.
- O texto orienta separar frases por linha e estrofes por linha em branco; isso ainda exige organização manual da letra.

### Reconhecimento e marcação
- `StartAutoRecognitionAsync` cria `LiveAudioTranscriptionService` associado ao instante de reprodução e recebe transcrições progressivas enquanto a música toca. O fluxo observado é reconhecimento incremental ao vivo; não é ainda forced alignment completo de toda a gravação nem análise estrutural preparatória do áudio.
- `MainWindow.TapSync.cs` usa `TapSyncPlanner.SplitStanzas`: um toque marca o início de uma estrofe. A prévia usa `BuildCues` e a constante `TapSecondsPerLineForLast = 3.0` para estimar a duração das linhas finais. Não há cartões individuais por frase/linha, nem navegação/reordenação individual de ocorrências nessa tela.
- A estrutura nova (`LyricsStructureAnalyzer`) tem testes no projeto de timeline, mas a classe principal `MainWindow.xaml.cs` não a chama. Portanto, o parser validado ainda não alimenta a experiência WPF.

### Persistência e recuperação
- `PermanentLyricsArchive` clona e persiste `Sections` e `ExecutionOrder`.
- Porém, `PersistCurrentCues` cria/atualiza os itens da pasta "Músicas sincronizadas" copiando metadados, `FullLyrics` e `Cues`, sem copiar `Sections` e `ExecutionOrder`. Essa integração precisa ser corrigida antes de depender do novo modelo na interface.
- A chave do arquivo permanente prioriza o ID do vídeo; para outros itens, usa URL e, como fallback, metadados. O fluxo de reabertura precisa ser validado para manter a identidade exata da gravação.

### Controle remoto
- A página HTML atual expõe somente duas abas: Bíblia e Ao Vivo.
- O servidor atende comandos pela rota `/api` autenticada por PIN; o dispatcher remoto já conhece comandos de Ao Vivo, Bíblia e marcação por toque, mas ainda não há editor remoto de cadastro de letras.
- A nova aba deverá usar o mesmo modelo/serviço do Windows. O contrato atual e a leitura do corpo HTTP ainda precisam ser auditados antes de decidir se a edição de letras usará novos comandos ou uma operação POST dedicada. Não quebrar as rotas já usadas por Ao Vivo/Bíblia.

### Próxima ação técnica
Corrigir e testar a sincronização entre modelo estrutural, biblioteca e arquivo permanente; em seguida terminar auditoria de endpoints/protocolo e decidir o transporte seguro para texto longo. Só então implementar a interface automática/manual e a aba remota.


## 27/11/2026 — Auditoria técnica aprofundada

- **Motor atual:** `LiveAudioTranscriptionService` captura o mix de saída do Windows via WASAPI loopback, acumula blocos de 5 segundos, converte para mono 16 kHz e transcreve com Whisper GGML Base local em português. É reconhecimento progressivo durante reprodução; não oferece forced alignment da gravação inteira, voice separation ou mapa de estrutura musical.
- **Parser atual:** usa parágrafos delimitados por linhas em branco e fingerprints textuais exatos. Repetição textual idêntica pode sugerir coro, mas variações pequenas não são agrupadas; repetição também não é prova musical por si só. A ocorrência ainda não armazena tempo/confiança.
- **Modelo de ocorrência:** `LyricsExecutionOccurrence` guarda ID da seção, rótulo/tipo, índice e origem; campos temporais e confiança ainda precisam ser adicionados no desenho do alinhador.
- **Controle remoto:** `RemoteControlServer.HandleAsync` aceita apenas GET; buffer de requisição de 65.536 bytes; `/api` verifica PIN e encaminha `c` (comando) e `a` (argumento). A página atual tem apenas Bíblia/Ao Vivo. Para texto extenso, a proposta é adicionar rota POST autenticada separada, sem mudar os comandos GET existentes.
- **Preservação de compatibilidade:** nenhuma rota existente foi alterada nesta etapa. A nova rota POST é uma extensão aditiva planejada para o editor remoto, mantendo Ao Vivo/Bíblia sob o mesmo contrato atual.


## 28/11/2026 — Remodelagem da grade, compatibilidade e prioridade online/offline

### Pedido do usuário
- A antiga aba de letras não estava funcional para o fluxo pretendido. O usuário apresentou capturas do Holyrics como referência de pesquisa integrada e edição por grade de slides/cartões: selecionar cada bloco visual, acompanhar a prévia, marcar o instante na música e salvar.
- O usuário também esclareceu que o arquivo baixado offline deve ser cópia de segurança: com internet, a reprodução deve usar o vídeo original do YouTube para que a imagem apareça; sem internet, utilizar o arquivo local.
- O usuário forneceu logs de compilação com ambiguidades WPF/Windows Forms e nomes de controles TapSync removidos.

### Implementações e correções
- Criada a interface “Cadastro de letras / Estúdio de Letras”: pesquisa LRCLIB, pesquisa web alternativa, texto-fonte integral, cartões por slide de projeção, prévia, repetição de bloco, reordenação, marcação manual sequencial, desfazer, salvar e cancelar.
- A busca LRCLIB foi documentada com os campos reais de pesquisa (título/intérprete/álbum); “Pesquisar na web” fica separado para busca alternativa.
- O Whisper local incremental passou a comparar com a sequência expandida de cartões e mapear detecções para o slide; detecções estimadas/baixa confiança ficam sinalizadas.
- `CueTimeline.Normalize` passou a preservar origem, ocorrência, confiança, necessidade de revisão e tipo de marcação.
- `LyricsExecutionOccurrence.LineOrder` armazena a ordem personalizada dos slides; arquivo permanente e clones preservam o campo.
- Corrigidos tipos WPF/WinForms ambíguos em `MainWindow.LyricsEditor.cs`; `KeyEventArgs` é explicitamente WPF.
- `MainWindow.TapSync.cs` foi convertido da marcação por estrofe para cartão/slide e referências antigas a controles removidos foram eliminadas.
- `MainWindow.RemoteLive.cs` calcula estado da marcação a partir de cartões e preserva o comando remoto `tapundo`; `TapUndo()` wrapper mantém atalho de voz.
- Player online-primeiro: fila e seleção na Biblioteca tentam YouTube quando existe vídeo original; a cópia local permanece como reserva até `state == 1`. `playerError` recorre a arquivo local válido.
- A validade do arquivo local agora limita somente o fallback; se o arquivo expirou, o vídeo original ainda pode tocar online.

### Builds e testes confirmados
- **#974**, commit `91a7bc1`, run `46105291580`: sucesso nos jobs `cloud-worker-tests` e `build`.
- **#975**, commit `26c70d1`, run `46105922703`: sucesso nos dois jobs; validou prioridade online e fallback local.
- **#976**, commit `b2a0dfa`, run `46106316394`: sucesso nos dois jobs; suíte auxiliar 17/17; Windows build e `Test cue timeline` aprovados. Artefatos: Windows x64 `15247444367`, portátil `15248120574`, instalador `15248205075`.
- ZIP do portátil (artefato #976) verificado: ZIP externo e interno íntegros (`testzip() == None`), arquivo interno inclui `Lyravoxen.exe` (1.438 entradas). ZIP do instalador íntegro; executável extraído possui cabeçalho `MZ`.

### Limitações e próximo passo
- A sincronização automática ainda é reconhecimento Whisper incremental durante a reprodução, não forced alignment completo anterior ao playback.
- A terceira aba remota dedicada ao cadastro de letras ainda não foi construída; compatibilidade dos comandos de marcação existentes foi mantida.
- Ainda não há teste manual real em música cantada. Próxima ação é a rodada manual controlada do usuário com vídeo online/offline, backup expirado e marcação por cartões, registrando resultados sem assumir sucesso antes da verificação.


## 28/11/2026 — Correções solicitadas após captura do Estúdio de Letras

- Implementado ajuste manual persistente de tempo por cartão e propagação para os cartões posteriores, com passos de 0,1 s e 0,5 s e ação de desfazer.
- Implementado “Continuar daqui”, para quando a música já está em andamento ou a letra é adicionada depois do início.
- Priorização heurística de PT-BR no catálogo, com rótulo explícito de probabilidade.
- Corrigidos os botões desabilitados na primeira seleção do catálogo; a pesquisa permanece independente do player.
- Criado mecanismo inicial de failover YouTube/arquivo local e retorno ao YouTube preservando posição. O monitor é executado em intervalos de 2 s, exige duas falhas consecutivas para evitar falsos positivos e continua ativo durante buffering.
- CI #980 falhou por cobertura da Ajuda; #981 falhou por namespaces ausentes na classe parcial; ambas as causas foram corrigidas.
- CI #982 aprovado integralmente, com artefatos Windows, portátil e instalador.
- CI #983 estava em andamento para validar o último ajuste de detecção em buffering.
- Teste manual de perda/retorno real de internet ainda pendente; CI não substitui esse teste.


## 28/11/2026 — Linha do tempo visual, prévia e controles de ritmo

- Usuário pediu uma linha do tempo sob a prévia da projeção, arrastável com mouse e controlável pelas setas, para revisar o adiantamento/atraso antes de confirmar.
- Implementado slider em escala de segundos da música. As setas movem 0,1 s; Shift + setas, 0,5 s; Page Up/Page Down, 1 s.
- A leitura temporária mostra a alteração prevista para a frase selecionada e a próxima frase marcada. Arrastar não grava nada; Aplicar salva o deslocamento na frase selecionada e nas posteriores; Cancelar restaura o tempo; Desfazer reverte o ajuste aplicado.
- A prévia usa a função pura PreviewShiftAfterAnchor, com teste para garantir que a lista original não é alterada antes de aplicar e que a frase não ultrapassa a anterior.
- Os controles “Acelerar 1%” e “Desacelerar 1%” foram adicionados antes desta etapa e aprovados no build #984.
- Build #985 revelou ambiguidade do tipo KeyEventArgs entre WPF e Windows Forms; corrigido no commit 899ed86.
- Build #986 aprovado: ambos os jobs, compilação WPF, teste de timeline e demais testes auxiliares concluídos com sucesso.
- Artefatos #986: Windows x64 15273631141; portátil 15273516795; instalador 15273197863.
- Teste manual no computador do usuário ainda pendente; roteiro criado em docs/ROTEIRO_TESTES_CADASTRO_LETRAS.md. CI não comprova arraste real do mouse nem queda/retorno de rede no WebView2.


## 28/11/2026 — Failover online/offline e retomada pelo ponto salvo

- Usuário relatou parada de áudio e letra quando a internet caiu, e reprodução offline começando do zero. Screenshot da música “Trio R3 — Te ver” e relatório técnico foram analisados.
- Causa identificada: a sonda YouTube podia lançar exceção em queda de rede; o timer a ignorava sem incrementar o contador de falhas.
- Causa identificada: a reserva offline era procurada apenas por VideoId, mas MP3/WAV importados diretamente não têm necessariamente esse ID.
- Causa identificada: o MediaElement recebia Play antes de MediaOpened/seek, o que permitia iniciar em zero; a posição também não era persistida na ficha offline.
- Correções: probe do YouTube usa método que converte timeout/HTTP exceptions em indisponibilidade; duas falhas consecutivas disparam failover.
- Matcher tenta ID exato e, sem ID, utiliza TrackTitleParser/LyricsPreviewService para limpar nomes completos, separar intérprete/título, retirar sufixos e aceitar apenas correspondência única e válida.
- Arquivos expirados, inexistentes ou correspondências ambíguas não são usados.
- MediaElement espera MediaOpened, posiciona, só então inicia. Os cues em memória são transferidos à cópia offline e a projeção não é limpa durante a troca.
- Posição persistida em LastPlaybackPositionSeconds na biblioteca; salva durante playback, pause e fechamento. Reabrir offline usa o último ponto; selecionar a mesma faixa em execução não reinicia.
- Retorno online mantém áudio local até o YouTube confirmar a reprodução no segundo corrente.
- Diagnóstico/Relatório ampliados para testar YouTube e Whisper nativo e mostrar associação/estado da reserva offline.
- Build #988 falhou por falta de System.IO; corrigido.
- Build #989 aprovado com failover principal.
- Build #990 falhou na análise estática por método legado de normalização redundante; removido.
- Build #991 cancelado por atualização subsequente.
- **Build #992 aprovado**: ambos os jobs, WPF, Test cue timeline, parser de nome de arquivo e diagnóstico Whisper.
- Artefatos #992: Windows x64 15275724729, ZIP portátil 15276283376, instalador 15276926788.
- Teste manual de queda/retorno de internet e retomada no Windows ainda pendente. Roteiro em docs/ROTEIRO_TESTES_FAILOVER_OFFLINE.md.


## 28/11/2026 — Novo áudio de WhatsApp recebido; conteúdo ainda não transcrito

### Solicitação do usuário
O usuário anexou o áudio `WhatsApp Ptt 2026-11-28 at 15.38.00.ogg` e pediu para ouvi-lo, identificar se contém informações sobre o projeto Lyravoxen e, como regra de trabalho, salvar cada etapa antes de avançar. Também pediu eficiência, criatividade e continuidade de desenvolvimento.

### Verificação efetivamente realizada
- Arquivo acessível no ambiente da conversa.
- Duração confirmada: **01:57.493**.
- Formato: **OGG / Opus**, mono, 48 kHz.
- Áudio foi aberto e dividido em trechos sequenciais para inspeção.
- **Não existe transcrição textual confiável disponível neste ambiente neste momento.** Não atribuir ao áudio funcionalidades, requisitos, decisões ou aprovações sem recuperar as palavras com segurança.
- Foi pesquisada uma integração que pode exportar transcrições: **Descript**. O estado consultado informa que ainda não está conectado/instalado; nenhuma conexão foi feita e nenhum dado foi enviado ao serviço.
- Nenhuma ideia ou decisão do conteúdo do áudio foi inferida ou aprovada.

### Estado da etapa
- **Concluído:** recebimento, abertura e identificação técnica do arquivo.
- **Pendente:** transcrever integralmente os 1m57s e separar trechos que mencionam o Lyravoxen de conversas paralelas.
- **Pendente após transcrição:** extrair requisitos, ideias, decisões explícitas, dúvidas e ações; classificar em solicitação do usuário, proposta, aprovada, rejeitada ou pendente; só então planejar implementação.

### Próxima ação
Obter uma transcrição fiel do áudio. O Descript foi sugerido como opção de transcrição, mas depende de conexão/autorização do usuário; alternativamente, o usuário pode fornecer uma transcrição exportada. Depois, revisar o texto completo e registrar cada decisão antes de implementar qualquer mudança.

### Regra de continuidade solicitada
A partir desta etapa, em trabalhos do Lyravoxen: salvar um checkpoint cronológico nos arquivos de memória/histórico antes de avançar para a próxima etapa, registrar alterações e resultados reais, não tratar sugestões como decisões aprovadas e atualizar o registro compacto de retomada. Não afirmar que uma etapa foi salva sem confirmar a atualização do GitHub.
