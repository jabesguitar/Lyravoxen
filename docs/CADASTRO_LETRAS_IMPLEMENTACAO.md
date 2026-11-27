# Cadastro de letras — especificação funcional e plano de implementação

Atualizado em 27/11/2026. Branch: feature/offline-library-on-redesign.

## Objetivo
Converter a aba Cadastrar letras em editor de sincronização real, com dois modos (automático e manual), salvamento permanente e acesso equivalente pelo controle remoto.

## Requisitos confirmados pelo usuário
1. **Automático:** colar/informar a letra e vincular ao vídeo/áudio; analisar o áudio real, reconhecer frases e posicionar entradas com timestamps. Revisar/corrigir antes de finalizar. Não usar distribuição uniforme como se fosse reconhecimento.
2. **Manual:** exibir cada frase/linha em blocos selecionáveis; seleção por mouse ou teclado/setas; marcar no instante da reprodução; navegar para trás/frente, desfazer, corrigir e concluir.
3. **Estrutura musical:** separar texto-fonte da sequência de execução; permitir refrões/trechos repetidos sem duplicar ou destruir o texto original.
4. **Persistência definitiva:** guardar título, autor, intérprete, URL/ID da gravação, letra integral, sequência de ocorrências e tempos. Reabrir recupera tudo sem recadastrar/ressincronizar.
5. **Controle remoto:** terceira aba Cadastrar músicas ao lado de Ao vivo e Bíblia; mesma biblioteca/serviços e salvamento, com interface responsiva.
6. **Higiene:** todo botão deve executar ação funcional e verificável; remover controles obsoletos/inúteis, sem remover recursos válidos.
7. Manter solução gratuita e leve, compatível com Windows 10/11 e CPUs antigas sempre que tecnicamente viável. Não mesclar na main sem autorização.

## Auditoria do estado existente
| Componente | Existe hoje | Lacuna |
|---|---|---|
| Whisper local | Captura/transcrição incremental em blocos de 5 s durante playback | Não faz alinhamento integral prévio; repetições e trechos não reconhecidos precisam de solução robusta |
| LiveLyricsMatcher / AutoSyncPlanner | Correspondência sequencial e estimativa de linhas puladas | Não garante alinhamento completo nem repetição estrutural de refrões |
| TapSyncPlanner / MainWindow.TapSync | Um toque por estrofe; Space/botão; salva cues | Não há cartões por frase/linha nem navegação por setas/mouse como pedido |
| PermanentLyricsArchive | JSON com backup, chave por ID/URL/metadados e cues | Auditar todos os caminhos de escrita/recuperação e integrar sequência de ocorrências |
| Controle remoto | Colagem de letra para atual/próxima e ações básicas de toque/estimativa | Falta aba de cadastro e editor completo compartilhando dados do desktop |

## Pesquisa técnica — candidatos, ainda sem decisão
- sherpa-onnx (k2-fsa): verificar forced alignment, português, runtime Windows/CPU e tamanho dos modelos.
- WhisperX: referência de timestamps por palavra/alinhamento; avaliar dependências Python/PyTorch e impacto no tamanho/uso de CPU.
- karaoke-subs: estudar pipeline de alinhamento de letra com áudio, inclusive vocais isolados, sem assumir compatibilidade/licença.
- aubio e Sonic Visualiser/MATCH Vamp: investigar como ferramentas auxiliares de análise/inspeção temporal.

## Arquitetura de trabalho proposta (sugestão técnica, não decisão aprovada)
- Modelo separado para texto-fonte e sequência de execução: cada ocorrência aponta para bloco/linha original e tem posição/tempo próprios; refrão pode reaparecer várias vezes.
- Serviço de alinhamento substituível, para testar e trocar o motor sem redesenhar o editor.
- Modo automático produz timestamps e confiança por trecho; baixa confiança vira pendência de revisão, nunca tempo fictício silencioso.
- Editor manual corrige timestamps automáticos.
- Persistência atômica com backup e chave estável por gravação/ID, integrada à biblioteca e ao arquivo permanente.
- API remota protegida por PIN expõe operações sobre o mesmo serviço/modelo; sem duplicar biblioteca em armazenamento independente do navegador.

## Testes de aceitação
- Música cantada em português; playback sem voz; introdução instrumental; pausas; ruído; versos repetidos; refrão repetido; retorno de seção.
- Automático: frases reconhecidas nos instantes corretos; incertezas visíveis; revisão manual possível.
- Manual: cada frase selecionável por clique e setas; marcação no tempo real; desfazer/voltar/avançar/reordenar; salvar e reabrir.
- Persistência: fechar/reabrir, carregar outra música e recuperar a gravação exata, mesmo sem áudio local.
- Remoto: cadastrar no celular e ver no Windows; editar no Windows e ver no celular.
- Interface: nenhum botão sem ação ou promessa enganosa; controles essenciais acessíveis no celular.
- CI verde não substitui testes funcionais.

## Estado
- Especificação confirmada em 27/11/2026.
- Auditoria inicial dos arquivos principais feita.
- Implementação desta nova etapa ainda não iniciada.
- Próxima ação: mapear gravação/recuperação e atalhos, testar motores candidatos com áudio português e implementar modelo compartilhado + editores.
