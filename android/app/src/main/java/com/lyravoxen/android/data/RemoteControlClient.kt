package com.lyravoxen.android.data

import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import java.net.HttpURLConnection
import java.net.URL
import java.net.URLEncoder

/**
 * Bridge opcional para controlar uma instalação Windows do Lyravoxen na mesma rede.
 * A aplicação Android continua separada e não depende deste servidor para abrir.
 */
class RemoteControlClient {
    suspend fun command(baseAddress: String, pin: String, command: String, argument: String = ""): Result<String> =
        withContext(Dispatchers.IO) {
            runCatching {
                val base = baseAddress.trim().removeSuffix("/")
                require(base.startsWith("http://") || base.startsWith("https://")) {
                    "Informe o endereço completo, por exemplo http://192.168.0.5:5280"
                }
                val query = listOf(
                    "pin" to pin,
                    "c" to command,
                    "a" to argument
                ).joinToString("&") { (key, value) ->
                    "$key=${URLEncoder.encode(value, "UTF-8")}"
                }
                val connection = (URL("$base/api?$query").openConnection() as HttpURLConnection).apply {
                    requestMethod = "GET"
                    connectTimeout = 5000
                    readTimeout = 8000
                    setRequestProperty("Accept", "application/json")
                }
                try {
                    val code = connection.responseCode
                    val stream = if (code in 200..299) connection.inputStream else connection.errorStream
                    val body = stream.bufferedReader().use { it.readText() }
                    if (code !in 200..299) error("Servidor respondeu HTTP $code: $body")
                    body
                } finally {
                    connection.disconnect()
                }
            }
        }
}
