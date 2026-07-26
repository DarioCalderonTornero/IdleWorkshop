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

---

## EconomyManager

**Tipo:** Manager (Singleton)  
**GameObject:** `---MANAGERS---` → hijo vacío llamado `EconomyManager`  
**Responsabilidad:** Único sitio donde existe el saldo del jugador en `double`. Ningún otro sistema guarda monedas directamente. Notifica a la UI y otros sistemas cuando el saldo cambia.

---

### Campos en el Inspector
| Campo | Tipo | Descripción |
|---|---|---|
| `startingCoins` | `double` | Monedas con las que empieza una partida nueva |

---

### Eventos que expone
| Evento | Tipo | Descripción |
|---|---|---|
| `OnCoinsChanged` | `Action<double>` | Se dispara cada vez que el saldo cambia |

---

### API pública
| Método | Descripción |
|---|---|
| `AddCoins(double)` | Añade monedas y dispara `OnCoinsChanged` |
| `SpendCoins(double)` | Resta monedas si hay saldo suficiente. Devuelve `bool` |
| `CanAfford(double)` | Devuelve `true` si el jugador puede permitirse el gasto |
| `GetCurrentCoins()` | Devuelve el saldo actual |
| `LoadCoins(double)` | Carga el saldo desde el sistema de Save/Load |

---

### Es llamado desde
| Script | Motivo |
|---|---|
| `Worker` | `AddCoins` al completar una mesa |
| `WorkDeskUnlockable` | `SpendCoins` y `CanAfford` al desbloquear una mesa |
| `UpgradeableBase` | `SpendCoins` y `CanAfford` al mejorar |
| `UpgradePanelUI` | `CanAfford` para activar/desactivar botones |
| `SaveManager` | `GetCurrentCoins` al guardar, `LoadCoins` al cargar |

---

### Escucha eventos de
Ninguno.

---

### Notas técnicas
- El saldo es `double` para soportar números de escala idle (billones y más)
- Contiene código de testeo en `Update` (teclas Q/W para cambiar `timeScale`) — pendiente de borrar antes de build
- `LoadCoins` no usa `AddCoins` para evitar lógica innecesaria, carga directamente y notifica a la UI

---

## CustomerManager

**Tipo:** Manager (Singleton)  
**GameObject:** `---MANAGERS---` → hijo vacío llamado `CustomerManager`  
**Responsabilidad:** Gestiona el spawn de clientes, la cola de espera y el reparto de trabajo a las `WorkStation` libres. No conoce a ningún `Worker` directamente.

---

### Campos en el Inspector
| Campo | Tipo | Descripción |
|---|---|---|
| `pathWaypoints` | `Transform[]` | Puntos del camino que siguen los clientes hasta la cola |
| `deskSlot` | `Transform` | Punto donde se para el primer cliente de la cola |
| `itemDropTarget` | `Transform` | Punto encima de la mesa donde cae el objeto |
| `slotSpacingY` | `float` | Separación vertical entre clientes en la cola (negativo = hacia abajo) |
| `customerPrefab` | `GameObject` | Prefab del cliente a instanciar |
| `itemDatabase` | `ItemDatabase` (SO) | Base de datos de objetos restaurables |
| `spawnInterval` | `float` | Segundos entre spawns de clientes |
| `maxCustomers` | `int` | Máximo de clientes en cola simultáneamente |
| `workStations` | `List<WorkStation>` | Lista de talleres disponibles (se puede pre-asignar o registrar en runtime) |

---

### API pública
| Método | Descripción |
|---|---|
| `RegisterWorkStation(WorkStation)` | Añade un taller al sistema en runtime |
| `UnregisterWorkStation(WorkStation)` | Elimina un taller del sistema |
| `ServeNextCustomer(WorkStation)` | Avisa al primer cliente de la cola que puede recoger su objeto |
| `OnItemPlacedOnDesk(ItemDefinition, GameObject)` | Llamado por `Customer` cuando deja el objeto en la mesa |
| `OnCustomerLeaving(Customer)` | Llamado por `Customer` cuando recoge su objeto y sale |
| `OnCustomerDone(Customer)` | Llamado por `Customer` cuando cruza el borde — destruye el GameObject |
| `GetSlotPosition(int)` | Devuelve la posición en mundo del slot de cola según índice |

---

### Es llamado desde
| Script | Motivo |
|---|---|
| `Customer` | Callbacks de ciclo de vida del cliente |
| `WorkStation` | `ServeNextCustomer` al completar el trabajo |
| `WorkStationUnlocker` | `RegisterWorkStation` al desbloquear un taller nuevo |

---

### Notas técnicas
- Usa Gizmos en el editor para visualizar waypoints, slots de cola y punto de drop del objeto
- El spawn ocurre en un loop de corrutina — no en `Update`
- La cola es una `List<Customer>` ordenada; el índice 0 es siempre el primero

---

## AudioManager

**Tipo:** Manager (Singleton)  
**GameObject:** `---MANAGERS---` → hijo vacío llamado `AudioManager` con dos componentes `AudioSource`  
**Responsabilidad:** Centraliza la reproducción de música y efectos de sonido. Separa música (loop) y SFX (one shot) en dos `AudioSource` independientes con volúmenes controlables por separado.

---

### Campos en el Inspector
| Campo | Tipo | Descripción |
|---|---|---|
| `musicAudioSource` | `AudioSource` | Componente de audio para la música — debe tener `Loop` activado |
| `SFXAudioSource` | `AudioSource` | Componente de audio para efectos de sonido |
| `music1Clip` | `AudioClip` | Clip de música principal — se reproduce automáticamente al arrancar |

---

### API pública
| Método | Descripción |
|---|---|
| `PlayMusic(AudioClip, float)` | Reproduce un clip en loop con el volumen indicado |
| `StopMusic()` | Para la música |
| `PlaySFX(AudioClip, float)` | Reproduce un efecto de sonido puntual con `PlayOneShot` |

---

### Es llamado desde
Actualmente ningún script llama a `PlaySFX` — pendiente de integrar cuando se añadan efectos de sonido.

---

### Notas técnicas
- La música arranca automáticamente en `Start` con `music1Clip`
- `PlaySFX` usa `PlayOneShot` para que los SFX no corten entre sí
- **Bug conocido:** `PlaySFX` usa `musicAudioSource` en vez de `SFXAudioSource` — hay que corregir esta línea: `musicAudioSource.PlayOneShot` → `SFXAudioSource.PlayOneShot`
