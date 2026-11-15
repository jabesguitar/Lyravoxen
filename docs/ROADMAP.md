# Roadmap

## Lote 01 — Fundação Windows
- [x] Interface inicial WPF
- [x] Player oficial do YouTube incorporado via IFrame Player API
- [x] Leitura assíncrona do tempo de reprodução
- [x] Projeção em janela independente, preferencialmente na segunda tela
- [x] Biblioteca e histórico locais em JSON, com gravação atômica e recuperação por cópia de segurança

## Lote 02 — Operação e projeção
- [x] Prévia do operador com frase atual e próxima frase
- [x] Atalhos de teclado para navegação e telas de apresentação
- [x] Salvar/carregar letras e tempos em JSON
- [x] Temas de projeção e telas de espera/encerramento
- [ ] Seleção manual do monitor de saída nas configurações
- [ ] Testes práticos de operação com dois monitores e projetores

## Lote 03 — Holyrics
- [x] Configuração de IP, porta e token
- [x] Adaptador de integração com a API do Holyrics
- [x] Teste de permissão e envio de texto
- [ ] Validação prática com uma instalação real do Holyrics

## Lote 04 — Sincronização inteligente
- [x] Importar áudio local MP3/WAV
- [x] Editor manual de tempos, importação LRC/SRT e correção de frases
- [x] Busca de letras sincronizadas com confirmação antes de aplicar
- [x] Testes automatizados da linha do tempo e antecipação configurável no código
- [ ] Transcrição local e offline da música tocada no player, sem depender de LRCLIB ou Holyrics
- [ ] Pré-carregar o modelo de reconhecimento ao iniciar o aplicativo (ou em segundo plano), para não esperar o modelo depois que a música começar
- [ ] Enquanto o YouTube carrega, consultar catálogo e preparar título/artista; iniciar a captura assim que o player emitir áudio
- [ ] Captura isolada do áudio do player via WASAPI process loopback, sem gravar sons de outros aplicativos; avaliar isolamento do processo WebView2 para não capturar abas de pesquisa
- [ ] Motor de reconhecimento local baseado em whisper.cpp, com download opcional do modelo e suporte a CPUs sem AVX2
- [ ] Processamento contínuo em janelas curtas com sobreposição, resultados parciais e timestamps alinhados ao relógio da reprodução
- [ ] Buffer circular para manter alguns segundos de áudio enquanto o reconhecimento trabalha; não atrasar nem acelerar a música
- [ ] Exibir prévia assim que houver trechos confiáveis; indicar estado de reconhecimento e atraso estimado, sem projetar texto incerto automaticamente
- [ ] Converter a transcrição em trechos com timestamps; validar/ajustar os tempos e o texto no editor antes da projeção
- [ ] Fallback para catálogo de letras, arquivos LRC/SRT e cadastro manual
- [ ] Testes práticos com músicas cantadas, ao vivo, instrumentais e diferentes gravações

## Lote 05 — Distribuição
- [x] Compilação automatizada Windows x64
- [ ] Instalador Windows x64
- [ ] Testes no i5-3470 e Xeon E5-2689
- [ ] Aplicativo Android complementar (projeto separado)


## Princípios de qualidade do produto
- [ ] Separar reprodução, captura de áudio, reconhecimento, linha do tempo, projeção e integrações em módulos independentes
- [ ] Usar identificador de sessão por música para impedir resultados atrasados de alterarem a faixa atual
- [ ] Executar reconhecimento em segundo plano com cancelamento ao trocar de faixa e limites de CPU/memória configuráveis
- [ ] Exibir estados reais: carregando, reconhecendo, prévia disponível, sincronizado, sem correspondência e erro
- [ ] Não projetar transcrições incertas sem confirmação ou política de confiança configurada
- [ ] Manter funcionamento principal sem internet após disponibilizar o modelo local, exceto reprodução de links online
- [ ] Criar testes automatizados para troca rápida de músicas, concorrência, timestamps, buffer, cancelamento e perda de conexão
- [ ] Medir latência ponta a ponta e consumo de CPU/RAM em hardware antigo antes de definir requisitos mínimos
- [ ] Criar testes de integração com WebView2 e Holyrics opcional; falha do Holyrics não pode interromper o player nem a projeção própria
- [ ] Publicar versões somente após build, testes automatizados e validação manual dos fluxos críticos
