# Terminal de comandos

Desarrollo de una consola de comandos in-game modular o reutilizable para la mayoría de proyectos, con el objetivo de atender las necesidades de testeo y debugging en el desarrollo de videojuegos.

Principales acciones:
- Procesar cadenas de texto
- Separar las cadenas por comandos y parametros
- Ejecutar dichos comandos
- Dar feedback de lo ocurrido, tanto si el comando fue exitoso como si no
- Tener un historial de comandos que permita autocompletar 

 ### Casos de uso

 **1. Abrir/Cerrar consola en cualquier momento del juego con una tecla**

    Mediante la tecla "|" la terminal deberá abrirse. Con esto se bloquean los inputs del jugador para recibir todo hacia la terminal. Con la misma tecla se deberá cerrar y devolver el foco al juego. 

 **2. Ingresar texto y poder enviarlo**

    El usuario ingresa un comando + parametros de personalizacion. La consola recibe dicha cadena y la procesa. 

 **3. Visualizar el registro log de los comandos hechos**

    La terminal deberá informar con un mensaje si la acción fue exitosa (verde) o si se encontró un error (rojo).

 **4. Recibir feedback y lista de comandos disponibles**

    En caso de ser un comando inexistente o tener parametros incorrectos, la terminal deberá imprimir una recomendación o la lista de parametros disponibles para ese comando, o la lista de comandos en general.

 **5. Navegar en el historial de comandos** 

    Mediante las flechas arriba y abajo, el campo de entrada de texto se autocompletará con el historial de comandos hechos.

 ### Arquitectura y flujos de comunicación
 
 - Terminal de comandos:  
    - Interfaz de usuario (UI)
    - Procesador de texto
    - Catalogo de comandos

 - Scripts externos (Procesadores de acciones del juego: enemigos, objetos, etc)

 ### Acciones o tipos de comandos 

 - Manipulacion del tiempo
 - Debugging visual y del entorno.
   - Encender las navmesh y hitboxes.
 - Manipulación de estados de la IA.
 - Economía y progresión. 

 ### Futuras Actualizaciones (Backlog)
 - Comandos de Economía y progresión.
 - Dibujado visual de las rutas del NavMesh (Path visualization).
 - Desvinculación de la cámara (Freecam).