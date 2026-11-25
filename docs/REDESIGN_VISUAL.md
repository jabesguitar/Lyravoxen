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
- Lyravoxen_Icone_App_1024.png
- Lyravoxen.ico
- Lyravoxen_SplashScreen_Logo_Oficial_1080x1920.png

## Próximos passos
1. Adicionar o .ico em src/Lyravoxen.Desktop/Assets/Lyravoxen.ico e confirmar o ApplicationIcon no .csproj.
2. Adicionar splash em Assets e ligá-la à inicialização sem alterar StartupUri nem fluxo de startup.
3. Aplicar os tokens visuais gradualmente às telas WPF, preservando nomes x:Name, bindings, Click handlers e eventos.
4. Compilar e executar os testes existentes.
5. Testar manualmente reprodução, sincronização, histórico, biblioteca, projeção própria e integração Holyrics.
6. Revisar a branch antes de qualquer merge em main.

## Estado atual
- Branch isolada: redesign/brand-visual-layer
- Alteração já realizada: tokens de cor compartilhados em App.xaml.
- Nenhuma funcionalidade foi intencionalmente alterada.
- Ainda não foi validada uma compilação nem teste manual nesta etapa.
