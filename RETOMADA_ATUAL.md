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
