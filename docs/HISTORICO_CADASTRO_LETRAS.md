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
