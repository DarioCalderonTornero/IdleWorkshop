# 📋 SCRIPTS — Grupo: Upgrades

---

## IUpgradeable

**Tipo:** Interface  
**GameObject:** Ninguno — es un contrato que implementan otros scripts  
**Responsabilidad:** Define el contrato que debe cumplir cualquier elemento mejorable del juego. Permite que `UpgradePanelUI` y `TapHandler` trabajen con cualquier elemento mejorable sin conocer su tipo concreto.

---

### Miembros que define
| Miembro | Tipo | Descripción |
|---|---|---|
| `UpgradeData` | Propiedad | Referencia al SO con los datos de mejora |
| `CurrentLevel` | Propiedad | Nivel actual del elemento |
| `CanUpgrade()` | Método | Devuelve `true` si se puede mejorar |
| `Upgrade()` | Método | Ejecuta la mejora |

---

### Implementado por
| Script | Descripción |
|---|---|
| `UpgradeableBase` | Implementación base — el resto heredan de ella |

---

### Es usado desde
| Script | Motivo |
|---|---|
| `UpgradePanelUI` | Trabaja con `IUpgradeable` para mostrar y ejecutar mejoras sin conocer el tipo concreto |
| `TapHandler` | Busca `IUpgradeable` en el objeto tocado para abrir el panel |

---

## UpgradeableBase

**Tipo:** MonoBehaviour abstracto  
**GameObject:** Ninguno directamente — es la clase base de la que heredan `WorkDeskUpgradeable`, `WorkerUpgradeable` y `ReceptionDeskUpgradeable`  
**Responsabilidad:** Implementación base de `IUpgradeable`. Gestiona el nivel actual, calcula el coste usando la curva matemática del `UpgradeData`, gasta las monedas y llama a `OnUpgraded` para que cada subclase aplique su efecto concreto.

---

### Campos en el Inspector
| Campo | Tipo | Descripción |
|---|---|---|
| `upgradeData` | `UpgradeData` (SO) | Datos de mejora — coste, factor de crecimiento, nivel máximo y efectos |

---

### API pública
| Método | Descripción |
|---|---|
| `CanUpgrade()` | Comprueba si el jugador puede permitirse la mejora y no se ha alcanzado el nivel máximo |
| `Upgrade()` | Gasta monedas, sube el nivel y llama a `OnUpgraded` |
| `LoadLevel(int)` | Restaura el nivel desde la partida guardada y aplica los efectos sin gastar monedas |

---

### Método abstracto que deben implementar las subclases
| Método | Descripción |
|---|---|
| `OnUpgraded(int)` | Llamado al subir de nivel — cada subclase aplica su efecto concreto |

---

### Llama a
| Script | Motivo |
|---|---|
| `EconomyManager` | `CanAfford` y `SpendCoins` al mejorar |

---

### Es heredado por
| Script | Efecto que aplica en `OnUpgraded` |
|---|---|
| `WorkDeskUpgradeable` | Reduce tiempo de proceso y aumenta recompensa en `WorkTable` |
| `WorkerUpgradeable` | Aumenta velocidad de movimiento del `Worker` |
| `ReceptionDeskUpgradeable` | Reduce tiempo de espera (pendiente de conectar) |

---

### Notas técnicas
- `currentLevel` empieza en 1 — el nivel 1 es el estado inicial, no el primer upgrade
- `GetCostForLevel` usa `level - 1` como exponente — el nivel 1 cuesta exactamente `baseCost`
- `LoadLevel` llama a `OnUpgraded` para que los efectos se apliquen correctamente al cargar

---

## UpgradeData

**Tipo:** ScriptableObject  
**GameObject:** Ninguno — vive en `Assets/ScriptableObjects/Upgrades`  
**Responsabilidad:** Define todos los parámetros de mejora de un elemento mejorable. Es el SO que se asigna en el inspector de `UpgradeableBase` y sus subclases.

---

### Campos configurables en el Inspector
| Campo | Tipo | Descripción |
|---|---|---|
| `elementName` | `string` | Nombre del elemento que se muestra en el panel de mejora |
| `description` | `string` | Descripción que se muestra en el panel de mejora |
| `elementImage` | `Sprite` | Icono que se muestra en el panel de mejora |
| `baseCost` | `double` | Coste base del nivel 1 |
| `growthFactor` | `float` | Factor multiplicador del coste por nivel (ej: 1.5 = +50% por nivel) |
| `maxLevel` | `int` | Nivel máximo alcanzable |
| `timeReductionPerLevel` | `float` | Reducción del multiplicador de tiempo por nivel — solo mesas de trabajo |
| `rewardIncreasePerLevel` | `float` | Incremento del multiplicador de recompensa por nivel — solo mesas de trabajo |
| `baseStarChance` | `float` | Probabilidad base de dar estrella (0-100) |
| `starChanceIncreasePerLevel` | `float` | Incremento de probabilidad de estrella por nivel |
| `maxStarChance` | `float` | Probabilidad máxima de estrella (0-100) |

---

### Métodos
| Método | Descripción |
|---|---|
| `GetCostForLevel(int)` | Devuelve el coste para un nivel concreto: `baseCost × growthFactor^(level-1)` |
| `GetStarChanceForLevel(int)` | Devuelve la probabilidad de estrella para un nivel concreto, con tope en `maxStarChance` |

---

### Es usado desde
| Script | Motivo |
|---|---|
| `UpgradeableBase` | `GetCostForLevel` al calcular el coste de mejora |
| `WorkDeskUpgradeable` | `GetStarChanceForLevel` y `timeReductionPerLevel`, `rewardIncreasePerLevel` al mejorar |
| `UpgradePanelUI` | Muestra `elementName`, `description`, `elementImage` y el coste formateado |

---

### SOs existentes en el proyecto
| Archivo | Usado en |
|---|---|
| `WorkDeskUpgrade` | Todas las `WorkDesk` del taller |
| `WorkerUpgrades` | El `Worker` del taller |

---

### Notas técnicas
- Los campos de tiempo, recompensa y estrella son genéricos — no todos aplican a todos los elementos. `WorkerUpgradeable` ignora `timeReductionPerLevel` y `rewardIncreasePerLevel` por ejemplo
- Se pueden crear tantos SOs como elementos mejorables haya en el juego con `Create → Idle → Upgrade Data`
