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
