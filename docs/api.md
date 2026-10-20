# Referencia de la API — `RavelasoWebClient`

Namespace único: `Ravelaso.UiPath.Selenium.Core`.

---

## `RavelasoWebClient`

El cliente principal. Envuelve un `IWebDriver` y aplica la **espera global** en cada paso expuesto.

### Constructores

```csharp
public RavelasoWebClient(BrowserType browser);
public RavelasoWebClient(BrowserType browser, BrowserOptions options);
```

```csharp
using var client = new RavelasoWebClient(BrowserType.Chrome);
using var client = new RavelasoWebClient(BrowserType.Firefox, new BrowserOptions { Headless = true });
```

> El constructor **crea el driver real** (`ChromeDriver` / `EdgeDriver` / `FirefoxDriver`) a través de `BrowserDriverFactory`. El navegador se lanza al construir el cliente.

### Propiedades

| Miembro | Tipo | Descripción |
|---|---|---|
| `Driver` | `IWebDriver` | Acceso directo a Selenium crudo (escape hatch) |
| `Options` | `BrowserOptions` | Configuración viva del cliente |
| `Browser` | `BrowserType` | Browser en uso |
| `GlobalDelay` | `TimeSpan` | Espera global aplicada en cada paso. Default **5 s**. `TimeSpan.Zero` la desactiva |
| `ElementWaitTimeout` | `TimeSpan` | Timeout por defecto de las esperas condicionales. Default 10 s |

### Campo protegido

```csharp
protected readonly IWebDriver _driver;
```

Tipado como **interfaz `IWebDriver`** para que IntelliSense muestre `Manage()`, `Navigate()`, `FindElement()`, `SwitchTo()`, `Title`, `Url`, `PageSource`… Es el punto de extensión para escribir métodos propios:

```csharp
public class MiCliente : RavelasoWebClient
{
    public MiCliente() : base(BrowserType.Edge) { }

    public void Maximizar() => _driver.Manage().Window.Maximize();
}
```

### Navegación

| Método | Devuelve | Notas |
|---|---|---|
| `Navigate(string url)` | `void` | Aplica la espera global y luego navega |
| `GetTitle()` | `string` | Título de la página actual |
| `GetUrl()` | `string` | URL actual |
| `GetPageSource()` | `string` | HTML completo de la página |

### Interacción

| Método | Devuelve | Notas |
|---|---|---|
| `Click(By locator)` | `void` | Espera global → espera condicional → `Click()` |
| `Type(By locator, string text, bool clear = true)` | `void` | `clear` vacía el campo antes de escribir |
| `GetText(By locator)` | `string` | Texto visible del elemento |
| `GetAttribute(By locator, string name)` | `string?` | `null` si el atributo no existe |

### Detección

| Método | Devuelve | Notas |
|---|---|---|
| `Exists(By locator)` | `bool` | No lanza excepción si el elemento no existe |
| `Exists(By locator, TimeSpan timeout)` | `bool` | Espera hasta `timeout` a que aparezca |
| `IsDisplayed(By locator)` | `bool` | `false` si no existe o no es visible |
| `WaitFor(By locator)` | `bool` | Espera `ElementWaitTimeout` a que aparezca |
| `WaitFor(By locator, TimeSpan timeout)` | `bool` | Espera con timeout explícito |

### Dropdowns (`<select>` nativos)

Implementado con `OpenQA.Selenium.Support.UI.SelectElement`, del paquete `Selenium.Support` 4.51.0.

| Método | Devuelve | Notas |
|---|---|---|
| `Select(By locator, string value)` | `bool` | Selecciona por **valor** y, si no coincide, por **texto visible**. `false` si no hay coincidencia |
| `SelectByIndex(By locator, int index)` | `bool` | `false` si el índice está fuera de rango |

### Diagnóstico

| Método | Devuelve | Notas |
|---|---|---|
| `Screenshot(string path)` | `string` | Guarda un PNG y devuelve la ruta absoluta. Crea el directorio si no existe |
| `ExecuteScript(string script, params object[] args)` | `object?` | Ejecuta JS en la página |

### Ciclo de vida

| Miembro | Descripción |
|---|---|
| `Quit()` | Cierra el navegador y libera el driver |
| `Dispose()` | Implementa `IDisposable`; llama a `Quit()` de forma segura (idempotente) |

---

## `BrowserOptions`

| Propiedad | Tipo | Default |
|---|---|---|
| `Browser` | `BrowserType` | `Edge` |
| `Headless` | `bool` | `false` |
| `Maximize` | `bool` | `true` |
| `WindowWidth` | `int` | `1920` |
| `WindowHeight` | `int` | `1080` |
| `PageLoadTimeout` | `TimeSpan` | 60 s |
| `ImplicitWait` | `TimeSpan` | `TimeSpan.Zero` |
| `GlobalDelay` | `TimeSpan` | 5 s |
| `ElementWaitTimeout` | `TimeSpan` | 10 s |
| `BinaryPath` | `string?` | `null` |
| `ExtraArguments` | `List<string>` | vacío |

---

## `BrowserType`

```csharp
public enum BrowserType { Chrome, Edge, Firefox }
```

---

## `BrowserDriverFactory`

```csharp
public static IWebDriver Create(BrowserType browser, BrowserOptions? options = null);
```

Traduce `BrowserOptions` a las opciones nativas del navegador:

| Browser | Headless | Tamaño de ventana |
|---|---|---|
| Chrome | `--headless=new` | `--window-size=W,H` |
| Edge | `--headless=new` | `--window-size=W,H` |
| Firefox | `-headless` | `-width=W -height=H` |

---

## Pipeline de un paso

Toda operación expuesta sigue esta secuencia:

```
1. BeforeStep()          → Thread.Sleep(GlobalDelay)      ← espera global entre pasos
2. WaitElement(locator)  → WebDriverWait(timeout)         ← espera condicional real
3. Acción                → Click / SendKeys / lectura
```

`BeforeStep()` es `protected virtual` para poder ampliarlo en clases derivadas.

> `GlobalDelay` se aplica **antes** de la acción; con `GlobalDelay = 5 s` y 20 pasos, el proceso 누적ará ~100 s sólo en esperas. Para automatización rápida conviene `TimeSpan.Zero` y confiar en las esperas condicionales.

---

## Notas de versión (Selenium 4.51.0)

Detalles comprobados por reflexión sobre los assemblies reales, útiles para verificar la implementación:

| Aspecto | Realidad en 4.51.0 |
|---|---|
| `SelectElement` | **No** está en `Selenium.WebDriver`; viene en `Selenium.Support` 4.51.0 |
| `WebDriverWait` | Sí está en `Selenium.WebDriver` (`OpenQA.Selenium.Support.UI`) |
| `Screenshot.SaveAsFile` | Sólo existe la sobrecarga `SaveAsFile(string)` → guarda PNG. La sobrecarga con `ImageFormat` ya no existe |
| `ImageFormat` | Sólo aparece en `OpenQA.Selenium.BiDi.BrowsingContext.ImageFormat` |
| Assets publicados | `Selenium.WebDriver` → `net462`, `net8.0`, `netstandard2.0` · `Selenium.Support` → `netstandard2.0` |
| `IWebDriver.Manage()` | Devuelve `IOptions` → `Manage().Window`, `Manage().Timeouts()`, `Manage().Cookies`, `Manage().Logs`, `Manage().Network` |

### Excepciones que el cliente controla

| Excepción | Dónde se captura |
|---|---|
| `NoSuchElementException` (deriva de `NotFoundException`) | `Exists`, `IsDisplayed`, `WaitFor` → devuelven `false` |
| `WebDriverTimeoutException` | `Exists`, `IsDisplayed`, `WaitFor` → devuelven `false` |
| `InvalidElementStateException` | `Type` al vaciar un campo que no es un input (`contenteditable`, etc.) → se ignora y se escribe igualmente |
