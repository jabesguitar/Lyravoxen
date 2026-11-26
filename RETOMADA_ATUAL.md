# Lyravoxen — Registro de retomada
**Atualizado em:** 26/11/2026  
**Branch:** `feature/offline-library-on-redesign`  
**Commit de referência:** `0f5b591aa417c408cc7ca55df43096ae26043130`

## Objetivo imediato
Consolidar as correções da aba **Próxima música**, o fluxo real de mídia offline e a documentação, antes de pedir uma única rodada de testes manuais ao usuário.

## Evidências recebidas do usuário
Foram recebidas duas capturas de tela em 26/11/2026.

### 1. Aba Próxima música
- O painel lateral esquerdo ainda apresenta barra de rolagem vertical.
- Os controles de preparar, confirmar/verificar, proteção, permitir, voltar/avançar, atualizar/copiar link e fontes continuam ocupando espaço vertical e parte das opções fica abaixo da área visível.
- Pedido do usuário: reorganizar os controles em layout compacto/minimalista, mantendo as ações necessárias visíveis sem rolar.
- Restrição: não reduzir a área útil do navegador/WebView2 nem prejudicar pesquisa, abas e reprodução.

### 2. Botão “Baixar offline” nos Pedidos
- A ação atual abre uma caixa de diálogo para escolher um arquivo MP3/WAV já existente.
- O usuário esperava que o botão baixasse a mídia da fonte autorizada e a salvasse automaticamente na Biblioteca Offline, sem abrir um seletor de arquivo para localizar algo.
- Correção de produto necessária: distinguir claramente **Baixar de URL direta autorizada** de **Importar arquivo MP3/WAV local**. A importação local deve continuar disponível como alternativa.
- Ao baixar de uma URL direta autorizada, salvar em armazenamento do Lyravoxen, validar tipo MIME/extensão/tamanho/timeout, calcular hash, evitar duplicatas e associar a cópia à identidade exata do pedido/gravação.
- Se o pedido contiver somente URL do YouTube, preservar URL/ID e explicar que o link por si só não fornece um arquivo offline autorizado. Não implementar extração de áudio, DRM bypass ou contorno de restrições da plataforma.
- Ao preparar/tocar novamente, priorizar a cópia local vinculada à versão exata e não procurar correspondência apenas pelo título.

## CI concluído — execução 45874275889
**Status:** sucesso em 26/11/2026.  
**Branch:** `feature/offline-library-on-redesign`  
**SHA:** `0f5b591aa417c408cc7ca55df43096ae26043130`

Jobs aprovados:
- `cloud-worker-tests`: sucesso.
  - Check Help coverage
  - Extract and test Cloudflare Worker
  - Install Playwright Chromium
  - Test mobile remote page
- `build`: sucesso.
  - Restore e Build .NET
  - Test cue timeline
  - Pacote portátil ZIP
  - Instalador Inno Setup
  - Upload dos artefatos

Artefatos gerados:
- `Lyravoxen-Installer-Windows-x64` — artifact ID `15144741646`
- `Lyravoxen-Windows-x64-portable` — artifact ID `15144651792`
- `Lyravoxen-Windows-x64` — artifact ID `15143443015`

A execução comprova build e testes automatizados indicados acima. **Não comprova** inspeção visual em diferentes resoluções, reprodução real offline, nem download de áudio por URL.

## Decisões e restrições preservadas
- Não fazer merge na `main` sem aprovação explícita.
- Não pedir ao usuário para instalar builds intermediários.
- O usuário só fará uma rodada manual quando o conjunto estiver consolidado.
- Manter o app leve e preservar o redesign aprovado.
- Preservar histórico, letras, metadados, URLs e identidade exata das gravações.
- Manter importação manual MP3/WAV.
- Não prometer remoção de anúncios first-party/in-stream do YouTube.

## Checklist de trabalho
### A fazer
- [ ] Inspecionar e reorganizar o painel esquerdo de Próxima música para remover a necessidade de rolagem, sem clipping.
- [ ] Manter editor/prévia/navegador com áreas úteis preservadas.
- [ ] Implementar download de URL direta de áudio autorizada, separado da importação local.
- [ ] Validar MIME, extensão, tamanho, timeout, hash, deduplicação e mensagens de erro.
- [ ] Garantir que o arquivo baixado seja reproduzido offline pelo fluxo de preparar/tocar a próxima música.
- [ ] Implementar/validar expiração automática enquanto o app estiver aberto, apagando somente o arquivo e preservando ficha/letra/metadados.
- [ ] Revisar proteção do navegador e exceção “Permitir neste site”.
- [ ] Atualizar Ajuda e “Novidades desta versão”.
- [ ] Executar build, testes automatizados e verificações estáticas/visuais possíveis.
- [ ] Gerar uma única compilação consolidada para a rodada manual do usuário.

### Feito e confirmado
- [x] Capturas de tela analisadas e problemas reproduzidos visualmente no relato.
- [x] Run `45874275889` terminou com sucesso.
- [x] Artefatos Windows x64, portátil e instalador foram gerados.
- [x] Testes do Worker, página remota mobile e cobertura da Ajuda passaram no run citado.

### Testes manuais — somente após consolidação
- [ ] Próxima música: verificar que todos os controles necessários ficam visíveis sem rolagem.
- [ ] Próxima música: verificar pesquisa, abas e reprodução no navegador.
- [ ] Pedidos: testar URL direta autorizada e confirmar download/salvamento automático.
- [ ] Pedidos: testar URL apenas do YouTube e confirmar mensagem transparente sem falsa indicação de download.
- [ ] Pedidos: importar arquivo MP3/WAV local como alternativa.
- [ ] Preparar/tocar: desligar internet e confirmar reprodução da cópia local exata.
- [ ] Biblioteca Offline: conferir validade e preservação de letras/metadados após expiração.
- [ ] Proteção: alternar proteção e exceção de domínio sem quebrar sites necessários.

## Próxima ação
Continuar diretamente na branch `feature/offline-library-on-redesign`, inspecionando os XAML e serviços existentes antes de alterar. Não pedir nenhuma ação ao usuário agora. Atualizar este registro e `MEMORIA_CONTINUA.md` após mudanças significativas.


## Atualização após as capturas — 26/11/2026
- Alterado o painel esquerdo da aba Próxima música no commit 7580049ac07b6096e1b86fd602bb8716541df236.
- Removido o ScrollViewer que permitia rolagem vertical; os controles foram reorganizados em grade compacta, com botões de menor altura/padding e fontes menores. As ações de proteção, navegação, atualização, copiar link, fontes e abas foram mantidas.
- A coluna direita que contém o navegador/WebView2 foi preservada sem alteração intencional de largura/estrutura.
- Workflow acionado automaticamente pelo push: 45875746475 (status queued no último acompanhamento). Não instalar/testar até o CI terminar e até a rodada consolidada.
- A alteração visual ainda precisa passar por build e inspeção real; não declarar como validada visualmente antes disso.

## Registro documental
- Arquivo completo de retomada: este arquivo, mantido no repositório.
- Histórico cronológico: MEMORIA_CONTINUA.md atualizado no commit 8a115be77d78aab91945537d425a7b8c6bc65134.
- Cópia local para o usuário: RETOMADA_ATUAL_LYRAVOXEN.md.


## Pacote único de correções solicitado — 26/11/2026

O usuário pediu que as correções abaixo sejam tratadas como um único bloco integrado, sem entregas ou testes fragmentados. Nenhuma dessas tarefas deve ser considerada concluída apenas por estar planejada.

### A. Navegador com proteção contra anúncios e rastreadores
- O usuário já havia solicitado que o navegador interno se comporte de forma semelhante a um navegador com proteção, reduzindo anúncios intrusivos, pop-ups e rastreadores.
- A proteção atual baseada em listas/domínios e a exceção Permitir neste site foram relatadas como não funcionando corretamente; precisam ser investigadas, corrigidas e testadas.
- Melhorar bloqueio de anúncios e rastreadores de terceiros, pop-ups e redirecionamentos indesejados, mantendo exceções por site e controles claros de ligar/desligar proteção.
- Preservar compatibilidade com YouTube, pesquisa, login, abas, sites de letras, downloads autorizados e reprodução WebView2.
- Limite técnico a comunicar: não prometer bloqueio de anúncios first-party/in-stream servidos pelo próprio YouTube ou pela mesma origem do conteúdo. Não quebrar player ou APIs essenciais para tentar removê-los.
- Incluir explicação honesta na Ajuda sobre o que a proteção bloqueia, o que pode permanecer e como permitir um site.

### B. Painel da aba Próxima música
- Corrigir a rolagem vertical do painel lateral esquerdo, deixando os comandos principais acessíveis sem rolar.
- Usar botões minimalistas/compactos e reorganização em grade, sem cortar comandos.
- Preservar integralmente a área útil do navegador/WebView2, pesquisa, abas e reprodução.
- A primeira alteração visual foi enviada no commit 7580049ac07b6096e1b86fd602bb8716541df236; ainda depende do CI e inspeção visual real.

### C. Sistema único de Biblioteca Offline — Pedidos e Biblioteca
O usuário confirmou que o mesmo problema do botão Baixar offline dos Pedidos também ocorre na aba Biblioteca Offline: a ação abre seletor/pesquisador de arquivos ou pastas e não salva a mídia na Biblioteca como esperado. Corrigir ambos os pontos com um único serviço central.

**Pedidos**
- Baixar URL direta de arquivo de áudio autorizada e salvar automaticamente no armazenamento interno do Lyravoxen, sem pedir pasta de destino nem abrir seletor de arquivo de origem.
- Associar o arquivo ao pedido e à identidade exata da gravação/versão.
- Se a URL for apenas do YouTube, preservar URL/ID e informar que não há arquivo offline autorizado disponível; não extrair áudio nem contornar restrições da plataforma.

**Biblioteca Offline**
- A ação para adicionar/preparar mídia offline deve salvar a cópia no armazenamento interno do Lyravoxen e registrar o item na coleção/pasta selecionada.
- Não abrir seletor de pasta de destino no fluxo normal de download/salvamento.
- Manter ação separada Importar MP3/WAV para selecionar um arquivo já existente no computador; após selecionar, copiar para o armazenamento interno e criar/atualizar o registro da Biblioteca.

**Serviço comum e critérios técnicos**
- Um diretório interno padrão, criação automática de pastas, registro persistente, progresso/estado e mensagens claras.
- Validar protocolo, MIME, extensão, tamanho máximo e timeout para URL direta autorizada.
- Calcular hash e deduplicar sem confundir gravações ou tons diferentes; nunca casar somente pelo título.
- Unificar metadados e prazo de expiração (5, 7, 14 ou 30 dias), limpeza automática enquanto o app estiver aberto e após reinicialização.
- Expiração remove somente o arquivo local; preservar ficha, letra, histórico, metadados e link.
- Garantir que preparar/tocar use a cópia local exata, inclusive após reiniciar o app e sem internet.

### D. Documentação, testes e entrega
- Atualizar Ajuda e Novidades da versão para navegador/proteção, Pedidos, Biblioteca, importação, download e limites.
- Atualizar MEMORIA_CONTINUA.md e RETOMADA_ATUAL.md cronologicamente.
- Rodar build e testes automatizados; fazer inspeção estática e visual possível; não afirmar teste manual que não ocorreu.
- Só depois entregar uma única compilação consolidada e um checklist de testes para o usuário.
- Não solicitar instalação de builds intermediários e não fazer merge na main sem aprovação explícita.

### Checklist integrado de aceitação
- [ ] Próxima música: comandos principais visíveis sem rolagem e sem clipping; navegador mantém tamanho útil.
- [ ] Navegador: proteção liga/desliga; bloqueio de terceiros/pop-ups funciona; Permitir neste site cria exceção efetiva; YouTube/pesquisa/login/abas/letras/reprodução continuam funcionais.
- [ ] Navegador: Ajuda explica limites de anúncios first-party/in-stream do YouTube.
- [ ] Pedidos: URL direta autorizada baixa e salva automaticamente na pasta interna.
- [ ] Pedidos: URL somente YouTube não é falsamente apresentada como arquivo baixado; URL/ID permanecem vinculados.
- [ ] Biblioteca: adicionar mídia registra e salva automaticamente na coleção selecionada, sem pedir pasta de destino.
- [ ] Biblioteca: importar MP3/WAV seleciona origem, copia para armazenamento interno e cadastra o item.
- [ ] Pedidos e Biblioteca usam o mesmo serviço, hash/deduplicação, metadados e validade.
- [ ] Reprodução da cópia correta funciona offline e após reiniciar o app.
- [ ] Expiração remove apenas mídia e preserva registros/letras/metadados.
- [ ] Ajuda, Novidades, testes CI e build consolidado aprovados.


## 26/11/2026 — Correção do erro de cobertura da Ajuda
- Erro reportado pelo usuário: `tests/help_coverage.py` falhou porque o botão `Permitir site` não era citado literalmente na Ajuda.
- Atualizado `MainWindow.Help.cs` para documentar o rótulo exato `Permitir site`, seu alias `Permitir neste site` e os passos para criar exceção persistente e recarregar a página.
- A validação local do conteúdo atual encontrou zero rótulos ausentes na comparação textual executada nesta sessão. O resultado oficial será o workflow do GitHub após o commit.
- Commit da correção: `4e70f3846631deac46ba686082e4f183516ccbbb`.
- O build completo ainda não foi declarado pronto. O próximo passo é conferir o workflow acionado por esse commit e corrigir qualquer falha antes de solicitar teste manual.

## Observação funcional importante sobre Biblioteca Offline
- Inspeção do código confirmou que `ImportAudio_Click` abre `OpenFileDialog` para escolher um arquivo-fonte MP3/WAV e depois copia o arquivo para `%LOCALAPPDATA%/Lyravoxen/Media`, cria `LibraryVideo` offline e chama `SaveLibrary`. Esse seletor é de origem do arquivo, não de destino. O fluxo de URL direta ainda não existe na Biblioteca; `AddCurrentVideoToFolder_Click` guarda apenas link/ID e letra, sem baixar mídia.
- Em Pedidos, `ImportOfflineForRequest` também abre seletor de arquivo-fonte, copia para a pasta interna e associa ao pedido; não é download da URL do pedido.
- Para atender ao pedido do usuário, o pacote deve tornar explícito o fluxo: URL direta autorizada = download automático; arquivo local = selecionar origem e copiar automaticamente para a biblioteca interna; link YouTube isolado = não oferecer falso download nem extrair áudio.
