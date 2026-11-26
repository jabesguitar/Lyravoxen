# MEMORIA_CONTINUA — Lyravoxen
Atualizado em 25/11/2026

## Objetivo do projeto
Aplicativo Windows 10/11 para letras de músicas, sincronização e projeção com Holyrics, com futura versão Android APK. O redesign atual é exclusivamente visual: não alterar funcionalidades, integrações, dados, protocolos ou fluxos.

## Decisões do usuário
- Manter a estrutura e todas as funcionalidades existentes.
- Aplicar nova identidade visual com base na logomarca Lyravoxen aprovada: azul-noite, ciano neon, azul, violeta e detalhe amarelo.
- Botões devem ser modernos, arredondados e com acabamento neon, seguindo a aparência do controle remoto reformulado.
- O aplicativo Windows é a prioridade; Android será etapa futura.
- Não testar funcionalidades ainda. Primeiro concluir o layout de todas as telas e do controle remoto; depois o usuário fará uma rodada completa de testes.
- Não mesclar na main até aprovação visual e funcional.

## Histórico cronológico resumido
1. Projeto Lyravoxen já possuía player YouTube, cadastro e sincronização de letras, biblioteca, histórico, navegador interno, Central Bíblica, projeção própria, integração Holyrics, controle remoto por IP e pedidos.
2. Usuário solicitou preservar a estrutura funcional e reformular apenas a identidade visual.
3. Usuário aprovou a marca Lyravoxen com símbolo de lira/equalizador, ciano/azul/violeta, ondas e nome Lyravoxen.
4. Usuário enviou arquivos de marca para `src/Lyravoxen.Desktop/Assets`: `Lyravoxen.ico`, `Lyravoxen_Icone_App_1024.png`, `LyravoxenSplash_Windows_1920x1080.png` e splash vertical.
5. Criada e usada a branch isolada `redesign/brand-visual-layer`.
6. Ajustes de compilação anteriores: adicionados `using System.IO;` em `Services/AudioMonitorService.cs`, `Services/RemoteControlServer.cs` e `MainWindow.RemoteQueue.cs` após erros CS0246/CS0103. Workflow #644 foi concluído com sucesso antes das alterações visuais recentes.
7. Controle remoto por IP recebeu cabeçalho com a marca, layout mobile, cartões e botões modernos. O usuário verificou a nova aparência, mas a imagem do logo apareceu quebrada.
8. Corrigida a resposta HTTP da rota `/brand.png` (CRLF válido) para o navegador carregar o PNG.
9. Criado `tools/New-Splash.ps1` para gerar `Assets/LyravoxenSplash_Compact.png` em 520 × 460, evitando a splash enorme anterior; workflow chama o script antes de compilar.
10. `Lyravoxen.Desktop.csproj` aponta para a splash compacta e copia o PNG do ícone para a pasta de saída.
11. `MainWindow.xaml`: estilo compartilhado de botões arredondados/neon, abas internas, seletores ComboBox escuros, cabeçalho com logo via site-of-origin.
12. `App.xaml`: gradientes azul-noite nos fundos e painéis.
13. `MainWindow.Help.cs`: janela de ajuda recebeu tema escuro.
14. Atualizado `docs/REDESIGN_VISUAL.md` com o escopo, histórico e pendências.

## Estado atual
- Branch ativa de trabalho: `redesign/brand-visual-layer`.
- Alterações visuais mais recentes estão no GitHub, mas ainda não foram compiladas depois das últimas modificações.
- Splash compacta é gerada durante o workflow; o arquivo PNG não precisa ser enviado manualmente.
- Logo no controle remoto deve carregar pela rota `/brand.png`; falta confirmar após nova compilação.
- Capturas recebidas: Ao vivo, Cadastrar letra, Próxima música, Central Bíblica, Ajuda e duas telas do controle remoto por celular.
- Nenhum teste funcional foi feito pelo usuário ainda.

## Pendências
1. Rodar Windows build na branch e corrigir erros de compilação, caso apareçam.
2. Conferir splash compacta e ícone do Windows.
3. Conferir logomarca no cabeçalho do aplicativo.
4. Conferir o logo do controle remoto por IP no celular.
5. Revisar visualmente todas as áreas: Ao vivo, Cadastrar letra, Próxima música, Central Bíblica, Ajuda e sub-abas Sincronização, Controles manuais, Histórico de vídeos e Biblioteca.
6. Só depois iniciar rodada única de testes funcionais do usuário.
7. Após aprovação, avaliar merge na main.

## Próxima ação
Orientar o usuário a executar o workflow Windows build na branch `redesign/brand-visual-layer` e enviar o resultado.

### Correção de compilação — 25/11/2026
- O Windows Build falhou em `MainWindow.xaml(168,127)` com MC3072: `CharacterSpacing` não existe no namespace WPF.
- Inspecionado o arquivo: havia dois atributos não suportados, nas linhas do subtítulo do cabeçalho e do rótulo WINDOWS.
- Removidos somente os dois atributos `CharacterSpacing`; textos, cores, tamanhos e demais estilos foram preservados.
- Commit da correção: `c37bf76` (`Remove unsupported WPF CharacterSpacing attributes`).
- Próxima ação: executar novamente o workflow Windows build na branch `redesign/brand-visual-layer`. Ainda não há confirmação de compilação após esta correção.


### Redefinição de layout — 25/11/2026
- Usuário pediu redistribuição de botões e visualizações nas abas Ao vivo, Cadastrar letra, Próxima música e Bíblia, sem mexer nas funções; logomarca integrada ao fundo em vez de avatar/botão.
- Commit `44e9393`: reorganizou XAML: cabeçalho com watermark, abas principais com estilo mais destacado, maior espaço para prévia ao vivo, cadastro com ferramentas à esquerda/editor vertical ao centro/prévia à direita, próxima música com rail de pesquisa à esquerda e navegador à direita, Bíblia com painel de prévia maior.
- Commit `cb0fd7c`: ajustou ordem dos elementos no Grid do cabeçalho para compatibilidade WPF.
- Commit `68bd44d`: transformou área de leitura da Bíblia em tela escura de prévia com watermark discreta.
- Commit `4896298`: controle remoto usa logomarca como watermark CSS no cabeçalho e fundo, sem avatar/imagem inline.
- Verificação estática: XAML com 76 nomes x:Name sem duplicatas, 105 eventos e tags balanceadas. Ainda precisa compilar.
- Pendência: executar Windows Build na branch `redesign/brand-visual-layer`; não baixar nem iniciar testes funcionais antes de confirmar build verde.

- Ajuste final do cabeçalho: commit `f672381` faz a logomarca ocupar a largura do cabeçalho como watermark discreta atrás do conteúdo, sem moldura/azulejo de ícone.
- Estado atualizado: alterações visuais em XAML/CSS estão no GitHub; ainda aguardam Windows Build. Não afirmar que a compilação passou até o usuário executar o workflow.


### Revisão das capturas adicionais — 25/11/2026
- As capturas reais em 1366×768 mostraram que a primeira distribuição não estava suficientemente lateralizada: Cadastro de letra ficou com botões no topo, editor excessivamente horizontal e sem prévia visível; Próxima música deixou ações na faixa inferior; Bíblia precisava de prévia mais evidente.
- Commit `a0898f4`: reorganização adicional de Cadastro de letra em trilho esquerdo de ferramentas/marcação e painel direito com editor vertical + prévia atual/próxima. Próxima música: área inferior em painel de status à esquerda e comandos à direita. Biblioteca: navegação/pesquisa à esquerda, seleção/edição e prévia ampliadas à direita. Histórico de vídeos: busca e lista em áreas separadas. Bíblia: prévia com tela escura à direita e controles no mesmo painel.
- Verificação estática: 76 identificadores x:Name sem duplicatas, 105 eventos, tags balanceadas. Isso não é compilação.
- Estado: alterações no GitHub, branch `redesign/brand-visual-layer`; aguardando Windows Build e inspeção visual em 1366×768.

- Correção do Windows Build após erro MC3000 (25/11/2026): o arquivo `src/Lyravoxen.Desktop/MainWindow.xaml` continha três tags de fechamento `</TabItem>` excedentes dentro de `SyncTabs`, após Controles manuais, Histórico de vídeos e Biblioteca. Commit `7114238` removeu apenas essas três tags; verificação estática confirmou tags XML balanceadas. A compilação ainda precisa ser executada novamente no GitHub Actions.


### Refinamento visual solicitado após capturas — 25/11/2026
- Usuário pediu: reduzir o cabeçalho/logo para apenas o símbolo em relevo discreto, liberar espaço de navegação na aba Próxima música, melhorar a área de prévia e distribuição da Central Bíblica e remover a rolagem vertical da coluna de controles da aba Ao vivo.
- Commit `9c3e9b3` em `src/Lyravoxen.Desktop/MainWindow.xaml`: removeu a faixa alta do cabeçalho e manteve o símbolo como marca d'água discreta no canto superior direito; moveu controles de música, Holyrics, projeção e ferramentas para uma barra horizontal flexível na aba Ao vivo; redistribuiu player e painel de sincronização em duas colunas; reduziu o trilho lateral da aba Próxima música de 260 para 220 px; ampliou proporcionalmente as colunas de versículos e prévia da Bíblia e colocou navegação/projeção/limpar telão em uma faixa compacta horizontal.
- Verificação estática após edição: tags XML balanceadas; 76 nomes x:Name sem duplicatas. Eventos Click/SelectionChanged/etc. foram preservados no XAML. Não houve alteração intencional em código C# nem em lógica de funcionalidades.
- Estado: commit enviado para a branch de redesign; ainda precisa executar Windows Build. Depois de build verde, usuário deve conferir o encaixe da barra de controles em sua resolução e a área de prévia bíblica antes de iniciar testes funcionais.


### Acabamento final do cabeçalho e abas principais — 25/11/2026
- Na captura enviada, o usuário confirmou que as quatro áreas principais ficaram perfeitas, exceto o símbolo da marca cortado no topo e os botões principais das abas com aparência antiga.
- Solicitação do usuário: mostrar o símbolo inteiro em relevo em todas as abas; reformular somente os cinco botões de navegação (Ao vivo, Cadastrar letra, Próxima música, Bíblia e Ajuda), mantendo a funcionalidade e o estado selecionado claramente destacado; cor de destaque marcante, harmonizada com o azul escuro/ciano e com presença visual forte.
- Commit `13f61b1` em `src/Lyravoxen.Desktop/MainWindow.xaml`: retirou o símbolo da camada atrás do conteúdo e o posicionou no fim do Grid raiz, acima da área das abas, com 48×48 px, margem superior, visibilidade integral, sombra ciano e sem capturar cliques. Substituiu o estilo das abas principais por gradientes azul-violeta; hover azul mais claro; aba ativa em gradiente ciano/azul/violeta, contorno dourado e brilho ciano, preservando TabControl e a seleção nativa.
- Verificação estática: tags XML balanceadas; 77 nomes x:Name únicos. Ainda não é confirmação de compilação.
- Estado: as alterações foram enviadas para a branch de redesign. O restante do layout foi mantido sem alterações nesta etapa. Próxima ação: executar Windows Build e conferir visualmente o símbolo e as cinco abas. Não iniciar testes funcionais até o usuário considerar o layout finalizado.


### Otimização operacional da aba Próxima música — 25/11/2026
- Usuário solicitou manter os comandos da próxima faixa visíveis sem rolagem, por ser um fluxo de uso ao vivo; remover somente os atalhos Cifra Club, Vagalume, Ouvir Música e Harpa Cristã; preservar as funcionalidades existentes e evitar interferência no trabalho futuro do Claude.
- Commit `98060cf` em `src/Lyravoxen.Desktop/MainWindow.xaml`: painel PRÓXIMA MÚSICA movido para o topo da coluna lateral, com faixa preparada, prévia de letra compacta, opção de avanço automático e botões Preparar próxima, Confirmar letra e Verificar letra; campo de pesquisa, voltar/avançar, atualizar, copiar link, fontes e gestão de abas vêm abaixo. Removidos do painel os atalhos Cifra Club, Vagalume, Ouvir Música e Harpa Cristã; mantidos YouTube, Google e Letras.mus.br.
- Preservados nomes x:Name e manipuladores/eventos dos controles existentes, inclusive PrepareNext_Click, ConfirmNextLyrics_Click, VerifyNextLyrics_Click, AutoAdvanceBox_Changed e os eventos de navegação. Não houve alteração intencional em C# nem em lógica de pesquisa/preparação. A remoção foi somente dos quatro botões de atalho na interface desta aba; não removeu código de suporte nem fontes do restante do app.
- Verificação estática: XML balanceado; 77 nomes x:Name únicos; manipuladores preservados. Ainda não equivale a compilação nem teste funcional.
- Próxima ação: executar Windows Build na branch `redesign/brand-visual-layer`; após sucesso, conferir se a coluna lateral exibe os comandos de próxima música na resolução do usuário e então seguir para os testes integrados quando ele aprovar.


### Identidade das abas e temas claro/escuro — 25/11/2026
- Solicitação do usuário: redesenhar as cinco abas principais (Ao vivo, Cadastro de letras, Próxima música, Bíblia e Ajuda) inspiradas na referência enviada: botão cápsula com ícone circular integrado à esquerda, cores distintas e destaque visual da aba ativa. Também solicitou modo claro e escuro tanto no aplicativo Windows quanto no controle remoto via IP; a aparência atual permanece como modo escuro padrão e o seletor deve ser discreto e identificável.
- Implementação no branch \`redesign/brand-visual-layer\`:
  - \`MainWindow.xaml\`: abas principais passaram a usar cabeçalho composto com ícone circular e rótulo; estilo global das abas foi redesenhado para cápsula, com gradiente e brilho na aba selecionada. Adicionado botão discreto de alternância de tema no canto superior direito. Cores de interface foram convertidas para recursos dinâmicos, permitindo troca de paleta em tempo de execução.
  - \`App.xaml.cs\`: criada paleta clara e método \`App.SetTheme\`; tema é persistido em \`%LOCALAPPDATA%/Lyravoxen/theme-mode.txt\`, iniciando em escuro se não houver preferência.
  - \`MainWindow.xaml.cs\`: botão \`ThemeToggle_Click\` conectado e ícone/tooltip inicializados de acordo com a preferência carregada.
  - \`MainWindow.Help.cs\`: aba Ajuda recebeu o mesmo padrão de ícone circular; janela de ajuda adapta cores ao tema selecionado.
  - \`Services/RemoteControlServer.cs\`: botão discreto sol/lua adicionado ao controle via IP; tema claro tem overrides de cartões, campos, botões, prévia e status; preferência fica no \`localStorage\` do navegador do celular e é reaplicada ao abrir a página.
- Cores de ícones: Ao vivo turquesa, Cadastro de letras rosa, Próxima música azul, Bíblia violeta e Ajuda violeta. A aba ativa recebe gradiente ciano/azul e brilho para ser facilmente identificada.
- A referência de design enviada pelo usuário foi usada como inspiração de composição (círculo com ícone + corpo cápsula), sem copiar os rótulos/cores exatos do exemplo.
- Estado: commits da implementação foram aplicados em sequência no branch; ainda falta executar o workflow Windows Build e testar visualmente o modo claro/escuro no Windows e no celular. A alternância do controle remoto foi validada por inspeção estática do HTML/CSS/JS; nenhuma compilação ou teste em dispositivo foi executado nesta etapa.
- Próxima ação: executar o workflow somente após o usuário confirmar que terminou de solicitar mudanças visuais; se verde, baixar o artefato e revisar ambas as aparências. Não iniciar testes de funcionalidade até o usuário autorizar.


### Correção de compilação Windows — 25/11/2026
- Erro recebido no workflow Windows: ambiguidade entre `System.Drawing.Color/ColorConverter` e `System.Windows.Media.Color/ColorConverter` em `App.xaml.cs` após a introdução do tema dinâmico.
- Correção aplicada sem alterar a lógica visual: aliases explícitos `MediaColor` e `MediaColorConverter` apontam para `System.Windows.Media`, e as conversões de cores do WPF passaram a usar esses nomes inequívocos.
- Atualizado `.github/workflows/windows-build.yml` para também executar automaticamente em push na branch `redesign/brand-visual-layer`, sem alterar o gatilho da branch `main` nem executar Windows Build na branch Android.
- Commits: correção de código `86c7a03212e30b38ea96eed00c8365396b16528d`; gatilho do workflow `be99a23c6ec36a5e23ec60aa099d80407ee2720b`.
- Estado: correção enviada; nova execução do Windows Build deve iniciar pelo push. Ainda não há resultado de compilação posterior à correção. Avisos CS8602/CS4014 anteriores não eram erros bloqueantes e não foram alterados nesta correção.
- Próxima ação: acompanhar a execução do Windows Build na branch `redesign/brand-visual-layer`. Se ficar verde, baixar o artefato Windows e seguir depois para o workflow Android separadamente.


### Refinamento de legibilidade do modo claro — 25/11/2026
- Usuário informou que a aparência geral do Windows EXE ficou boa, mas o modo claro estava pouco inteligível nas capturas das abas Ao vivo, Cadastrar letra, Próxima música e Central Bíblica.
- Escopo confirmado: ajustar somente cores/contraste do modo claro; preservar modo escuro, estrutura, abas, layout e funcionalidades.
- Commit `fc4e5627c9c36911af353f5002adc1cfb1103525` em `src/Lyravoxen.Desktop/App.xaml.cs`: refinou os tons claros dos painéis e superfícies, escureceu bordas/contornos e textos secundários, e aumentou a distinção entre áreas, mantendo o fundo geral claro e os acentos ciano/azul.
- Não foram alterados arquivos de lógica funcional nem o CSS do controle remoto nesta etapa.
- Estado: alteração enviada à branch `redesign/brand-visual-layer`; ainda precisa executar Windows Build e conferir visualmente o modo claro no EXE. O usuário ainda não realizou testes funcionais.
- Próxima ação: executar Windows Build; se ficar verde, abrir o EXE, alternar para modo claro e enviar capturas para validar contraste antes de qualquer outro ajuste.


### Contraste da prévia de letras no modo claro — 25/11/2026
- Usuário enviou captura da aba Ao vivo em uso e apontou que as letras da prévia/projeção ficavam brancas sobre uma superfície branca no modo claro, dificultando a leitura.
- A captura evidencia principalmente o texto da prévia da letra, não a barra de progresso do microfone da Central Bíblica. O ajuste foi limitado aos textos de prévia identificados no XAML.
- Commit `eacecfce36e83c0b93eee05d29ca7c3b70d43864` em `src/Lyravoxen.Desktop/MainWindow.xaml`: os textos `ProjectionPreviewText`, suas duas visualizações vinculadas e `BiblePreviewVerseText` passaram de `Brush_FFFFFF` para `Brush_F2F6F8`, recurso dinâmico que permanece claro no tema escuro e assume azul-marinho no tema claro.
- Nenhuma lógica de sincronização, reprodução ou projeção foi modificada.
- Estado: commit enviado à branch `redesign/brand-visual-layer`; falta Windows Build e validação visual do modo claro/escuro. O usuário ainda não confirmou o resultado.
- Próxima ação: executar Windows Build e verificar a legibilidade da prévia ao vivo e da prévia bíblica nos dois temas.


### Correção específica da prévia na aba Cadastrar Letras — 25/11/2026
- Usuário confirmou que a prévia da aba Ao vivo passou a exibir as letras corretamente, mas a prévia da aba Cadastrar Letras continuava com texto branco sobre fundo branco no modo claro.
- Causa identificada no XAML: o TextBlock de prévia da aba Cadastrar Letras tinha `Foreground="{DynamicResource Brush_FFFFFF}"` fixo, ao contrário do recurso dinâmico corrigido na aba Ao vivo.
- Commit `eaf17f0bf612053e0cb6e80ab694a2b44a7cd87b` em `src/Lyravoxen.Desktop/MainWindow.xaml`: o texto vinculado a `ProjectionPreviewText` dentro da prévia de Cadastrar Letras agora usa `Brush_F2F6F8`, que se adapta ao tema (azul-marinho no claro e claro no escuro).
- Nenhuma função, editor, sincronização ou projeção foi alterada.
- Estado: alteração enviada à branch `redesign/brand-visual-layer`; falta compilar e conferir a tela Cadastrar Letras nos dois temas.
- Próxima ação: executar Windows Build e validar a prévia na aba Cadastrar Letras.


### Consolidação funcional solicitada — 26/11/2026 (em andamento)
- Usuário autorizou avançar sem interromper para testes manuais; os testes de uso real serão feitos por ele após receber o conjunto consolidado.
- Base de trabalho mantida exclusivamente na branch `redesign/brand-visual-layer`; nenhuma outra branch foi alterada.
- Pedidos confirmados: controle remoto com ordem telão → controles → ouvir áudio do PC → link YouTube → próxima música → pedidos em espera → pesquisa/lista da biblioteca; painel de pedidos e painel de biblioteca recolhíveis; gestão de remetentes/IP para pedidos locais e internet; remover som/alerta ao confirmar letra e recolher prévia; corrigir avanço automático; Esc limpa projeção sem fechar o app; modo offline com arquivos autorizados e retenção configurável de 5/7/14/30 dias.
- Inspeção do código revelou que o Worker Cloudflare atual guarda apenas texto e data do pedido, sem IP do remetente. O runtime Cloudflare disponibiliza metadados da conexão ao Worker; para permitir bloqueio na internet será necessário registrar IP no Worker, criar endpoints autenticados de consulta/gestão de bloqueios e propagar o identificador/IP ao desktop. Ainda não implementado.
- Alterações de código preparadas nesta etapa (ainda não commitadas): reorganização da página remota com pedidos antes da biblioteca e ambos recolhíveis; confirmação de letra sem MessageBox/som e recolhimento da prévia após sucesso; Esc limpa a projeção local/Holyrics sem encerrar reprodução nem app; evento MediaEnded de áudio offline passa a acionar a mesma lógica de avanço automático do YouTube.
- A atualização dos testes de página remota também foi preparada para validar a nova ordem e os dois painéis recolhíveis.
- Reconhecimento bíblico por microfone permanece fora do escopo, conforme decisão anterior do usuário; não foi reintroduzido.
- Ainda não há commit, build, teste automatizado executado ou pacote de instalação para esta consolidação. Próxima ação: concluir bloqueio de remetentes e offline, integrar demais correções, executar verificações/build e então entregar uma única versão para teste manual.
