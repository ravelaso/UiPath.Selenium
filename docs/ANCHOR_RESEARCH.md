# Anchor System Research

**Objetivo:** Entender cómo UiPath implementa anchors para poder crear un sistema similar typado y fluent en Ravelaso.UiPath.Selenium.

---

## 1. ¿Qué son los Anchors en UiPath?

Los anchors (anclajes) en UiPath son un patrón de búsqueda que permite encontrar elementos dinámicos usando como referencia otro elemento visible en la pantalla.

**Problema que resuelven:**
- Elementos sin IDs o atributos únicos
- Elementos que cambian de posición (tablas dinámicas)
- Elementos cuya posición relativa es más stable que su ubicación absoluta

**Ejemplo conceptual:**
```
Tabla dinámica que cambia cada día:

Día 1:
┌─────────────┬──────────────┬──────────┐
│ Report Name │ Date         │ Download │
├─────────────┼──────────────┼──────────┤
│ Sales Q4    │ 2026-10-15   │ [↓]      │ ← Quiero descargar
│ Inventory   │ 2026-10-16   │ [↓]      │
└─────────────┴──────────────┴──────────┘

Día 2 (orden cambió):
┌─────────────┬──────────────┬──────────┐
│ Report Name │ Date         │ Download │
├─────────────┼──────────────┼──────────┤
│ Inventory   │ 2026-10-16   │ [↓]      │
│ Sales Q4    │ 2026-10-15   │ [↓]      │ ← Aún quiero este
└─────────────┴──────────────┴──────────┘

SIN ANCHORS: Clickear fila 0 (❌ fallaría Día 2)
CON ANCHORS: "Encontrar botón en la fila donde fecha = 2026-10-15" (✅ funciona ambos días)
```

---

## 2. Cómo UiPath Implementa Anchors

### **Componentes Principales**

#### A. **Anchor (Referencia)**
- Es un **elemento visible** que se usa como punto de referencia
- Debe ser **relativamente stable** (su posición/existencia no cambia)
- Ejemplos: texto, imagen, etiqueta, nombre

#### B. **Target (Objetivo)**
- El **elemento que queremos manipular** (click, type, etc.)
- Se busca **relativo al anchor**
- Puede estar arriba, abajo, izquierda, derecha del anchor

#### C. **Direction (Dirección)**
- Define la relación espacial entre anchor y target
- UiPath busca el target **más cercano** en esa dirección

---

### **Estrategias de Búsqueda en UiPath**

#### **1. Busca por PROXIMIDAD ESPACIAL**
UiPath calcula la distancia del anchor al target en diferentes direcciones:

```
           ↑ UP (arriba)
           │
    ←──────●──────→  LEFT / RIGHT
           │
           ↓ DOWN (abajo)
```

**Algoritmo:**
1. Localiza el anchor en la pantalla
2. Define un área de búsqueda (ej: debajo del anchor)
3. Encuentra el elemento más cercano en esa dirección

**Ventaja:** No depende de XPath, funciona con elementos sin atributos

---

#### **2. Utiliza ATRIBUTOS UI**
UiPath también puede buscar por:
- **Text**: Texto visible en el elemento
- **Class**: Atributo CSS class
- **Role**: Accesibilidad (button, textbox, link)
- **Index**: Posición entre elementos similares

---

#### **3. Combinación ANCHOR + LOCATOR**
```
Anchor (referencia visual): "2026-10-22"
Target (elemento a manipular): Button con clase "download"

Búsqueda:
1. Encontrar elemento con texto "2026-10-22"
2. Desde ese punto, buscar el button más cercano (normalmente a la derecha o abajo)
3. Clickear ese button
```

---

## 3. Ejemplos de UiPath Anchors

### **Patrón 1: Tabla con Descarga Dinámica**

```
HTML:
<table>
  <tr>
    <td>Sales Report</td>
    <td>2026-10-22</td>
    <td><button class="download">↓</button></td>
  </tr>
  <tr>
    <td>Inventory</td>
    <td>2026-10-23</td>
    <td><button class="download">↓</button></td>
  </tr>
</table>

Flujo UiPath:
1. Use Anchor Activity
2. Anchor Point: Find Element → Text = "2026-10-22"
3. Target: Find Element → Button with Class "download"
4. Direction: Right (o Down si está en la siguiente columna)
5. Action: Click
```

**¿Por qué funciona?**
- El anchor "2026-10-22" es único en esa fila
- El botón siempre está al lado/debajo del anchor
- Si la tabla reordena, seguimos encontrando la fecha correcta

---

### **Patrón 2: Formulario con Labels**

```
HTML:
<div>
  <label>Email:</label>
  <input type="text" placeholder="Enter email">
</div>
<div>
  <label>Phone:</label>
  <input type="text" placeholder="Enter phone">
</div>

Flujo UiPath:
1. Anchor: Text = "Phone:"
2. Target: Input field
3. Direction: Right
4. Action: Type "555-1234"
```

---

### **Patrón 3: Lista con Acciones**

```
HTML:
<div class="item">
  <span class="name">Item A</span>
  <button class="edit">Edit</button>
  <button class="delete">Delete</button>
</div>

Flujo UiPath:
1. Anchor: Text = "Item A"
2. Target: Button with Text "Delete"
3. Direction: Right
4. Action: Click
```

---

## 4. Directivas de UiPath Anchors

### **Parámetros Clave**

| Parámetro | Valores | Descripción |
|---|---|---|
| **AnchorPoint** | Element (Selector) | El elemento de referencia |
| **Target** | Element (Selector) | El elemento a encontrar |
| **Position** | `Left, Right, Above, Below` | Dirección del target relativo al anchor |
| **Offset** | Pixels | Distancia permitida desde el anchor |
| **SearchScope** | `Parent, Ancestor, Child` | Límite de búsqueda |

---

## 5. Desafíos de los Anchors

### **❌ Lo que NO funciona bien**

1. **Anchors muy dinámicos**
   - Texto que cambia frecuentemente
   - Posiciones completamente aleatorias

2. **Elementos sin visual**
   - Elementos ocultos
   - Elementos sin atributos estables

3. **Múltiples matches**
   - Si hay varias fechas iguales
   - Si hay varios botones con mismo texto

4. **Cambios drásticos de layout**
   - Si el HTML estructura cambia completamente
   - Si el elemento se mueve a otro contenedor

---

## 6. Patrones de XPath Avanzado (Referencia para Selenium)

Como UiPath usa **proximidad espacial**, en Selenium podemos aproximarnos con **XPath avanzado:**

### **Opción A: XPath con "following-sibling"**
```xpath
// En la misma fila, encontrar el siguiente botón
//tr[contains(., '2026-10-22')]//button[@class='download']
```

### **Opción B: XPath con "ancestor"**
```xpath
// Encontrar el input que está después del label "Phone:"
//label[contains(text(), 'Phone:')]/following-sibling::input
```

### **Opción C: XPath con "preceding"**
```xpath
// Encontrar el botón cuyo hermano anterior es el span con "Item A"
//button[@class='delete'][preceding-sibling::span[contains(text(), 'Item A')]]
```

### **Opción D: XPath con "parent/ancestor"**
```xpath
// Encontrar el botón dentro del mismo contenedor que el ancla
//span[contains(text(), 'Item A')]/ancestor::div[@class='item']//button[@class='delete']
```

---

## 7. Propiedades que Hacen un Buen Anchor

### **✅ Características de un BUEN ANCHOR**

- ✓ **Único o suficientemente único** en su contexto
- ✓ **Stable** - No cambia en ejecuciones posteriores
- ✓ **Visible** - Se puede ver en la UI
- ✓ **Accesible** - No está oculto o disabled
- ✓ **Cercano al target** - Distancia razonable
- ✓ **En el mismo contenedor** - Mismo TR, DIV, etc.

### **❌ Características de un MAL ANCHOR**

- ✗ Texto que cambia constantemente
- ✗ Múltiples coincidencias
- ✗ Muy lejos del target
- ✗ En diferentes contenedores
- ✗ Elementos dinámicos (generados por JS)

---

## 8. Casos de Uso Reales

### **Caso 1: Web Scraping de Tabla Dinámicas**
```
Necesidad: Extraer datos de una fila específica en tabla que cambia

Anchor: Número de factura único (ej: "INV-2026-001")
Target: Celda con monto de la factura
Dirección: Right/Same Row
```

### **Caso 2: Rellenar Formularios Dinámicos**
```
Necesidad: Llenar campo de email cuya posición cambia

Anchor: Label "Email:"
Target: Input field adyacente
Dirección: Right o Below
```

### **Caso 3: Manipular Listas Dinámicas**
```
Necesidad: Editar un item específico en una lista que reordena

Anchor: Nombre del item (ej: "Project Alpha")
Target: Botón "Edit" en la misma fila
Dirección: Right
```

### **Caso 4: Navegar Interfaces Complejas**
```
Necesidad: Clickear botón de acción en panel con múltiples elementos

Anchor: Sección visible (ej: "Advanced Settings")
Target: Toggle switch en esa sección
Dirección: Below
```

---

## 9. Comparación: UiPath vs Selenium XPath

| Aspecto | UiPath Anchor | Selenium XPath |
|---|---|---|
| **Legibilidad** | Alta (visual) | Baja (compleja) |
| **Robustez** | Alta (espacial) | Media (sintaxis) |
| **Performance** | Más lento (cálculos) | Más rápido (directo) |
| **Mantenibilidad** | Fácil cambiar | Difícil debuggear |
| **Debugging** | Visual (pantalla) | Texto (consola) |

---

## 10. Requisitos para Sistema de Anchors en Ravelaso

Basado en la investigación, el sistema debería:

### **Funcionalidades Requeridas**

1. ✓ **Definir Anchor** - Especificar elemento de referencia (Locator)
2. ✓ **Definir Target** - Especificar elemento buscado (Locator)
3. ✓ **Definir Dirección** - Dónde buscar el target relativo al anchor
4. ✓ **Buscar** - Resolver el target basado en anchor + dirección
5. ✓ **Ejecutar Acción** - Click, Type, GetText, etc. en el elemento encontrado

### **API Typada Requerida**

- Fluent builder (como `Loc`)
- IntelliSense completo
- Sin strings mágicos (XPath manual)
- Manejo de errores claro

### **Direcciones Soportadas**

- `Right` - Target está a la derecha del anchor
- `Left` - Target está a la izquierda del anchor
- `Below` - Target está debajo del anchor
- `Above` - Target está arriba del anchor
- `SameRow` - Target en la misma fila del anchor
- `SameContainer` - Target en el mismo div/contenedor
- `Innermost` - Target es hijo del anchor

---

## 11. Próximos Pasos

1. **Diseñar API** - Basado en este análisis
2. **Implementar AnchorLocator** - Clase typada
3. **Agregar métodos a Locator** - `.AsAnchor()`, `.WithDirection()`, etc.
4. **Documentar con ejemplos** - Casos reales
5. **Testear** - Unit + integration tests

---

## Referencias

- [UiPath Anchor Activity Documentation](https://docs.uipath.com/activities/other-activities/anchor) (teórico)
- [UiPath UI Automation Best Practices](https://docs.uipath.com/studio/latest/user-guide/ui-automation-best-practices)
- XPath Advanced: `following-sibling`, `ancestor`, `preceding-sibling`
- HTML Structure: `<tr>`, `<div class="row">`, `<section>`

---

**Documento creado:** 2026-10-23  
**Estado:** Research Phase  
**Siguiente:** Design del AnchorLocator API
