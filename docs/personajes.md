# Personajes

Además del jugador, la plaza tiene dos tipos de personajes: los turistas, que la llenan según el escenario y la hora,
y cinco personajes principales del pueblo con los que se puede hablar. Los dos usan la NavMesh de la escena,
animaciones humanoides y las sillas de las terrazas, pero cada grupo tiene su propio controlador. El código está en `Scripts/NPC` (comportamiento)
y `Scripts/Dialogue` (conversación).

## Jugador

El jugador lleva un modelo propio hecho con MakeHuman, como los personajes principales, sobre el controlador en
tercera persona de Starter Assets y sus animaciones. Está en `Art/Characters/Player/Cesar` y usa su propio avatar
humanoide. En primera persona el cuerpo no se dibuja, pero sigue proyectando sombra (`PlayerViewModeSwitcher`).

## Turistas

`TouristSpawner` decide cuántos turistas hay en cada momento con la curva de afluencia del escenario
(`ScenarioDefinition.CrowdAt`). Los hace llegar por bocacalles que el jugador no ve y les asigna paradas en los
puntos de interés:

- **Grupos:** llegan solos o en grupo (parejas, familias, amigos), hacen juntos de una a tres paradas y se marchan
  juntos.
- **Paradas:** la fuente, los miradores del monasterio, el atrio (junto a la fachada y en la balaustrada, subiendo por
  la escalinata), corrillos para charlar y las sillas de las terrazas. Una mesa o un corrillo no se comparte con otro
  grupo (`SpotOccupancy`).
- **Actividades:** mirar, hacer fotos con el móvil (`TouristPhone`), charlar o sentarse un rato.
- **Sentarse:** `SeatPlanner` elige por qué lado se entra a la silla sin atravesar la mesa ni otras sillas. El turista
  aparta la silla, se sienta y la deja en su sitio al irse.
- **Aspecto:** 35 personajes de Microsoft Rocketbox, cada uno vestido para una época (verano, todo el año o invierno).
  No se repite ninguno mientras quede alguno libre para la época del escenario (`TouristCasting`).
- **Pies en el suelo:** `FootGrounding` apoya los pies sobre el terreno real con IK.
- **Reutilización:** los turistas que se van se guardan en un pool (`TouristPool`) y se reutilizan.

Las reglas que no dependen de la escena (cuántos turistas, tamaño de los grupos, ropa por época) están en
`TouristCrowd`.

## Personajes principales

![Fray Paulino, José Luis, Paqui, Marisé e Inés María](img/personajes.jpg)


| Personaje | Papel | Dónde está |
|---|---|---|
| Fray Paulino | Franciscano del Real Monasterio. Sereno, culto y cercano; saluda con «Paz y bien». | En el atrio, entre la portería, la estatua de San Francisco y lo alto de la escalinata |
| José Luis | Dueño del Café Bar Cerezo. Hostelero de toda la vida, serio pero amable. | En la puerta de su bar, bajo los soportales del lado este, y en su terraza |
| Paqui | Dueña del Unide Alimentación. Comerciante práctica y espabilada. | En la puerta del Unide, en el frente sur, junto al puesto de fruta |
| Marisé | Guía turística de Guadalupe y su comarca, apasionada del campo. | Junto a la oficina de turismo, en el lado oeste, y por la calle de al lado |
| Inés María | Vecina de toda la vida. Dicharachera, graciosa y cotilla con cariño. | Sentada en la terraza del Mesón Extremeño con su Coca-Cola |

Cada uno tiene su prefab (`PF_Main_<Nombre>`), con un modelo propio hecho con MakeHuman, y un perfil
(`SO_NpcProfile_<Nombre>`). `MainCharacter` controla su comportamiento:

- **Paseo o silla:** pasean por su zona parándose a ratos, o están sentados. Un personaje sentado reserva su mesa para
  que no la ocupe ningún turista (`ReservedSpots`).
- **Saludo:** al acercarse el jugador se paran, le miran, saludan con su gesto y dicen una frase del momento
  (`GreetingRules`). La primera vez se presentan; después eligen entre frases generales, de la hora, del escenario,
  de la plaza llena y con el nombre del jugador, sin repetir las últimas. Inés María llama al jugador si pasa cerca.
- **Conversación:** de cerca y de cara, con F, se puede hablar con ellos. José Luis da la mano antes de empezar
  (`Handshake`). Mientras conversan atienden al jugador y gesticulan al hablar.
- **Detalles:** Inés María bebe de su vaso (`MainCharacterDrink`) y, en Navidad, cuando el árbol ocupa su mesa, se
  cambia a otra de la misma terraza con su bebida.

## Perfiles y conversación

![Conversación con Paqui: el personaje abre con una de sus frases](img/chat.jpg)


El perfil de cada personaje (`NpcProfile`, un ScriptableObject editable en el Inspector) reúne:

- **Identidad:** nombre, papel, dónde está y personalidad.
- **Temas y datos:** lo que sabe y puede dar por cierto sobre el monasterio, el pueblo, su oficio y la comarca.
- **Prudencias:** lo que debe tratar con cuidado o no afirmar.
- **Remisiones:** a qué otro personaje manda al jugador según el tema.
- **Frases:** las que dice en cada momento (presentación, hora, escenario, gente, nombre del jugador, despedida y
  disculpa si no hay conexión).

En la conversación, `PromptBuilder` combina el perfil, las reglas comunes a todos los personajes
(`SO_ConversationRules`) y el momento de la visita (hora, escenario, afluencia, nombre del jugador y notas de
conversaciones anteriores). El personaje abre con una de sus frases, sin esperar a la API. Cada respuesta de Gemini
llega en streaming y el chat la va mostrando. Al despedirse se guarda un resumen (`VisitNote`) que conocen los demás
personajes: si le has dicho tu nombre a uno, los otros pueden usarlo.

Los personajes no consultan internet: solo saben lo que hay en su perfil y en las notas de la visita.

## Animación

- Controladores `AC_Tourist` y `AC_MainCharacter`, con animaciones humanoides de Mixamo sin desplazamiento de la raíz:
  el movimiento lo da la NavMesh.
- Gestos de saludo, de conversación, de sentarse y de levantarse, y los de beber de Inés María.
- Fuera de cámara no se animan; cómo se evita que se corten al sentarse está en [rendimiento.md](rendimiento.md).
