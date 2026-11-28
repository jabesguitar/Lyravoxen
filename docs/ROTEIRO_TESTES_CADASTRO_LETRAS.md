# Roteiro de testes — Cadastro de Letras e sincronização

**Versão de referência:** Windows build #986, commit `899ed86f1a56f7962b5b89279374b5974f35f74e`  
**Workflow aprovado:** https://github.com/jabesguitar/Lyravoxen/actions/runs/46169606748  
**Branch:** `feature/lyrics-structure-fast-sync`

Este roteiro separa testes de interface, persistência, reprodução e rede. Não misture todos de uma vez: primeiro confirme a linha do tempo e só depois teste a troca online/offline.

## 1. Preparação

1. Abra o workflow #986 no GitHub: https://github.com/jabesguitar/Lyravoxen/actions/runs/46169606748.
2. Na seção **Artifacts**, baixe um dos pacotes:
   - `Lyravoxen-Installer-Windows-x64` para instalar normalmente; ou
   - `Lyravoxen-Windows-x64-portable` para extrair e executar sem instalar.
3. Feche versões anteriores do aplicativo antes de abrir a nova.
4. Escolha uma música de teste com letra sincronizada. O melhor é uma música com várias frases marcadas e intervalos suficientes para perceber o deslocamento.
5. Para os testes de falha de internet, salve antes uma cópia offline válida da mesma música/vídeo na Biblioteca Offline. O arquivo deve corresponder ao mesmo ID do YouTube e não pode estar expirado.

## 2. Testar a pesquisa enquanto a música toca

1. Na aba **Ao vivo**, carregue e inicie uma música.
2. Sem pausar o player, abra **Cadastro de letras** e pesquise pelo título/intérprete.
3. Confira se a janela de pesquisa abre e se os botões do primeiro resultado já aparecem habilitados quando há dados disponíveis.
4. Escolha uma letra e importe o texto ou os tempos.
5. Confirme que a música não voltou ao início nem foi interrompida.
6. Se a letra foi inserida depois do começo, selecione o cartão que corresponde à frase que está sendo cantada e use **Continuar daqui**. A frase selecionada deve receber o tempo atual e a seleção deve avançar para a próxima.

**Resultado esperado:** pesquisa e importação independentes do player; continuidade da reprodução e possibilidade de retomar a marcação pela frase atual.

## 3. Testar a linha do tempo visual

Faça este teste com um cartão que já tenha tempo. Cartões sem marcação deixam a linha do tempo desabilitada de propósito.

1. Em **Cadastro de letras**, crie/carregue os cartões e selecione uma frase que mostre um tempo (por exemplo, `00:24.0`).
2. Olhe abaixo da prévia da projeção. O marcador da linha do tempo deve ficar no segundo salvo dessa frase.
3. Arraste o marcador um pouco para a direita. O texto abaixo deve mostrar uma prévia temporária: segundo original → segundo simulado. A linha deve mostrar também a previsão para a próxima frase marcada.
4. Arraste para a esquerda e confira que a previsão muda junto. A frase grande na prévia continua identificando qual cartão está sendo ajustado.
5. Clique no marcador e use as setas:
   - **← / →:** move 0,1 segundo por toque.
   - **Shift + ← / →:** move 0,5 segundo por toque.
   - **Page Up / Page Down:** move 1 segundo.
6. Clique em **Cancelar prévia**. O marcador e a leitura devem voltar ao horário original. O tempo exibido no cartão da lista não pode ter mudado.
7. Mova o marcador novamente para outro ponto e confira a leitura prévia.
8. Clique em **Aplicar ajuste**. O tempo da frase selecionada deve ser atualizado; as frases marcadas posteriores devem receber o mesmo deslocamento; as anteriores devem permanecer iguais.
9. Confira que o texto de status informa que o ajuste foi salvo.
10. Clique em **Desfazer**. A frase selecionada e as posteriores devem recuperar os tempos anteriores.
11. Aplique novamente, clique em **Salvar cadastro**, feche/reabra a música e confirme que os horários continuam iguais.
12. Selecione um cartão sem tempo: a linha do tempo deve ficar desabilitada e orientar que a frase seja marcada primeiro.

**Resultado esperado:** arrastar e usar teclas só criam prévia; nada é gravado até Aplicar. Cancelar não altera o cadastro. Aplicar salva o ponto selecionado e o deslocamento das frases seguintes; Desfazer restaura.

## 4. Testar os controles de ajuste em segundos

1. Selecione um cartão marcado.
2. Teste `−0,1 s` e `−0,5 s`: a frase selecionada e as posteriores devem ser adiantadas.
3. Teste `+0,1 s` e `+0,5 s`: a frase selecionada e as posteriores devem ser atrasadas.
4. Use Desfazer entre os testes para não acumular deslocamentos acidentais.
5. Confira que uma frase não ultrapassa a frase anterior; o aplicativo mantém um intervalo mínimo de 0,05 s.

**Resultado esperado:** os controles alteram apenas a linha do tempo das letras e preservam a ordem das frases.

## 5. Testar o ajuste progressivo de ritmo

1. Selecione um cartão marcado que tenha várias frases marcadas depois dele.
2. Anote os tempos do cartão selecionado e de pelo menos duas frases posteriores.
3. Clique em **Acelerar 1%**. O tempo da âncora e as frases anteriores devem permanecer; os intervalos entre a âncora e as frases seguintes devem diminuir em 1%.
4. Use **Desfazer**.
5. Clique em **Desacelerar 1%**. O intervalo entre os tempos posteriores deve aumentar em 1%.
6. Use Desfazer e salve/reabra para confirmar persistência quando desejar manter o ajuste.

**Resultado esperado:** ajuste progressivo da linha do tempo das letras. O áudio não deve mudar de velocidade.

## 6. Testar o retorno online/offline

> **Pré-condição obrigatória:** a Biblioteca Offline precisa conter um arquivo válido do mesmo vídeo/ID do YouTube que será tocado. Sem isso, não há fonte local para assumir a reprodução.

1. Confirme que a cópia offline está presente, não expirou e corresponde ao mesmo vídeo.
2. Conecte a internet, abra o vídeo no YouTube e inicie a reprodução.
3. Anote o tempo mostrado pelo player e a frase que está sendo projetada.
4. Desconecte a internet (Wi-Fi ou cabo) sem pausar a música.
5. Aguarde o monitor detectar duas falhas de conexão. O aplicativo deverá iniciar a cópia offline no ponto aproximado em que o vídeo parou e manter a letra associada.
6. Observe a projeção por alguns segundos: não deve voltar à primeira frase.
7. Restaure a internet sem parar a cópia local.
8. Aguarde o monitor preparar o YouTube e confirmar que o vídeo começou. O áudio local deve continuar até essa confirmação; depois o player online deverá retomar no ponto atual.
9. Repita uma vez se houver buffering ou se a rede oscilar.

**Resultado esperado:** YouTube → arquivo local e arquivo local → YouTube sem reinício perceptível e com a frase correspondente. O monitor verifica a conexão em intervalos curtos; a confirmação de retomada online pode levar alguns segundos.

**Teste negativo:** repita com uma música sem cópia offline. O aplicativo deve informar que não existe uma reserva local válida; não deve inventar ou escolher outro áudio.

## 7. Testar a prioridade Brasil

1. Pesquise uma música que tenha mais de uma versão linguística no catálogo.
2. Confira se as letras com sinais de português brasileiro aparecem antes das versões claramente em outros idiomas.
3. Leia o selo da lista: `PT-BR provável`, `Português provável` ou `Outro idioma provável`.
4. Confira o texto antes de importar.

**Limitação:** a ordenação é heurística pelo texto da letra. O catálogo LRCLIB não fornece uma confirmação confiável de país para cada resultado; letras curtas ou multilíngues podem ser classificadas incorretamente.

## 8. Registrar os resultados

Use esta tabela ao testar no computador. Marque **Passou**, **Falhou** ou **Não testado** e anote a música/tempo e o que ocorreu.

| Teste | Resultado | Observação |
|---|---|---|
| Pesquisa com música tocando | Não testado | |
| Continuar daqui | Não testado | |
| Linha do tempo com mouse | Não testado | |
| Linha do tempo com setas | Não testado | |
| Cancelar prévia sem salvar | Não testado | |
| Aplicar e persistir | Não testado | |
| Desfazer ajuste | Não testado | |
| Ajustes ±0,1 s / ±0,5 s | Não testado | |
| Acelerar 1% / Desacelerar 1% | Não testado | |
| Failover YouTube → offline | Não testado | |
| Retorno offline → YouTube | Não testado | |
| Busca Brasil primeiro | Não testado | |

Ao relatar um problema, informe o número do teste, a música usada, o tempo aproximado da faixa e o resultado esperado versus o que aconteceu. Um print da tela e o relatório copiado pelo botão **Relatório** ajudam a diagnosticar sem repetir os testes.

## 9. O que o CI já comprovou e o que não comprovou

- O build #986 comprovou compilação WPF, execução da suíte automatizada de timeline, serviço offline, Ajuda, Worker, formulário de pedidos e página remota mobile.
- O CI não executou a interface em seu computador, não arrastou o controle com mouse real e não simulou a queda de internet durante uma música tocando no WebView2.
- Por isso, os testes das seções 2 a 7 precisam ser feitos no Windows antes de considerar a etapa validada em uso real.
