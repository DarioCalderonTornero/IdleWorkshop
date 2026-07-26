# 📋 SCRIPTS — Grupo: Managers

---

## InputManager

**Tipo:** Manager (Singleton)  
**GameObject:** `---MANAGERS---` → hijo vacío llamado `InputManager`  
**Responsabilidad:** Centraliza todo el input del juego usando el New Input System. 
Ningún otro script toca el hardware directamente — todos se suscriben a los eventos de este Manager.

---

### Campos en el Inspector
Ninguno. No requiere asignación manual.

---

### Eventos que expone
| Evento | Tipo | Descripción |
|---|---|---|
| `OnDragStarted` | `Action` | Se dispara al iniciar un arrastre |
| `OnDragEnded` | `Action` | Se dispara al soltar el arrastre |
| `OnPointerPosition` | `Action<Vector2>` | Posición continua del puntero en pantalla |
| `OnTap` | `Action<Vector2>` | Tap rápido con la posición donde se tocó |

---

### Es escuchado desde
| Script | Evento que escucha |
|---|---|
| `CameraController` | `OnDragStarted`, `OnDragEnded`, `OnPointerPosition` |
| `TapHandler` | `OnTap` |

---

### Notas técnicas
- Usa el asset `IdleInputActions` generado por el New Input System
- El Input Actions asset tiene tres acciones: `Drag`, `Tap` y `PointerPosition`
- `Tap` está configurado como `Button` con binding `Primary Touch/Tap [Touchscreen]` y `Left Button [Mouse]`
- `Drag` distingue arrastre de tap — no dispara `OnTap`
- `currentPos` se actualiza continuamente con `PointerPosition` y se pasa al evento `OnTap`
