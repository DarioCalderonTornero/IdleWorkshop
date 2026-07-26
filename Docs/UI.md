# 📋 SCRIPTS — Grupo: UI

---

## UpgradePanelUI

**Tipo:** MonoBehaviour (Singleton)  
**GameObject:** `Canvas` → hijo llamado `UpgradePanel`  
**Responsabilidad:** Panel que se desliza desde abajo al tapear una mesa o worker. Tiene dos modos: modo mejora (muestra nivel, coste y botón de mejorar) y modo desbloqueo (muestra coste y botón de desbloquear). Se cierra al tapear fuera del panel. Se actualiza automáticamente cuando cambia el saldo.

---

### Campos en el Inspector
| Campo | Tipo | Descripción |
|---|---|---|
| `panelRect` | `RectTransform` | RectTransform del panel — usado para la animación de entrada/salida |
| `elementImage` | `Image` | Icono del elemento mejorable |
| `elementNameText` | `TextMeshProUGUI` | Nombre del elemento |
| `levelText` | `TextMeshProUGUI` | Nivel actual |
| `descriptionText` | `TextMeshProUGUI` | Descripción del elemento |
| `upgradeCostText` | `TextMeshProUGUI` | Coste de la siguiente mejora formateado |
| `upgradeButton` | `Button` | Botón de mejorar |
| `closeButton` | `Button` | Botón de cerrar el panel |
| `animDuration` | `float` | Duración de la animación de entrada/salida en segundos |
| `hiddenY` | `float` | Posición Y anclada cuando el panel está oculto |
| `shownY` | `float` | Posición Y anclada cuando el panel está visible |
| `upgradeContent` | `GameObject` | Contenedor del contenido de mejora normal — se oculta en modo desbloqueo |
| `unlockButton` | `Button` | Botón de desbloquear mesa — se muestra en modo desbloqueo |
| `unlockCostText` | `TextMeshProUGUI` | Texto del coste de desbloqueo o requisitos pendientes |

---

### Estados del panel
| Estado | Descripción |
|---|---|
| `Hidden` | Panel oculto — posición `hiddenY` |
| `Showing` | Animando hacia arriba |
| `Visible` | Panel completamente visible |
| `Hiding` | Animando hacia abajo |

---

### API pública
| Método | Descripción |
|---|---|
| `Show(IUpgradeable)` | Abre el panel en modo mejora con el elemento indicado |
| `ShowUnlock(WorkDeskUnlockable)` | Abre el panel en modo desbloqueo con la mesa indicada |
| `Hide()` | Cierra el panel con animación |

---

### Propiedades públicas
| Propiedad | Descripción |
|---|---|
| `IsVisible` | `true` si el panel está en estado `Visible`, `Showing` o `Hiding` — usado por `TapHandler` para evitar abrir el panel mientras se está cerrando |

---

### Escucha eventos de
| Script | Evento | Motivo |
|---|---|---|
| `EconomyManager` | `OnCoinsChanged` | Actualiza el botón de mejora/desbloqueo según el saldo actual |

---

### Es llamado desde
| Script | Motivo |
|---|---|
| `TapHandler` | `Show` y `ShowUnlock` al detectar tap en worker o mesa |

---

### Notas técnicas
- El panel detecta taps fuera de sí mismo en `Update` usando `RectTransformUtility.RectangleContainsScreenPoint` y se cierra automáticamente
- `IsVisible` incluye el estado `Hiding` para evitar que el `TapHandler` reabra el panel mientras se cierra
- Los costes se formatean con `CurrencyFormatter`
- En modo desbloqueo muestra los requisitos de nivel pendientes si no se cumplen

---

## CoinUI

**Tipo:** MonoBehaviour  
**GameObject:** `Canvas` → hijo llamado `GlobalCoins`  
**Responsabilidad:** Muestra el saldo actual del jugador en pantalla. Se suscribe al evento `OnCoinsChanged` del `EconomyManager` y actualiza el texto usando `CurrencyFormatter`.

---

### Campos en el Inspector
| Campo | Tipo | Descripción |
|---|---|---|
| `coinText` | `TextMeshProUGUI` | Texto donde se muestra el saldo |

---

### Escucha eventos de
| Script | Evento | Motivo |
|---|---|---|
| `EconomyManager` | `OnCoinsChanged` | Actualiza el texto con el nuevo saldo formateado |

---

### Notas técnicas
- Usa `CurrencyFormatter.Format` para mostrar los números en formato legible (1.25K, 3.5M...)
- Se desuscribe del evento en `OnDestroy` para evitar memory leaks

---

## RepairProgressUI

**Tipo:** MonoBehaviour  
**GameObject:** Hijo del `Worker` llamado `ProgressUI` — dentro de un Canvas en World Space  
**Responsabilidad:** Muestra un círculo de progreso sobre el `Worker` mientras procesa un objeto en una mesa. Se actualiza cada frame durante el procesado y se oculta al terminar.

---

### Campos en el Inspector
| Campo | Tipo | Descripción |
|---|---|---|
| `fillImage` | `Image` | Image con `Fill Method = Radial 360` que representa el progreso |
| `root` | `GameObject` | GameObject raíz del Canvas — se activa/desactiva para mostrar u ocultar |
| `offset` | `Vector3` | Offset respecto a la posición del worker (por defecto 1.2 unidades hacia arriba) |

---

### Setup requerido en el Canvas
