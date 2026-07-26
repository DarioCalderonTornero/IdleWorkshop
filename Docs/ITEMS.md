# 📋 SCRIPTS — Grupo: Items

---

## ItemDefinition

**Tipo:** ScriptableObject  
**GameObject:** Ninguno — vive en `Assets/ScriptableObjects/Item`  
**Responsabilidad:** Define todos los datos de un objeto restaurable. Es la unidad básica de contenido del juego — cada objeto que un cliente puede traer al taller tiene su propio SO de tipo `ItemDefinition`.

---

### Campos configurables en el Inspector
| Campo | Tipo | Descripción |
|---|---|---|
| `itemName` | `string` | Nombre del objeto — debe ser único, se usa como clave en el sistema de guardado del bestiario |
| `description` | `string` | Descripción del objeto que se muestra en el bestiario |
| `itemPrefab` | `GameObject` | Prefab que se instancia como representación visual del objeto en escena |
| `sprite` | `Sprite` | Sprite del objeto — se asigna al `SpriteRenderer` del prefab al instanciarlo |
| `displayScale` | `Vector2` | Escala visual del objeto cuando se transporta |
| `rarity` | `ItemRarity` | Rareza del objeto — determina la probabilidad de que aparezca |
| `baseRepairTime` | `float` | Tiempo base de proceso en segundos — modificado por el multiplicador de la mesa |
| `rewardCoins` | `int` | Recompensa base en monedas — modificada por el multiplicador de la mesa |

---

### Enum asociado: `ItemRarity`
| Valor | Descripción |
|---|---|
| `Common` | Común — probabilidad más alta |
| `Uncommon` | Poco común |
| `Rare` | Raro |
| `Epic` | Épico |
| `Legendary` | Legendario — probabilidad más baja |

---

### Es usado desde
| Script | Motivo |
|---|---|
| `CustomerManager` | `GetRandom` para asignar un objeto a cada cliente |
| `Customer` | Instancia el prefab, asigna el sprite y transporta el objeto |
| `Worker` | `baseRepairTime` y `rewardCoins` para calcular tiempo y recompensa |
| `WorkTable` | `GetProcessTime` y `GetReward` aplican multiplicadores sobre los valores base |
| `BestiaryManager` | Clave de registro de descubrimientos, estrellas y vendidos |
| `BestiaryUI` | Muestra nombre, descripción, sprite y rareza en el panel de detalle |

---

### Notas técnicas
- `itemName` debe ser único por objeto — es la clave que usa `BestiaryManager` para guardar y cargar el estado del bestiario
- Se crean con `Create → Restaurador → Item Definition`

---

## ItemDatabase

**Tipo:** ScriptableObject  
**GameObject:** Ninguno — vive en `Assets/ScriptableObjects/Item`  
**Responsabilidad:** Contiene todos los `ItemDefinition` del juego y gestiona la selección aleatoria ponderada por rareza. Es el punto de entrada para obtener un objeto aleatorio para cada cliente.

---

### Campos configurables en el Inspector
| Campo | Tipo | Descripción |
|---|---|---|
| `items` | `ItemDefinition[]` | Array con todos los objetos restaurables del juego |
| `chanceCommon` | `float` | Probabilidad de rareza Common (por defecto 80) |
| `chanceUncommon` | `float` | Probabilidad de rareza Uncommon (por defecto 10) |
| `chanceRare` | `float` | Probabilidad de rareza Rare (por defecto 7) |
| `chanceEpic` | `float` | Probabilidad de rareza Epic (por defecto 2.5) |
| `chanceLegendary` | `float` | Probabilidad de rareza Legendary (por defecto 0.5) |

---

### API pública
| Método | Descripción |
|---|---|
| `GetAll()` | Devuelve todos los `ItemDefinition` — usado por `BestiaryUI` para poblar la cuadrícula |
| `GetRandom()` | Devuelve un `ItemDefinition` aleatorio ponderado por rareza |

---

### Es usado desde
| Script | Motivo |
|---|---|
| `CustomerManager` | `GetRandom` para asignar un objeto a cada cliente al spawnearlo |
| `BestiaryUI` | `GetAll` para mostrar todos los objetos en la cuadrícula del bestiario |
| `SaveManager` | Se pasa como referencia a `BestiaryManager.LoadSaveData` para reconstruir los `ItemDefinition` al cargar |

---

### Notas técnicas
- Las probabilidades de rareza deben sumar 100 — si no lo hacen el sistema puede no funcionar correctamente
- Si no hay ítems de la rareza sorteada, devuelve uno aleatorio de cualquier rareza con un warning en consola
- Se crea con `Create → Restaurador → Item Database`
