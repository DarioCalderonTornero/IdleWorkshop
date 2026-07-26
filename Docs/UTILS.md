# 📋 SCRIPTS — Grupo: Utils

---

## CurrencyFormatter

**Tipo:** Clase estática pura (no MonoBehaviour)  
**GameObject:** Ninguno — no va en escena  
**Responsabilidad:** Formatea números grandes en formato legible para la UI. Convierte valores `double` a strings con sufijos K, M, B, T y notación científica para números muy grandes.

---

### API pública
| Método | Descripción |
|---|---|
| `Format(double)` | Devuelve el número formateado como string |

---

### Reglas de formato
| Rango | Decimales | Ejemplo |
|---|---|---|
| `< 1.000` | Sin decimales | `850` |
| `1.000 - 9.999` | Hasta 2 decimales | `1.25K` |
| `10.000 - 99.999` | Hasta 1 decimal | `10.3K` |
| `100.000 - 999.999` | Sin decimales | `125K` |
| Las mismas reglas aplican para M, B y T | | |
| `> 999T` | Notación científica | `1.5e+15` |

---

### Es usado desde
| Script | Motivo |
|---|---|
| `CoinUI` | Formatear el saldo del jugador |
| `UpgradePanelUI` | Formatear el coste de mejora y desbloqueo |

---

### Notas técnicas
- Clase estática pura — no necesita instanciarse ni ponerse en escena
- Los valores negativos devuelven `"0"`
- Se puede ampliar fácilmente añadiendo más sufijos (aa, ab...) para escalas mayores

---

## EconomyTester

**Tipo:** MonoBehaviour  
**GameObject:** Cualquier GameObject en escena durante el desarrollo — pendiente de borrar antes de build  
**Responsabilidad:** Script de testeo para probar el sistema de economía y guardado desde el teclado durante el desarrollo. No debe estar en el build final.

---

### Campos en el Inspector
Ninguno.

---

### Teclas
| Tecla | Acción |
|---|---|
| `A` | Añade 1.143 monedas |
| `S` | Gasta 10.534 monedas |
| `M` | Borra el archivo de guardado via `SaveManager.DeleteSave()` |

---

### Notas técnicas
- **Pendiente de borrar antes del build final**
- Usa el New Input System directamente — aceptable en scripts de testeo

---

## TapHandler

**Tipo:** Manager (Singleton)  
**GameObject:** `---MANAGERS---` → hijo vacío llamado `TapHandler`  
**Responsabilidad:** Centraliza toda la lógica de tap en la escena. Se suscribe a `InputManager.OnTap`, convierte la posición de pantalla a mundo, hace un raycast y decide qué hacer según el componente que encuentre en el objeto tocado. También aplica tap boost a todos los workers en cada tap.

---

### Campos en el Inspector
| Campo | Tipo | Descripción |
|---|---|---|
| `mainCamera` | `Camera` | Cámara principal — si no se asigna coge `Camera.main` automáticamente |
| `tapLayers` | `LayerMask` | Layers que el raycast puede detectar — asignar el layer `Tappable` donde estén Worker y WorkDesks |

---

### Lógica de tap en orden de prioridad
1. Si `BestiaryUI.IsOpen` → ignorar tap
2. Si `UpgradePanelUI.Instance.IsVisible` → ignorar tap
3. Aplicar tap boost a todos los workers via `WorkerRegistry`
4. Hacer raycast — si no golpea nada → ignorar
5. Si golpea `WorkerUpgradeable` → abrir panel de mejora del worker
6. Si golpea `WorkDeskUnlockable` desbloqueada → abrir panel de mejora de mesa
7. Si golpea `WorkDeskUnlockable` bloqueada y es la siguiente en la cola → abrir panel de desbloqueo

---

### Escucha eventos de
| Script | Evento | Motivo |
|---|---|---|
| `InputManager` | `OnTap` | Recibe la posición del tap para procesar |

---

### Llama a
| Script | Motivo |
|---|---|
| `WorkerRegistry` | `ApplyTapBoostToAll` en cada tap |
| `UpgradePanelUI` | `Show` o `ShowUnlock` según el objeto tocado |
| `WorkStation` | `GetNextLockedDesk` para validar si la mesa bloqueada es la siguiente |

---

### Notas técnicas
- El tap boost se aplica en todo tap válido — no solo al tocar un worker
- `tapLayers` debe configurarse en el inspector con el layer donde estén los colliders de Worker y WorkDesks
- Se desuscribe del evento `OnTap` en `OnDestroy` para evitar memory leaks
