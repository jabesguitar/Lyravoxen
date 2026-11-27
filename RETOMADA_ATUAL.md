# ESTADO ATUAL — RETOMADA PRIORITÁRIA (27/11/2026)

> Leia esta seção antes de continuar. Resumo do estado real; histórico abaixo preservado.

## Decisão atual do usuário
A frente de bloqueio de anúncios do YouTube está **PAUSADA**. O usuário precisa avançar para outras funcionalidades e não quer mais consumir tempo com compilações sem evidência de eficácia. Não prometer remoção de anúncios do YouTube.

## Repositório
- https://github.com/jabesguitar/Lyravoxen
- Branch: `feature/offline-library-on-redesign`
- `main` intacta.
- Build #901 passou, mas não valida eficácia de anúncios.

## Frente de anúncios — estado
- Tentativas feitas: bloqueador por domínios/listas, filtros de endpoints, protótipo Brave adblock-rust e extensão uBlock Origin Lite no WebView2.
- Testes do usuário confirmaram anúncios patrocinados em vídeo e na lateral; objetivo não alcançado.
- Não há garantia técnica de que uBlock Lite ou botão de controle remova anúncios in-stream/first-party do YouTube.
- Ideia sugerida pelo usuário: botão redondo uBlock abaixo do controle de tema na aba Próxima música.
- Um botão pode controlar habilitar/desabilitar uma extensão que esteja instalada no WebView2, mas isso não equivale a bloquear anúncios. **Não implementar como indicador de proteção eficaz.**
- Nenhum botão foi implementado nesta decisão.
- Preservar código experimental; não apagar ou reverter sem necessidade.
- Estado: PAUSADO até existir solução com evidência técnica suficiente.

## Outras implementações existentes
- Biblioteca Offline com pasta configurável e subpastas Musicas/Letras/Dados/Temporarios.
- Arquivo independente de letras/sincronizações e recuperação por ID/URL.
- Integração de armazenamento com Pedidos; importação MP3/WAV e fluxo para URL direta autorizada.
- Retenção configurável 5/7/14/30 dias.
- Barra global de reprodução offline e seleção visual em listas.
- Redesign de Pedidos recebidos e Controle pelo celular.
- Ajustes no painel Próxima música e lista da Biblioteca; alguns cenários ainda dependem de validação manual consolidada.

## Próxima ação
Perguntar/seguir a próxima funcionalidade do Lyravoxen que o usuário deseja priorizar. Não voltar à frente de anúncios por iniciativa própria. Manter `MEMORIA_CONTINUA.md` e `RETOMADA_ATUAL.md` atualizados após mudanças significativas.

## Regras
- Não pedir provas já fornecidas.
- Distinguir implementado, compilado, testado e aprovado.
- Não entregar builds intermediários como se fossem finais.
- Não alterar `main` sem autorização.
- Preservar o histórico abaixo desta seção.

---
---

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


## Continuação executada — 26/11/2026

### Estado atual da implementação
- Branch preservada: `feature/offline-library-on-redesign`. A `main` não foi alterada.
- Criado o projeto compartilhado `src/Lyravoxen.Offline`, sem dependência de WPF, para permitir testes funcionais multiplataforma.
- Implementado `OfflineMediaService` para download de URLs diretas autorizadas e importação local MP3/WAV:
  - limite padrão de 50 MB, timeout de 45 segundos, cancelamento e remoção de arquivos temporários após falha;
  - validação HTTP/HTTPS, MIME quando informado, extensão e assinatura MP3/WAV;
  - validação DNS/IP público, bloqueio de redes privadas e redirecionamentos limitados; conexão usa o IP público validado para reduzir risco de DNS rebinding;
  - hash SHA-256 e deduplicação por conteúdo;
  - links do YouTube são recusados como fonte de arquivo de áudio.
- Aba Pedidos: URLs diretas de áudio são baixadas; links somente do YouTube explicam a limitação e oferecem seleção de arquivo autorizado local. A cópia é associada ao pedido e à Biblioteca.
- Biblioteca: aceita URL direta ou importação local. Adicionado editor de título, intérprete e letra na própria aba Biblioteca, sem depender da aba Cadastrar letras. A sincronização existente é preservada.
- Pastas distintas podem referenciar a mesma mídia deduplicada sem duplicar os bytes.
- Expiração: limpeza a cada minuto e na inicialização. O arquivo só é excluído quando não há outra referência ativa na Biblioteca ou em Pedidos; fichas, letras e metadados permanecem.
- Aba Próxima música: painel de comandos reorganizado em layout compacto, mantendo a coluna do navegador/WebView2.
- Proteção: política de bloqueio extraída para componente puro e testável. Os domínios protegidos do YouTube/Googlevideo permanecem permitidos; domínios de anúncios podem ser bloqueados e exceções do usuário são respeitadas.

### Testes automatizados adicionados
Projeto: `tests/Lyravoxen.OfflineTests`.
- Importação local WAV e MP3.
- Download direto WAV e MP3 com respostas HTTP controladas.
- Hash/deduplicação e preservação dos bytes.
- Recusa de YouTube como fonte de áudio.
- Bloqueio de DNS privado e redirecionamento para rede privada.
- Redirecionamento HTTPS permitido, limite de tamanho e recusa de HTML.
- Política do navegador: preservar YouTube/googlevideo, bloquear domínio de anúncio, respeitar exceções e evitar correspondência parcial.
- Expiração: preservar arquivo ainda referenciado por outra pasta ou pedido e permitir excluir quando não houver referências.

### Evidência de CI
- Run `45888141360`, commit `1c888e98`: job `cloud-worker-tests` concluído com sucesso. Serviço offline, cobertura da Ajuda, C# static checks, testes do Worker e página remota mobile aprovados. O teste offline nessa revisão cobria os fluxos de WAV, URL direta, redirecionamento, limite de tamanho e política do navegador; ainda não incluía os testes de expiração e MP3 acrescentados depois.
- Run `45887612874`, commit `5003f831`: jobs `cloud-worker-tests` e `build` concluídos com sucesso. A compilação Windows confirmou a integração inicial do serviço com Pedidos/Biblioteca e o timer de expiração, mas ocorreu antes do editor da Biblioteca e da compactação final da aba Próxima música. Não é a compilação final desta rodada.
- Run `45888415155`, commit `2b9f8418`: no último acompanhamento, o teste offline, cobertura da Ajuda, C# static checks e testes do Worker estavam aprovados; Playwright / teste mobile ainda estava em execução. Esse run contém os testes adicionais de MP3 e política de expiração. Aguardar conclusão antes de liberar build.

### Limitações que ainda impedem declarar pronto
- Os testes de mídia usam arquivos sintéticos e handlers HTTP controlados; ainda não comprovam reprodução real de MP3/WAV pelo MediaElement do Windows nem download contra diferentes servidores externos.
- A política do bloqueador está testada, mas ainda falta teste de integração com WebView2 real confirmando que a pesquisa, login e reprodução do YouTube permanecem funcionais e que a exceção “Permitir site” funciona em navegação real.
- A nova disposição da aba Próxima música foi validada por XAML/build, mas ainda requer inspeção visual em uma janela real e em diferentes resoluções.
- A execução Windows `45887612874` é evidência de build intermediário, não da versão final com o editor e o layout compactado.
- O workflow contém cron diário `17 10 * * *`, mas GitHub Actions executa `schedule` somente a partir da branch padrão. Como não foi autorizado merge na `main`, a rotina diária ainda não está ativa para esta branch. Os testes continuam sendo executados a cada push e podem ser acionados manualmente pela interface do GitHub.

### Próximas ações
1. Aguardar o término do run `45888415155` e conferir os logs completos dos 21 testes funcionais.
2. Fazer uma compilação Windows consolidada com todas as alterações atuais; corrigir qualquer falha de build e repetir.
3. Atualizar este registro com o SHA final, os jobs e os artefatos reais.
4. Só então preparar a única compilação para a rodada de testes do usuário, com checklist específico de navegação real, reprodução offline, expiração e editor da Biblioteca.
5. Não fazer merge na `main` sem aprovação explícita.


## Resultado da compilação consolidada — 26/11/2026

<badge color="success">CI e build Windows aprovados</badge>

- Workflow: [45889091270](https://github.com/jabesguitar/Lyravoxen/actions/runs/45889091270)
- SHA validado: `76e1f018a9e84c65505de05064eb50534ae0cd86`
- Branch: `feature/offline-library-on-redesign`
- Job `cloud-worker-tests`: sucesso.
  - 21 verificações do serviço offline e políticas de navegador/expiração.
  - Cobertura da Ajuda: sucesso.
  - C# static checks: sucesso.
  - Cloudflare Worker tests: sucesso.
  - Playwright Chromium / mobile remote page: sucesso.
- Job `build`: sucesso.
  - Geração de ícone e splash compacta: sucesso.
  - Restore e `dotnet publish` Windows x64: sucesso.
  - Cue timeline tests: sucesso.
  - ZIP portátil: sucesso.
  - Instalador Inno Setup: sucesso.
  - Upload dos três artefatos: sucesso.

### Artefatos desta compilação
- `Lyravoxen-Windows-x64` — artifact ID `15150797165` — 76,260,202 bytes.
- `Lyravoxen-Windows-x64-portable` — artifact ID `15150578815` — 76,294,856 bytes.
- `Lyravoxen-Installer-Windows-x64` — artifact ID `15150673234` — 53,834,443 bytes.
- Os três artefatos constavam como disponíveis e não expirados no momento da consulta.

### Correção realizada durante a compilação
- A primeira tentativa completa falhou por uma referência ambígua a `HorizontalAlignment.Right` no editor da Biblioteca.
- Corrigido para `System.Windows.HorizontalAlignment.Right`.
- A compilação seguinte no SHA acima passou por completo.

### Situação para a rodada única do usuário
- A compilação Windows consolidada foi gerada e está disponível nos artefatos do workflow.
- Ainda não há comprovação manual em um PC com WebView2 real de anúncios, pesquisa/login/reprodução do YouTube; a política de bloqueio foi testada isoladamente.
- A reprodução real de MP3/WAV offline e o comportamento visual do editor/layout em diferentes resoluções também dependem da validação manual no computador anfitrião.
- Não fazer merge na `main` sem aprovação explícita.
- Rotina diária: cron está no workflow da branch, mas GitHub Actions só dispara `schedule` na branch padrão. Como a `main` permanece intacta, os testes diários ainda não estão ativos; o CI roda a cada push e pode ser iniciado manualmente pela interface do GitHub.

### Próxima ação
Entregar ao usuário esta única compilação consolidada e o checklist manual final. Se os testes manuais forem aprovados, registrar a aprovação antes de considerar merge na `main`.


## Revisão corretiva — Pedidos, Biblioteca Offline e anúncios — 26/11/2026

### Solicitações do usuário
- A aba Próxima música ainda mostra anúncios no início do vídeo.
- Pedidos e Biblioteca Offline não estavam concluindo o fluxo esperado para links do YouTube; o usuário relatou dificuldade em baixar, salvar em pasta e renomear/organizar músicas.
- Exibir IP/endereço de origem de quem usa o formulário público, com bloqueio e desbloqueio para reduzir spam/sabotagem.
- Fazer uma única revisão ampla e uma única rodada manual, sem builds vazios intermediários.

### Limites técnicos e de plataforma
- Não implementar extração de áudio do YouTube nem contorno de anúncios/restrições. O player incorporado pode exibir anúncios servidos pela própria origem do YouTube; o bloqueador de domínios de terceiros não garante removê-los.
- Para offline, são suportados: arquivo MP3/WAV local autorizado ou URL HTTPS direta que entregue o arquivo de áudio. URL de página do YouTube não é URL direta de áudio.
- MAC não é disponibilizado ao servidor quando alguém acessa um site pela internet; o Worker pode registrar o IP público fornecido pelo Cloudflare. Não exibir campo MAC fictício.
- O código do Worker foi atualizado na branch, mas o serviço em produção `lyravoxen-pedidos.ferkt319.workers.dev` não foi implantado por esta sessão: não há conexão autorizada com a conta Cloudflare. Para ativar IP/rate-limit na URL pública, é necessária uma implantação única do novo código pelo painel Cloudflare. Pedidos antigos, criados antes do Worker novo, podem não ter IP.

### Implementações desta rodada
- Worker Cloudflare:
  - captura e armazena IP público `CF-Connecting-IP` junto com texto e horário do pedido;
  - bloqueio/desbloqueio autenticado por IP via `/bloqueios`;
  - limite básico de 5 envios por IP por janela de 10 minutos, além da fila global de até 150 pedidos;
  - aviso no formulário sobre registro do IP público e horário para prevenção de abuso, esclarecendo que MAC não é coletado;
  - documentação orienta a atualizar o Worker existente sem recriar KV ou trocar `ADMIN_TOKEN`.
  - KV é eventualmente consistente; o rate-limit no Worker é proteção básica. Para ataques concorrentes/coordenados, complementar com regra Rate Limiting no Cloudflare.
- App Pedidos:
  - sincroniza a lista de bloqueios remotos quando abre a janela;
  - exibe IP público por pedido quando o Worker atualizado o fornece;
  - oferece Bloquear este IP / Desbloquear este IP e Atualizar bloqueios da internet;
  - se IP vier vazio, avisa que o Worker público pode estar desatualizado;
  - normaliza e valida endereços IP ao cadastrar bloqueios.
- Biblioteca:
  - importar URL direta que falhe agora oferece selecionar arquivo MP3/WAV local;
  - colar URL do YouTube apresenta explicação e abre diretamente o seletor de arquivo autorizado;
  - botão de importação continua aceitando múltiplos MP3/WAV;
  - seleção de destino ao salvar pedido offline; não fixa tudo numa pasta genérica;
  - novos comandos Mover para outra pasta e Remover desta pasta (o segundo preserva o arquivo de áudio);
  - seletor permite criar nova pasta;
  - busca inclui título, intérprete e compositor;
  - renomear pasta valida nomes duplicados;
  - adicionar vídeo atual à pasta usa identidade exata (ID do vídeo ou caminho da mídia offline), preservando metadados;
  - SaveLibrary agora retorna sucesso/falha; operações críticas não exibem sucesso falso quando a persistência falha.
- Ajuda atualizada para os novos botões e comportamento.

### Validação final
- Workflow: [45894030900](https://github.com/jabesguitar/Lyravoxen/actions/runs/45894030900)
- SHA: `4f39f92562b6eb470b16be1d3513e9d1ecbe8543`
- Branch: `feature/offline-library-on-redesign`
- Job `cloud-worker-tests`: sucesso.
  - Serviço offline: 21 verificações aprovadas (MP3/WAV, URL direta, deduplicação, SSRF, redirecionamentos, limite de tamanho, política de anúncios e expiração compartilhada).
  - Worker: 44 testes aprovados, incluindo bloqueio/desbloqueio por IP e rate-limit.
  - Ajuda: cobertura aprovada.
  - C# static checks: aprovados.
  - Formulário público em Chromium: 25 testes aprovados.
  - Página mobile em Chromium: 112 testes aprovados.
- Job `build`: sucesso.
  - Ícone e splash: sucesso.
  - Restore e `dotnet publish` Windows x64: sucesso.
  - Cue timeline tests: sucesso.
  - ZIP portátil: sucesso.
  - Instalador Inno Setup: sucesso.
  - Upload dos três artefatos: sucesso.
- Artefatos gerados:
  - `Lyravoxen-Windows-x64`: ID `15153435863`, 76,266,769 bytes.
  - `Lyravoxen-Windows-x64-portable`: ID `15153276186`, 76,302,238 bytes.
  - `Lyravoxen-Installer-Windows-x64`: ID `15153231348`, 53,845,674 bytes.
  - Expiração prevista dos artefatos: 24/02/2027.
- `main` continua no SHA `2051135a6e4915c13dda5730fa0248ed7dbcfe2f`; não foi alterada nem recebeu merge.

### Limitações ainda abertas
- Anúncios in-stream/first-party do YouTube continuam fora do alcance garantido do bloqueador; não prometer reprodução YouTube sem anúncios.
- O Worker em produção precisa ser atualizado manualmente uma única vez para IP e bloqueios remotos aparecerem nos pedidos futuros.
- Testes automatizados de mídia usam fixtures sintéticas e servidor HTTP controlado; reprodução real via WebView2/MediaElement e navegação real no PC anfitrião ainda exigem validação do usuário.
- O pacote está pronto para uma única rodada manual no PC anfitrião; não pedir testes de builds intermediários.

### Próxima ação
1. Usuário instala a versão consolidada.
2. Uma única implantação do Worker atualizado no Cloudflare, mantendo namespace KV e segredo existentes.
3. Uma única rodada manual com MP3/WAV real, salvar/mover/renomear, pedido público com IP e navegação YouTube.
4. Registrar os resultados antes de qualquer merge na `main`.

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

### Regra vigente
Trabalhar somente em IP/bloqueios, download/Biblioteca Offline e proteção de anúncios na aba Próxima Música. Não alterar o que já funciona, não fazer redesign e não tocar na branch `main`. Esta regra mais recente prevalece sobre pendências antigas de redesign registradas acima.

### Alteração aplicada
- Commit `78e88d6942a578ac4491b94b577fe9185c2b3211`, arquivo `src/Lyravoxen.Desktop/MainWindow.Requests.cs`.
- Para pedidos com URL do YouTube, o botão **Baixar offline** agora explica que o link não é arquivo MP3/WAV baixável diretamente e pergunta se o operador deseja associar um arquivo autorizado que já possua.
- Se responder Não, não abre seletor. Se responder Sim, mantém o fluxo existente de importação/associação ao pedido e Biblioteca.
- URLs não-YouTube continuam tentando download direto. Não houve extração de áudio nem alteração do player.

### Verificações
- Branch confirmada: `feature/offline-library-on-redesign`; comparação com `main`: 222 commits à frente, 0 atrás. Nenhum merge foi feito.
- Build #810 passou antes desta alteração; não conta como validação do commit novo.
- Push nesta branch executa `cloud-worker-tests`; o job Windows `build` só executa em `workflow_dispatch` ou na `main`. Workflow não foi alterado.

### Pendências / próxima ação
- [ ] Conferir CI acionado pelo commit `78e88d6`.
- [ ] Após CI verde, executar build Windows consolidado via workflow manual.
- [ ] Atualizar Worker existente no Cloudflare mantendo KV `PEDIDOS` e secret `ADMIN_TOKEN`; testar IP/bloqueio/desbloqueio em produção.
- [ ] Testar reprodução offline real da cópia autorizada exata.
- [ ] Manter explícito que anúncios first-party/in-stream do YouTube não podem ser garantidamente removidos sem comprometer o player.
- Continuar auditoria da Biblioteca e retenção sem reescrever a limpeza já existente.


## 27/11/2026 — Build consolidado #822 concluído; iniciar rodada única de testes

### Build e CI
- Workflow [Windows build #822](https://github.com/jabesguitar/Lyravoxen/actions/runs/45942283747) concluído com sucesso no commit `eeb1f78d433fe09d46a7448b91023ef3ef256c04`.
- `cloud-worker-tests`: todos os passos passaram, incluindo mídia offline, cobertura da Ajuda, C# estático, Worker, formulário público e página remota móvel.
- `build`: Restore, publish Windows x64, teste da linha do tempo, ZIP portátil, instalador Inno Setup e uploads passaram.
- Correção aplicada após log do usuário: `using System.IO;` nos arquivos `YoutubeAudioDownloadService.cs`, `PermanentLyricsArchive.cs` e `MainWindow.OfflineStorage.cs`.

### Artefatos disponíveis (expiração 25/02/2027)
- Instalador Windows x64 — artifact ID `15173087837`; SHA-256 `98203e478aed295ab6d7b5ed61da3b6462a0d2e20a7486536cde002a2ff03bad`.
- Pasta Windows x64 — artifact ID `15172753278`; SHA-256 `f1dbf9e007935adcdedfb3be252766572f865de75e4e07e57045afba59cd3c12`.
- ZIP portátil — artifact ID `15172354465`; SHA-256 `680ce96f6fd6322f62a574fb9fc832e81fd5946367f7b7291360d84a2e0a838e`.

### Instrução para a rodada manual consolidada
1. Abrir a execução #822 e, na seção Artifacts, baixar `Lyravoxen-Installer-Windows-x64`.
2. Extrair o ZIP baixado e executar `Lyravoxen-Setup-Windows-x64.exe`. Fechar o Lyravoxen antes de instalar. Não precisa baixar os três pacotes; o instalador é o recomendado.
3. Executar a lista abaixo sem apagar a biblioteca atual nem dados anteriores.

### Checklist de testes do usuário
- [ ] Abrir o aplicativo e confirmar que inicia normalmente, sem erro ou tela vazia.
- [ ] Configuração de armazenamento: abrir a opção de pasta da Biblioteca Offline; escolher/criar uma pasta dedicada; confirmar que o app cria `Musicas`, `Letras`, `Dados` e `Temporarios` dentro dela.
- [ ] Cadastro de letra: abrir uma música com letra e cues, salvar, fechar e reabrir; confirmar que letra e sincronização continuam registradas.
- [ ] Arquivo de áudio: associar/importar um MP3 ou WAV autorizado; confirmar que aparece na Biblioteca Offline e reproduz.
- [ ] Persistência: remover somente a cópia de áudio (não apagar a ficha/letra pelo app, se houver essa opção); baixar/importar novamente a mesma gravação; confirmar que a letra continua vinculada.
- [ ] Pedidos recebidos: abrir um pedido e clicar em **Baixar offline**; confirmar a resposta apresentada e que o arquivo fica na pasta configurada. Para link YouTube, anotar a mensagem/resultado exato; não considerar download concluído se não houver arquivo reproduzível.
- [ ] Preparar próxima música: selecionar a cópia offline vinculada ao pedido; desligar a internet; tocar a próxima e confirmar reprodução local da gravação exata.
- [ ] Retenção: conferir a validade selecionada e que a limpeza não apaga letras/metadados; não alterar a data do sistema para simular expiração.
- [ ] Próxima Música: navegar, pesquisar e reproduzir; confirmar que o player continua funcionando com proteção ligada.
- [ ] Proteção de anúncios: registrar se anúncios/trackers são bloqueados; anúncios in-stream do próprio YouTube podem continuar aparecendo e não devem ser considerados falha se o player permanecer funcional.
- [ ] Pedidos pela internet: verificar recebimento dos pedidos e se o IP aparece; conferir bloqueio/desbloqueio somente após atualização do Worker em produção.

### Limites e pendências
- O build #822 prova compilação e testes automatizados, não prova download real do YouTube, reprodução offline no PC nem publicação do Worker Cloudflare em produção.
- O Worker existente ainda precisa receber o código atualizado pelo usuário no painel Cloudflare, preservando binding KV `PEDIDOS` e secret `ADMIN_TOKEN`; não criar outro Worker nem apagar recursos.
- Não fazer merge na `main`. Branch de teste permanece `feature/offline-library-on-redesign`.
- Após o usuário executar os testes, registrar cada resultado (aprovado/falhou/não testado) e corrigir somente o que falhar, sem repetir o checklist inteiro desnecessariamente.


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


## Situação atual — 27/11/2026 (Build #855)

- Build #855 concluído com sucesso: https://github.com/jabesguitar/Lyravoxen/actions/runs/46018345034
- Instalador Windows x64 disponível no artefato `Lyravoxen-Installer-Windows-x64` (ID 15207121706). ZIP portátil: `Lyravoxen-Windows-x64-portable` (ID 15207491300). Artefatos expiram em 25/02/2027.
- Branch: `feature/offline-library-on-redesign`; `main` não foi alterada.
- Próxima etapa: instalar o artefato do build #855 e validar em execução real: (1) música offline continua ao trocar de abas; (2) pausar/retomar/parar/volume/posição; (3) seleção destacada no histórico e na Biblioteca; (4) link direto MP3/WAV, escolha de destino e registro na coleção; (5) retenção e preservação da letra/dados.
- Ainda pendente: publicação/validação do Worker Cloudflare em produção. Os testes automatizados do código passaram, mas produção não foi atualizada por esta execução.
- Não fazer merge em `main` sem autorização expressa.


## Atualização — 27/11/2026: lista da Biblioteca e janela de pedidos

- Corrigida a troca de pastas para limpar o filtro antigo antes de carregar as músicas da coleção selecionada.
- Comandos inferiores da coluna Músicas da coleção limitados em altura com rolagem própria para não ocultar a lista de músicas.
- Janela Pedidos recebidos redesenhada: janela maior, cabeçalho/contador, configurações recolhidas, cards por pedido com conjunto/cantor, título, link e botões de ação legíveis. Botão Copiar link removido dos cards; bloqueio de IP permanece secundário.
- Build #862: https://github.com/jabesguitar/Lyravoxen/actions/runs/46025648719 (em andamento no momento deste registro).
- Próxima etapa: aguardar #862. Se verde, baixar instalador e validar: trocar entre pastas com e sem filtro; conferir lista visível; abrir Pedidos recebidos e conferir cards, texto e botões. Não mexer na main.


## Atualização — 27/11/2026: redesign Controle pelo celular

- Janela Controle pelo celular redesenhada para combinar com Pedidos recebidos: fundo azul-escuro, cartões, QR destacado, endereço legível/copiar, PIN em destaque e botão Gerar novo PIN. Mantidos QR, endereços, PIN e instruções existentes.
- Commit: c4b7796e38dd502c4a276a603987dc0c331be18a.
- Build #866: https://github.com/jabesguitar/Lyravoxen/actions/runs/46029745807 (em andamento).
- Próxima ação: aguardar build. Se verde, baixar instalador único e validar visualmente as telas Biblioteca Offline, Pedidos recebidos e Controle pelo celular. Branch feature/offline-library-on-redesign; não alterar main.

- Observação operacional: o push de documentação cancelou a execução concorrente #866; acompanhar a execução mais recente disparada pelo commit final de documentação antes de liberar o instalador.


## Atualização — 27/11/2026: filtro conservador de endpoints de anúncios YouTube

- Implementada política de requisições que bloqueia caminhos conhecidos `/pagead/` e `/api/stats/ads` em `youtube.com` e subdomínios, além da lista de hosts já existente.
- Preservados API IFrame, páginas normais do YouTube, `googlevideo.com` e exceções de domínio permitidas pelo usuário.
- Testes adicionados para confirmar bloqueio desses endpoints e preservação dos recursos de reprodução.
- Commits: `c026684` (política), `b27d130` (integração WebView2), `c554d1c` (testes).
- Build #870 passou antes dessas alterações, mas não valida os commits novos. Acompanhar a execução disparada pelo commit mais recente.
- Próxima ação: confirmar workflow verde; instalar/abrir o app e testar vários vídeos na aba Próxima música, incluindo início, reprodução contínua, troca de vídeo e carregamento de letras. Registrar se anúncios pré-roll ainda aparecem ou se algum vídeo/player falha.
- Limite conhecido: não é bloqueio garantido de anúncios in-stream/servidos dinamicamente; nenhuma alteração no player principal foi feita.
- Branch `feature/offline-library-on-redesign`; `main` permanece intocada.


## Atualização — 27/11/2026: correção CS0103 BrowserAdBlocker

- Log recebido mostrou nove erros CS0103: `BrowserAdBlocker` não existia no contexto de `MainWindow.xaml.cs`.
- Causa: `BrowserAdBlocker.cs` estava com namespace/classe errados (conteúdo de `BrowserRequestPolicy`), removendo o serviço esperado.
- Serviço restaurado em `Lyravoxen.Services`, com integração ao novo filtro de endpoints YouTube preservada.
- Commit: `bfebc4b14ebba8deea3801cf63e7295d447b53d9`.
- Build #876: https://github.com/jabesguitar/Lyravoxen/actions/runs/46034132906 (em andamento ao registrar).
- Próxima ação: aguardar #876 e, se falhar, abrir logs do job build e corrigir a próxima falha. Avisos CS8602/CS4014 permanecem não fatais e fora do escopo desta correção.
- Branch feature/offline-library-on-redesign; main intocada.


## Atualização — 27/11/2026: teste real confirmou anúncios ainda presentes

- Usuário testou a compilação e relatou dois anúncios já na primeira música reproduzida na aba Próxima música.
- Conclusão: filtro pontual por caminhos de anúncio não resolveu pré-roll/in-stream; funcionalidade ainda não atende ao objetivo.
- Build #878 passou: https://github.com/jabesguitar/Lyravoxen/actions/runs/46034192649. Build verde não comprova bloqueio de anúncios.
- Pesquisa de alternativa: estudar integração do motor upstream `brave/adblock-rust` (MPL-2.0), que oferece filtro ABP, bloqueio de rede, cosmético e scriptlets. Não copiar o wrapper ZWebWrap (GPL-3.0-or-later) como um todo.
- Próxima ação: fazer protótipo isolado da integração Rust/C ABI + C# P/Invoke/WebView2; medir compatibilidade, build e impacto em CPUs antigas antes de conectar ao player. Criar testes com URLs/tipos de recurso e testes manuais com YouTube.
- Limite: nem motor ABP/uBO garante identificar anúncios inseridos no mesmo fluxo de mídia do vídeo.
- Nenhuma alteração adicional no código após o teste do usuário. Não mexer na main.


## Atualização — 27/11/2026: correção CS0117 WebSocket

- Build #886 falhou porque o enum WebView2 1.0.2903.40 não define `CoreWebView2WebResourceContext.WebSocket`.
- Removido esse case; os demais tipos de requisição permanecem e recursos sem mapeamento usam `other`.
- Commit: `db889fbb79c6e292a21b9505259e4f22f8256e76`.
- Acompanhar novo workflow. Próximo objetivo: validar compilação do Rust e .NET, DLL nativa incluída na publicação e testes; não considerar anúncios resolvidos antes de teste real.


## Atualização — 27/11/2026: teste visual falhou

- Duas capturas enviadas pelo usuário mostram anúncios em vídeo Pantene e Mercado Livre, além de anúncios patrocinados laterais na aba Próxima música.
- Conclusão: proteção ainda não atende ao requisito. Não solicitar novas provas.
- O motor ABP em implementação não basta por si só: falta validar carregamento nativo em runtime e avaliar filtros cosméticos/scriptlets para interface; anúncios in-stream são um problema separado e não há garantia de bloqueio por URL.
- Próxima ação: conferir build mais recente e a inicialização efetiva da DLL/listas; depois desenhar uma abordagem separada para elementos patrocinados da página e anúncios dentro do player. Testar antes de pedir novo teste ao usuário.
- Não apresentar a proteção como concluída nem entregar versão como solução. Branch feature/offline-library-on-redesign; main intocada.


## Atualização — 27/11/2026: protótipo com extensão open source no WebView2

- Mudança de abordagem após evidência visual de anúncios: integrar extensão pronta, em vez de insistir apenas no motor próprio.
- Extensão escolhida: uBlock Origin Lite (Edge), release 2026.1124.1811, GPL-3.0, distribuída sem modificações.
- Workflow baixa a versão oficial, extrai a pasta com manifest.json, inclui no pacote Windows e obtém o LICENSE.
- Inicialização WebView2 corrigida para criar CoreWebView2Environment com AreBrowserExtensionsEnabled=true, perfil persistente LocalAppData/Lyravoxen/WebView2 e AddBrowserExtensionAsync.
- A revisão com essa integração ainda precisa de build; build #891 verde refere-se ao protótipo Rust anterior e não valida extensão.
- Próximo passo: validar compilação e bundle; depois confirmar em runtime que a extensão aparece em GetBrowserExtensionsAsync/IsEnabled e testar filtros de página. Atenção: API de extensão existe no WebView2, mas não há ainda confirmação de compatibilidade integral das APIs usadas pelo uBOL.
- Branch feature/offline-library-on-redesign; main intocada. Não solicitar ao usuário que prove novamente presença de anúncios.


## Atualização — 27/11/2026: teste real do uBlock Origin Lite não funcionou

- Usuário testou build #897 e a captura mostra anúncio em vídeo "Aproxime-se de Deus" (1 de 2), anúncio lateral e anúncio patrocinado; portanto resultado negativo.
- Build #897 compilou e empacotou, mas não comprova que a extensão foi ativada nem que suas regras foram aplicadas.
- Código atual chama AddBrowserExtensionAsync mas não confirma com GetBrowserExtensionsAsync, IsEnabled ou diagnóstico de regras. Status da extensão é sobrescrito depois pela mensagem "Player pronto". Painel "81.671 regras / 0 solicitações bloqueadas" mede apenas o bloqueador legado, não uBOL.
- Próxima ação: melhorar diagnóstico e instalação idempotente: consultar extensão já instalada, confirmar ID/nome/IsEnabled, evitar reinstalar, guardar versão/erros/WebView2 Runtime em log local e mostrar estado real no painel. Validar DNR separadamente antes de nova compilação para o usuário.
- Não pedir novas provas; usuário já mostrou anúncios em três capturas e quer avançar. Não declarar bloqueio resolvido.
- Manter este arquivo e MEMORIA_CONTINUA.md sempre atualizados. Branch feature/offline-library-on-redesign; main intocada.


## Atualização — 27/11/2026: nova captura confirma anúncios ainda presentes

- Nova captura do usuário na aba Próxima música, busca "harpa crista 15": anúncio em vídeo Amazon Prime/Blade Runner 2099, marcado "Patrocinado · 2 de 2", duração 0:06 e reprodução em 0:03/0:06; anúncio patrocinado lateral também visível.
- Conclusão: uBlock Origin Lite integrado não resolveu o problema. Não solicitar novas provas.
- Próxima etapa antes de nova compilação: instrumentar GetBrowserExtensionsAsync, confirmar ID/nome/versão/IsEnabled; instalar apenas quando ausente; registrar versão WebView2, caminho e HRESULTs; separar estado real da extensão do contador legado; validar DNR com fixture controlada. Se DNR funcionar e anúncios continuarem, estudar outra solução open source compatível em vez de repetir a mesma abordagem.
- Não declarar proteção eficaz nem pedir novo teste até termos diagnóstico técnico. Build #897 verde apenas para compilação/empacotamento; teste real falhou.
- Manter este arquivo e MEMORIA_CONTINUA.md atualizados. Branch feature/offline-library-on-redesign; main intocada.
# ESTADO ATUAL — RETOMADA PRIORITÁRIA (27/11/2026)

> Leia esta seção antes de continuar. Resumo do estado real; histórico abaixo preservado.

## Decisão atual do usuário
A frente de bloqueio de anúncios do YouTube está **PAUSADA**. O usuário precisa avançar para outras funcionalidades e não quer mais consumir tempo com compilações sem evidência de eficácia. Não prometer remoção de anúncios do YouTube.

## Repositório
- https://github.com/jabesguitar/Lyravoxen
- Branch: `feature/offline-library-on-redesign`
- `main` intacta.
- Build #901 passou, mas não valida eficácia de anúncios.

## Frente de anúncios — estado
- Tentativas feitas: bloqueador por domínios/listas, filtros de endpoints, protótipo Brave adblock-rust e extensão uBlock Origin Lite no WebView2.
- Testes do usuário confirmaram anúncios patrocinados em vídeo e na lateral; objetivo não alcançado.
- Não há garantia técnica de que uBlock Lite ou botão de controle remova anúncios in-stream/first-party do YouTube.
- Ideia sugerida pelo usuário: botão redondo uBlock abaixo do controle de tema na aba Próxima música.
- Um botão pode controlar habilitar/desabilitar uma extensão que esteja instalada no WebView2, mas isso não equivale a bloquear anúncios. **Não implementar como indicador de proteção eficaz.**
- Nenhum botão foi implementado nesta decisão.
- Preservar código experimental; não apagar ou reverter sem necessidade.
- Estado: PAUSADO até existir solução com evidência técnica suficiente.

## Outras implementações existentes
- Biblioteca Offline com pasta configurável e subpastas Musicas/Letras/Dados/Temporarios.
- Arquivo independente de letras/sincronizações e recuperação por ID/URL.
- Integração de armazenamento com Pedidos; importação MP3/WAV e fluxo para URL direta autorizada.
- Retenção configurável 5/7/14/30 dias.
- Barra global de reprodução offline e seleção visual em listas.
- Redesign de Pedidos recebidos e Controle pelo celular.
- Ajustes no painel Próxima música e lista da Biblioteca; alguns cenários ainda dependem de validação manual consolidada.

## Próxima ação
Perguntar/seguir a próxima funcionalidade do Lyravoxen que o usuário deseja priorizar. Não voltar à frente de anúncios por iniciativa própria. Manter `MEMORIA_CONTINUA.md` e `RETOMADA_ATUAL.md` atualizados após mudanças significativas.

## Regras
- Não pedir provas já fornecidas.
- Distinguir implementado, compilado, testado e aprovado.
- Não entregar builds intermediários como se fossem finais.
- Não alterar `main` sem autorização.
- Preservar o histórico abaixo desta seção.

---
---

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


## Continuação executada — 26/11/2026

### Estado atual da implementação
- Branch preservada: `feature/offline-library-on-redesign`. A `main` não foi alterada.
- Criado o projeto compartilhado `src/Lyravoxen.Offline`, sem dependência de WPF, para permitir testes funcionais multiplataforma.
- Implementado `OfflineMediaService` para download de URLs diretas autorizadas e importação local MP3/WAV:
  - limite padrão de 50 MB, timeout de 45 segundos, cancelamento e remoção de arquivos temporários após falha;
  - validação HTTP/HTTPS, MIME quando informado, extensão e assinatura MP3/WAV;
  - validação DNS/IP público, bloqueio de redes privadas e redirecionamentos limitados; conexão usa o IP público validado para reduzir risco de DNS rebinding;
  - hash SHA-256 e deduplicação por conteúdo;
  - links do YouTube são recusados como fonte de arquivo de áudio.
- Aba Pedidos: URLs diretas de áudio são baixadas; links somente do YouTube explicam a limitação e oferecem seleção de arquivo autorizado local. A cópia é associada ao pedido e à Biblioteca.
- Biblioteca: aceita URL direta ou importação local. Adicionado editor de título, intérprete e letra na própria aba Biblioteca, sem depender da aba Cadastrar letras. A sincronização existente é preservada.
- Pastas distintas podem referenciar a mesma mídia deduplicada sem duplicar os bytes.
- Expiração: limpeza a cada minuto e na inicialização. O arquivo só é excluído quando não há outra referência ativa na Biblioteca ou em Pedidos; fichas, letras e metadados permanecem.
- Aba Próxima música: painel de comandos reorganizado em layout compacto, mantendo a coluna do navegador/WebView2.
- Proteção: política de bloqueio extraída para componente puro e testável. Os domínios protegidos do YouTube/Googlevideo permanecem permitidos; domínios de anúncios podem ser bloqueados e exceções do usuário são respeitadas.

### Testes automatizados adicionados
Projeto: `tests/Lyravoxen.OfflineTests`.
- Importação local WAV e MP3.
- Download direto WAV e MP3 com respostas HTTP controladas.
- Hash/deduplicação e preservação dos bytes.
- Recusa de YouTube como fonte de áudio.
- Bloqueio de DNS privado e redirecionamento para rede privada.
- Redirecionamento HTTPS permitido, limite de tamanho e recusa de HTML.
- Política do navegador: preservar YouTube/googlevideo, bloquear domínio de anúncio, respeitar exceções e evitar correspondência parcial.
- Expiração: preservar arquivo ainda referenciado por outra pasta ou pedido e permitir excluir quando não houver referências.

### Evidência de CI
- Run `45888141360`, commit `1c888e98`: job `cloud-worker-tests` concluído com sucesso. Serviço offline, cobertura da Ajuda, C# static checks, testes do Worker e página remota mobile aprovados. O teste offline nessa revisão cobria os fluxos de WAV, URL direta, redirecionamento, limite de tamanho e política do navegador; ainda não incluía os testes de expiração e MP3 acrescentados depois.
- Run `45887612874`, commit `5003f831`: jobs `cloud-worker-tests` e `build` concluídos com sucesso. A compilação Windows confirmou a integração inicial do serviço com Pedidos/Biblioteca e o timer de expiração, mas ocorreu antes do editor da Biblioteca e da compactação final da aba Próxima música. Não é a compilação final desta rodada.
- Run `45888415155`, commit `2b9f8418`: no último acompanhamento, o teste offline, cobertura da Ajuda, C# static checks e testes do Worker estavam aprovados; Playwright / teste mobile ainda estava em execução. Esse run contém os testes adicionais de MP3 e política de expiração. Aguardar conclusão antes de liberar build.

### Limitações que ainda impedem declarar pronto
- Os testes de mídia usam arquivos sintéticos e handlers HTTP controlados; ainda não comprovam reprodução real de MP3/WAV pelo MediaElement do Windows nem download contra diferentes servidores externos.
- A política do bloqueador está testada, mas ainda falta teste de integração com WebView2 real confirmando que a pesquisa, login e reprodução do YouTube permanecem funcionais e que a exceção “Permitir site” funciona em navegação real.
- A nova disposição da aba Próxima música foi validada por XAML/build, mas ainda requer inspeção visual em uma janela real e em diferentes resoluções.
- A execução Windows `45887612874` é evidência de build intermediário, não da versão final com o editor e o layout compactado.
- O workflow contém cron diário `17 10 * * *`, mas GitHub Actions executa `schedule` somente a partir da branch padrão. Como não foi autorizado merge na `main`, a rotina diária ainda não está ativa para esta branch. Os testes continuam sendo executados a cada push e podem ser acionados manualmente pela interface do GitHub.

### Próximas ações
1. Aguardar o término do run `45888415155` e conferir os logs completos dos 21 testes funcionais.
2. Fazer uma compilação Windows consolidada com todas as alterações atuais; corrigir qualquer falha de build e repetir.
3. Atualizar este registro com o SHA final, os jobs e os artefatos reais.
4. Só então preparar a única compilação para a rodada de testes do usuário, com checklist específico de navegação real, reprodução offline, expiração e editor da Biblioteca.
5. Não fazer merge na `main` sem aprovação explícita.


## Resultado da compilação consolidada — 26/11/2026

<badge color="success">CI e build Windows aprovados</badge>

- Workflow: [45889091270](https://github.com/jabesguitar/Lyravoxen/actions/runs/45889091270)
- SHA validado: `76e1f018a9e84c65505de05064eb50534ae0cd86`
- Branch: `feature/offline-library-on-redesign`
- Job `cloud-worker-tests`: sucesso.
  - 21 verificações do serviço offline e políticas de navegador/expiração.
  - Cobertura da Ajuda: sucesso.
  - C# static checks: sucesso.
  - Cloudflare Worker tests: sucesso.
  - Playwright Chromium / mobile remote page: sucesso.
- Job `build`: sucesso.
  - Geração de ícone e splash compacta: sucesso.
  - Restore e `dotnet publish` Windows x64: sucesso.
  - Cue timeline tests: sucesso.
  - ZIP portátil: sucesso.
  - Instalador Inno Setup: sucesso.
  - Upload dos três artefatos: sucesso.

### Artefatos desta compilação
- `Lyravoxen-Windows-x64` — artifact ID `15150797165` — 76,260,202 bytes.
- `Lyravoxen-Windows-x64-portable` — artifact ID `15150578815` — 76,294,856 bytes.
- `Lyravoxen-Installer-Windows-x64` — artifact ID `15150673234` — 53,834,443 bytes.
- Os três artefatos constavam como disponíveis e não expirados no momento da consulta.

### Correção realizada durante a compilação
- A primeira tentativa completa falhou por uma referência ambígua a `HorizontalAlignment.Right` no editor da Biblioteca.
- Corrigido para `System.Windows.HorizontalAlignment.Right`.
- A compilação seguinte no SHA acima passou por completo.

### Situação para a rodada única do usuário
- A compilação Windows consolidada foi gerada e está disponível nos artefatos do workflow.
- Ainda não há comprovação manual em um PC com WebView2 real de anúncios, pesquisa/login/reprodução do YouTube; a política de bloqueio foi testada isoladamente.
- A reprodução real de MP3/WAV offline e o comportamento visual do editor/layout em diferentes resoluções também dependem da validação manual no computador anfitrião.
- Não fazer merge na `main` sem aprovação explícita.
- Rotina diária: cron está no workflow da branch, mas GitHub Actions só dispara `schedule` na branch padrão. Como a `main` permanece intacta, os testes diários ainda não estão ativos; o CI roda a cada push e pode ser iniciado manualmente pela interface do GitHub.

### Próxima ação
Entregar ao usuário esta única compilação consolidada e o checklist manual final. Se os testes manuais forem aprovados, registrar a aprovação antes de considerar merge na `main`.


## Revisão corretiva — Pedidos, Biblioteca Offline e anúncios — 26/11/2026

### Solicitações do usuário
- A aba Próxima música ainda mostra anúncios no início do vídeo.
- Pedidos e Biblioteca Offline não estavam concluindo o fluxo esperado para links do YouTube; o usuário relatou dificuldade em baixar, salvar em pasta e renomear/organizar músicas.
- Exibir IP/endereço de origem de quem usa o formulário público, com bloqueio e desbloqueio para reduzir spam/sabotagem.
- Fazer uma única revisão ampla e uma única rodada manual, sem builds vazios intermediários.

### Limites técnicos e de plataforma
- Não implementar extração de áudio do YouTube nem contorno de anúncios/restrições. O player incorporado pode exibir anúncios servidos pela própria origem do YouTube; o bloqueador de domínios de terceiros não garante removê-los.
- Para offline, são suportados: arquivo MP3/WAV local autorizado ou URL HTTPS direta que entregue o arquivo de áudio. URL de página do YouTube não é URL direta de áudio.
- MAC não é disponibilizado ao servidor quando alguém acessa um site pela internet; o Worker pode registrar o IP público fornecido pelo Cloudflare. Não exibir campo MAC fictício.
- O código do Worker foi atualizado na branch, mas o serviço em produção `lyravoxen-pedidos.ferkt319.workers.dev` não foi implantado por esta sessão: não há conexão autorizada com a conta Cloudflare. Para ativar IP/rate-limit na URL pública, é necessária uma implantação única do novo código pelo painel Cloudflare. Pedidos antigos, criados antes do Worker novo, podem não ter IP.

### Implementações desta rodada
- Worker Cloudflare:
  - captura e armazena IP público `CF-Connecting-IP` junto com texto e horário do pedido;
  - bloqueio/desbloqueio autenticado por IP via `/bloqueios`;
  - limite básico de 5 envios por IP por janela de 10 minutos, além da fila global de até 150 pedidos;
  - aviso no formulário sobre registro do IP público e horário para prevenção de abuso, esclarecendo que MAC não é coletado;
  - documentação orienta a atualizar o Worker existente sem recriar KV ou trocar `ADMIN_TOKEN`.
  - KV é eventualmente consistente; o rate-limit no Worker é proteção básica. Para ataques concorrentes/coordenados, complementar com regra Rate Limiting no Cloudflare.
- App Pedidos:
  - sincroniza a lista de bloqueios remotos quando abre a janela;
  - exibe IP público por pedido quando o Worker atualizado o fornece;
  - oferece Bloquear este IP / Desbloquear este IP e Atualizar bloqueios da internet;
  - se IP vier vazio, avisa que o Worker público pode estar desatualizado;
  - normaliza e valida endereços IP ao cadastrar bloqueios.
- Biblioteca:
  - importar URL direta que falhe agora oferece selecionar arquivo MP3/WAV local;
  - colar URL do YouTube apresenta explicação e abre diretamente o seletor de arquivo autorizado;
  - botão de importação continua aceitando múltiplos MP3/WAV;
  - seleção de destino ao salvar pedido offline; não fixa tudo numa pasta genérica;
  - novos comandos Mover para outra pasta e Remover desta pasta (o segundo preserva o arquivo de áudio);
  - seletor permite criar nova pasta;
  - busca inclui título, intérprete e compositor;
  - renomear pasta valida nomes duplicados;
  - adicionar vídeo atual à pasta usa identidade exata (ID do vídeo ou caminho da mídia offline), preservando metadados;
  - SaveLibrary agora retorna sucesso/falha; operações críticas não exibem sucesso falso quando a persistência falha.
- Ajuda atualizada para os novos botões e comportamento.

### Validação final
- Workflow: [45894030900](https://github.com/jabesguitar/Lyravoxen/actions/runs/45894030900)
- SHA: `4f39f92562b6eb470b16be1d3513e9d1ecbe8543`
- Branch: `feature/offline-library-on-redesign`
- Job `cloud-worker-tests`: sucesso.
  - Serviço offline: 21 verificações aprovadas (MP3/WAV, URL direta, deduplicação, SSRF, redirecionamentos, limite de tamanho, política de anúncios e expiração compartilhada).
  - Worker: 44 testes aprovados, incluindo bloqueio/desbloqueio por IP e rate-limit.
  - Ajuda: cobertura aprovada.
  - C# static checks: aprovados.
  - Formulário público em Chromium: 25 testes aprovados.
  - Página mobile em Chromium: 112 testes aprovados.
- Job `build`: sucesso.
  - Ícone e splash: sucesso.
  - Restore e `dotnet publish` Windows x64: sucesso.
  - Cue timeline tests: sucesso.
  - ZIP portátil: sucesso.
  - Instalador Inno Setup: sucesso.
  - Upload dos três artefatos: sucesso.
- Artefatos gerados:
  - `Lyravoxen-Windows-x64`: ID `15153435863`, 76,266,769 bytes.
  - `Lyravoxen-Windows-x64-portable`: ID `15153276186`, 76,302,238 bytes.
  - `Lyravoxen-Installer-Windows-x64`: ID `15153231348`, 53,845,674 bytes.
  - Expiração prevista dos artefatos: 24/02/2027.
- `main` continua no SHA `2051135a6e4915c13dda5730fa0248ed7dbcfe2f`; não foi alterada nem recebeu merge.

### Limitações ainda abertas
- Anúncios in-stream/first-party do YouTube continuam fora do alcance garantido do bloqueador; não prometer reprodução YouTube sem anúncios.
- O Worker em produção precisa ser atualizado manualmente uma única vez para IP e bloqueios remotos aparecerem nos pedidos futuros.
- Testes automatizados de mídia usam fixtures sintéticas e servidor HTTP controlado; reprodução real via WebView2/MediaElement e navegação real no PC anfitrião ainda exigem validação do usuário.
- O pacote está pronto para uma única rodada manual no PC anfitrião; não pedir testes de builds intermediários.

### Próxima ação
1. Usuário instala a versão consolidada.
2. Uma única implantação do Worker atualizado no Cloudflare, mantendo namespace KV e segredo existentes.
3. Uma única rodada manual com MP3/WAV real, salvar/mover/renomear, pedido público com IP e navegação YouTube.
4. Registrar os resultados antes de qualquer merge na `main`.

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

### Regra vigente
Trabalhar somente em IP/bloqueios, download/Biblioteca Offline e proteção de anúncios na aba Próxima Música. Não alterar o que já funciona, não fazer redesign e não tocar na branch `main`. Esta regra mais recente prevalece sobre pendências antigas de redesign registradas acima.

### Alteração aplicada
- Commit `78e88d6942a578ac4491b94b577fe9185c2b3211`, arquivo `src/Lyravoxen.Desktop/MainWindow.Requests.cs`.
- Para pedidos com URL do YouTube, o botão **Baixar offline** agora explica que o link não é arquivo MP3/WAV baixável diretamente e pergunta se o operador deseja associar um arquivo autorizado que já possua.
- Se responder Não, não abre seletor. Se responder Sim, mantém o fluxo existente de importação/associação ao pedido e Biblioteca.
- URLs não-YouTube continuam tentando download direto. Não houve extração de áudio nem alteração do player.

### Verificações
- Branch confirmada: `feature/offline-library-on-redesign`; comparação com `main`: 222 commits à frente, 0 atrás. Nenhum merge foi feito.
- Build #810 passou antes desta alteração; não conta como validação do commit novo.
- Push nesta branch executa `cloud-worker-tests`; o job Windows `build` só executa em `workflow_dispatch` ou na `main`. Workflow não foi alterado.

### Pendências / próxima ação
- [ ] Conferir CI acionado pelo commit `78e88d6`.
- [ ] Após CI verde, executar build Windows consolidado via workflow manual.
- [ ] Atualizar Worker existente no Cloudflare mantendo KV `PEDIDOS` e secret `ADMIN_TOKEN`; testar IP/bloqueio/desbloqueio em produção.
- [ ] Testar reprodução offline real da cópia autorizada exata.
- [ ] Manter explícito que anúncios first-party/in-stream do YouTube não podem ser garantidamente removidos sem comprometer o player.
- Continuar auditoria da Biblioteca e retenção sem reescrever a limpeza já existente.


## 27/11/2026 — Build consolidado #822 concluído; iniciar rodada única de testes

### Build e CI
- Workflow [Windows build #822](https://github.com/jabesguitar/Lyravoxen/actions/runs/45942283747) concluído com sucesso no commit `eeb1f78d433fe09d46a7448b91023ef3ef256c04`.
- `cloud-worker-tests`: todos os passos passaram, incluindo mídia offline, cobertura da Ajuda, C# estático, Worker, formulário público e página remota móvel.
- `build`: Restore, publish Windows x64, teste da linha do tempo, ZIP portátil, instalador Inno Setup e uploads passaram.
- Correção aplicada após log do usuário: `using System.IO;` nos arquivos `YoutubeAudioDownloadService.cs`, `PermanentLyricsArchive.cs` e `MainWindow.OfflineStorage.cs`.

### Artefatos disponíveis (expiração 25/02/2027)
- Instalador Windows x64 — artifact ID `15173087837`; SHA-256 `98203e478aed295ab6d7b5ed61da3b6462a0d2e20a7486536cde002a2ff03bad`.
- Pasta Windows x64 — artifact ID `15172753278`; SHA-256 `f1dbf9e007935adcdedfb3be252766572f865de75e4e07e57045afba59cd3c12`.
- ZIP portátil — artifact ID `15172354465`; SHA-256 `680ce96f6fd6322f62a574fb9fc832e81fd5946367f7b7291360d84a2e0a838e`.

### Instrução para a rodada manual consolidada
1. Abrir a execução #822 e, na seção Artifacts, baixar `Lyravoxen-Installer-Windows-x64`.
2. Extrair o ZIP baixado e executar `Lyravoxen-Setup-Windows-x64.exe`. Fechar o Lyravoxen antes de instalar. Não precisa baixar os três pacotes; o instalador é o recomendado.
3. Executar a lista abaixo sem apagar a biblioteca atual nem dados anteriores.

### Checklist de testes do usuário
- [ ] Abrir o aplicativo e confirmar que inicia normalmente, sem erro ou tela vazia.
- [ ] Configuração de armazenamento: abrir a opção de pasta da Biblioteca Offline; escolher/criar uma pasta dedicada; confirmar que o app cria `Musicas`, `Letras`, `Dados` e `Temporarios` dentro dela.
- [ ] Cadastro de letra: abrir uma música com letra e cues, salvar, fechar e reabrir; confirmar que letra e sincronização continuam registradas.
- [ ] Arquivo de áudio: associar/importar um MP3 ou WAV autorizado; confirmar que aparece na Biblioteca Offline e reproduz.
- [ ] Persistência: remover somente a cópia de áudio (não apagar a ficha/letra pelo app, se houver essa opção); baixar/importar novamente a mesma gravação; confirmar que a letra continua vinculada.
- [ ] Pedidos recebidos: abrir um pedido e clicar em **Baixar offline**; confirmar a resposta apresentada e que o arquivo fica na pasta configurada. Para link YouTube, anotar a mensagem/resultado exato; não considerar download concluído se não houver arquivo reproduzível.
- [ ] Preparar próxima música: selecionar a cópia offline vinculada ao pedido; desligar a internet; tocar a próxima e confirmar reprodução local da gravação exata.
- [ ] Retenção: conferir a validade selecionada e que a limpeza não apaga letras/metadados; não alterar a data do sistema para simular expiração.
- [ ] Próxima Música: navegar, pesquisar e reproduzir; confirmar que o player continua funcionando com proteção ligada.
- [ ] Proteção de anúncios: registrar se anúncios/trackers são bloqueados; anúncios in-stream do próprio YouTube podem continuar aparecendo e não devem ser considerados falha se o player permanecer funcional.
- [ ] Pedidos pela internet: verificar recebimento dos pedidos e se o IP aparece; conferir bloqueio/desbloqueio somente após atualização do Worker em produção.

### Limites e pendências
- O build #822 prova compilação e testes automatizados, não prova download real do YouTube, reprodução offline no PC nem publicação do Worker Cloudflare em produção.
- O Worker existente ainda precisa receber o código atualizado pelo usuário no painel Cloudflare, preservando binding KV `PEDIDOS` e secret `ADMIN_TOKEN`; não criar outro Worker nem apagar recursos.
- Não fazer merge na `main`. Branch de teste permanece `feature/offline-library-on-redesign`.
- Após o usuário executar os testes, registrar cada resultado (aprovado/falhou/não testado) e corrigir somente o que falhar, sem repetir o checklist inteiro desnecessariamente.


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


## Situação atual — 27/11/2026 (Build #855)

- Build #855 concluído com sucesso: https://github.com/jabesguitar/Lyravoxen/actions/runs/46018345034
- Instalador Windows x64 disponível no artefato `Lyravoxen-Installer-Windows-x64` (ID 15207121706). ZIP portátil: `Lyravoxen-Windows-x64-portable` (ID 15207491300). Artefatos expiram em 25/02/2027.
- Branch: `feature/offline-library-on-redesign`; `main` não foi alterada.
- Próxima etapa: instalar o artefato do build #855 e validar em execução real: (1) música offline continua ao trocar de abas; (2) pausar/retomar/parar/volume/posição; (3) seleção destacada no histórico e na Biblioteca; (4) link direto MP3/WAV, escolha de destino e registro na coleção; (5) retenção e preservação da letra/dados.
- Ainda pendente: publicação/validação do Worker Cloudflare em produção. Os testes automatizados do código passaram, mas produção não foi atualizada por esta execução.
- Não fazer merge em `main` sem autorização expressa.


## Atualização — 27/11/2026: lista da Biblioteca e janela de pedidos

- Corrigida a troca de pastas para limpar o filtro antigo antes de carregar as músicas da coleção selecionada.
- Comandos inferiores da coluna Músicas da coleção limitados em altura com rolagem própria para não ocultar a lista de músicas.
- Janela Pedidos recebidos redesenhada: janela maior, cabeçalho/contador, configurações recolhidas, cards por pedido com conjunto/cantor, título, link e botões de ação legíveis. Botão Copiar link removido dos cards; bloqueio de IP permanece secundário.
- Build #862: https://github.com/jabesguitar/Lyravoxen/actions/runs/46025648719 (em andamento no momento deste registro).
- Próxima etapa: aguardar #862. Se verde, baixar instalador e validar: trocar entre pastas com e sem filtro; conferir lista visível; abrir Pedidos recebidos e conferir cards, texto e botões. Não mexer na main.


## Atualização — 27/11/2026: redesign Controle pelo celular

- Janela Controle pelo celular redesenhada para combinar com Pedidos recebidos: fundo azul-escuro, cartões, QR destacado, endereço legível/copiar, PIN em destaque e botão Gerar novo PIN. Mantidos QR, endereços, PIN e instruções existentes.
- Commit: c4b7796e38dd502c4a276a603987dc0c331be18a.
- Build #866: https://github.com/jabesguitar/Lyravoxen/actions/runs/46029745807 (em andamento).
- Próxima ação: aguardar build. Se verde, baixar instalador único e validar visualmente as telas Biblioteca Offline, Pedidos recebidos e Controle pelo celular. Branch feature/offline-library-on-redesign; não alterar main.

- Observação operacional: o push de documentação cancelou a execução concorrente #866; acompanhar a execução mais recente disparada pelo commit final de documentação antes de liberar o instalador.


## Atualização — 27/11/2026: filtro conservador de endpoints de anúncios YouTube

- Implementada política de requisições que bloqueia caminhos conhecidos `/pagead/` e `/api/stats/ads` em `youtube.com` e subdomínios, além da lista de hosts já existente.
- Preservados API IFrame, páginas normais do YouTube, `googlevideo.com` e exceções de domínio permitidas pelo usuário.
- Testes adicionados para confirmar bloqueio desses endpoints e preservação dos recursos de reprodução.
- Commits: `c026684` (política), `b27d130` (integração WebView2), `c554d1c` (testes).
- Build #870 passou antes dessas alterações, mas não valida os commits novos. Acompanhar a execução disparada pelo commit mais recente.
- Próxima ação: confirmar workflow verde; instalar/abrir o app e testar vários vídeos na aba Próxima música, incluindo início, reprodução contínua, troca de vídeo e carregamento de letras. Registrar se anúncios pré-roll ainda aparecem ou se algum vídeo/player falha.
- Limite conhecido: não é bloqueio garantido de anúncios in-stream/servidos dinamicamente; nenhuma alteração no player principal foi feita.
- Branch `feature/offline-library-on-redesign`; `main` permanece intocada.


## Atualização — 27/11/2026: correção CS0103 BrowserAdBlocker

- Log recebido mostrou nove erros CS0103: `BrowserAdBlocker` não existia no contexto de `MainWindow.xaml.cs`.
- Causa: `BrowserAdBlocker.cs` estava com namespace/classe errados (conteúdo de `BrowserRequestPolicy`), removendo o serviço esperado.
- Serviço restaurado em `Lyravoxen.Services`, com integração ao novo filtro de endpoints YouTube preservada.
- Commit: `bfebc4b14ebba8deea3801cf63e7295d447b53d9`.
- Build #876: https://github.com/jabesguitar/Lyravoxen/actions/runs/46034132906 (em andamento ao registrar).
- Próxima ação: aguardar #876 e, se falhar, abrir logs do job build e corrigir a próxima falha. Avisos CS8602/CS4014 permanecem não fatais e fora do escopo desta correção.
- Branch feature/offline-library-on-redesign; main intocada.


## Atualização — 27/11/2026: teste real confirmou anúncios ainda presentes

- Usuário testou a compilação e relatou dois anúncios já na primeira música reproduzida na aba Próxima música.
- Conclusão: filtro pontual por caminhos de anúncio não resolveu pré-roll/in-stream; funcionalidade ainda não atende ao objetivo.
- Build #878 passou: https://github.com/jabesguitar/Lyravoxen/actions/runs/46034192649. Build verde não comprova bloqueio de anúncios.
- Pesquisa de alternativa: estudar integração do motor upstream `brave/adblock-rust` (MPL-2.0), que oferece filtro ABP, bloqueio de rede, cosmético e scriptlets. Não copiar o wrapper ZWebWrap (GPL-3.0-or-later) como um todo.
- Próxima ação: fazer protótipo isolado da integração Rust/C ABI + C# P/Invoke/WebView2; medir compatibilidade, build e impacto em CPUs antigas antes de conectar ao player. Criar testes com URLs/tipos de recurso e testes manuais com YouTube.
- Limite: nem motor ABP/uBO garante identificar anúncios inseridos no mesmo fluxo de mídia do vídeo.
- Nenhuma alteração adicional no código após o teste do usuário. Não mexer na main.


## Atualização — 27/11/2026: correção CS0117 WebSocket

- Build #886 falhou porque o enum WebView2 1.0.2903.40 não define `CoreWebView2WebResourceContext.WebSocket`.
- Removido esse case; os demais tipos de requisição permanecem e recursos sem mapeamento usam `other`.
- Commit: `db889fbb79c6e292a21b9505259e4f22f8256e76`.
- Acompanhar novo workflow. Próximo objetivo: validar compilação do Rust e .NET, DLL nativa incluída na publicação e testes; não considerar anúncios resolvidos antes de teste real.


## Atualização — 27/11/2026: teste visual falhou

- Duas capturas enviadas pelo usuário mostram anúncios em vídeo Pantene e Mercado Livre, além de anúncios patrocinados laterais na aba Próxima música.
- Conclusão: proteção ainda não atende ao requisito. Não solicitar novas provas.
- O motor ABP em implementação não basta por si só: falta validar carregamento nativo em runtime e avaliar filtros cosméticos/scriptlets para interface; anúncios in-stream são um problema separado e não há garantia de bloqueio por URL.
- Próxima ação: conferir build mais recente e a inicialização efetiva da DLL/listas; depois desenhar uma abordagem separada para elementos patrocinados da página e anúncios dentro do player. Testar antes de pedir novo teste ao usuário.
- Não apresentar a proteção como concluída nem entregar versão como solução. Branch feature/offline-library-on-redesign; main intocada.


## Atualização — 27/11/2026: protótipo com extensão open source no WebView2

- Mudança de abordagem após evidência visual de anúncios: integrar extensão pronta, em vez de insistir apenas no motor próprio.
- Extensão escolhida: uBlock Origin Lite (Edge), release 2026.1124.1811, GPL-3.0, distribuída sem modificações.
- Workflow baixa a versão oficial, extrai a pasta com manifest.json, inclui no pacote Windows e obtém o LICENSE.
- Inicialização WebView2 corrigida para criar CoreWebView2Environment com AreBrowserExtensionsEnabled=true, perfil persistente LocalAppData/Lyravoxen/WebView2 e AddBrowserExtensionAsync.
- A revisão com essa integração ainda precisa de build; build #891 verde refere-se ao protótipo Rust anterior e não valida extensão.
- Próximo passo: validar compilação e bundle; depois confirmar em runtime que a extensão aparece em GetBrowserExtensionsAsync/IsEnabled e testar filtros de página. Atenção: API de extensão existe no WebView2, mas não há ainda confirmação de compatibilidade integral das APIs usadas pelo uBOL.
- Branch feature/offline-library-on-redesign; main intocada. Não solicitar ao usuário que prove novamente presença de anúncios.


## Atualização — 27/11/2026: teste real do uBlock Origin Lite não funcionou

- Usuário testou build #897 e a captura mostra anúncio em vídeo "Aproxime-se de Deus" (1 de 2), anúncio lateral e anúncio patrocinado; portanto resultado negativo.
- Build #897 compilou e empacotou, mas não comprova que a extensão foi ativada nem que suas regras foram aplicadas.
- Código atual chama AddBrowserExtensionAsync mas não confirma com GetBrowserExtensionsAsync, IsEnabled ou diagnóstico de regras. Status da extensão é sobrescrito depois pela mensagem "Player pronto". Painel "81.671 regras / 0 solicitações bloqueadas" mede apenas o bloqueador legado, não uBOL.
- Próxima ação: melhorar diagnóstico e instalação idempotente: consultar extensão já instalada, confirmar ID/nome/IsEnabled, evitar reinstalar, guardar versão/erros/WebView2 Runtime em log local e mostrar estado real no painel. Validar DNR separadamente antes de nova compilação para o usuário.
- Não pedir novas provas; usuário já mostrou anúncios em três capturas e quer avançar. Não declarar bloqueio resolvido.
- Manter este arquivo e MEMORIA_CONTINUA.md sempre atualizados. Branch feature/offline-library-on-redesign; main intocada.


## Atualização — 27/11/2026: nova captura confirma anúncios ainda presentes

- Nova captura do usuário na aba Próxima música, busca "harpa crista 15": anúncio em vídeo Amazon Prime/Blade Runner 2099, marcado "Patrocinado · 2 de 2", duração 0:06 e reprodução em 0:03/0:06; anúncio patrocinado lateral também visível.
- Conclusão: uBlock Origin Lite integrado não resolveu o problema. Não solicitar novas provas.
- Próxima etapa antes de nova compilação: instrumentar GetBrowserExtensionsAsync, confirmar ID/nome/versão/IsEnabled; instalar apenas quando ausente; registrar versão WebView2, caminho e HRESULTs; separar estado real da extensão do contador legado; validar DNR com fixture controlada. Se DNR funcionar e anúncios continuarem, estudar outra solução open source compatível em vez de repetir a mesma abordagem.
- Não declarar proteção eficaz nem pedir novo teste até termos diagnóstico técnico. Build #897 verde apenas para compilação/empacotamento; teste real falhou.
- Manter este arquivo e MEMORIA_CONTINUA.md atualizados. Branch feature/offline-library-on-redesign; main intocada.


## Requisito atual prioritário — sequência de letra (27/11/2026)

O usuário quer DUAS opções:

- **Automática (preferida):** colar a letra original sem reorganizar; o sistema reconhece no áudio a ordem real das frases e as repetições do coro, mesmo quando o coro aparece uma única vez no texto do site.
- **Manual (alternativa):** editor visual de blocos selecionáveis, onde o usuário marca trechos e monta a ordem real; pode repetir um bloco sem duplicar o texto-fonte; salva sequência vinculada à música para reutilização.

Regras:
- Preservar letra original, blocos-fonte, sequência de ocorrências e tempos como dados distintos.
- Não exigir que o usuário rotule estrofe/coro.
- Sequência manual precisa persistir, ser recuperável e editável.
- O matcher atual `LiveLyricsMatcher` é sequencial/monotônico e não resolve repetições fora de ordem; não anunciar automático como pronto sem desenvolver/testar.
- Atualizar a aba Ajuda junto com os recursos, incluindo uso, atalhos e recuperação.
- Próximo passo: modelar persistência de blocos e ocorrências; implementar/testar organizador manual persistente primeiro; depois desenvolver alinhamento automático com repetição.
- Não entregar somente marcação linear por espaço como solução final.
- Frente de anúncios continua pausada; não misturar com esta etapa.
- Branch feature/offline-library-on-redesign; main intocada.
# ESTADO ATUAL — RETOMADA PRIORITÁRIA (27/11/2026)

> Leia esta seção antes de continuar. Resumo do estado real; histórico abaixo preservado.

## Decisão atual do usuário
A frente de bloqueio de anúncios do YouTube está **PAUSADA**. O usuário precisa avançar para outras funcionalidades e não quer mais consumir tempo com compilações sem evidência de eficácia. Não prometer remoção de anúncios do YouTube.

## Repositório
- https://github.com/jabesguitar/Lyravoxen
- Branch: `feature/offline-library-on-redesign`
- `main` intacta.
- Build #901 passou, mas não valida eficácia de anúncios.

## Frente de anúncios — estado
- Tentativas feitas: bloqueador por domínios/listas, filtros de endpoints, protótipo Brave adblock-rust e extensão uBlock Origin Lite no WebView2.
- Testes do usuário confirmaram anúncios patrocinados em vídeo e na lateral; objetivo não alcançado.
- Não há garantia técnica de que uBlock Lite ou botão de controle remova anúncios in-stream/first-party do YouTube.
- Ideia sugerida pelo usuário: botão redondo uBlock abaixo do controle de tema na aba Próxima música.
- Um botão pode controlar habilitar/desabilitar uma extensão que esteja instalada no WebView2, mas isso não equivale a bloquear anúncios. **Não implementar como indicador de proteção eficaz.**
- Nenhum botão foi implementado nesta decisão.
- Preservar código experimental; não apagar ou reverter sem necessidade.
- Estado: PAUSADO até existir solução com evidência técnica suficiente.

## Outras implementações existentes
- Biblioteca Offline com pasta configurável e subpastas Musicas/Letras/Dados/Temporarios.
- Arquivo independente de letras/sincronizações e recuperação por ID/URL.
- Integração de armazenamento com Pedidos; importação MP3/WAV e fluxo para URL direta autorizada.
- Retenção configurável 5/7/14/30 dias.
- Barra global de reprodução offline e seleção visual em listas.
- Redesign de Pedidos recebidos e Controle pelo celular.
- Ajustes no painel Próxima música e lista da Biblioteca; alguns cenários ainda dependem de validação manual consolidada.

## Próxima ação
Perguntar/seguir a próxima funcionalidade do Lyravoxen que o usuário deseja priorizar. Não voltar à frente de anúncios por iniciativa própria. Manter `MEMORIA_CONTINUA.md` e `RETOMADA_ATUAL.md` atualizados após mudanças significativas.

## Regras
- Não pedir provas já fornecidas.
- Distinguir implementado, compilado, testado e aprovado.
- Não entregar builds intermediários como se fossem finais.
- Não alterar `main` sem autorização.
- Preservar o histórico abaixo desta seção.

---
---

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


## Continuação executada — 26/11/2026

### Estado atual da implementação
- Branch preservada: `feature/offline-library-on-redesign`. A `main` não foi alterada.
- Criado o projeto compartilhado `src/Lyravoxen.Offline`, sem dependência de WPF, para permitir testes funcionais multiplataforma.
- Implementado `OfflineMediaService` para download de URLs diretas autorizadas e importação local MP3/WAV:
  - limite padrão de 50 MB, timeout de 45 segundos, cancelamento e remoção de arquivos temporários após falha;
  - validação HTTP/HTTPS, MIME quando informado, extensão e assinatura MP3/WAV;
  - validação DNS/IP público, bloqueio de redes privadas e redirecionamentos limitados; conexão usa o IP público validado para reduzir risco de DNS rebinding;
  - hash SHA-256 e deduplicação por conteúdo;
  - links do YouTube são recusados como fonte de arquivo de áudio.
- Aba Pedidos: URLs diretas de áudio são baixadas; links somente do YouTube explicam a limitação e oferecem seleção de arquivo autorizado local. A cópia é associada ao pedido e à Biblioteca.
- Biblioteca: aceita URL direta ou importação local. Adicionado editor de título, intérprete e letra na própria aba Biblioteca, sem depender da aba Cadastrar letras. A sincronização existente é preservada.
- Pastas distintas podem referenciar a mesma mídia deduplicada sem duplicar os bytes.
- Expiração: limpeza a cada minuto e na inicialização. O arquivo só é excluído quando não há outra referência ativa na Biblioteca ou em Pedidos; fichas, letras e metadados permanecem.
- Aba Próxima música: painel de comandos reorganizado em layout compacto, mantendo a coluna do navegador/WebView2.
- Proteção: política de bloqueio extraída para componente puro e testável. Os domínios protegidos do YouTube/Googlevideo permanecem permitidos; domínios de anúncios podem ser bloqueados e exceções do usuário são respeitadas.

### Testes automatizados adicionados
Projeto: `tests/Lyravoxen.OfflineTests`.
- Importação local WAV e MP3.
- Download direto WAV e MP3 com respostas HTTP controladas.
- Hash/deduplicação e preservação dos bytes.
- Recusa de YouTube como fonte de áudio.
- Bloqueio de DNS privado e redirecionamento para rede privada.
- Redirecionamento HTTPS permitido, limite de tamanho e recusa de HTML.
- Política do navegador: preservar YouTube/googlevideo, bloquear domínio de anúncio, respeitar exceções e evitar correspondência parcial.
- Expiração: preservar arquivo ainda referenciado por outra pasta ou pedido e permitir excluir quando não houver referências.

### Evidência de CI
- Run `45888141360`, commit `1c888e98`: job `cloud-worker-tests` concluído com sucesso. Serviço offline, cobertura da Ajuda, C# static checks, testes do Worker e página remota mobile aprovados. O teste offline nessa revisão cobria os fluxos de WAV, URL direta, redirecionamento, limite de tamanho e política do navegador; ainda não incluía os testes de expiração e MP3 acrescentados depois.
- Run `45887612874`, commit `5003f831`: jobs `cloud-worker-tests` e `build` concluídos com sucesso. A compilação Windows confirmou a integração inicial do serviço com Pedidos/Biblioteca e o timer de expiração, mas ocorreu antes do editor da Biblioteca e da compactação final da aba Próxima música. Não é a compilação final desta rodada.
- Run `45888415155`, commit `2b9f8418`: no último acompanhamento, o teste offline, cobertura da Ajuda, C# static checks e testes do Worker estavam aprovados; Playwright / teste mobile ainda estava em execução. Esse run contém os testes adicionais de MP3 e política de expiração. Aguardar conclusão antes de liberar build.

### Limitações que ainda impedem declarar pronto
- Os testes de mídia usam arquivos sintéticos e handlers HTTP controlados; ainda não comprovam reprodução real de MP3/WAV pelo MediaElement do Windows nem download contra diferentes servidores externos.
- A política do bloqueador está testada, mas ainda falta teste de integração com WebView2 real confirmando que a pesquisa, login e reprodução do YouTube permanecem funcionais e que a exceção “Permitir site” funciona em navegação real.
- A nova disposição da aba Próxima música foi validada por XAML/build, mas ainda requer inspeção visual em uma janela real e em diferentes resoluções.
- A execução Windows `45887612874` é evidência de build intermediário, não da versão final com o editor e o layout compactado.
- O workflow contém cron diário `17 10 * * *`, mas GitHub Actions executa `schedule` somente a partir da branch padrão. Como não foi autorizado merge na `main`, a rotina diária ainda não está ativa para esta branch. Os testes continuam sendo executados a cada push e podem ser acionados manualmente pela interface do GitHub.

### Próximas ações
1. Aguardar o término do run `45888415155` e conferir os logs completos dos 21 testes funcionais.
2. Fazer uma compilação Windows consolidada com todas as alterações atuais; corrigir qualquer falha de build e repetir.
3. Atualizar este registro com o SHA final, os jobs e os artefatos reais.
4. Só então preparar a única compilação para a rodada de testes do usuário, com checklist específico de navegação real, reprodução offline, expiração e editor da Biblioteca.
5. Não fazer merge na `main` sem aprovação explícita.


## Resultado da compilação consolidada — 26/11/2026

<badge color="success">CI e build Windows aprovados</badge>

- Workflow: [45889091270](https://github.com/jabesguitar/Lyravoxen/actions/runs/45889091270)
- SHA validado: `76e1f018a9e84c65505de05064eb50534ae0cd86`
- Branch: `feature/offline-library-on-redesign`
- Job `cloud-worker-tests`: sucesso.
  - 21 verificações do serviço offline e políticas de navegador/expiração.
  - Cobertura da Ajuda: sucesso.
  - C# static checks: sucesso.
  - Cloudflare Worker tests: sucesso.
  - Playwright Chromium / mobile remote page: sucesso.
- Job `build`: sucesso.
  - Geração de ícone e splash compacta: sucesso.
  - Restore e `dotnet publish` Windows x64: sucesso.
  - Cue timeline tests: sucesso.
  - ZIP portátil: sucesso.
  - Instalador Inno Setup: sucesso.
  - Upload dos três artefatos: sucesso.

### Artefatos desta compilação
- `Lyravoxen-Windows-x64` — artifact ID `15150797165` — 76,260,202 bytes.
- `Lyravoxen-Windows-x64-portable` — artifact ID `15150578815` — 76,294,856 bytes.
- `Lyravoxen-Installer-Windows-x64` — artifact ID `15150673234` — 53,834,443 bytes.
- Os três artefatos constavam como disponíveis e não expirados no momento da consulta.

### Correção realizada durante a compilação
- A primeira tentativa completa falhou por uma referência ambígua a `HorizontalAlignment.Right` no editor da Biblioteca.
- Corrigido para `System.Windows.HorizontalAlignment.Right`.
- A compilação seguinte no SHA acima passou por completo.

### Situação para a rodada única do usuário
- A compilação Windows consolidada foi gerada e está disponível nos artefatos do workflow.
- Ainda não há comprovação manual em um PC com WebView2 real de anúncios, pesquisa/login/reprodução do YouTube; a política de bloqueio foi testada isoladamente.
- A reprodução real de MP3/WAV offline e o comportamento visual do editor/layout em diferentes resoluções também dependem da validação manual no computador anfitrião.
- Não fazer merge na `main` sem aprovação explícita.
- Rotina diária: cron está no workflow da branch, mas GitHub Actions só dispara `schedule` na branch padrão. Como a `main` permanece intacta, os testes diários ainda não estão ativos; o CI roda a cada push e pode ser iniciado manualmente pela interface do GitHub.

### Próxima ação
Entregar ao usuário esta única compilação consolidada e o checklist manual final. Se os testes manuais forem aprovados, registrar a aprovação antes de considerar merge na `main`.


## Revisão corretiva — Pedidos, Biblioteca Offline e anúncios — 26/11/2026

### Solicitações do usuário
- A aba Próxima música ainda mostra anúncios no início do vídeo.
- Pedidos e Biblioteca Offline não estavam concluindo o fluxo esperado para links do YouTube; o usuário relatou dificuldade em baixar, salvar em pasta e renomear/organizar músicas.
- Exibir IP/endereço de origem de quem usa o formulário público, com bloqueio e desbloqueio para reduzir spam/sabotagem.
- Fazer uma única revisão ampla e uma única rodada manual, sem builds vazios intermediários.

### Limites técnicos e de plataforma
- Não implementar extração de áudio do YouTube nem contorno de anúncios/restrições. O player incorporado pode exibir anúncios servidos pela própria origem do YouTube; o bloqueador de domínios de terceiros não garante removê-los.
- Para offline, são suportados: arquivo MP3/WAV local autorizado ou URL HTTPS direta que entregue o arquivo de áudio. URL de página do YouTube não é URL direta de áudio.
- MAC não é disponibilizado ao servidor quando alguém acessa um site pela internet; o Worker pode registrar o IP público fornecido pelo Cloudflare. Não exibir campo MAC fictício.
- O código do Worker foi atualizado na branch, mas o serviço em produção `lyravoxen-pedidos.ferkt319.workers.dev` não foi implantado por esta sessão: não há conexão autorizada com a conta Cloudflare. Para ativar IP/rate-limit na URL pública, é necessária uma implantação única do novo código pelo painel Cloudflare. Pedidos antigos, criados antes do Worker novo, podem não ter IP.

### Implementações desta rodada
- Worker Cloudflare:
  - captura e armazena IP público `CF-Connecting-IP` junto com texto e horário do pedido;
  - bloqueio/desbloqueio autenticado por IP via `/bloqueios`;
  - limite básico de 5 envios por IP por janela de 10 minutos, além da fila global de até 150 pedidos;
  - aviso no formulário sobre registro do IP público e horário para prevenção de abuso, esclarecendo que MAC não é coletado;
  - documentação orienta a atualizar o Worker existente sem recriar KV ou trocar `ADMIN_TOKEN`.
  - KV é eventualmente consistente; o rate-limit no Worker é proteção básica. Para ataques concorrentes/coordenados, complementar com regra Rate Limiting no Cloudflare.
- App Pedidos:
  - sincroniza a lista de bloqueios remotos quando abre a janela;
  - exibe IP público por pedido quando o Worker atualizado o fornece;
  - oferece Bloquear este IP / Desbloquear este IP e Atualizar bloqueios da internet;
  - se IP vier vazio, avisa que o Worker público pode estar desatualizado;
  - normaliza e valida endereços IP ao cadastrar bloqueios.
- Biblioteca:
  - importar URL direta que falhe agora oferece selecionar arquivo MP3/WAV local;
  - colar URL do YouTube apresenta explicação e abre diretamente o seletor de arquivo autorizado;
  - botão de importação continua aceitando múltiplos MP3/WAV;
  - seleção de destino ao salvar pedido offline; não fixa tudo numa pasta genérica;
  - novos comandos Mover para outra pasta e Remover desta pasta (o segundo preserva o arquivo de áudio);
  - seletor permite criar nova pasta;
  - busca inclui título, intérprete e compositor;
  - renomear pasta valida nomes duplicados;
  - adicionar vídeo atual à pasta usa identidade exata (ID do vídeo ou caminho da mídia offline), preservando metadados;
  - SaveLibrary agora retorna sucesso/falha; operações críticas não exibem sucesso falso quando a persistência falha.
- Ajuda atualizada para os novos botões e comportamento.

### Validação final
- Workflow: [45894030900](https://github.com/jabesguitar/Lyravoxen/actions/runs/45894030900)
- SHA: `4f39f92562b6eb470b16be1d3513e9d1ecbe8543`
- Branch: `feature/offline-library-on-redesign`
- Job `cloud-worker-tests`: sucesso.
  - Serviço offline: 21 verificações aprovadas (MP3/WAV, URL direta, deduplicação, SSRF, redirecionamentos, limite de tamanho, política de anúncios e expiração compartilhada).
  - Worker: 44 testes aprovados, incluindo bloqueio/desbloqueio por IP e rate-limit.
  - Ajuda: cobertura aprovada.
  - C# static checks: aprovados.
  - Formulário público em Chromium: 25 testes aprovados.
  - Página mobile em Chromium: 112 testes aprovados.
- Job `build`: sucesso.
  - Ícone e splash: sucesso.
  - Restore e `dotnet publish` Windows x64: sucesso.
  - Cue timeline tests: sucesso.
  - ZIP portátil: sucesso.
  - Instalador Inno Setup: sucesso.
  - Upload dos três artefatos: sucesso.
- Artefatos gerados:
  - `Lyravoxen-Windows-x64`: ID `15153435863`, 76,266,769 bytes.
  - `Lyravoxen-Windows-x64-portable`: ID `15153276186`, 76,302,238 bytes.
  - `Lyravoxen-Installer-Windows-x64`: ID `15153231348`, 53,845,674 bytes.
  - Expiração prevista dos artefatos: 24/02/2027.
- `main` continua no SHA `2051135a6e4915c13dda5730fa0248ed7dbcfe2f`; não foi alterada nem recebeu merge.

### Limitações ainda abertas
- Anúncios in-stream/first-party do YouTube continuam fora do alcance garantido do bloqueador; não prometer reprodução YouTube sem anúncios.
- O Worker em produção precisa ser atualizado manualmente uma única vez para IP e bloqueios remotos aparecerem nos pedidos futuros.
- Testes automatizados de mídia usam fixtures sintéticas e servidor HTTP controlado; reprodução real via WebView2/MediaElement e navegação real no PC anfitrião ainda exigem validação do usuário.
- O pacote está pronto para uma única rodada manual no PC anfitrião; não pedir testes de builds intermediários.

### Próxima ação
1. Usuário instala a versão consolidada.
2. Uma única implantação do Worker atualizado no Cloudflare, mantendo namespace KV e segredo existentes.
3. Uma única rodada manual com MP3/WAV real, salvar/mover/renomear, pedido público com IP e navegação YouTube.
4. Registrar os resultados antes de qualquer merge na `main`.

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

### Regra vigente
Trabalhar somente em IP/bloqueios, download/Biblioteca Offline e proteção de anúncios na aba Próxima Música. Não alterar o que já funciona, não fazer redesign e não tocar na branch `main`. Esta regra mais recente prevalece sobre pendências antigas de redesign registradas acima.

### Alteração aplicada
- Commit `78e88d6942a578ac4491b94b577fe9185c2b3211`, arquivo `src/Lyravoxen.Desktop/MainWindow.Requests.cs`.
- Para pedidos com URL do YouTube, o botão **Baixar offline** agora explica que o link não é arquivo MP3/WAV baixável diretamente e pergunta se o operador deseja associar um arquivo autorizado que já possua.
- Se responder Não, não abre seletor. Se responder Sim, mantém o fluxo existente de importação/associação ao pedido e Biblioteca.
- URLs não-YouTube continuam tentando download direto. Não houve extração de áudio nem alteração do player.

### Verificações
- Branch confirmada: `feature/offline-library-on-redesign`; comparação com `main`: 222 commits à frente, 0 atrás. Nenhum merge foi feito.
- Build #810 passou antes desta alteração; não conta como validação do commit novo.
- Push nesta branch executa `cloud-worker-tests`; o job Windows `build` só executa em `workflow_dispatch` ou na `main`. Workflow não foi alterado.

### Pendências / próxima ação
- [ ] Conferir CI acionado pelo commit `78e88d6`.
- [ ] Após CI verde, executar build Windows consolidado via workflow manual.
- [ ] Atualizar Worker existente no Cloudflare mantendo KV `PEDIDOS` e secret `ADMIN_TOKEN`; testar IP/bloqueio/desbloqueio em produção.
- [ ] Testar reprodução offline real da cópia autorizada exata.
- [ ] Manter explícito que anúncios first-party/in-stream do YouTube não podem ser garantidamente removidos sem comprometer o player.
- Continuar auditoria da Biblioteca e retenção sem reescrever a limpeza já existente.


## 27/11/2026 — Build consolidado #822 concluído; iniciar rodada única de testes

### Build e CI
- Workflow [Windows build #822](https://github.com/jabesguitar/Lyravoxen/actions/runs/45942283747) concluído com sucesso no commit `eeb1f78d433fe09d46a7448b91023ef3ef256c04`.
- `cloud-worker-tests`: todos os passos passaram, incluindo mídia offline, cobertura da Ajuda, C# estático, Worker, formulário público e página remota móvel.
- `build`: Restore, publish Windows x64, teste da linha do tempo, ZIP portátil, instalador Inno Setup e uploads passaram.
- Correção aplicada após log do usuário: `using System.IO;` nos arquivos `YoutubeAudioDownloadService.cs`, `PermanentLyricsArchive.cs` e `MainWindow.OfflineStorage.cs`.

### Artefatos disponíveis (expiração 25/02/2027)
- Instalador Windows x64 — artifact ID `15173087837`; SHA-256 `98203e478aed295ab6d7b5ed61da3b6462a0d2e20a7486536cde002a2ff03bad`.
- Pasta Windows x64 — artifact ID `15172753278`; SHA-256 `f1dbf9e007935adcdedfb3be252766572f865de75e4e07e57045afba59cd3c12`.
- ZIP portátil — artifact ID `15172354465`; SHA-256 `680ce96f6fd6322f62a574fb9fc832e81fd5946367f7b7291360d84a2e0a838e`.

### Instrução para a rodada manual consolidada
1. Abrir a execução #822 e, na seção Artifacts, baixar `Lyravoxen-Installer-Windows-x64`.
2. Extrair o ZIP baixado e executar `Lyravoxen-Setup-Windows-x64.exe`. Fechar o Lyravoxen antes de instalar. Não precisa baixar os três pacotes; o instalador é o recomendado.
3. Executar a lista abaixo sem apagar a biblioteca atual nem dados anteriores.

### Checklist de testes do usuário
- [ ] Abrir o aplicativo e confirmar que inicia normalmente, sem erro ou tela vazia.
- [ ] Configuração de armazenamento: abrir a opção de pasta da Biblioteca Offline; escolher/criar uma pasta dedicada; confirmar que o app cria `Musicas`, `Letras`, `Dados` e `Temporarios` dentro dela.
- [ ] Cadastro de letra: abrir uma música com letra e cues, salvar, fechar e reabrir; confirmar que letra e sincronização continuam registradas.
- [ ] Arquivo de áudio: associar/importar um MP3 ou WAV autorizado; confirmar que aparece na Biblioteca Offline e reproduz.
- [ ] Persistência: remover somente a cópia de áudio (não apagar a ficha/letra pelo app, se houver essa opção); baixar/importar novamente a mesma gravação; confirmar que a letra continua vinculada.
- [ ] Pedidos recebidos: abrir um pedido e clicar em **Baixar offline**; confirmar a resposta apresentada e que o arquivo fica na pasta configurada. Para link YouTube, anotar a mensagem/resultado exato; não considerar download concluído se não houver arquivo reproduzível.
- [ ] Preparar próxima música: selecionar a cópia offline vinculada ao pedido; desligar a internet; tocar a próxima e confirmar reprodução local da gravação exata.
- [ ] Retenção: conferir a validade selecionada e que a limpeza não apaga letras/metadados; não alterar a data do sistema para simular expiração.
- [ ] Próxima Música: navegar, pesquisar e reproduzir; confirmar que o player continua funcionando com proteção ligada.
- [ ] Proteção de anúncios: registrar se anúncios/trackers são bloqueados; anúncios in-stream do próprio YouTube podem continuar aparecendo e não devem ser considerados falha se o player permanecer funcional.
- [ ] Pedidos pela internet: verificar recebimento dos pedidos e se o IP aparece; conferir bloqueio/desbloqueio somente após atualização do Worker em produção.

### Limites e pendências
- O build #822 prova compilação e testes automatizados, não prova download real do YouTube, reprodução offline no PC nem publicação do Worker Cloudflare em produção.
- O Worker existente ainda precisa receber o código atualizado pelo usuário no painel Cloudflare, preservando binding KV `PEDIDOS` e secret `ADMIN_TOKEN`; não criar outro Worker nem apagar recursos.
- Não fazer merge na `main`. Branch de teste permanece `feature/offline-library-on-redesign`.
- Após o usuário executar os testes, registrar cada resultado (aprovado/falhou/não testado) e corrigir somente o que falhar, sem repetir o checklist inteiro desnecessariamente.


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


## Situação atual — 27/11/2026 (Build #855)

- Build #855 concluído com sucesso: https://github.com/jabesguitar/Lyravoxen/actions/runs/46018345034
- Instalador Windows x64 disponível no artefato `Lyravoxen-Installer-Windows-x64` (ID 15207121706). ZIP portátil: `Lyravoxen-Windows-x64-portable` (ID 15207491300). Artefatos expiram em 25/02/2027.
- Branch: `feature/offline-library-on-redesign`; `main` não foi alterada.
- Próxima etapa: instalar o artefato do build #855 e validar em execução real: (1) música offline continua ao trocar de abas; (2) pausar/retomar/parar/volume/posição; (3) seleção destacada no histórico e na Biblioteca; (4) link direto MP3/WAV, escolha de destino e registro na coleção; (5) retenção e preservação da letra/dados.
- Ainda pendente: publicação/validação do Worker Cloudflare em produção. Os testes automatizados do código passaram, mas produção não foi atualizada por esta execução.
- Não fazer merge em `main` sem autorização expressa.


## Atualização — 27/11/2026: lista da Biblioteca e janela de pedidos

- Corrigida a troca de pastas para limpar o filtro antigo antes de carregar as músicas da coleção selecionada.
- Comandos inferiores da coluna Músicas da coleção limitados em altura com rolagem própria para não ocultar a lista de músicas.
- Janela Pedidos recebidos redesenhada: janela maior, cabeçalho/contador, configurações recolhidas, cards por pedido com conjunto/cantor, título, link e botões de ação legíveis. Botão Copiar link removido dos cards; bloqueio de IP permanece secundário.
- Build #862: https://github.com/jabesguitar/Lyravoxen/actions/runs/46025648719 (em andamento no momento deste registro).
- Próxima etapa: aguardar #862. Se verde, baixar instalador e validar: trocar entre pastas com e sem filtro; conferir lista visível; abrir Pedidos recebidos e conferir cards, texto e botões. Não mexer na main.


## Atualização — 27/11/2026: redesign Controle pelo celular

- Janela Controle pelo celular redesenhada para combinar com Pedidos recebidos: fundo azul-escuro, cartões, QR destacado, endereço legível/copiar, PIN em destaque e botão Gerar novo PIN. Mantidos QR, endereços, PIN e instruções existentes.
- Commit: c4b7796e38dd502c4a276a603987dc0c331be18a.
- Build #866: https://github.com/jabesguitar/Lyravoxen/actions/runs/46029745807 (em andamento).
- Próxima ação: aguardar build. Se verde, baixar instalador único e validar visualmente as telas Biblioteca Offline, Pedidos recebidos e Controle pelo celular. Branch feature/offline-library-on-redesign; não alterar main.

- Observação operacional: o push de documentação cancelou a execução concorrente #866; acompanhar a execução mais recente disparada pelo commit final de documentação antes de liberar o instalador.


## Atualização — 27/11/2026: filtro conservador de endpoints de anúncios YouTube

- Implementada política de requisições que bloqueia caminhos conhecidos `/pagead/` e `/api/stats/ads` em `youtube.com` e subdomínios, além da lista de hosts já existente.
- Preservados API IFrame, páginas normais do YouTube, `googlevideo.com` e exceções de domínio permitidas pelo usuário.
- Testes adicionados para confirmar bloqueio desses endpoints e preservação dos recursos de reprodução.
- Commits: `c026684` (política), `b27d130` (integração WebView2), `c554d1c` (testes).
- Build #870 passou antes dessas alterações, mas não valida os commits novos. Acompanhar a execução disparada pelo commit mais recente.
- Próxima ação: confirmar workflow verde; instalar/abrir o app e testar vários vídeos na aba Próxima música, incluindo início, reprodução contínua, troca de vídeo e carregamento de letras. Registrar se anúncios pré-roll ainda aparecem ou se algum vídeo/player falha.
- Limite conhecido: não é bloqueio garantido de anúncios in-stream/servidos dinamicamente; nenhuma alteração no player principal foi feita.
- Branch `feature/offline-library-on-redesign`; `main` permanece intocada.


## Atualização — 27/11/2026: correção CS0103 BrowserAdBlocker

- Log recebido mostrou nove erros CS0103: `BrowserAdBlocker` não existia no contexto de `MainWindow.xaml.cs`.
- Causa: `BrowserAdBlocker.cs` estava com namespace/classe errados (conteúdo de `BrowserRequestPolicy`), removendo o serviço esperado.
- Serviço restaurado em `Lyravoxen.Services`, com integração ao novo filtro de endpoints YouTube preservada.
- Commit: `bfebc4b14ebba8deea3801cf63e7295d447b53d9`.
- Build #876: https://github.com/jabesguitar/Lyravoxen/actions/runs/46034132906 (em andamento ao registrar).
- Próxima ação: aguardar #876 e, se falhar, abrir logs do job build e corrigir a próxima falha. Avisos CS8602/CS4014 permanecem não fatais e fora do escopo desta correção.
- Branch feature/offline-library-on-redesign; main intocada.


## Atualização — 27/11/2026: teste real confirmou anúncios ainda presentes

- Usuário testou a compilação e relatou dois anúncios já na primeira música reproduzida na aba Próxima música.
- Conclusão: filtro pontual por caminhos de anúncio não resolveu pré-roll/in-stream; funcionalidade ainda não atende ao objetivo.
- Build #878 passou: https://github.com/jabesguitar/Lyravoxen/actions/runs/46034192649. Build verde não comprova bloqueio de anúncios.
- Pesquisa de alternativa: estudar integração do motor upstream `brave/adblock-rust` (MPL-2.0), que oferece filtro ABP, bloqueio de rede, cosmético e scriptlets. Não copiar o wrapper ZWebWrap (GPL-3.0-or-later) como um todo.
- Próxima ação: fazer protótipo isolado da integração Rust/C ABI + C# P/Invoke/WebView2; medir compatibilidade, build e impacto em CPUs antigas antes de conectar ao player. Criar testes com URLs/tipos de recurso e testes manuais com YouTube.
- Limite: nem motor ABP/uBO garante identificar anúncios inseridos no mesmo fluxo de mídia do vídeo.
- Nenhuma alteração adicional no código após o teste do usuário. Não mexer na main.


## Atualização — 27/11/2026: correção CS0117 WebSocket

- Build #886 falhou porque o enum WebView2 1.0.2903.40 não define `CoreWebView2WebResourceContext.WebSocket`.
- Removido esse case; os demais tipos de requisição permanecem e recursos sem mapeamento usam `other`.
- Commit: `db889fbb79c6e292a21b9505259e4f22f8256e76`.
- Acompanhar novo workflow. Próximo objetivo: validar compilação do Rust e .NET, DLL nativa incluída na publicação e testes; não considerar anúncios resolvidos antes de teste real.


## Atualização — 27/11/2026: teste visual falhou

- Duas capturas enviadas pelo usuário mostram anúncios em vídeo Pantene e Mercado Livre, além de anúncios patrocinados laterais na aba Próxima música.
- Conclusão: proteção ainda não atende ao requisito. Não solicitar novas provas.
- O motor ABP em implementação não basta por si só: falta validar carregamento nativo em runtime e avaliar filtros cosméticos/scriptlets para interface; anúncios in-stream são um problema separado e não há garantia de bloqueio por URL.
- Próxima ação: conferir build mais recente e a inicialização efetiva da DLL/listas; depois desenhar uma abordagem separada para elementos patrocinados da página e anúncios dentro do player. Testar antes de pedir novo teste ao usuário.
- Não apresentar a proteção como concluída nem entregar versão como solução. Branch feature/offline-library-on-redesign; main intocada.


## Atualização — 27/11/2026: protótipo com extensão open source no WebView2

- Mudança de abordagem após evidência visual de anúncios: integrar extensão pronta, em vez de insistir apenas no motor próprio.
- Extensão escolhida: uBlock Origin Lite (Edge), release 2026.1124.1811, GPL-3.0, distribuída sem modificações.
- Workflow baixa a versão oficial, extrai a pasta com manifest.json, inclui no pacote Windows e obtém o LICENSE.
- Inicialização WebView2 corrigida para criar CoreWebView2Environment com AreBrowserExtensionsEnabled=true, perfil persistente LocalAppData/Lyravoxen/WebView2 e AddBrowserExtensionAsync.
- A revisão com essa integração ainda precisa de build; build #891 verde refere-se ao protótipo Rust anterior e não valida extensão.
- Próximo passo: validar compilação e bundle; depois confirmar em runtime que a extensão aparece em GetBrowserExtensionsAsync/IsEnabled e testar filtros de página. Atenção: API de extensão existe no WebView2, mas não há ainda confirmação de compatibilidade integral das APIs usadas pelo uBOL.
- Branch feature/offline-library-on-redesign; main intocada. Não solicitar ao usuário que prove novamente presença de anúncios.


## Atualização — 27/11/2026: teste real do uBlock Origin Lite não funcionou

- Usuário testou build #897 e a captura mostra anúncio em vídeo "Aproxime-se de Deus" (1 de 2), anúncio lateral e anúncio patrocinado; portanto resultado negativo.
- Build #897 compilou e empacotou, mas não comprova que a extensão foi ativada nem que suas regras foram aplicadas.
- Código atual chama AddBrowserExtensionAsync mas não confirma com GetBrowserExtensionsAsync, IsEnabled ou diagnóstico de regras. Status da extensão é sobrescrito depois pela mensagem "Player pronto". Painel "81.671 regras / 0 solicitações bloqueadas" mede apenas o bloqueador legado, não uBOL.
- Próxima ação: melhorar diagnóstico e instalação idempotente: consultar extensão já instalada, confirmar ID/nome/IsEnabled, evitar reinstalar, guardar versão/erros/WebView2 Runtime em log local e mostrar estado real no painel. Validar DNR separadamente antes de nova compilação para o usuário.
- Não pedir novas provas; usuário já mostrou anúncios em três capturas e quer avançar. Não declarar bloqueio resolvido.
- Manter este arquivo e MEMORIA_CONTINUA.md sempre atualizados. Branch feature/offline-library-on-redesign; main intocada.


## Atualização — 27/11/2026: nova captura confirma anúncios ainda presentes

- Nova captura do usuário na aba Próxima música, busca "harpa crista 15": anúncio em vídeo Amazon Prime/Blade Runner 2099, marcado "Patrocinado · 2 de 2", duração 0:06 e reprodução em 0:03/0:06; anúncio patrocinado lateral também visível.
- Conclusão: uBlock Origin Lite integrado não resolveu o problema. Não solicitar novas provas.
- Próxima etapa antes de nova compilação: instrumentar GetBrowserExtensionsAsync, confirmar ID/nome/versão/IsEnabled; instalar apenas quando ausente; registrar versão WebView2, caminho e HRESULTs; separar estado real da extensão do contador legado; validar DNR com fixture controlada. Se DNR funcionar e anúncios continuarem, estudar outra solução open source compatível em vez de repetir a mesma abordagem.
- Não declarar proteção eficaz nem pedir novo teste até termos diagnóstico técnico. Build #897 verde apenas para compilação/empacotamento; teste real falhou.
- Manter este arquivo e MEMORIA_CONTINUA.md atualizados. Branch feature/offline-library-on-redesign; main intocada.
# ESTADO ATUAL — RETOMADA PRIORITÁRIA (27/11/2026)

> Leia esta seção antes de continuar. Resumo do estado real; histórico abaixo preservado.

## Decisão atual do usuário
A frente de bloqueio de anúncios do YouTube está **PAUSADA**. O usuário precisa avançar para outras funcionalidades e não quer mais consumir tempo com compilações sem evidência de eficácia. Não prometer remoção de anúncios do YouTube.

## Repositório
- https://github.com/jabesguitar/Lyravoxen
- Branch: `feature/offline-library-on-redesign`
- `main` intacta.
- Build #901 passou, mas não valida eficácia de anúncios.

## Frente de anúncios — estado
- Tentativas feitas: bloqueador por domínios/listas, filtros de endpoints, protótipo Brave adblock-rust e extensão uBlock Origin Lite no WebView2.
- Testes do usuário confirmaram anúncios patrocinados em vídeo e na lateral; objetivo não alcançado.
- Não há garantia técnica de que uBlock Lite ou botão de controle remova anúncios in-stream/first-party do YouTube.
- Ideia sugerida pelo usuário: botão redondo uBlock abaixo do controle de tema na aba Próxima música.
- Um botão pode controlar habilitar/desabilitar uma extensão que esteja instalada no WebView2, mas isso não equivale a bloquear anúncios. **Não implementar como indicador de proteção eficaz.**
- Nenhum botão foi implementado nesta decisão.
- Preservar código experimental; não apagar ou reverter sem necessidade.
- Estado: PAUSADO até existir solução com evidência técnica suficiente.

## Outras implementações existentes
- Biblioteca Offline com pasta configurável e subpastas Musicas/Letras/Dados/Temporarios.
- Arquivo independente de letras/sincronizações e recuperação por ID/URL.
- Integração de armazenamento com Pedidos; importação MP3/WAV e fluxo para URL direta autorizada.
- Retenção configurável 5/7/14/30 dias.
- Barra global de reprodução offline e seleção visual em listas.
- Redesign de Pedidos recebidos e Controle pelo celular.
- Ajustes no painel Próxima música e lista da Biblioteca; alguns cenários ainda dependem de validação manual consolidada.

## Próxima ação
Perguntar/seguir a próxima funcionalidade do Lyravoxen que o usuário deseja priorizar. Não voltar à frente de anúncios por iniciativa própria. Manter `MEMORIA_CONTINUA.md` e `RETOMADA_ATUAL.md` atualizados após mudanças significativas.

## Regras
- Não pedir provas já fornecidas.
- Distinguir implementado, compilado, testado e aprovado.
- Não entregar builds intermediários como se fossem finais.
- Não alterar `main` sem autorização.
- Preservar o histórico abaixo desta seção.

---
---

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


## Continuação executada — 26/11/2026

### Estado atual da implementação
- Branch preservada: `feature/offline-library-on-redesign`. A `main` não foi alterada.
- Criado o projeto compartilhado `src/Lyravoxen.Offline`, sem dependência de WPF, para permitir testes funcionais multiplataforma.
- Implementado `OfflineMediaService` para download de URLs diretas autorizadas e importação local MP3/WAV:
  - limite padrão de 50 MB, timeout de 45 segundos, cancelamento e remoção de arquivos temporários após falha;
  - validação HTTP/HTTPS, MIME quando informado, extensão e assinatura MP3/WAV;
  - validação DNS/IP público, bloqueio de redes privadas e redirecionamentos limitados; conexão usa o IP público validado para reduzir risco de DNS rebinding;
  - hash SHA-256 e deduplicação por conteúdo;
  - links do YouTube são recusados como fonte de arquivo de áudio.
- Aba Pedidos: URLs diretas de áudio são baixadas; links somente do YouTube explicam a limitação e oferecem seleção de arquivo autorizado local. A cópia é associada ao pedido e à Biblioteca.
- Biblioteca: aceita URL direta ou importação local. Adicionado editor de título, intérprete e letra na própria aba Biblioteca, sem depender da aba Cadastrar letras. A sincronização existente é preservada.
- Pastas distintas podem referenciar a mesma mídia deduplicada sem duplicar os bytes.
- Expiração: limpeza a cada minuto e na inicialização. O arquivo só é excluído quando não há outra referência ativa na Biblioteca ou em Pedidos; fichas, letras e metadados permanecem.
- Aba Próxima música: painel de comandos reorganizado em layout compacto, mantendo a coluna do navegador/WebView2.
- Proteção: política de bloqueio extraída para componente puro e testável. Os domínios protegidos do YouTube/Googlevideo permanecem permitidos; domínios de anúncios podem ser bloqueados e exceções do usuário são respeitadas.

### Testes automatizados adicionados
Projeto: `tests/Lyravoxen.OfflineTests`.
- Importação local WAV e MP3.
- Download direto WAV e MP3 com respostas HTTP controladas.
- Hash/deduplicação e preservação dos bytes.
- Recusa de YouTube como fonte de áudio.
- Bloqueio de DNS privado e redirecionamento para rede privada.
- Redirecionamento HTTPS permitido, limite de tamanho e recusa de HTML.
- Política do navegador: preservar YouTube/googlevideo, bloquear domínio de anúncio, respeitar exceções e evitar correspondência parcial.
- Expiração: preservar arquivo ainda referenciado por outra pasta ou pedido e permitir excluir quando não houver referências.

### Evidência de CI
- Run `45888141360`, commit `1c888e98`: job `cloud-worker-tests` concluído com sucesso. Serviço offline, cobertura da Ajuda, C# static checks, testes do Worker e página remota mobile aprovados. O teste offline nessa revisão cobria os fluxos de WAV, URL direta, redirecionamento, limite de tamanho e política do navegador; ainda não incluía os testes de expiração e MP3 acrescentados depois.
- Run `45887612874`, commit `5003f831`: jobs `cloud-worker-tests` e `build` concluídos com sucesso. A compilação Windows confirmou a integração inicial do serviço com Pedidos/Biblioteca e o timer de expiração, mas ocorreu antes do editor da Biblioteca e da compactação final da aba Próxima música. Não é a compilação final desta rodada.
- Run `45888415155`, commit `2b9f8418`: no último acompanhamento, o teste offline, cobertura da Ajuda, C# static checks e testes do Worker estavam aprovados; Playwright / teste mobile ainda estava em execução. Esse run contém os testes adicionais de MP3 e política de expiração. Aguardar conclusão antes de liberar build.

### Limitações que ainda impedem declarar pronto
- Os testes de mídia usam arquivos sintéticos e handlers HTTP controlados; ainda não comprovam reprodução real de MP3/WAV pelo MediaElement do Windows nem download contra diferentes servidores externos.
- A política do bloqueador está testada, mas ainda falta teste de integração com WebView2 real confirmando que a pesquisa, login e reprodução do YouTube permanecem funcionais e que a exceção “Permitir site” funciona em navegação real.
- A nova disposição da aba Próxima música foi validada por XAML/build, mas ainda requer inspeção visual em uma janela real e em diferentes resoluções.
- A execução Windows `45887612874` é evidência de build intermediário, não da versão final com o editor e o layout compactado.
- O workflow contém cron diário `17 10 * * *`, mas GitHub Actions executa `schedule` somente a partir da branch padrão. Como não foi autorizado merge na `main`, a rotina diária ainda não está ativa para esta branch. Os testes continuam sendo executados a cada push e podem ser acionados manualmente pela interface do GitHub.

### Próximas ações
1. Aguardar o término do run `45888415155` e conferir os logs completos dos 21 testes funcionais.
2. Fazer uma compilação Windows consolidada com todas as alterações atuais; corrigir qualquer falha de build e repetir.
3. Atualizar este registro com o SHA final, os jobs e os artefatos reais.
4. Só então preparar a única compilação para a rodada de testes do usuário, com checklist específico de navegação real, reprodução offline, expiração e editor da Biblioteca.
5. Não fazer merge na `main` sem aprovação explícita.


## Resultado da compilação consolidada — 26/11/2026

<badge color="success">CI e build Windows aprovados</badge>

- Workflow: [45889091270](https://github.com/jabesguitar/Lyravoxen/actions/runs/45889091270)
- SHA validado: `76e1f018a9e84c65505de05064eb50534ae0cd86`
- Branch: `feature/offline-library-on-redesign`
- Job `cloud-worker-tests`: sucesso.
  - 21 verificações do serviço offline e políticas de navegador/expiração.
  - Cobertura da Ajuda: sucesso.
  - C# static checks: sucesso.
  - Cloudflare Worker tests: sucesso.
  - Playwright Chromium / mobile remote page: sucesso.
- Job `build`: sucesso.
  - Geração de ícone e splash compacta: sucesso.
  - Restore e `dotnet publish` Windows x64: sucesso.
  - Cue timeline tests: sucesso.
  - ZIP portátil: sucesso.
  - Instalador Inno Setup: sucesso.
  - Upload dos três artefatos: sucesso.

### Artefatos desta compilação
- `Lyravoxen-Windows-x64` — artifact ID `15150797165` — 76,260,202 bytes.
- `Lyravoxen-Windows-x64-portable` — artifact ID `15150578815` — 76,294,856 bytes.
- `Lyravoxen-Installer-Windows-x64` — artifact ID `15150673234` — 53,834,443 bytes.
- Os três artefatos constavam como disponíveis e não expirados no momento da consulta.

### Correção realizada durante a compilação
- A primeira tentativa completa falhou por uma referência ambígua a `HorizontalAlignment.Right` no editor da Biblioteca.
- Corrigido para `System.Windows.HorizontalAlignment.Right`.
- A compilação seguinte no SHA acima passou por completo.

### Situação para a rodada única do usuário
- A compilação Windows consolidada foi gerada e está disponível nos artefatos do workflow.
- Ainda não há comprovação manual em um PC com WebView2 real de anúncios, pesquisa/login/reprodução do YouTube; a política de bloqueio foi testada isoladamente.
- A reprodução real de MP3/WAV offline e o comportamento visual do editor/layout em diferentes resoluções também dependem da validação manual no computador anfitrião.
- Não fazer merge na `main` sem aprovação explícita.
- Rotina diária: cron está no workflow da branch, mas GitHub Actions só dispara `schedule` na branch padrão. Como a `main` permanece intacta, os testes diários ainda não estão ativos; o CI roda a cada push e pode ser iniciado manualmente pela interface do GitHub.

### Próxima ação
Entregar ao usuário esta única compilação consolidada e o checklist manual final. Se os testes manuais forem aprovados, registrar a aprovação antes de considerar merge na `main`.


## Revisão corretiva — Pedidos, Biblioteca Offline e anúncios — 26/11/2026

### Solicitações do usuário
- A aba Próxima música ainda mostra anúncios no início do vídeo.
- Pedidos e Biblioteca Offline não estavam concluindo o fluxo esperado para links do YouTube; o usuário relatou dificuldade em baixar, salvar em pasta e renomear/organizar músicas.
- Exibir IP/endereço de origem de quem usa o formulário público, com bloqueio e desbloqueio para reduzir spam/sabotagem.
- Fazer uma única revisão ampla e uma única rodada manual, sem builds vazios intermediários.

### Limites técnicos e de plataforma
- Não implementar extração de áudio do YouTube nem contorno de anúncios/restrições. O player incorporado pode exibir anúncios servidos pela própria origem do YouTube; o bloqueador de domínios de terceiros não garante removê-los.
- Para offline, são suportados: arquivo MP3/WAV local autorizado ou URL HTTPS direta que entregue o arquivo de áudio. URL de página do YouTube não é URL direta de áudio.
- MAC não é disponibilizado ao servidor quando alguém acessa um site pela internet; o Worker pode registrar o IP público fornecido pelo Cloudflare. Não exibir campo MAC fictício.
- O código do Worker foi atualizado na branch, mas o serviço em produção `lyravoxen-pedidos.ferkt319.workers.dev` não foi implantado por esta sessão: não há conexão autorizada com a conta Cloudflare. Para ativar IP/rate-limit na URL pública, é necessária uma implantação única do novo código pelo painel Cloudflare. Pedidos antigos, criados antes do Worker novo, podem não ter IP.

### Implementações desta rodada
- Worker Cloudflare:
  - captura e armazena IP público `CF-Connecting-IP` junto com texto e horário do pedido;
  - bloqueio/desbloqueio autenticado por IP via `/bloqueios`;
  - limite básico de 5 envios por IP por janela de 10 minutos, além da fila global de até 150 pedidos;
  - aviso no formulário sobre registro do IP público e horário para prevenção de abuso, esclarecendo que MAC não é coletado;
  - documentação orienta a atualizar o Worker existente sem recriar KV ou trocar `ADMIN_TOKEN`.
  - KV é eventualmente consistente; o rate-limit no Worker é proteção básica. Para ataques concorrentes/coordenados, complementar com regra Rate Limiting no Cloudflare.
- App Pedidos:
  - sincroniza a lista de bloqueios remotos quando abre a janela;
  - exibe IP público por pedido quando o Worker atualizado o fornece;
  - oferece Bloquear este IP / Desbloquear este IP e Atualizar bloqueios da internet;
  - se IP vier vazio, avisa que o Worker público pode estar desatualizado;
  - normaliza e valida endereços IP ao cadastrar bloqueios.
- Biblioteca:
  - importar URL direta que falhe agora oferece selecionar arquivo MP3/WAV local;
  - colar URL do YouTube apresenta explicação e abre diretamente o seletor de arquivo autorizado;
  - botão de importação continua aceitando múltiplos MP3/WAV;
  - seleção de destino ao salvar pedido offline; não fixa tudo numa pasta genérica;
  - novos comandos Mover para outra pasta e Remover desta pasta (o segundo preserva o arquivo de áudio);
  - seletor permite criar nova pasta;
  - busca inclui título, intérprete e compositor;
  - renomear pasta valida nomes duplicados;
  - adicionar vídeo atual à pasta usa identidade exata (ID do vídeo ou caminho da mídia offline), preservando metadados;
  - SaveLibrary agora retorna sucesso/falha; operações críticas não exibem sucesso falso quando a persistência falha.
- Ajuda atualizada para os novos botões e comportamento.

### Validação final
- Workflow: [45894030900](https://github.com/jabesguitar/Lyravoxen/actions/runs/45894030900)
- SHA: `4f39f92562b6eb470b16be1d3513e9d1ecbe8543`
- Branch: `feature/offline-library-on-redesign`
- Job `cloud-worker-tests`: sucesso.
  - Serviço offline: 21 verificações aprovadas (MP3/WAV, URL direta, deduplicação, SSRF, redirecionamentos, limite de tamanho, política de anúncios e expiração compartilhada).
  - Worker: 44 testes aprovados, incluindo bloqueio/desbloqueio por IP e rate-limit.
  - Ajuda: cobertura aprovada.
  - C# static checks: aprovados.
  - Formulário público em Chromium: 25 testes aprovados.
  - Página mobile em Chromium: 112 testes aprovados.
- Job `build`: sucesso.
  - Ícone e splash: sucesso.
  - Restore e `dotnet publish` Windows x64: sucesso.
  - Cue timeline tests: sucesso.
  - ZIP portátil: sucesso.
  - Instalador Inno Setup: sucesso.
  - Upload dos três artefatos: sucesso.
- Artefatos gerados:
  - `Lyravoxen-Windows-x64`: ID `15153435863`, 76,266,769 bytes.
  - `Lyravoxen-Windows-x64-portable`: ID `15153276186`, 76,302,238 bytes.
  - `Lyravoxen-Installer-Windows-x64`: ID `15153231348`, 53,845,674 bytes.
  - Expiração prevista dos artefatos: 24/02/2027.
- `main` continua no SHA `2051135a6e4915c13dda5730fa0248ed7dbcfe2f`; não foi alterada nem recebeu merge.

### Limitações ainda abertas
- Anúncios in-stream/first-party do YouTube continuam fora do alcance garantido do bloqueador; não prometer reprodução YouTube sem anúncios.
- O Worker em produção precisa ser atualizado manualmente uma única vez para IP e bloqueios remotos aparecerem nos pedidos futuros.
- Testes automatizados de mídia usam fixtures sintéticas e servidor HTTP controlado; reprodução real via WebView2/MediaElement e navegação real no PC anfitrião ainda exigem validação do usuário.
- O pacote está pronto para uma única rodada manual no PC anfitrião; não pedir testes de builds intermediários.

### Próxima ação
1. Usuário instala a versão consolidada.
2. Uma única implantação do Worker atualizado no Cloudflare, mantendo namespace KV e segredo existentes.
3. Uma única rodada manual com MP3/WAV real, salvar/mover/renomear, pedido público com IP e navegação YouTube.
4. Registrar os resultados antes de qualquer merge na `main`.

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

### Regra vigente
Trabalhar somente em IP/bloqueios, download/Biblioteca Offline e proteção de anúncios na aba Próxima Música. Não alterar o que já funciona, não fazer redesign e não tocar na branch `main`. Esta regra mais recente prevalece sobre pendências antigas de redesign registradas acima.

### Alteração aplicada
- Commit `78e88d6942a578ac4491b94b577fe9185c2b3211`, arquivo `src/Lyravoxen.Desktop/MainWindow.Requests.cs`.
- Para pedidos com URL do YouTube, o botão **Baixar offline** agora explica que o link não é arquivo MP3/WAV baixável diretamente e pergunta se o operador deseja associar um arquivo autorizado que já possua.
- Se responder Não, não abre seletor. Se responder Sim, mantém o fluxo existente de importação/associação ao pedido e Biblioteca.
- URLs não-YouTube continuam tentando download direto. Não houve extração de áudio nem alteração do player.

### Verificações
- Branch confirmada: `feature/offline-library-on-redesign`; comparação com `main`: 222 commits à frente, 0 atrás. Nenhum merge foi feito.
- Build #810 passou antes desta alteração; não conta como validação do commit novo.
- Push nesta branch executa `cloud-worker-tests`; o job Windows `build` só executa em `workflow_dispatch` ou na `main`. Workflow não foi alterado.

### Pendências / próxima ação
- [ ] Conferir CI acionado pelo commit `78e88d6`.
- [ ] Após CI verde, executar build Windows consolidado via workflow manual.
- [ ] Atualizar Worker existente no Cloudflare mantendo KV `PEDIDOS` e secret `ADMIN_TOKEN`; testar IP/bloqueio/desbloqueio em produção.
- [ ] Testar reprodução offline real da cópia autorizada exata.
- [ ] Manter explícito que anúncios first-party/in-stream do YouTube não podem ser garantidamente removidos sem comprometer o player.
- Continuar auditoria da Biblioteca e retenção sem reescrever a limpeza já existente.


## 27/11/2026 — Build consolidado #822 concluído; iniciar rodada única de testes

### Build e CI
- Workflow [Windows build #822](https://github.com/jabesguitar/Lyravoxen/actions/runs/45942283747) concluído com sucesso no commit `eeb1f78d433fe09d46a7448b91023ef3ef256c04`.
- `cloud-worker-tests`: todos os passos passaram, incluindo mídia offline, cobertura da Ajuda, C# estático, Worker, formulário público e página remota móvel.
- `build`: Restore, publish Windows x64, teste da linha do tempo, ZIP portátil, instalador Inno Setup e uploads passaram.
- Correção aplicada após log do usuário: `using System.IO;` nos arquivos `YoutubeAudioDownloadService.cs`, `PermanentLyricsArchive.cs` e `MainWindow.OfflineStorage.cs`.

### Artefatos disponíveis (expiração 25/02/2027)
- Instalador Windows x64 — artifact ID `15173087837`; SHA-256 `98203e478aed295ab6d7b5ed61da3b6462a0d2e20a7486536cde002a2ff03bad`.
- Pasta Windows x64 — artifact ID `15172753278`; SHA-256 `f1dbf9e007935adcdedfb3be252766572f865de75e4e07e57045afba59cd3c12`.
- ZIP portátil — artifact ID `15172354465`; SHA-256 `680ce96f6fd6322f62a574fb9fc832e81fd5946367f7b7291360d84a2e0a838e`.

### Instrução para a rodada manual consolidada
1. Abrir a execução #822 e, na seção Artifacts, baixar `Lyravoxen-Installer-Windows-x64`.
2. Extrair o ZIP baixado e executar `Lyravoxen-Setup-Windows-x64.exe`. Fechar o Lyravoxen antes de instalar. Não precisa baixar os três pacotes; o instalador é o recomendado.
3. Executar a lista abaixo sem apagar a biblioteca atual nem dados anteriores.

### Checklist de testes do usuário
- [ ] Abrir o aplicativo e confirmar que inicia normalmente, sem erro ou tela vazia.
- [ ] Configuração de armazenamento: abrir a opção de pasta da Biblioteca Offline; escolher/criar uma pasta dedicada; confirmar que o app cria `Musicas`, `Letras`, `Dados` e `Temporarios` dentro dela.
- [ ] Cadastro de letra: abrir uma música com letra e cues, salvar, fechar e reabrir; confirmar que letra e sincronização continuam registradas.
- [ ] Arquivo de áudio: associar/importar um MP3 ou WAV autorizado; confirmar que aparece na Biblioteca Offline e reproduz.
- [ ] Persistência: remover somente a cópia de áudio (não apagar a ficha/letra pelo app, se houver essa opção); baixar/importar novamente a mesma gravação; confirmar que a letra continua vinculada.
- [ ] Pedidos recebidos: abrir um pedido e clicar em **Baixar offline**; confirmar a resposta apresentada e que o arquivo fica na pasta configurada. Para link YouTube, anotar a mensagem/resultado exato; não considerar download concluído se não houver arquivo reproduzível.
- [ ] Preparar próxima música: selecionar a cópia offline vinculada ao pedido; desligar a internet; tocar a próxima e confirmar reprodução local da gravação exata.
- [ ] Retenção: conferir a validade selecionada e que a limpeza não apaga letras/metadados; não alterar a data do sistema para simular expiração.
- [ ] Próxima Música: navegar, pesquisar e reproduzir; confirmar que o player continua funcionando com proteção ligada.
- [ ] Proteção de anúncios: registrar se anúncios/trackers são bloqueados; anúncios in-stream do próprio YouTube podem continuar aparecendo e não devem ser considerados falha se o player permanecer funcional.
- [ ] Pedidos pela internet: verificar recebimento dos pedidos e se o IP aparece; conferir bloqueio/desbloqueio somente após atualização do Worker em produção.

### Limites e pendências
- O build #822 prova compilação e testes automatizados, não prova download real do YouTube, reprodução offline no PC nem publicação do Worker Cloudflare em produção.
- O Worker existente ainda precisa receber o código atualizado pelo usuário no painel Cloudflare, preservando binding KV `PEDIDOS` e secret `ADMIN_TOKEN`; não criar outro Worker nem apagar recursos.
- Não fazer merge na `main`. Branch de teste permanece `feature/offline-library-on-redesign`.
- Após o usuário executar os testes, registrar cada resultado (aprovado/falhou/não testado) e corrigir somente o que falhar, sem repetir o checklist inteiro desnecessariamente.


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


## Situação atual — 27/11/2026 (Build #855)

- Build #855 concluído com sucesso: https://github.com/jabesguitar/Lyravoxen/actions/runs/46018345034
- Instalador Windows x64 disponível no artefato `Lyravoxen-Installer-Windows-x64` (ID 15207121706). ZIP portátil: `Lyravoxen-Windows-x64-portable` (ID 15207491300). Artefatos expiram em 25/02/2027.
- Branch: `feature/offline-library-on-redesign`; `main` não foi alterada.
- Próxima etapa: instalar o artefato do build #855 e validar em execução real: (1) música offline continua ao trocar de abas; (2) pausar/retomar/parar/volume/posição; (3) seleção destacada no histórico e na Biblioteca; (4) link direto MP3/WAV, escolha de destino e registro na coleção; (5) retenção e preservação da letra/dados.
- Ainda pendente: publicação/validação do Worker Cloudflare em produção. Os testes automatizados do código passaram, mas produção não foi atualizada por esta execução.
- Não fazer merge em `main` sem autorização expressa.


## Atualização — 27/11/2026: lista da Biblioteca e janela de pedidos

- Corrigida a troca de pastas para limpar o filtro antigo antes de carregar as músicas da coleção selecionada.
- Comandos inferiores da coluna Músicas da coleção limitados em altura com rolagem própria para não ocultar a lista de músicas.
- Janela Pedidos recebidos redesenhada: janela maior, cabeçalho/contador, configurações recolhidas, cards por pedido com conjunto/cantor, título, link e botões de ação legíveis. Botão Copiar link removido dos cards; bloqueio de IP permanece secundário.
- Build #862: https://github.com/jabesguitar/Lyravoxen/actions/runs/46025648719 (em andamento no momento deste registro).
- Próxima etapa: aguardar #862. Se verde, baixar instalador e validar: trocar entre pastas com e sem filtro; conferir lista visível; abrir Pedidos recebidos e conferir cards, texto e botões. Não mexer na main.


## Atualização — 27/11/2026: redesign Controle pelo celular

- Janela Controle pelo celular redesenhada para combinar com Pedidos recebidos: fundo azul-escuro, cartões, QR destacado, endereço legível/copiar, PIN em destaque e botão Gerar novo PIN. Mantidos QR, endereços, PIN e instruções existentes.
- Commit: c4b7796e38dd502c4a276a603987dc0c331be18a.
- Build #866: https://github.com/jabesguitar/Lyravoxen/actions/runs/46029745807 (em andamento).
- Próxima ação: aguardar build. Se verde, baixar instalador único e validar visualmente as telas Biblioteca Offline, Pedidos recebidos e Controle pelo celular. Branch feature/offline-library-on-redesign; não alterar main.

- Observação operacional: o push de documentação cancelou a execução concorrente #866; acompanhar a execução mais recente disparada pelo commit final de documentação antes de liberar o instalador.


## Atualização — 27/11/2026: filtro conservador de endpoints de anúncios YouTube

- Implementada política de requisições que bloqueia caminhos conhecidos `/pagead/` e `/api/stats/ads` em `youtube.com` e subdomínios, além da lista de hosts já existente.
- Preservados API IFrame, páginas normais do YouTube, `googlevideo.com` e exceções de domínio permitidas pelo usuário.
- Testes adicionados para confirmar bloqueio desses endpoints e preservação dos recursos de reprodução.
- Commits: `c026684` (política), `b27d130` (integração WebView2), `c554d1c` (testes).
- Build #870 passou antes dessas alterações, mas não valida os commits novos. Acompanhar a execução disparada pelo commit mais recente.
- Próxima ação: confirmar workflow verde; instalar/abrir o app e testar vários vídeos na aba Próxima música, incluindo início, reprodução contínua, troca de vídeo e carregamento de letras. Registrar se anúncios pré-roll ainda aparecem ou se algum vídeo/player falha.
- Limite conhecido: não é bloqueio garantido de anúncios in-stream/servidos dinamicamente; nenhuma alteração no player principal foi feita.
- Branch `feature/offline-library-on-redesign`; `main` permanece intocada.


## Atualização — 27/11/2026: correção CS0103 BrowserAdBlocker

- Log recebido mostrou nove erros CS0103: `BrowserAdBlocker` não existia no contexto de `MainWindow.xaml.cs`.
- Causa: `BrowserAdBlocker.cs` estava com namespace/classe errados (conteúdo de `BrowserRequestPolicy`), removendo o serviço esperado.
- Serviço restaurado em `Lyravoxen.Services`, com integração ao novo filtro de endpoints YouTube preservada.
- Commit: `bfebc4b14ebba8deea3801cf63e7295d447b53d9`.
- Build #876: https://github.com/jabesguitar/Lyravoxen/actions/runs/46034132906 (em andamento ao registrar).
- Próxima ação: aguardar #876 e, se falhar, abrir logs do job build e corrigir a próxima falha. Avisos CS8602/CS4014 permanecem não fatais e fora do escopo desta correção.
- Branch feature/offline-library-on-redesign; main intocada.


## Atualização — 27/11/2026: teste real confirmou anúncios ainda presentes

- Usuário testou a compilação e relatou dois anúncios já na primeira música reproduzida na aba Próxima música.
- Conclusão: filtro pontual por caminhos de anúncio não resolveu pré-roll/in-stream; funcionalidade ainda não atende ao objetivo.
- Build #878 passou: https://github.com/jabesguitar/Lyravoxen/actions/runs/46034192649. Build verde não comprova bloqueio de anúncios.
- Pesquisa de alternativa: estudar integração do motor upstream `brave/adblock-rust` (MPL-2.0), que oferece filtro ABP, bloqueio de rede, cosmético e scriptlets. Não copiar o wrapper ZWebWrap (GPL-3.0-or-later) como um todo.
- Próxima ação: fazer protótipo isolado da integração Rust/C ABI + C# P/Invoke/WebView2; medir compatibilidade, build e impacto em CPUs antigas antes de conectar ao player. Criar testes com URLs/tipos de recurso e testes manuais com YouTube.
- Limite: nem motor ABP/uBO garante identificar anúncios inseridos no mesmo fluxo de mídia do vídeo.
- Nenhuma alteração adicional no código após o teste do usuário. Não mexer na main.


## Atualização — 27/11/2026: correção CS0117 WebSocket

- Build #886 falhou porque o enum WebView2 1.0.2903.40 não define `CoreWebView2WebResourceContext.WebSocket`.
- Removido esse case; os demais tipos de requisição permanecem e recursos sem mapeamento usam `other`.
- Commit: `db889fbb79c6e292a21b9505259e4f22f8256e76`.
- Acompanhar novo workflow. Próximo objetivo: validar compilação do Rust e .NET, DLL nativa incluída na publicação e testes; não considerar anúncios resolvidos antes de teste real.


## Atualização — 27/11/2026: teste visual falhou

- Duas capturas enviadas pelo usuário mostram anúncios em vídeo Pantene e Mercado Livre, além de anúncios patrocinados laterais na aba Próxima música.
- Conclusão: proteção ainda não atende ao requisito. Não solicitar novas provas.
- O motor ABP em implementação não basta por si só: falta validar carregamento nativo em runtime e avaliar filtros cosméticos/scriptlets para interface; anúncios in-stream são um problema separado e não há garantia de bloqueio por URL.
- Próxima ação: conferir build mais recente e a inicialização efetiva da DLL/listas; depois desenhar uma abordagem separada para elementos patrocinados da página e anúncios dentro do player. Testar antes de pedir novo teste ao usuário.
- Não apresentar a proteção como concluída nem entregar versão como solução. Branch feature/offline-library-on-redesign; main intocada.


## Atualização — 27/11/2026: protótipo com extensão open source no WebView2

- Mudança de abordagem após evidência visual de anúncios: integrar extensão pronta, em vez de insistir apenas no motor próprio.
- Extensão escolhida: uBlock Origin Lite (Edge), release 2026.1124.1811, GPL-3.0, distribuída sem modificações.
- Workflow baixa a versão oficial, extrai a pasta com manifest.json, inclui no pacote Windows e obtém o LICENSE.
- Inicialização WebView2 corrigida para criar CoreWebView2Environment com AreBrowserExtensionsEnabled=true, perfil persistente LocalAppData/Lyravoxen/WebView2 e AddBrowserExtensionAsync.
- A revisão com essa integração ainda precisa de build; build #891 verde refere-se ao protótipo Rust anterior e não valida extensão.
- Próximo passo: validar compilação e bundle; depois confirmar em runtime que a extensão aparece em GetBrowserExtensionsAsync/IsEnabled e testar filtros de página. Atenção: API de extensão existe no WebView2, mas não há ainda confirmação de compatibilidade integral das APIs usadas pelo uBOL.
- Branch feature/offline-library-on-redesign; main intocada. Não solicitar ao usuário que prove novamente presença de anúncios.


## Atualização — 27/11/2026: teste real do uBlock Origin Lite não funcionou

- Usuário testou build #897 e a captura mostra anúncio em vídeo "Aproxime-se de Deus" (1 de 2), anúncio lateral e anúncio patrocinado; portanto resultado negativo.
- Build #897 compilou e empacotou, mas não comprova que a extensão foi ativada nem que suas regras foram aplicadas.
- Código atual chama AddBrowserExtensionAsync mas não confirma com GetBrowserExtensionsAsync, IsEnabled ou diagnóstico de regras. Status da extensão é sobrescrito depois pela mensagem "Player pronto". Painel "81.671 regras / 0 solicitações bloqueadas" mede apenas o bloqueador legado, não uBOL.
- Próxima ação: melhorar diagnóstico e instalação idempotente: consultar extensão já instalada, confirmar ID/nome/IsEnabled, evitar reinstalar, guardar versão/erros/WebView2 Runtime em log local e mostrar estado real no painel. Validar DNR separadamente antes de nova compilação para o usuário.
- Não pedir novas provas; usuário já mostrou anúncios em três capturas e quer avançar. Não declarar bloqueio resolvido.
- Manter este arquivo e MEMORIA_CONTINUA.md sempre atualizados. Branch feature/offline-library-on-redesign; main intocada.


## Atualização — 27/11/2026: nova captura confirma anúncios ainda presentes

- Nova captura do usuário na aba Próxima música, busca "harpa crista 15": anúncio em vídeo Amazon Prime/Blade Runner 2099, marcado "Patrocinado · 2 de 2", duração 0:06 e reprodução em 0:03/0:06; anúncio patrocinado lateral também visível.
- Conclusão: uBlock Origin Lite integrado não resolveu o problema. Não solicitar novas provas.
- Próxima etapa antes de nova compilação: instrumentar GetBrowserExtensionsAsync, confirmar ID/nome/versão/IsEnabled; instalar apenas quando ausente; registrar versão WebView2, caminho e HRESULTs; separar estado real da extensão do contador legado; validar DNR com fixture controlada. Se DNR funcionar e anúncios continuarem, estudar outra solução open source compatível em vez de repetir a mesma abordagem.
- Não declarar proteção eficaz nem pedir novo teste até termos diagnóstico técnico. Build #897 verde apenas para compilação/empacotamento; teste real falhou.
- Manter este arquivo e MEMORIA_CONTINUA.md atualizados. Branch feature/offline-library-on-redesign; main intocada.


## Requisito atual prioritário — sequência de letra (27/11/2026)

O usuário quer DUAS opções:

- **Automática (preferida):** colar a letra original sem reorganizar; o sistema reconhece no áudio a ordem real das frases e as repetições do coro, mesmo quando o coro aparece uma única vez no texto do site.
- **Manual (alternativa):** editor visual de blocos selecionáveis, onde o usuário marca trechos e monta a ordem real; pode repetir um bloco sem duplicar o texto-fonte; salva sequência vinculada à música para reutilização.

Regras:
- Preservar letra original, blocos-fonte, sequência de ocorrências e tempos como dados distintos.
- Não exigir que o usuário rotule estrofe/coro.
- Sequência manual precisa persistir, ser recuperável e editável.
- O matcher atual `LiveLyricsMatcher` é sequencial/monotônico e não resolve repetições fora de ordem; não anunciar automático como pronto sem desenvolver/testar.
- Atualizar a aba Ajuda junto com os recursos, incluindo uso, atalhos e recuperação.
- Próximo passo: modelar persistência de blocos e ocorrências; implementar/testar organizador manual persistente primeiro; depois desenvolver alinhamento automático com repetição.
- Não entregar somente marcação linear por espaço como solução final.
- Frente de anúncios continua pausada; não misturar com esta etapa.
- Branch feature/offline-library-on-redesign; main intocada.


## Atualização de escopo — 27/11/2026

- Todo o fluxo de preparação e sincronização de letras deve ficar na aba **Cadastrar letras**: entrada da letra, análise automática, organizador manual, repetição de blocos, revisão, salvar e recuperar.
- Não distribuir esse fluxo por outras abas. A aba Ao vivo segue dedicada à reprodução e projeção durante o evento.
- Usuário autorizou retirar botões disfuncionais, não utilizados ou redundantes no escopo trabalhado. Antes de remover, inspecionar eventos, referências e dependências; não apagar controles úteis ou atalhos ativos.
- Simplificar a interface para operação rápida, com funções essenciais claras e Ajuda atualizada para explicar todos os recursos implementados.
- Próxima ação: auditar a aba Cadastrar letras (XAML + code-behind + handlers) e o painel de controles manuais, mapear controles ativos/inativos, depois implementar os dois modos e a limpeza segura em uma rodada coerente.


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
