# Hotel Umbral

| Entrega | Implementación | Ubicación |
| --- | --- | --- |
| 1. Arquitectura y ascensor | Galería con dos habitaciones visitables, cabina con dos hojas independientes, indicador de pisos, viaje, llegada y salida continua al pasillo | `GameScene`, `HotelAuthoring.Architecture.cs`, `ElevatorSequence.cs`, `Assets/HotelUmbral/Prefabs` |
| 2. Dirección visual | Roble, papel estampado, terrazo, alfombra, señalética, luminarias, cuadros, maletas y utilería. Luz localizada, niebla y polvo con densidad limitada | `HotelAuthoring.Props.cs`, `HotelMedia.cs`, `Assets/HotelUmbral` |
| 3. Interacción | Selección, agarre y suelta XRI, puerta interior, interruptor, nota y llave manipulables. Jarrón, botella, vaso, marco y remate de porcelana con fragmentos, partículas y limpieza | `HotelButton.cs`, `HotelCarryable.cs`, `HotelSwingDoor.cs`, `BreakableObject.cs` |
| 4. Criatura y atmósfera | Navegación horneada, línea de visión, tres miradas, acecho, persecución, retirada, recuperación y reacción al ruido. Director ambiental con tiempos y ubicaciones configurables | `Creature.cs`, `HotelNavigation.cs`, `HotelNoise.cs`, `HorrorEventDirector.cs` |
| 5. Captura | Desplazamiento corto del modelo, animación existente de GhostGirl, sonido, luz, háptica y fundido estereoscópico. El ajuste de sustos reduce movimiento, volumen y efectos | `CreatureAttackSequence.cs`, `HotelFade.shader`, `JumpscareTrigger.cs` |
| 6. Flujo y accesibilidad | Cuatro sellos con orden visible, código nuevo, respuesta del teclado, desbloqueo, pantallas de resultado y reinicio. Audio y ajuste de sustos persistentes | `GameCodeManager.cs`, `Keypad.cs`, `DoorController.cs`, `WinTrigger.cs`, `HotelOptions.cs`, `GameOver`, `WinScene` |

Se guardaron 67 mallas, una NavMesh, 43 prefabs, 26 materiales, 7 texturas y 18 WAV en `Assets/HotelUmbral`. Las escenas contienen las referencias de juego. La criatura, sus animaciones, el rig, la tipografía Inter y los grupos de mezcla reutilizan recursos del proyecto original.

## Editar

- En la jerarquía de `GameScene`, abre **Hotel Umbral → Arquitectura modular**. Los módulos son instancias de prefabs con pivotes locales y colliders. Edita el prefab para cambiar todas sus instancias. Usa las instancias para componer un tramo concreto.
- Tras cambiar colisiones estáticas, revisa la lista **Static Collision** de `HotelNavigation` y ejecuta **Hotel Umbral → Rebake Gallery Navigation**. Las hojas móviles se controlan fuera de ese horneado. Si agregas suelo, enlaza su collider a un `TeleportationArea` y añade el área a la lista del ascensor si debe bloquearse durante la introducción.
- En **Registro de seguridad · pistas**, mueve las placas junto con sus anclajes. El arreglo **Number Spawn Points** fija el orden de lectura y del teclado. Conserva cuatro anclajes y mantén sus identificadores visibles.
- Duplica un prop rompible para crear una variante. Conserva un `XRGrabInteractable`, el `Rigidbody` con detección continua, y un prefab de fragmentos con colliders y cuerpos independientes. El umbral de impacto, radio de ruido y tiempo de limpieza se ajustan en `BreakableObject`.
- Ajusta la atenuación desde cada `AudioSource`. Los sonidos nuevos están en **Sounds** y se envían a **SFX** o **MX** del mezclador. Modifica los eventos, distancias y tiempos del director desde la escena.
- La regeneración completa por línea de comandos con `HotelAuthoring.Generate` reconstruye los recursos y recompone las tres escenas. Úsala en una copia de trabajo limpia cuando quieras regenerar el diseño procedural. Para editar el hotel entregado basta abrir la escena.

## Verificar y compilar

Ejecuta los siguientes comandos desde la raíz del proyecto. Ajusta la ruta del Editor si Unity Hub usa otra ubicación.

```powershell
$unity = 'C:/Program Files/Unity/Hub/Editor/6000.3.19f1/Editor/Unity.exe'
$project = (Get-Location).Path
New-Item -ItemType Directory -Path Logs -Force | Out-Null
& $unity -batchmode -projectPath $project -executeMethod HotelSmoke.Run -logFile "$project/Logs/HotelSmoke.log" | Out-Host
& $unity -batchmode -nographics -quit -projectPath $project -executeMethod HotelAuthoring.BuildWindows -logFile "$project/Logs/HotelBuild.log" | Out-Host
```

La prueba recorre las tres escenas de build y se ejecuta en Play Mode. Incluye selección y agarre por XRI simulado, apertura, navegación a las habitaciones, pistas, impactos físicos de las cinco variantes, ruido, límite de mirada, rechazo y aceptación del teclado, victoria, captura y reinicio. Devuelve un código distinto de cero ante un fallo. Con dispositivo gráfico genera PNG en `Builds/Preview`.

La ejecución en Unity `6000.3.19f1` pasó las comprobaciones. Se revisaron capturas de cabina, pasillo, habitación, salida y criatura. Unity Search emitió una excepción de indexación del Editor en modo batch durante el arranque. El verificador descarta ese caso de UnityEditor y falla ante las excepciones del juego.

El comando de Windows genera un ejecutable x64 con Mono y restaura el backend del proyecto al terminar. El módulo IL2CPP de Windows no está instalado en el Editor disponible.

### Visor y rendimiento

El entorno usa colliders simples, geometría modular con agrupación estática, texturas procedurales pequeñas, atenuación local, presupuestos de partículas y limpieza de fragmentos. La configuración URP para Android conserva 4× MSAA, hasta dos luces adicionales por objeto y sombras adicionales desactivadas. La configuración de Windows admite sombras adicionales con un atlas de 1024.

El equipo de esta entrega tiene una RTX 3060 y carece de un runtime OpenXR activo y de Android Build Support para este Editor. Quedan pendientes el APK y las mediciones de CPU y GPU con visor. En el visor hay que verificar seguimiento, rayos y toque físico, locomoción, háptica, mezcla binaural y comodidad. Registra tiempos durante cabina, habitaciones, rotura y captura según la frecuencia del visor. El presupuesto es de 13,9 ms a 72 Hz o 11,1 ms a 90 Hz.
