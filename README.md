# HarryUtils
Utils que uso generalmente en mis proyectos de Unity

## Instalar en un proyecto
El paquete está publicado en [OpenUPM](https://openupm.com/packages/com.harryb97.harryutils/). Con la [CLI de OpenUPM](https://openupm.com/docs/getting-started-cli.html), desde la carpeta del proyecto:

```sh
openupm add com.harryb97.harryutils
```

Las actualizaciones aparecen en el Package Manager (**My Registries**) con el botón **Update**.

También se puede instalar por URL de git con la etiqueta de la versión:

```json
"com.harryb97.harryutils": "https://github.com/HarryB977/HarryUtils.git#2.0.1"
```

## Pools de objetos
`Pool<T>` (namespace `HarryUtils.ObjectPool`) reutiliza componentes en vez de instanciarlos y destruirlos.

```csharp
var pool = new Pool<Bullet>(bulletPrefab, _prewarm: 10, _maxSize: 20, _overflow: PoolOverflow.ReuseOldest);

Bullet bullet = pool.Get();   // activa el objeto; null si la política es Fail y la pool está llena
pool.Release(bullet);         // lo desactiva y lo devuelve; false (con aviso) si no es de la pool o ya estaba liberado
```

- **Eventos:** `Created`, `Got`, `Released` y `Destroyed`.
- **`IPoolable`:** los componentes del objeto que lo implementen reciben `OnGetFromPool` y `OnReleaseToPool`.
- **Políticas** (`PoolOverflow`) cuando se alcanza `_maxSize` (0 = sin límite): `Grow` crea más e ignora el máximo, `ReuseOldest` libera y entrega de nuevo el objeto activo más antiguo (`Oldest`; `Active` se enumera del más antiguo al más nuevo) y `Fail` no entrega nada.
- Otros miembros: `TryGet`, `Prewarm`, `ReleaseAll`, `Clear` (destruye las inactivas), `Dispose` (destruye todas) y los contadores `CountActive`, `CountInactive` y `CountAll`.

`ObjectPool<T>` y `LimitedObjectPool<T>` siguen disponibles.

## Editar el paquete desde un proyecto
1. En el manifest del proyecto, apuntar el paquete a la copia local del repositorio:
   `"com.harryb97.harryutils": "file:<ruta-local>/HarryUtils"`
2. Hacer los cambios, subir la versión en `package.json` y anotarlos en `CHANGELOG.md`.
3. Hacer commit, crear la etiqueta con la versión (por ejemplo `git tag -a 2.0.2 -m "harryutils 2.0.2"`) y subir ambos: `git push --follow-tags`.
4. Volver a poner en el manifest la versión del registro. OpenUPM publica la nueva versión a partir de la etiqueta en unos 15-30 minutos.

Una etiqueta publicada no se mueve: si hay que corregir algo, se sube la versión.
