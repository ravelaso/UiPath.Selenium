# Plan de implementación — Wrapper de Selenium (`Ravelaso.UiPath.Selenium`)

> **Estado:** aprobado · **Fecha:** 2026-10-20 · **Alcance:** primera base del cliente Selenium
> **Nota:** la implementación todavía no está escrita. Este documento es la especificación de referencia para construirla.

---

## 1. Objetivo

Construir un cliente .NET que abstraiga Selenium WebDriver para que un equipo de RPA (UiPath) pueda:

1. Declarar **qué browser** se va a usar (`Chrome`, `Edge`, `Firefox`) en el constructor, sin escribir una línea de `new ChromeDriver()` / `new EdgeDriver()` en el código de negocio.
2. Contar con una **espera global entre pasos** (equivalente a un `Time.Sleep(4)`), configurable, que se aplique automáticamente en **cada** operación expuesta.
3. Localizar elementos de forma **simplificada** (`Loc.Id("x")`, `Loc.Css(".btn")`, `Loc.Id("x").And.Class("btn")`) en lugar de memorizar la API `By` de Selenium.
4. Comprobar **existencia/visibilidad** de elementos y **esperar** a que aparezcan con timeout real.
5. Mantener **IntelliSense completo** al escribir métodos propios sobre `_driver` (`.Manage()`, `.Navigate()`, `.FindElement()`, `.SwitchTo()`…).

---

## 2. Estado inicial del repositorio

| Elemento | Valor |
|---|---|
| Solución | `Ravelaso.UiPath.Selenium.slnx` (1 proyecto) |
| Proyecto | `Ravelaso.UiPath.Selenium.Core` → `net10.0`, `Nullable enable`, `ImplicitUsings enable` |
| Dependencias actuales | `Selenium.WebDriver` 4.51.0 (se añadirá `Selenium.Support` 4.51.0, ver 2.2) |
| Código existente | `RavelasoWebClient.cs` con la clase **vacía** `RavelasoEdgeClient` (nombre de archivo ≠ nombre de clase) |
| IDE | Rider (`.idea/`) |
| SDK local | `dotnet` 10.0.102 (SDKs 6 / 8 / 9 / 10 disponibles) |
| UiPath | Studio **25.10.6** instalado en el equipo |

### 2.1 TFM objetivo vs. UiPath Studio

UiPath Studio 25.10 soporta `net8.0` en su editor .NET (además de `net6.0` y `net461` legacy), pero **no soporta `net10.0`**. El proyecto actual en `net10.0` es, por tanto, **no consumible** desde un workflow de UiPath tal cual.

**Decisión: multi-target `net8.0;net10.0`.**

| TFM | Destino |
|---|---|
| `net8.0` | Workflows de UiPath Studio 25.10 y cualquier proyecto .NET 8 LTS |
| `net10.0` | Desarrollo local en Rider con el C# más reciente |

Se descarta `netstandard2.0`: UiPath ya soporta `net8.0`, y mantenerlo obligaba a renunciar a APIs modernas y a `SelectElement` (ver 2.2). El único coste es no poder consumir la librería desde workflows .NET Framework 4.6.1, que no es un caso de uso previsto.

Ambos TFMs resuelven el asset `net8.0` de `Selenium.WebDriver` 4.51.0 (el paquete no publica un asset `net10.0`).

### 2.2 `SelectElement` vive en otro paquete: `Selenium.Support`

Verificación por reflexión sobre los assemblies reales de `Selenium.WebDriver` 4.51.0 (net8.0) y `Selenium.Support` 4.51.0 (netstandard2.0):

| Tipo | `Selenium.WebDriver` | `Selenium.Support` |
|---|---|---|
| `OpenQA.Selenium.Support.UI.WebDriverWait` | ✅ | — |
| `OpenQA.Selenium.Support.UI.SelectElement` | ❌ | ✅ |
| `OpenQA.Selenium.Support.UI.PopupWindowFinder` | ❌ | ✅ |
| `OpenQA.Selenium.Support.UI.LoadableComponent` | ❌ | ✅ |

**Decisión:** añadir `Selenium.Support` 4.51.0 como `PackageReference` y usar `SelectElement` nativo. Su único asset es `netstandard2.0`, compatible con `net8.0` y `net10.0`, así que no hace falta ningún TFM adicional. **No se requiere helper interno de dropdowns.**

### 2.3 `Screenshot.SaveAsFile` sólo tiene una sobrecarga

En Selenium 4.51 `OpenQA.Selenium.Screenshot` expone únicamente `SaveAsFile(string)`, que guarda en PNG. Ya no existe la sobrecarga `SaveAsFile(string, ImageFormat)` y el enum `ImageFormat` sólo aparece en el namespace BiDi (`OpenQA.Selenium.BiDi.BrowsingContext.ImageFormat`).

**Decisión:** usar `SaveAsFile(ruta)` y documentar que la captura se guarda en PNG.

---

## 3. Decisiones de diseño

| # | Decisión | Motivo |
|---|---|---|
| D1 | Multi-target `net8.0;net10.0` | Cubre UiPath Studio 25.10 (`net8.0`) y el desarrollo local en Rider (`net10.0`) |
| D2 | Sleep global **+** esperas condicionales | Cubre el requisito literal (`Time.Sleep`) sin ser frágil ante páginas lentas |
| D3 | `GlobalDelay` por defecto = **5 segundos** | Valor pedido; configurable a `TimeSpan.Zero` para desactivarlo |
| D4 | Locators concatenables (builder fluido) | Combinar ID + clase CSS u otras condiciones sin memorizar XPath |
| D5 | `_driver` tipado como `IWebDriver` | Es la clave del requisito de IntelliSense |
| D6 | `BrowserDriverFactory` estático | El usuario nunca ve `ChromeDriver`/`EdgeDriver`/`FirefoxDriver` |
| D7 | `Selenium.Support` como dependencia explícita | Aporta `SelectElement` y `PopupWindowFinder` (ver 2.2) |
| D8 | Namespace plano `Ravelaso.UiPath.Selenium.Core` | Menos `using` para el consumidor final en UiPath |
| D9 | C# moderno sin restricciones | Sin `netstandard2.0` se pueden usar `record`, `init`, `required` y atributos de nulabilidad |

---

## 4. Arquitectura

```
Ravelaso.UiPath.Selenium/
├─ Ravelaso.UiPath.Selenium.slnx
├─ docs/
│  ├─ README.md          ← índice + quick start
│  ├─ plan.md            ← este documento
│  ├─ api.md             ← referencia de la API del cliente
│  └─ locators.md        ← reglas del builder de localizadores
└─ Ravelaso.UiPath.Selenium.Core/
   ├─ Ravelaso.UiPath.Selenium.Core.csproj   ← multi-target net8.0;net10.0
   ├─ BrowserType.cs                          ← enum { Chrome, Edge, Firefox }
   ├─ BrowserOptions.cs                       ← Headless, ventana, timeouts, delay
   ├─ BrowserDriverFactory.cs                 ← crea el IWebDriver correcto
   ├─ LocatorType.cs                          ← estrategias de localización
   ├─ Locator.cs                              ← builder fluido (inmutable)
   ├─ Loc.cs                                  ← fachada estática (punto de entrada)
   └─ RavelasoWebClient.cs                    ← el cliente
```

---

## 5. Diseño detallado

### 5.1 `BrowserType`

```csharp
public enum BrowserType { Chrome, Edge, Firefox }
```

### 5.2 `BrowserOptions`

Objeto de configuración mutable, instanciado por el usuario y entregado al cliente:

| Propiedad | Tipo | Default | Descripción |
|---|---|---|---|
| `Browser` | `BrowserType` | `Edge` | Browser a lanzar |
| `Headless` | `bool` | `false` | `--headless=new` (Chrome/Edge), `-headless` (Firefox) |
| `Maximize` | `bool` | `true` | Maximiza la ventana al arrancar (ignorado en headless) |
| `WindowWidth` | `int` | `1920` | Ancho de ventana |
| `WindowHeight` | `int` | `1080` | Alto de ventana |
| `PageLoadTimeout` | `TimeSpan` | 60 s | `Manage().Timeouts().PageLoad` |
| `ImplicitWait` | `TimeSpan` | `TimeSpan.Zero` | `Manage().Timeouts().ImplicitWait` (no se mezcla con esperas explícitas) |
| `GlobalDelay` | `TimeSpan` | **5 s** | Espera global entre pasos (D3) |
| `ElementWaitTimeout` | `TimeSpan` | 10 s | Timeout de las esperas condicionales por defecto |
| `BinaryPath` | `string?` | `null` | Ruta al ejecutable del browser |
| `ExtraArguments` | `List<string>` | vacío | Argumentos extra del navegador |

### 5.3 `BrowserDriverFactory`

```csharp
public static IWebDriver Create(BrowserType browser, BrowserOptions? options = null);
```

- Traduce `BrowserOptions` a `ChromeOptions` / `EdgeOptions` / `FirefoxOptions`.
- Aplica los timeouts con `Manage().Timeouts()`.
- Maximiza si corresponde.
- Lanza `ArgumentOutOfRangeException` para un `BrowserType` no soportado.

### 5.4 `Loc` + `Locator` (builder de localizadores)

`Loc` es una **clase estática** que devuelve instancias de `Locator`. `Locator` es **inmutable**: cada encadenado devuelve una instancia nueva, de modo que un locator base puede reutilizarse como prefijo de varias búsquedas.

`Locator` expone:
- Métodos de encadenado: `Tag`, `Id`, `Class`, `Name`, `Attr`, `Text`, `Css`, `XPath`, `LinkText`, `PartialLinkText`.
- La propiedad `And` (marcador de lectura) que permite escribir `Loc.Id("x").And.Class("y")`.
- Conversión **implícita** a `By`, para poder pasarlo directamente a `FindElement` o a los métodos del cliente.
- `ToBy()` explícito y `ToString()` con el selector generado.

Reglas de render (detalle en [`locators.md`](./locators.md)):

- **Condición única** → se devuelve el `By` nativo (`By.Id`, `By.ClassName`, `By.CssSelector`…).
- **Varias condiciones sin XPath ni texto** → modo **CSS**: `tag#id.clase1.clase2[name="n"][attr="v"]`.
- **Con XPath o texto** → modo **XPath**, el resto se convierte a predicados.
- Un fragmento CSS con combinadores (`>`, `+`, `~`, espacio) es **atómico**: encadenar sobre él produce un error explícito que remite a `Loc.XPath`.

### 5.5 `RavelasoWebClient`

```csharp
public class RavelasoWebClient : IDisposable
{
    protected readonly IWebDriver _driver;   // D5 — clave del IntelliSense
    public IWebDriver Driver => _driver;      // escape hatch a Selenium crudo
    public BrowserOptions Options { get; }
    public BrowserType Browser => Options.Browser;
    public TimeSpan GlobalDelay { get; set; }
    public TimeSpan ElementWaitTimeout { get; set; }
}
```

#### Pipeline de cada paso

```
BeforeStep()          → Thread.Sleep(GlobalDelay)      ← la "espera global"
            → WaitElement(locator, timeout)  ← espera condicional real (WebDriverWait)
            → acción (Click / SendKeys / lectura)
```

`BeforeStep()` es `protected virtual` para que un cliente derivado pueda enganchar su propia lógica.

#### Superficie pública expuesta

| Grupo | Métodos |
|---|---|
| Navegación | `Navigate(url)`, `GetTitle()`, `GetUrl()`, `GetPageSource()` |
| Interacción | `Click(locator)`, `Type(locator, texto, clear = true)`, `GetText(locator)`, `GetAttribute(locator, nombre)` |
| Detección | `Exists(locator, timeout?)`, `IsDisplayed(locator)`, `WaitFor(locator, timeout?)` |
| Dropdowns | `Select(locator, valor)`, `SelectByIndex(locator, índice)` — sobre `<select>` nativos vía `SelectElement` (`Selenium.Support`) |
| Diagnóstico | `Screenshot(ruta)` (PNG), `ExecuteScript(script, args)` |
| Ciclo de vida | `Quit()`, `Dispose()` |

Todos aceptan `Locator` (conversión implícita a `By`) **y** `By` crudo, de modo que se puede mezclar el wrapper con Selenium nativo.

---

## 6. Requisito de IntelliSense — cómo se garantiza

1. `_driver` es de tipo **`IWebDriver`** (interfaz), nunca `ChromeDriver`. Por eso IntelliSense ofrece `Manage()`, `Navigate()`, `FindElement()`, `SwitchTo()`, `Title`, `Url`, `PageSource`, `Close()`, `Quit()`.
2. El campo es `protected` → las clases derivadas también lo autocompletan.
3. `public IWebDriver Driver => _driver;` da salida a Selenium crudo cuando el wrapper no cubra una necesidad.
4. XML documentation en todas las clases y miembros públicos + `GenerateDocumentationFile` → tooltips documentados.
5. `dotnet restore` limpio y build verde en los dos TFM → metadatos de referencia correctos para los IDE.

**Verificación manual:** escribir `_driver.Manage().` debe listar `Window`, `Timeouts()`, `Cookies`, `Logs`, `Network`, `Timeouts`.

---

## 7. Orden de implementación

| Paso | Tarea | Entregable |
|---|---|---|
| 1 | `csproj`: multi-target `net8.0;net10.0`, `Selenium.Support`, `LangVersion`, XML docs | `.csproj` actualizado |
| 2 | `BrowserType`, `BrowserOptions` | Enums y configuración |
| 3 | `BrowserDriverFactory` | Creación del driver por browser |
| 4 | `LocatorType`, `Locator`, `Loc` | Builder de localizadores |
| 5 | `RavelasoWebClient`: base, `BeforeStep`, pipeline, ciclo de vida | Cliente base |
| 6 | Operaciones: navegación → interacción → detección → dropdowns → diagnóstico | API completa |
| 7 | Build de los 2 TFM + smoke test | Verificación |

---

## 8. Verificación

- `dotnet build` verde en `net8.0` y `net10.0`.
- **Smoke test** con `data:text/html,…` (sin dependencia de red): abrir Edge headless, `Navigate`, `Click` sobre `<button id="go">`, `GetText`, `Exists` de un id inexistente → `false`, `WaitFor` con timeout, `Select` sobre un `<select>`.
- Comprobación manual de IntelliSense en Rider (`.Manage()`, `.FindElement()`).

---

## 9. Fuera de alcance (fase 2)

- Empaquetado como UiPath Library (`.uipx` / `uipath.json`) y publicación en Orchestrator.
- `RemoteWebDriver` / Selenium Grid.
- Capturas en base64, descarga de ficheros, manejo de iframes/popups.
- Eventos de WebDriver BiDi, `PageLoadStrategy` por browser, proxies, user-agent.
- Suites de tests (xUnit) y CI.

---

## 10. Capacidades de lenguaje y dependencias

Al quedar fuera `netstandard2.0`, el código puede usar el C# moderno sin restricciones:

- `record`, `init` y `required`.
- Atributos de anotación nula (`NotNullWhen`, `MemberNotNull`…).
- APIs de BCL de .NET 8/10 (`OperatingSystem.IsWindows()`, `Environment.ProcessPath`, `TimeProvider`…).

`Nullable enable` e `ImplicitUsings` funcionan en ambos TFMs sin shims.

| Paquete | Versión | Asset resuelto en `net8.0` / `net10.0` |
|---|---|---|
| `Selenium.WebDriver` | 4.51.0 | `net8.0` |
| `Selenium.Support` | 4.51.0 | `netstandard2.0` |
