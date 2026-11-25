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
