# Nightmare Update

La rama parte de Hotel Umbral en `e3242d3`. El trabajo usa el mismo repositorio desde un directorio Git separado para conservar las modificaciones locales de Unity del proyecto que se usó para jugar la versión anterior. Se actualizó la escena existente `GameScene` con `NightmareAuthoring.Upgrade` y se reutilizaron el elevador, la geometría modular, el rig XR, los interactuables y las escenas de resultado.

| Entrega | Qué cambia | Archivos principales |
| --- | --- | --- |
| 1. Oscuridad | Iluminación fría de menor alcance, dos lámparas averiadas de inicio, niebla, manchas, balizas bajas, habitaciones oscuras y zonas rojas. Las placas mantienen texto legible durante un corte. | `GameScene.unity`, `NightmareLighting.cs`, `NightmareStain.shader`, `Assets/HotelUmbral/Nightmare` |
| 2. Amenaza y tensión | Director con cinco estados e histéresis. La criatura entra en juego antes del primer sello, mantiene la persecución con NavMesh y acelera en embestidas cortas con señal sonora. | `HorrorTensionDirector.cs`, `Creature.cs`, `NightmareProfile.cs` |
| 3. Atmósfera y audio | Pasos y voces direccionales, golpes tras las paredes, lámparas que fallan, caída de cuadro, portazo, maleta desplazada, rotura y apariciones. El director elige por proximidad y fase, con pausas entre eventos. | `HorrorEventDirector.cs`, `JumpscareTrigger.cs`, `NightmareSound.cs`, `NightmareAuthoring.Events.cs`, `Nightmare/Sounds` |
| 4. Acertijo y salida | Los cuatro dígitos siguen en sus soportes. Mantener la vista en un sello próximo lo registra y dos sellos emiten ruido. El progreso y la estancia en las habitaciones incrementan la presión. Completar los sellos o empezar a teclear activa el clímax. | `GameCodeManager.cs`, `Keypad.cs`, `HorrorTensionDirector.cs`, `NightmareTelemetry.cs` |
| 5. Captura y dificultad | Preparación de ataque, avance del modelo, impacto, háptica y fundido. El modo de sustos suaves reduce cambios de luz y sonido. Normal, Pesadilla y Terror extremo se seleccionan en la cabina. | `CreatureAttackSequence.cs`, `HotelOptions.cs`, `NightmareProfile.cs` |

Se eligieron balizas fijas de baja intensidad y placas contrastadas para orientar al jugador con las manos libres. La niebla usa el renderizador URP existente. Las luces averiadas y la baliza de persecución se deshabilitan al apagarse para ahorrar ranuras de luces adicionales. Los tres perfiles reutilizan los límites de luces de URP y la misma ruta de colisiones. La criatura conserva tres oportunidades en cada perfil. Un repelido retrasa la próxima embestida y la criatura puede seguir acercándose a velocidad normal tras recuperarse.

## Jugar y editar

Abre el proyecto con Unity `6000.3.19f1`, carga `Assets/Scenes/GameScene.unity` y pulsa **Play** en el Editor. En la cabina pulsa el botón inferior para alternar el perfil. **Pesadilla** es el valor inicial. Pulsa **Play / Jugar** para comenzar la partida. En Windows sin visor usa WASD y ratón, pulsa E o clic izquierdo para interactuar, E para soltar y clic derecho para lanzar. Esc libera el cursor.

Los perfiles se editan en `Assets/HotelUmbral/Nightmare`. Los parámetros principales gobiernan velocidad, ráfagas, descanso, reacción, duración del repelido, demora por habitación, intervalo de eventos, luz, mezcla y ayudas escritas. `Nightmare Update` contiene los sistemas añadidos a la escena. El menú **Hotel Umbral → Apply Nightmare Update** reaplica la puesta en escena y regenera sus recursos. Guarda primero el trabajo de edición manual que quieras conservar. Si regeneras Hotel Umbral desde su comando original, aplica Nightmare después.

## Pruebas y mediciones

| Cámara de comparación | Hotel Umbral | Nightmare | Reducción |
| --- | ---: | ---: | ---: |
| Pasillo al salir | 50,35 | 16,20 | 67,8 % |
| Sello de la habitación 104 | 36,18 | 13,84 | 61,8 % |
| Persiana y teclado | 34,67 | 19,78 | 42,9 % |

Valores de luminancia de píxel codificada en sRGB de 0 a 255, muestreados cada cuatro píxeles en capturas Direct3D 11 con la misma cámara y resolución 1280×720. El número de la habitación y el teclado conservan contraste visual en las capturas. Los 13 WAV originales suman 27,1 s y su pico PCM es de 0,701 de la escala digital. Esa comprobación mide los archivos fuente.

| Hotel Umbral | Nightmare Update |
| --- | --- |
| ![Pasillo de Hotel Umbral](NightmareEvidence/Pasillo-Hotel-Umbral.png) | ![Pasillo de Nightmare Update](NightmareEvidence/Pasillo-Nightmare.png) |

`HotelSmoke.Run` cubre las tres escenas, viaje, botones, agarre XRI simulado, las cinco roturas físicas, navegación, oclusión, las tres miradas, teclado, victoria, captura y reinicio. Incluye un recorrido adicional que mueve el CharacterController a velocidad de escritorio, registra los cuatro sellos mediante mirada y utiliza las defensas en el teclado. Las rutas son automatizadas y los resultados corresponden a esta máquina.

En el recorrido inicial la criatura capturó al jugador a los 30,0 s mientras tecleaba, con dos miradas aún disponibles. Ese ensayo motivó un intervalo mínimo antes de la siguiente embestida tras repelerla. La ruta posterior usó las miradas restantes en la salida y registró una victoria.

| Recorrido automatizado Pesadilla | Medición |
| --- | ---: |
| Duración de la victoria, desde Play | 35,7 s |
| Primer encuentro desde Play | 10,7 s |
| Primer encuentro desde la entrada al pasillo | 1,8 s |
| Persecuciones registradas | 5 |
| Sellos registrados y miradas consumidas | 4 y 3 |
| Tiempo dedicado al teclado | 4,7 s |

Los eventos observados incluyeron caída de cuadro, apagón y respiración detrás del jugador. La segunda victoria tomó 35,8 s y activó pasos detrás, apagón y rotura de vidrio. También pasó la prueba que cierra una puerta de habitación, bloquea la ruta NavMesh y verifica que puede reabrirse.

La telemetría se guarda como JSON en `Builds/NightmareTelemetry` en el Editor. En un ejecutable con `-nightmareTelemetry`, se guarda en `Application.persistentDataPath/NightmareTelemetry`. Cada sesión incluye duración desde Play, primer encuentro, persecuciones, tiempos entre sellos, miradas, tiempo desde la primera entrada en el teclado, desenlace y eventos. Los tiempos usan segundos del juego. Los archivos se quedan en disco local y registran índices de sellos y acciones del teclado.

```powershell
$unity = 'C:/Program Files/Unity/Hub/Editor/6000.3.19f1/Editor/Unity.exe'
$project = (Get-Location).Path
New-Item -ItemType Directory -Path Logs -Force | Out-Null
& $unity -batchmode --burst-disable-compilation -projectPath $project -executeMethod HotelSmoke.Run -logFile "$project/Logs/NightmareSmoke.log" | Out-Host
& $unity -batchmode -nographics -quit --burst-disable-compilation -projectPath $project -executeMethod HotelAuthoring.BuildWindows -logFile "$project/Logs/NightmareBuild.log" | Out-Host
```

La compilación Windows x64 con Mono terminó con cero avisos y se comprobó que el ejecutable arranca con Direct3D 11. Los registros de esta máquina están en `Logs/NightmareSmoke.log`, `Logs/NightmareBuild.log` y `Logs/NightmarePlayer.log`.

La máquina de validación usa una RTX 3060 y carece de un runtime OpenXR activo. Las pruebas de visor físico, mezcla binaural, comodidad, tiempos de GPU y Android quedan pendientes. El sistema bloqueó una DLL de Burst JIT generada en este directorio, por lo que se usó el parámetro de batch para deshabilitar Burst en esta ejecución. La opción de proyecto se conserva.
