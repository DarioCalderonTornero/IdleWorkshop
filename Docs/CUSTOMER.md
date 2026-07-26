# 📋 SCRIPTS — Grupo: Customer

---

## Customer

**Tipo:** MonoBehaviour  
**GameObject:** Instanciado dinámicamente desde `CustomerManager` usando el prefab de cliente  
**Responsabilidad:** Gestiona el comportamiento individual de cada cliente mediante una máquina de estados. Sigue el camino hasta la cola, deja el objeto en la mesa de recepción, espera a ser atendido, recoge el objeto restaurado y sale del mapa.

---

### Campos en el Inspector
| Campo | Tipo | Descripción |
|---|---|---|
| `moveSpeed` | `float` | Velocidad de movimiento del cliente |
| `headAnchor` | `Transform` | Punto sobre la cabeza donde se parenta el objeto transportado |
| `dropDuration` | `float` | Duración de la animación del objeto cayendo a la mesa |
| `pickupDuration` | `float` | Duración de la animación del objeto subiendo de vuelta a la cabeza |
| `queueAdvanceDelay` | `float` | Segundos de espera tras recoger el objeto antes de que avance la cola |
| `exitX` | `float` | Coordenada X a la que se dirige el cliente al salir del mapa |

---

### Estados
| Estado | Descripción |
|---|---|
| `FollowingPath` | Sigue los waypoints hasta llegar al final del camino |
| `MovingToSlot` | Se mueve hacia su posición en la cola |
| `Dropping` | Anima el objeto cayendo a la mesa de recepción |
| `Waiting` | Espera a ser atendido o a avanzar en la cola |
| `PickingUp` | Anima el objeto subiendo de vuelta a la cabeza |
| `Leaving` | Se mueve hacia el borde del mapa para salir |

---

### API pública
| Método | Descripción |
|---|---|
| `Init(CustomerManager, Transform[], Vector3, int, ItemDefinition, Vector3)` | Inicializa el cliente con todos los datos necesarios para su ciclo de vida |
| `MoveToSlot(int, Vector3)` | Actualiza la posición en la cola cuando avanza un hueco |
| `BeServed()` | Llamado por `CustomerManager` cuando el trabajo está listo — inicia `PickUpItemRoutine` |

---

### Llama a
| Script | Motivo |
|---|---|
| `CustomerManager` | `OnItemPlacedOnDesk` al dejar el objeto, `OnCustomerLeaving` al recogerlo, `OnCustomerDone` al salir |

---

### Es llamado desde
| Script | Motivo |
|---|---|
| `CustomerManager` | `Init` al instanciar, `MoveToSlot` al avanzar la cola, `BeServed` al terminar el trabajo |

---

### Notas técnicas
- El objeto se instancia sobre la cabeza en `Init` y se destruye en `OnDestroy` si aún existe
- La cola avanza con `queueAdvanceDelay` después de recoger el objeto — sin esperar a que el cliente salga del mapa
- El movimiento usa `MoveTowards` — sin físicas ni NavMesh

---

## CustomerManager

**Tipo:** Manager (Singleton)  
**GameObject:** `---MANAGERS---` → hijo vacío llamado `CustomerManager`  
**Responsabilidad:** Gestiona el spawn de clientes, la cola de espera ordenada y el reparto de trabajo a las `WorkStation` libres. No conoce a ningún `Worker` directamente — solo habla con `WorkStation`.

---

### Campos en el Inspector
| Campo | Tipo | Descripción |
|---|---|---|
| `pathWaypoints` | `Transform[]` | Puntos del camino que siguen los clientes hasta la cola — deben estar en orden |
| `deskSlot` | `Transform` | Punto donde se para el primer cliente de la cola |
| `itemDropTarget` | `Transform` | Punto encima de la mesa de recepción donde cae el objeto |
| `slotSpacingY` | `float` | Separación vertical entre clientes en la cola (negativo = hacia abajo) |
| `customerPrefab` | `GameObject` | Prefab del cliente a instanciar |
| `itemDatabase` | `ItemDatabase` (SO) | Base de datos de objetos restaurables |
| `spawnInterval` | `float` | Segundos entre spawns de clientes |
| `maxCustomers` | `int` | Máximo de clientes en cola simultáneamente |
| `workStations` | `List<WorkStation>` | Lista de talleres disponibles — se puede pre-asignar o registrar en runtime |

---

### API pública
| Método | Descripción |
|---|---|
| `RegisterWorkStation(WorkStation)` | Añade un taller al sistema en runtime |
| `UnregisterWorkStation(WorkStation)` | Elimina un taller del sistema |
| `ServeNextCustomer(WorkStation)` | Avisa al primer cliente de la cola que puede recoger su objeto |
| `OnItemPlacedOnDesk(ItemDefinition, GameObject)` | Llamado por `Customer` cuando deja el objeto — busca taller libre y asigna trabajo |
| `OnCustomerLeaving(Customer)` | Llamado por `Customer` cuando recoge su objeto — avanza la cola |
| `OnCustomerDone(Customer)` | Llamado por `Customer` cuando cruza el borde — destruye el GameObject |
| `GetSlotPosition(int)` | Devuelve la posición en mundo del slot de cola según índice |

---

### Llama a
| Script | Motivo |
|---|---|
| `Customer` | `Init` al instanciar, `MoveToSlot` al avanzar cola, `BeServed` al terminar trabajo |
| `WorkStation` | `RequestWork` al asignar trabajo |

---

### Es llamado desde
| Script | Motivo |
|---|---|
| `Customer` | Callbacks de ciclo de vida |
| `WorkStation` | `ServeNextCustomer` al completar el trabajo |
| `WorkStationUnlocker` | `RegisterWorkStation` al desbloquear un taller nuevo |

---

### Notas técnicas
- El spawn ocurre en un loop de corrutina — no en `Update`
- La cola es una `List<Customer>` ordenada — el índice 0 es siempre el primero
- Usa Gizmos en el editor para visualizar waypoints (amarillo), slots de cola (cyan) y punto de drop (verde)
