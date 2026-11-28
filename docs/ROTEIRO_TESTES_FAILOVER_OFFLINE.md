# Roteiro de teste — failover online/offline e retomada de posição

**Código:** 0287a44d05da0f8d76f7a4ae71b6f1bf3af99161  
**Build de referência:** [Windows build #992](https://github.com/jabesguitar/Lyravoxen/actions/runs/46178529269)  
**Branch:** `feature/lyrics-structure-fast-sync`

Este roteiro verifica o comportamento real em seu Windows. O CI aprovou compilação e testes automatizados, mas não consegue provocar uma queda real de internet no seu WebView2.

## 1. Instalar a versão correta

1. Abra o workflow #992: https://github.com/jabesguitar/Lyravoxen/actions/runs/46178529269.
2. Na seção **Artifacts**, baixe `Lyravoxen-Installer-Windows-x64` para instalar normalmente ou `Lyravoxen-Windows-x64-portable` para usar sem instalação.
3. Feche o Lyravoxen antigo antes de abrir esta versão.
4. Não use o pacote de builds anteriores (#986, #989); utilize os artefatos do #992.

## 2. Preparar uma cópia offline reconhecível

1. Abra a aba **Biblioteca Offline**.
2. Confirme que o arquivo MP3/WAV está presente, não expirou e pode ser aberto.
3. A identificação pode ser feita por ID do YouTube ou pelo título/intérprete.
4. Nomes de arquivo como `Trio R3 - Te ver (Play Back).mp3` agora são limpos pelo parser do Lyravoxen: sufixos comuns são retirados e o intérprete/título são separados.
5. Se o nome do arquivo for genérico ou não tiver intérprete, abra a edição da música na biblioteca e preencha o título e o intérprete para corresponder à ficha da música online.
6. Evite manter duas cópias com o mesmo título e sem intérprete identificável. Nessa situação, o aplicativo deliberadamente não escolhe uma delas no automático.

## 3. Usar Diagnóstico antes da reprodução

1. Carregue a música do YouTube que deseja testar.
2. Clique em **🩺 Diagnóstico**.
3. Na parte “Reprodução e failover”, procure:
   - faixa atual e posição;
   - quantidade de frases temporizadas;
   - última sonda do YouTube;
   - **Cópia offline correspondente: SIM**.
4. O resultado deve dizer se a reserva foi reconhecida pelo ID do YouTube ou por título/intérprete único.
5. Se disser **NÃO encontrada**, não provoque a queda ainda: corrija o título/intérprete da cópia offline e execute Diagnóstico novamente.

**Atenção:** LRCLIB “OK” significa apenas que o catálogo de letras respondeu. Para o failover, o importante é a linha específica “YouTube (sonda usada pelo failover)”.

## 4. Testar YouTube → arquivo offline

1. Com o Diagnóstico indicando cópia correspondente **SIM**, inicie a música online.
2. Espere a faixa tocar por pelo menos 20–30 segundos. Anote o segundo mostrado pelo player e a frase projetada.
3. Desconecte o Wi-Fi ou cabo de rede sem pausar a música.
4. O monitor faz sondas curtas e exige duas falhas consecutivas para evitar falso positivo. Aguarde alguns segundos.
5. O status deve indicar que a conexão foi interrompida e que a cópia offline está sendo retomada.
6. Confira se o áudio volta aproximadamente no segundo em que a reprodução online parou.
7. Confira se a letra continua na frase correspondente — não deve limpar o telão nem voltar à primeira frase.

**Resultado esperado:** o MP3/WAV começa após o evento de abertura, recebe o seek para a posição guardada e só então começa a tocar. A projeção atual e os cues da ficha online são preservados.

## 5. Testar arquivo offline → YouTube

1. Deixe a cópia offline tocando, sem encerrar a faixa.
2. Reconecte a internet.
3. O Lyravoxen prepara o vídeo original no segundo atual. O áudio local continua tocando até o YouTube confirmar que começou.
4. Confira se o ponto e a frase projetada permanecem alinhados.
5. Se o YouTube não confirmar a retomada no prazo de segurança, a cópia local deve continuar; o aplicativo tenta novamente depois.

**Resultado esperado:** a mudança de fonte não manda a música ao começo nem deixa a projeção sem letra.

## 6. Testar seleção manual e último ponto salvo

1. Toque uma faixa offline até aproximadamente 00:30.
2. Pause-a ou feche o Lyravoxen normalmente. A posição deve ser salva na ficha da Biblioteca Offline.
3. Reabra o aplicativo e selecione a mesma faixa na Biblioteca Offline.
4. Confira se ela retoma perto do último segundo salvo.
5. Enquanto uma música estiver tocando online, selecione a cópia offline correspondente na biblioteca: a seleção não deve reiniciar o YouTube; a cópia fica vinculada como reserva.
6. Selecione outra faixa offline que já tenha posição salva: ela deve retomar daquele ponto.
7. Quando a música termina normalmente ou é encerrada explicitamente, a posição é zerada para que uma próxima reprodução intencional comece do início.

## 7. Testar Diagnóstico e Relatório

### 🩺 Diagnóstico
Confere Holyrics, microfones, modelo de voz, tentativa de carga da biblioteca nativa Whisper, monitores, LRCLIB, YouTube, posição atual, cues e associação da cópia offline. Se o Whisper falhar, o motivo aparece separado do estado da reprodução.

### 📋 Relatório
Clique logo depois de uma falha. Ele copia os registros recentes e acrescenta faixa, fonte atual, segundo, estado do player, número de frases temporizadas, resultado da última sonda do YouTube e caminho/validade da cópia local. Cole o texto na conversa usando Ctrl+V.

Os dois botões são úteis e devem permanecer. Diagnóstico é uma verificação ativa; Relatório é o registro técnico do que aconteceu.

## 8. Como registrar cada resultado

| Teste | Resultado | Observação |
|---|---|---|
| Diagnóstico encontra a cópia correta | Não testado | |
| YouTube toca online | Não testado | |
| Queda de internet aciona a cópia local | Não testado | |
| Música retoma no segundo aproximado | Não testado | |
| Letra mantém a frase correspondente | Não testado | |
| Retorno da internet volta ao YouTube | Não testado | |
| Retorno preserva posição e letra | Não testado | |
| Seleção manual da mesma faixa não reinicia | Não testado | |
| Reabrir faixa offline retoma ponto salvo | Não testado | |
| Diagnóstico identifica falha Whisper | Não testado | |
| Relatório contém o estado do failover | Não testado | |

Ao relatar, informe o título da música, o segundo aproximado em que caiu a conexão e se o Diagnóstico mostrou a cópia correspondente como SIM ou NÃO. Copie o Relatório logo após o problema. Uma captura de tela do status e da projeção ajuda.

## 9. Limites reais

- Sem cópia local válida e identificável, não há áudio para assumir.
- A associação por título exige uma única correspondência segura; duplicatas ambíguas são recusadas.
- O build #992 passou na compilação e nos testes configurados, mas a troca durante perda real de internet ainda depende desta rodada manual.
- O erro de carregamento nativo Whisper do relatório enviado afeta o recurso de reconhecimento cantado; não é a causa da reprodução de MP3 nem da projeção por cues já temporizados.
- AVX2 indisponível sozinho não identifica a causa da falha Whisper; o Diagnóstico agora testa diretamente a carga do motor.
