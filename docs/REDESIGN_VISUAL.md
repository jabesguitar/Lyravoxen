# Redesign visual do Lyravoxen

## Objetivo
Modernizar a identidade visual do aplicativo sem alterar a lógica, os serviços ou os fluxos funcionais existentes.

## Regra de preservação
- Não modificar handlers/eventos, serviços, modelos, persistência, sincronização, reprodução, YouTube, Holyrics ou projeção durante esta etapa.
- Mudanças visuais devem ficar em XAML, recursos visuais, imagens, fontes e propriedades de apresentação.
- Toda mudança futura de funcionalidade deve preservar os recursos e tokens visuais existentes.
- Trabalhar nesta branch; não mesclar em main até revisão visual e testes do usuário.

## Identidade de referência
Logomarca aprovada como referência visual pelo usuário em 25/11/2026: lira branca com equalizador ciano/azul/violeta, linhas brancas, ondas ciano/violeta, nome Lyravoxen em branco e ciano e arco amarelo, sobre azul-marinho.

## Tokens de cor
- Fundo principal: #06152F
- Painéis: #0B2450
- Acento ciano: #00D9F5
- Texto principal: #F2F6F8
- Texto secundário: #B5C8E7
- Botões: #123A66
- Campos: #071A3B
- Bordas: #245B88
- Violeta e amarelo devem ser usados como acentos pontuais coerentes com a marca, sem prejudicar contraste.

## Plataformas
- Windows 10/11: plataforma atual e prioridade do redesign.
- Android APK: etapa futura; exige implementação/adaptação Android própria. Não tratar a splash vertical como substituta automática de uma tela nativa responsiva.

## Arquivos de marca preparados fora do repositório
- Lyravoxen_Icone_App_1024.png (deve ser enviado ao repositório como src/Lyravoxen.Desktop/Assets/Lyravoxen-Brand.png)
- LyravoxenSplash_Windows_1920x1080.png (deve ser enviado como src/Lyravoxen.Desktop/Assets/LyravoxenSplash_Windows.png)
- Lyravoxen_SplashScreen_Logo_Oficial_1080x1920.png (reservada para futura versão Android)
- Lyravoxen.ico (gerado automaticamente pelo script de build a partir da imagem aprovada)

## Próximos passos
1. Enviar a imagem quadrada aprovada para src/Lyravoxen.Desktop/Assets/Lyravoxen-Brand.png.
2. Enviar a splash horizontal Windows para src/Lyravoxen.Desktop/Assets/LyravoxenSplash_Windows.png.
3. O script tools/New-AppIcon.ps1 gera Lyravoxen.ico a partir da imagem aprovada; o .csproj já aponta para esse ICO.
4. O .csproj usa o item WPF SplashScreen para exibir a splash automaticamente durante a inicialização.
5. Aplicar os tokens visuais gradualmente às telas WPF, preservando nomes x:Name, bindings, Click handlers e eventos.
6. Compilar e executar os testes existentes.
7. Testar manualmente reprodução, sincronização, histórico, biblioteca, projeção própria e integração Holyrics.
8. Revisar a branch antes de qualquer merge em main.

## Estado atual
- Branch isolada: redesign/brand-visual-layer
- Alteração já realizada: tokens de cor compartilhados em App.xaml.
- Nenhuma funcionalidade foi intencionalmente alterada.
- O gerador de ícone foi ajustado para usar a arte aprovada, em vez do ícone genérico anterior.
- O projeto foi configurado para a splash horizontal Windows.
- Os dois arquivos PNG ainda precisam ser enviados pelo usuário à pasta Assets antes do build.
- Ainda não foi validada uma compilação nem teste manual nesta etapa.


## Controle remoto por IP — atualização de 25/11/2026
- O usuário solicitou que a interface web de controle remoto na rede local acompanhe a identidade visual da splash/logo.
- Redesenho visual aplicado em `Services/RemoteControlServer.cs`: cabeçalho com logomarca, paleta azul-marinho/ciano/azul/violeta e detalhes dourados, cartões, campos, botões e adaptação a telas móveis.
- Foi adicionada a rota GET `/brand.png`, servindo o PNG da marca a partir de `AppContext.BaseDirectory/Assets/Lyravoxen_Icone_App_1024.png`.
- O PNG foi incluído no `.csproj` com `CopyToOutputDirectory="PreserveNewest"`.
- Os IDs HTML, funções JavaScript, chamadas `/api`, `/audio`, `/qr.png`, `/pedido`, PIN e comandos existentes foram mantidos; não houve alteração intencional de protocolo ou comportamento funcional.
- Estado: alterações registradas na branch; compilação e teste visual do controle remoto ainda pendentes.
