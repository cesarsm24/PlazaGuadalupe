# Rendimiento

Optimización de la escena: cómo se midió, qué se cambió y con qué resultado.

## Método

Las medidas se tomaron en el Editor, en Play, con las estadísticas de render de Unity (draw calls, batches,
triángulos y objetos que proyectan sombra). Se usaron tres vistas fijas de la plaza a la altura de los ojos, con el
control de la cámara desactivado para que fueran comparables:

1. Desde la fuente hacia el monasterio.
2. Desde la lonja hacia las terrazas.
3. Desde las terrazas hacia la fuente.

Cada vista se midió varias veces de día, con el escenario *Día normal*, antes y después de los cambios. Los turistas
llegan al azar, así que entre muestras hay algo de variación.

## Punto de partida

- 1.492 renderers de malla; el mobiliario no era estático y no había LOD ni datos de oclusión.
- Sombras del sol hasta 150 m con 4 cascadas.
- Los personajes principales se animaban aunque estuvieran fuera de pantalla (50 renderers con piel).
- El SRP Batcher ya estaba activo y los edificios y el monasterio ya eran estáticos.

## Cambios

- **Batching estático del mobiliario fijo:** macetas, papeleras, bolardos, farolas, puestos, señales, monumentos,
  fuente y árbol de Navidad. Las mesas y sillas de las terrazas siguen dinámicas: los turistas apartan las sillas para
  sentarse, y el planificador de asientos mide el tablero con su propia malla, que el batching estático sustituye por
  la malla combinada.
- **Culling por distancia:** LOD Group de un solo nivel que deja de dibujar el objeto cuando ocupa poco en pantalla:
  macetas al 3 %, puestos al 2,5 %, terrazas, papeleras, bolardos y atril al 2 %, turistas y personajes principales
  al 1,5 %.
- **Occlusion culling:** datos horneados con los edificios como oclusores (oclusor mínimo de 3 m) y todo lo estático
  como ocluible.
- **Sombras del sol:** hasta 80 m con 3 cascadas. La cámara del menú está a 56 m del monasterio, así que no pierde
  sombras, y en las capturas de antes y después no se aprecia diferencia.
- **Animación:** los Animator usan *Cull Update Transforms* y los personajes principales ya no se animan fuera de
  pantalla. Para que no se corten al sentarse, usan una caja de límites fija de 2,2 m alrededor del hueso raíz.
  Se comprobó con la malla deformada que ningún vértice se sale de ella, ni de pie, ni andando, ni sentados.

## Resultados (día)

| Vista | Draw calls | Batches | Triángulos | Objetos en sombras |
|---|---|---|---|---|
| Fuente → monasterio | 7.160 → 4.340 (−39 %) | 2.200 → 1.840 (−17 %) | 1,28 M → 1,08 M | 4.310 → 2.770 (−36 %) |
| Lonja → terrazas | 3.810 → 1.880 (−51 %) | 760 → 470 (−38 %) | 0,75 M → 0,61 M | 2.830 → 1.580 (−44 %) |
| Terrazas → fuente | 2.840 → 2.010 (−29 %) | 1.460 → 1.200 (−18 %) | 0,95 M → 0,79 M | 1.940 → 1.340 (−31 %) |

El mayor ahorro viene de las sombras: con menos distancia y una cascada menos, cada objeto se dibuja menos veces en
los mapas de sombras. El batching estático reduce las llamadas del mobiliario, y el culling por distancia y la
oclusión quitan lo que no se ve desde la plaza.

## Noche

De noche la escena pesa menos que de día: en la vista de las terrazas, unos 970 draw calls frente a 2.010. Los
proyectores del monasterio son las únicas luces con sombra además del sol, y cuestan unos 110 draw calls y 200.000
triángulos en la vista que los ve. Se dejaron como estaban.

## Cómo repetir la medida

1. Abrir la escena `PlazaGuadalupe` y entrar en Play.
2. Empezar la visita en *Día normal*, por la mañana.
3. Con la ventana *Stats* del Game view (o el Profiler, módulo *Rendering*), anotar los valores desde las vistas de
   la fuente, la lonja y las terrazas.
