# LYRAVOXEN — ESTADO ATUAL E RETOMADA (28/11/2026)

**Branch ativa:** `feature/lyrics-structure-fast-sync`  
**HEAD de código validado:** `b2a0dfabdad6e470e0ec50d0cccdadd2844aafe8`  
**Build de referência do código funcional:** Windows build #977 — [run 46106917809](https://github.com/jabesguitar/Lyravoxen/actions/runs/46106917809), concluído com sucesso nos dois jobs. O commit #977 é documental; o código funcional continua sendo o de `b2a0dfa`.  
**Main:** não houve merge nem alteração na `main`.

## Resultados CI confirmados

- **#974 — commit `91a7bc1`:** compilação Windows, testes de timeline e suíte auxiliar concluídos com sucesso; gerados Windows x64, ZIP portátil e instalador.
- **#975 — commit `26c70d1`:** mesmos dois jobs aprovados; validou a preferência pelo vídeo original do YouTube e fallback para a cópia local quando o player falha.
- **#976 — commit `b2a0dfa`:** ambos os jobs concluídos com sucesso. A suíte auxiliar passou 17/17 etapas; o job Windows passou por Rust, build C#, bundle, `Test cue timeline`, ZIP portátil e instalador.
- **Artefatos do #976:** Windows x64 (`15247444367`), portátil (`15248120574`) e instalador (`15248205075`).
- **Integridade conferida nesta sessão:** ZIP externo do portátil e ZIP externo do instalador passaram em `zipfile.testzip()`; o ZIP portátil interno também passou, contém 1.438 entradas e inclui `Lyravoxen.exe`; o instalador extraído tem cabeçalho `MZ`.
- Os testes CI provam compilação e os testes automatizados configurados; não substituem teste manual real de sincronização com uma música cantada.

## Cadastro de Letras — o que está no código

- A antiga aba de marcação por estrofe foi substituída por **Cadastro de letras / Estúdio de Letras**.
- Texto-fonte completo permanece separado das ocorrências de execução.
- Parser textual reconhece cabeçalhos de estrofe/verso, coro/refrão, ponte, intro/final e marcadores de repetição `2x`/`x2`; blocos repetidos apontam para o mesmo texto-fonte.
- A grade cria cartões por **slide/bloco de projeção** (respeitando `_maxProjectionLines`), e não apenas um botão por linha. Cada cartão mostra o conteúdo do slide, seção/ocorrência, tempo e estado de revisão.
- Marcação manual sequencial: iniciar, marcar o próximo slide com clique/Espaço, desfazer, concluir/salvar ou cancelar. Também existem repetição de bloco, reordenação e prévia.
- Busca integrada gratuita via LRCLIB para título/intérprete/álbum; botão separado abre pesquisa web para outras fontes. A pesquisa LRCLIB não é busca no corpo integral da letra.
- Tempos guardam seção, ocorrência, linha-fonte, confiança, necessidade de revisão e flag manual; normalização e arquivo permanente preservam esses dados.
- A projeção isola seção/ocorrência para impedir vazamento de linhas entre blocos; ordem personalizada dos slides é persistida.
- Reconhecimento automático atual continua sendo **Whisper local incremental durante a reprodução**, comparando linhas reconhecidas com a sequência de cartões e marcando o slide correspondente. Não é ainda forced alignment prévio da música inteira nem foi validado manualmente em uma gravação real.
- Controles de marcação antigos foram removidos/adaptados; os comandos remotos existentes e o atalho por voz mantêm compatibilidade com a marcação sequencial. **A terceira aba remota completa de Cadastro de Letras ainda não foi implementada.**

## Biblioteca Offline — comportamento implementado

- Quando um item offline contém um ID/URL original do YouTube e há internet, a fila e a seleção direta na Biblioteca tentam primeiro o vídeo original, para preservar a imagem/vídeo.
- A referência da cópia local permanece reservada até o player informar estado de reprodução (`state == 1`). Se o player envia `playerError` e a cópia local ainda existe e não expirou, ela é usada como fallback.
- A validade de retenção afeta somente a cópia local: mesmo que o arquivo tenha expirado ou sido removido, o aplicativo ainda tenta reproduzir o vídeo original online.
- Sem internet, o fallback local só ocorre se o arquivo existir e estiver dentro da validade. Caso contrário, a aplicação informa que não existe cópia offline utilizável.
- Arquivos MP3/WAV importados sem vídeo original vinculado não têm como abrir um vídeo do YouTube; permanecem reprodução local.
- Limite conhecido: o fallback após falha online depende do evento de erro do player. Um player travado sem emitir `ready`/`playerError` ainda pode exigir melhoria futura com timeout de inicialização.

## Próximo passo prático

1. O pacote #976 está aprovado para **uma rodada de teste manual controlada**. Use o portátil ou instalador do #976, não os pacotes dos runs anteriores.
2. Testar um item offline com internet (deve abrir YouTube e mostrar o vídeo), o mesmo item sem internet (deve usar o arquivo local), um item com cópia local expirada mas link original válido (deve tentar YouTube), e um vídeo privado/sem incorporação com cópia local válida (deve cair para áudio local).
3. Na aba Cadastro de Letras, testar colar uma letra completa, criar cartões/slides, marcar em reprodução, desfazer/repetir, salvar, fechar/reabrir e conferir que os tempos e a ordem persistem.
4. Registrar resultados reais antes de declarar a remodelagem concluída. Depois avançar para: aba remota dedicada ao cadastro e protótipo/benchmark de alinhamento completo de áudio cantado em português.

## Regras permanentes

- Não fazer merge na `main` sem autorização explícita.
- Não chamar o usuário para testar builds intermediários.
- Não afirmar que o alinhamento automático completo ou a terceira aba remota está pronta.
- Preservar dados e recursos válidos fora do escopo.
- Manter solução gratuita e leve sempre que viável.
- Atualizar este estado e o histórico após novos resultados; snapshots antigos abaixo permanecem como histórico, não como status atual.

---

# HISTÓRICO ANTERIOR PRESERVADO

# ESTADO ATUAL — Lyravoxen (27/11/2026)

**Frente ativa:** remodelagem integral da aba Cadastro de Letras e integração equivalente ao controle remoto.  
**Branch:** `feature/lyrics-structure-fast-sync`  
**HEAD mais recente:** `ad79c1df7d1f8356ffa328750e77cffa8c29fd04` (correção de cópia/restauração de estrutura; CI em andamento)  
**Último CI verde:** run `46067616951` (#931). **CI do ajuste seguinte:** run `46069001270`, em andamento para o commit `ad79c1d`.

## Resultado confirmado do CI
- `build`: sucesso. Restore, build .NET, engine nativa de bloqueio, bundle, testes de cue timeline, pacote portátil e instalador Windows x64 foram concluídos e enviados como artefatos.
- `cloud-worker-tests`: sucesso. Testes do serviço de mídia offline, cobertura da Ajuda, verificações C#, Worker Cloudflare e página remota/mobile concluídos.
- Artefatos criados: `Lyravoxen-Windows-x64` (ID `15231637109`), `Lyravoxen-Windows-x64-portable` (ID `15231617268`) e `Lyravoxen-Installer-Windows-x64` (ID `15230839460`). Os arquivos ZIP baixados foram verificados quanto à integridade.
- Esse CI valida a compilação e os testes automatizados configurados; não valida alinhamento automático de áudio cantado, interface nova, cadastro remoto nem teste funcional de uma música real.

## Estado técnico do Cadastro de Letras
- Criado analisador textual determinístico para cabeçalhos e repetições, separando blocos-fonte das ocorrências de execução.
- `LibraryVideo.Sections` e `ExecutionOrder` adicionados e persistidos no arquivo permanente.
- Testes automatizados da estrutura, repetição e recuperação foram executados dentro do job `Test cue timeline`, que passou no run #931.
- **Ainda não implementado:** nova interface automática/manual, alinhamento real de áudio, cartões por linha/bloco, editor de revisão e terceira aba de Cadastro de Letras no controle remoto.
- A versão gerada pelo run #931 é uma compilação de validação do estado atual da branch, não a versão final para a rodada única de testes do usuário.

## Próximas ações
1. Auditar handlers, persistência e recuperação completos da aba atual para definir o fluxo substituto sem perder registros existentes.
2. Auditar endpoints e protocolo remoto, preservando Ao Vivo e Bíblia.
3. Comparar motores de alinhamento com áudio cantado em português e medir precisão, CPU, RAM e tamanho antes de escolher dependência.
4. Implementar o editor remodelado e integração remota, com testes de unidade/integração.
5. Rodar novo CI completo, validar artefatos e só então preparar a versão consolidada para o usuário.

## Regras de continuidade
- Manter a branch isolada; não fazer merge na `main` sem autorização.
- Não chamar o usuário para testar etapas intermediárias.
- Não declarar análise de áudio, UI ou integração remota concluídas sem evidência.
- Preservar histórico abaixo deste resumo, mesmo quando registros antigos apontarem estados de branches/etapas anteriores.

---

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


### Continuação da implementação — 26/11/2026
- Commit parcial anterior: `40c9d0fbd8351847c0cd3f97f325275d58a4eccd` na branch `redesign/brand-visual-layer` (layout remoto, recolhimento de painéis, confirmação sem alerta, tecla Esc e avanço automático de áudio local).
- Offline: a biblioteca já importava MP3/WAV para a pasta local do aplicativo. Foi ampliada para solicitar validade de 5, 7, 14 ou 30 dias, gravar `OfflineExpiresAt`, excluir somente o arquivo local vencido e manter o registro/letra na biblioteca. Ao selecionar um item vencido, o app informa que a cópia não está disponível em vez de tentar abrir uma URL vazia. O vínculo automático de arquivo offline importado com um pedido de YouTube ainda precisa ser concluído; nesta fase, a importação local não baixa áudio de YouTube.
- Bloqueio de remetentes: o Worker Cloudflare está sendo ampliado para capturar o cabeçalho `CF-Connecting-IP`, guardar o IP junto ao pedido, expor endpoint autenticado `/bloqueios` para listar/bloquear/desbloquear, e recusar envios de IPs bloqueados. O desktop propaga o IP do Worker e mantém lista local nomeada em `%LOCALAPPDATA%/Lyravoxen/pedidos-bloqueados.json`; o servidor HTTP local também consulta essa lista antes de aceitar pedidos. A sincronização e os testes automatizados ainda não foram executados.
- Cuidado técnico: endereços IP públicos podem mudar ou ser compartilhados por várias pessoas (NAT/operadora); o bloqueio por IP é uma barreira operacional, não uma identificação pessoal infalível. MAC address não é disponibilizado ao serviço web remoto.
- Próximas ações: validar que a nova API Worker foi integrada no código gerado, adicionar testes de bloqueio por IP, conferir a associação entre arquivo offline e música pedida, executar lint/testes/build e corrigir erros antes da entrega consolidada.


### Ajuste de robustez — 26/11/2026
- Refinada a tecla Esc para fechar a janela de projeção própria (não apenas limpar o texto), atualizar o botão para “Abrir projeção própria” e enviar o fechamento ao Holyrics. A música continua tocando e o Lyravoxen permanece aberto.
- Ajustado o fim de áudio offline para zerar o estado antigo do player YouTube antes de chamar o avanço automático, evitando que um estado de reprodução anterior impeça a troca.
- Commit da consolidação anterior: `42efc9a44cad121b34142499ceaa8cd8b7c31605`. Ainda falta confirmar o workflow dessa revisão e executar os testes de integração da página/Worker.


### Correção de compilação da gestão de bloqueios — 26/11/2026
- Windows Build #662 (commit `42efc9a`) falhou com um único erro bloqueante: `MainWindow.Requests.cs(12,49) CS0103: Path não existe no contexto atual`.
- Causa: o novo caminho do arquivo local de IPs bloqueados usava `Path`/ `Environment` sem importar `System.IO`; corrigido adicionando `using System.IO;` ao arquivo. Os demais itens no log eram avisos preexistentes/não bloqueantes.
- Commit da correção: pendente de envio e novo Windows Build.


### Associação de pedidos a cópias offline — 26/11/2026
- Ampliado o fluxo de pedidos: ao preparar uma música recebida, o Lyravoxen procura uma cópia offline válida na biblioteca pelo nome exato normalizado da música (campo do pedido comparado ao título/intérprete da faixa importada). Se encontrar arquivo local existente e dentro da validade, prepara essa cópia como próxima sem depender da internet; a reprodução só começa quando o operador aciona “Tocar próxima”.
- Se não houver correspondência exata válida, o fluxo original do YouTube permanece intacto. A prévia antiga de letra da próxima é recolhida ao preparar uma faixa offline.
- A preparação offline em memória ainda não é persistida para retomada após fechar o aplicativo e ainda não é exposta no JSON do controle remoto; a preparação e reprodução offline foram implementadas no fluxo da janela Pedidos do Windows. Esses pontos devem ser avaliados antes da entrega final.
- Build Windows #664 (commit `5bf96d2`) avançou com compilação e teste de linha do tempo concluídos com sucesso; etapa de instalador ainda estava em execução no momento do registro. O erro CS0103 de `Path` foi corrigido com `using System.IO;`.


### Teste automatizado do Worker integrado ao workflow — 26/11/2026
- O workflow Windows foi ampliado com um job Ubuntu independente que extrai o Worker do código C# e executa `tests/js/worker.test.mjs`, incluindo os cenários de captura do IP público, bloqueio, listagem e desbloqueio. O build Windows agora depende da aprovação desse teste, evitando gerar instalador quando o Worker não passar.
- A associação de pedidos a arquivos offline usa correspondência exata normalizada do nome informado no pedido; não faz aproximação por semelhança para evitar tocar a faixa errada.


### Testes da página remota adicionados — 26/11/2026
- O teste de interface mobile foi ajustado para respeitar os painéis recolhidos: verifica que pedidos e biblioteca iniciam fechados, expande cada painel antes de interagir e valida que ambos permanecem expansíveis.
- O workflow Ubuntu foi ampliado para instalar Playwright/Chromium e executar `phone.test.mjs`, além dos 37 testes do Worker. Assim a ordenação, os painéis recolhíveis e os fluxos da interface móvel são verificados automaticamente antes do build Windows.


### Correção detectada pelo teste da página móvel — 26/11/2026
- O teste Playwright identificou que a primeira reorganização da aba Ao Vivo havia deixado de fora o painel auxiliar oculto `pastepanel`, usado pelo botão “Criar letra”. A causa foi reconstruir a área apenas com os cartões visíveis e não preservar esse painel.
- Correção: reinserido o painel oculto original antes dos painéis recolhíveis, preservando seu HTML e identificadores. O teste móvel permanece habilitado para validar os fluxos completos, não apenas a ordem visual.


### Ajuste de sincronização do teste mobile — 26/11/2026
- Após preservar `pastepanel`, o teste Playwright passou pelos fluxos de pedido, biblioteca e controles (111 verificações OK), mas uma verificação inicial do status após digitar o PIN falhou por condição de corrida: o teste verificava o texto antes da resposta assíncrona de estado.
- Ajuste apenas no teste: aguardar explicitamente o status “Bíblia” antes da asserção. A falha não indicou regressão de funcionalidade da aplicação; o teste deve ser repetido para confirmar.


### Instruções de atualização do Worker público — 26/11/2026
- O arquivo `LEIA-ME-nuvem.txt` gerado pelo app agora inclui uma seção específica para instalações existentes: no Lyravoxen, gerar novo `worker.js`, substituir o código no Worker Cloudflare já criado e fazer Deploy, preservando o namespace KV `PEDIDOS` e o secret `ADMIN_TOKEN`. Essa atualização é necessária para que o bloqueio de IP público seja aplicado no serviço remoto sem apagar a fila já armazenada.
- Teste mobile corrigido para aguardar estado inicial após PIN; revisão Playwright em andamento para validar 112 verificações.


### Entrega consolidada para teste manual — 26/11/2026
- Branch final de trabalho: `redesign/brand-visual-layer`.
- Commit final: `50617adff3c2558dc8918e7e8a4800633ae1ed0a`.
- Workflow Windows #670 concluído com sucesso: teste do Worker Cloudflare aprovado (37 OK, 0 falhas); teste Playwright da página móvel aprovado (112 OK, 0 falhas); build/publish Windows concluído; teste de linha do tempo concluído; ZIP portátil e instalador Windows x64 gerados e publicados como artefatos do GitHub Actions.
- Artefatos run ID `45834348800`: `Lyravoxen-Windows-x64` (id `15124971540`), `Lyravoxen-Installer-Windows-x64` (id `15124856317`), `Lyravoxen-Windows-x64-portable` (id `15124392164`). Validade indicada pelo GitHub: até 24/02/2027.
- Funcionalidades implementadas nesta rodada: reordenação da página remota, pedidos e biblioteca recolhíveis; manter painel de colar letra; remover janela/alerta de confirmação e recolher prévia após sucesso; Esc fecha/limpa projeção própria e envia fechamento ao Holyrics sem encerrar o app ou a reprodução; avanço automático após áudio local; bloqueio local e Cloudflare por IP com identificação por nome; cópias offline autorizadas MP3/WAV com validade 5/7/14/30 dias, remoção automática do arquivo vencido mantendo metadados/letra, e associação por correspondência exata normalizada do nome do pedido à cópia offline válida.
- A atualização do Worker Cloudflare está documentada no `LEIA-ME-nuvem.txt` gerado pelo app: instalações existentes precisam substituir o código do Worker existente e fazer Deploy, preservando KV `PEDIDOS` e secret `ADMIN_TOKEN`, para que o bloqueio público por IP funcione. Sem esse passo, o bloqueio local continua funcionando, mas o Worker antigo não conhece a lista de IPs bloqueados.
- Associação offline: o arquivo precisa ser importado legalmente na biblioteca e seu nome/título deve corresponder exatamente ao nome da música informado no pedido após normalização; se não houver correspondência, mantém-se o fluxo original do YouTube. A cópia offline preparada como próxima é reproduzida ao acionar “Tocar próxima”; a preparação offline ainda não é persistida ao reiniciar o aplicativo nem aparece como próxima no controle remoto.
- Limitações e pendências para a etapa seguinte: validar manualmente o comportamento em Windows real, inclusive teclado Esc, Holyrics, áudio local/avanço automático, pedido local e pela internet, bloqueio após atualizar o Worker, importação/expiração e correspondência offline; avaliar persistência e exibição remota da próxima faixa offline. O reconhecimento bíblico por microfone permanece excluído, conforme decisão do usuário.
- Nenhum teste físico/manual foi realizado nesta sessão; os testes automatizados/build estão aprovados. Próxima etapa: usuário baixa o instalador ou pacote portátil e executa a rodada manual, devolvendo resultados/prints para correções.


### Proteção do navegador interno — 26/11/2026
- Solicitação do usuário: reforçar a proteção do navegador da aba Próxima música, em especial contra anúncios e rastreadores, mantendo WebView2, abas e navegação internas, sem Brave instalado, extensões externas ou custos.
- Implementação enviada à branch isolada `redesign/brand-visual-layer`. Commit da correção de compilação e versão validada: `880f267e062d6baf8039dce5ff47956444bab39d`.
- Novo serviço `src/Lyravoxen.Desktop/Services/BrowserAdBlocker.cs`: filtragem de solicitações por domínio com regras internas e lista pública unificada StevenBlack Hosts; lista baixada/cacheada em `%LOCALAPPDATA%/Lyravoxen/browser-hosts.txt`; preferências e exceções por site em `browser-protection.json`. A atualização da lista ocorre em segundo plano, sem bloquear a abertura inicial do navegador.
- Todas as instâncias WebView2 da aba Próxima música recebem o filtro antes da navegação, inclusive solicitações de documentos/iframes e workers através de `CoreWebView2WebResourceRequestSourceKinds.All`. A interface recebeu controles para ativar/desativar a proteção e permitir o domínio da aba atual.
- Proteção intencionalmente preserva domínios essenciais do YouTube e seus CDNs de vídeo. Não há promessa de bloquear anúncios servidos pelo próprio domínio do YouTube ou anúncios inseridos no fluxo de vídeo; o filtro é por domínio e não substitui o Brave Shields nem listas cosméticas completas.
- Primeiro ciclo de CI identificou ausência de `using System.IO;` no novo serviço; corrigido antes da validação final.
- Workflow Windows #45836887297 (run ID `45836887297`) concluído com sucesso no commit `880f267e062d6baf8039dce5ff47956444bab39d`: testes do Worker (37 OK, 0 falhas), Playwright mobile (112 OK, 0 falhas), compilação/publicação Windows, teste de linha do tempo, ZIP portátil e instalador x64 gerados.
- Artefatos desse workflow: `Lyravoxen-Windows-x64` (id `15126766492`), `Lyravoxen-Windows-x64-portable` (id `15126741379`), `Lyravoxen-Installer-Windows-x64` (id `15125498118`); expiração indicada pelo GitHub: 24/02/2027.
- Limitação de validação: os testes automatizados existentes não simulam a navegação real do WebView2 nem medem bloqueio de anúncios no YouTube. Ainda não houve teste manual de reprodução, pesquisa, letras e consumo de memória/processador no PC do usuário. Portanto, a compilação e os testes de regressão do app passaram, mas a eficácia real contra anúncios deve ser validada no uso.
- Próxima ação: disponibilizar o instalador/ZIP para teste manual; verificar YouTube (pesquisa, reprodução, pre-roll/mid-roll), Google, Cifra Club, Letras.mus.br, exceção por domínio, alternância da proteção, múltiplas abas e estabilidade/performance. Corrigir qualquer quebra antes de considerar o bloqueador plenamente validado.

### Destaque do botão Limpar telão no controle remoto — 26/11/2026
- Usuário esclareceu que o botão vermelho é o botão de limpar/encerrar a projeção na aba Ao vivo do controle remoto pelo celular, não na janela principal do Windows.
- No HTML servido por `Services/RemoteControlServer.cs`, o botão “🧹 Limpar telão” já chamava a ação existente `clearlive`, distinta de “⏹ Encerrar” (que encerra a música).
- Ajuste visual aplicado: adicionada a classe CSS `warn` ao botão “Limpar telão”, usando o estilo vermelho já existente. A ação, confirmação e retorno “Voltar ao automático” foram preservados.
- Commit: `fa87d1738121a42cb52d63077265e7aeebbaa30c`.
- Próxima ação: confirmar Windows Build após o push. Não iniciar testes funcionais até o layout ser aprovado; na conferência visual do celular, verificar que “Limpar telão” está vermelho e “Encerrar” continua sendo uma ação separada.

### Build após o destaque vermelho do botão Limpar telão — 26/11/2026
- Commit de código: `fa87d1738121a42cb52d63077265e7aeebbaa30c`; atualização de documentação: `4c9adcc27b5aa33783a975697154f7e8a6f5fc6b`.
- Windows Build #685 / run ID `45850726104` concluído com sucesso na branch `redesign/brand-visual-layer`, commit final `4c9adcc27b5aa33783a975697154f7e8a6f5fc6b`.
- Job `cloud-worker-tests`: 37 testes do Worker aprovados e teste Playwright do controle remoto aprovado (112 verificações, 0 falhas).
- Job `build`: geração do ícone e splash compacta, restore, compilação Windows, teste da linha do tempo, pacote ZIP portátil e instalador Inno Setup concluídos com sucesso.
- Artefatos (válidos até 24/02/2027):
  - `Lyravoxen-Windows-x64` — ID `15133402352`, SHA-256 `cd05ecc28fdb51d4e34f5a68c413c72fd9e2a69b0775169e1c17e30b7fdd427f`.
  - `Lyravoxen-Windows-x64-portable` — ID `15133357515`, SHA-256 `e4dc53b2c3b131ae8de9eae9529111d0724077b441666b257710f2df1adc1eb4`.
  - `Lyravoxen-Installer-Windows-x64` — ID `15132703638`, SHA-256 `9075545c5e49747900f57ae7ac1940c032197560e2969ca83dbfc39934b3d71c`.
- O ajuste do botão vermelho está incluído no build aprovado. A branch permanece separada; não houve merge para `main`.
- Importante: os testes Playwright validam a interface remota em navegador automatizado, não o aspecto real em todos os celulares. Nenhum teste manual do aplicativo Windows, do WebView2/YouTube, de áudio/Holyrics ou de dispositivos físicos foi realizado nesta rodada.
- Próxima etapa: o usuário deve baixar o instalador ou o ZIP portátil e conferir o layout final, especialmente o botão vermelho “Limpar telão” na aba Ao vivo do celular. Só após aprovação visual começar a rodada manual funcional.

### Conferência inicial do usuário — anúncios e início dos testes manuais — 26/11/2026
- Usuário enviou duas capturas do navegador interno na aba “Próxima música”.
- Evidência observada: anúncio patrocinado na lateral do YouTube e anúncio em vídeo (Amazon Prime) antes da música. A proteção de anúncios continua parcial; não considerar aprovada nem afirmar bloqueio completo.
- Orientação dada: testar YouTube com internet ligada; observar se anúncios aparecem antes, durante ou na lateral e verificar reprodução/áudio.
- Teste de áudio offline: em “Cadastrar Letras” > guia interna “Biblioteca”, criar ou selecionar uma pasta, clicar “Importar áudio MP3/WAV para uso offline”, escolher arquivo próprio/autorizado, definir validade (5, 7, 14 ou 30 dias), importar. Selecionar a faixa na lista “Músicas da coleção” para iniciar reprodução.
- Em seguida desligar a internet e selecionar novamente a faixa na Biblioteca. A reprodução deve continuar normalmente. Seleção de faixa offline chama PlayLocalAudio diretamente.
- Não é necessário preparar a próxima música para esse teste inicial. O teste de reprodução offline pela Biblioteca é distinto do teste de “Preparar próxima”.
- Estado dos testes manuais: ainda não informado pelo usuário; aguardando resultado de YouTube online e áudio local sem internet.

### Resultado do usuário — teste de reprodução e limpeza automática — 26/11/2026
- Usuário enviou captura da aba “Ao vivo” com vídeo do YouTube “Imagine - Cassiane” em 4:51/4:52, indicando reprodução online quase até o fim.
- Resultado relatado: ao terminar o hino, a letra não sai sozinha; usuário precisa clicar em “Sair da projeção”. Registrar como falha confirmada da limpeza automática no uso real, apesar de EndPlayback já chamar PublishProjectionOnly("") e fechar apresentação rápida do Holyrics no código. Investigar origem do evento de término do WebView2/YouTube e se a limpeza precisa fechar/limpar explicitamente a projeção ativa. Não alterar sem análise e teste.
- Captura também mostra status: “Falha ao enviar a letra ao Holyrics: No quick presentation available”. Registrar como problema separado de integração Holyrics; não confundir com o defeito de limpeza automática.
- Botão vermelho do controle remoto: usuário confirmou “ficou vermelho ok” — ajuste visual aprovado.
- O usuário pediu um checklist contínuo por etapa, com estados feito/correto/pendente.
- Estado manual:
  - Reprodução YouTube online: reprodução observada até 4:51/4:52; considerar parcialmente verificada, falta confirmar som e fluxo completo.
  - Limpeza automática da letra ao terminar: FALHOU no teste real.
  - Botão vermelho Limpar telão no controle: visualmente aprovado pelo usuário.
  - Áudio offline MP3/WAV: ainda não testado; usuário pediu tutorial.
- Tutorial fornecido: na aba Ao vivo, selecionar guia interna “Biblioteca”; criar/selecionar pasta; “Importar áudio MP3/WAV para uso offline”; escolher arquivo de áudio próprio/autorizado; definir 5/7/14/30 dias; aguardar importação; clicar na faixa da lista para tocar; desligar internet e selecionar novamente a faixa para validar offline. Não usar “Preparar próxima” para este teste.
- Próxima ação técnica: investigar correção de limpeza automática ao término e integração Holyrics “No quick presentation available”. Próxima ação do usuário: executar o tutorial de importação/reprodução offline e reportar resultado; manter checklist atualizado.

### Nova proposta do usuário — biblioteca automática de segurança offline — 26/11/2026
**Solicitação/ideia do usuário (ainda em fase de definição; não implementada nem aprovada como especificação final):**
- Manter a importação manual de áudio MP3/WAV externo. Ela atende casos em que alguém entrega um arquivo que não está no YouTube.
- Adicionar uma biblioteca/cache automático: quando uma música for reproduzida, o Lyravoxen deve tentar guardar uma cópia de áudio local, junto com os dados/letra associados, com prazo de validade configurável.
- Se a conexão cair, permitir reproduzir a cópia local sem internet.
- Para pedidos, quando chegar uma música com um arquivo de áudio disponível/anexado, baixar/copiar automaticamente para a área local de segurança, sem exigir ação manual, com prazo de retenção configurável.
- Ao vencer o prazo, excluir automaticamente o arquivo de áudio, preservando o registro/dados do pedido e da música conforme definido.
- Objetivo: redundância para cultos/eventos sem internet; evitar que uma música solicitada fique indisponível por queda de conexão.
**Distinção importante:** importação manual existente ≠ cache automático de músicas reproduzidas ≠ download automático de anexos de áudio dos pedidos.
**Auditoria preliminar do código atual na branch `redesign/brand-visual-layer`:**
- A importação manual MP3/WAV existe em `MainWindow.xaml.cs`, dentro da Biblioteca; copia o arquivo para `%LOCALAPPDATA%\\Lyravoxen\\Media`, registra validade e cria LibraryVideo offline.
- Selecionar uma faixa offline na Biblioteca chama `PlayLocalAudio(video)`.
- Pedidos atuais são essencialmente texto com links/metadados. `CloudRequestClient.FetchAsync` busca `id`, `text` e `ip`; `CloudTickAsync` importa texto usando `SongRequestStore.MessagesToJson` e `Parse`. O formulário web atual aceita nome/link e envia texto JSON. Não foi encontrado fluxo atual de upload/armazenamento binário de anexos de áudio nos pedidos.
- Música do YouTube é atualmente reproduzida dentro do WebView2; o código examinado não demonstra mecanismo de obtenção/armazenamento de arquivo de áudio do stream. Portanto, cache automático de qualquer música do YouTube não pode ser prometido sem investigar fontes, permissões, direitos e viabilidade técnica. Uma alternativa segura é cache de arquivos fornecidos pelo usuário/solicitante ou URLs diretas de áudio autorizadas.
- Não confundir cache de reprodução com download irrestrito de áudio do YouTube.
**Pontos a definir antes de implementar:**
1. O que exatamente significa “música reproduzida”: qualquer vídeo iniciado, ou só faixas reconhecidas/cadastradas?
2. Para pedidos, o solicitante poderá anexar MP3/WAV no formulário/link? Como tratar pedidos recebidos pelo WhatsApp/Cloudflare, que hoje só carregam texto/link?
3. Prazo de retenção: uma configuração global (ex.: 1, 3, 7, 15, 30 dias) ou prazos separados para cache de reprodução e anexos de pedidos?
4. Limite de armazenamento e limpeza automática quando o disco ficar cheio.
5. Comportamento quando há cópia local válida: oferecer “Reproduzir offline” ou preferir automaticamente a cópia local quando a internet falhar?
**Estado:** ideia compreendida e registrada como proposta pendente. Nenhum código foi alterado para essa funcionalidade. Preservar a importação manual existente.

### Confirmação de escopo — biblioteca existente e cache automático de segurança — 26/11/2026
- Usuário confirmou que já existe uma Biblioteca no Lyravoxen; não criar outra biblioteca/aba separada.
- Requisito de UI: novos controles e configurações devem usar botões minimalistas/compactos, ocupar pouco espaço e não reduzir nem redimensionar a área/tela de reprodução da música. Preservar o player na dimensão atual observada na captura do usuário.
- Importação manual MP3/WAV permanece como recurso para áudio externo enviado por alguém, inclusive música que não esteja no YouTube.
- Nova função desejada: cache automático de segurança integrado à Biblioteca atual. Quando o operador inserir/receber um link de música nos Pedidos, verificar se já há cópia local válida. Se não houver, tentar obter automaticamente uma cópia de áudio, desde que a fonte disponibilize download autorizado. Registrar validade configurável; permitir reprodução offline; expirar e remover o arquivo automaticamente sem apagar metadados/letra.
- Objetivo principal: se a internet cair durante evento/culto, o operador já terá um áudio local pronto para tocar.
- Para pedidos, a solicitação do usuário é que a verificação/download aconteça automaticamente ao chegar o pedido, e não apenas quando clicar “Preparar como próxima”.
- Auditoria de 26/11: formulário de pedido atual recebe nome e link; CloudRequestClient/CloudWorker armazenam texto e IP, não mídia binária. SongRequest não possui campo de caminho local/arquivo anexado. Necessário desenhar upload de áudio para pedidos e armazenamento local com validação.
- Limite técnico/legal: link do YouTube não garante acesso a arquivo de áudio baixável. Não implementar extração/baixar áudio do YouTube sem autorização/permissão da fonte. A automação poderá baixar anexos MP3/WAV ou links diretos de áudio que autorizem download; para links YouTube, verificar existência de cópia local e indicar que é necessário fornecer um arquivo autorizado se não houver cópia.
- Implementação ainda não iniciada nesta etapa; não declarar recurso funcional até haver código, testes e build. Próximo desenho técnico: cache/serviço único dentro da Biblioteca atual; ingestão automática de arquivos dos pedidos; deduplicação por hash; validade e limpeza automática; UI compacta; player e layout principal inalterados.

### Ajuste de navegação e rolagem — confirmação do usuário — 26/11/2026
- Usuário reforçou preferência global: não quer rolagem vertical para encontrar botões/opções dentro das telas do aplicativo. Exemplos explícitos: “Cadastrar Letras” e “Próxima música”, onde hoje é necessário rolar para alcançar outros comandos.
- Diretriz de UI: reorganizar com botões minimalistas/compactos, grupos de comandos, melhor distribuição horizontal e hierarquia visual, preservando a área/tamanho do player. Não simplesmente reduzir fonte a ponto de prejudicar leitura.
- Nova decisão aprovada: pode ser criada uma nova aba principal dedicada à Biblioteca Offline, posicionada imediatamente depois da aba Bíblia e antes da aba Ajuda, caso a complexidade da gestão offline exija espaço próprio.
- A nova aba não substitui a Biblioteca existente nem a importação manual. Deve integrar-se à biblioteca/dados atuais e servir à segurança offline: fila de downloads autorizados, arquivos recebidos nos pedidos, status, validade, espaço usado, remoção automática e ações essenciais.
- Requisitos mantidos: botões minimalistas; não reduzir/redimensionar o player; preservar o layout principal de reprodução; não introduzir rolagem vertical para acessar comandos. Para grandes listas, preferir busca, filtros, paginação ou painéis de altura fixa sem barra de rolagem visível, mantendo acesso aos itens.
- Auditoria XAML preliminar: foram encontrados ScrollViewer explícitos na área de sincronização (linha ~234), comandos da letra (~273), ferramentas de cadastro (~408) e template de ComboBox (scroll interno da lista suspensa). Há ainda conteúdo de listas que naturalmente rola. Rever cuidadosamente cada tela e distinguir rolagem de comandos de navegação de conteúdo textual/listas. O usuário pede que nada exija rolagem para encontrar botões; solução deve preservar acessibilidade/legibilidade.
- Estado: decisão registrada, nenhuma alteração visual/código aplicada nesta etapa. Próxima etapa de implementação: plano de compactação por aba, criar aba Biblioteca Offline entre Bíblia e Ajuda se necessária, depois implementar o fluxo automático de arquivos de pedidos e cache offline permitido, sem alterar o tamanho do player.

### Decisão de arquitetura da Biblioteca Offline — mover Histórico e Biblioteca para fora de Ao Vivo — 26/11/2026
- Solicitação explícita do usuário: se houver uma área própria de Biblioteca Offline, não faz sentido manter as guias “Histórico de vídeos” e “Biblioteca” dentro da aba “Ao vivo”. Remover essas duas guias da área Ao vivo e transferi-las para a nova área dedicada.
- Nova aba principal “Biblioteca Offline” ficará depois de “Bíblia” e antes de “Ajuda”.
- A nova aba deve reunir:
  - Histórico de vídeos (preservar busca, lista, detalhes e ações existentes).
  - Biblioteca atual (preservar pastas, músicas, importação manual MP3/WAV e reprodução local).
  - Novas ferramentas compactas de segurança offline (arquivos de pedidos, cache autorizado, prazo de retenção, status e limpeza).
- Não duplicar dados nem criar uma segunda biblioteca física. Mover a interface e integrar recursos à mesma estrutura de biblioteca já existente.
- Na aba Ao Vivo, manter foco exclusivo em reprodução, sincronização, projeção e controles necessários ao evento. O player mantém a dimensão atual.
- Diretriz geral permanece: botões minimalistas/compactos; eliminar rolagem vertical usada apenas para localizar botões/opções. Conteúdo longo (letras/listas) pode exigir navegação própria; evitar esconder dados ou prejudicar acessibilidade.
- Não iniciar testes funcionais antes de concluir a reorganização visual e o fluxo da Biblioteca Offline. Ao final da implementação, pausar para o usuário testar e fornecer checklist de verificação.
- Estado desta decisão: aprovada pelo usuário. Código ainda não alterado nesta etapa; próxima etapa de implementação na branch `redesign/brand-visual-layer`.

### Opção de download antecipado por pedido — computador e controle remoto — 26/11/2026
- Solicitação explícita do usuário: em cada item da aba Pedidos, disponibilizar ação para baixar/preparar previamente a música para uso offline, acionável tanto no aplicativo Windows quanto pelo controle remoto no celular.
- Objetivo: antes do evento, com internet disponível, baixar o arquivo de áudio para que uma falha de conexão durante o evento não impeça a reprodução.
- A ação deve verificar primeiro se já existe cópia local válida; se existir, não duplicar. Se houver arquivo/anexo MP3/WAV ou URL direta de áudio com download autorizado, baixar para a Biblioteca Offline, respeitar validade configurada e mostrar estado (baixando, disponível offline, falha/indisponível).
- A ação no celular deve chamar o mesmo fluxo seguro do computador, não implementar download independente no telefone. O download é executado pelo PC anfitrião, onde ficará o arquivo local que será reproduzido.
- Para pedidos com apenas URL do YouTube, não prometer download/extração de áudio; informar que falta arquivo ou fonte com download permitido. O link do YouTube permanece associado ao pedido.
- Integrar com a nova aba Biblioteca Offline e com a biblioteca física já existente; sem duplicar conteúdo.
- UI: botão/ícone compacto no cartão do pedido, tanto na janela Pedidos do PC quanto na aba Pedidos do controle remoto. Não aumentar a área do player.
- Estado: requisito aprovado para implementação; ainda não implementado nem testado. Incluir no checklist da rodada de desenvolvimento.


### Decisão aprovada — automação máxima da Biblioteca Offline e preservação de versões exatas — 26/11/2026
- Usuário escolheu explicitamente a Opção 3: automação máxima dentro das fontes permitidas.
- Requisito adicional explícito: preservar as versões exatas dos hinos, inclusive gravações em tons abaixo (ex.: 1 tom, 1,5 tom, 2 tons ou 3 tons abaixo), playbacks e outras variantes.
- Identidade de uma faixa offline não poderá ser determinada apenas pelo título/nome do hino. A associação deve priorizar o identificador exato do vídeo (por exemplo, ID do YouTube) e/ou identificador/hash do arquivo de áudio, mantendo metadados da versão/tonalidade quando disponíveis.
- Não substituir automaticamente uma gravação por outra de mesmo título, nem considerar que versões em tonalidades diferentes são equivalentes.
- Para links do YouTube, preservar o link e o ID exatos para reprodução online. Não extrair nem contornar restrições de download da plataforma.
- Para contingência offline, automatizar a obtenção/ingestão somente de arquivos anexados, arquivos locais fornecidos pelo usuário/solicitante ou URLs diretas de áudio cujo download seja autorizado. Se o pedido tiver apenas link do YouTube e não houver arquivo correspondente autorizado, mostrar claramente “Offline não preparado/arquivo necessário”; manter a referência exata do vídeo.
- A verificação automática dos pedidos deve procurar correspondência exata da versão, evitar duplicatas e registrar estado de preparação, validade e disponibilidade offline. A preparação poderá ser acionada pelo Windows ou controle remoto, sendo o PC anfitrião responsável pelo armazenamento.
- A importação manual MP3/WAV continua disponível como alternativa complementar, sem ser removida.
- Esta decisão aprova a direção de arquitetura e os requisitos, não significa que o recurso já esteja implementado, compilado ou testado.
- Próxima etapa técnica: desenhar e implementar o modelo de identidade/versionamento de faixas e o fluxo de ingestão autorizada, integrados à Biblioteca existente e à nova área “Biblioteca Offline”, sem reduzir o player e sem reintroduzir rolagem para localizar comandos.


### Checklist consolidado para rodada de ajustes de hoje — 26/11/2026
**Pedido do usuário:** preparar compilação para testes e recuperar também problemas antigos que ficaram pendentes. Este checklist distingue estado conhecido de itens que precisam ser revalidados. A compilação atual não deve ser apresentada como implementação da Biblioteca Offline, pois essa funcionalidade ainda não foi codificada.

#### A. Falhas confirmadas em teste real / prioridade alta
- [ ] **Limpeza automática da projeção ao fim do hino:** teste real em 26/11 mostrou que a letra permanece no telão após o vídeo terminar; usuário precisa clicar “Sair da projeção”. Corrigir o evento de término do WebView2/YouTube e validar também a limpeza no destino de projeção ativo.
- [ ] **Integração Holyrics — No quick presentation available:** erro apareceu na captura real. Investigar separadamente da limpeza automática; verificar criação/seleção de apresentação rápida e envio da letra.
- [ ] **Anúncios no YouTube:** capturas mostraram anúncio lateral patrocinado e anúncio em vídeo antes da música. Bloqueio atual é parcial. Melhorar somente dentro das possibilidades técnicas permitidas, sem prometer remoção de anúncios servidos pelo próprio YouTube.
- [ ] **Áudio offline MP3/WAV:** usuário ainda não informou o resultado do teste orientado (importar, tocar e repetir sem internet). Executar/registrar esse teste.

#### B. Problemas e solicitações antigas a revalidar
- [ ] **Projeção própria / saída:** usuário relatou que a projeção própria entrava sem atalho de saída e podia deixar o operador preso. Conferir estado atual e garantir uma saída claramente acessível, ou confirmar remoção dessa opção caso a decisão de escopo mais recente tenha prevalecido.
- [ ] **Sincronização manual:** foi criada uma área/aba própria, mas o usuário relatou anteriormente que não conseguiu utilizá-la. Testar marcação/avanço manual durante a música e captura de letra não encontrada; não considerar concluída sem teste real.
- [ ] **Sincronização e blocos de letra:** conferir quantidade de linhas configurada, substituição do bloco anterior pelo próximo e comportamento nos intervalos instrumentais (tela vazia quando não há letra).
- [ ] **Próxima música:** revisar busca, preparação, confirmação da letra e botões anterior/próximo/projetar; manter comandos visíveis sem rolagem para encontrá-los.
- [ ] **Copiar link atual:** houve pedidos antigos para restaurar o atalho na Próxima música e, depois, o usuário considerou desnecessário o botão geral Copiar link atual por já existir cópia na aba de música. Conferir a decisão mais recente: manter cópia contextual onde útil e não duplicar botão redundante.
- [ ] **Metadados da música:** conferir ordem e compactação dos campos Autor, Composição, Intérprete e Título; pedido mais recente foi reduzir a altura/ocupação desses campos.
- [ ] **Busca e captura de letras/hinos:** validar busca por título e cantor, identificação a partir do link e cadastro manual quando a letra não for encontrada. Fontes de referência anteriormente solicitadas incluem Cifra Club, Letras.mus.br e Harpa Cristã; não reintroduzir atalhos que o usuário pediu para retirar da aba Próxima música sem necessidade.
- [ ] **Confirmação de letra:** revalidar o fluxo Sim/Não/Criar e associação da letra criada à gravação correta para reutilização.
- [ ] **Projeção e atalhos Holyrics:** confirmar saída/limpeza sem depender de ESC ou de janela órfã; Ctrl+F9 foi relatado anteriormente como não funcional em um teste antigo, então revalidar somente se o atalho continuar no escopo.
- [ ] **Prévia e legibilidade:** conferir tamanho/contraste da prévia, especialmente em modo claro, sem reduzir a área do player.
- [ ] **Navegação sem rolagem:** revisar Cadastrar Letras e Próxima Música; botões e opções operacionais devem estar visíveis sem rolar. Conteúdo textual/listas pode ter navegação própria.
- [ ] **Controle remoto no celular:** conferir pedidos, aba Ao vivo, botão vermelho Limpar telão (visual já aprovado), tema claro/escuro e comandos de preparação.
- [ ] **Compatibilidade/desempenho:** conferir funcionamento em Windows 10/11 e máquinas Intel/Xeon mais antigas, sem exigir AVX2.

#### C. Biblioteca Offline — aprovada, mas ainda não implementada
- [ ] Mover as guias existentes Histórico de vídeos e Biblioteca para a nova aba principal Biblioteca Offline (depois de Bíblia e antes de Ajuda), sem duplicar dados.
- [ ] Preservar importação manual MP3/WAV e reprodução local.
- [ ] Implementar identidade exata de gravação/versão (ID do vídeo e/ou hash do arquivo; metadados de tonalidade quando disponíveis), sem casar apenas por título.
- [ ] Preservar variantes exatas: original, tons abaixo, playbacks e outras gravações; nunca substituir silenciosamente uma versão por outra.
- [ ] Preparação automática dos pedidos por arquivo/anexo ou URL direta de áudio com download autorizado; ação no Windows e no controle remoto, com armazenamento no PC anfitrião.
- [ ] Para pedido contendo somente link do YouTube, preservar URL/ID exatos e informar que arquivo offline autorizado é necessário; não extrair nem contornar restrições.
- [ ] Deduplicação, estado de download, validade configurável, gestão de espaço e limpeza automática sem apagar metadados/letras.
- [ ] Não reduzir nem redimensionar o player.

#### D. Estado da compilação / limite desta rodada
- Último build funcional conhecido: Windows Build #685, run ID 45850726104, commit 4c9adcc27b5aa33783a975697154f7e8a6f5fc6b, concluído com sucesso em 26/11.
- Desde o build #685, os commits 64baa00, 10533a3 e fb943b3 atualizaram somente MEMORIA_CONTINUA.md; não implementaram as mudanças funcionais da Biblioteca Offline nem corrigiram os problemas acima.
- A branch redesign/brand-visual-layer tem workflow Windows configurado para executar em push. Uma nova compilação da HEAD atual valida que o código existente ainda compila, mas não transforma recursos documentados em funcionalidades.
- **Próxima ação:** iniciar uma compilação de conferência da HEAD atual para gerar pacote de testes do código existente; em paralelo, manter o checklist acima como lista de correções. Depois da compilação, fornecer ao usuário o link do workflow/artefatos e orientar a rodada de verificação sem declarar bugs corrigidos antecipadamente.


## 26/11/2026 — Biblioteca Offline aplicada sobre o redesign aprovado
- Correção de continuidade: a implementação anterior na branch baseada em `main` foi rejeitada pelo usuário porque restaurou layout antigo. PR #11 foi fechada sem merge.
- Branch de trabalho correta criada a partir de `redesign/brand-visual-layer`: `feature/offline-library-on-redesign`.
- A interface visual original do redesign foi mantida. As subabas XAML existentes Histórico de vídeos e Biblioteca foram movidas, sem reconstruir seus controles, para uma nova aba principal Biblioteca Offline inserida após a área Bíblia.
- A Ajuda contextual foi atualizada para apontar para Biblioteca Offline. O workflow Windows foi habilitado para a branch de trabalho.
- Nenhuma lógica de player, sincronização, controle remoto, Holyrics ou tema foi intencionalmente alterada nesta etapa.
- Estado: alterações enviadas; CI Windows e testes automatizados ainda precisam concluir. Não considerar a compilação aprovada até o workflow ficar verde.


## 26/11/2026 — Requisitos detalhados confirmados pelo usuário para a próxima rodada

### Solicitações do usuário (confirmadas nesta conversa)
1. **Retenção programável da Biblioteca Offline:** o operador escolhe por quantos dias cada arquivo offline ficará armazenado. Presets explicitamente citados: 5, 7, 14 e 30 dias (o usuário quer escolher/programar o prazo). Ao vencer, excluir somente a cópia local de mídia; preservar cadastro, link/ID exato, letra, tempos, metadados, pasta e histórico. O prazo deve ser visível e editável, com data de expiração e estado.
2. **Preparar offline diretamente nos Pedidos:** cada pedido recebido deve ter ação explícita “Baixar offline”/“Preparar offline”, para o operador preparar antes do evento. A ação deve usar o PC anfitrião como armazenamento e refletir estado/progresso. Evitar duplicatas e associar ao pedido a gravação/versão exata.
3. **Fonte do arquivo offline:** preservar o link/ID exato do YouTube como referência. Não implementar extração de áudio do YouTube nem contornar restrições da plataforma. Quando só houver URL de vídeo sem arquivo offline autorizado, a ação deve explicar que precisa de MP3/WAV fornecido/importado ou URL direta de áudio cujo download seja autorizado. Quando houver arquivo autorizado, copiar/baixar para o armazenamento local e associar ao pedido.
4. **Cadastrar letras — layout compacto:** reduzir botões, espaçamentos, altura e rótulos para estilo minimalista e reorganizar em grupos/grade, de modo que todos os comandos operacionais caibam sem rolagem no painel lateral. Não alterar a largura/proporção/área do editor “Letra completa” nem da prévia da projeção.
5. **Próxima música — layout compacto:** reorganizar comandos em grupos/grade com botões menores e minimalistas; todos os controles operacionais devem ficar visíveis sem rolagem no painel esquerdo. Não reduzir o navegador/WebView2 nem sua área de visualização. As abas internas do navegador podem continuar com navegação própria.
6. **Biblioteca — explicar e organizar ações:** manter “Adicionar vídeo atual a esta pasta”, “Importar áudio MP3/WAV para uso offline” e “Corrigir nomes de todas as músicas”, mas explicar na Ajuda o que cada uma faz, quando usar e o alcance. “Corrigir nomes de todas as músicas” é uma ação em lote que limpa/sugere separação de intérprete e título e remove sufixos como Playback/Legendado/tons abaixo; não executar automaticamente sem comando do usuário. Avaliar agrupamento/compactação visual dos botões na área da Biblioteca.
7. **Ajuda/tutorial por atualização:** toda funcionalidade ou alteração relevante entregue deve atualizar a Ajuda do aplicativo com instrução de uso e um pequeno registro “Novidades desta versão”, com o que mudou, onde encontrar e como testar. Incluir verificação automatizada de cobertura da Ajuda antes de cada compilação.
8. **Proteção no navegador da aba Próxima música:** usuário quer experiência semelhante a um navegador com proteção contra anúncios/rastreadores e controle para permitir exceções por site. O código atual usa bloqueio de domínios via WebView2 e lista hosts, mas exclui explicitamente domínios essenciais do YouTube (youtube.com, googlevideo.com, ytimg.com e APIs) para não quebrar o player. Isso não bloqueia anúncios servidos pelo próprio YouTube/mesma origem e não deve ser descrito como bloqueio total. Investigar e testar a proteção por tipo de recurso/domínio sem quebrar pesquisa, reprodução, login, abas e carregamento de letras; manter botão “Proteção” e “Permitir neste site”. Não prometer remoção de anúncios in-stream/first-party.
9. **Processo de continuidade:** em cada comando/etapa, apresentar checklist separado em “a fazer”, “feito” e “testar”, indicando exatamente em qual aba, janela e controle o usuário deve testar. Atualizar `MEMORIA_CONTINUA.md` cronologicamente a cada mudança significativa e não declarar testes reais do usuário como concluídos sem confirmação.

### Estado técnico observado na branch `feature/offline-library-on-redesign` (26/11/2026)
- `LibraryVideo` já contém `IsOffline`, `LocalPath`, `Format` e `OfflineExpiresAt`; o modelo ainda não basta para garantir política completa por faixa/pedido, origem autorizada, hash/ID exato, estado de download e erro/progresso.
- `SongRequest` contém Id, Group, Name, Url, ReceivedAt e SenderIp; ainda não possui estado offline, caminho local, validade ou associação ao ID/hash da gravação.
- A janela Pedidos já oferece “Preparar como próxima”, “Abrir”, “Copiar link” e “Descartar”; não há ação Baixar offline.
- `Cadastrar letras` ainda tem um `ScrollViewer` no painel lateral e botões verticais grandes. O editor `ManualLyricsBox` e a prévia ficam nas colunas centrais/direita; preservar suas proporções e área.
- `Próxima música` ainda tem `ScrollViewer` em todo o painel esquerdo; botões de preparar, confirmar, verificar, proteção, navegação, fontes e abas ocupam coluna de 220 px. O navegador está na coluna direita; preservar sua área.
- A Biblioteca ainda mostra três botões grandes no rodapé da lista. A ação “Corrigir nomes de todas as músicas” altera nomes em lote; precisa de descrição clara e confirmação/escopo visível.
- `BrowserAdBlocker` é filtragem por requisição/domínio com lista local + lista StevenBlack. Os domínios do YouTube estão em `ProtectedHosts` e são sempre permitidos para não interromper vídeo; por isso não remove publicidade nativa/in-stream do YouTube. “Permitir neste site” grava exceção por host.
- Ajuda contextual já existe, mas precisa incluir Biblioteca Offline e a ação Baixar offline, explicar ações da Biblioteca, proteção e novidades por versão. Cobertura automatizada existente: `tests/help_coverage.py`.

### Checklist de implementação desta etapa
**A fazer**
- [ ] Definir/implementar política de validade por faixa offline com presets 5/7/14/30 dias e prazo editável, com expiração automática e remoção somente do arquivo local.
- [ ] Implementar fluxo autorizado de ingestão offline, identidade exata da versão, deduplicação, progresso, estado e mensagens claras quando só houver link YouTube.
- [ ] Adicionar ação “Baixar offline” em cada pedido, no PC; verificar se a ação no controle remoto/celular é parte da etapa seguinte e manter PC como anfitrião do armazenamento.
- [ ] Compactar painel lateral de Cadastrar letras sem rolagem, preservando área do editor e prévia.
- [ ] Compactar painel esquerdo de Próxima música sem rolagem, preservando área do navegador.
- [ ] Reorganizar/explicar as três ações da Biblioteca e adicionar Ajuda para cada uma.
- [ ] Investigar proteção de navegador sem quebrar YouTube; documentar limites e testar exceções por site.
- [ ] Atualizar Ajuda e seção Novidades da versão para cada recurso entregue.
- [ ] Atualizar este arquivo com resultados/commits e executar testes de build, testes automatizados e cobertura da Ajuda.

**Feito nesta conversa**
- [x] Recuperadas as decisões anteriores de automação máxima, armazenamento no PC anfitrião, prazos configuráveis, botão de preparação nos Pedidos e preservação exata de versões/tonalidades.
- [x] Inspecionado o código atual da branch e identificadas lacunas entre requisitos e implementação.
- [x] Registrado o escopo detalhado e os limites técnicos da proteção do navegador e de fontes offline.

**Testar após implementação**
- [ ] Biblioteca Offline → selecionar uma faixa → definir 5, 7, 14 ou 30 dias → conferir data de expiração → simular vencimento em teste controlado → confirmar que somente o arquivo local é removido e letra/metadados permanecem.
- [ ] Pedidos → receber/importar um pedido → clicar “Baixar offline” com arquivo autorizado → verificar progresso, deduplicação, reprodução sem internet e prazo.
- [ ] Pedidos → clicar “Baixar offline” num pedido que só tenha link YouTube → confirmar mensagem clara de arquivo necessário e preservação do link/ID.
- [ ] Cadastrar letras → verificar todos os comandos visíveis sem rolagem; confirmar que editor e prévia mantêm tamanho/proporção.
- [ ] Próxima música → verificar todos os comandos visíveis sem rolagem; confirmar que navegador/WebView2 mantém área e que pesquisa/abas/reprodução continuam funcionando.
- [ ] Biblioteca → testar Adicionar vídeo atual, Importar MP3/WAV e Corrigir nomes em lote; confirmar que a Ajuda explica cada ação e que nenhuma ação em lote ocorre sem clique/confirmar.
- [ ] Próxima música → testar Proteção ligada/desligada e Permitir neste site; verificar que sites normais funcionam e registrar anúncios que permanecem no próprio YouTube.
- [ ] Ajuda → conferir Novidades da versão e instruções de cada função nova.

### Próxima ação técnica
Implementar em blocos pequenos na branch `feature/offline-library-on-redesign`, começando por arquitetura de mídia/validade e ação nos Pedidos, depois ajustes compactos de interface, proteção do navegador e Ajuda. Não reduzir player/editor/prévia/navegador. Gerar uma única compilação após concluir o bloco funcional e os testes automatizados, evitando builds intermediários.



## 26/11/2026 — Primeira implementação integrada para rodada única de testes

### Implementado nesta rodada (branch `feature/offline-library-on-redesign`)
- `SongRequest` agora registra caminho local, ID do vídeo, validade e formato da cópia offline, mantendo compatibilidade com pedidos antigos por propriedades opcionais/default.
- Cada cartão da janela Pedidos recebeu o botão **⬇ Baixar offline**. O fluxo permite escolher 5, 7, 14 ou 30 dias, selecionar MP3/WAV local autorizado, copiar o arquivo para `%LOCALAPPDATA%/Lyravoxen/Media`, associar ao pedido e ao ID exato do vídeo YouTube quando reconhecível, salvar em uma pasta “Pedidos offline” e exibir estado/validade no cartão.
- O preparo de próxima música não associa mais cópias por título apenas; para pedido offline, exige correspondência do caminho local associado e valida ID de vídeo quando disponível, evitando trocar gravações/tons diferentes silenciosamente.
- Painel de comandos de Cadastrar letras e Próxima música teve controles compactados e rolagem vertical desativada. A área de editor, prévia e navegador não teve suas definições de colunas alteradas intencionalmente; validar visualmente no build.
- Ajuda atualizada para explicar importação, prazos, expiração, ação Baixar offline, ações da Biblioteca e limites reais do bloqueador de anúncios.
- Corrigido atributo XAML duplicado detectado na revisão estática.

### Limites explícitos desta implementação
- **Não baixa/decodifica áudio diretamente do YouTube.** O botão nos Pedidos seleciona arquivo local MP3/WAV autorizado. O link e ID exatos ficam associados como referência. Isso não é download automático do vídeo nem reprodução offline extraída do YouTube.
- A validade automática existente é verificada na inicialização do app via `CleanupExpiredOfflineCopies`; não foi adicionado serviço em background enquanto o app permanece aberto.
- O botão Baixar offline atualmente exige seleção manual de arquivo no PC anfitrião. Ainda não há importação automática por URL de áudio remota, nem comando no celular para escolher arquivo do PC.
- A UI compactada teve rolagem desativada e botões reduzidos; ainda depende de inspeção real em resolução e escala do monitor para confirmar que todos os controles cabem sem clipping.
- O bloqueador continua filtrando domínios de terceiros; anúncios first-party/in-stream do YouTube não são garantidamente removidos.

### Checklist atualizado
**Feito**
- [x] Campos de estado offline em pedidos.
- [x] Botão Baixar offline na janela Pedidos (arquivo MP3/WAV autorizado).
- [x] Escolha de validade 5/7/14/30 dias no fluxo do pedido.
- [x] Associação do pedido com ID exato do vídeo, quando presente, e caminho do arquivo local.
- [x] Estado e data de validade exibidos no pedido.
- [x] Evitar correspondência por título isolado ao preparar cópia offline.
- [x] Compactação inicial dos comandos em Cadastrar letras e Próxima música.
- [x] Atualização inicial da Ajuda.
- [x] Revisão estática de atributos duplicados no XAML.

**Aguardando CI**
- [ ] Build Windows x64 e testes automáticos.
- [ ] Cobertura da Ajuda (`tests/help_coverage.py`).
- [ ] Verificar compilação XAML e eventos dos botões.

**Testar pelo usuário em uma única rodada depois do CI verde**
- [ ] Biblioteca Offline → importar MP3/WAV → escolher 5/7/14/30 dias → confirmar arquivo na pasta e data.
- [ ] Pedidos → pedido com link YouTube → Baixar offline → selecionar arquivo autorizado → conferir estado no cartão e pasta “Pedidos offline”.
- [ ] Pedidos → preparar como próxima → confirmar que reproduz o arquivo local associado, sem confundir versões de mesmo título.
- [ ] Fechar/reabrir o app após vencimento de teste → confirmar remoção apenas da mídia e preservação do registro/letra.
- [ ] Cadastrar letras → todos os comandos visíveis sem rolagem; editor e prévia com mesmo espaço visual.
- [ ] Próxima música → todos os comandos visíveis sem rolagem; navegador com área preservada.
- [ ] Ajuda → conferir instruções e novidades.
- [ ] Proteção → alternar ligada/desligada, permitir domínio e testar pesquisa/reprodução; registrar anúncios que persistem no próprio YouTube.

### Próxima ação
Aguardar o workflow Windows mais recente terminar. Corrigir falhas de compilação/testes antes de entregar. Não gerar outra compilação até consolidar correções e checks automatizados; não fazer merge em `main` sem aprovação.


## 26/11/2026 — Correções após falhas de CI e preparação antecipada de pedidos

### Diagnóstico confirmado nos logs
- Foram consultadas as execuções recentes, não apenas uma.
- Erros de XAML: atributo `MinHeight` duplicado na linha 350, em duas execuções iniciais. Corrigido no commit `157f8dc8283b84ab26e2939d80115fa635c026e5`.
- Erro de compilação nas tentativas seguintes: `LibraryFolder` e `LibraryVideo` não encontrados em `MainWindow.Requests.cs`; os modelos estão no namespace `Lyravoxen.Models`. Corrigido com `using Lyravoxen.Models;` no commit `bf9fac68555475fb87504696b2edab73dee151ae`.
- A execução de cobertura da Ajuda falhou numa versão intermediária; textos e comandos em falta foram adicionados, e a execução subsequente passou nessa etapa.
- Não foi feita instalação local do usuário para diagnosticar; os erros foram identificados pelos logs do Actions.

### Requisito confirmado pelo usuário: pré-baixar pedidos antes do evento
- O usuário quer que os pedidos recebidos pelo site cheguem à aba Pedidos e possam ser preparados/baixados antecipadamente, antes do início do evento, para evitar falta de música durante a programação.
- Implementação já iniciada: botão **⬇ Baixar offline** em cada pedido; permite escolher arquivo MP3/WAV local autorizado, escolher validade, guardar no PC anfitrião e associar ao pedido e ao ID do vídeo. O cartão mostra o estado da preparação.
- Fluxo desejado: pedido recebido pelo site → aparece na aba Pedidos → operador clica Baixar offline/preparar antecipadamente → arquivo fica guardado e identificado para aquela gravação → na hora do evento, preparar como próxima/tocar a cópia certa.
- Limite: a implementação atual seleciona arquivo local; não baixa áudio diretamente do YouTube. Não deve ser descrita como downloader automático do YouTube. A gravação exata deve ser preservada; não casar por título apenas.

### Próxima etapa
- Aguardar o CI do commit `bf9fac68555475fb87504696b2edab73dee151ae`.
- Se o build passar, conferir fluxo completo de pedidos com cópia offline, associação ao pedido e preparação como próxima, e gerar um único pacote de testes.
- Se falhar, ler os logs do run mais recente e corrigir antes de entregar.
- Não fazer merge na `main` sem aprovação.


## 26/11/2026 — URL oficial do formulário de pedidos esclarecida pelo usuário

- URL fornecida: https://lyravoxen-pedidos.ferkt319.workers.dev/
- O endereço é o Cloudflare Worker do formulário público; os participantes usam o caminho `/pedido`. O programa consulta o endpoint protegido `/pedidos` e importa mensagens contendo `nome` e `link`.
- A página/Worker guarda texto e links de músicas (incluindo URLs do YouTube); não recebe nem armazena o arquivo de áudio. O computador Lyravoxen recebe a URL e a mantém no objeto `SongRequest.Url`.
- Confirmação do usuário: a funcionalidade desejada é preparar antecipadamente, diretamente no cartão do pedido que chega à janela Pedidos, a mesma música/versão indicada no link, para tocar offline no evento. Não é para baixar o HTML do formulário nem apenas copiar o link.
- A implementação atual do botão **Baixar offline** ainda abre seletor local de MP3/WAV; ela não baixa o conteúdo apontado pelo link. O botão deve ser entendido como importação/associação de um arquivo local, não download remoto.
- Próximo trabalho: avaliar fluxo seguro para links diretos de arquivos de áudio autorizados (download para PC, validação de tipo/tamanho, timeout, hash, deduplicação, validade) e manter URLs YouTube como referência exata sem extração de áudio ou contorno de proteção. Para YouTube, não declarar offline até existir um arquivo local obtido por meio autorizado.
- O usuário reforçou que deseja fazer todos os testes de uma só vez, após correções e validação prévia; evitar entregar builds fragmentadas.


## 26/11/2026 — Evidências visuais e novo registro de retomada

### Solicitações/relato do usuário
- O usuário enviou duas capturas da aplicação em execução (1366×768).
- Na aba Próxima música, o painel lateral esquerdo continua com rolagem vertical e parte dos controles/fontes de pesquisa fica abaixo da área visível. Solicita botões minimalistas e comandos acessíveis sem rolar.
- Na janela Pedidos, a ação Baixar offline abre o seletor de arquivos MP3/WAV para localizar um arquivo existente. O usuário esperava download e salvamento automáticos da mídia de uma URL autorizada, pronta para reprodução posterior sem internet.

### Diagnóstico técnico confirmado pela inspeção do código
- MainWindow.xaml ainda contém um ScrollViewer no painel esquerdo da aba Próxima música; portanto a compactação anterior não resolveu o problema visual confirmado pela captura.
- MainWindow.Requests.cs implementa a ação atual como seleção/importação de arquivo local; não é download remoto. A interface e a Ajuda precisam distinguir Baixar de URL direta autorizada de Importar MP3/WAV do computador.
- SongRequestStore.cs contém caminho local, ID do vídeo, validade e formato, mas ainda não implementa estado completo de download, hash ou progresso.
- A reprodução offline vinculada ao pedido ainda precisa ser verificada de ponta a ponta, sem correspondência apenas por título.

### Limites preservados
- Para URLs diretas de arquivos de áudio autorizados, implementar download com validação de protocolo, MIME/extensão, tamanho, timeout, hash, deduplicação e associação à gravação exata.
- Para links que apontem somente ao YouTube, preservar URL/ID e informar claramente que não há arquivo offline disponível. Não extrair áudio nem contornar DRM/restrições da plataforma.
- Manter a importação local MP3/WAV como alternativa separada.
- Preservar a área do navegador/WebView2, pesquisa, abas e reprodução; não sacrificar o espaço do navegador para compactar os comandos.

### CI confirmado nesta data
- Workflow 45874275889, SHA 0f5b591aa417c408cc7ca55df43096ae26043130, terminou com success.
- Jobs cloud-worker-tests e build aprovados; cobertura da Ajuda, testes do Worker, página remota mobile, build .NET, cue timeline, ZIP portátil e instalador foram concluídos com sucesso.
- Artefatos gerados: Lyravoxen-Installer-Windows-x64 (ID 15144741646), Lyravoxen-Windows-x64-portable (ID 15144651792) e Lyravoxen-Windows-x64 (ID 15143443015).
- CI verde comprova compilação e os testes automatizados listados, não comprova download remoto de áudio, reprodução offline real ou inspeção visual em diferentes resoluções.

### Documentação de retomada
- Criado RETOMADA_ATUAL.md na branch feature/offline-library-on-redesign, commit f6171a01d247ec390de2672239000eac34bc44f0.
- Esse arquivo contém estado compacto, evidências das capturas, artefatos do CI, decisões preservadas, checklist e próxima ação.

### Próximas ações
1. Ajustar o painel esquerdo da aba Próxima música para que os comandos principais caibam sem rolagem, sem clipping e sem reduzir a área útil do navegador.
2. Separar as ações de download de URL direta autorizada e importação local MP3/WAV.
3. Implementar validações, progresso, hash/deduplicação e associação exata da cópia offline.
4. Garantir reprodução da cópia local vinculada ao pedido, inclusive após reiniciar o app e sem internet.
5. Completar expiração automática em tempo real, proteção do navegador e Ajuda.
6. Rodar testes e gerar uma única compilação consolidada para o usuário.
7. Não fazer merge na main sem aprovação explícita.


### Ajuste de interface após as capturas — 26/11/2026
- Commit 7580049ac07b6096e1b86fd602bb8716541df236: substituído o painel lateral rolável da aba Próxima música por uma grade compacta sem ScrollViewer; controles reorganizados com alturas/paddings menores e rótulos compactos.
- A coluna direita do navegador/WebView2 foi mantida como estava; a alteração não mexeu intencionalmente em sua largura ou estrutura.
- Workflow acionado pelo push: 45875746475, ainda em fila no último acompanhamento. A alteração ainda não está validada pelo build nem por inspeção visual real.
- Próxima ação: acompanhar CI; em caso de falha, ler logs e corrigir antes de pedir teste. Continuar implementação de download por URL direta autorizada separado da importação local.


## Atualização cronológica — 26/11/2026 (retomada da Biblioteca Offline)

> Esta atualização complementa e supersede, para a etapa atual, as anotações antigas que diziam para não testar ainda ou que o redesign era somente visual. O escopo atual aprovado pelo usuário inclui Pedidos, Biblioteca Offline e navegador da aba Próxima música.

### Solicitações do usuário
- Fazer “Baixar offline” funcionar de verdade em Pedidos.
- Na Biblioteca, permitir salvar, editar e baixar offline, mantendo MP3/WAV local como alternativa.
- Melhorar a navegação do YouTube e a proteção contra anúncios.
- Testar funcionalmente, não produzir builds vazios e não fazer merge sem autorização.

### Implementações feitas nesta retomada
- Novo projeto multiplataforma `src/Lyravoxen.Offline`.
- Serviço `OfflineMediaService`: download de URL direta autorizada, importação local MP3/WAV, validação de assinatura/MIME/extensão, limite 50 MB, timeout 45 s, DNS/IP público, bloqueio de SSRF/redirecionamentos privados, hash SHA-256 e deduplicação.
- Pedidos e Biblioteca usam o mesmo serviço. Link somente do YouTube não é tratado como download de áudio; o app explica a limitação e oferece arquivo local autorizado.
- Editor direto na Biblioteca para título, intérprete e letra.
- Expiração periódica e segura: remove os bytes apenas quando nenhuma referência ativa permanece; preserva registros, letra e metadados.
- Painel esquerdo da aba Próxima música compactado sem alterar intencionalmente a largura/estrutura do navegador.
- Política de requisições do navegador extraída e testada independentemente.

### Testes adicionados
- `tests/Lyravoxen.OfflineTests` cobre importação MP3/WAV, download direto, deduplicação, bloqueio de YouTube, DNS/redirecionamento privado, limite de tamanho, HTML disfarçado, política de anúncios e preservação de arquivos compartilhados durante expiração.
- Workflow executa testes offline, cobertura da Ajuda, análise estática C#, Worker e página remota mobile.
- Foi configurada uma agenda diária no YAML, mas o evento `schedule` do GitHub roda somente na branch padrão. Como `main` não foi alterada, a rotina diária ainda não está ativa para a branch de trabalho.

### Evidências e limites
- Run `45888141360`, SHA `1c888e98`: testes multiplataforma e C# static checks aprovados; build foi deliberadamente omitido na branch de trabalho enquanto a implementação ainda estava em validação.
- Run `45887612874`, SHA `5003f831`: build Windows e jobs automatizados concluídos com sucesso para a integração inicial do serviço; antecede o editor e a compactação final da aba Próxima música, portanto não é o build final.
- Run `45888415155`, SHA `2b9f8418`: no último registro, testes de mídia, cobertura da Ajuda, C# static checks e Worker aprovados; Playwright ainda estava em execução. Inclui a revisão mais recente dos testes de MP3 e expiração.
- Não há comprovação ainda de reprodução real de áudio offline no Windows, teste integrado com WebView2 real ou inspeção visual da nova disposição em várias resoluções.

### Estado do projeto após esta retomada
- Branch: `feature/offline-library-on-redesign`.
- `main`: não alterada.
- Build final: pendente até terminar a validação automatizada e executar a compilação Windows consolidada.
- Teste manual do usuário: ainda não solicitado; será uma única rodada após entrega consolidada.
- Próxima ação: concluir o run atual, executar build final Windows, corrigir falhas e registrar SHA, logs e artefatos reais em `RETOMADA_ATUAL.md`.


### Fechamento da compilação consolidada — 26/11/2026
- Workflow `45889091270`, SHA `76e1f018a9e84c65505de05064eb50534ae0cd86`, concluído com sucesso.
- `cloud-worker-tests`: sucesso — 21 testes do serviço offline/políticas, cobertura da Ajuda, C# static checks, Worker e Playwright/mobile.
- `build`: sucesso — `dotnet publish` Windows x64, timeline tests, ZIP portátil e instalador Inno Setup.
- Artefatos:
  - Windows x64: ID `15150797165`.
  - Portable ZIP: ID `15150578815`.
  - Instalador: ID `15150673234`.
- Correção de build: referência WPF `HorizontalAlignment.Right` qualificada como `System.Windows.HorizontalAlignment.Right` após erro CS0176 na primeira tentativa; compilação subsequente passou.
- A branch `main` foi consultada e permanece no SHA `2051135a6e4915c13dda5730fa0248ed7dbcfe2f`; não houve merge nem alteração nela.
- Estado: pacote Windows consolidado disponível para uma única rodada manual. Ainda dependem de teste no PC anfitrião: reprodução offline real, navegação/ads em WebView2 real e inspeção visual em diferentes resoluções.
- O agendamento diário foi escrito no YAML da branch, mas ainda não dispara até o workflow estar na branch padrão; manter `main` intacta sem autorização.


### 26/11/2026 — Revisão corretiva após feedback visual/funcional do usuário
- Usuário enviou duas capturas de tela e relatou: anúncios in-stream no começo dos vídeos na aba Próxima música; fluxo de baixar offline não funcionava com links do YouTube; Biblioteca difícil de salvar, organizar e renomear; pediu IP/MAC do remetente e bloqueio/desbloqueio no painel Pedidos.
- Esclarecido limite técnico: site não consegue obter MAC de dispositivo remoto; Cloudflare Worker consegue registrar IP público via `CF-Connecting-IP`. O IP é dado pessoal e o formulário foi atualizado para informar que IP e horário são registrados.
- Não foi implementada extração de áudio do YouTube nem tentativa de contornar anúncios/restrições. O player embutido pode continuar mostrando anúncios first-party. Offline continua com MP3/WAV local autorizado ou URL direta de arquivo de áudio.
- Worker atualizado em `CloudWorker.cs`: IP armazenado com pedido, API autenticada `/bloqueios`, bloqueio/desbloqueio, limite de 5 envios por IP/10 minutos e limite global de fila. Testes do Worker passaram.
- Atenção operacional: o código de Worker novo está no repositório e o app gera o arquivo atualizado, mas a URL pública `lyravoxen-pedidos.ferkt319.workers.dev` não foi implantada por esta sessão por ausência de conexão à conta Cloudflare. Atualização manual única do Worker é necessária; não recriar KV nem trocar ADMIN_TOKEN. Pedidos antigos podem não conter IP.
- UI Pedidos sincroniza bloqueios remotos ao abrir, mostra IP por pedido, oferece bloquear/desbloquear e atualizar lista remota; avisa quando o IP não chega porque o Worker pode estar desatualizado.
- Biblioteca agora oferece fallback direto para importação local quando a URL é YouTube ou falha como arquivo direto; pedido offline permite escolher pasta; ações de mover/remover música por pasta; criar pasta no seletor; busca por título/intérprete/compositor; validação de nomes de pasta duplicados; identidade exata para registros de mídia. Salvamento de biblioteca retorna sucesso/falha para não mostrar sucesso falso.
- Ajuda cobre novos botões e regras.
- Validação final workflow `45894030900`, SHA `4f39f92562b6eb470b16be1d3513e9d1ecbe8543`: 21 testes offline, 44 Worker, 25 formulários Chromium, 112 testes mobile Chromium, cobertura Ajuda e C# static checks; todos aprovados. Build Windows x64, timeline tests, ZIP portátil e instalador também aprovados.
- Artefatos: Windows x64 ID `15153435863`; portable ZIP ID `15153276186`; instalador ID `15153231348`. Workflow: https://github.com/jabesguitar/Lyravoxen/actions/runs/45894030900.
- `main` não alterada; SHA consultado `2051135a6e4915c13dda5730fa0248ed7dbcfe2f`. Branch feature atual recebeu commits de correção e documentação.
- Pendências reais: atualização manual única do Worker no Cloudflare; teste manual no PC anfitrião de áudio real, biblioteca, IP público e WebView2; anúncios do YouTube first-party não são removidos.


## 26/11/2026 — Consolidação da Biblioteca Offline e pacote Windows final (#807)

### Fonte e escopo deste registro
- Registro acrescentado a partir do histórico de execuções do GitHub Actions fornecido pelo usuário nesta conversa em 26/11/2026.
- Branch de trabalho: `feature/offline-library-on-redesign`.
- Este registro complementa o histórico anterior; não substitui nem apaga decisões ou checklists anteriores.
- Os resultados de Actions comprovam execução automatizada/compilação, não substituem validação manual do usuário no aplicativo.

### Marcos de compilação confirmados pelo histórico apresentado
- **Windows Build #804** — concluído com sucesso; commit `4f39f92562b6eb470b16be1d3513e9d1ecbe8543`; mensagem: “Run the final consolidated Windows package after all feature tests passed”; duração informada 3m25s.
- **Windows Build #807** — concluído com sucesso; commit `832ef7510f2c984a5a9ef76de7447c143d875f61`; mensagem: “Keep Windows packaging manual after consolidated release”; duração informada 1m23s.
- **Windows Build #780** — concluído com sucesso; commit `b2ae780b693c8856492b6521fdc9d797a40ddb48`; mensagem: “Record successful Windows validation and final artifact IDs”; duração informada 3m42s.
- **Windows Build #778** — concluído com sucesso; commit `76e1f01`; correção de referência de alinhamento WPF no editor da biblioteca.
- **Windows Build #775** — concluído com sucesso; commit `29e1994`; registro cronológico da implementação offline.
- **Windows Build #710** — concluído com sucesso; commit `5245e66`; registra a implementação da Biblioteca Offline sobre o layout aprovado.
- O usuário apresentou também a sequência de execuções #661 a #807. A lista inclui muitos runs cancelados, falhos e concluídos; não interpretar cancelamentos como falhas de código nem como validações bem-sucedidas. Os resultados individuais devem ser consultados no Actions quando for necessário investigar uma alteração específica.

### Alterações funcionais/estruturais registradas na sequência #661–#807
- Continuidade do redesign visual aprovado na branch `redesign/brand-visual-layer`, com a Biblioteca Offline desenvolvida sobre esse layout na branch `feature/offline-library-on-redesign`.
- Implementação e consolidação da área Biblioteca Offline, incluindo Histórico de vídeos e Biblioteca movidos para a nova área, mantendo a intenção de preservar a interface aprovada e os dados/fluxos existentes.
- Preparação offline de áudio MP3/WAV autorizado associada aos pedidos, com seleção do arquivo no PC anfitrião, identidade da gravação/vídeo, validade e estados; não significa extração de áudio do YouTube.
- Gestão de pastas: criar/selecionar destino, mover e remover itens, ações de organização e confirmação de persistência.
- Proteção do navegador/WebView2 e testes/políticas de requisições, incluindo validação de endereços públicos, bloqueio de destinos privados/redirecionamentos perigosos e exceções por site. Não afirmar bloqueio completo de anúncios first-party/in-stream do YouTube.
- Formulário público de pedidos: testes com e sem JavaScript, aviso de privacidade, divulgação de coleta de IP e limitação de solicitações por IP; sincronização da lista de bloqueio entre o aplicativo e o Worker.
- Melhorias e testes da fila, biblioteca, importação MP3/WAV, deduplicação, expiração de arquivos compartilhados, preservação de referências e recursos de ajuda.
- Ajustes compactos para a aba Próxima música, com intenção de manter controles acessíveis sem rolagem e preservar a área do navegador/player.

### Decisão operacional registrada no commit #807
- Após a compilação consolidada #804, o workflow foi alterado/documentado para manter o empacotamento Windows manual depois da release consolidada.
- Não disparar novos pacotes Windows automaticamente para cada commit de documentação ou ajuste subsequente, salvo decisão posterior do usuário ou necessidade explicitamente aprovada.
- Runs #805 e #806 foram cancelados conforme o histórico fornecido; não são o pacote final. #807 concluiu com sucesso.

### Estado atual conhecido em 26/11/2026
- Branch de referência desta etapa: `feature/offline-library-on-redesign`.
- Última compilação Windows concluída com sucesso no histórico fornecido: **#807**, commit `832ef7510f2c984a5a9ef76de7447c143d875f61`.
- Compilação consolidada anterior: **#804**, commit `4f39f92562b6eb470b16be1d3513e9d1ecbe8543`.
- A branch do redesign original permanece como ancestral/referência visual; não usar a `main` antiga como fonte do layout novo.
- PR #11 baseada na `main` foi rejeitada/fechada anteriormente e não deve ser retomada como base. A implementação posterior na branch `feature/offline-library-on-redesign` é uma nova implementação sobre o redesign aprovado.
- **Validação manual do usuário após #807: pendente de relato nesta conversa.** Não marcar reprodução, sincronização, Holyrics, pedidos, proteção de anúncios, controle remoto, importação/reprodução offline ou layout em resolução real como aprovados sem confirmação explícita.
- Nenhum merge para `main` foi autorizado por este registro.

### Checklist de continuidade
**Feito / evidência de CI**
- [x] Implementação da Biblioteca Offline sobre o redesign registrada no histórico (#710).
- [x] Marcos de compilação #780, #804 e #807 concluídos com sucesso conforme histórico apresentado pelo usuário.
- [x] Política de empacotamento manual após release consolidada registrada no commit #807.
- [x] Histórico de runs #661–#807 fornecido pelo usuário incorporado como contexto de continuidade.

**Aguardando teste manual do usuário**
- [ ] Abrir o pacote gerado pela compilação consolidada e confirmar que o redesign visual aprovado está presente, sem retorno ao layout antigo.
- [ ] Verificar Biblioteca Offline: navegação, pastas, itens, edição de metadados/letras e persistência após fechar/reabrir.
- [ ] Verificar importação MP3/WAV autorizado, reprodução local e funcionamento sem internet.
- [ ] Verificar pedido com arquivo local autorizado: preparação, associação à gravação exata, validade e reprodução.
- [ ] Verificar pedido contendo somente link YouTube: mensagem correta de indisponibilidade offline sem extração/contorno.
- [ ] Verificar organização de pastas: criar, escolher destino, mover, remover e persistir alterações.
- [ ] Verificar formulário público de pedidos, aviso de privacidade, limite por IP e moderação/bloqueio.
- [ ] Verificar navegador protegido, exceções por site e reprodução/pesquisa no YouTube; registrar anúncios que permanecerem.
- [ ] Revalidar reprodução, sincronização, limpeza automática ao fim da música e integração Holyrics, que possuem histórico anterior de problemas reais.
- [ ] Verificar controle remoto no celular e os fluxos de pedidos/preparação.
- [ ] Confirmar se os painéis compactos permanecem sem rolagem e sem cortar botões na resolução real do usuário.

### Próxima ação
- O usuário abrirá e testará a compilação mais recente; ao retornar, registrar cada resultado como aprovado, falhou ou inconclusivo, com evidência e etapa exata.
- Antes de qualquer nova alteração de código, comparar a solicitação com este checklist e preservar os fluxos já aprovados.


## 26/11/2026 — Auditoria da identidade visual da compilação (#804 e #807)

### Resultado da comparação direta do código
- Arquivo principal comparado: `src/Lyravoxen.Desktop/MainWindow.xaml`.
- Na branch `feature/offline-library-on-redesign`, o arquivo tem blob SHA `28281f23522a86456af45a22f48c7803a3ae1525`, 66.438 caracteres, inclui recursos dinâmicos de tema e a aba principal “Biblioteca Offline”.
- Na branch `main`, o mesmo arquivo tem blob SHA `1e82984fd26985d0644153ad24fd58a2492ca9f3`, 43.557 caracteres, e não contém a aba “Biblioteca Offline” nem a estrutura visual dinâmica encontrada no redesign.
- `App.xaml.cs` na branch de trabalho contém a implementação de tema (`SetTheme`); na `main`, essa implementação não existe.
- Comparação GitHub: `feature/offline-library-on-redesign` está 212 commits à frente de `main`, sem commits atrás no momento da consulta.
- Conclusão objetiva: **o código da branch de trabalho contém o layout novo; a branch main contém o layout antigo.**

### O que ocorreu com as compilações
- Commit #804 (`4f39f92562b6eb470b16be1d3513e9d1ecbe8543`) removeu a condição que restringia o job Windows Build a `workflow_dispatch` ou `main`. Isso permitiu que o job de empacotamento Windows fosse executado também na branch de trabalho. O histórico fornecido pelo usuário registra a execução #804 como concluída com sucesso e como pacote Windows consolidado.
- Commit #807 (`832ef7510f2c984a5a9ef76de7447c143d875f61`) voltou a introduzir a condição:
  `if: github.event_name == 'workflow_dispatch' || github.ref == 'refs/heads/main'`.
- Consequência importante: após #807, um push normal na branch `feature/offline-library-on-redesign` executa os testes do job `cloud-worker-tests`, mas **não executa o job `build`**. Para gerar pacote Windows nessa branch, é necessário disparar manualmente o workflow (`workflow_dispatch`) escolhendo a branch correta.
- O commit #807 alterou somente a condição do job no workflow; não modificou `MainWindow.xaml`, `App.xaml` ou o layout do aplicativo.
- Portanto, #807 não é evidência de que um novo executável com layout antigo foi produzido. O pacote consolidado #804 é o candidato identificado para conter o layout novo, pois o build foi liberado para a branch de trabalho naquele commit.
- Ainda não foi possível confirmar, pelos dados disponíveis nesta auditoria, qual arquivo EXE/ZIP o usuário abriu nem se o executável exibido na captura veio do artefato #804 ou de um pacote anterior da `main`. Não atribuir a causa como certeza sem conferir o nome/link/artefato efetivamente baixado.

### Estado e próxima ação
- [x] Confirmado que o código atual da branch de trabalho contém o redesign e a Biblioteca Offline.
- [x] Confirmado que `main` permanece com a interface antiga.
- [x] Identificada a razão operacional pela qual builds posteriores em push da branch podem não gerar executável: condição do job introduzida em #807.
- [ ] Confirmar qual pacote o usuário abriu (instalador ou ZIP portátil) e sua origem.
- [ ] Se for necessário gerar novo pacote, executar manualmente Windows Build na branch `feature/offline-library-on-redesign`; não fazer merge, reset ou alteração de layout.
- [ ] Ao testar, conferir primeiro as cinco abas principais e a presença da aba Biblioteca Offline antes de avaliar funcionalidades.

## 27/11/2026 — Windows Build #810 concluído na branch de redesign

### Execução confirmada no GitHub Actions
- Usuário informou que o workflow **Windows build #810** ficou verde: execução manual por `workflow_dispatch`.
- Run: `45931354035`.
- Link: https://github.com/jabesguitar/Lyravoxen/actions/runs/45931354035
- Branch: `feature/offline-library-on-redesign`.
- Commit exato compilado: `9531d4a6c65b89e5fdb3ddef4d9e50d665cf1ea7`.
- Job `cloud-worker-tests`: `success`; etapas concluídas incluem testes do serviço offline, cobertura da Ajuda, verificações estáticas C#, testes do Worker, formulário público e página remota mobile.
- Job `build`: `success`; etapas concluídas incluem geração do ícone, splash compacta, restore/build .NET, cue timeline tests, pacote ZIP portátil e instalador Inno Setup, com upload dos três artefatos.
- Isso comprova que a compilação foi feita a partir da branch correta e do commit registrado; não comprova ainda que o usuário abriu/instalou o pacote ou aprovou visualmente o aplicativo.

### Artefatos publicados (não expirados na consulta)
- Instalador Windows x64: `Lyravoxen-Installer-Windows-x64`, artifact ID `15168917805`, aproximadamente 51,3 MB.
- Pacote Windows x64: `Lyravoxen-Windows-x64`, artifact ID `15168088378`, aproximadamente 72,7 MB.
- ZIP portátil: `Lyravoxen-Windows-x64-portable`, artifact ID `15167933847`, aproximadamente 72,7 MB.
- Expiração informada pelo GitHub: 25/02/2027 às 02:57 UTC.
- SHA-256 publicados pelo GitHub:
  - Instalador: `2dd1fa63fa62cc8d9bafe8b4c061adf0a00ecac9d10bab50c72e37819554cc38`.
  - Windows x64: `19a63f08f8c40bd6a7a0c7613e77994e9ffffa012bc6c65ae051432a19132e5b`.
  - ZIP portátil: `57949e6a49cad49bf204841daff8a9ebaecf695de71615b23ede8f7c38989773`.

### Estado atualizado
- [x] Workflow manual executado na branch `feature/offline-library-on-redesign`.
- [x] Jobs de testes e build concluídos com sucesso.
- [x] Artefatos Windows x64, portátil e instalador publicados e identificados.
- [ ] Usuário baixar/abrir o pacote #810 e confirmar que o layout novo aparece, incluindo Biblioteca Offline.
- [ ] Rodada manual de testes de funcionalidades conforme checklist anterior; nenhum desses testes deve ser marcado como aprovado sem relato do usuário.
- [ ] Depois do retorno do usuário, registrar resultados específicos neste arquivo.
- Não fazer merge para `main` sem autorização explícita.

### Próxima ação
- Baixar preferencialmente o instalador `Lyravoxen-Installer-Windows-x64` (artifact ID `15168917805`) para instalação normal; alternativamente, usar o ZIP portátil `Lyravoxen-Windows-x64-portable` (artifact ID `15167933847`) para testar sem instalação.
- Ao verificar o aplicativo, primeiro confirmar a interface nova e a presença da Biblioteca Offline. Depois registrar as falhas/ajustes reais sem reiniciar etapas já concluídas.

## 27/11/2026 — Esclarecimento do requisito de mídia offline do YouTube

### Solicitação e justificativa do usuário
- O usuário esclareceu que insiste no YouTube porque muitas gravações necessárias não existem em outras fontes, especialmente versões específicas de playback.
- Exemplos citados: mesma música em meio tom abaixo, dois tons abaixo e outras tonalidades/arranjos preparados por terceiros.
- Para o evento, não basta encontrar outra gravação com o mesmo título: precisa ser exatamente a versão selecionada, preservando tom, arranjo e identidade do vídeo.
- Objetivo operacional: ter previamente uma cópia local utilizável quando faltar energia ou internet, sem comprometer a continuidade do evento.
- Restrição de custo reafirmada: solução gratuita, sem APIs ou assinaturas pagas adicionais.

### Decisão de produto / interpretação
- Preservar o vínculo exato entre item de repertório e gravação: YouTube videoId + URL original + metadados/descrição da versão (ex.: -1 semitom, -2 tons, playback).
- Não substituir automaticamente uma gravação por outra que tenha o mesmo título.
- A interface deve distinguir claramente: disponível online, arquivo offline pronto, fonte não baixável diretamente e requer arquivo autorizado.
- O requisito do usuário é continuidade offline da versão exata, não apenas cache de letras ou link.

### Limite e alternativas técnicas
- Sites de terceiros que aceitam URL do YouTube normalmente extraem ou convertem o fluxo de mídia; o fato de existirem não significa que o Lyravoxen possa reproduzir essa técnica ou que ela seja estável/segura/permitida.
- Não implementar extração de streams protegidos, DRM bypass, nem contorno de anúncios/restrições do YouTube.
- YouTube Premium/offline mantém a mídia no ambiente oficial do YouTube e não disponibiliza um MP3/WAV comum para o Lyravoxen acessar como arquivo local.
- Caminhos gratuitos viáveis para manter a gravação exata: arquivo fornecido pelo próprio criador/intérprete/autor do playback; link direto de download disponibilizado oficialmente; arquivo MP3/WAV que o usuário já possua e tenha direito de usar; ou gravação/produção própria autorizada.
- O fluxo do Lyravoxen deve permitir anexar/importar essa cópia ao item já cadastrado no YouTube, preservando o videoId como referência da versão e o hash do arquivo como identidade da cópia local.
- Se não houver arquivo autorizado, manter o link exato para reprodução online pelo player oficial e informar que ainda não há cópia offline; não declarar “Baixado” quando só foi salvo o link.

### Pendência
- Avaliar UX para associar um arquivo offline ao item do YouTube sem perder título, intérprete, autor, tom e videoId; permitir que o operador escolha explicitamente qual gravação/arquivo corresponde à versão selecionada.
- Testar reprodução offline real dessa cópia local, com internet desligada, antes de declarar o fluxo aprovado.

## 27/11/2026 — Confirmação do fluxo desejado para “Baixar offline” e retenção

### Requisito confirmado pelo usuário
- Na janela **Pedidos**, ao receber um pedido com link do YouTube, deve existir a ação **Baixar offline** no próprio pedido, sem exigir que o operador procure manualmente um arquivo no computador.
- O usuário espera que o download seja iniciado a partir do link recebido e que o arquivo fique guardado localmente para uso no evento, evitando depender de internet/energia.
- O mesmo princípio de armazenamento temporário deve atender à Biblioteca Offline.
- O usuário quer escolher o prazo de retenção dentre as opções já planejadas: **5, 7, 14 ou 30 dias**.
- Ao vencer o prazo, apagar somente o arquivo de mídia local para evitar acúmulo e peso excessivo no computador; preservar pedido, título, link, letra, metadados e histórico.
- A seleção do prazo deve ser clara no fluxo de download/armazenamento; o vencimento precisa ser visível e a limpeza automática deve ser confiável.
- O usuário reforçou que a experiência desejada é pelo link do pedido, sem seletor de arquivos como caminho principal.

### Limite técnico e comportamento honesto
- Um link de página/vídeo do YouTube não é uma URL direta de arquivo MP3/WAV. O serviço de mídia atual rejeita explicitamente hosts YouTube.
- Não implementar extração do fluxo de áudio protegido do YouTube, contorno de DRM ou mecanismos de proteção/anúncios.
- Portanto, não apresentar a ação como se qualquer URL YouTube fosse baixável pelo Lyravoxen. A ação deve verificar a origem e explicar claramente quando o link não fornece um arquivo direto.
- Caminhos compatíveis para obter a cópia exata: URL direta de arquivo publicada para download pelo criador/fornecedor; arquivo fornecido pelo criador; ou arquivo MP3/WAV local que o usuário possua e esteja autorizado a usar. Associar essa cópia ao pedido original, preservando o YouTube videoId e os metadados da versão.
- Não substituir uma versão específica (ex.: meio tom abaixo ou dois tons abaixo) por outra gravação de mesmo título.

### Implementação necessária
- Rever a experiência do botão **Baixar offline**: iniciar tentativa de download direto apenas para fontes que realmente entreguem arquivo de áudio permitido; se a URL for página YouTube, não abrir imediatamente seletor como se o download estivesse acontecendo. Exibir estado/motivo e oferecer associação de cópia autorizada como alternativa explícita.
- Unificar o serviço de armazenamento entre Pedidos e Biblioteca Offline.
- Registrar por item: caminho local, hash, formato, tamanho, data de armazenamento e data de expiração.
- Aplicar retenção configurável 5/7/14/30 dias; executar limpeza automática na inicialização e periodicamente enquanto o app estiver aberto.
- A limpeza deve remover somente mídia expirada, não registros, letras, metadados, links, histórico ou pastas do usuário.
- Garantir atomicidade do download (arquivo temporário e promoção após validação), validação real MP3/WAV, deduplicação por hash e associação à gravação exata.
- Permitir que o usuário identifique claramente se a música está apenas online ou se a cópia offline está pronta.

### Estado
- [x] Usuário confirmou o fluxo pretendido e as opções de retenção 5, 7, 14 e 30 dias.
- [ ] Ajustar UI e lógica de Baixar offline nos Pedidos para distinguir link YouTube de URL direta de arquivo.
- [ ] Unificar cache temporário de Pedidos e Biblioteca Offline.
- [ ] Implementar/testar retenção e limpeza automática, preservando metadados.
- [ ] Validar reprodução offline da cópia exata com internet desligada.

## 27/11/2026 — Auditoria de continuidade antes da finalização consolidada

### Verificações já existentes no código
- A limpeza de cópias offline expiradas já está implementada em `MainWindow.xaml.cs`:
  - é executada durante `LoadLibrary()`;
  - timer de limpeza dispara a cada minuto enquanto o app está aberto;
  - remove referências expiradas da Biblioteca e dos Pedidos, preservando título, URL, letra e metadados;
  - só apaga os bytes quando nenhum item restante referencia o mesmo caminho;
  - persiste alterações em biblioteca e pedidos.
- `OfflineExpiryPolicy.CanDelete` e testes existentes cobrem preservação de arquivos ainda referenciados e remoção de arquivos sem referências.
- Portanto, retenção/limpeza não deve ser reescrita sem necessidade. A tarefa é validar cenários reais de prazo 5/7/14/30, reinício do app, arquivo compartilhado e falha de exclusão.
- O serviço central `OfflineMediaService` já aceita download HTTP(S) de MP3/WAV direto, com limite, validação de conteúdo, redirects limitados, proteção contra destinos privados, hash e deduplicação.
- A UI dos Pedidos ainda direciona links YouTube para seletor de arquivo local e apresenta a mensagem de que não extrai áudio. Essa é a razão concreta da experiência não corresponder ao pedido do usuário de baixar a partir do link YouTube.
- Os testes offline atuais verificam download direto MP3/WAV, importação local, recusa explícita de YouTube, SSRF, redirects, tamanho, HTML, deduplicação, política do navegador e expiração por referência.
- O teste de Worker já verifica envio com cabeçalho `CF-Connecting-IP`, persistência do IP, criação/listagem de bloqueio, bloqueio de novo pedido e desbloqueio.

### Limites operacionais
- O Worker gerado no código atual implementa `GET /bloqueios`, `POST /bloqueios` e registro `CF-Connecting-IP`; a tela do usuário recebeu 404 e IP ausente, evidência de Worker publicado antigo.
- Não há integração Cloudflare conectada para publicar diretamente o Worker nesta sessão. A atualização do Worker existente permanece dependente de ação no painel Cloudflare; preservar namespace KV `PEDIDOS` e secret `ADMIN_TOKEN`.
- Não implementar extração de áudio/streams protegidos do YouTube nem contorno de anúncios. A ação não pode declarar sucesso quando o link é somente uma página/vídeo.
- O navegador atual usa bloqueio por host e explicitamente não garante remover anúncios in-stream/first-party do YouTube. A auditoria de proteção deve manter compatibilidade do player e não prometer equivalência total ao Brave.

### Próxima execução consolidada
1. Priorizar a correção operacional do Worker e preparar instrução única de atualização, sem recriar recursos Cloudflare.
2. Revisar a UI de “Baixar offline” para não abrir seletor como se estivesse baixando do YouTube; manter tentativa de URL direta de áudio e alternativa explícita de associar arquivo autorizado à gravação exata.
3. Revisar o cache automático na Biblioteca e o vínculo com as faixas preparadas/projetadas; não baixar mídia de páginas YouTube por extração protegida.
4. Adicionar testes adicionais de retenção (5/7/14/30), limpeza após reinício, arquivo compartilhado entre pedido e biblioteca e ausência de perda de metadados.
5. Auditar proteção WebView2 e testes de regressão para navegação, pesquisa e reprodução.
6. Rodar workflow completo e gerar uma única compilação consolidada; pedir uma única rodada de testes manuais do usuário.

### Estado desta auditoria
- [x] Confirmada existência de limpeza automática a cada minuto e na carga da biblioteca.
- [x] Confirmada política de não excluir arquivos ainda referenciados.
- [x] Confirmados testes automatizados do Worker para IP/bloqueio/desbloqueio.
- [x] Confirmado que a UI atual não faz download de link YouTube; oferece importação local.
- [ ] Ampliar testes de retenção e integração de mídia.
- [ ] Atualizar Worker em produção no Cloudflare.
- [ ] Consolidar correções restantes e gerar build único.

## 27/11/2026 — Verificação do build #810 e testes automatizados

### Execução confirmada
- Workflow: [Windows build #810](https://github.com/jabesguitar/Lyravoxen/actions/runs/45931354035)
- Resultado geral: `completed successfully`.
- Branch: `feature/offline-library-on-redesign`.
- Commit compilado: `9531d4a6c65b89e5fdb3ddef4d9e50d665cf1ea7`.
- Job `cloud-worker-tests`: sucesso.
  - Teste do serviço de mídia offline: sucesso.
  - Cobertura da ajuda: sucesso.
  - Verificações estáticas C#: sucesso.
  - Testes do Cloudflare Worker: sucesso.
  - Testes Playwright do formulário público: sucesso.
  - Testes Playwright da página remota móvel: sucesso.
- Job `build`: sucesso.
  - Restore e publish Windows: sucesso.
  - Teste da linha do tempo: sucesso.
  - ZIP portátil, pasta Windows e instalador: publicados.

### Artefatos disponíveis do #810
- Instalador: `Lyravoxen-Installer-Windows-x64`, artifact ID `15168917805`, SHA-256 `2dd1fa63fa62cc8d9bafe8b4c061adf0a00ecac9d10bab50c72e37819554cc38`.
- Pasta Windows: `Lyravoxen-Windows-x64`, artifact ID `15168088378`, SHA-256 `19a63f08f8c40bd6a7a0c7613e77994e9ffffa012bc6c65ae051432a19132e5b`.
- ZIP portátil: `Lyravoxen-Windows-x64-portable`, artifact ID `15167933847`, SHA-256 `57949e6a49cad49bf204841daff8a9ebaecf695de71615b23ede8f7c38989773`.
- Artefatos expiram em 25/02/2027.

### Conclusão e limite da validação
- Os testes automatizados do código presente no commit #810 passaram.
- Isso não valida ainda o Worker que está efetivamente publicado no endereço Cloudflare do usuário. O endpoint fornecido não pôde ser inspecionado pelo navegador de pesquisa nesta sessão.
- A auditoria do código confirma que o Worker gerado contém rotas autenticadas `GET/POST /bloqueios` e coleta `CF-Connecting-IP`. A versão atualmente publicada pelo usuário já havia apresentado IP ausente/404, logo precisa de atualização manual.
- O próximo bloqueio operacional é publicar o Worker atualizado no Cloudflare existente, mantendo o KV `PEDIDOS` e o secret `ADMIN_TOKEN`. Não criar outro Worker nem apagar o KV.
- Após publicação, testar o formulário de pedido, recebimento do IP, bloqueio e desbloqueio com dois IPs de teste distintos; só então marcar esse fluxo como validado em produção.

### Próxima ação manual
- No Lyravoxen, abrir Pedidos → Pedidos pela internet → Gerar código do serviço (worker.js).
- No Cloudflare, abrir o Worker que atende `lyravoxen-pedidos.ferkt319.workers.dev`, editar o código, substituir pelo worker.js gerado no app e clicar Deploy.
- Preservar bindings e secrets existentes: `PEDIDOS` e `ADMIN_TOKEN`.


## 27/11/2026 — Retomada estrita e ajuste do fluxo YouTube em Pedidos

### Regra de continuidade reafirmada pelo usuário
- Trabalhar somente nos três pontos solicitados: IP/bloqueios dos pedidos; download e Biblioteca Offline; proteção de anúncios na aba Próxima Música.
- Não alterar recursos que já funcionam, não fazer redesign e não tocar na branch `main`.
- A regra mais recente prevalece sobre instruções antigas de redesign/testes em etapas registradas anteriormente neste arquivo.

### Alteração aplicada
- Arquivo: `src/Lyravoxen.Desktop/MainWindow.Requests.cs`.
- Commit: `78e88d6942a578ac4491b94b577fe9185c2b3211`.
- No botão **Baixar offline** de um pedido cujo link é YouTube, a mensagem agora esclarece que o link não é arquivo MP3/WAV baixável diretamente e pergunta explicitamente se o operador deseja associar um arquivo autorizado que já possua.
- Se responder **Não**, a operação termina sem abrir seletor de arquivos. Se responder **Sim**, mantém o fluxo existente de seleção/importação do arquivo autorizado, associação ao pedido e Biblioteca, hash, metadados e retenção.
- URLs que não são YouTube continuam tentando o download direto existente; o fluxo de fallback para arquivo local não foi alterado.
- Nenhuma extração de áudio, contorno de anúncios ou alteração na reprodução do YouTube foi implementada.

### Verificações realizadas
- Releitura do estado da branch: `feature/offline-library-on-redesign`.
- Comparação com `main`: branch de trabalho está 222 commits à frente e 0 atrás; nenhum merge ou alteração na `main` foi feito.
- Build #810, commit `9531d4a6c65b89e5fdb3ddef4d9e50d665cf1ea7`, confirmado com jobs `cloud-worker-tests` e `build` concluídos com sucesso. Esse resultado é anterior à alteração deste registro e não valida o commit novo.
- Workflow `.github/workflows/windows-build.yml`: push na branch executa `cloud-worker-tests`; job `build` só executa por `workflow_dispatch` ou na `main`. Não foi alterado.

### Pendências reais
- [ ] Aguardar e conferir os testes automáticos acionados pelo commit `78e88d6`.
- [ ] Fazer build Windows consolidado por `workflow_dispatch` depois dos testes, sem criar builds intermediários desnecessários.
- [ ] Worker publicado no Cloudflare ainda precisa de atualização manual no Worker existente; manter binding KV `PEDIDOS` e secret `ADMIN_TOKEN`.
- [ ] Testar IP, bloqueio e desbloqueio em produção após deploy do Worker.
- [ ] Testar reprodução offline real com arquivo autorizado associado à gravação exata.
- [ ] Não há solução garantida para remover anúncios first-party/in-stream do YouTube sem comprometer o player; manter essa limitação explícita.

### Próxima ação técnica
- Conferir a execução de CI do commit `78e88d6`. Se passar, solicitar apenas o build consolidado via workflow manual. Em paralelo, continuar auditoria segura do fluxo da Biblioteca Offline e dos testes de retenção, sem reescrever a limpeza que já está implementada.


## 27/11/2026 — Build consolidado #822 concluído após correção de compilação

### Correção aplicada antes do build
- O log de compilação enviado pelo usuário apontou ausência de `System.IO` em três arquivos: `Services/YoutubeAudioDownloadService.cs`, `Services/PermanentLyricsArchive.cs` e `MainWindow.OfflineStorage.cs`.
- Adicionado `using System.IO;` nos três arquivos, sem alterar a lógica dos serviços.
- Commits de correção: `904e2cd`, `9c59f34` e `eeb1f78` (branch `feature/offline-library-on-redesign`).

### Resultado verificado
- Workflow [Windows build #822](https://github.com/jabesguitar/Lyravoxen/actions/runs/45942283747), commit `eeb1f78d433fe09d46a7448b91023ef3ef256c04`: concluído com sucesso.
- `cloud-worker-tests`: sucesso — teste de mídia offline, cobertura da Ajuda, verificações estáticas C#, testes do Worker, formulário público e página remota móvel.
- `build`: sucesso — Restore, publish Windows x64, teste da linha do tempo, ZIP portátil, instalador Inno Setup e upload dos três artefatos.

### Artefatos #822 (válidos até 25/02/2027)
- Instalador Windows x64: artifact ID `15173087837`, SHA-256 `98203e478aed295ab6d7b5ed61da3b6462a0d2e20a7486536cde002a2ff03bad`.
- Pasta Windows x64: artifact ID `15172753278`, SHA-256 `f1dbf9e007935adcdedfb3be252766572f865de75e4e07e57045afba59cd3c12`.
- ZIP portátil: artifact ID `15172354465`, SHA-256 `680ce96f6fd6322f62a574fb9fc832e81fd5946367f7b7291360d84a2e0a838e`.

### Estado e limites
- Build e testes automatizados estão aprovados para o commit acima. Isso não substitui os testes manuais no computador do usuário.
- O usuário deverá instalar o instalador #822 e executar a lista de testes descrita em `RETOMADA_ATUAL.md`.
- A atualização do Worker em produção no Cloudflare continua pendente; os testes automatizados validam o código, não a publicação no ambiente do usuário.
- Não fazer merge na `main`; permanecer na branch de desenvolvimento.


## 27/11/2026 — Melhorias de Biblioteca Offline e reprodução persistente

### Solicitações do usuário
- Destacar visualmente a opção selecionada nas listas de músicas utilizadas (Histórico de vídeos), pastas e músicas da Biblioteca.
- Fazer a reprodução offline continuar ao navegar entre abas, com controles de pausar/retomar, parar, volume e posição.
- Adicionar à Biblioteca Offline um campo para colar link, escolher pasta física de destino e baixar uma faixa individualmente.
- Usuário autorizou colocar as mudanças em prática e concluir a etapa para liberar tempo para outros projetos.

### Implementações enviadas à branch
- MainWindow.xaml: estilo global de ListBoxItem reforça seleção com fundo ciano/azul, borda e brilho; hover distinto. A alteração alcança listas do histórico e biblioteca.
- MainWindow.xaml.cs: handlers de Histórico e Biblioteca deixam de limpar SelectedItem imediatamente, mantendo a seleção visível após carregar/reproduzir.
- LocalAudioPlayer passou de UnloadedBehavior=Stop para Manual, evitando que a navegação de abas ordene explicitamente a parada do áudio.
- Barra global OfflinePlaybackBar adicionada no nível raiz da janela, com identificação da faixa, pausar/retomar, parar, volume e posição.
- PlayLocalAudio atualiza a barra, volume e posição; handlers controlam o MediaElement; timer atualiza o slider; encerramento oculta a barra.
- Biblioteca Offline recebeu campo de URL direta MP3/WAV, seletor de pasta física de destino e botão de download para a coleção selecionada.
- Download manual usa OfflineMediaService existente, que valida mídia direta e grava no destino; o item é registrado na coleção selecionada com retenção 5/7/14/30 dias e persistência na biblioteca.
- Links de página YouTube não são aceitos neste novo campo de download direto; mensagem orienta a importar uma cópia autorizada. Não há bypass de DRM, paywall ou anúncios.

### Commits
- 19536c982776a7c12fab7db6501ecfa8cd6ffd3c — destaque de seleção e barra global de controles no XAML.
- 5075494d10cc20cf2b80824e07aef783091419a6 — handlers de reprodução, pausa, parada, volume e posição.
- b87d62addaa7746017e74dcb6c99e44266d5cf21 — UI de download manual.
- 1e9e37813364f8750dc02d0b55533464b865cdb6 — lógica de download manual e escolha do destino.
- fd6c16f1215dfc49b05175a1c56cc0106e7d64f2 — manter item de histórico/biblioteca selecionado após ação.

### Estado de validação
- Alterações enviadas à branch feature/offline-library-on-redesign; nenhuma alteração na main.
- Ainda não há build Windows posterior a esses commits nem teste real de reprodução ao alternar abas, controles ou download manual.
- Precisa validar XML/XAML, compilação, referências dos handlers e os cenários de reprodução e download. Não declarar concluído até build verde.
- A barra de posição deve ser conferida em uso (sincronização do slider e seek); o destino físico externo à pasta padrão deve ser validado com retenção/limpeza.

### Próxima ação
- Rodar validações estáticas e workflow Windows build na branch. Corrigir qualquer falha de compilação. Depois entregar um único instalador atualizado e checklist curto focado apenas nas três mudanças desta etapa.


## 27/11/2026 — Build #855 concluído com sucesso

- Workflow: https://github.com/jabesguitar/Lyravoxen/actions/runs/46018345034
- Run ID: 46018345034; commit: 24894084251fc467590b8fa3efdadd6ae391e4a6; branch feature/offline-library-on-redesign.
- Resultado: sucesso nos jobs cloud-worker-tests e build. Passaram teste OfflineMediaService, cobertura da Ajuda, verificações estáticas C#, testes do Cloudflare Worker, formulário público e página móvel; também Restore, publish Windows x64, teste de timeline, ZIP portátil, instalador e upload dos artefatos.
- Artefatos disponíveis até 25/02/2027:
  - Instalador Windows x64: ID 15207121706, 53.858.548 bytes.
  - Pasta Windows x64: ID 15207466365, 76.282.040 bytes.
  - ZIP portátil: ID 15207491300, 76.317.985 bytes.
- O build comprova compilação e testes automatizados, mas não substitui validação funcional no computador do usuário. A reprodução offline ao alternar abas, controles de pausa/retomada/posição/volume e download manual com destino escolhido precisam ser conferidos em execução real.


## 27/11/2026 — Correção da lista da Biblioteca e redesign da janela Pedidos recebidos

### Relato do usuário
- Ao selecionar uma pasta na Biblioteca Offline, a lista de músicas recebidas/da coleção deixou de aparecer.
- Janela Pedidos recebidos considerada visualmente básica: pediu cards/barras individuais com destaque para nome do conjunto/cantor, música, link e botões; mais espaço para visualizar a lista, visual minimalista com fonte legível. Autorizou remover botões desnecessários.

### Alterações implementadas na branch
- MainWindow.xaml: os comandos inferiores da coluna Músicas da coleção foram colocados em ScrollViewer com altura máxima, preservando espaço vertical para a lista de faixas.
- MainWindow.xaml.cs: ao trocar de pasta, o filtro de pesquisa da pasta anterior é limpo antes de atualizar a lista, evitando que uma busca antiga esconda todas as músicas da nova pasta.
- MainWindow.Requests.cs: janela Pedidos recebidos ampliada para 920x760 (mín. 720x560), fundo escuro coerente com o app, cabeçalho com contador, barra compacta de ações e configurações de recebimento recolhidas em Expander para liberar espaço à fila.
- Cards individuais destacam título da música, conjunto/cantor (campo Group existente), link, data/IP, situação offline e botões de ação. A ação Copiar link foi removida dos cards para reduzir ruído; permanecem Preparar próxima, Baixar offline, Abrir link e Descartar pedido. Bloqueio de IP permanece disponível como ação secundária.
- Os dados recebidos não foram alterados: a estrutura atual do pedido contém Group (nome do conjunto/cantor), Name (nome da música) e Url (link); não há campo de artista separado no modelo atual.

### Commits
- ef8e87da46e3b5e8bfd255ab0b6448c11d59abaf — tentativa inicial de limitar a área dos comandos inferiores.
- a8feb9ec4e3bf3e0ee9cfa49b99f9b26e03bf06a — limpar busca ao trocar de pasta.
- 3ba92662caba4b3a35954434a72c0b54b5568586 — redesign da janela de pedidos.
- cfa82f1b3b1009d70d4bee30435bc475a4f1e35d — limpeza do cabeçalho do redesign.

### Validação
- Build #862 está em execução; commits intermediários foram cancelados automaticamente quando novos commits foram enviados. Só o resultado do #862 deve ser considerado.
- A correção de lista e o redesign ainda precisam de build verde e validação visual no Windows.


## 27/11/2026 — Redesign do Controle pelo celular

- Solicitação do usuário: aplicar o mesmo cuidado visual da janela Pedidos recebidos à janela aberta pelo botão Controle pelo celular, mantendo as funções; o usuário autorizou seguir diretamente.
- MainWindow.Remote.cs: janela passou de layout claro simples de 420 px para painel escuro de 560 px, cartões separados, títulos e instruções em hierarquia, QR code destacado, endereço em campo legível com botão Copiar endereço, PIN em destaque e ação Gerar novo PIN. Conteúdo rolável e responsivo à altura disponível.
- Não foi removida nenhuma capacidade existente: QR, lista de URLs locais, PIN, renovação do PIN, instruções de Wi-Fi e Firewall continuam disponíveis.
- Commit: c4b7796e38dd502c4a276a603987dc0c331be18a.
- Build #866 está em andamento: https://github.com/jabesguitar/Lyravoxen/actions/runs/46029745807. Aguardando resultado; ainda não validar como concluído.


## 27/11/2026 — Proteção adicional contra endpoints de anúncios na aba Próxima música

### Objetivo
- Pedido do usuário: colocar em prática uma proteção mais efetiva contra anúncios na experiência do YouTube, especialmente na aba Próxima música, sem prejudicar a reprodução.

### Inspeção e limites técnicos
- A aba Próxima música usa WebView2 com `BrowserAdBlocker` já conectado; o player de reprodução principal usa a API oficial IFrame do YouTube em outra instância.
- O bloqueador anterior atuava principalmente por domínio e preservava integralmente domínios centrais do YouTube, portanto não filtrava endpoints de anúncio servidos no próprio domínio `youtube.com`.
- Referências open source estudadas anteriormente: ZWebWrap (GPL-3.0-or-later; não foi copiado) e YouTube-Desktop-App (MIT/Electron; filtros como referência, não integração direta).
- Foi escolhida uma alteração conservadora, sem dependências novas: filtrar caminhos de requisição conhecidos `/pagead/` e `/api/stats/ads` apenas em hosts `youtube.com` e subdomínios, mantendo a API IFrame, páginas normais e hosts de entrega de mídia liberados.
- Exceção de domínio configurada pelo usuário continua prevalecendo.

### Alterações e commits
- `c026684d69d7d26e8ec42ad946eb9d9ece24ba3e` — adiciona a política `ShouldBlockRequest(Uri...)` com regras conservadoras de caminho.
- `b27d13035a8fc7c19a873d20dd81eceae701932e` — conecta o evento WebView2 à nova política de requisição.
- `c554d1cb08bf047fdee8184f682ceb6dd1320f4f` — acrescenta testes para bloquear endpoints conhecidos e preservar API IFrame, entrega googlevideo e exceções do usuário.

### O que isso faz e o que não promete
- Bloqueia solicitações reconhecidas por esses caminhos de anúncio e mantém bloqueio por listas de domínios já existente.
- Não injeta scripts no player, não altera respostas de vídeo e não usa proxy, certificados locais, cookies ou bypass de DRM/paywall.
- Não garante remover anúncios in-stream ou anúncios entregues por endpoints variáveis/first-party. A compatibilidade real deve ser testada com vídeos variados.
- O filtro por caminho é aplicado ao WebView2 da aba de navegação/Próxima música, pois é ali que `BrowserAdBlocker.Attach` já estava conectado; o player principal segue separado para reduzir risco de regressão.

### Validação
- Build #870 concluiu com sucesso antes destes três commits: https://github.com/jabesguitar/Lyravoxen/actions/runs/46030925149.
- Os commits desta implementação acionam nova execução do workflow; ainda é necessário aguardar build verde e testar reprodução real no aplicativo.
- Não alterar `main` nem fazer merge sem autorização.


## 27/11/2026 — Correção de compilação: BrowserAdBlocker

- O usuário forneceu o log do `dotnet publish`, com nove erros CS0103 em `MainWindow.xaml.cs` (linhas 768–799): o símbolo `BrowserAdBlocker` não existia no contexto.
- Causa raiz confirmada: o arquivo `src/Lyravoxen.Desktop/Services/BrowserAdBlocker.cs` havia sido sobrescrito incorretamente com o conteúdo de `BrowserRequestPolicy`, removendo a classe `Lyravoxen.Services.BrowserAdBlocker`.
- Correção: restaurada a implementação completa do serviço WebView2, preservando configurações, cache/lista de hosts, exceções, contagem e o novo encaminhamento de requisições para `BrowserRequestPolicy.ShouldBlockRequest`.
- Commit corretivo: `bfebc4b14ebba8deea3801cf63e7295d447b53d9`.
- Build #876 disparado para validar a correção: https://github.com/jabesguitar/Lyravoxen/actions/runs/46034132906.
- Avisos CS8602 e CS4014 do log original não foram alterados; não eram erros fatais nem a causa do publish falhar.
- Aguardando resultado do #876. Não considerar o publish corrigido até o workflow terminar com sucesso.


## 27/11/2026 — Teste real: anúncios do YouTube continuam aparecendo

- Retorno do usuário após instalar/testar a compilação: na primeira música pesquisada e reproduzida, apareceram dois anúncios.
- Resultado funcional: a filtragem pontual de `/pagead/` e `/api/stats/ads` não resolveu a presença de anúncios pré-roll/in-stream. A proteção não pode ser considerada concluída.
- Build #878 concluiu com sucesso após a restauração de `BrowserAdBlocker`: https://github.com/jabesguitar/Lyravoxen/actions/runs/46034192649. Isso comprova compilação/testes automatizados, não remoção de anúncios.
- Nova pesquisa técnica:
  - ZWebWrap implementa motor de filtro completo com `adblock-rust`, scriptlets e injeção cosmética, mas o próprio wrapper tem licença GPL-3.0-or-later e ainda não tem release; não copiar o wrapper integralmente para o Lyravoxen.
  - O motor upstream `brave/adblock-rust` é MPL-2.0 e oferece network blocking, cosmetic filtering, resource replacements e uBlock Origin scriptlet support. É uma base tecnicamente mais apropriada para avaliar integração independente, observando as obrigações MPL.
  - O código Windows do ZWebWrap usa classificação de tipo de recurso, contexto da página e avaliação de regras por URL; também aplica injeção de scripts cosméticos. Isso é substancialmente mais completo do que a lista de domínios atual.
- Próxima direção técnica a avaliar: integrar o motor upstream Brave adblock-rust (MPL-2.0) como biblioteca nativa Windows x64 com interface C ABI/P/Invoke para o C# WebView2; carregar listas ABP/uBO, avaliar tipo de recurso e URL da página de origem, aplicar exceções de segurança e injeção cosmética em documentos. Avaliar tamanho, consumo de memória, suporte a CPUs antigas e processo de build antes de adotar.
- Não assumir que mesmo um motor completo removerá todos os anúncios do YouTube: anúncios server-side/in-stream servidos como parte do próprio fluxo podem não ser distinguíveis de conteúdo normal.
- Nenhuma integração nova desse motor foi implementada ainda. A alteração atual continua sendo o filtro pontual e deve ser considerada insuficiente para o objetivo do usuário.
- Branch `feature/offline-library-on-redesign`; `main` permanece intocada.


## 27/11/2026 — Correção de compilação do protótipo nativo de filtragem

- O usuário forneceu erro CS0117: `CoreWebView2WebResourceContext` não contém `WebSocket`.
- Causa: o SDK WebView2 1.0.2903.40 usado pelo projeto não expõe esse membro no enum.
- Correção aplicada: removido apenas o mapeamento explícito `WebSocket`; recursos não listados continuam classificados como `other`.
- Commit: `db889fbb79c6e292a21b9505259e4f22f8256e76`.
- Build #886 falhou por esse único erro fatal; avisos CS8602/CS4014 permanecem não fatais.
- A correção dispara novo workflow. Ainda não há confirmação de compilação completa do motor Rust, cópia da DLL, nem teste real com YouTube.


## 27/11/2026 — Evidência visual: motor de filtragem ainda não remove anúncios do YouTube

- O usuário enviou duas capturas de tela do Lyravoxen na aba Próxima música.
- Captura 1: anúncio em vídeo Pantene, identificado como patrocinado, dentro do conteúdo reproduzido; também há anúncio patrocinado na lateral.
- Captura 2: anúncio em vídeo Mercado Livre, identificado como patrocinado, dentro do conteúdo reproduzido; também há anúncio patrocinado na lateral.
- Resultado: a proteção atual continua falhando no objetivo principal. Não pedir novas provas ao usuário.
- A integração do motor Brave adblock-rust e listas EasyList/EasyPrivacy, mesmo se compilada e carregada, ainda não comprovou bloqueio de anúncios do YouTube. O pipeline atual cobre requisições de rede; não há integração completa de filtros cosméticos/scriptlets uBlock nem evidência de que os anúncios in-stream sejam bloqueáveis por esse caminho.
- Próxima investigação deve separar dois alvos: (1) anúncios de interface/elementos patrocinados da página de pesquisa e recomendações, que podem exigir filtros cosméticos/scriptlets e regras adequadas; (2) anúncios pré-roll/in-stream dentro do player, que podem ser servidos pelo próprio fluxo do YouTube e não são garantidamente removíveis por bloqueio de URL.
- Antes de novas mudanças, confirmar resultado do build mais recente e verificar se a DLL nativa foi carregada em runtime (não apenas se foi copiada para a pasta). Instrumentar estado do motor, lista carregada e contadores sem expor dados sensíveis.
- Não declarar proteção eficaz até testes funcionais reproduzíveis com YouTube, inclusive pré-roll, e sem regressões na reprodução/letras.
- Não alterar `main`; branch de desenvolvimento.


## 27/11/2026 — Nova estratégia: extensão de navegador open source integrada ao WebView2

- Após o teste visual confirmar anúncios Pantene e Mercado Livre no YouTube, o usuário expressou frustração com a repetição de tentativas incompletas e pediu uso prático de projetos open source existentes, inclusive fora do GitHub.
- A estratégia foi alterada: em vez de recriar um bloqueador, integrar uma extensão de navegador existente no WebView2.
- Projeto selecionado para protótipo: uBlock Origin Lite (uBOL), distribuição oficial Edge, release `2026.1124.1811`, projeto `uBlockOrigin/uBOL-home`, licença GPL-3.0. Extensão será distribuída sem modificações, com arquivo LICENSE e referência ao código-fonte upstream.
- A documentação Microsoft confirma `CoreWebView2Profile.AddBrowserExtensionAsync`, persistência da extensão no perfil e a necessidade de habilitar `CoreWebView2EnvironmentOptions.AreBrowserExtensionsEnabled`.
- Alterações realizadas:
  - `MainWindow.xaml.cs`: cria ambiente WebView2 compartilhado com extensões habilitadas; configura perfil em LocalAppData/Lyravoxen/WebView2; instala a extensão uBOL do diretório `Extensions/uBOLite` se o manifesto existir.
  - `.github/workflows/windows-build.yml`: baixa ZIP oficial Edge do release fixo, extrai a pasta que contém `manifest.json`, empacota no artefato e inclui LICENSE upstream.
  - Mantém filtro legado e protótipo Rust como camadas adicionais/fallback por enquanto.
- Commits principais:
  - `59d82f352f518a0ea76c1cc614cbfda83ebb581a` — primeira integração experimental (inicialização WPF; corrigida depois para usar EnvironmentOptions).
  - `2b7758511e28a1bb8cb5985b50e7f24bfe05427c` — download e inclusão do ZIP oficial da extensão.
  - `463f56ef916a5aeb6887998f799aad430a4bf58c` — caminho correto do LICENSE.
  - `80a5cccc823428bbe60205cf77610089117156d7` — habilitação correta via CoreWebView2EnvironmentOptions e ambiente compartilhado.
- Build #891 foi concluído com sucesso antes da integração da extensão e comprovou a compilação do protótipo Rust e do aplicativo naquela revisão: https://github.com/jabesguitar/Lyravoxen/actions/runs/46039033387.
- A revisão com a extensão ainda precisa de build. Mesmo com extensão instalada, a compatibilidade específica do uBOL com APIs disponíveis no WebView2 e a eficácia contra anúncios in-stream precisam ser verificadas. A Microsoft documenta a API de extensão; isso não garante que todas as APIs usadas pela extensão tenham suporte idêntico ao Edge completo.
- Próxima ação: confirmar build do commit `80a5ccc`/workflow subsequente, verificar extensão no perfil e adicionar teste de runtime/telemetria local para confirmar instalação/ativação. Não pedir novas provas de que anúncios existem; usuário já forneceu duas capturas.
- Não alterar `main`.


## 27/11/2026 — Teste do uBlock Origin Lite no WebView2: falhou

- Usuário instalou/testou a build #897 e enviou captura de tela.
- Resultado visual: anúncio pré-roll "Aproxime-se de Deus" (1 de 2) dentro do player, além de anúncios patrocinados na lateral. Pesquisa `harpa crista 525`; anúncio visível em 0:14/4:06 com botão Pular.
- Portanto, a integração experimental uBlock Origin Lite ainda não resolveu os anúncios no WebView2. Não solicitar novas provas.
- Build #897 passou e gerou artefatos, mas isso só confirma compilação e empacotamento, não eficácia da extensão.
- Inspeção do código:
  - `InitializeBrowserViewAsync` chama `AddBrowserExtensionAsync` se `Extensions/uBOLite/manifest.json` existir.
  - Não consulta `GetBrowserExtensionsAsync` para confirmar a extensão instalada, nome/ID e `IsEnabled`.
  - A variável `_uBlockExtensionInstalled` só confirma que a chamada terminou sem exceção, não que regras DNR/scriptlets estão funcionando no YouTube.
  - O status escrito durante instalação é posteriormente sobrescrito por "Player pronto..." na inicialização, ocultando o diagnóstico.
  - O painel "Proteção ativa · 81.671 regras · 0 solicitações bloqueadas" é do bloqueador legado/host list; não mede regras nem contadores do uBOL.
  - A instalação é chamada a cada inicialização do processo porque o boolean é estático em memória, embora a API persista a extensão no perfil; a documentação diz que adicionar o mesmo caminho reinstala a extensão.
- Documentação consultada:
  - Microsoft: `AddBrowserExtensionAsync` retorna objeto `CoreWebView2BrowserExtension`, instalado fica `IsEnabled=true` por padrão e persiste no perfil; `GetBrowserExtensionsAsync` pode listar as extensões. https://learn.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.core.corewebview2profile.addbrowserextensionasync
  - WebView2 permite extensões, mas extensões que dependem de ícone/popup não têm UI do navegador no WebView2. Isso não prova falha do motor de filtragem. https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/overview-features-apis
  - uBOL usa Manifest V3/declarativeNetRequest, com limitações inerentes do DNR e regras/scriptlets declarativos. https://github.com/uBlockOrigin/uBOL-home/wiki/Frequently-asked-questions-%28FAQ%29
- Diagnóstico honesto: ainda não sabemos se uBOL foi instalado/ativado e se DNR aplicou regras; o app não expõe esses estados. Também não sabemos se os anúncios de vídeo vistos são bloqueáveis pela versão Lite. Não afirmar que o WebView2 não suporta DNR sem teste/documentação específica.
- Próxima ação de engenharia (antes de outra build para o usuário):
  1. Consultar `GetBrowserExtensionsAsync` após instalação e em cada inicialização; localizar por ID/nome, conferir `IsEnabled`; não reinstalar se já instalado.
  2. Registrar diagnóstico local em arquivo: caminho do pacote, manifest/version, extensão ID/nome/IsEnabled, versão do WebView2 Runtime, erros HRESULT de instalação e eventos de navegação.
  3. Expor no painel uma indicação inequívoca "uBlock Lite: instalado/ativo/não carregado" separada do contador legado.
  4. Verificar se o build contém de fato os arquivos e se a extensão é a variante Edge correta; validar hashes/versão.
  5. Executar teste de compatibilidade DNR mínimo com extensão MV3 de diagnóstico ou fixture de requisições controladas antes de voltar a testar anúncios reais.
  6. Se uBOL estiver ativo e DNR funcionar mas o anúncio do YouTube continuar, avaliar a limitação da extensão/YouTube e outras arquiteturas de navegador, sem repetir o bloqueador legado.
- Preferência de continuidade reiterada pelo usuário: manter sempre `MEMORIA_CONTINUA.md` e `RETOMADA_ATUAL.md` atualizados com o que foi feito e a próxima ação para retomada após interrupção.
- Branch `feature/offline-library-on-redesign`; `main` não alterada.


## 27/11/2026 — Segunda confirmação visual: uBlock Lite não impediu anúncios de vídeo

- Usuário enviou nova captura da aba Próxima música, pesquisa `harpa crista 15`.
- A captura mostra anúncio em vídeo Amazon Prime/Blade Runner 2099, identificado como patrocinado, "2 de 2", com duração de 6 segundos e reprodução em 0:03/0:06. Também aparece anúncio patrocinado na lateral.
- Resultado: falha confirmada novamente. A extensão uBlock Origin Lite, mesmo empacotada e com chamada de instalação no código, não está impedindo os anúncios exibidos no teste.
- Não pedir mais capturas ou provas ao usuário. Ele já forneceu evidências repetidas.
- Diagnóstico ainda em aberto: não foi confirmado por API se uBOL está realmente instalado e habilitado no perfil; também não foi validado se as regras DNR da extensão funcionam no WebView2. Mesmo ativa, uBOL Lite pode ter limitações para anúncios específicos do YouTube; não atribuir a causa a uma delas sem instrumentação.
- Próxima ação de engenharia obrigatória antes de qualquer novo instalador:
  1. Implementar diagnóstico real usando `CoreWebView2.Profile.GetBrowserExtensionsAsync()`; listar extensão ID, nome, versão e `IsEnabled`.
  2. Procurar extensão existente por ID e não chamar `AddBrowserExtensionAsync` repetidamente; instalar apenas se ausente e então reler a lista.
  3. Registrar versão do WebView2 Runtime, caminho do pacote, manifesto e exceções/HRESULT em log local.
  4. Separar no UI o status da extensão do contador antigo "regras/domínios/bloqueios"; não exibir "proteção ativa" como se significasse que uBOL bloqueia.
  5. Criar uma prova de funcionamento de DNR com uma extensão MV3 mínima/fixture local e requisição controlada, antes de voltar a YouTube.
  6. Se a extensão estiver ativa e DNR estiver funcional mas anúncios persistirem, pesquisar/avaliar outro projeto open source comprovadamente compatível com WebView2/Chromium; não repetir o bloqueador legado nem insistir na mesma extensão sem justificativa.
- Build #897 teve sucesso na compilação/empacotamento, mas a eficácia funcional falhou. Compilação verde não é aceitação.
- Preferência permanente reiterada: manter sempre `MEMORIA_CONTINUA.md` e `RETOMADA_ATUAL.md` atualizados com trabalho concluído, problemas e próxima ação para retomada.
- Branch `feature/offline-library-on-redesign`; `main` não alterada.


## 27/11/2026 — Resumo de passagem de etapa e prevenção de retrabalho

- Usuário solicitou uma lista objetiva do que foi feito para poder avançar para outra etapa, ressaltando o custo de tempo de sessões longas e a necessidade de continuidade confiável.
- Foi consolidado o estado atual no topo de `RETOMADA_ATUAL.md`, preservando o conteúdo histórico anterior.
- O resumo distingue alterações implementadas, builds aprovados, testes reais falhos e diagnósticos ainda pendentes.
- Estado atual: build #901 passou após atualização documental; a versão com uBlock Origin Lite (#897) compilou, mas o usuário confirmou anúncios de vídeo e laterais. Bloqueio de anúncios não está concluído.
- Próxima ação técnica: verificar estado real da extensão via `GetBrowserExtensionsAsync`, ID/nome/versão/`IsEnabled`, registrar runtime/HRESULT e testar DNR isoladamente antes de decidir manter/substituir a abordagem. Não pedir mais provas de anúncios.
- Objetivo operacional: evitar novas instalações intermediárias e concentrar trabalho antes da próxima rodada manual; usuário quer avançar para outras etapas do Lyravoxen.


## 27/11/2026 — Decisão do usuário: pausar a frente de bloqueio de anúncios

- Após sucessivos testes negativos (bloqueador legado, filtros de endpoints, protótipo Brave adblock-rust e uBlock Origin Lite no WebView2), o usuário informou que precisa avançar para outras etapas e não quer continuar gastando tempo sem garantia.
- O usuário cogitou um botão redondo de controle uBlock na aba Próxima música, abaixo do botão de tema.
- Orientação técnica: é possível implementar um controle visual para habilitar/desabilitar uma extensão já instalada via API WebView2, condicionado à extensão estar instalada e habilitada; isso NÃO garante remoção de anúncios in-stream/first-party do YouTube.
- Não prometer bloqueio total nem criar um botão que sugira eficácia não comprovada.
- Recomendação dada: pausar a frente de bloqueio de anúncios como prioridade, sem interromper o projeto Lyravoxen como um todo. Preservar o código experimental e os registros para eventual retomada.
- Nenhum novo botão foi implementado nem qualquer código alterado nesta decisão.
- Próxima ação: seguir para outra funcionalidade do Lyravoxen escolhida pelo usuário; a frente de anúncios fica pausada até existir uma arquitetura com evidência de compatibilidade e eficácia suficiente para justificar novo esforço.


## 27/11/2026 — Requisito confirmado: sequência automática e organizador manual persistente

- O usuário esclareceu que não exige apenas uma sincronização automática nem uma ordem fixa. Quer duas alternativas complementares:
  1. **Automática (preferencial):** colar a letra em qualquer organização original; o sistema analisa o áudio, reconhece frases e repetições (especialmente coros que aparecem uma vez no site mas se repetem na gravação), e monta a sequência real de execução sem o usuário ter que dizer "estrofe/coro".
  2. **Manual (alternativa confiável):** editor visual em blocos/trechos selecionáveis, inspirado no fluxo de seleção de blocos de ferramentas de projeção. O usuário marca os trechos e organiza a ordem real; pode inserir o mesmo bloco várias vezes sem duplicar o texto original; salva a sequência associada à música para reutilização futura.
- A letra-fonte original deve ser preservada separadamente da sequência de execução e dos tempos. A sequência manual persistida deve sobreviver a reabertura/reimportação da mesma música e poder ser editada.
- O automático não pode ser prometido como já existente: o matcher atual `LiveLyricsMatcher` é monotônico/sequencial e não resolve sozinho repetição de refrões fora da ordem original. Será necessário desenvolver alinhamento tolerante a repetição/retorno e avaliar reconhecimento de áudio.
- Não forçar o usuário a pré-classificar estrofes/coros. A classificação pode ser opcional/assistida; o usuário quer selecionar trechos sem ter que rotular cada um.
- Ajuda do aplicativo deve ser atualizada junto com cada recurso entregue, explicando botões, atalhos, modos automático/manual, como salvar e recuperar sequência.
- Não há aprovação para remover a letra original ou descartar sincronizações já salvas.
- Próxima etapa técnica: desenhar modelo de dados persistente separado (blocos-fonte, sequência de ocorrências e tempos); estudar como encaixar no atual `LyricCue`/persistência; implementar primeiro organizador manual com reuso de bloco e salvar/carregar, mais testes; depois aprimorar o modo automático com repetição. Evitar entregar uma versão que só tenha marcação linear por espaço como se atendesse ao requisito.


## 27/11/2026 — Escopo de interface confirmado para a aba Cadastrar letras

- O usuário esclareceu que todo o fluxo novo deve ficar dentro da aba **Cadastrar letras**: colar/importar a letra, analisar/sincronizar automaticamente, organizar manualmente blocos e repetições, revisar, salvar e recuperar a sequência.
- Não espalhar essas funções em outras abas nem exigir alternância para completar o cadastro/sincronização. A aba Ao vivo permanece para operação/projeção durante o evento; a aba Cadastrar letras concentra preparação, organização e sincronização.
- O usuário autorizou retirar botões e controles disfuncionais, não utilizados ou redundantes dentro do escopo trabalhado.
- Remoção deve ser baseada em inspeção de uso/event handlers/referências. Não remover funções úteis, atalhos, fluxos ativos ou controles com dependências sem verificar impacto.
- Priorizar interface simples e rápida: controles essenciais visíveis, funções avançadas organizadas sem poluir a tela, Ajuda atualizada para cada recurso entregue.
- Requisitos de sequência automática e organizador manual persistente permanecem: preservar letra original, separar sequência de ocorrências e tempos; permitir repetir blocos sem duplicar texto.
- Nenhum botão foi removido nesta decisão; esta é a autorização e diretriz para a próxima implementação.


### Nova etapa — Cadastro de letras funcional (27/11/2026)

**Solicitação e requisitos confirmados pelo usuário**
- Transformar a aba Cadastrar letras em editor funcional de preparação e sincronização; não fazer apenas redesign visual.
- **Automático:** usuário informa o vídeo/link e cola a letra integral; o sistema analisa o áudio real, reconhece a correspondência entre frases e posiciona tempos de entrada, permitindo revisão antes de finalizar. Não confundir estimativa uniforme com sincronização automática real.
- **Manual:** mostrar frases/linhas em blocos quadrados selecionáveis; selecionar com mouse ou teclado/setas, acompanhar a reprodução e marcar cada entrada no instante correto. Permitir voltar, corrigir, reordenar e concluir.
- Ao finalizar, salvar permanentemente letra original, ordem de execução, repetições de refrão, tempos/cues, autor, intérprete, título e vínculo com origem/URL/ID. Reabrir deve recuperar a sincronização sem refazer o trabalho.
- Disponibilizar a mesma função no controle remoto por IP como terceira aba, além de Ao vivo e Bíblia; computador e celular compartilham biblioteca, persistência e resultados, com layout adaptado.
- Remover controles/botões sem função real ou sem uso. Não deixar ações decorativas ou promessas que não executam.
- Preservar funções úteis e não alterar a branch main sem autorização.

**Auditoria inicial do código na branch feature/offline-library-on-redesign**
- LiveAudioTranscriptionService.cs: captura local do áudio do Windows e transcrição incremental via Whisper.net em blocos de 5 segundos, idioma português, sem API paga. É reconhecimento durante a reprodução, não análise integral prévia do vídeo.
- LiveLyricsMatcher.cs + AutoSyncPlanner.cs: correspondência incremental monotônica e estimativa de tempos para linhas puladas. Não é alinhamento forçado completo e não garante todas as repetições de refrão.
- MainWindow.TapSync.cs + TapSyncPlanner.cs: modo manual divide texto em estrofes e grava um toque por estrofe (Espaço/botão); ainda não implementa cartões por frase/linha selecionáveis com setas/mouse.
- PermanentLyricsArchive.cs: arquivo JSON independente com backup, chave por ID do YouTube/URL/metadados e cópia de letra/cues. MainWindow.xaml.cs consulta _permanentLyrics ao abrir vídeo; auditar e completar todos os pontos de gravação/recuperação do novo fluxo.
- MainWindow.RemoteLive.cs: permite colar letra para música atual/próxima, salvar texto, estimar tempos e marcar por toque. Página remota ainda precisa da aba de cadastro e editor completo.
- MainWindow.xaml: cadastro atual tem painel de toque por estrofe, editor de texto e prévia; não corresponde ao editor em blocos quadrados pedido.

**Pesquisa técnica iniciada (referências abertas, não decisões aprovadas)**
- k2-fsa/sherpa-onnx: documenta forced alignment (timestamps de palavras a partir de áudio + transcrição); investigar Windows/CPU, português, tamanho e licenças.
- m-bain/whisperX: referência para timestamps em nível de palavra/alinhamento; avaliar dependências Python/PyTorch e peso para app leve.
- jordisantamaria/karaoke-subs e ACE4AKSHAY/TimeStamper: referências de pipeline e ferramentas de alinhamento; estudar abordagem e auditar licença/dependências antes de reutilizar.
- aubio e Sonic Visualiser/MATCH Vamp: possíveis ferramentas auxiliares de análise/inspeção temporal, não decisão de implementação.

**Critérios obrigatórios**
- Não declarar pronto por compilar ou aparecer na interface.
- Testar canto em português, playback instrumental, ruído/intro, pausas, linhas repetidas e refrão repetido; indicar baixa confiança e permitir correção manual.
- Testar persistência após fechar/reabrir e carregar outro vídeo da mesma música; recuperar a gravação/ID correto.
- Confirmar que controle remoto e Windows compartilham registros, nos dois sentidos.
- Validar mouse, setas, Enter/Espaço, desfazer, voltar, corrigir, concluir e salvar.
- Remover somente botões comprovadamente sem ação/uso; preservar recursos válidos e documentar remoções.

**Etapa atual:** especificação confirmada e auditoria inicial concluída; implementação ainda não iniciada nesta etapa. Próxima ação: mapear handlers e persistência de cues, provar mecanismo de alinhamento com áudio em português e implementar editor comum para desktop e remoto, seguido de testes e build integrado.


### Complemento confirmado — estrutura musical, velocidade e preservação do produto (27/11/2026)

**Novos requisitos explícitos do usuário**
- O modo automático deve identificar e estruturar estrofes e coro/refrão, inclusive quando a ordem de execução for Estrofe 1 → Coro → Estrofe 2 → Coro → Estrofe 3 → Coro. Não presumir que o texto original seja a ordem linear da execução.
- Detectar indicações editoriais de sites como [Estrofe], [Coro], [Refrão], [Refrão 2x] quando existirem, mas também funcionar quando o site não fornecer rótulos.
- Preservar texto-fonte sem duplicação e criar uma sequência de ocorrências de execução: o mesmo bloco de coro pode ser referenciado diversas vezes com timestamps diferentes.
- Rapidez é requisito de operação ao vivo: preparar estrutura antes do evento quando possível, manter reconhecimento incremental de baixa latência durante a música, evitar análise demorada na hora da apresentação e oferecer correção manual imediata.
- Preservar o layout moderno/atual e todas as funções que já funcionam. Alterar somente a área/fluxo solicitado. Não restaurar layout antigo, não refatorar telas não relacionadas, não remover recursos válidos e não sobrescrever a versão moderna.
- Usuário autorizou pesquisa ampla, experimentos e comandos; manter histórico de todas as mudanças e cópia no GitHub para retomada futura.

**Pesquisa técnica adicional — achados**
- Librosa `segment.recurrence_matrix` calcula recorrência/semelhança entre frames de áudio; a documentação explica que conexões indicam frames repetidos. `recurrence_to_lag` e `recurrence_novelty` ajudam a identificar repetição e fronteiras entre seções. Isso pode apoiar agrupamento estrutural, mas não atribui semanticamente o rótulo 'coro' sozinho.
- WhisperX `alignment.py` inclui modelo Hugging Face para português (`jonatasgrosman/wav2vec2-large-xlsr-53-portuguese`) e faz forced alignment palavra/trecho com transcrição conhecida. É tecnicamente promissor para alinhamento de texto já conhecido; dependências PyTorch/torchaudio e tamanho/carga devem ser medidos antes de uso no produto.
- sherpa-onnx é multiplataforma, offline e tem suporte a várias linguagens/CPU, mas a tabela oficial consultada de modelos de forced alignment lista exemplos em inglês/chinês e não confirmou um modelo português pronto. Não selecionar para alinhamento português sem prova concreta.
- Essentia é C++ e otimizada para processamento de áudio, mas a documentação indica licença AGPLv3 (ou comercial) e suporte Windows parcial. Isso pode conflitar com distribuição gratuita/proprietária; não incorporar sem revisão jurídica/licença.
- `marcjohnce/keytool` descreve segmentação estrutural com recorrência de CQT/MFCC, agrupamento espectral e rótulos heurísticos (coro como parte recorrente mais forte; estrofe como outra parte recorrente). Usar como referência de arquitetura, verificar licença e não copiar código sem auditoria.
- `chiranr19/hookfinder` foi descartado como dependência: o próprio repositório informa licença source-available sem permissão de copiar, modificar ou redistribuir. Pode ser lido apenas como referência conceitual não reutilizável.

**Arquitetura recomendada para protótipo (proposta técnica, ainda não aprovada como implementação)**
1. Parser textual: preservar blocos/linhas e reconhecer cabeçalhos/indicações explícitas de estrofe, coro, ponte, repetição e número de vezes.
2. Agrupamento por texto: normalização para comparar blocos idênticos/quase idênticos; marcar duplicatas prováveis como mesma seção com múltiplas ocorrências, sem fundir versos diferentes por engano.
3. Análise de áudio offline/preparatória: extrair características harmônicas/temporais e recorrência para encontrar seções musicalmente semelhantes; combinar com limites de silêncio, mudanças de energia e batida. Resultado é hipótese com confiança, não verdade absoluta.
4. Alinhamento: testar WhisperX Portuguese forced alignment em arquivo local, comparando qualidade/tempo/CPU com Whisper.net existente. Para execução ao vivo, manter reconhecedor incremental já existente como caminho rápido e usar estrutura preparada para mapear repetição/retorno.
5. Sequência de execução: modelo de ocorrências com `SectionId`, `OccurrenceIndex`, `StartSeconds`, `EndSeconds`, `Confidence` e referência ao bloco-fonte. Permite coro reaparecer 2x/3x sem duplicar o texto-fonte.
6. Revisão expressa: antes de entrar ao vivo, exibir sequência detectada e permitir corrigir rótulos/ordem com poucos cliques; no evento, operação de marcação deve ser imediata.

**Preservação de interface e escopo**
- Branch criada para isolar esta etapa: `feature/lyrics-structure-fast-sync`, partindo de `feature/offline-library-on-redesign` após os commits documentais.
- Manter XAML, estilos, recursos visuais, navegação e handlers atuais que já funcionam. Fazer alterações localizadas apenas na aba Cadastrar letras e nos pontos mínimos de serviço/modelo/controle remoto necessários.
- Antes de cada alteração: registrar arquivo/hash/trecho afetado; depois: registrar diff, motivo, teste e resultado. Não substituir arquivos inteiros por versões antigas.
- Não afirmar implementação/teste/build concluído sem executar e registrar.

**Estado:** pesquisa adicional feita e arquitetura de protótipo delimitada; nenhum código funcional foi alterado nesta etapa. Próxima ação técnica: criar protótipo de parser/agrupador de blocos com casos de repetição, em seguida comparar motor de alinhamento português e medir latência; só então integrar no editor preservando layout atual.


### Protótipo inicial de estrutura textual criado (27/11/2026)
- Criado `src/Lyravoxen.Desktop/Models/LyricsStructureAnalyzer.cs` na branch `feature/lyrics-structure-fast-sync`.
- O protótipo reconhece marcadores textuais explícitos para estrofe/verso, coro/refrão, ponte, intro e final; interpreta marcadores de repetição `2x`/`x2` até 8 repetições.
- Blocos sem rótulo com texto normalizado idêntico compartilham uma identidade de seção e geram ocorrências separadas na ordem de execução. Isso evita duplicar o texto-fonte para refrões repetidos.
- Blocos únicos sem evidência permanecem `Unknown` e `NeedsReview`; o protótipo não inventa classificação de estrofe/coro.
- Adicionados casos de teste ao `tests/Lyravoxen.TimelineTests/Program.cs` e inclusão do modelo no `.csproj` de testes: sequência estrofe/coro, coro 2x, repetição sem rótulo, identidade de bloco e incerteza de classificação.
- Atualizado `.github/workflows/windows-build.yml` para incluir a branch de protótipo nos gatilhos e na condição de build.
- Limitação atual: ainda é somente parser textual. Não analisa áudio, não descobre repetições quando o texto-fonte contém apenas uma ocorrência de coro sem rótulo e não está integrado à UI/cadastro/arquivo permanente.
- Validação: não foi possível executar `dotnet run` nesta sessão porque o ambiente local não tem .NET SDK. A API do GitHub também não mostrou execução de Actions para a branch no momento da consulta. Portanto, código/testes estão commitados, mas ainda não foram compilados nem aprovados.
- Próxima ação: executar CI/build no GitHub, corrigir eventuais erros, depois implementar análise preparatória de áudio e integrar com sequência de ocorrências, sem modificar o layout moderno fora da área solicitada.


### Persistência da estrutura musical adicionada ao modelo (27/11/2026)
- `LibraryVideo` agora contém `Sections` e `ExecutionOrder`, além de `FullLyrics` e `Cues`, separando blocos-fonte e ocorrências na ordem da execução.
- `PermanentLyricsArchive` foi ampliado para copiar/gravar/recuperar profundamente seções e ocorrências no arquivo JSON permanente; registros com sequência de execução também podem ser arquivados.
- A suíte `Lyravoxen.TimelineTests` inclui um caso de salvar e reabrir o arquivo permanente e verificar que as duas ocorrências do coro permanecem distintas.
- Os arquivos de teste incluem `LyricsStructureAnalyzer.cs` e `PermanentLyricsArchive.cs`.
- **Validação ainda pendente:** a execução de Actions da branch foi cancelada por concorrência entre os commits sucessivos; não há resultado de compilação/teste do último conjunto de mudanças. O teste de persistência está escrito, mas ainda não aprovado.
- Próxima ação: obter uma execução CI completa sobre o commit mais recente; corrigir erros antes de ligar o parser à interface. A UI moderna não foi alterada e nenhuma função existente foi removida.


### Correção do primeiro teste de estrutura (27/11/2026)
- O log do job `Test cue timeline` mostrou falha em `Program.cs:263`: a asserção esperava 6 ocorrências.
- Contagem correta do caso: 3 estrofes + coro 1 + coro 2 + coro final 2x = 7 ocorrências. O teste estava errado; a falha não indicava erro no parser.
- Corrigida a expectativa de 6 para 7 e detalhada a conta na mensagem do teste. As demais asserções do caso continuam verificando quatro ocorrências de coro, quatro blocos-fonte (três estrofes + um coro) e índices do coro de 1 a 4.
- Próximo passo: aguardar a execução completa do CI no novo commit. Até ela terminar, a correção está commitada, mas não aprovada.


### Protocolo permanente de continuidade — 27/11/2026
- Solicitação explícita do usuário: não interromper o desenvolvimento para perguntas rotineiras; seguir até uma entrega testável. Só perguntar antes de mudanças que possam alterar, substituir, remover ou comprometer estrutura/funções existentes que já funcionam.
- A cada etapa relevante, registrar no GitHub o que foi solicitado, o que foi sugestão técnica, decisões aprovadas/pendentes, arquivos alterados, commits, testes executados e resultados reais.
- Manter este arquivo como histórico cronológico completo e `RETOMADA_ATUAL.md` como resumo operacional da etapa vigente.
- Ao iniciar uma nova conversa de trabalho, consultar primeiro esses dois arquivos e o estado real da branch/commits/Actions; não reiniciar o planejamento nem presumir que o resumo está atualizado.
- Ao encerrar uma etapa/conversa, atualizar os registros no GitHub. Se não for possível acessar o GitHub, preparar o registro aqui em Markdown e informar claramente que não foi publicado.
- Não perguntar novamente preferências já confirmadas: preservar o layout moderno e funções existentes, manter branch isolada, priorizar solução gratuita/leve, automático primeiro e manual por blocos como alternativa, e preparar a mesma capacidade no controle remoto.
- Limite de segurança: se uma implementação exigir alterar protocolo, banco/arquivo incompatível, remover comportamento funcional, mudar arquitetura de integração ou afetar outras abas, interromper antes dessa mudança e pedir aprovação específica.
- Esta solicitação não autoriza merge na `main`, remoção de recursos existentes ou mudanças fora do escopo sem análise.


## 27/11/2026 — CI #931 validado
- Usuário informou que o workflow #931 terminou com sucesso. Verificação direta do run `46067616951` confirmou ambos os jobs: `build` e `cloud-worker-tests` em `completed/success`.
- O job Windows concluiu restore/build .NET, teste de cue timeline, empacotamento portátil, instalador Inno Setup e upload dos três artefatos.
- O job cloud concluiu os testes do serviço offline, Help coverage, verificações C# e testes do Worker/página mobile.
- Artefatos baixados e verificados: instalador ZIP íntegro e pacote portátil interno íntegro; IDs registrados no estado atual no início deste arquivo.
- Limite: esta execução valida o estado existente e a suíte configurada, não implementa nem prova alinhamento de áudio, nova UI ou cadastro remoto.


## 27/11/2026 — Correção da persistência estrutural em andamento
- Auditoria confirmou que `PersistCurrentCues` copiava texto e cues, mas não `Sections`/`ExecutionOrder` ao criar ou atualizar itens na pasta sincronizada. Também não recuperava as duas propriedades do arquivo permanente no caminho de restauração do vídeo preparado.
- Commit `ad79c1df7d1f8356ffa328750e77cffa8c29fd04` adiciona cópias profundas dessas propriedades na criação, atualização e recuperação.
- Workflow disparado: `46069001270`; no último acompanhamento estava `in_progress`, com `cloud-worker-tests` executando Playwright. O resultado de compilação desse commit ainda não foi confirmado.
- Auditoria adicional: motor de voz usa WASAPI loopback e Whisper GGML Base em segmentos de 5 s; parser atual usa repetição textual exata e não gera timestamps/confiança; controle remoto é GET-only com PIN e buffer 65.536 bytes.
- Próxima ação: confirmar CI do commit `ad79c1d`, corrigir qualquer falha e então ampliar o modelo temporal/confiança e desenvolver o fluxo remoto aditivo via POST.


## Marco de continuidade confirmado — build #977 (28/11/2026)

**Verificação realizada no GitHub nesta retomada.**

- Repositório: `jabesguitar/Lyravoxen`.
- Branch ativa: `feature/lyrics-structure-fast-sync`. O commit `5598481` era o HEAD no momento da execução #977; os commits posteriores desta retomada alteraram somente os dois arquivos de continuidade.
- Workflow: [Windows build #977](https://github.com/jabesguitar/Lyravoxen/actions/runs/46106917809), concluído com sucesso em 28/11/2026.
- Jobs aprovados: `cloud-worker-tests` e `build`.
- O commit `5598481` tem como pai `b2a0dfabdad6e470e0ec50d0cccdadd2844aafe8`. A comparação confirmou um único commit e quatro arquivos alterados, todos de documentação: `MEMORIA_CONTINUA.md`, `RETOMADA_ATUAL.md`, `docs/CADASTRO_LETRAS_IMPLEMENTACAO.md` e `docs/HISTORICO_CADASTRO_LETRAS.md`. Não houve alteração do código funcional nesse commit.
- Portanto, o código da aplicação validado pelo build #977 continua sendo o código de `b2a0dfa`; o #977 repetiu a compilação e o empacotamento desse código junto com a atualização documental.

### Etapas aprovadas no #977

- `cloud-worker-tests`: teste do serviço de mídia offline, cobertura da Ajuda, verificações estáticas C#, Worker Cloudflare, formulário público de pedidos e página remota mobile.
- `build`: geração do ícone e splash, restore/build .NET, engine nativa Rust de bloqueio, bundle do aplicativo e da extensão uBlock Origin Lite para Edge, teste `Test cue timeline`, pacote portátil e instalador Windows.
- Os dois jobs foram concluídos com `success`. Isso comprova os testes automatizados configurados e a compilação/empacotamento, não substitui o teste manual real no computador e com música reproduzida.

### Artefatos do build #977

- Windows x64: `15247723159` — [execução #977](https://github.com/jabesguitar/Lyravoxen/actions/runs/46106917809).
- Portátil ZIP: `15247643490`.
- Instalador Windows: `15248535524`.
- No momento da consulta, os três artefatos constavam como não expirados.

### Etapa em andamento e próxima ação

A remodelagem do Cadastro de Letras e o fluxo online-first com fallback offline estão implementados no código validado; **a validação manual controlada continua pendente**.

1. No pacote do #977, testar item offline com internet (deve tentar o vídeo original do YouTube e exibir vídeo), o mesmo item sem internet (deve tocar a cópia local), cópia local expirada com link original válido (deve tentar o YouTube) e vídeo privado/sem incorporação com cópia local válida (deve recorrer ao arquivo local).
2. No Cadastro de Letras, colar uma letra completa, gerar cartões/slides, marcar durante a reprodução, desfazer/repetir/reordenar, salvar, fechar e reabrir para conferir persistência dos tempos e da ordem.
3. Registrar os resultados reais antes de declarar concluída a remodelagem.
4. Depois da validação, avançar para a aba remota dedicada ao Cadastro de Letras e para o protótipo/benchmark de alinhamento completo de áudio cantado em português.

### Limitações conhecidas que permanecem

- O reconhecimento automático ainda é Whisper local incremental durante a reprodução; não é forced alignment prévio da música inteira e ainda não foi validado em gravação real.
- A terceira aba remota dedicada ao Cadastro de Letras ainda não está implementada.
- O fallback online/offline após erro depende do evento `playerError`; player travado sem `ready` ou `playerError` pode precisar de timeout de inicialização.
- A suíte CI verde não deve ser apresentada como prova de sincronização musical real.

### Regras preservadas

- Não fazer merge na `main` sem autorização explícita.
- Preservar layout moderno, dados, metadados e recursos funcionais fora do escopo.
- Soluções gratuitas e leves sempre que viáveis.
- Não pedir testes de builds intermediários; esta é a rodada manual consolidada registrada como pendente.
- Atualizar este arquivo e `RETOMADA_ATUAL.md` após cada novo marco, sem apagar snapshots históricos.
