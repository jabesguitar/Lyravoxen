namespace Lyravoxen;

internal static class YouTubePlayerPage
{
    public static string Build(string videoId, string sessionId)
    {
        const string template = """
<!doctype html>
<html lang="pt-br">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<meta name="referrer" content="strict-origin-when-cross-origin">
<style>
html,body{margin:0;width:100%;height:100%;background:#080d12;overflow:hidden}
#player{width:100%;height:100%}
</style>
</head>
<body>
<div id="player"></div>
<script>
var player;
const sessionId = '__SESSION_ID__';
function sendMessage(message){ message.session = sessionId; window.chrome.webview.postMessage(message); }
function onYouTubeIframeAPIReady(){
  player = new YT.Player('player', {
    width:'100%', height:'100%', videoId:'__VIDEO_ID__',
    playerVars:{playsinline:1, controls:1, rel:0, origin:'https://lyravoxen.local'},
    events:{
      onReady:function(){sendMessage({type:'ready'});},
      onStateChange:function(event){sendMessage({type:'state',state:event.data});},
      onError:function(event){sendMessage({type:'playerError',code:event.data});}
    }
  });
  window.setInterval(function(){
    if(player && typeof player.getCurrentTime === 'function'){
      sendMessage({type:'time',seconds:player.getCurrentTime(),duration:player.getDuration(),state:player.getPlayerState()});
    }
  },200);
}
</script>
<script>
window.addEventListener('error', function(event){
  if(event && event.target && event.target.tagName === 'SCRIPT' &&
     String(event.target.src || '').includes('youtube.com/iframe_api')){
    sendMessage({type:'playerError',code:-1,detail:'Não foi possível carregar a API oficial do player do YouTube. Verifique a conexão com a internet e tente novamente.'});
  }
}, true);
</script>
<script src="https://www.youtube.com/iframe_api"></script>
</body>
</html>
""";
        return template.Replace("__VIDEO_ID__", videoId, StringComparison.Ordinal)
            .Replace("__SESSION_ID__", sessionId, StringComparison.Ordinal);
    }
}