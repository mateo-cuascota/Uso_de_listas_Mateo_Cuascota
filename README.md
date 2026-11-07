# Lámparas de Gemas – Uso de Listas y Diccionarios

**Autor:** Mateo Cuascota
**Motor:** Unity 6.3 LTS (6000.3.9f1) · URP · C# · Visual Scripting

Proyecto de práctica para aprender a usar `List<T>` y `Dictionary<K,V>` en Unity.
Se instancian lámparas, se encienden, se recogen sus gemas y se usan para dañarlas o curarlas.

## Cómo abrirlo
1. Clonar el repositorio.
2. En Unity Hub: **Añadir → Añadir proyecto desde el disco** y elegir esta carpeta.
3. Abrir con **Unity 6000.3.9f1** y cargar `Assets/Scenes/SampleScene.unity`.

## Controles
| Acción | Control |
|---|---|
| Crear lámpara (máx. 5) | Botón **Instanciar** |
| Prender / apagar todas en cadena | Botón **Encender / Apagar** |
| Eliminar todas | Botón **Desinstanciar** |
| Prender / apagar una lámpara | Clic izquierdo |
| Recoger gema (lámpara encendida) | Clic derecho |
| Eliminar una lámpara | Clic central o **Supr** |
| Usar gema del inventario | **E** |
| Cambiar gema | **0** Diamante · **1** Rubí · **2** Esmeralda · **3** Zafiro |
| Mostrar / ocultar controles | **H** |
| Cámara / música | **C** / **M** |

## Dónde se usa `List<T>`
- `List<Lampara> lamparas` (`ControlLamparas.cs`): lámparas activas en pantalla.
  `Add()` al instanciar, `Remove()` al eliminar una o cuando su energía llega a 0,
  `Count` para el contador y los botones, `ForEach` para cambiar color o desaparecerlas.
- `List<string> inventario` (`GestorGemas.cs`): gemas recogidas en tiempo real.
- Visual Scripting: la variable de escena `ListaDeLamparas` con el nodo **Add List Item** (botón Instanciar).

## Dónde se usa `Dictionary<K,V>`
- `Dictionary<string, int> danoCuracion` (`GestorGemas.cs`): nombre de la gema → daño o curación
  (Diamante +15, Rubí −25, Esmeralda +30, Zafiro −10). Se lee con `TryGetValue()` al presionar **E**.
- `Dictionary<string, Color> colores`: color de cada gema.
- `Dictionary<string, Action<Lampara>> efectos`: efecto visual de cada gema.
- `Add()` registra cada gema y `ContainsKey()` evita llaves repetidas.

## Scripts principales
| Script | Qué hace |
|---|---|
| `ControlLamparas.cs` | Lista de lámparas, botones Encender / Desinstanciar, eliminar con `Remove()` |
| `GestorGemas.cs` | Diccionarios, inventario, teclas y recuadro de controles |
| `Lampara.cs` | Estado de cada lámpara: luz, partículas, energía, efectos |
| `CamaraOrbita.cs` | Cámara que gira alrededor de las lámparas |
| `Sonidos.cs` | Carga y reproduce los sonidos |
| `Editor/BuildItch.cs` | Menú **Tools → Build para itch.io (.zip)** |
