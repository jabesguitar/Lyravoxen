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
