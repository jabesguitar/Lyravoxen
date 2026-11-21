import re,json,os
D=os.path.join(os.path.dirname(os.path.abspath(__file__)),'..','..','src','Lyravoxen.Desktop','Services')
def verb(s): return [m.replace('""','"') for m in re.findall(r'@"((?:[^"]|"")*)"',s)]
st=open(D+'/SongRequestStore.cs',encoding='utf-8').read(); lp=open(D+'/LyricsPreviewService.cs',encoding='utf-8').read()
P={}
b=st[st.index('ChatHeader = new('):]; b=b[:b.index('RegexOptions.Compiled);')]; P['chatHeader']=''.join(verb(b))
b=st[st.index('GroupLine = new('):]; b=b[:b.index('RegexOptions')]; P['groupLine']=verb(b)[0]
P['token']=re.search(r'Regex\.Match\(text, @"(#LYRA:[^"]*)"\)',st).group(1)
P['link']=re.search(r'Regex\.Match\(line, @"(\(https[^"]*)"',st).group(1)
P['nameClean']=re.search(r'Regex\.Replace\(line\[\.\.link\.Index\], @"([^"]*)"',st).group(1)
P['hasLink']=re.search(r'Regex\.IsMatch\(text, @"([^"]*)"',st).group(1)
P['cleanBrackets']=re.search(r'public static string CleanTitle.*?Regex\.Replace\(title \?\? "", @"([^"]*)"',lp,re.S).group(1)
P['cleanWords']=re.search(r'public static string CleanTitle.*?text = Regex\.Replace\(text,\s*@"([^"]*)"',lp,re.S).group(1)
P['lrcTag']=re.search(r'Regex\.Matches\(raw, @"([^"]*)"\)',lp).group(1)
P['lrcLead']=re.search(r'Regex\.Replace\(raw, @"([^"]*)"',lp).group(1)
P['split']=re.search(r'Regex\.Split\(cleaned, @"([^"]*)"\)',lp).group(1)

lp=open(D+'/LyricsPreviewService.cs',encoding='utf-8').read()
tp=open(D+'/TrackTitleParser.cs',encoding='utf-8').read()
lc=open(D+'/LyricsTextCleaner.cs',encoding='utf-8').read()
body=lp[lp.index('public static string CleanTitle'):]; body=body[:body.index('public static List<string> BuildQueries')]
pieces=verb(body)
P['cleanBrackets']=pieces[0]; P['emoji']=pieces[1]; P['cleanWords']=pieces[2]
P['partSplit']=verb(tp[tp.index('PartSplit = new('):tp.index('RegexOptions.Compiled);',tp.index('PartSplit = new('))])[0]
blk=tp[tp.index('ChannelSuffix = new('):]; blk=blk[:blk.index('RegexOptions')]; P['channelSuffix']=''.join(verb(blk))
blk=tp[tp.index('NotAnArtistChannel = new('):]; blk=blk[:blk.index('RegexOptions')]; P['notArtist']=''.join(verb(blk))
labels=verb(lc[lc.index('const string Labels'):lc.index(';',lc.index('const string Labels'))])[0]
def comp(name):
    blk=lc[lc.index(name+' = new('):]; blk=blk[:blk.index('RegexOptions')]
    parts=re.findall(r'@"((?:[^"]|"")*)"|(Labels)',blk)
    return ''.join(labels if lab else txt.replace('""','"') for txt,lab in parts)
P['bracketLabel']=comp('BracketLabel'); P['plainLabel']=comp('PlainLabel'); P['chord']=comp('Chord'); P['credit']=comp('Credit'); P['repeat']=comp('Repeat'); P['onlySymbols']=comp('OnlySymbols')


json.dump(P,open(os.path.join(os.path.dirname(os.path.abspath(__file__)),'patterns.json'),'w',encoding='utf-8'),ensure_ascii=False,indent=1)
