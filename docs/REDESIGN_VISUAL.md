# Redesign visual do Lyravoxen

## Objetivo
Modernizar a identidade visual do aplicativo sem alterar a lógica, os serviços ou os fluxos funcionais existentes.

## Regra de preservação
- Não modificar handlers/eventos, serviços, modelos, persistência, sincronização, reprodução, YouTube, Holyrics ou projeção durante esta etapa.
- Mudanças visuais devem ficar em XAML, recursos visuais, imagens, fontes e propriedades de apresentação.
- Toda mudança futura de funcionalidade deve preservar os recursos e tokens visuais existentes.
- Trabalhar nesta branch; não mesclar em main até revisão visual e testes do usuário.

## Identidade de referência
Logomarca aprovada como referência visual pelo usuário em 25/11/2026: lira branca com equalizador ciano/azul/violeta, linhas brancas, ondas ciano/violeta, nome Lyravoxen em branco e ciano e arco amarelo, sobre azul-marinho.

## Tokens de cor
- Fundo principal: #06152F
- Painéis: #0B2450
- Acento ciano: #00D9F5
- Texto principal: #F2F6F8
- Texto secundário: #B5C8E7
- Botões: #123A66
- Campos: #071A3B
- Bordas: #245B88
- Violeta e amarelo devem ser usados como acentos pontuais coerentes com a marca, sem prejudicar contraste.

## Plataformas
- Windows 10/11: plataforma atual e prioridade do redesign.
- Android APK: etapa futura; exige implementação/adaptação Android própria. Não tratar a splash vertical como substituta automática de uma tela nativa responsiva.

## Arquivos e identidade visual
- Fonte do ícone aprovado: `src/Lyravoxen.Desktop/Assets/Lyravoxen_Icone_App_1024.png`.
- Ícone Windows: `Assets/Lyravoxen.ico`, gerado por `tools/New-AppIcon.ps1`.
- Splash Windows compacta: `Assets/LyravoxenSplash_Compact.png`, gerada por `tools/New-Splash.ps1` em 520 × 460 px.
- Splash vertical 1080 × 1920 permanece reservada para futura versão Android; não é usada como splash do Windows.
- A imagem da marca é copiada para a saída do Windows e carregada pelo controle remoto em `/brand.png`.

## Escopo do redesign desktop
- Aba Ao vivo: manter player, URL, comandos Holyrics, diagnóstico, backup, pedidos, celular, projeção e abas internas; aplicar cartões, cabeçalho de marca, controles arredondados e ciano neon.
- Cadastrar letra: manter área de texto, cadastro rápido, busca, sincronização e marcação por toque; padronizar abas, botões, campos e seletores.
- Próxima música: manter navegador interno, abas, sites de referência, preparar/confirmar/verificar letra; padronizar superfícies e navegação.
- Central Bíblica: preservar livros, capítulos, versículos, referência, microfone e comandos de projeção; aplicar os mesmos estilos.
- Ajuda: janela separada agora usa fundo escuro, tipografia clara, cartões e botões coerentes com a marca.
- Abas internas Sincronização, Controles manuais, Histórico de vídeos e Biblioteca recebem o mesmo estilo de abas e componentes.
- A imagem do cabeçalho WPF usa URI `pack://siteoforigin` para carregar o PNG copiado junto ao executável.

## Regra de preservação
- Não modificar handlers/eventos, serviços de reprodução, sincronização, biblioteca, histórico, YouTube, Holyrics, Bíblia, projeção ou persistência durante o redesign.
- Preservar nomes `x:Name`, bindings, eventos, IDs HTML, funções JavaScript, rotas e parâmetros de API.
- Mudanças visuais ficam em XAML, CSS/HTML de apresentação, recursos e scripts de geração de assets.
- Trabalhar somente em `redesign/brand-visual-layer`; não mesclar em `main` antes de revisão e testes do usuário.

## Histórico da etapa — 25/11/2026
1. Usuário aprovou a marca circular Lyravoxen (lira/equalizador ciano, azul e violeta, ondas e nome em branco/ciano com detalhe amarelo).
2. Usuário enviou o ícone PNG 1024, o ICO e as artes de splash para `src/Lyravoxen.Desktop/Assets`.
3. Workflow Windows #644 compilou com sucesso antes das alterações mais recentes do controle remoto e do redesign abrangente.
4. Capturas do usuário mostraram que o executável testado ainda tinha visual antigo e a splash estava grande demais; o usuário pediu para concluir o redesign antes dos testes funcionais.
5. Controle remoto por IP recebeu nova identidade visual; captura posterior mostrou o cabeçalho, mas a imagem da marca não carregou.
6. Corrigido o framing HTTP CRLF da rota `/brand.png`; a rota serve o PNG da pasta `Assets`.
7. Criado `tools/New-Splash.ps1` para gerar uma splash compacta de 520 × 460 px, com o ícone, o nome Lyravoxen e a frase “LETRAS PARA O SEU LOUVOR”.
8. Workflow `windows-build.yml` agora gera a splash compacta antes de publicar; o `.csproj` aponta para `LyravoxenSplash_Compact.png`.
9. `MainWindow.xaml`: abas internas, botões, campos, seletores e cabeçalho receberam estilos arredondados/neon; a imagem do cabeçalho foi ajustada para carregar o PNG copiado na pasta de saída.
10. `App.xaml`: fundo e painéis receberam gradientes azul-noite.
11. `MainWindow.Help.cs`: janela de ajuda recebeu o tema escuro e botões coordenados.

## Estado atual
- Branch: `redesign/brand-visual-layer`.
- Implementação visual aplicada no código, mas ainda não recompilada após as alterações acima.
- O usuário ainda não iniciou os testes funcionais; os testes devem ocorrer somente depois que todas as telas e o controle remoto estiverem com a identidade visual correta.
- Próxima ação: executar Windows build na branch, corrigir qualquer erro de compilação e então conferir as telas e o controle remoto visualmente.
- Depois da validação visual, o usuário fará uma rodada única de testes funcionais; somente após aprovação considerar merge em `main`.
 
## Controle remoto por IP — atualização de 25/11/2026
- O usuário solicitou que a interface web de controle remoto na rede local acompanhe a identidade visual da splash/logo.
- Redesenho visual aplicado em `Services/RemoteControlServer.cs`: cabeçalho com logomarca, paleta azul-marinho/ciano/azul/violeta e detalhes dourados, cartões, campos, botões e adaptação a telas móveis.
- Foi adicionada a rota GET `/brand.png`, servindo o PNG da marca a partir de `AppContext.BaseDirectory/Assets/Lyravoxen_Icone_App_1024.png`.
- O PNG foi incluído no `.csproj` com `CopyToOutputDirectory="PreserveNewest"`.
- Os IDs HTML, funções JavaScript, chamadas `/api`, `/audio`, `/qr.png`, `/pedido`, PIN e comandos existentes foram mantidos; não houve alteração intencional de protocolo ou comportamento funcional.
- Estado: alterações registradas na branch; compilação e teste visual do controle remoto ainda pendentes.


## Redefinição de layout por áreas — 25/11/2026
- Solicitação do usuário: redistribuir botões e áreas de navegação/visualização em todas as abas, sem alterar funcionalidades; destacar a prévia do que está sendo projetado; tornar o editor de letras mais vertical; deixar a logomarca integrada ao fundo, não dentro de um quadro/botão.
- `MainWindow.xaml`: cabeçalho deixa de exibir o ícone dentro de um quadro e passa a usar a marca como watermark discreta; abas principais ficam maiores, arredondadas e com destaque ciano.
- Ao vivo: coluna de controles lateral preservada, mais espaço horizontal para player e sincronização; painel de sincronização e área de prévia recebem alturas mínimas maiores e a tela de projeção fica mais destacada.
- Cadastrar letra: reorganizado em três áreas — ferramentas na lateral esquerda, editor de texto alto no centro e prévia da projeção à direita. Os controles e seus eventos originais foram mantidos.
- Próxima música: controles de pesquisa e preparação na lateral esquerda; navegador WebView2 destacado e ampliado à direita; eventos e nomes de controles preservados.
- Central Bíblica: livros, capítulos e versículos continuam na área de navegação à esquerda; painel de prévia à direita ganhou moldura de tela escura, watermark discreta e maior largura; comandos Anterior/Próximo/Projetar/Limpar permanecem no painel.
- Controle remoto: logomarca integrada como marca d'água no cabeçalho e no fundo da página via CSS, sem imagem de logotipo embutida como botão/avatar.
- Verificação estática realizada no XAML: 76 nomes x:Name, sem duplicatas; 105 eventos declarados; aninhamento de tags validado. Isso não substitui compilação WPF.
- Nenhum handler, nome x:Name, rota ou protocolo foi intencionalmente alterado. A compilação ainda está pendente.
- Próxima ação: executar Windows Build na branch `redesign/brand-visual-layer`; corrigir qualquer erro de XAML/WPF antes de baixar e inspecionar a interface.


## Revisão das capturas adicionais — 25/11/2026
- As capturas reais do usuário em 1366×768 mostraram que a reorganização anterior de Cadastro de letra não se refletiu corretamente: os comandos ficaram no topo, o editor continuou largo e a prévia lateral não apareceu; Próxima música também deixou os comandos de preparar faixa no rodapé em uma faixa muito horizontal; Bíblia precisava de uma tela de prévia mais evidente.
- Novo commit `a0898f4`: Cadastro de letra foi refeito em duas colunas funcionais dentro do espaço útil: trilho esquerdo de ferramentas e marcação por toque; painel direito dividido em editor vertical e prévia grande da frase atual/próxima. Os nomes x:Name e eventos originais foram preservados, inclusive o campo ManualLyricsBox e controles de marcação.
- Próxima música: a faixa de pesquisa, fontes, abas e navegação do navegador permanece como área de navegação; a região inferior foi redividida em painel de estado/execução à esquerda e painel de comandos à direita, para não deixar botões isolados em uma longa faixa.
- Biblioteca: painel de navegação por pastas/músicas e pesquisa continua à esquerda; área de seleção/edição e prévia em painel maior à direita, mantendo eventos de seleção, edição, projeção e gerenciamento.
- Histórico de vídeos: pesquisa e lista agora ocupam lados distintos, com a lista mais larga e comandos agrupados lateralmente.
- Central Bíblica: preview segue à direita em tela escura, com os controles de navegação/projeção integrados no mesmo painel; a estrutura de livros/capítulos/versículos permanece à esquerda.
- Verificação estática do XAML após o commit: 76 x:Name sem duplicatas, 105 eventos, tags balanceadas e âncoras dos previews/editor/controles existentes. Build ainda não executado.
- Pendência: executar o workflow Windows Build na branch `redesign/brand-visual-layer` e inspecionar o ZIP resultante em 1366×768 antes de qualquer alteração funcional.
