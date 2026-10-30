# HarryUtils
Utils que uso generalmente en mis proyectos de Unity

## Instalar en un proyecto
Se añade al `Packages/manifest.json` del proyecto con la URL de git y la etiqueta de la versión:

```json
"com.harryb97.harryutils": "https://github.com/RazgriZ77/HarryUtils.git#2.0.0"
```

Al ser un repositorio privado, Unity usa las credenciales de git del sistema (Git Credential Manager).

## Editar el paquete desde un proyecto
1. En el manifest del proyecto, apuntar el paquete a la copia local del repositorio:
   `"com.harryb97.harryutils": "file:<ruta-local>/HarryUtils"`
2. Hacer los cambios, subir la versión en `package.json` y anotarlos en `CHANGELOG.md`.
3. Hacer commit, crear la etiqueta con la versión (por ejemplo `git tag -a 2.0.1 -m "harryutils 2.0.1"`) y subir ambos: `git push --follow-tags`.
4. Volver a poner la URL de git en el manifest con la nueva etiqueta.

Una etiqueta publicada no se mueve: si hay que corregir algo, se sube la versión.
