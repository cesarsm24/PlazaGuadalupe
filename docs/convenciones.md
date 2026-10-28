# Convenciones

Reglas de nombres, estilo y organización que sigue todo el proyecto.

## Código C#

- Identificadores en inglés; comentarios y documentación en español.
- Namespace `Guadalupe.<Módulo>`, uno por ensamblado aunque haya subcarpetas, con bloque de llaves (Unity 6 usa C# 9).
- Un tipo principal por fichero, con el mismo nombre. Los enums y structs pequeños que solo sirven a una clase van en
  su propio fichero.
- `PascalCase` para tipos, métodos, propiedades, eventos y constantes; `_camelCase` para campos privados; `camelCase`
  para parámetros y variables locales. Interfaces con prefijo `I`.
- Siglas de tres o más letras como palabra (`NpcProfile`, `LlmSettings`); el módulo `NPC` conserva su nombre.
- `[SerializeField] private` en lugar de campos públicos, expuestos con propiedades de solo lectura.
- Llaves estilo Allman, siempre también en `if` y `for` de una línea.
- Comentarios XML (`<summary>`, `<param>`, `<remarks>`) en lo público que no se explica por su nombre; comentarios en
  línea solo para explicar el porqué.
- Sin `Find` ni `GetComponent` en `Update`: las referencias se cachean en `Awake` o se asignan en el Inspector.
- Comunicación entre módulos por eventos; sin singletons.
- Lógica pura en clases C# normales, independientes de la escena; los `MonoBehaviour` hacen de adaptador.
- Código asíncrono con `Awaitable`, con cancelación y tiempo máximo en las llamadas de red.

```csharp
using UnityEngine;

namespace Guadalupe.Ambience
{
    /// <summary>Sombrilla de una mesa de terraza, que <see cref="TerraceUmbrellas"/> abre o recoge.</summary>
    public sealed class TerraceUmbrella : MonoBehaviour
    {
        private Renderer[] _parts;

        /// <summary>Muestra la sombrilla abierta o la oculta.</summary>
        public void SetOpen(bool open)
        {
            _parts ??= GetComponentsInChildren<Renderer>(true);
            foreach (Renderer part in _parts)
            {
                part.enabled = open;
            }
        }
    }
}
```

## Assets

- Lo propio va en `Assets/_Guadalupe/`; lo de terceros, en `Assets/ThirdParty/` sin modificar (se usan variantes de
  prefab o copias dentro de `_Guadalupe`).
- Nombres sin espacios, en inglés, con prefijo de tipo:

| Prefijo | Tipo | Prefijo | Tipo |
|---|---|---|---|
| `PF_` | Prefab | `SC_` | Escenario |
| `M_` | Material | `SO_` | Otros ScriptableObjects |
| `T_` | Textura | `URP_`, `VP_` | Render y perfiles de volumen |
| `SM_` | Malla o modelo | `CB_` | Mezclas de cámara |
| `TD_`, `TL_` | Datos y capas de terreno | `AC_`, `AOC_`, `AM_` | Controladores, overrides y máscaras de animación |
| `FA_` | Fuente de la interfaz | `AMB_`, `SFX_` | Sonido ambiente y efectos |

- Lo propio de la plaza y las piezas de los personajes principales van en español (`M_Zocalo`, `M_Paqui_Polo`,
  `SM_Rotulo_…`).
- Conservan su nombre de origen los recursos de terceros (ambientCG, Rocketbox, Mixamo, fuentes `.ttf`) y lo que no
  es un asset de arte: datos JSON, shaders, acciones de entrada, UI y NavMesh.
- Texturas de 1K por defecto y 2K como máximo.

## Importación

Las reglas de importación se aplican solas según la carpeta:

- `Art/Models`: modelos tal cual se exportaron y sin animación (`ModelImportRules`).
- `Art/Characters`: personajes con esqueleto, como Humanoid (`CharacterImportRules`). Sus materiales URP se crean con
  **Guadalupe → Personajes → Crear materiales de los turistas (Rocketbox)** y **… del jugador y los
  personajes principales (MakeHuman)**.
- `Art/Animations`: Humanoid, sin desplazamiento de la raíz (los mueve la NavMesh) y en bucle, salvo los gestos y las
  transiciones de sentarse (`AnimationImportRules`).

## Escena

- **Jerarquía** en español y ASCII, sin espacios ni tildes: grupos en PascalCase (`Mobiliario`) e instancias como
  `Tipo_Identificador`, con números de dos cifras (`Farola_02`, `Rotulo_CafeBarCerezo`). Lo generado sigue
  `HierarchyNames`.
- **Capas:** `Environment` (lo generado y el monasterio), `Props` (mobiliario), `Player`, `NPC` e `Interactable`. Lo
  que no tiene colliders (palomas, cámaras, sonido, grupos vacíos) se queda en `Default`.
