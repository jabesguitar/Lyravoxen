"""Procura nomes de variáveis locais repetidos no mesmo escopo (CS0128/CS0136), que o compilador recusa.
Aproximação por chaves { }: pode ter alarmes falsos (confira à mão). python3 tests/csharp_scopes.py"""
import re,os,sys
ROOT=os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)),'..'))
D=os.path.join(ROOT,'src','Lyravoxen.Desktop')
def strip(t):
    t=re.sub(r'"{3,}.*?"{3,}','""',t,flags=re.S); t=re.sub(r'@"(?:[^"]|"")*"','""',t); t=re.sub(r'\$?"(?:\\.|[^"\\\n])*"','""',t); t=re.sub(r"'(?:\\.|[^'\\])'","''",t)
    t=re.sub(r'/\*.*?\*/','',t,flags=re.S); return re.sub(r'//[^\n]*','',t)
TYPE=r'(?:var|int|long|double|float|bool|string|object|byte|short|char|decimal|[A-Z]\w*(?:<[^<>;]*(?:<[^<>;]*>[^<>;]*)*>)?(?:\[\])?\??)'
DECL=[re.compile(r'(?<![\w\.])'+TYPE+r'\s+([a-z_]\w*)\s*(?:=(?!=|>)|;|,\s*[a-z_]\w*\s*[=;])'),
      re.compile(r'\bout\s+(?:var|'+TYPE+r')\s+(\w+)'),
      re.compile(r'\bis\s+(?:not\s+)?(?:\{[^}]*\}|'+TYPE+r')\s+(\w+)\b(?!\s*\()'),
      re.compile(r'\bforeach\s*\(\s*(?:var|'+TYPE+r')\s+(\w+)\s+in\b'),
      re.compile(r'\bcatch\s*\([\w\.]+\s+(\w+)\)')]
KEYW=set('return throw await new in is as if else for while switch case default break continue using lock try catch finally var when and or not true false null this base ref out params'.split())
problems=[]
for dp,_,fs in os.walk(D):
    if '/obj' in dp or '/bin' in dp: continue
    for f in fs:
        if not f.endswith('.cs'): continue
        path=os.path.join(dp,f); text=strip(open(path,encoding='utf-8').read())
        stack=[set()]; method_sig=None
        for ln,line in enumerate(text.split('\n'),1):
            # parâmetros do método: linha com ( ) seguida de { na linha ou na próxima; aproximação: ao abrir '{', se a linha anterior terminou em ')'
            pre=line
            # declarações na linha (antes de processar chaves)
            names=[]
            for rx in DECL:
                for m in rx.finditer(line):
                    n=m.group(1)
                    if n in KEYW or n.startswith('__'): continue
                    names.append(n)
            for n in names:
                if any(n in sc for sc in stack):
                    problems.append(f'{os.path.relpath(path,ROOT)}:{ln}: "{n}" já declarada neste escopo ou num escopo externo')
                stack[-1].add(n)
            for ch in line:
                if ch=='{': stack.append(set())
                elif ch=='}':
                    if len(stack)>1: stack.pop()
        # fim do arquivo
if problems:
    print('POSSÍVEIS NOMES REPETIDOS:'); [print(' -',p) for p in problems]; sys.exit(1)
print('csharp_scopes: nenhum nome local repetido encontrado.')
