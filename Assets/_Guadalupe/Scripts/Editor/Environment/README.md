# Generador del entorno

Herramienta de Editor que construye la plaza de Santa María de Guadalupe a partir de datos
geográficos reales. Se ejecuta desde **Guadalupe → Entorno → Generar entorno de la plaza** y
sustituye el objeto `Entorno` de la escena `PlazaGuadalupe` (marcado con `GeneratedEnvironment`).

El generador construye solo lo que sale de los datos: terreno, casas, soportales, rótulos y arcos de
calle. El monasterio y el atrio son un modelo (`PF_Monastery`) colocado en la escena, igual que el
mobiliario.

## Flujo

`PlazaEnvironmentGenerator.Generate` ejecuta los pasos en orden, compartiendo un único
`GenerationContext` (datos cargados, objetos de la escena, terreno, materiales y resultados intermedios):

1. **Comprobaciones**: `DesignDataValidator` revisa los ficheros de diseño, los materiales y los ajustes, y
   se comprueba que la escena tiene un `NavMeshSurface` y el agente las medidas del jugador.
2. **Datos**: planta de edificios del Catastro, calles y contornos de OpenStreetMap, modelo digital de
   elevaciones, ficheros de diseño de la plaza (`Data/Design`) y ajustes del generador (`GeneratorSettings`).
3. **Escena**: localiza lo colocado a mano que necesita lo generado (sol, reloj, controladores
   de iluminación y el monasterio, que debe estar una sola vez) y asigna el reloj de la simulación al
   `TowerClock` del monasterio.
4. **Terreno**: alturas del MDE, explanada de la plaza y capas de pavimento, de momento solo en memoria.
5. **Pueblo**: una malla por parte catastral, con soportales, fachadas, rótulos y arcos de calle.
6. **Iluminación**: genera los faroles de pared y las cortinas de luz de los balcones, y asigna a los
   controladores de `Iluminacion` las luces que vienen en prefabs (farolas, fuente, árbol y monasterio).
7. **NavMesh**: `NavigationBaker` hornea el `NavMeshSurface` de `Navegacion` (capas `Environment` y
   `Props`). También se puede hornear sola desde **Guadalupe → Entorno → Hornear NavMesh**.
8. **Guardado**: el terreno se copia en `TD_Guadalupe`, las mallas se guardan por grupo
   (`SM_Buildings`, `SM_Signs`, `SM_BalconyCurtains`), se guarda la NavMesh y el entorno nuevo
   sustituye al anterior.

La generación es atómica: todo lo anterior al guardado se hace en memoria, con el entorno anterior
desactivado. Si algo falla antes, se descarta lo nuevo y la escena y los recursos quedan como estaban.

Los ficheros de diseño se pueden revisar sin generar desde **Guadalupe → Entorno → Validar datos de diseño**.

## Modelo del monasterio

El monasterio y el atrio se modelaron en código, y ese código se conserva como herramienta:
**Guadalupe → Entorno → Reexportar modelo del monasterio** (`MonasteryModelExporter`, en
`Scripts/Editor/MonasteryExport`) construye el monasterio, el atrio, sus focos y sus adornos de Navidad
sobre el terreno actual, los exporta como `SM_Monastery.fbx` y `SM_Atrium.fbx`, actualiza `PF_Monastery`
sin perder sus componentes y vuelve a asignar los focos al controlador de la escena. Solo hace falta si
cambian sus medidas.

## Carpetas

| Carpeta      | Contenido |
|--------------|-----------|
| `Generation` | Punto de entrada y contexto de la generación, repartido en datos de entrada (`GenerationData`), lo colocado en la escena (`SceneServices`), la raíz generada (`GeneratedRoot`) y los resultados intermedios (`GenerationResults`); ajustes, materiales, prefabs, trazado de la plaza y nombres de la jerarquía. |
| `Data`       | Lectura de Catastro, OSM, MDE y ficheros de diseño; proyección geográfica, catálogo de casas y validación (`DesignDataValidator`). |
| `Geometry`   | `MeshBuilder`, utilidades de polígonos y polilíneas, hash estable y piezas arquitectónicas reutilizables. |
| `Plan`       | Medidas del atrio y del frente del monasterio (`AtriumPlan`) y su marco de coordenadas (`PlanFrame`). |
| `Terrain`    | Terreno, forma del suelo y pintado de capas. |
| `Town`       | Casas: análisis de soportales, aleros, fachadas, escaparates, rótulos y arcos de calle. |
| `Lighting`   | Faroles de pared, cortinas de luz de los balcones y asignación de las luces a los controladores. |
| `Navigation` | Horneado de la NavMesh. |

La herramienta del monasterio está en `Scripts/Editor/MonasteryExport`, en su propio ensamblado: usa el
generador, pero el generador no la conoce.

## Convenciones

- Cada clase `XxxBuilder` es estática, recibe el contexto y crea sus objetos bajo un grupo de
  la jerarquía (`Edificios`, `Rotulos`...), cuyo nombre declara en `GroupName`.
- Los nombres de la jerarquía siguen `HierarchyNames`: español en ASCII y `Tipo_Identificador`, con los
  números en dos cifras (`Parte_02`).
- Las mallas se construyen con `MeshBuilder` por submallas; las clases `*Slots` fijan qué
  material corresponde a cada índice.
- La generación es determinista: las variaciones (contraventanas, balcones...) usan semillas
  derivadas de la referencia catastral, de modo que dos ejecuciones producen la misma escena.
- El atrio y el frente del monasterio se describen en un marco local (u, v) en metros desde la fuente,
  con cotas de nivel, y `PlanFrame` lo pasa al mundo; el resto del entorno usa coordenadas locales de OSM.
- Los ajustes (`GeneratorSettings`), los materiales (`EnvironmentMaterials`) y los prefabs (`PropPrefabs`)
  son assets editables en el Inspector.
- Lo que se diseña a mano está en la escena, fuera de `Entorno`: monasterio, mobiliario, límite jugable,
  iluminación, navegación, simulación, jugador, sol, cielo, sonido, palomas e interfaz. El generador no
  lo mueve ni lo borra; solo le asigna lo que depende de lo generado (el reloj de la torre, las luces de
  los controladores y los adornos de Navidad, que deja ocultos).
