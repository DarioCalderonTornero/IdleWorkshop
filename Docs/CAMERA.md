# 📋 SCRIPTS — Grupo: Camera

---

## CameraController

**Tipo:** MonoBehaviour  
**GameObject:** `Main Camera` — requiere componente `Camera`  
**Responsabilidad:** Gestiona el movimiento de la cámara 2D mediante drag. Se suscribe a los eventos del `InputManager` — no accede al hardware directamente. Incluye inercia suave al soltar y límites configurables del mundo.

---

### Campos en el Inspector
| Campo | Tipo | Descripción |
|---|---|---|
| `xMin` | `float` | Límite izquierdo del mundo |
| `xMax` | `float` | Límite derecho del mundo |
| `yMin` | `float` | Límite inferior del mundo |
| `yMax` | `float` | Límite superior del mundo |
| `dragSpeed` | `float` | Multiplicador de velocidad del drag |
| `useInertia` | `bool` | Activa o desactiva la inercia al soltar |
| `inertiaFriction` | `float` | Velocidad a la que se frena la inercia — valores más altos = frena antes |

---

### Escucha eventos de
| Script | Evento | Motivo |
|---|---|---|
| `InputManager` | `OnDragStarted` | Inicia el drag y guarda el punto de origen en mundo |
| `InputManager` | `OnDragEnded` | Termina el drag y activa la inercia |
| `InputManager` | `OnPointerPosition` | Actualiza la posición actual del puntero |

---

### Notas técnicas
- Usa `OnEnable` y `OnDisable` para suscribirse y desuscribirse — correcto para componentes que pueden activarse y desactivarse
- El clamp de posición tiene en cuenta el tamaño de la cámara ortográfica para que los bordes del mundo no queden visibles
- Usa Gizmos en el editor para visualizar los límites del mundo al seleccionar la cámara
- `[RequireComponent(typeof(Camera))]` garantiza que siempre hay un componente `Camera` en el mismo GameObject
