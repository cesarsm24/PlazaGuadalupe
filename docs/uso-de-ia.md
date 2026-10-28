# Declaración de uso de inteligencia artificial

Este documento recoge qué herramientas de inteligencia artificial se han usado en el proyecto, para qué y cómo se ha
revisado su resultado.

## Herramientas

| Herramienta | Uso |
|---|---|
| Claude (Anthropic), mediante Claude Code | Asistente de desarrollo durante la construcción del proyecto |
| Google Gemini (API) | Parte del juego: genera las respuestas de los personajes en la conversación |

## Trabajo del autor

El autor ha hecho lo esencial del proyecto en Unity:

- El terreno de la plaza y su entorno.
- La importación de los modelos, su integración en el proyecto y los prefabs (mobiliario, elementos de la plaza,
  monasterio y personajes).
- El montaje de la escena: colocación de todo lo anterior, iluminación, navegación y límites de la zona jugable.
- El diseño: qué recrear y con qué nivel de detalle, el estilo visual, los escenarios, los personajes y las
  decisiones técnicas.
- El material de referencia del lugar y los datos de los personajes.
- La prueba de cada cambio en el Editor y en juego, hasta darlo por bueno.

## Asistente de desarrollo

Claude se ha usado como apoyo, a partir de las indicaciones del autor y siempre con su revisión, para:

- **Modelos y código:** generación de los modelos (monasterio y personajes) y de los scripts de C#, y corrección de
  errores.
- **Detalles:** perfeccionar y pulir el aspecto de la plaza (fachadas, monasterio, luz de día y de noche, Navidad) y
  el comportamiento de los personajes.
- **Revisión:** repasos de estilo y organización del código, y búsqueda de código y assets sin usar.
- **Optimización:** medición del rendimiento y ajuste de batching, culling y sombras.
- **Documentación:** borradores del README y de los documentos de `docs/`.

Ningún resultado se ha incorporado sin revisarlo. Los datos sobre Guadalupe que conocen los personajes se
contrastaron con fuentes públicas y el autor confirmó los que se usan.

## IA dentro del juego

La conversación con los personajes principales usa la API de Google Gemini en tiempo real:

- Cada personaje recibe unas instrucciones con su perfil, unas reglas comunes, el momento de la visita y lo que el
  jugador ha contado antes. El detalle está en [personajes.md](personajes.md).
- Los personajes no consultan internet: solo saben lo que hay en su perfil.
- Las respuestas se generan en el momento, así que pueden contener imprecisiones. Las prudencias del perfil limitan
  lo que afirman sobre temas delicados.
- Sin clave o sin conexión, el resto del juego funciona y los personajes se disculpan.

## Lo que no se ha generado con IA

- **Datos geográficos:** Catastro, EU-DEM y OpenStreetMap.
- **Recursos de terceros:** texturas de ambientCG, personajes de Rocketbox, animaciones de Mixamo, Starter Assets y
  tipografías, con las licencias que se indican en el README.
- **Imágenes y sonido:** ninguna imagen, textura ni sonido procede de un generador de imágenes o de audio. Las
  texturas propias y el sonido ambiente salen de scripts propios y de los recursos indicados en los créditos.
