# Decisiones de diseño

Las decisiones que más condicionan el proyecto, cada una con su contexto, lo que se decidió y sus consecuencias. El
detalle técnico está en el resto de documentos.

## D1 · Unity 6.3 LTS con URP
- **Contexto:** entrega con fecha fija y asignatura basada en URP.
- **Decisión:** Unity 6000.3.25f1 LTS con Universal Render Pipeline, UI Toolkit, Input System y Cinemachine.
- **Consecuencias:** versión estable y compatible con los paquetes usados, a cambio de no tener las novedades de las
  versiones tech stream.

## D2 · Entorno propio en lugar de Cesium o 3D Tiles
- **Contexto:** los modelos fotogramétricos (Cesium, Google 3D Tiles) pesan mucho, necesitan conexión y una clave, no
  se pueden editar y complican colisiones, NavMesh y estilo.
- **Decisión:** construir el entorno con datos abiertos (Catastro, EU-DEM y OpenStreetMap) y modelado propio, con el
  máximo detalle en el monasterio.
- **Consecuencias:** ejecutable sin conexión, editable y con un estilo coherente; menos fidelidad fotográfica lejos de
  la plaza.

## D3 · Edificios del Catastro y terreno EU-DEM
- **Contexto:** en OpenStreetMap faltan casas de los lados este y sur de la plaza. El MDT de 5 m del CNIG exige una
  descarga manual.
- **Decisión:** partes de edificio del Catastro (servicio INSPIRE), con su número de plantas, y elevaciones EU-DEM de
  25 m por API; OpenStreetMap solo para la plaza, el monasterio y la fuente.
- **Consecuencias:** entorno completo con alturas reales escalonadas y descarga automatizada
  (`Tools/GeoData`). La menor resolución del terreno se compensa aplanando la plaza con un plano ajustado.

## D4 · Entorno generado por una herramienta de Editor
- **Contexto:** hay más de 600 partes de edificio y los datos o el diseño pueden cambiar.
- **Decisión:** un generador C# de Editor, atómico y determinista, con el diseño de cada casa y del soportal en
  ficheros JSON validados. Detalle de fachada solo a menos de 90 m de la plaza.
- **Consecuencias:** el entorno se regenera en segundos y siempre igual; el detalle está donde se ve y la geometría
  lejana es barata. Ver [entorno.md](entorno.md).

## D5 · Monasterio y atrio como modelos FBX
- **Contexto:** generar el monasterio por código en cada regeneración lo hacía opaco y difícil de retocar.
- **Decisión:** construirlo una vez con una herramienta propia (`MonasteryModelExporter`), exportarlo a FBX y usarlo
  como prefab.
- **Consecuencias:** en la escena es un asset normal; la herramienta solo hace falta si cambian sus medidas.

## D6 · Una sola escena y un ensamblado por módulo
- **Contexto:** el plan inicial tenía escenas de arranque, menú y plaza.
- **Decisión:** una única escena con el menú y el chat como paneles sobre la plaza, y un asmdef por módulo con
  dependencias explícitas (`Core` no depende de nada).
- **Consecuencias:** sin pantallas de carga ni objetos persistentes entre escenas; compilación por módulos y sin
  dependencias circulares. Ver [arquitectura.md](arquitectura.md).

## D7 · Cuatro escenarios y tres franjas, con el tiempo a ritmo real
- **Contexto:** la plaza cambia mucho según el día y la hora, y la demo necesita casos comparables.
- **Decisión:** escenarios *Día normal*, *Fin de semana o festivo*, *Gran peregrinación* (8 de septiembre) y *Navidad*
  (21 de diciembre), como ScriptableObjects, y franjas *Mañana* (10:30), *Tarde* (poco antes de la puesta de sol) y
  *Noche* (22:30). El reloj avanza a tiempo real, sin reloj en pantalla ni control de velocidad.
- **Consecuencias:** 12 combinaciones desde el menú, que se cambian en caliente con Esc; el sol se calcula para la
  fecha de cada escenario.

## D8 · Conversación con Gemini en streaming
- **Contexto:** la conversación es el núcleo de la interacción con los personajes. Un modo sin conexión con frases
  fijas daba una experiencia pobre.
- **Decisión:** `GeminiClient` detrás de la interfaz `ILanguageModel`, con respuestas en streaming. Los personajes
  abren con una frase escrita, sin esperar a la API, y se disculpan si no hay respuesta.
- **Consecuencias:** coste nulo con el plan gratuito y proveedor intercambiable; hace falta clave y red para conversar,
  pero el resto del juego funciona sin ellas.

## D9 · Perfiles de personaje como ScriptableObjects
- **Contexto:** los datos de cada personaje deben poder editarse y comprobarse sin tocar código.
- **Decisión:** `NpcProfile` con papel, ubicación, personalidad, temas, datos, prudencias, remisiones y frases por
  momento, en lugar de un JSON en StreamingAssets.
- **Consecuencias:** se editan en el Inspector sin tocar código; los personajes solo saben lo que hay en su
  perfil. Ver [personajes.md](personajes.md).

## D10 · Memoria compartida de la visita
- **Contexto:** sin memoria, cada conversación empezaría de cero y los personajes parecerían no conocerse, aunque
  viven en la misma plaza.
- **Decisión:** al despedirse, la conversación se resume en una `VisitNote` (nombre del jugador y lo que ha contado)
  que se incluye en las instrucciones de los demás personajes.
- **Consecuencias:** los personajes usan tu nombre y lo que has contado, con una petición más al modelo por
  conversación.

## D11 · Alcance centrado en la parte gráfica
- **Contexto:** el planteamiento inicial incluía un módulo de gestión urbana con sensores, indicadores y mapa de calor.
- **Decisión:** se descartó para dedicar el tiempo al entorno, los personajes y la conversación.
- **Consecuencias:** menos funciones, pero mejor acabado en lo que se ve y se usa.

## D12 · Qué es estático y qué no
- **Contexto:** el mobiliario no era estático y las sombras del sol cubrían 150 m con 4 cascadas.
- **Decisión:** batching estático del mobiliario fijo, con las terrazas dinámicas, culling por tamaño en pantalla,
  occlusion culling y sombras hasta 80 m con 3 cascadas.
- **Consecuencias:** entre un 17 % y un 51 % menos de draw calls según la vista, sin cambio visible. Ver
  [rendimiento.md](rendimiento.md).

## D13 · Luz de noche y luces de Navidad
- **Contexto:** con luz ambiental de tres colores, la intensidad ambiental de Unity no tiene efecto y las fachadas se
  veían casi como al atardecer. El suelo recibe muchas luces y URP solo aplica unas pocas por objeto.
- **Decisión:** de noche se escalan los tres colores ambientales y los reflejos del cielo (al 70 %), la cúpula de nubes
  pinta un cielo estrellado, y el resplandor del árbol de Navidad en el suelo es un disco aditivo en lugar de una luz.
- **Consecuencias:** noche más creíble, en la que destacan faroles y focos, sin coste de luces adicionales.

## Limitaciones

- **Plataformas:** hay ejecutables para macOS (Apple silicon e Intel), Windows y Linux de 64 bits. Los de Windows y
  Linux se compilan desde macOS y no se han probado en esos sistemas.
- **Conversación en línea:** hablar con los personajes necesita conexión y una clave de Gemini, y sus respuestas
  pueden contener imprecisiones.
- **Zona acotada:** se recorre la plaza y su entorno inmediato; no se entra al monasterio ni a las calles del pueblo.
- **Afluencia simulada:** la gente de cada escenario sigue curvas de diseño, no datos reales.

## Trabajo futuro

- **Gemelo digital de la plaza:** sustituir la afluencia simulada por datos reales (conteos de visitantes, ocupación
  de terrazas, tiempo) para ver la plaza tal como está en cada momento y probar medidas de gestión.
- **Probar en más equipos:** las versiones de Windows y Linux en sus sistemas, y la de macOS en distintos Mac.
- **Voz:** que los personajes hablen y escuchen además de escribir.
- **Casas más realistas:** fachadas de la plaza con más detalle y texturas propias de cada casa.
- **Más entorno:** el interior de la iglesia y las calles que llevan a la plaza.
