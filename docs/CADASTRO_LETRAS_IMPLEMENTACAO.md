## Atualização de implementação — 28/11/2026 (HEAD `b2a0dfa`)

### Build #976 aprovado
Run https://github.com/jabesguitar/Lyravoxen/actions/runs/46106316394. Jobs `cloud-worker-tests` e `build`: ambos `completed/success`. A suíte auxiliar passou 17/17 etapas; o Windows passou pela compilação, `Test cue timeline`, empacotamento portátil e instalador. Artefatos: `15247444367` (Windows x64), `15248120574` (portátil), `15248205075` (instalador). ZIP portátil e ZIP do instalador foram verificados; o ZIP portátil interno contém `Lyravoxen.exe`.

### Cadastro de Letras implementado nesta branch
- A interface WPF foi redesenhada como Estúdio de Letras, com pesquisa integrada LRCLIB, pesquisa web alternativa, texto-fonte, cartões por slide de projeção, prévia e comandos de salvar/desfazer/repetir/reordenar.
- A marcação sequencial antiga foi adaptada aos cartões; removidas referências a `TapMarkButton`, `TapStartButton`, `TapUndoButton`, `TapFinishButton`, `TapCancelButton` e `TapProgressText`.
- Controles dinâmicos do diálogo de pesquisa usam tipos WPF totalmente qualificados para evitar ambiguidades com Windows Forms; `KeyEventArgs` também está qualificado.
- Controle remoto e comando de voz continuam chamando os handlers compatíveis de marcação/desfazer; a UI remota dedicada de Cadastro de Letras ainda está pendente.
- Metadados de cue e ordem personalizada dos slides sobrevivem à normalização e persistência.
- Reconhecimento automático permanece incremental via Whisper durante a reprodução. Forced alignment da gravação inteira e teste em músicas reais ainda não foram concluídos.

### Biblioteca Offline: online-primeiro
- A seleção de item offline pela fila ou pela Biblioteca tenta o vídeo original do YouTube quando acessível, mantendo a cópia local temporariamente como fallback.
- O fallback só é descartado quando o player informa que iniciou a reprodução (`state == 1`); um evento `playerError` usa a cópia local se existir e não estiver expirada.
- A validade do arquivo local não bloqueia a tentativa de reprodução online. Sem internet e sem arquivo local válido, o usuário recebe aviso em vez de tentar um arquivo expirado.
- O arquivo local permanece como cópia de contingência, não como fonte prioritária quando o vídeo online está disponível.

### Limites e validação pendente
- O CI testa compilação, timeline/projeção, lint, Ajuda, serviço offline, Worker e página mobile; não valida performance/precisão de alinhamento em gravações cantadas reais.
- Fazer teste manual controlado com vídeo online, fallback offline, item offline expirado com internet e gravações reais na grade de cartões.
- Depois dos testes, continuar a integração de uma terceira aba remota dedicada e pesquisar/medir forced alignment leve em português.

---

# Cadastro de letras — especificação funcional e plano de implementação

Atualizado em 27/11/2026. Branch ativa: feature/lyrics-structure-fast-sync. HEAD de referência: 6350d2819950907e7490422353cd1b6d52c7322f.

## Objetivo
Converter a aba Cadastrar letras em editor de sincronização real, com dois modos (automático e manual), salvamento permanente e acesso equivalente pelo controle remoto.

## Requisitos confirmados pelo usuário
1. **Automático:** colar/informar a letra e vincular ao vídeo/áudio; analisar o áudio real, reconhecer frases e posicionar entradas com timestamps. Revisar/corrigir antes de finalizar. Não usar distribuição uniforme como se fosse reconhecimento.
2. **Manual:** exibir cada frase/linha em blocos selecionáveis; seleção por mouse ou teclado/setas; marcar no instante da reprodução; navegar para trás/frente, desfazer, corrigir e concluir.
3. **Estrutura musical:** separar texto-fonte da sequência de execução; permitir refrões/trechos repetidos sem duplicar ou destruir o texto original.
4. **Persistência definitiva:** guardar título, autor, intérprete, URL/ID da gravação, letra integral, sequência de ocorrências e tempos. Reabrir recupera tudo sem recadastrar/ressincronizar.
5. **Controle remoto:** terceira aba Cadastrar músicas ao lado de Ao vivo e Bíblia; mesma biblioteca/serviços e salvamento, com interface responsiva.
6. **Higiene:** todo botão deve executar ação funcional e verificável; remover controles obsoletos/inúteis, sem remover recursos válidos.
7. Manter solução gratuita e leve, compatível com Windows 10/11 e CPUs antigas sempre que tecnicamente viável. Não mesclar na main sem autorização.

## Auditoria do estado existente
| Componente | Existe hoje | Lacuna |
|---|---|---|
| Whisper local | Captura/transcrição incremental em blocos de 5 s durante playback | Não faz alinhamento integral prévio; repetições e trechos não reconhecidos precisam de solução robusta |
| LiveLyricsMatcher / AutoSyncPlanner | Correspondência sequencial e estimativa de linhas puladas | Não garante alinhamento completo nem repetição estrutural de refrões |
| TapSyncPlanner / MainWindow.TapSync | Um toque por estrofe; Space/botão; salva cues | Não há cartões por frase/linha nem navegação por setas/mouse como pedido |
| PermanentLyricsArchive | JSON com backup, chave por ID/URL/metadados e cues | Auditar todos os caminhos de escrita/recuperação e integrar sequência de ocorrências |
| Controle remoto | Colagem de letra para atual/próxima e ações básicas de toque/estimativa | Falta aba de cadastro e editor completo compartilhando dados do desktop |

## Pesquisa técnica — candidatos, ainda sem decisão
- sherpa-onnx (k2-fsa): verificar forced alignment, português, runtime Windows/CPU e tamanho dos modelos.
- WhisperX: referência de timestamps por palavra/alinhamento; avaliar dependências Python/PyTorch e impacto no tamanho/uso de CPU.
- karaoke-subs: estudar pipeline de alinhamento de letra com áudio, inclusive vocais isolados, sem assumir compatibilidade/licença.
- aubio e Sonic Visualiser/MATCH Vamp: investigar como ferramentas auxiliares de análise/inspeção temporal.

## Arquitetura de trabalho proposta (sugestão técnica, não decisão aprovada)
- Modelo separado para texto-fonte e sequência de execução: cada ocorrência aponta para bloco/linha original e tem posição/tempo próprios; refrão pode reaparecer várias vezes.
- Serviço de alinhamento substituível, para testar e trocar o motor sem redesenhar o editor.
- Modo automático produz timestamps e confiança por trecho; baixa confiança vira pendência de revisão, nunca tempo fictício silencioso.
- Editor manual corrige timestamps automáticos.
- Persistência atômica com backup e chave estável por gravação/ID, integrada à biblioteca e ao arquivo permanente.
- API remota protegida por PIN expõe operações sobre o mesmo serviço/modelo; sem duplicar biblioteca em armazenamento independente do navegador.

## Testes de aceitação
- Música cantada em português; playback sem voz; introdução instrumental; pausas; ruído; versos repetidos; refrão repetido; retorno de seção.
- Automático: frases reconhecidas nos instantes corretos; incertezas visíveis; revisão manual possível.
- Manual: cada frase selecionável por clique e setas; marcação no tempo real; desfazer/voltar/avançar/reordenar; salvar e reabrir.
- Persistência: fechar/reabrir, carregar outra música e recuperar a gravação exata, mesmo sem áudio local.
- Remoto: cadastrar no celular e ver no Windows; editar no Windows e ver no celular.
- Interface: nenhum botão sem ação ou promessa enganosa; controles essenciais acessíveis no celular.
- CI verde não substitui testes funcionais.

## Estado após o CI #931
- Especificação do produto e liberdade de remodelagem aprovadas pelo usuário.
- Auditoria inicial dos componentes de sincronização, analisador textual e arquivo permanente concluída.
- Parser determinístico implementado: marcadores explícitos, repetição 2x/x2, agrupamento textual e ocorrências independentes.
- `LibraryVideo.Sections` e `ExecutionOrder` adicionados; `PermanentLyricsArchive` persiste e recupera os dados estruturais.
- Testes do parser, repetição e persistência passaram no job `Test cue timeline` do run `46067616951`.
- CI: jobs `build` e `cloud-worker-tests` concluídos com sucesso. Windows build, pacote portátil e instalador foram publicados.
- Artefatos do run: `15231637109` (Windows x64), `15231617268` (portátil), `15230839460` (instalador).
- **Ainda pendente:** redesign da UI, alinhamento de áudio real, cartões manuais, auditoria/integração remota e testes funcionais reais.
- A compilação #931 é uma base de validação, não a versão final da remodelagem nem uma solicitação para o usuário testar.
- Próxima ação: auditar handlers e persistência de ponta a ponta, depois auditar o protocolo remoto; em paralelo, preparar benchmark de alinhadores de áudio cantado em português.

## Complemento: detecção automática de estrutura e desempenho ao vivo (27/11/2026)

### Comportamento obrigatório
- Interpretar a letra como texto-fonte + sequência de execução. Exemplo de execução: Estrofe 1 → Coro → Estrofe 2 → Coro → Estrofe 3 → Coro.
- Reconhecer rótulos editoriais quando presentes (`[Estrofe]`, `[Coro]`, `[Refrão]`, `[Refrão 2x]`) e procurar repetições textuais quando não houver rótulos.
- Não depender apenas de repetição textual: comparar padrões do áudio para localizar retornos instrumentais/melódicos e trechos recorrentes.
- A classificação automática deve mostrar confiança e permitir correção rápida. Não declarar como coro uma parte apenas por ser mais alta ou mais repetida sem cruzar evidências.
- Registrar cada ocorrência do coro/estrofe com seu próprio tempo, referenciando o mesmo bloco-fonte. Isso mantém a letra original sem cópias artificiais e respeita a ordem cantada.
- Preparar análise antes do evento quando possível; durante a apresentação, manter o reconhecimento incremental e a projeção responsivos. A análise estrutural pesada não deve bloquear o telão.

### Pesquisa e avaliação de candidatos
| Candidato | O que foi confirmado | Risco/decisão atual |
|---|---|---|
| Librosa recurrence/novelty | Recurrence matrix mede semelhança/repetição entre frames; recurrence-to-lag e novelty ajudam a inferir fronteiras estruturais | Candidato para protótipo offline; Python/NumPy/SciPy e empacotamento ainda precisam ser medidos |
| WhisperX | Há modelo de alinhamento português `jonatasgrosman/wav2vec2-large-xlsr-53-portuguese` | Promissor para forced alignment de transcrição conhecida; PyTorch/torchaudio podem pesar muito |
| sherpa-onnx | Offline, multiplataforma e suporte a CPU; documenta forced alignment | Modelos prontos de alinhamento português não foram confirmados na tabela oficial consultada |
| Essentia | C++ otimizada para análise e uso em tempo real; Windows parcial | AGPLv3/comercial: não adotar sem revisar compatibilidade da licença |
| keytool | Referência descreve CQT/MFCC, recorrência e agrupamento estrutural, com nomes heurísticos de seção | Estudar abordagem e licença antes de reutilizar |
| hookfinder | Detecta segmento de destaque/gancho | Descartado para dependência: licença do repositório não permite cópia, modificação ou redistribuição |

### Proposta de pipeline
1. Parser de texto: separar blocos e interpretar marcadores explícitos de seção/repetição.
2. Comparador textual: normalizar acentos/pontuação e agrupar blocos idênticos ou muito semelhantes; manter exceções para variações de letra.
3. Analisador de áudio preparatório: extrair características harmônicas/rítmicas, recorrência e novidades para encontrar retornos e fronteiras; classificar com heurísticas transparentes e confiança.
4. Alinhador de texto/áudio: prova de conceito com WhisperX português contra o Whisper.net já integrado; medir tempo de análise, RAM, CPU e precisão com áudio cantado (não só fala).
5. Modelo de execução: ocorrências distintas apontam para um mesmo bloco-fonte e guardam timestamps próprios, ordem e confiança.
6. Modo ao vivo: reconhecer continuamente em pequenos blocos e casar com a próxima ocorrência esperada, sem bloquear UI/projeção; fornecer revisão/ajuste manual imediato.

### Regras de preservação
- Layout moderno atual é baseline obrigatório. Não reverter ao layout antigo.
- Preservar todos os handlers e recursos comprovadamente funcionais.
- Limitar alterações visuais à área de cadastro e apenas adicionar os elementos indispensáveis para os novos fluxos.
- Mudanças em serviços/modelos/controle remoto devem ser pequenas e justificadas pelo requisito.
- Fazer cópia/histórico no GitHub por etapa, com branch isolada, commits e registro de arquivos/diffs/testes.
- Nada é considerado pronto sem testes funcionais reais e resultado registrado.

### Estado desta revisão
Pesquisa concluída; branch `feature/lyrics-structure-fast-sync` criada a partir da branch de trabalho atual. Nenhum código de produto foi alterado ainda. Próximo passo: protótipo/testes de estrutura textual e medição comparativa do alinhador português, antes da integração ao layout moderno.


## Protótipo de parser textual (27/11/2026)

Foi criado `src/Lyravoxen.Desktop/Models/LyricsStructureAnalyzer.cs` com uma primeira etapa determinística e leve:
- reconhece cabeçalhos explícitos como `[Coro]`, `[Refrão]`, `[Estrofe 1]`, `[Ponte]`, `[Intro]` e `[Final]`;
- expande marcadores de repetição `2x`/`x2` (limite de segurança 8);
- agrupa blocos de texto idênticos após normalização de caixa, acentos e pontuação;
- cria um registro-fonte por seção e ocorrências separadas em ordem, permitindo que o mesmo coro retorne sem duplicar sua letra;
- blocos sem evidência ficam como `Unknown`/`NeedsReview`, evitando adivinhar que toda parte única é estrofe.

Foram acrescentados testes de sequência Estrofe 1 → Coro → Estrofe 2 → Coro → Estrofe 3 → Coro 2x, repetição textual sem rótulo, `2x`, identidade compartilhada e revisão de blocos incertos. O `.csproj` da suíte inclui o novo arquivo. O workflow foi configurado para considerar a branch `feature/lyrics-structure-fast-sync`.

**Status de validação:** não compilado nesta sessão: o ambiente local não possui .NET SDK e não foi observada execução de GitHub Actions para a branch. Os testes estão escritos, não podem ser declarados aprovados. O parser não faz ainda análise de áudio nem integração de interface/persistência.


## Persistência da estrutura musical

O modelo `LibraryVideo` recebeu duas propriedades aditivas:
- `Sections`: blocos-fonte da letra e sua classificação.
- `ExecutionOrder`: ocorrências independentes que apontam para os blocos-fonte, permitindo repetir coro sem duplicar a letra.

`PermanentLyricsArchive` foi atualizado para clonar e persistir profundamente essas propriedades no JSON, mantendo o arquivo permanente independente do áudio. A condição de gravação também aceita um registro que tenha sequência de execução mesmo que ainda não possua cues temporais. Foi adicionado teste de salvar/reabrir e confirmar duas ocorrências de coro.

**Validação:** ainda pendente. Os workflows disparados durante os commits sucessivos foram cancelados por concorrência; não declarar compilação ou teste de persistência aprovado até uma execução completa.


## Correção da expectativa do teste (27/11/2026)
O primeiro CI chegou ao teste estrutural e falhou porque a expectativa era 6. A fixture tem 3 estrofes, duas entradas unitárias do coro e uma indicação final de coro 2x, totalizando 7 ocorrências. A asserção foi corrigida para 7; não foi necessário mudar o algoritmo do analisador.


## 27/11/2026 — Resultado do CI #931
O run `46067616951` foi consultado diretamente no GitHub. Os dois jobs (`build` e `cloud-worker-tests`) terminaram com sucesso. O job Windows passou por restore, build, testes da linha do tempo, pacote portátil e instalador. Os três artefatos foram publicados e baixados; os ZIPs foram verificados sem erros de integridade. Esta evidência confirma apenas o código presente no commit `6350d2819950907e7490422353cd1b6d52c7322f`; não demonstra ainda funcionamento de interface nova, alinhamento de áudio cantado ou cadastro remoto.


## Auditoria funcional inicial do fluxo atual (27/11/2026)

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


## Auditoria técnica aprofundada (27/11/2026)

- **Motor atual:** `LiveAudioTranscriptionService` captura o mix de saída do Windows via WASAPI loopback, acumula blocos de 5 segundos, converte para mono 16 kHz e transcreve com Whisper GGML Base local em português. É reconhecimento progressivo durante reprodução; não oferece forced alignment da gravação inteira, voice separation ou mapa de estrutura musical.
- **Parser atual:** usa parágrafos delimitados por linhas em branco e fingerprints textuais exatos. Repetição textual idêntica pode sugerir coro, mas variações pequenas não são agrupadas; repetição também não é prova musical por si só. A ocorrência ainda não armazena tempo/confiança.
- **Modelo de ocorrência:** `LyricsExecutionOccurrence` guarda ID da seção, rótulo/tipo, índice e origem; campos temporais e confiança ainda precisam ser adicionados no desenho do alinhador.
- **Controle remoto:** `RemoteControlServer.HandleAsync` aceita apenas GET; buffer de requisição de 65.536 bytes; `/api` verifica PIN e encaminha `c` (comando) e `a` (argumento). A página atual tem apenas Bíblia/Ao Vivo. Para texto extenso, a proposta é adicionar rota POST autenticada separada, sem mudar os comandos GET existentes.
- **Preservação de compatibilidade:** nenhuma rota existente foi alterada nesta etapa. A nova rota POST é uma extensão aditiva planejada para o editor remoto, mantendo Ao Vivo/Bíblia sob o mesmo contrato atual.


## Marco de implementação — correções de sincronização e continuidade (28/11/2026)

### Solicitações do usuário nesta etapa
- Ajustar manualmente o tempo da frase selecionada em décimos ou meio segundo e propagar a correção a todas as frases seguintes.
- Inserir/pesquisar a letra com a música já em andamento e marcar o cartão correspondente ao trecho atual, continuando dali sem reiniciar.
- Trocar automaticamente YouTube → arquivo local quando a internet cair e voltar local → YouTube quando a conexão retornar, preservando segundo atual e dados da letra.
- Priorizar versões brasileiras na pesquisa.
- Corrigir os botões de uso do primeiro resultado que apareciam desabilitados.
- Manter a pesquisa disponível durante a reprodução.

### Código implementado na branch
- **Ajuste fino da linha do tempo:** controles −0,5 s, −0,1 s, +0,1 s, +0,5 s; o deslocamento inicia no cartão selecionado, atinge os tempos já marcados posteriores e fica pendente para cartões futuros ainda sem tempo. A correção respeita uma distância mínima de 0,05 s em relação à frase anterior, é gravada por música e pode ser desfeita.
- **Retomar do cartão atual:** botão “↪ Continuar daqui”; o cartão selecionado recebe o segundo corrente de reprodução e a seleção avança para a frase seguinte. “Iniciar sequência” também respeita o cartão selecionado quando ele ainda não tem tempo.
- **Pesquisa sem reiniciar:** o diálogo de catálogo mantém o player separado; importar o texto não chama carregamento de vídeo. A busca está habilitada no fluxo enquanto a música continua tocando.
- **Catálogo Brasil primeiro:** resultados são ordenados por sinais linguísticos de PT-BR, depois correspondência de título/intérprete e disponibilidade de tempos. O rótulo é “PT-BR provável”/“Português provável”; LRCLIB não fornece filtro ou metadado confiável de país, portanto não se afirma que a origem brasileira foi verificada.
- **Correção do diálogo:** o estado dos botões é atualizado imediatamente após ligar o handler de seleção, resolvendo a seleção inicial que não disparava o refresh. A importação de tempos fica habilitada se houver ao menos um cue.
- **Failover inicial:** monitor de rede a cada 2 s; depois de duas falhas consecutivas tenta usar a cópia offline válida do mesmo vídeo no playhead atual. No retorno da rede, prepara o YouTube no segundo atual enquanto o áudio local continua; só troca quando o player online confirma reprodução. Se a tentativa não confirmar dentro de 12 s, a cópia local continua tocando e uma nova tentativa ocorre mais tarde.
- **Proteção de buffering:** commit 075d774 remove a exigência de o estado do player continuar exatamente em “playing” para o monitor observar a conexão. Assim, o monitor também continua ativo quando o YouTube entra em buffering, desde que a reprodução tenha sido iniciada e não esteja pausada pelo usuário.

### Registro de CI desta etapa
- **#980 — run 46166477303:** falhou em Check Help coverage porque os novos botões ainda não estavam descritos na Ajuda. Nenhuma compilação Windows foi executada.
- **#981 — run 46166686551:** a Ajuda e os testes auxiliares passaram; o build revelou namespaces ausentes no arquivo parcial de failover (File, Directory, Path, HttpRequestMessage, HttpMethod, HttpCompletionOption). Corrigido adicionando explicitamente System, System.IO, System.Net.Http, System.Threading, System.Threading.Tasks, System.Collections.Generic e System.Linq.
- **#982 — run 46167026801, commit e0b25b3:** ambos os jobs concluídos com sucesso. dotnet publish, engine Rust, integração da extensão, Test cue timeline, pacote portátil e instalador aprovados.
- Artefatos #982: Windows x64 15272102711; ZIP portátil 15272262281; instalador 15271444107. Todos constavam como não expirados na consulta.
- **#983 — run 46167514287, commit 075d774:** valida a proteção de buffering adicionada depois do #982; resultado estava em andamento no momento deste registro. Não declarar essa última alteração aprovada até o workflow terminar.

### Limites e validação ainda necessária
- CI comprova compilação e testes automatizados, mas ainda não simula perda/retorno real da internet no WebView2 em reprodução contínua. A troca online/offline ainda precisa de teste manual em Windows com cópia local válida do mesmo vídeo.
- O monitor só consegue manter a música quando existe arquivo offline local utilizável correspondente ao mesmo ID do YouTube; sem essa cópia, não há áudio local para assumir.
- O ajuste implementado é um deslocamento em segundos a partir de um cartão (aplicado à sequência seguinte), não uma alteração da velocidade física da faixa nem uma reamostragem do áudio.
- PT-BR é classificação provável por texto; pode errar em letras curtas, instrumentais ou com palavras compartilhadas entre idiomas.
- A terceira aba remota completa de Cadastro de Letras e o forced alignment prévio integral em português continuam pendentes, conforme o plano anterior.

### Commits desta etapa
- fc547433406e31df1ab910e376ce5629c96022e4: núcleo inicial de failover e retenção de posição.
- c0820a3c385e3ee77552d17012d7f2c0412b27f0: controles de ajuste, retomada de cartão, ranking PT-BR e testes.
- c096a98f152f65206adcf9b2d6388cb16e70f04d: textos da Ajuda e namespace WebView2.
- e0b25b393e043bd4f2898ce9a09e9bb670c2b1e2: namespaces explícitos do failover; base do build #982 aprovado.
- 075d77490459de68f57901716ec88f41f8c28fae: correção de monitor durante buffering; CI #983 pendente nesta atualização documental.

### Regras de continuidade preservadas
- Branch feature/lyrics-structure-fast-sync; main intocada.
- Manter o histórico anterior abaixo, sem apagar snapshots e decisões antigas.
- Não declarar failover real testado antes da simulação manual de rede.
- Após o teste consolidado, atualizar os resultados e então avançar para a aba remota completa e o benchmark de alinhamento em português.
