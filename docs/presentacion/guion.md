# Guion del video — Hola Mundo CI/CD con Jenkins

Video de **hasta 20 minutos**. Integrantes: **Osvaldo Agustin Lago** (LU 57725) y **Juan Agustin Acosta** (LU 57851). En el guion figuran como **A** y **B**: definan quién es cada uno.

- **Parte 1, teoría:** la animación (`docs/presentacion/animacion.html`), que se avanza con la barra espaciadora.
- **Parte 2, práctica:** pantalla real (VS Code, terminal, Jenkins y GitHub).

| Parte | Quién | Contenido | Tiempo aprox. |
|---|---|---|---|
| Teoría | A | Escenas 1–3: el problema, Jenkins por dentro, agentes y Docker | 5:30 |
| Teoría | B | Escenas 4–7: pipeline como código, el viaje de un commit, el pipeline protege, escalar | 6:00 |
| Práctica | A | El código y el acto 1 (camino feliz) | 3:30 |
| Práctica | B | El acto 2 (el pipeline protege) y el cierre | 3:00 |
| **Total** | | | **~18 min** |

Cómo leer este guion:
- **[Espacio]** significa apretar la barra espaciadora para pasar al siguiente momento de la animación.
- El texto citado es lo que se dice. No hace falta leerlo literal: es una base para decirlo con palabras propias.
- No lean el panel de la animación en voz alta palabra por palabra. Conviene contarlo y dejar que el panel lo refuerce.

---

## Antes de grabar

**Checklist:**
- [ ] Docker Desktop corriendo.
- [ ] Desde `infra/`, levantar Jenkins: `docker compose up -d`.
- [ ] Levantar el túnel: `docker compose --profile tunel up -d`.
- [ ] El inspector del túnel responde en http://localhost:4040.
- [ ] El último build está en verde y `curl.exe http://localhost:8081` responde. Anotar la versión: la llamamos **N**.
- [ ] Las imágenes base ya están descargadas (`mcr.microsoft.com/dotnet/sdk:10.0` y `mcr.microsoft.com/dotnet/aspnet:10.0`), para que el build no se demore bajándolas.
- [ ] Si en esa PC corre `argos-platform`, frenarlo antes: ocupa el puerto 8080.
- [ ] `git status` limpio y `git pull` hecho.

**Para grabar:**
- Usar OBS Studio en 1080p. Grabar cada parte por separado y después unirlas.
- **Animación:**
  - abrirla en el navegador;
  - **F** para pantalla completa y **H** para ocultar la ayuda;
  - con las teclas **1 a 7** se salta a una escena: sirve para grabar escena por escena.
- **Pantalla real:**
  - zoom del navegador y de VS Code al 125–150%, para que se lea en el video;
  - la terminal en PowerShell, con letra grande.
- Las esperas de los builds se pueden acelerar al editar, pero hay que decirlo en voz ("aceleramos la espera"), para que quede claro que la demo es real.

---

# Parte 1 — Teoría (animación)

## Escena 1 · El problema — **A** (~2:00)

**Momento 1 · Portada.** *En pantalla: "Hola Mundo CI/CD con Jenkins". La cámara gira alrededor del escritorio.*
> "Hola, somos A y B. En este trabajo vamos a mostrar Jenkins, una de las herramientas más usadas para automatizar la integración y la entrega continua, lo que se conoce como CI/CD. Primero vamos con la teoría, apoyados en esta animación, y después lo vemos funcionando de verdad en nuestra PC."

**[Espacio] Momento 2 · El commit.** *En pantalla: el desarrollador tipea y aparece el cubo azul del commit.*
> "Todo empieza con algo cotidiano: un desarrollador cambia el código (en nuestro caso, el saludo de una API) y lo sube al repositorio con un commit y un push."

**[Espacio] Momento 3 · Todo a mano.** *En pantalla: caen cinco bloques naranjas, que se apilan y se tambalean.*
> "¿Y después qué? Sin automatización, alguien tiene que compilar, correr los tests, empaquetar la aplicación, desplegarla y verificar que ande. Cada vez, para cada cambio."

**[Espacio] Momento 4 · Lento y frágil.** *En pantalla: "Probar" se pone rojo y la pila se cae.*
> "Esto es lento y, sobre todo, frágil. Alcanza con que un día alguien se saltee las pruebas para que el error les llegue a los usuarios."

**[Espacio] Momento 5 · La línea de montaje.** *En pantalla: los bloques se ordenan sobre la cinta y el commit la recorre.*
> "La idea de CI/CD es convertir esos pasos en una línea de montaje: una máquina los ejecuta siempre en el mismo orden y de la misma forma, cada vez que hay un cambio. Esa línea se llama **pipeline**. Fíjense que ahora las tareas tienen nombre: Build, Test, Docker Image, Deploy y Smoke Test. Son exactamente las etapas de nuestro proyecto."

**[Espacio] Momento 6 · CI y CD.** *En pantalla: las llaves CI y CD sobre la cinta, y el panel.*
> "El pipeline tiene dos mitades. **CI**, integración continua: cada cambio se compila y se prueba automáticamente, y así un error aparece en minutos y no semanas después. **CD**, entrega o despliegue continuo: lo que pasó las pruebas se empaqueta y se despliega solo. Lo que viaja por la cinta es un **artefacto** (para nosotros, una imagen Docker) que se construye una sola vez y se despliega igual en cualquier entorno. Y quien mueve esta cinta es Jenkins."

## Escena 2 · Jenkins por dentro — **A** (~1:45)

**[Espacio] Momento 1 · ¿Qué es Jenkins?** *En pantalla: la torre de control sube desde el piso.*
> "Jenkins es un servidor de automatización **open source**, escrito en Java. Nació en 2004 con el nombre Hudson, y desde 2011 se llama Jenkins. La gran diferencia con Azure DevOps o GitHub Actions es que Jenkins es **self-hosted**: lo instalás y lo operás vos. En nuestro caso, corre en un contenedor Docker en la PC."

**[Espacio] Momento 2 · El controller.** *En pantalla: aparece el disco `JENKINS_HOME`, conectado a la torre.*
> "El corazón de Jenkins es el **controller**. Sirve la interfaz web, agenda los builds y decide quién los ejecuta. Todo su estado (los jobs, el historial, los plugins, los usuarios) vive en una carpeta, `JENKINS_HOME`. Nosotros la pusimos en un volumen de Docker, así podemos borrar y recrear el contenedor sin perder nada."

**[Espacio] Momento 3 · Plugins.** *En pantalla: seis plugins se enganchan en el anillo que gira.*
> "El núcleo de Jenkins es chico: casi todo lo que hace viene de **plugins**. Hay más de mil ochocientos, para Git, GitHub, Docker, reportes de tests… Nosotros no los instalamos a mano: están listados en un archivo, `plugins.txt`, y se instalan al construir la imagen de Jenkins. Son plugins como código. La contra es que cada plugin es una dependencia más que hay que mantener y actualizar."

**[Espacio] Momento 4 · Jobs y builds.** *En pantalla: la tarjeta del job va largando los builds #1 a #5.*
> "Dos palabras que vamos a usar todo el tiempo: job y build. El **job** es la receta: qué hacer, con qué código y cuándo. Cada vez que se ejecuta es un **build**, con su número, su consola y su resultado: verde si salió bien, rojo si falló. Un detalle de nuestro proyecto: el número de build es la versión de la aplicación. Para quien viene de Azure DevOps: el job es el pipeline, y el build es el run."

## Escena 3 · Agentes y Docker — **A** (~1:45)

**[Espacio] Momento 1 · Agentes.** *En pantalla: dos servidores reciben trabajo del controller.*
> "¿Quién hace el trabajo? El controller coordina, pero los que ejecutan son los **agentes**. Cada agente tiene **executors**, que son los lugares para correr trabajos en paralelo. Si hacen falta más builds a la vez, u otro sistema operativo, se suman agentes: así escala Jenkins. En Azure DevOps es la misma idea, con los agent pools."

**[Espacio] Momento 2 · Agentes efímeros.** *En pantalla: los contenedores violetas de .NET nacen, trabajan y desaparecen.*
> "En nuestro proyecto no hay agentes fijos: usamos **agentes efímeros**. Cada etapa de .NET pide un contenedor con el SDK, que nace, compila o prueba, y se borra. Gracias a eso, Jenkins no necesita tener .NET instalado, y cada build arranca de un entorno limpio. Los contenedores comparten el workspace, así lo que compiló Build es exactamente lo que prueba Test."

**[Espacio] Momento 3 · Imagen y contenedor.** *En pantalla: la imagen se arma capa por capa y estampa tres contenedores.*
> "Ya que hablamos de contenedores, dos ideas de Docker. Una **imagen** es una plantilla inmutable, armada en capas: el sistema base, el runtime de .NET y nuestra aplicación. Un **contenedor** es esa imagen en ejecución. La imagen es el molde, y los contenedores son las piezas. Y como las capas se cachean, si solo cambia la app se reconstruye solo la última."

**[Espacio] Momento 4 · El socket.** *En pantalla: Jenkins conectado al Docker de la PC, con el aviso de riesgo latiendo.*
> "Un último detalle técnico. Jenkins corre dentro de un contenedor, pero lanza otros contenedores. ¿Cómo? Usa el Docker de la PC a través de un socket montado, `docker.sock`. Por eso los contenedores nacen al lado de Jenkins, no adentro. Esto tiene un riesgo importante: quien usa ese socket controla todo el Docker de la máquina, que es casi como ser administrador. Para un laboratorio está bien; en una empresa se usan agentes aislados."
>
> "B, te dejo la pregunta que sigue: ¿cómo sabe Jenkins cuándo tiene que arrancar?"

## Escena 4 · Pipeline como código — **B** (~2:00)

**[Espacio] Momento 1 · El Jenkinsfile.** *En pantalla: el editor resalta cada parte del archivo.*
> "Todo lo que vimos está definido en un archivo: el **Jenkinsfile**. Vive en el repositorio, junto al código, así que se versiona y se revisa en un pull request como cualquier otro cambio. La estructura es declarativa: un pipeline, un agente, los stages, y dentro de cada stage los steps, que son los comandos. Al final, el bloque `post` dice qué hacer según el resultado. Y arriba están los **triggers**: cuándo se dispara."

**[Espacio] Momento 2 · Polling.** *En pantalla: la pelotita naranja pregunta "¿hay cambios?" y vuelve con "no".*
> "Hay dos formas de disparar un build. La primera es el **polling**: Jenkins le pregunta a GitHub cada dos minutos si hay commits nuevos. Es simple y funciona siempre, pero tiene demora, y la mayoría de las veces la respuesta es 'no'."

**[Espacio] Momento 3 · Webhook.** *En pantalla: el commit cae en GitHub y el sobre del webhook vuela a Jenkins.*
> "La segunda es al revés: un **webhook**. Cuando llega un push, GitHub le avisa a Jenkins en el momento, y el build arranca en segundos. En el Jenkinsfile se declara con `githubPush()`. Pero tiene una condición: GitHub tiene que poder llegar a Jenkins por internet."

**[Espacio] Momento 4 · NAT.** *En pantalla: sube el muro y el webhook rebota, mientras el polling pasa por arriba.*
> "Y ahí está el problema. Nuestra PC está en una red privada, detrás de un router que hace **NAT**. Desde adentro podemos salir a internet (por eso el polling funciona), pero desde afuera nadie puede iniciar una conexión hacia la PC. El webhook choca contra el router. En Azure DevOps este problema no existe, porque el servicio está en la nube, con una dirección pública."

**[Espacio] Momento 5 · Túnel.** *En pantalla: el túnel crece por arriba del muro y el webhook entra por ahí.*
> "La solución es un **túnel** con ngrok. ngrok abre una conexión saliente desde la PC hacia su nube y la mantiene abierta. GitHub manda el webhook a una URL pública fija, y ngrok lo reenvía por el túnel hasta Jenkins. Además, le pusimos una política: solo deja pasar la ruta del webhook, así la interfaz de Jenkins no queda expuesta. Lo que quedó pendiente para un entorno real es firmar el webhook con un *secret*."

## Escena 5 · El viaje de un commit — **B** (~2:00)

**[Espacio] Momento 1 · La arquitectura.** *En pantalla: vista general de todas las piezas.*
> "Juntemos todo. Afuera está GitHub. Adentro de nuestra PC: el túnel, Jenkins, los contenedores efímeros, la imagen y el entorno de staging, en el puerto 8081. Sigamos un commit de punta a punta."

**[Espacio] Momento 2 · Push.**
> "Primero, el `git push`: el cambio sale de la PC hacia GitHub. Salir no es problema."

**[Espacio] Momento 3 · Webhook.**
> "GitHub manda el webhook, que entra por el túnel. Jenkins lo recibe, descarga el código y arranca el build. Abajo ven la barra de stages, igual a la que muestra Jenkins."

**[Espacio] Momento 4 · Build y Test.**
> "Build y Test corren en un contenedor efímero con el SDK de .NET. Compila, pasan los tres tests, se publica el reporte… y el contenedor se borra."

**[Espacio] Momento 5 · Docker Image.**
> "Con los tests en verde se construye la imagen Docker, etiquetada con el número de build: `hola-mundo:17`. Ese es el artefacto, lo mismo que iría a producción."

**[Espacio] Momento 6 · Deploy.**
> "Deploy: se borra el contenedor de staging viejo, que tenía la versión 16, y se levanta uno nuevo con la imagen recién construida."

**[Espacio] Momento 7 · Smoke test.**
> "Y el último control: el **smoke test**. Jenkins le hace un `curl` a staging y verifica dos cosas: que responda, y que responda con la versión 17. Si siguiera el contenedor viejo, también contestaría, pero con la versión equivocada. Del push al deploy, sin tocar nada: eso es CI/CD."

## Escena 6 · El pipeline protege — **B** (~1:15)

**[Espacio] Momento 1 · Alguien rompe algo.** *En pantalla: el commit naranja con el 🐞.*
> "Ahora lo más importante: ¿qué pasa cuando alguien rompe algo? Un cambio con un bug llega al repositorio exactamente igual que cualquier otro."

**[Espacio] Momento 2 · El test falla.** *En pantalla: el contenedor se pone rojo y baja la barrera.*
> "Pero en la etapa de Test, un test falla: esperaba 'dev' y recibió 'desarrollo'. El pipeline se corta acá, y las etapas siguientes ni se ejecutan."

**[Espacio] Momento 3 · Staging intacto.**
> "Y staging sigue intacto, con la versión 17, la última que pasó todas las pruebas. Eso es un **quality gate**. El equipo se entera por el build en rojo, con el reporte del test que falló, y el usuario nunca vio el error."

**[Espacio] Momento 4 · Se corrige con otro commit.**
> "¿Cómo se arregla? Con otro commit. Hacemos `git revert`, el pipeline vuelve a pasar y despliega solo. Staging salta de la versión 17 a la 19: el build 18, el roto, nunca llegó a desplegarse."

## Escena 7 · Escalar y comparar — **B** (~1:30)

**[Espacio] Momento 1 · Del laboratorio a una empresa.**
> "Lo que armamos es chico, pero tiene la forma de un sistema real."

**[Espacio] Momento 2 · Escalar.** *En pantalla: cada pieza se transforma.*
> "Y escalarlo no es un salto grande:
> - la imagen local pasa a un **registry**, con un `docker push` más;
> - el contenedor de staging pasa a **Kubernetes** o App Service, y cambia solo la etapa de deploy;
> - el túnel desaparece cuando Jenkins tiene una **URL pública**, y el pipeline no cambia;
> - un solo nodo pasa a ser **controller más agentes**;
> - y la configuración por interfaz pasa a ser código, con **JCasC**."

**[Espacio] Momento 3 · Jenkins, ADO o GitHub Actions.**
> "¿Entonces, Jenkins, Azure DevOps o GitHub Actions? Jenkins conviene cuando hace falta infraestructura propia, requisitos especiales o integraciones a medida; su costo es que hay que operarlo y mantenerlo. Azure DevOps y GitHub Actions resuelven la infraestructura, a cambio de menos control. Pero los conceptos son los mismos: pipeline como código, agentes, etapas y disparadores. Quien entiende uno, entiende los otros."

**[Espacio] Momento 4 · Ahora, la práctica.** *En pantalla: confeti.*
> "Basta de teoría: ahora vamos a la práctica."

---

# Parte 2 — Práctica (pantalla real)

## P1 · El código — **A** (~1:15)

*En pantalla: VS Code con el repo abierto.*

1. **`Jenkinsfile`.** Mostrar `triggers`, el `agent { docker { ... } }` del stage Build, el `junit` del stage Test y los stages Deploy y Smoke Test.
   > "Este es el Jenkinsfile real. Acá están los dos triggers, webhook y polling. El stage Build pide su contenedor con el SDK de .NET. Test publica los resultados con `junit`. Y al final, el deploy a staging y el smoke test, que verifica la versión."
2. **`infra/docker-compose.yml`.** Mostrar el volumen `jenkins_home`, el socket montado y el servicio `tunel`.
   > "Así levantamos Jenkins: con un volumen para `JENKINS_HOME`, el socket de Docker y el túnel de ngrok como un servicio más."
3. **`Dockerfile`.** Mostrar las dos etapas, `sdk` y `aspnet`.
   > "Y el Dockerfile de la app es multi-stage: compila con el SDK, pero la imagen final lleva solo el runtime."

## P2 · Acto 1: el camino feliz — **A** (~2:15)

1. **Mostrar staging antes del cambio:**
   ```powershell
   curl.exe http://localhost:8081
   ```
   > "Staging responde con la versión N."
2. **Hacer el cambio.** En `src/HolaMundo.Api/Saludo.cs`, cambiar `MensajeActual` por un mensaje nuevo, por ejemplo `"Hola mundo desde el video"`, y **guardar el archivo**.
3. **Subirlo:**
   ```powershell
   git diff
   git commit -am "Demo: cambio el saludo"
   git push
   ```
   > "Hacemos el push… y no tocamos nada más."
4. **Jenkins.** Ya debería haber un build nuevo. Abrirlo y mostrar la causa, **"Started by GitHub push"**.
   > "En segundos arrancó el build, disparado por el webhook."
   - Opcional: en GitHub, **Settings → Webhooks → Recent Deliveries**, mostrar la entrega en verde.
5. **Vista de stages.** Esperar a que termine (acelerado al editar) y mostrar todo en verde. Abrir el **reporte de tests**: 3 de 3.
6. **Mostrar staging después del cambio:**
   ```powershell
   curl.exe http://localhost:8081
   ```
   > "El mensaje nuevo, con la versión N+1. Del push a staging, solo."
   >
   > "B, ahora rompelo."

## P3 · Acto 2: el pipeline protege — **B** (~2:15)

1. **Meter el bug.** En `src/HolaMundo.Api/Program.cs`, cambiar `?? "dev"` por `?? "desarrollo"` y **guardar**.
   > "Alguien cambia un valor por defecto, sin darse cuenta de que hay un test que lo controla."
   ```powershell
   git diff
   git commit -am "Demo: cambio el valor por defecto (rompe un test)"
   git push
   ```
2. **Jenkins.** El build arranca solo y queda **rojo en Test**. Mostrar que Docker Image, Deploy y Smoke Test no se ejecutaron. Abrir el reporte del test que falló: `Expected: "dev"`, `Actual: "desarrollo"`.
   > "El test lo detectó. No se construyó ninguna imagen ni se desplegó nada."
3. **Mostrar staging:**
   ```powershell
   curl.exe http://localhost:8081
   ```
   > "Staging sigue con la versión N+1, la última buena. El usuario nunca vio el error."
4. **Corregirlo con `git revert`:**
   ```powershell
   git revert HEAD --no-edit
   git push
   ```
   > "Lo corregimos con otro commit: un revert."
   - Mostrar el build nuevo en verde y correr otra vez `curl.exe http://localhost:8081`: devuelve la versión N+3. El build N+2 fue el rojo y nunca se desplegó.

## P4 · Cierre — **B** (~0:45)

*En pantalla: unos segundos de la portada del informe y del anexo de capturas.*
> "Para cerrar: en el informe está la teoría completa y el análisis crítico, con los riesgos que mencionamos (el socket, el túnel y el webhook sin firma). En el anexo documentamos cada paso del armado con sus capturas y los problemas reales que fuimos resolviendo: por ejemplo, un paso de tests que terminaba "bien" sin haber corrido ni un test, hasta que el reporte de JUnit lo dejó en evidencia.
>
> La conclusión: CI/CD no es una herramienta, es una forma de trabajar. Jenkins es una muy buena manera de aprenderla, porque te obliga a entender cada pieza. Gracias."

*Si el profesor no aprueba el anexo, cambiar esa frase por: "en el informe documentamos los problemas reales que fuimos resolviendo".*

---

## Si se pasan de tiempo

Estos recortes no rompen la historia. En orden, de lo que menos se pierde a lo que más:
1. Escena 7, momento 1 (es solo una transición): pasarlo sin hablar.
2. Escena 5, momentos 2 a 6: narrarlos en una sola frase corrida, sin pausas.
3. P1 (el código): mostrar solo el `Jenkinsfile`.
4. P3, paso 4 (el revert): mostrarlo acelerado, sin narrar.
