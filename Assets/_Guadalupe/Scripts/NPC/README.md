# Personajes

Módulo `Guadalupe.NPC`: los turistas que pueblan la plaza y los cinco personajes principales con los que se
puede hablar. Los dos usan la NavMesh de la escena, animaciones humanoides de Mixamo y las sillas de las
terrazas, pero cada grupo tiene su propio controlador.

## Turistas

`TouristSpawner` (grupo `Turistas` de la escena) decide cuántos turistas hay según el escenario y la hora
(`ScenarioDefinition.CrowdAt`), los hace llegar en grupos por las bocacalles que el jugador no ve y les da
paradas en los destinos de `PuntosDeInteres` (`TouristDestination`). Se apoya en:

- `TouristCasting`: qué personaje sale y con qué ropa, sin repetir mientras quede alguno libre para la época.
- `SpotOccupancy`: qué sitios de cada destino están libres; una mesa o un corrillo no se comparte.
- `SeatPlanner`: cómo se sienta alguien en una silla de terraza (por qué lado entra y dónde se coloca).
- `TouristPool`: los turistas que se van se guardan para reutilizarlos.
- `TouristCrowd`: las reglas sin dependencias de la escena (cuántos, tamaño de los grupos, ropa, pasos).

Cada turista (`Tourist`) recorre las fases de una parada: llegar, colocarse, sentarse si hay silla, quedarse
un rato haciendo su actividad (`TouristActivity`) y marcharse. `TouristPhone` saca el móvil al hacer fotos y
`FootGrounding` apoya los pies en el suelo real por IK.

Los modelos son de Microsoft Rocketbox (`Art/Characters/Rocketbox`), con sus materiales creados desde
**Guadalupe → Personajes → Crear materiales de los turistas (Rocketbox)**. El controlador es `AC_Tourist`.

## Personajes principales

Fray Paulino, José Luis, Paqui, Marisé e Inés María están en `PersonajesPrincipales`, cada uno con su prefab
`PF_Main_<Nombre>` y su perfil (`SO_NpcProfile_<Nombre>`: nombre, frases y lo que necesita la conversación).
`MainCharacter` los mueve:

- Pasean por su zona (un segmento con radio) parándose a ratos, o están sentados en su silla.
- Al acercarse el jugador se paran, le miran, saludan con su gesto y dicen una frase del momento
  (`GreetingRules`): se presentan la primera vez y luego eligen entre las frases generales, las de la hora,
  las del escenario, las de la plaza llena y las que usan el nombre del jugador, sin repetir las últimas.
  Las frases están en el perfil, agrupadas por momento (`SpokenLines`). Inés María llama al jugador si pasa cerca.
- De cerca y de cara se puede hablar con ellos (tecla F; la conversación está en el módulo `Dialogue`).
  Solo José Luis da la mano antes (`Handshake`). Mientras conversan atienden al jugador y gesticulan al hablar.
- Inés María bebe de su vaso (`MainCharacterDrink`) y, si el escenario quita su mesa (el árbol de Navidad),
  se cambia a su silla de repuesto con la bebida.

Las reglas sin escena (distancias de saludo, zona, tiempos del gesto de beber) están en `MainCharacterRules`.
La interfaz (bocadillo, aviso para hablar y subtítulos) es `InteractionHud`, en el módulo `UI`.

### Cambiar el aspecto de un personaje

Los modelos son de MakeHuman (MPFB para Blender) y están en `Art/Characters/Main/<Nombre>/`, como
`<Nombre>.fbx` y `Textures/<pieza>.png`.
**Guadalupe → Personajes → Crear materiales del jugador y los personajes principales (MakeHuman)** lo importa como
Humanoid y crea sus materiales (`M_<Nombre>_<Pieza>`). El prefab contiene el modelo como instancia del FBX,
así que al reimportarlo se actualiza solo; el comportamiento no depende del modelo.

## Sillas y sitios reservados

`Seats` reúne las medidas comunes de las sillas (altura del asiento, holgura de la cadera). Un personaje
sentado reserva su mesa en `ReservedSpots` para que ningún turista se siente en ella.
