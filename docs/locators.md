# Localizadores — `Loc` y `Locator`

Los localizadores del wrapper son **fluidos y combinables**: se define un elemento por ID, por clase, por CSS… y se pueden **encadenar** condiciones.

```csharp
using Ravelaso.UiPath.Selenium.Core;

Loc.Id("username")                     // → By.Id("username")
Loc.Css(".btn-primary")                // → By.CssSelector(".btn-primary")
Loc.Id("username").And.Class("form-control")   // → #username.form-control
```

---

## Cómo funciona

- `Loc` es una **clase estática**: es el punto de entrada.
- `Loc.X(...)` devuelve un `Locator`, que representa un selector **en construcción**.
- `Locator` es **inmutable**: cada encadenado devuelve una instancia nueva. Esto permite reutilizar un locator base como prefijo:

  ```csharp
  var form = Loc.Tag("form");
  var user = form.And.Id("username");   // no modifica `form`
  var pass = form.And.Id("password");   // tampoco
  ```

- `Locator` tiene **conversión implícita a `By`**, así que se puede pasar directamente a cualquier método de Selenium o del cliente:

  ```csharp
  driver.FindElement(Loc.Id("username"));   // compila sin cast
  client.Click(Loc.Id("username"));
  ```

- `Locator.ToBy()` fuerza la conversión de forma explícita y `Locator.ToString()` devuelve el selector generado.

---

## Puntos de entrada

| Método | `By` nativo equivalente |
|---|---|
| `Loc.Id("x")` | `By.Id("x")` |
| `Loc.Name("x")` | `By.Name("x")` |
| `Loc.Class("x")` | `By.ClassName("x")` |
| `Loc.Tag("x")` | `By.TagName("x")` |
| `Loc.Css(".x")` | `By.CssSelector(".x")` |
| `Loc.XPath("//x")` | `By.XPath("//x")` |
| `Loc.LinkText("x")` | `By.LinkText("x")` |
| `Loc.PartialLinkText("x")` | `By.PartialLinkText("x")` |
| `Loc.Text("x")` | XPath con texto |
| `Loc.Attr("data-id", "7")` | CSS `[data-id="7"]` |
| `Loc.Of(LocatorType.Id, "x")` | Según `LocatorType` |

`LocatorType` cubre `Id`, `Name`, `ClassName`, `TagName`, `CssSelector`, `XPath`, `LinkText` y `PartialLinkText`; sirve para localizadores **dinámicos** (por ejemplo, leídos de un fichero de configuración).

---

## Encadenado

Los mismos nombres están disponibles sobre `Locator`, y la propiedad marcadora `And` permite leer el encadenado de forma explícita:

```csharp
Loc.Tag("input").And.Name("user").And.Attr("type", "text")
Loc.Id("username").And.Class("form-control")
Loc.Css(".btn").And.Tag("div")
Loc.XPath("//li").And.Class("item")
```

`And` es sólo un marcador: `Loc.Id("x").Class("y")` produce exactamente lo mismo que `Loc.Id("x").And.Class("y")`.

---

## Reglas de render

El selector final depende de las condiciones acumuladas.

### 1. Una sola condición → `By` nativo

Se devuelve el `By` nativo de Selenium (más rápido y familiar):

```csharp
Loc.Id("username")        → By.Id("username")
Loc.Class("btn")          → By.ClassName("btn")
Loc.Css(".btn-primary")   → By.CssSelector(".btn-primary")
Loc.XPath("//div")        → By.XPath("//div")
```

### 2. Varias condiciones, sin XPath ni texto → modo **CSS**

```
tag#id.clase1.clase2[name="n"][attr="v"]
```

```csharp
Loc.Tag("input").And.Id("user").And.Class("form-control").And.Attr("type", "text")
→ input#user.form-control[type="text"]
```

- El **ID** usa `#id` si es un identificador CSS válido; si no, se degrada a `[id="valor"]`.
- La **clase** usa `.clase` si es válida; si no, se degrada a `[class~="valor"]` (coincidencia exacta por token).
- Los **atributos** se renderizan como `[nombre="valor"]` con el valor entrecomillado y escapado.

### 3. Varias condiciones, con XPath o texto → modo **XPath**

El resto de condiciones se convierten en predicados XPath:

```csharp
Loc.Id("x").And.Class("btn")
→ //*[@id="x"][contains(concat(' ', normalize-space(@class), ' '), ' btn ')]

Loc.XPath("//li").And.Class("item")
→ //li[contains(concat(' ', normalize-space(@class), ' '), ' item ')]

Loc.Id("x").And.Text("Guardar")
→ //*[@id="x"][contains(normalize-space(text()), "Guardar")]
```

- La **clase** en XPath exige coincidencia exacta por token (mismo criterio que `By.ClassName`).
- El **tag** en XPath se expresa como `[local-name()="span"]`, para poder combinarse con cualquier raíz.

### 4. Fragmentos CSS con combinadores

Un fragmento CSS que contenga combinadores (`>`, `+`, `~`, espacio o coma) describe una **selección que no se puede componer** con otras condiciones. En ese caso encadenar lanza:

```
InvalidOperationException:
  El selector CSS 'div > span' contiene combinadores y no puede combinarse con otras
  condiciones. Usa Loc.XPath(...) para ese caso.
```

Motivo: `div > span` + `.active` no es `#algo .active`; mezclar produciría un selector incorrecto. La alternativa es:

```csharp
Loc.XPath("//div/span[contains(@class, 'active')]")
```

---

## Buenas prácticas

1. **Prefiere IDs y atributos estables** frente a clases CSS, que cambian con el maquetado.
2. **Una condición siempre que puedas**: un solo ID produce el `By` nativo, que es más rápido que un selector compuesto.
3. **Reserva XPath para texto** o para relaciones (`//div[@id='x']//span`).
4. **Reutiliza el prefijo**: `Loc.Tag("form")` como base de varios locators.
5. **Comprueba existencia antes de interactuar** si el elemento puede no estar:
   `if (client.Exists(Loc.Id("banner"))) { … }`.

---

## Combine con Selenium nativo

Como `Locator` se convierte a `By`, se puede mezclar con el API crudo:

```csharp
var input = driver.FindElement(Loc.Id("username"));
driver.FindElement(By.Name("password")).SendKeys("1234");
```

Y al revés, los métodos del cliente aceptan un `By` construido a mano:

```csharp
client.Click(By.CssSelector(".legacy-selector"));
```
