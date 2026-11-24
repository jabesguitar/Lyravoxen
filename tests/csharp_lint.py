"""Verificador estático (sem compilador) dos erros de C# mais comuns: usings que faltam, campos '_x' sem declaração,
chamadas sem definição e membros estáticos de classes do projeto que não existem.  python3 tests/csharp_lint.py"""
import re,os,sys,glob
ROOT=os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)),'..'))
D=os.path.join(ROOT,'src','Lyravoxen.Desktop')
files=[os.path.join(dp,f) for dp,_,fs in os.walk(D) for f in fs if f.endswith('.cs') and '/obj/' not in dp and '/bin/' not in dp]
def strip(t):
    t=re.sub(r'"{3,}.*?"{3,}','""',t,flags=re.S); t=re.sub(r'@"(?:[^"]|"")*"','""',t); t=re.sub(r'\$?"(?:\\.|[^"\\\n])*"','""',t); t=re.sub(r"'(?:\\.|[^'\\])'","''",t)
    t=re.sub(r'/\*.*?\*/','',t,flags=re.S); return re.sub(r'//[^\n]*','',t)
src={f:open(f,encoding='utf-8').read() for f in files}
clean={f:strip(t) for f,t in src.items()}
problems=[]
# 1) usings
TYPES={'Process':'System.Diagnostics','Stopwatch':'System.Diagnostics','ProcessStartInfo':'System.Diagnostics','ConcurrentQueue':'System.Collections.Concurrent','ConcurrentDictionary':'System.Collections.Concurrent','BlockingCollection':'System.Collections.Concurrent','Marshal':'System.Runtime.InteropServices','MemoryMarshal':'System.Runtime.InteropServices','IPAddress':'System.Net','IPEndPoint':'System.Net','Dns':'System.Net','TcpListener':'System.Net.Sockets','TcpClient':'System.Net.Sockets','NetworkStream':'System.Net.Sockets','SocketException':'System.Net.Sockets','Socket':'System.Net.Sockets','Brush':'System.Windows.Media','Brushes':'System.Windows.Media','Color':'System.Windows.Media','ColorConverter':'System.Windows.Media','SolidColorBrush':'System.Windows.Media','Key':'System.Windows.Input','Keyboard':'System.Windows.Input','KeyEventArgs':'System.Windows.Input','ObservableCollection':'System.Collections.ObjectModel','ICommand':'System.Windows.Input','HorizontalAlignment':'System.Windows','VerticalAlignment':'System.Windows','TextWrapping':'System.Windows','FontWeights':'System.Windows','CornerRadius':'System.Windows','WindowStartupLocation':'System.Windows','SizeToContent':'System.Windows','MessageBoxButton':'System.Windows','MessageBoxImage':'System.Windows','StackPanel':'System.Windows.Controls','WrapPanel':'System.Windows.Controls','TextBlock':'System.Windows.Controls','ListBox':'System.Windows.Controls','Grid':'System.Windows.Controls','ScrollViewer':'System.Windows.Controls','ScrollBarVisibility':'System.Windows.Controls','Stream':'System.IO','File':'System.IO','Path':'System.IO','Directory':'System.IO','MemoryStream':'System.IO','IOException':'System.IO','FileStream':'System.IO','StreamReader':'System.IO','StreamWriter':'System.IO','FileInfo':'System.IO','DirectoryInfo':'System.IO','SearchOption':'System.IO','FileNotFoundException':'System.IO','FileMode':'System.IO','FileAccess':'System.IO','TextWriter':'System.IO','TextReader':'System.IO','HttpClient':'System.Net.Http','HttpRequestException':'System.Net.Http','HttpRequestMessage':'System.Net.Http','HttpMethod':'System.Net.Http','HttpResponseMessage':'System.Net.Http','StringContent':'System.Net.Http','HttpContent':'System.Net.Http','Regex':'System.Text.RegularExpressions','JsonSerializer':'System.Text.Json','JsonDocument':'System.Text.Json','JsonElement':'System.Text.Json','JsonException':'System.Text.Json','JsonValueKind':'System.Text.Json','JsonSerializerOptions':'System.Text.Json','JsonNode':'System.Text.Json.Nodes','JsonObject':'System.Text.Json.Nodes','StringBuilder':'System.Text','Encoding':'System.Text','CultureInfo':'System.Globalization','NumberStyles':'System.Globalization','CharUnicodeInfo':'System.Globalization','UnicodeCategory':'System.Globalization','Channel':'System.Threading.Channels','BoundedChannelOptions':'System.Threading.Channels','DispatcherTimer':'System.Windows.Threading','RandomNumberGenerator':'System.Security.Cryptography','AuthenticationHeaderValue':'System.Net.Http.Headers','BitmapImage':'System.Windows.Media.Imaging','QRCodeGenerator':'QRCoder','PngByteQRCode':'QRCoder','WasapiLoopbackCapture':'NAudio.Wave','WaveInEventArgs':'NAudio.Wave','RoutedEventArgs':'System.Windows','MessageBoxResult':'System.Windows','Visibility':'System.Windows','Thickness':'System.Windows','LibraryFolder':'Lyravoxen.Models','LibraryVideo':'Lyravoxen.Models','LyricCue':'Lyravoxen.Models','CueTimeline':'Lyravoxen.Models','TapSyncPlanner':'Lyravoxen.Models','LyricsPreviewService':'Lyravoxen.Services','TrackTitleParser':'Lyravoxen.Services','LyricsTextCleaner':'Lyravoxen.Services','SongRequestStore':'Lyravoxen.Services','WhatsAppSettings':'Lyravoxen.Services','AudioMonitorService':'Lyravoxen.Services','VagalumeLyricsService':'Lyravoxen.Services','LyricsSourceSettings':'Lyravoxen.Services','CloudRequestClient':'Lyravoxen.Services','CloudWorker':'Lyravoxen.Services','OfflineRequestForm':'Lyravoxen.Services','AppLog':'Lyravoxen.Services','LyricsPreview':'Lyravoxen.Services','LyricsAvailability':'Lyravoxen.Services','ParsedTrack':'Lyravoxen.Services','SongRequest':'Lyravoxen.Services'}
for f,t in clean.items():
    if '/tests/' in f: continue
    usings=set(re.findall(r'^\s*using\s+([\w\.]+)\s*;',src[f],re.M))
    ns=re.search(r'^\s*namespace\s+([\w\.]+)',src[f],re.M); own=ns.group(1) if ns else ''
    for tname,namespace in TYPES.items():
        if not re.search(r'(?<![\w\.])'+tname+r'\b',t): continue
        if namespace in usings or namespace==own: continue
        if re.search(re.escape(namespace)+r'\.'+tname+r'\b',t): continue
        # classe definida neste arquivo?
        if re.search(r'\b(class|record|enum|struct)\s+'+tname+r'\b',t): continue
        problems.append(f'{os.path.relpath(f,ROOT)}: usa {tname} sem "using {namespace};"')
# 2) campos _x
decl=set()
for t in clean.values():
    decl|=set(re.findall(r'(?:private|public|internal|protected|readonly|static|const|volatile)\s+[^;=(){}]*?\b(_\w+)\s*(?:=|;|\{)',t))
    decl|=set(re.findall(r'\b(?:var|int|string|bool|double|long|object)\??\s+(_\w+)\s*(?:=|;)',t))
    decl|=set(re.findall(r'^\s*(?:private|public|internal|protected)\s[^;{}\n]*?\s(_\w+)\s*(?:=|;)',t,re.M))
    decl|=set(re.findall(r'\(\s*(?:[\w<>\[\]\?,\. ]+)\s+(_\w+)\s*[,)]',t))
    decl|=set(re.findall(r'(?:\(|,)\s*(?:[\w<>\[\]\?\. ]+)\s+(_\w+)\s*(?=[,)=])',t))
xaml_names=set(re.findall(r'x:Name="(\w+)"',open(os.path.join(D,'MainWindow.xaml'),encoding='utf-8').read()))
for f,t in clean.items():
    if '/tests/' in f: continue
    for m in sorted(set(re.findall(r'(?<![\w\.])(_[a-z]\w*)\b',t))):
        if m not in decl and m!='_': problems.append(f'{os.path.relpath(f,ROOT)}: campo/variável "{m}" sem declaração encontrada')
# 3) membros estáticos de classes do projeto
classes=['LyricsPreviewService','TrackTitleParser','LyricsTextCleaner','SongRequestStore','TapSyncPlanner','CueTimeline','VagalumeLyricsService','WhatsAppSettings','LyricsSourceSettings','CloudRequestClient','CloudWorker','OfflineRequestForm','AppLog','YouTubeLink']
allsrc='\n'.join(clean.values())
for cname in classes:
    m=re.search(r'\b(?:static\s+)?class\s+'+cname+r'\b[^{]*\{',allsrc)
    if not m: problems.append(f'classe {cname} não encontrada'); continue
    i=m.end(); depth=1
    while depth and i<len(allsrc):
        depth+= (allsrc[i]=='{')-(allsrc[i]=='}'); i+=1
    body=allsrc[m.end():i]
    members=set(re.findall(r'\b(\w+)\s*(?:\(|=>|\{|=|;)',body))
    for f,t in clean.items():
        if '/tests/' in f: continue
        for use in set(re.findall(r'(?<![\w\.])'+cname+r'\.(\w+)',t)):
            if use not in members and use not in ('Equals','ToString'):
                problems.append(f'{os.path.relpath(f,ROOT)}: {cname}.{use} não existe')
# 4) chamadas sem ponto que não são definidas em lugar nenhum
defined=set(re.findall(r'\b(\w+)\s*\(',' '.join(re.findall(r'(?:private|public|internal|protected|static|async|override|void|Task|bool|string|int|double|object|var)\s+[\w<>\[\]\?,\(\) ]*?\b(\w+)\s*\(',allsrc))))
defined=set(re.findall(r'(?:private|public|internal|protected|static|async|override)[^;{=]*?\b(\w+)\s*\(',allsrc))|set(re.findall(r'\b(?:void|Task(?:<[^>]+>)?|bool|string|int|double|object|byte\[\]\??|IEnumerable<\w+>|List<[^>]+>)\s+(\w+)\s*\(',allsrc))
declared_lines=set()
for _t in clean.values():
    for _line in re.findall(r'^\s*(?:public|private|internal|protected|static)[^\n;]*',_t,re.M):
        declared_lines|=set(re.findall(r'\b(\w+)\s*\(',_line))
        declared_lines|=set(re.findall(r'\b(\w+)\s*(?:\{\s*get|=>)',_line))
local=declared_lines|set(re.findall(r'\b(?:Task|void|bool|string|int|double|var|object|\w+\??)\s+(\w+)\s*\([^;{]*\)\s*(?:=>|\{)',allsrc))
known=set('ReferenceEquals InitializeComponent Activate Close Show DllImport Handler RequestHandler AudioStreamer QrPng OnlineLink if for foreach while switch catch lock using fixed nameof typeof sizeof checked default new return await throw when and or not is as base this get set add remove value Check var yield'.split())
for f,t in clean.items():
    if '/tests/' in f: continue
    for name in sorted(set(re.findall(r'(?<![\w\.>\$@])([A-Z]\w+)\s*\(',t))):
        if name in defined or name in local or name in known: continue
        if re.search(r'\b(class|record|struct|enum)\s+'+name+r'\b',allsrc): continue   # construtor
        if re.search(r'new\s+'+name+r'\b',t): continue
        problems.append(f'{os.path.relpath(f,ROOT)}: chamada "{name}(" sem definição encontrada')
if problems:
    print('POSSÍVEIS PROBLEMAS:'); [print(' -',p) for p in sorted(set(problems))]; sys.exit(1)
print('csharp_lint: nada suspeito encontrado.')
