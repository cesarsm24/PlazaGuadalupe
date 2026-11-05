# Plaza de Santa María de Guadalupe

Recreación 3D interactiva en Unity de la Plaza de Santa María de Guadalupe (Cáceres) y su entorno, frente al Real Monasterio, con turistas autónomos, personajes del pueblo y conversación con ellos mediante un modelo de lenguaje.

Proyecto final de la asignatura **Computación Gráfica** — Máster en Ingeniería Informática, Universidad de Extremadura.

El vídeo de demostración, las capturas y las descargas están en la [web del proyecto](https://cesarsm24.github.io/PlazaGuadalupe/).

## Capturas

![El Real Monasterio desde la plaza](Media/monasterio.jpg)

| | |
|---|---|
| ![Fachada, torres y ala este desde el atrio](Media/monasterio-lateral.jpg) | ![Recorriendo la plaza hacia la fuente](Media/jugador.jpg) |
| ![Terrazas bajo los soportales](Media/terrazas.jpg) | ![Soportales y rótulos](Media/soportales.jpg) |
| ![La plaza de noche](Media/noche.jpg) | ![La plaza en Navidad](Media/navidad.jpg) |
| ![Arco de Sevilla](Media/arco-sevilla.jpg) | |

## Estado

Versión 1.0, jugable de principio a fin: entorno, menú, escenarios y franjas horarias, turistas, personajes principales y conversación con ellos. Disponible para macOS, Windows y Linux en [Releases](https://github.com/cesarsm24/PlazaGuadalupe/releases/latest).

## Funcionalidades

- **Entorno 3D** de la plaza, generado a partir de datos abiertos: terreno real, escalinata, lonja y atrio, fuente, soportales corridos, Arco de Sevilla y Arco del Chorro Gordo, y fachada gótica del monasterio. Cada casa de la plaza tiene su propio aspecto (colores, zócalos, balcones, portadas, rótulos y número de plantas).
- **Jugador** con modelo propio, en tercera y primera persona, con teclado y ratón o mando.
- **Ciclo de día y noche** con la posición real del sol en Guadalupe para la fecha del escenario: faroles, iluminación monumental, ventanas encendidas, campanadas cada cuarto de hora, pájaros y palomas que se posan en la plaza y huyen del jugador.
- **Menú principal** sobre la plaza para elegir escenario (día normal, fin de semana o festivo, gran peregrinación y Navidad) y franja horaria (mañana, tarde o noche); la plaza cambia al momento.
- **Turistas autónomos** que llegan solos o en grupo, visitan la fuente, el monasterio, el atrio y las terrazas, hacen fotos, charlan o se sientan, con más o menos gente según el escenario y la hora.
- **Personajes del pueblo**: Fray Paulino, José Luis (Café Bar Cerezo), Paqui (Unide), Marisé (guía turística) e Inés María, cada uno en su sitio y con su forma de ser. Al acercarse saludan según el momento: se presentan la primera vez, comentan la hora, el día o la gente que hay y, si ya saben tu nombre, lo usan. Inés María te llama si pasas cerca.
- **Conversación por texto** con los personajes principales mediante Google Gemini: el personaje habla primero y su respuesta aparece según se genera, mientras gesticula. Cada uno responde según su perfil, la hora, el escenario y lo que hayas contado a los demás. José Luis te da la mano antes de empezar.

## Documentación

| Documento | Contenido |
|---|---|
| [Guía rápida](docs/instalacion.md) | Descargar y ejecutar, abrir el proyecto, clave de Gemini, controles y recorrido de prueba |
| [Arquitectura](docs/arquitectura.md) | Escena, módulos y sus dependencias, flujo de una visita y configuración |
| [Entorno](docs/entorno.md) | Datos geográficos, generador de la plaza, casas y soportales, monasterio, iluminación y navegación |
| [Personajes](docs/personajes.md) | Turistas, personajes principales, perfiles y conversación |
| [Rendimiento](docs/rendimiento.md) | Método de medida, optimizaciones y resultados |
| [Decisiones](docs/decisiones.md) | Decisiones de diseño, limitaciones y trabajo futuro |
| [Convenciones](docs/convenciones.md) | Código, assets, importación y escena |
| [Uso de IA](docs/uso-de-ia.md) | Herramientas de IA usadas en el desarrollo y en el juego |

## Tecnologías

- Unity 6.3 LTS (6000.3.25f1) · Universal Render Pipeline · UI Toolkit
- C# · AI Navigation (NavMesh) · Input System · Cinemachine · Starter Assets
- FBX Exporter para los modelos singulares
- Personajes de Microsoft Rocketbox y MakeHuman (MPFB para Blender), animaciones de Mixamo
- Google Gemini API

## Cómo empezar

Descarga la aplicación para macOS, Windows o Linux desde [Releases](https://github.com/cesarsm24/PlazaGuadalupe/releases/latest), o abre el proyecto con Unity 6000.3.25f1 y Git LFS. La [guía rápida](docs/instalacion.md) explica cómo ejecutarlo, activar la conversación con Gemini, los controles y qué probar en cinco minutos.

## Estructura

```
Assets/_Guadalupe/
├── Art/                Fuentes, materiales, modelos (FBX y mallas generadas), personajes, shaders, terreno y texturas
├── Audio/Ambience/     Sonido ambiente y campana
├── Data/Geo/           Datos geográficos abiertos (Catastro, EU-DEM, OSM)
├── Data/Design/        Catálogo de casas y trazado de la plaza (JSON)
├── Prefabs/            Jugador, monasterio, mobiliario, elementos singulares, palomas, turistas y personajes principales
├── Scenes/             Escena de la plaza y su NavMesh
├── ScriptableObjects/  Escenarios, estado de la sesión, perfiles de los personajes y reglas de la conversación
├── Scripts/            Código por módulos (ver abajo)
├── Settings/           Acciones de entrada, render (URP y postproceso) y recursos del generador
└── UI/                 Menú principal e interfaz de juego (UXML, USS y tema)
Assets/StreamingAssets/ Plantilla de configuración de la conversación con IA
Assets/ThirdParty/      Starter Assets (controlador del jugador)
Media/                  Capturas de este README
Tools/GeoData/          Descarga de los datos geográficos
Tools/Audio/            Síntesis del sonido ambiente
```

Cada carpeta de `Scripts/` es un ensamblado propio; los módulos, sus dependencias y la jerarquía de la escena se describen en [docs/arquitectura.md](docs/arquitectura.md).

## Datos geográficos

El entorno se genera a partir de datos abiertos (edificios del Catastro, elevaciones EU-DEM y OpenStreetMap) con **Guadalupe → Entorno → Generar entorno de la plaza**. Los datos están en `Assets/_Guadalupe/Data/Geo/` y se vuelven a descargar con:

```bash
python3 Tools/GeoData/descargar_datos.py
```

Cómo funciona el generador y qué se coloca a mano está en [docs/entorno.md](docs/entorno.md).

## Créditos y licencias

- Edificios: © Dirección General del Catastro, CC BY 4.0.
- Elevaciones: EU-DEM v1.1, © Copernicus Land Monitoring Service (Unión Europea), vía OpenTopoData.
- Cartografía: © colaboradores de OpenStreetMap, ODbL.
- Texturas: [ambientCG](https://ambientcg.com), CC0 (detalle en `Assets/_Guadalupe/Art/Textures/AmbientCG/LICENSE.txt`).
- Texturas de la fachada gótica y de la mampostería del monasterio (`T_GothicFacade`, `T_MonasteryMasonry`): elaboradas a partir de «Real Monasterio de Santa María de Guadalupe. Fachada» (José Luis Filpo Cabana, CC BY 3.0) y «De bronce - La puerta izquieda» (Jacinta Lluch Valero, CC BY-SA 2.0), de [Wikimedia Commons](https://commons.wikimedia.org); se distribuyen con licencia CC BY-SA.
- Tipografías de los rótulos y de la interfaz: UnifrakturMaguntia, Kalam, Cinzel y Lato ([Google Fonts](https://fonts.google.com)), SIL Open Font License 1.1 (detalle en `Assets/_Guadalupe/Art/Fonts/LICENSE.txt`).
- Controlador del jugador: Starter Assets (Unity Technologies).
- Turistas: [Microsoft Rocketbox](https://github.com/microsoft/Microsoft-Rocketbox), licencia MIT (detalle en `Assets/_Guadalupe/Art/Characters/Rocketbox/LICENSE.txt`); texturas reducidas a 1024 y dos variantes del cuerpo con la ropa de otro color.
- Jugador y personajes principales: creados con [MakeHuman](https://static.makehumancommunity.org/) mediante MPFB para Blender; recursos del paquete de sistema de MakeHuman, CC0.
- Animaciones de los personajes: [Mixamo](https://www.mixamo.com) (Adobe), uso libre en proyectos propios.
- Sonido ambiente: sintetizado con `Tools/Audio/sintetizar_ambiente.py`.

## Autor

César Sánchez Montes

## Licencia

Todos los derechos reservados: el proyecto se puede consultar y ejecutar para verlo y evaluarlo, pero no copiar, modificar ni reutilizar sin permiso del autor (ver [LICENSE](LICENSE)). Los recursos de terceros y sus derivados conservan su licencia (ver «Créditos y licencias»).
