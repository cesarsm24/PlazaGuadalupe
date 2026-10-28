# Conversación

Módulo `Guadalupe.Dialogue`: la conversación por texto con los personajes principales, con las respuestas de
Google Gemini.

## Cómo funciona

`ConversationDirector` (objeto `Conversacion` de la escena) lleva cada conversación:

1. Al pulsar F junto a un personaje, `InteractionHud` le pide que interactúe y, cuando termina el gesto (el
   apretón de manos de José Luis), empieza la conversación.
2. La `SessionState` indica con quién se habla (`ConversationPartner`): `PlayerControlLock` deja al jugador
   quieto con el cursor libre y `ConversationCamera` le encuadra por encima del hombro.
3. El personaje habla primero, al momento, con una de sus frases elegida según la hora, el día, la gente y el
   nombre del jugador (`MainCharacter.OpeningLine`); el modelo la recibe como lo que ya ha dicho. Cada respuesta
   llega a trozos y `ChatPanel` (módulo `UI`) la va mostrando al ritmo al que se pronuncia, mientras el
   personaje gesticula.
4. Con Esc se despide con una de sus frases. La conversación se resume en otra petición (nombre del jugador y
   una nota con lo que ha contado), que queda en la sesión para los demás personajes.

## Piezas

- `PromptBuilder`: las instrucciones de cada personaje, a partir de su perfil (`NpcProfile`), las reglas
  comunes (`SO_ConversationRules`) y el momento de la visita (`ConversationContext`: hora, escenario, gente,
  nombre del jugador y notas). También recorta el historial y prepara la petición del resumen.
- `Conversation`: el historial de una conversación y las peticiones al modelo.
- `ILanguageModel`: la interfaz del modelo; `GeminiClient` la implementa con `streamGenerateContent` en
  Server-Sent Events (`SseReader`, `GeminiJson`).
- `LlmSettings`: la configuración de `config.local.json` (ver el README del proyecto). La clave va en una
  cabecera y nunca se escribe en el registro.
- `VisitNote`: el resumen de una conversación. `ReplyGestures`: cuándo señala el personaje.

Si no hay respuesta (sin clave, sin conexión, límite de la clave...), el chat muestra un aviso y, si la
conversación no ha llegado a empezar, el personaje se disculpa con una de sus frases.
