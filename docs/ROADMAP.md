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
- [ ] Captura isolada do áudio do processo do Lyravoxen no Windows (WASAPI process loopback), sem gravar sons de outros aplicativos
- [ ] Motor de reconhecimento local baseado em whisper.cpp, com download opcional do modelo e suporte a CPUs sem AVX2
- [ ] Converter a transcrição em trechos com timestamps; validar/ajustar os tempos e o texto no editor antes da projeção
- [ ] Processamento por blocos durante a reprodução, com indicação clara de atraso e progresso
- [ ] Fallback para catálogo de letras, arquivos LRC/SRT e cadastro manual
- [ ] Testes práticos com músicas cantadas, ao vivo, instrumentais e diferentes gravações

## Lote 05 — Distribuição
- [x] Compilação automatizada Windows x64
- [ ] Instalador Windows x64
- [ ] Testes no i5-3470 e Xeon E5-2689
- [ ] Aplicativo Android complementar (projeto separado)
