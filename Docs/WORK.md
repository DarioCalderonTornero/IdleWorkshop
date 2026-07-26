# 📋 SCRIPTS — Grupo: Work

---

## WorkStation

**Tipo:** MonoBehaviour  
**GameObject:** `WorkStation` — prefab raíz del taller. El taller 0 existe en escena, el resto se instancian dinámicamente via `WorkStationUnlocker`  
**Responsabilidad:** Contenedor principal de un taller. Agrupa `ReceptionDesk`, hasta 5 `WorkDesk` y un `Worker`. Gestiona el estado de ocupación, recibe trabajo del `CustomerManager`, coordina al `Worker` y expone métodos de guardado y carga.

---

### Campos en el Inspector
| Campo | Tipo | Descripción |
|---|---|---|
| `receptionDesk` | `WorkTable` | Mesa de recepción donde el cliente deja el objeto |
| `workDesks` | `List<WorkDeskUnlockable>` | Lista de las 5 mesas de trabajo en orden (1 a 5) |
| `worker` | `Worker` | El worker de este taller |
| `receptionItemPoint` | `Transform` | Punto donde el worker deja el objeto restaurado para el cliente |
| `stationId` | `int` | Identificador único del taller — debe ser único y secuencial (0, 1, 2...) |

---

### API pública
| Método | Descripción |
|---|---|
| `RequestWork(GameObject, ItemDefinition)` | Recibe un trabajo del `CustomerManager` y lo delega al `Worker` |
| `OnWorkCompleted()` | Llamado por el `Worker` al terminar — avisa al `CustomerManager` y libera el taller |
| `GetUnlockedDesks()` | Devuelve la lista de `WorkTable` actualmente desbloqueadas |
| `GetNextLockedDesk()` | Devuelve la siguiente `WorkDeskUnlockable` bloqueada — usada por el `TapHandler` |
| `GetSaveData()` | Recopila el estado completo del taller para guardar |
| `LoadSaveData(WorkStationSaveData)` | Restaura el estado del taller desde datos guardados |

---

### Llama a
| Script | Motivo |
|---|---|
| `Worker` | `Init` y `StartWork` |
| `CustomerManager` | `ServeNextCustomer` al completar el trabajo |
| `WorkStationRegistry` | `Register` en su `Awake` |
| `WorkerUpgradeable` | `LoadLevel` al cargar |
| `WorkDeskUpgradeable` | `LoadLevel` al cargar |
| `WorkDeskUnlockable` | `Unlock` al cargar si la mesa estaba desbloqueada |

---

### Es llamado desde
| Script | Motivo |
|---|---|
| `CustomerManager` | `RequestWork` al asignar trabajo |
| `WorkStationRegistry` | `GetSaveData` y `LoadSaveData` |
| `TapHandler` | `GetNextLockedDesk` al tapear una mesa bloqueada |

---

### Notas técnicas
- El taller 0 tiene `stationId = 0` asignado en el inspector — los demás se asignan en sus prefabs con IDs únicos
- Las mesas desbloqueadas por defecto se registran en `Awake` — las bloqueadas se suscriben al evento `OnUnlocked`
- `_isBusy` evita que el `CustomerManager` asigne dos trabajos simultáneos al mismo taller

---

## Worker

**Tipo:** MonoBehaviour  
**GameObject:** Hijo de `WorkStation` llamado `Worker`  
**Responsabilidad:** Ejecuta el loop de trabajo completo dentro de su taller. Recoge el objeto en recepción, lo lleva por cada mesa desbloqueada en orden, lo procesa, gestiona el tap boost, acumula estrellas por ronda y devuelve el objeto al cliente.

---

### Campos en el Inspector
| Campo | Tipo | Descripción |
|---|---|---|
| `idlePosition` | `Transform` | Posición a la que vuelve el worker cuando está libre |
| `headAnchor` | `Transform` | Punto sobre la cabeza donde se parenta el objeto transportado |
| `baseMoveSpeed` | `float` | Velocidad base de movimiento |
| `progressUI` | `RepairProgressUI` | Referencia al círculo de progreso sobre el worker |

---

### API pública
| Método | Descripción |
|---|---|
| `Init(WorkStation)` | Inicializa el worker con su taller y lo registra en `WorkerRegistry` |
| `StartWork(GameObject, ItemDefinition)` | Inicia el loop de trabajo con el objeto recibido |
| `ApplyMoveSpeedMultiplier(float)` | Aplica un multiplicador a la velocidad base — llamado al mejorar |
| `ApplyTapBoost(float)` | Acumula segundos de boost que se consumen en `ProcessRoutine` |

---

### Llama a
| Script | Motivo |
|---|---|
| `WorkerRegistry` | `Register` en `Init`, `Unregister` en `OnDestroy` |
| `EconomyManager` | `AddCoins` al completar cada mesa |
| `BestiaryManager` | `RegisterItem` al procesar, `RegisterSold` al entregar al cliente |
| `StarPopupSpawner` | `Spawn` al conseguir una estrella |
| `WorkStation` | `OnWorkCompleted` al terminar el loop |
| `RepairProgressUI` | `Show`, `SetFill`, `Hide` durante el procesado |

---

### Es llamado desde
| Script | Motivo |
|---|---|
| `WorkStation` | `Init` y `StartWork` |
| `TapHandler` | `ApplyTapBoost` via `WorkerRegistry` |

---

### Notas técnicas
- El tap boost se acumula en `_tapBoostAccumulated` y se consume al inicio de cada frame de `ProcessRoutine`
- Las estrellas se acumulan por ronda completa (`starsThisRound`) y se registran en `BestiaryManager` al terminar todas las mesas
- Usa Gizmos en el editor para visualizar la posición idle
- `_boostLock` está declarado pero no se usa actualmente — reservado para thread safety si se necesita en el futuro

---

## WorkTable

**Tipo:** MonoBehaviour  
**GameObject:** `ReceptionDesk` y cada `WorkDesk1` a `WorkDesk5`, hijos de `WorkStation`  
**Responsabilidad:** Contenedor de dos posiciones (player slot e item slot) y gestor de los multiplicadores de tiempo y recompensa de cada mesa. También gestiona la probabilidad de estrella según el nivel.

---

### Campos en el Inspector
| Campo | Tipo | Descripción |
|---|---|---|
| `playerSlot` | `Transform` | Punto donde se para el worker mientras trabaja |
| `itemSlot` | `Transform` | Punto donde se deposita el objeto sobre la mesa |
| `baseTimeMultiplier` | `float` | Multiplicador de tiempo base (1 = sin modificar) |
| `baseRewardMultiplier` | `float` | Multiplicador de recompensa base (1 = sin modificar) |

---

### API pública
| Método | Descripción |
|---|---|
| `GetProcessTime(ItemDefinition)` | Devuelve el tiempo real de proceso aplicando el multiplicador actual |
| `GetReward(ItemDefinition)` | Devuelve la recompensa real en monedas aplicando el multiplicador actual |
| `ApplyMultipliers(float, float)` | Aplica nuevos multiplicadores de tiempo y recompensa |
| `ApplyUpgradeData(UpgradeData, int)` | Actualiza la probabilidad de estrella según el nivel — llamado al mejorar |
| `RollStar()` | Devuelve `true` si se consigue estrella en esta mesa según la probabilidad actual |

---

### Es llamado desde
| Script | Motivo |
|---|---|
| `Worker` | `GetProcessTime`, `GetReward`, `RollStar`, `PlayerSlotPos`, `ItemSlotPos` |
| `WorkDeskUpgradeable` | `ApplyMultipliers` y `ApplyUpgradeData` al mejorar |

---

### Notas técnicas
- Usa Gizmos en el editor para visualizar `playerSlot` (magenta) e `itemSlot` (rojo)
- La probabilidad de estrella empieza en 0 hasta que se llama a `ApplyUpgradeData` por primera vez
- `GetProcessTime` tiene un mínimo de 0.1s para evitar tiempos de proceso nulos

---

## WorkDeskUnlockable

**Tipo:** MonoBehaviour  
**GameObject:** Cada `WorkDesk1` a `WorkDesk5`, hijo de `WorkStation` — en el mismo GameObject que `WorkTable` y `WorkDeskUpgradeable`  
**Responsabilidad:** Gestiona el estado bloqueado/desbloqueado de cada mesa de trabajo. Controla el visual de bloqueado, comprueba requisitos de nivel previos al desbloqueo y notifica al resto del sistema cuando se desbloquea.

---

### Campos en el Inspector
| Campo | Tipo | Descripción |
|---|---|---|
| `unlockCost` | `double` | Coste en monedas para desbloquear esta mesa |
| `levelRequirements` | `LevelRequirement[]` | Requisitos de nivel de otras mesas para poder desbloquear esta (opcional) |
| `spriteRenderer` | `SpriteRenderer` | Renderer del sprite de la mesa para aplicar color de bloqueado |
| `lockedColor` | `Color` | Color que se aplica al sprite cuando la mesa está bloqueada |
| `unlockedByDefault` | `bool` | Si está marcado, la mesa empieza desbloqueada (usar en `WorkDesk1`) |

---

### API pública
| Método | Descripción |
|---|---|
| `Unlock()` | Desbloquea la mesa, aplica visual y dispara `OnUnlocked` |
| `MeetsRequirements()` | Devuelve `true` si se cumplen todos los requisitos de nivel |
| `GetMissingRequirementsText()` | Devuelve un texto con los requisitos pendientes para mostrar en la UI |

---

### Eventos que expone
| Evento | Tipo | Descripción |
|---|---|---|
| `OnUnlocked` | `Action<WorkDeskUnlockable>` | Se dispara cuando la mesa se desbloquea |

---

### Es llamado desde
| Script | Motivo |
|---|---|
| `WorkStation` | Se suscribe a `OnUnlocked` para registrar la mesa en `_unlockedDesks` |
| `TapHandler` | Comprueba `IsUnlocked` y llama a `UpgradePanelUI` según el estado |
| `UpgradePanelUI` | `Unlock`, `MeetsRequirements`, `GetMissingRequirementsText`, `UnlockCost` |
| `WorkStation` | `Unlock` al cargar si la mesa estaba desbloqueada | 
---

## WorkDeskUpgradeable

**Tipo:** MonoBehaviour  
**GameObject:** Cada `WorkDesk1` a `WorkDesk5`, hijo de `WorkStation` — en el mismo GameObject que `WorkTable` y `WorkDeskUnlockable`  
**Responsabilidad:** Hereda de `UpgradeableBase`. Al subir de nivel aplica nuevos multiplicadores de tiempo y recompensa a su `WorkTable` y actualiza la probabilidad de estrella.

---

### Campos en el Inspector
| Campo | Tipo | Descripción |
|---|---|---|
| `upgradeData` | `UpgradeData` (SO) | Datos de mejora — heredado de `UpgradeableBase`. Define coste, factor de crecimiento, nivel máximo, reducción de tiempo y aumento de recompensa |

---

### Llama a
| Script | Motivo |
|---|---|
| `WorkTable` | `ApplyMultipliers` y `ApplyUpgradeData` al subir de nivel |

---

### Es llamado desde
| Script | Motivo |
|---|---|
| `UpgradeableBase` | `OnUpgraded` al completar una mejora |
| `WorkStation` | `LoadLevel` al cargar la partida |
| `TapHandler` | Vía `IUpgradeable` para abrir el panel de mejora |

---

### Fórmulas aplicadas al subir de nivel
- **Tiempo:** `1 - (nivel - 1) × timeReductionPerLevel` (mínimo 0.1)
- **Recompensa:** `1 + (nivel - 1) × rewardIncreasePerLevel`
- **Estrella:** calculada por `UpgradeData.GetStarChanceForLevel(nivel)`

---

## WorkerUpgradeable

**Tipo:** MonoBehaviour  
**GameObject:** `Worker`, hijo de `WorkStation` — en el mismo GameObject que el componente `Worker`  
**Responsabilidad:** Hereda de `UpgradeableBase`. Al subir de nivel aumenta la velocidad de movimiento del `Worker`.

---

### Campos en el Inspector
| Campo | Tipo | Descripción |
|---|---|---|
| `upgradeData` | `UpgradeData` (SO) | Datos de mejora — heredado de `UpgradeableBase` |
| `moveSpeedMultiplierPerLevel` | `float` | Incremento de velocidad por nivel (ej: 0.1 = +10% por nivel) |

---

### Llama a
| Script | Motivo |
|---|---|
| `Worker` | `ApplyMoveSpeedMultiplier` al subir de nivel |

---

### Es llamado desde
| Script | Motivo |
|---|---|
| `UpgradeableBase` | `OnUpgraded` al completar una mejora |
| `WorkStation` | `LoadLevel` al cargar la partida |
| `TapHandler` | Vía `IUpgradeable` para abrir el panel de mejora |

---

### Fórmula aplicada al subir de nivel
- **Velocidad:** `1 + nivel × moveSpeedMultiplierPerLevel`

---

## ReceptionDeskUpgradeable

**Tipo:** MonoBehaviour  
**GameObject:** `ReceptionDesk`, hijo de `WorkStation` — en el mismo GameObject que `WorkTable`  
**Responsabilidad:** Hereda de `UpgradeableBase`. Al subir de nivel reduce el tiempo de espera de la mesa de recepción. El efecto real está pendiente de implementar — actualmente solo loguea el valor calculado.

---

### Campos en el Inspector
| Campo | Tipo | Descripción |
|---|---|---|
| `upgradeData` | `UpgradeData` (SO) | Datos de mejora — heredado de `UpgradeableBase` |
| `waitReductionPerLevel` | `float` | Reducción de tiempo de espera por nivel en segundos |

---

### Es llamado desde
| Script | Motivo |
|---|---|
| `UpgradeableBase` | `OnUpgraded` al completar una mejora |
| `TapHandler` | Vía `IUpgradeable` para abrir el panel de mejora |

---

### Notas técnicas
- `CurrentWaitReduction` expone la reducción total actual — listo para conectar cuando se implemente el efecto real
- Pendiente de conectar con `CustomerManager` o `WorkTable` para aplicar el efecto

---

## WorkStationUnlocker

**Tipo:** Manager (Singleton)  
**GameObject:** `---MANAGERS---` → hijo vacío llamado `WorkStationUnlocker`  
**Responsabilidad:** Gestiona el desbloqueo e instanciación de nuevos talleres. Mantiene el orden de desbloqueo, gasta las monedas necesarias y registra el nuevo taller en el sistema. También restaura talleres guardados al cargar la partida sin gastar monedas.

---

### Campos en el Inspector
| Campo | Tipo | Descripción |
|---|---|---|
| `workStationsData` | `List<WorkStationData>` (SO) | Lista ordenada de datos de cada taller desbloqueable |
| `workStationSpawnPoints` | `List<Transform>` | Posiciones en escena donde se instancia cada taller — deben coincidir en índice con `workStationsData` |

---

### Eventos que expone
| Evento | Tipo | Descripción |
|---|---|---|
| `OnWorkStationUnlocked` | `Action` | Se dispara cuando el jugador desbloquea un taller nuevo |

---

### API pública
| Método | Descripción |
|---|---|
| `UnlockNextWorkStation()` | Desbloquea e instancia el siguiente taller si el jugador puede permitírselo |
| `RestoreWorkStation(WorkStationSaveData)` | Instancia un taller al cargar la partida sin gastar monedas ni disparar eventos |

---

### Llama a
| Script | Motivo |
|---|---|
| `EconomyManager` | `CanAfford` y `SpendCoins` al desbloquear |
| `CustomerManager` | `RegisterWorkStation` al instanciar un taller nuevo |
| `WorkStation` | `LoadSaveData` al restaurar desde partida guardada |

---

### Es llamado desde
| Script | Motivo |
|---|---|
| `WorkStationRegistry` | `RestoreWorkStation` al cargar talleres guardados que no existen en escena |

---

### ScriptableObject asociado: `WorkStationData`
Cada taller desbloqueable tiene un SO de tipo `WorkStationData` con estos campos:

| Campo | Tipo | Descripción |
|---|---|---|
| `prefab` | `GameObject` | Prefab del taller a instanciar |
| `cost` | `double` | Coste en monedas para desbloquear |
| `UI` | `Sprite` | Icono del taller para la UI |
| `stationName` | `string` | Nombre del taller |

---

### Notas técnicas
- `nextIndex` se actualiza tanto al desbloquear como al restaurar — evita que se repitan talleres
- El taller 0 nunca pasa por `WorkStationUnlocker` — existe en escena desde el inicio
- Los `workStationSpawnPoints` deben estar en la escena como `Transform` vacíos en las posiciones correctas
