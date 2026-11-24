import re,sys,glob,os
base=os.path.join(os.path.dirname(os.path.abspath(__file__)),'..','..','src','Lyravoxen.Desktop','Services')
def raw(path, marker, quotes):
    s=open(path,encoding='utf-8').read()
    start=s.index(marker)+len(marker)
    open_q='"'*quotes
    a=s.index(open_q+'\n',start)+quotes+1
    b=s.index('\n'+open_q+';',a)
    return s[a:b]
open('form_template.html','w',encoding='utf-8').write(raw(base+'/OfflineRequestForm.cs','private const string Template =',3))
open('worker_template.js','w',encoding='utf-8').write(raw(base+'/CloudWorker.cs','private const string Template =',4))
open('phone_page.html','w',encoding='utf-8').write(raw(base+'/RemoteControlServer.cs','private const string PageHtml =',3))
print('extracted')
