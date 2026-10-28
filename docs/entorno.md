# Entorno de la plaza

Cómo se construyen la plaza, el pueblo y el monasterio, y cómo se iluminan según la hora y el escenario.

![Vista aérea de la plaza generada, con el monasterio al fondo](img/vista-aerea.jpg)

## Datos de partida

El entorno sale de datos geográficos abiertos, guardados en `Assets/_Guadalupe/Data/Geo/` y descargables de nuevo
con `python3 Tools/GeoData/descargar_datos.py`:

| Dato | Fuente | Uso |
|---|---|---|
| Edificios | Catastro (servicio INSPIRE), partes de edificio con número de plantas | Planta y altura de cada casa |
| Elevaciones | EU-DEM v1.1 (25 m), vía OpenTopoData | Terreno |
| Plaza, monasterio y fuente | OpenStreetMap | Contornos y calles |

Sobre ellos hay dos ficheros de diseño en `Assets/_Guadalupe/Data/Design/`:

- `houses.json`: el aspecto de cada casa de la plaza por referencia catastral (colores, zócalo, carpintería,
  balcones, portadas, rótulos y plantas).
- `plaza_layout.json`: el trazado de los soportales y de los arcos de calle.

`DesignDataValidator` comprueba los dos ficheros antes de cada generación; también se pueden revisar sin generar desde
**Guadalupe → Entorno → Validar datos de diseño**.

## Generador

**Guadalupe → Entorno → Generar entorno de la plaza** (`PlazaEnvironmentGenerator`) construye todo lo que sale de los
datos y sustituye el grupo `Entorno` de la escena. Los pasos comparten un `GenerationContext`:

1. **Comprobaciones:** ficheros de diseño, materiales, ajustes y medidas del agente de la NavMesh.
2. **Datos:** lectura del Catastro, OSM, el modelo de elevaciones y los ficheros de diseño.
3. **Escena:** localiza lo colocado a mano que necesita lo generado (sol, reloj, controladores de iluminación y el
   monasterio).
4. **Terreno:** alturas del modelo de elevaciones y capas de pavimento. La plaza se aplana con un plano ajustado por
   mínimos cuadrados, conservando la mitad de su pendiente para que no quede artificialmente horizontal.
5. **Pueblo:** una malla por parte catastral, con tejados, fachadas, ventanas, rótulos, soportales y arcos de calle.
6. **Iluminación:** faroles de pared y cortinas de luz de los balcones, y asignación de todas las luces a sus
   controladores.
7. **NavMesh:** horneado de la superficie de `Navegacion` con el entorno, el monasterio y el mobiliario.
8. **Guardado:** terreno, mallas por grupo y NavMesh como assets, y sustitución del entorno anterior.

Propiedades del generador:

- **Atómico:** todo lo anterior al guardado se hace en memoria. Si algo falla, la escena y los recursos quedan como
  estaban.
- **Determinista:** las variaciones de cada casa (contraventanas, balcones) usan semillas derivadas de su
  referencia catastral, así que dos generaciones producen la misma escena.
- **No toca lo colocado a mano:** monasterio, mobiliario, límite jugable, jugador e interfaz están fuera de `Entorno`.
  El generador solo les asigna lo que depende de lo generado.

## Casas y soportales

- Cada parte catastral se extruye con su número de plantas y lleva su tejado, alero y acabados del catálogo.
- El detalle de fachada (huecos, balcones, rejas, escaparates y rótulos) se coloca solo en las casas a menos de 90 m
  de la plaza y nunca en medianeras: cerca se ve, y lejos no cuesta geometría.
- El soportal corrido de la plaza sigue una polilínea explícita del trazado (`plaza_layout.json`). Las franjas del
  Catastro bajo él toman la altura de la casa de detrás, de modo que el soportal queda continuo.
- Los rótulos de los comercios usan tipografías propias y un shader de texto (`SignText`) que respeta la profundidad,
  solo se dibuja por la cara delantera y se desvanece con la distancia, de modo que se leen al acercarse.

## Monasterio y atrio

| Real | En el juego |
|---|---|
| ![La fachada del monasterio y la fuente, en la realidad](img/fachada-real.jpg) | ![La misma vista en el juego](img/fachada-juego.jpg) |


El monasterio y el atrio se modelaron por código y se guardan como modelos FBX (`SM_Monastery`, `SM_Atrium`) dentro
del prefab `PF_Monastery`. En la escena son assets normales que se pueden mover, inspeccionar y optimizar como
cualquier otro.

- **Guadalupe → Entorno → Reexportar modelo del monasterio** (`MonasteryModelExporter`) los vuelve a construir sobre el
  terreno actual, los exporta, actualiza el prefab sin perder sus componentes y reasigna los focos. Solo hace falta si
  cambian sus medidas.
- El atrio y el frente se describen en un marco local en metros desde la fuente (`AtriumPlan`, `PlanFrame`): la
  escalinata de 22 peldaños de 17 cm, la lonja, el arco del pasadizo, las balaustradas y las torres.
- La fachada gótica y la mampostería usan texturas propias (`T_GothicFacade`, `T_MonasteryMasonry`); sus créditos
  están en el README.
- El reloj de la torre (`TowerClock`) marca la hora de la simulación.

## Mobiliario

Fuente, terrazas, farolas, macetas, papeleras, bolardos, puestos de los comercios, señales, monumentos y árbol de
Navidad son prefabs colocados a mano en `Mobiliario`. La fuente tiene ocho chorros de partículas y el árbol de Navidad
es un cono de luces con estrella. Qué piezas son estáticas y por qué las terrazas no lo son se explica en
[rendimiento.md](rendimiento.md).

## Iluminación y ambiente

| Tarde | Noche |
|---|---|
| ![La plaza por la tarde](img/plaza-tarde.jpg) | ![La misma vista de noche, con los focos del monasterio](img/plaza-noche.jpg) |

| Real | En el juego |
|---|---|
| ![La fachada iluminada de noche, en la realidad](img/noche-real.jpg) | ![La fachada de noche en el juego](img/noche-juego.jpg) |
| ![La plaza en Navidad, en la realidad](img/navidad-real.jpg) | ![La plaza en Navidad en el juego](img/navidad-juego.jpg) |

- **Sol:** posición real para la latitud y longitud de Guadalupe y la fecha del escenario, con crepúsculo continuo.
  Sombras hasta 80 m con 3 cascadas.
- **Luz ambiental:** modo de tres colores (cielo, horizonte y suelo). De noche se atenúa junto con los reflejos del
  cielo para que destaquen los faroles.
- **Cielo:** skybox procedural y una cúpula de nubes (`Clouds`) que se tiñe según la hora. De noche la cúpula pinta un
  degradado azul marino con estrellas.
- **Faroles y ventanas:** se encienden de forma gradual al ponerse el sol (`StreetLights`).
- **Focos del monasterio:** proyectores con sombra que iluminan la fachada de noche (`NightFloodLights`).
- **Navidad:** cortinas de luz en los balcones, adornos del monasterio y árbol de luces (`FestiveLights`, shader
  aditivo `FestiveLights`). De día se ven las bombillas apagadas del árbol y de noche deja un resplandor cálido en el
  suelo. Las mesas que ocupa el árbol se ocultan.
- **Postproceso:** volumen (`VP_Plaza`) con tonemapping, bloom para las luces, ajuste de color y viñeta.
- **Sonido:** pájaros de día, el agua de la fuente y campanadas cada cuarto de hora, sintetizados con
  `Tools/Audio/sintetizar_ambiente.py`.
- **Palomas:** una bandada que se posa en la plaza, huye del jugador y no sale de noche.

## Navegación

La NavMesh (`Navegacion`) cubre la plaza, el atrio y las calles cercanas. El límite jugable (`LimiteJugable`) son
tapias invisibles que cierran la zona. Se vuelve a hornear sin regenerar con **Guadalupe → Entorno → Hornear
NavMesh** (`NavigationBaker`). Los turistas y los personajes principales la usan para moverse. El árbol de Navidad y
el jugador son obstáculos que la recortan en juego.

## Código

El generador está en `Assets/_Guadalupe/Scripts/Editor/Environment`, por áreas (`Generation`, `Data`, `Geometry`,
`Plan`, `Terrain`, `Town`, `Lighting`, `Navigation`), y la herramienta del monasterio en
`Scripts/Editor/MonasteryExport`, en un ensamblado propio. El README de esa carpeta describe las convenciones internas
(un `XxxBuilder` estático por grupo de la jerarquía, mallas por submallas con `MeshBuilder` y nombres de
`HierarchyNames`).
