# Lyravoxen — Aplicativo Windows

Aplicativo WPF para reprodução oficial de vídeos do YouTube, sincronização de letras e envio da projeção ao Holyrics.

## Requisitos
- Windows 10 (1809 ou superior) ou Windows 11, 64 bits
- WebView2 Runtime (normalmente já instalado no Windows 11; pode ser instalado no Windows 10)
- Conexão com a internet para o player oficial do YouTube

## Instalador Windows
O workflow também gera `Lyravoxen-Setup-Windows-x64.exe` usando Inno Setup. O instalador instala no perfil do usuário, cria um atalho no Menu Iniciar e oferece um atalho opcional na área de trabalho. O aplicativo é publicado como self-contained para .NET 8; o WebView2 Runtime permanece necessário para o player e a pesquisa incorporados.

Para obter o instalador, abra a execução mais recente em [GitHub Actions](https://github.com/jabesguitar/Lyravoxen/actions/workflows/windows-build.yml), entre em **Artifacts** e baixe `Lyravoxen-Installer-Windows-x64`.

## Pacote portátil
O fluxo de compilação do GitHub Actions gera:
- **Lyravoxen-Windows-x64**: arquivos publicados do aplicativo.
- **Lyravoxen-Windows-x64-portable**: arquivo ZIP com o aplicativo publicado.

Para obter o pacote, abra a execução mais recente em [GitHub Actions](https://github.com/jabesguitar/Lyravoxen/actions/workflows/windows-build.yml), entre em **Artifacts** e baixe o ZIP portátil. Extraia todo o conteúdo para uma pasta e execute `Lyravoxen.exe`. Não execute o EXE diretamente de dentro do ZIP.

O pacote portátil é publicado como self-contained para .NET 8; o WebView2 Runtime continua sendo um pré-requisito separado. Para instalação assistida, use o instalador Windows acima.

## Recursos atuais
- Player oficial incorporado do YouTube; não baixa nem extrai áudio.
- Busca de letras sincronizadas com confirmação **Sim / Não / Criar** antes de substituir a letra atual.
- Área **Criar / editar letra**: cole a letra completa e inicie a sincronização ao vivo sem reiniciar a música. O operador marca cada entrada no tempo atual; a próxima frase aparece automaticamente e cada marcação é salva na biblioteca local.
- Aba separada **Controles manuais** para navegar, editar, reordenar e projetar frases sem comprimir a prévia do telão.
- Importação LRC/SRT e edição das frases; seleção de 1 a 10 linhas exibidas em blocos consecutivos. Por exemplo, quatro linhas permanecem juntas até a quarta frase e então são substituídas pelas quatro seguintes.
- Reconhecimento automático experimental e gratuito da voz cantada: captura o áudio de saída do Windows (WASAPI loopback) e processa segmentos localmente com Whisper.net/whisper.cpp. Não usa API paga, conta, chave ou assinatura. O modelo multilíngue base é baixado gratuitamente na primeira utilização e permanece no computador para uso posterior offline.
- O runtime Whisper.net.Runtime.NoAvx foi escolhido para ampliar compatibilidade com processadores Intel mais antigos, sem exigir AVX2. O reconhecimento usa CPU e pode ter atraso dependendo do processador; instrumentos, coral, reverberação e pronúncia cantada podem reduzir a precisão.
- A transcrição local retorna segmentos com tempos aproximados. O aplicativo compara o texto com a próxima frase esperada e exige correspondência sequencial. Revise a sincronização antes de usar em uma apresentação.
- O histórico mantém o título do vídeo e o link/ID do YouTube, mas exibe e pesquisa os metadados informados pelo usuário: título da música, autor/compositor e intérprete.
- Prévia ampliada dentro do aplicativo. Não existe janela de projeção local: o Holyrics é o único responsável por exibir as letras, evitando que o Lyravoxen cubra a tela principal.
- Biblioteca local de letras sincronizadas pesquisável por título e intérprete corrigidos; letras criadas podem ser reutilizadas em outras gravações da mesma música.
- Aba Próxima música com atalhos internos para YouTube, Google, Cifra Club, Vagalume, Letras.mus.br, Ouvir Música e pesquisa da Harpa Cristã; copiar link da página atual ou da música em reprodução.
- Integração configurável com Holyrics: envia cada bloco de letras, mantém a ordem das atualizações e permite encerrar a apresentação rápida e a apresentação convencional pelo botão Sair da projeção.

## Limites e validação
- Alguns vídeos não permitem reprodução incorporada e podem ser recusados pelo próprio YouTube.
- A integração Holyrics requer uma instalação compatível, configuração local e permissões de API para ShowQuickPresentation, CloseCurrentQuickPresentation e CloseCurrentPresentation.
- A busca automática consulta a biblioteca local e o LRCLIB por título e intérprete, com nova tentativa pelo título isolado. Quando não há letra temporizada, a opção Criar permite marcar cada frase ao vivo no áudio que já está tocando. A sincronização automática por reconhecimento de voz ainda não está implementada.
- O CI valida publicação Windows x64 e testes automatizados da linha do tempo; não substitui teste físico com projetor, WebView2 e Holyrics.
- Compatibilidade real ainda precisa ser validada nos computadores Intel Core i5-3470 e Xeon E5-2689.
- A versão Android está em desenvolvimento no projeto separado `android/`, na branch `feature/android-foundation`. A fundação inicial e o workflow de APK debug estão criados; a paridade funcional ainda está pendente.
