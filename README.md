# Lyravoxen — Aplicativo Windows

Aplicativo WPF para reprodução oficial de vídeos do YouTube, sincronização de letras e projeção em monitor independente.

## Requisitos
- Windows 10 (1809 ou superior) ou Windows 11, 64 bits
- WebView2 Runtime (normalmente já instalado no Windows 11; pode ser instalado no Windows 10)
- Conexão com a internet para o player oficial do YouTube

## Pacote portátil
O fluxo de compilação do GitHub Actions gera:
- **Lyravoxen-Windows-x64**: arquivos publicados do aplicativo.
- **Lyravoxen-Windows-x64-portable**: arquivo ZIP com o aplicativo publicado.

Para obter o pacote, abra a execução mais recente em [GitHub Actions](https://github.com/jabesguitar/Lyravoxen/actions/workflows/windows-build.yml), entre em **Artifacts** e baixe o ZIP portátil. Extraia todo o conteúdo para uma pasta e execute `Lyravoxen.exe`. Não execute o EXE diretamente de dentro do ZIP.

O pacote é publicado como self-contained para .NET 8; o WebView2 Runtime continua sendo um pré-requisito separado. Ainda não é um instalador tradicional com assistente, atalhos e desinstalador.

## Recursos atuais
- Player oficial incorporado do YouTube; não baixa nem extrai áudio.
- Sincronização manual e automática por timestamps.
- Importação LRC/SRT e edição das frases.
- Projeção independente, temas, tela de espera e encerramento.
- Seleção da tela de saída e recuperação após alterações de monitor.
- Biblioteca, histórico e fila da próxima música.
- Integração configurável com Holyrics.

## Limites e validação
- Alguns vídeos não permitem reprodução incorporada e podem ser recusados pelo próprio YouTube.
- A integração Holyrics requer uma instalação compatível e configuração local.
- O CI valida publicação Windows x64 e testes automatizados da linha do tempo; não substitui teste físico com projetor, WebView2 e Holyrics.
- Compatibilidade real ainda precisa ser validada nos computadores Intel Core i5-3470 e Xeon E5-2689.
- APK Android exigiria um projeto separado.
