"""Confere se cada botão do XAML (e das janelas criadas em C#) é citado na Ajuda (MainWindow.Help.cs).
Rode depois de criar uma função nova:  python3 tests/help_coverage.py   -> lista o que falta explicar na Ajuda."""
import re,os,sys,unicodedata
D=os.path.join(os.path.dirname(os.path.abspath(__file__)),'..','src','Lyravoxen.Desktop')
def norm(t):
    t=unicodedata.normalize('NFD',t); t=''.join(c for c in t if unicodedata.category(c)!='Mn')
    return re.sub(r'[^a-z0-9]+',' ',t.lower()).strip()
help_text=norm(open(os.path.join(D,'MainWindow.Help.cs'),encoding='utf-8').read())
sources=[open(os.path.join(D,f),encoding='utf-8').read() for f in ['MainWindow.xaml']+[f for f in os.listdir(D) if f.startswith('MainWindow.') and f.endswith('.cs') and f!='MainWindow.Help.cs']]
labels=set()
for src in sources:
    labels|=set(re.findall(r'<Button[^>]*\bContent="([^"]{4,})"',src))
    labels|=set(re.findall(r'(?:RequestButton|Button\s*\{\s*Content\s*=)\(?\s*"([^"]{4,})"',src))
ignore={'Fechar','Salvar','Importar','Abrir','Cancelar','Testar conexão','Nova senha','Copiar senha'}
missing=[]
for label in sorted(labels):
    n=norm(label)
    if len(n)<4 or label in ignore or re.search(r'\(\d+\)$',label): continue  # rótulos com contador, ex.: Pedidos (0)
    if n not in help_text: missing.append(label)
print('Botões sem menção na Ajuda:' if missing else 'Ajuda cobre todos os botões.')
for m in missing: print('  -',m)
sys.exit(1 if missing else 0)
