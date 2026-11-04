# Change Log:

## 2.0.1

    - Publicado en OpenUPM: se instala con `openupm add com.harryb97.harryutils` y el Package Manager avisa de las actualizaciones
    - `package.json`: `repository` apunta a HarryB977/HarryUtils y el autor ya no incluye email
    - LICENSE: el titular pasa a ser HarryB97
    - README: instalación desde OpenUPM

## 2.0.0

### Cambios incompatibles

    - Se han eliminado 'SerializeToBytes' y 'DeserializeBytes'. Usaban 'BinaryFormatter', que es inseguro al deserializar datos externos y ya no existe en .NET 9

### Cambios

    - El ejemplo de ObjectPool pasa a ser un Sample importable desde el Package Manager ('Samples~/ObjectPool'). Antes sus scripts y prefabs se importaban en todos los proyectos
    - Los Assembly Definitions de Editor y del Sample ahora referencian por GUID en vez de por nombre
    - Licencia MIT, con su archivo LICENSE

### Fixes

    - Se declaran las dependencias que ya usaba el código: 'com.unity.mathematics', 'com.unity.nuget.newtonsoft-json' y 'com.unity.ugui'
    - El icono de 'NonDrawingGraphic' tenía el mismo GUID que el del paquete 'extensions.unity.nondrawinggraphic', lo que provocaba un conflicto de GUID al tener ambos instalados
    - Eliminado un .meta huérfano de una carpeta vacía ('Runtime/PopupField')

## 1.3.2

    - Se ha editado 'Timer' para exponer más variables y poder sumar o restar duración al temporizador

## 1.3.1

    - Se ha añadido una extensión para comprobar si un struct es 'null'

## 1.3.0
    
    - Se ha modificado 'Singleton.cs' para que no cree un objeto si no se ha encontrado, en vez de eso lanza un 'Exception'

## 1.2.2

    - Se han añadido dos extensiones, 'Contains' y 'RecursiveFindChild'

## 1.2.1

    - Se ha actualizado 'SafeArea.cs'

## 1.2.0

    - Se ha añadido NonDrawingGraphic, una imagen que no dibuja ningún sprite, solo capta raycasts. Perfecto para botones
    - Se ha añadido un nuevo tipo de Singleton para Clases que no heredan de nada
    - Se ha añadido un diccionario serializable

## 1.1.3

    - Se ha añadido un CHANGELOG.md
    - StateMachine: Se ha eliminado una comprobación que evitaba cambiar de estado en el caso de que se intentara con la misma key

## 1.0.0

    - Primera versión