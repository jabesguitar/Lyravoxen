# Testes de JavaScript (serviço na nuvem, formulário, página do celular, leitores de texto)

Rodam fora do Windows/.NET, com Node 20+ e Playwright (Chromium). Não substituem a compilação do app (`dotnet build`/Actions).

```
python3 extract.py          # tira do código C# o formulário, o worker e a página do celular
node worker.test.mjs        # serviço Cloudflare com KV falso
PW_MODULES=/caminho/node_modules/ node forms.test.mjs   # formulário com e sem JavaScript (Chromium)
PW_MODULES=/caminho/node_modules/ node phone.test.mjs   # aba Ao vivo do celular contra um "computador" falso
python3 extract_patterns.py && node parsers.test.mjs    # regex reais do C# aplicadas a conversas do WhatsApp
```
`build.mjs` é uma cópia em JavaScript de `OfflineRequestForm.Build` e `CloudWorker.Build`: se mudar o C#, atualize-a.
