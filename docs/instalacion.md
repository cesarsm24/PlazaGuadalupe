# Guía rápida

Cómo ejecutar el juego o abrir el proyecto, activar la conversación con los personajes y qué probar en cinco
minutos.

## Descargar y ejecutar

Descarga de la [última versión](https://github.com/cesarsm24/PlazaGuadalupe/releases/latest) el `.zip` de tu sistema y descomprímelo. Junto a la aplicación van un
`LEEME.txt`, la licencia y la plantilla `config.example.json`.

**macOS** (Apple silicon o Intel), `PlazaGuadalupe-macOS.zip`: abre `Plaza de Guadalupe.app`. La aplicación no está
firmada por Apple, así que la primera vez macOS la bloquea:

- **macOS 15 o posterior:** cierra el aviso y ve a **Ajustes del Sistema → Privacidad y seguridad**; abajo, junto al
  mensaje sobre «Plaza de Guadalupe», pulsa **Abrir igualmente** y confirma.
- **macOS 14 o anterior:** **clic derecho → Abrir** y confirma.

**Windows** (10 u 11, 64 bits), `PlazaGuadalupe-Windows.zip`: abre `Plaza de Guadalupe.exe`, sin separarlo de la
carpeta `Plaza de Guadalupe_Data`. Si aparece «Windows protegió su PC», pulsa **Más información → Ejecutar de todas
formas**.

**Linux** (64 bits), `PlazaGuadalupe-Linux.zip`: ejecuta `Plaza de Guadalupe.x86_64`, sin separarlo de la carpeta
`Plaza de Guadalupe_Data`. Si no arranca con doble clic, dale permiso de ejecución desde una terminal en esa carpeta:

```bash
chmod +x "Plaza de Guadalupe.x86_64"
./"Plaza de Guadalupe.x86_64"
```

En todos los casos solo hace falta la primera vez. Para conversar con los personajes hace falta conexión a internet y
una clave de Gemini (ver más abajo).

## Abrir el proyecto en Unity

Para ver el código y la escena hace falta Unity 6000.3.25f1 (Unity 6.3 LTS), instalado con Unity Hub, y
[Git LFS](https://git-lfs.com), porque modelos, texturas, audio y datos grandes van por LFS.

```bash
git lfs install
git clone https://github.com/cesarsm24/PlazaGuadalupe.git
```

Abre la carpeta con Unity Hub y carga la escena `Assets/_Guadalupe/Scenes/PlazaGuadalupe.unity` (también desde el
menú **Guadalupe → Abrir la plaza**). Pulsa Play: aparece el menú principal sobre la plaza.

## Activar la conversación

1. Consigue una clave en [Google AI Studio](https://aistudio.google.com).
2. Copia la plantilla `config.example.json` (va junto a la aplicación en el `.zip`, y en `Assets/StreamingAssets/` en
   el proyecto) con el nombre `config.local.json` y guárdala en:
   - **macOS:** `~/Library/Application Support/César Sánchez Montes/Plaza de Guadalupe/` (en Finder, **Ir → Ir a la
     carpeta**).
   - **Windows:** `%USERPROFILE%\AppData\LocalLow\César Sánchez Montes\Plaza de Guadalupe\` (pégala en la barra del
     Explorador de archivos).
   - **Linux:** `~/.config/unity3d/César Sánchez Montes/Plaza de Guadalupe/` (carpeta oculta).
   - **Editor:** la carpeta `UserSettings/` del proyecto.

   Crea la carpeta si no existe.
3. Sustituye `PEGA_AQUI_TU_API_KEY` por la clave. En el mismo fichero se cambian el modelo, el tiempo máximo de
   respuesta y la longitud de las respuestas.

El fichero no se sube al repositorio ni va dentro del ejecutable. Sin él, el juego funciona igual, pero los personajes
se disculpan al intentar hablar con ellos y el chat indica dónde poner el fichero.

## Controles

| Acción | Teclado y ratón | Mando |
|---|---|---|
| Moverse | WASD o flechas | Stick izquierdo |
| Mirar | Ratón | Stick derecho |
| Girar la cámara | Q / E | — |
| Correr | Mayúsculas | Gatillo izquierdo (LT / L2) |
| Saltar | Espacio | Botón inferior (A / Cruz) |
| Primera o tercera persona | V | Select (View / Share) |
| Abrir el menú | Esc | Start (Menu / Options) |
| Volver o cerrar el menú | Esc | Botón derecho (B / Círculo) |
| Hablar con un personaje | F | — |
| Enviar un mensaje | Enter | — |
| Despedirse (terminar la conversación) | Esc | — |

## Qué probar en cinco minutos

1. **Día normal, tarde.** Pasea por la plaza: turistas que llegan, se sientan en las terrazas y hacen fotos, y
   palomas que huyen al acercarte. Pulsa V para cambiar a primera persona.
2. **José Luis.** En la puerta del Café Bar Cerezo, bajo los soportales del lado este. Al acercarte te saluda; pulsa
   F, te da la mano y empieza la conversación. Pregúntale por la morcilla o dile tu nombre y despídete con Esc.
3. **Otro personaje.** Habla con Paqui (frente sur) o con Fray Paulino (en el atrio): ya saben tu nombre.
4. **Navidad, noche.** Pulsa Esc, elige Navidad y Noche: luces en los balcones y en el monasterio, cielo estrellado y
   el árbol encendido. Inés María se ha cambiado de mesa.
5. **Gran peregrinación.** La plaza llena de gente y el atrio saturado.
