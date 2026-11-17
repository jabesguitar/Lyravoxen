# Bíblia, microfone e identidade visual

## Requisitos aprovados
- Integração Holyrics com liga/desliga, mantendo o processo aberto quando a integração for desativada.
- Microfone bíblico com liga/desliga próprio, seleção de entrada e captação independente do áudio musical.
- Bíblia integrada com versões disponíveis no Holyrics apenas quando o uso/redistribuição estiver licenciado.
- Fundos modernos, discretos e legíveis, com centro limpo e elementos decorativos nas bordas.

## Implementado nesta branch
- Interruptor persistente de integração Holyrics na barra principal.
- Serviço independente de microfone usando entrada selecionada do Windows e Whisper.net local.
- Transcrição aparece em prévia; o parser Midvash pode sugerir referências faladas em português. Não há projeção automática de sugestões.
- Leitor bíblico com seleção de tradução, livro, capítulo e versículo, projeção manual e cache local por capítulo.
- Versões iniciais: ALM1911, Almeida Livre, Bíblia Portuguesa Mundial e Bíblia Livre Para Todos, respeitando as licenças publicadas pelo catálogo de dados.

## Licenciamento
O Holyrics lista traduções fornecidas por editoras e sociedades bíblicas parceiras. A existência dessas traduções no Holyrics não transfere ao Lyravoxen direitos de cópia ou redistribuição. Traduções com copyright ativo (por exemplo NVI, ARA, NAA, NVT e outras) só poderão ser incorporadas mediante autorização/licença. O seletor inicial usa versões de domínio público ou licenças abertas; o crédito informado pela API acompanha o texto exibido.

Fontes:
- https://holyrics.com.br/about.php
- https://api.midvash.com/pt-br
- https://api.midvash.com/docs
- https://github.com/midvash/bible-data

## Comportamento do microfone
- Desligado por padrão.
- Operador escolhe o dispositivo de entrada explicitamente.
- Áudio é transcrito localmente, sem upload para nuvem.
- Parser de referência depende de conexão à API Midvash; transcrição continua local.
- Resultado é sugestão para conferência, não é enviado ao telão automaticamente.
- Desativar ou fechar o aplicativo encerra a captura.

## Fundos
O usuário forneceu referências visuais com azul-marinho, iluminação azul, detalhes dourados, oliveira, rocha texturizada, coroa de espinhos e tecido vinho. Foram criados quatro fundos 16:9 e quatro adaptações 4:3, com espaço central limpo. Ainda precisam ser incorporados ao seletor de fundos do projetor próprio.


## Almeida Revista e Corrigida — SBB 2009
A prioridade para cultos é a edição **Almeida Revista e Corrigida © 2009 (ARC)** da Sociedade Bíblica do Brasil, não a Almeida 1911 histórica, não a Almeida Corrigida Fiel (ACF), e não a Almeida Revista e Atualizada (ARA).

A ARC SBB 2009 é protegida por direitos autorais. O Holyrics declara parceria com a SBB para essa tradução. O Lyravoxen identifica a edição brasileira pelo ID `pt_arc` (o ID `pt_rc69` corresponde à edição 1969 e não será usado como substituto). O Lyravoxen não deve baixar, embutir, copiar ou redistribuir o texto sem autorização. Em vez disso, a integração consulta as traduções instaladas no Holyrics (`GetBibleVersionsV2`) e solicita a projeção da referência na versão selecionada (`ShowVerse`). Isso preserva o texto oficial fornecido pelo próprio Holyrics, sem paráfrases, correção automática, revisão ortográfica, normalização ou alteração artificial pelo Lyravoxen.

A edição de 1969 (ARC69) é outra edição e deve aparecer separadamente se instalada. A edição 1911 de domínio público também é separada e nunca será rotulada como ARC SBB 2009.

A API documentada do Holyrics permite listar versões e projetar referências, mas não expõe uma função documentada para descarregar o texto integral da Bíblia ao Lyravoxen. Por isso, a leitura ARC no app independente depende de autorização/licença e fornecimento de um arquivo oficial pela SBB.

Fontes:
- SBB, Almeida Revista e Corrigida: https://www.sbb.org.br/almeida-revista-e-corrigida
- Holyrics, parcerias e versões: https://holyrics.com.br/about.php
- Holyrics API Server: https://github.com/holyrics/API-Server
- SBB+, lista ARC69 e ARC 2009: https://play.google.com/store/apps/details?id=org.bb.app
