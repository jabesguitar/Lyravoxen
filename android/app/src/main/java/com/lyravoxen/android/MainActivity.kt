package com.lyravoxen.android

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.compose.foundation.background
import androidx.compose.foundation.horizontalScroll
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Surface
import androidx.compose.material3.Switch
import androidx.compose.material3.Text
import androidx.compose.material3.lightColorScheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.lyravoxen.android.data.RemoteControlClient
import kotlinx.coroutines.launch

private enum class AppTab(val label: String, val glyph: String) {
    LIVE("Ao vivo", "▶"), LYRICS("Letras", "♫"), NEXT("Próxima", "＋"),
    BIBLE("Bíblia", "✦"), HELP("Ajuda", "?")
}

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContent { LyravoxenApp() }
    }
}

@Composable
private fun LyravoxenApp() {
    val context = LocalContext.current
    val preferences = remember { context.getSharedPreferences("lyravoxen-settings", 0) }
    var lightMode by rememberSaveable { mutableStateOf(preferences.getBoolean("light_mode", false)) }
    var selectedTab by rememberSaveable { mutableStateOf(AppTab.LIVE.name) }
    var server by rememberSaveable { mutableStateOf(preferences.getString("server", "") ?: "") }
    var pin by rememberSaveable { mutableStateOf(preferences.getString("pin", "") ?: "") }
    var status by rememberSaveable { mutableStateOf("Conecte ao computador ou use os recursos locais do aplicativo.") }
    var songUrl by rememberSaveable { mutableStateOf("") }
    var lyrics by rememberSaveable { mutableStateOf("") }
    var bibleRef by rememberSaveable { mutableStateOf("") }
    val scope = rememberCoroutineScope()
    val client = remember { RemoteControlClient() }

    val navy = Color(0xFF06152F)
    val panel = Color(0xFF0B2450)
    val cyan = Color(0xFF00D9F5)
    val scheme = if (lightMode) lightColorScheme(
        primary = Color(0xFF087F9D), secondary = Color(0xFF6750A4),
        background = Color(0xFFF1F6FC), surface = Color.White,
        onPrimary = Color.White, onBackground = Color(0xFF10233D), onSurface = Color(0xFF10233D)
    ) else darkColorScheme(
        primary = cyan, secondary = Color(0xFF9A78FF),
        background = navy, surface = panel,
        onPrimary = Color(0xFF001A2A), onBackground = Color(0xFFEAF4FF), onSurface = Color(0xFFEAF4FF)
    )

    MaterialTheme(colorScheme = scheme) {
        Scaffold(containerColor = MaterialTheme.colorScheme.background) { padding ->
            Column(
                modifier = Modifier.fillMaxSize().padding(padding)
                    .background(if (lightMode) Brush.verticalGradient(listOf(Color(0xFFF4F8FF), Color(0xFFE5EEF9)))
                    else Brush.verticalGradient(listOf(navy, Color(0xFF040E22))))
                    .verticalScroll(rememberScrollState()).padding(16.dp),
                verticalArrangement = Arrangement.spacedBy(14.dp)
            ) {
                Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(12.dp)) {
                    Box(
                        modifier = Modifier.width(54.dp).height(54.dp)
                            .background(if (lightMode) Color(0xFFE0F6FB) else Color(0xFF0B2450), RoundedCornerShape(17.dp)),
                        contentAlignment = Alignment.Center
                    ) { Text("𝄞", color = cyan, fontSize = 34.sp, fontWeight = FontWeight.Bold) }
                    Column(modifier = Modifier.weight(1f)) {
                        Text("Lyra", color = MaterialTheme.colorScheme.onBackground, fontSize = 25.sp, fontWeight = FontWeight.Bold)
                        Text("voxen  •  ESTÚDIO DE LETRAS", color = MaterialTheme.colorScheme.primary, fontSize = 11.sp, fontWeight = FontWeight.SemiBold)
                    }
                    Column(horizontalAlignment = Alignment.End) {
                        Text(if (lightMode) "CLARO" else "ESCURO", fontSize = 10.sp, color = MaterialTheme.colorScheme.primary)
                        Switch(checked = lightMode, onCheckedChange = {
                            lightMode = it
                            preferences.edit().putBoolean("light_mode", it).apply()
                        })
                    }
                }

                Row(
                    modifier = Modifier.fillMaxWidth().horizontalScroll(rememberScrollState()),
                    horizontalArrangement = Arrangement.spacedBy(8.dp)
                ) {
                    AppTab.values().forEach { tab ->
                        val active = selectedTab == tab.name
                        Button(
                            onClick = { selectedTab = tab.name },
                            shape = RoundedCornerShape(24.dp),
                            colors = ButtonDefaults.buttonColors(
                                containerColor = if (active) MaterialTheme.colorScheme.primary else MaterialTheme.colorScheme.surface,
                                contentColor = if (active) MaterialTheme.colorScheme.onPrimary else MaterialTheme.colorScheme.onSurface
                            )
                        ) { Text("${tab.glyph}  ${tab.label}", fontWeight = FontWeight.Bold, maxLines = 1) }
                    }
                }

                when (AppTab.valueOf(selectedTab)) {
                    AppTab.LIVE -> {
                        SectionTitle("PAINEL AO VIVO", "Reprodução, sincronização e projeção")
                        Panel {
                            Text("CONTROLE DE REDE (OPCIONAL)", color = MaterialTheme.colorScheme.primary, fontWeight = FontWeight.Bold)
                            OutlinedTextField(server, { server = it; preferences.edit().putString("server", it).apply() },
                                label = { Text("Endereço do computador") }, placeholder = { Text("http://192.168.0.5:5280") },
                                modifier = Modifier.fillMaxWidth(), singleLine = true)
                            OutlinedTextField(pin, { pin = it; preferences.edit().putString("pin", it).apply() },
                                label = { Text("PIN do controle") }, modifier = Modifier.fillMaxWidth(), singleLine = true)
                            ActionButton("Conectar / consultar estado") {
                                scope.launch {
                                    status = client.command(server, pin, "state").fold(
                                        onSuccess = { "Resposta do Lyravoxen Windows: $it" },
                                        onFailure = { "Não foi possível conectar: ${it.message}" }
                                    )
                                }
                            }
                            Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                                ActionButton("Tocar próxima", Modifier.weight(1f)) { scope.launch { status = send(client, server, pin, "playnext") } }
                                ActionButton("Encerrar música", Modifier.weight(1f)) { scope.launch { status = send(client, server, pin, "endlive") } }
                            }
                        }
                        Panel { Text("ESTADO", color = MaterialTheme.colorScheme.primary, fontWeight = FontWeight.Bold); Text(status) }
                    }
                    AppTab.LYRICS -> {
                        SectionTitle("CADASTRAR LETRA", "Editor inicial da experiência Android")
                        Panel {
                            Text("LETRA COMPLETA — UMA FRASE POR LINHA", color = MaterialTheme.colorScheme.primary, fontWeight = FontWeight.Bold)
                            OutlinedTextField(lyrics, { lyrics = it }, modifier = Modifier.fillMaxWidth().height(260.dp),
                                placeholder = { Text("Cole ou digite a letra aqui…") })
                            Text("A edição local e a biblioteca offline serão implementadas nesta base Android.", fontSize = 12.sp)
                            ActionButton("Enviar texto ao computador") {
                                scope.launch { status = send(client, server, pin, "lyricsnow", lyrics) }
                            }
                        }
                    }
                    AppTab.NEXT -> {
                        SectionTitle("PRÓXIMA MÚSICA", "Prepare a próxima faixa sem sair da tela")
                        Panel {
                            OutlinedTextField(songUrl, { songUrl = it }, modifier = Modifier.fillMaxWidth(),
                                label = { Text("Link do YouTube") }, placeholder = { Text("Cole o link da música") })
                            ActionButton("Preparar próxima música") {
                                scope.launch { status = send(client, server, pin, "setnext", "0|$songUrl") }
                            }
                            Text("A pesquisa e reprodução incorporadas do YouTube serão integradas na etapa de player Android.", fontSize = 12.sp)
                        }
                    }
                    AppTab.BIBLE -> {
                        SectionTitle("CENTRAL BÍBLICA", "Digite uma referência e envie ao sistema de projeção")
                        Panel {
                            OutlinedTextField(bibleRef, { bibleRef = it }, modifier = Modifier.fillMaxWidth(),
                                label = { Text("Referência bíblica") }, placeholder = { Text("Ex.: João 3:16") })
                            ActionButton("Localizar e projetar no computador") {
                                scope.launch { status = send(client, server, pin, "ref", bibleRef) }
                            }
                            Text("A leitura bíblica local, seleção de versões e prévia própria serão desenvolvidas na camada Android.", fontSize = 12.sp)
                        }
                    }
                    AppTab.HELP -> {
                        SectionTitle("AJUDA", "Lyravoxen para Android")
                        Panel {
                            Text("Esta é a fundação independente da versão Android.", fontWeight = FontWeight.Bold)
                            Text("O aplicativo Windows permanece separado. A conexão por IP é complementar; o Android não depende do computador para iniciar.")
                            Text("Próximas etapas: player oficial do YouTube, biblioteca local, sincronização de letras, projeção, Bíblia e controle remoto.")
                        }
                    }
                }
                Spacer(Modifier.height(12.dp))
            }
        }
    }
}

@Composable
private fun SectionTitle(title: String, subtitle: String) {
    Column(verticalArrangement = Arrangement.spacedBy(3.dp)) {
        Text(title, color = MaterialTheme.colorScheme.primary, fontSize = 21.sp, fontWeight = FontWeight.ExtraBold)
        Text(subtitle, color = MaterialTheme.colorScheme.onBackground, fontSize = 12.sp)
    }
}

@Composable
private fun Panel(content: @Composable () -> Unit) {
    Card(
        modifier = Modifier.fillMaxWidth(),
        shape = RoundedCornerShape(22.dp),
        colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface),
        elevation = CardDefaults.cardElevation(defaultElevation = 4.dp)
    ) { Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(12.dp), content = content) }
}

@Composable
private fun ActionButton(label: String, modifier: Modifier = Modifier, onClick: () -> Unit) {
    Button(
        onClick = onClick, modifier = modifier.fillMaxWidth(),
        shape = RoundedCornerShape(16.dp),
        colors = ButtonDefaults.buttonColors(containerColor = MaterialTheme.colorScheme.primary)
    ) { Text(label, fontWeight = FontWeight.Bold) }
}

private suspend fun send(client: RemoteControlClient, server: String, pin: String, command: String, argument: String = ""): String =
    client.command(server, pin, command, argument).fold(
        onSuccess = { "Comando enviado: $it" },
        onFailure = { "Falha: ${it.message}" }
    )
