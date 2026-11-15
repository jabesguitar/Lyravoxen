# Lyravoxen — Aplicativo Windows

Aplicativo WPF para reprodução oficial de vídeos do YouTube, sincronização de letras e projeção em monitor independente.

## Requisitos
- Windows 10 (1809 ou superior) ou Windows 11, 64 bits para o aplicativo principal
- Windows 11 ou Windows Server 2022 (build 20348+) para a transcrição local Whisper
- WebView2 Runtime (normalmente já instalado no Windows 11; pode ser instalado no Windows 10)
- Conexão com a internet para o player oficial do YouTube

## Pacote portátil
O fluxo de compilação do GitHub Actions gera:
- **Lyravoxen-Windows-x64**: arquivos publicados do aplicativo.
- **Lyravoxen-Windows-x64-portable**: arquivo ZIP com o aplicativo publicado.

Para obter o pacote, abra a execução mais recente em [GitHub Actions](https://github.com/jabesguitar/Lyravoxen/actions/workflows/windows-build.yml), entre em **Artifacts** e baixe o ZIP portátil. Extraia todo o conteúdo para uma pasta e execute `Lyravoxen.exe`. Não execute o EXE diretamente de dentro do ZIP.

O pacote experimental desta branch é publicado como self-contained para .NET 9; o WebView2 Runtime continua sendo um pré-requisito separado. A transcrição local também requer o Microsoft Visual C++ Redistributable 2015–2022 (x64) ([instalador oficial](https://aka.ms/vs/17/release/vc_redist.x64.exe)) e Windows 11/Server 2022 ou superior. Ainda não é um instalador tradicional com assistente, atalhos e desinstalador.

## Recursos atuais
- Player oficial incorporado do YouTube; não baixa nem extrai áudio.
- Sincronização manual e automática por timestamps.
- Importação LRC/SRT e edição das frases.
- Projeção independente, temas, tela de espera e encerramento.
- Seleção da tela de saída e recuperação após alterações de monitor.
- Biblioteca, histórico e fila da próxima música.
- Integração configurável com Holyrics.\n- Captura WASAPI restrita à árvore de processos do player WebView2, sem fallback para o áudio geral do computador.\n- Transcrição local experimental em português com Whisper tiny quantizado Q5_0; o primeiro uso baixa o modelo (aproximadamente dezenas de MB) para `%LOCALAPPDATA%\\Lyravoxen\\Models` e reaproveita nas próximas execuções.\n- Trechos reconhecidos entram na linha do tempo e na biblioteca; a precisão com voz cantada pode variar bastante e precisa de avaliação prática.

## Configuração da integração com Holyrics
1. No Holyrics, ative o **API Server Local** e confira o endereço e a porta configurados.
2. Use uma versão do Holyrics com API v2.25.0 ou superior e crie ou selecione um token com as permissões locais `ShowQuickPresentation` e `CloseCurrentQuickPresentation`.
3. No Lyravoxen, abra **Holyrics**, informe o IP do computador onde o API Server está ativo, a porta e o token; use `127.0.0.1` quando os dois programas estiverem no mesmo computador.
4. Clique em **Testar e salvar**. O diagnóstico do Lyravoxen verifica as duas permissões.

A permissão `ShowQuickPresentation` envia cada frase sincronizada ao telão do Holyrics. A permissão `CloseCurrentQuickPresentation` permite limpar a apresentação rápida quando a música muda, termina ou a frase é apagada. O fechamento atua somente sobre a apresentação rápida controlada por essa API; não deve ser usado para encerrar uma apresentação comum de slides.

## Busca automática de letras sincronizadas
Ao carregar um vídeo do YouTube sem letras salvas, o Lyravoxen consulta o catálogo LRCLIB usando o título obtido do vídeo. Se houver candidatos, mostra a gravação, a duração e uma prévia para confirmação antes de aplicar os tempos. O intérprete é opcional e pode melhorar a correspondência. Nem toda música — especialmente gravações ao vivo, versões de igreja ou vídeos com títulos diferentes — possui letra sincronizada nesse catálogo; nesses casos, importe um arquivo LRC/SRT ou cadastre os tempos manualmente.

## Limites e validação
- Alguns vídeos não permitem reprodução incorporada e podem ser recusados pelo próprio YouTube.
- A integração Holyrics requer uma instalação compatível e configuração local.
- O CI valida publicação Windows x64 e testes automatizados da linha do tempo; não substitui teste físico com projetor, WebView2 e Holyrics.
- Compatibilidade real ainda precisa ser validada nos computadores Intel Core i5-3470 e Xeon E5-2689.
- APK Android exigiria um projeto separado.\n- A captura por processo exige Windows 10 2004 (build 19041) ou superior; o runtime Whisper empacotado nesta branch exige Windows 11/Server 2022. Em Windows 10, o player continua disponível, mas a transcrição local é desativada.\n- A transcrição de música cantada não é garantida: Whisper é um reconhecedor de áudio/fala e pode omitir, alterar ou inventar palavras em vocais musicais. Confira os trechos antes de usar em apresentação pública.\n- Ainda é necessário testar fisicamente a exclusividade do processo WebView2, a latência e a carga de CPU no computador da igreja.
