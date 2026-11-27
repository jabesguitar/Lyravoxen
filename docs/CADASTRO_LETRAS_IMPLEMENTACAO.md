# Cadastro de letras — especificação funcional e plano de implementação

Atualizado em 27/11/2026. Branch: feature/offline-library-on-redesign.

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

## Estado
- Especificação confirmada em 27/11/2026.
- Auditoria inicial dos arquivos principais feita.
- Implementação desta nova etapa ainda não iniciada.
- Próxima ação: mapear gravação/recuperação e atalhos, testar motores candidatos com áudio português e implementar modelo compartilhado + editores.


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
