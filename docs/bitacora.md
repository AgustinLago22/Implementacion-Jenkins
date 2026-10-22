# Bitácora del proyecto

Registro paso a paso de lo que se hizo, las decisiones tomadas y los problemas encontrados. Alimenta la sección "Caso práctico" del informe.

> **Capturas:** no se versionan en el repo. Están en el Google Doc compartido del grupo, con su descripción. Las rutas `docs/capturas/pasoN-*.png` que aparecen abajo son los nombres que identifican cada figura en ese documento.

---

## Paso 0 — Preparación del repo (2026-10-20)
**Qué se hizo:** se creó el repo en GitHub (`AgustinLago22/Implementacion-Jenkins`) y la estructura mínima: `README.md`, `.gitignore` (.NET + Visual Studio), `.gitattributes`, `docs/bitacora.md` y `docs/capturas/`.
**Decisiones y por qué:**
- Un único repo con app, pipeline e infraestructura (*pipeline as code*): el `Jenkinsfile` se versiona junto al código que construye. En una empresa, `infra/` probablemente sería un repo aparte.
- `.gitattributes` con `* text=auto eol=lf`: los archivos se ejecutan en contenedores Linux, y los saltos de línea CRLF de Windows rompen los scripts. La regla queda en el repo y no depende de la configuración de Git de cada integrante (`core.autocrlf`).
- `.gitignore` excluye `bin/`, `obj/` y `TestResults/`: son salidas de compilación, y el pipeline siempre compila desde cero.
- `docs/capturas/.gitkeep`: Git no versiona carpetas vacías.
**Problemas y cómo se resolvieron:** el primer commit ("Readme agregado") solo contenía `CLAUDE.MD`, sin `README.md`. Se completó en el segundo commit.
**Evidencia guardada:** docs/capturas/paso0-repo-github.png
**Equivalente en ADO:** crear un repo en Azure Repos. El *pipeline as code* equivale a usar `azure-pipelines.yml` en lugar de los pipelines *Classic* configurados en la interfaz web.

---

## Paso 1 — Levantar Jenkins con Docker Compose (2026-10-20)
**Qué se hizo:** se escribió `infra/docker-compose.yml` con la imagen oficial `jenkins/jenkins:lts-jdk21` (Jenkins 2.580.1), el volumen nombrado `jenkins_home` y los puertos 8080 (UI) y 50000 (agentes inbound). Se completó el asistente inicial: desbloqueo con `initialAdminPassword`, plugins sugeridos y usuario admin. La Jenkins URL quedó en `http://localhost:8080/`.
**Decisiones y por qué:**
- Rama **LTS** en lugar de *weekly*: para una demo pesa más la estabilidad que tener lo último.
- `JENKINS_HOME` en un **volumen nombrado**: el contenedor es descartable y el estado del controller (jobs, builds, plugins, usuarios, credenciales) no. Se fijó `name: jenkins_home` para que Compose no le agregue el prefijo de la carpeta (`infra_`).
- `restart: unless-stopped`: Jenkins vuelve solo si se reinicia Docker Desktop.
- **Trabajo en equipo:** cada integrante corre su propio Jenkins. No se comparte el volumen: un volumen es local al Docker de cada PC, y sincronizarlo (OneDrive, un `.tar`) corrompe datos o filtra `secrets/`. Lo que se comparte es la configuración como código (Jenkinsfile, `plugins.txt`, JCasC).
**Problemas y cómo se resolvieron:**
1. **Docker Desktop no estaba corriendo** (`failed to connect to the docker API at npipe:////./pipe/dockerDesktopLinuxEngine`). Se inició Docker Desktop.
2. **Puerto 8080 ocupado** (`Bind for 0.0.0.0:8080 failed: port is already allocated`). Otro stack del autor (`argos-platform`) arranca solo con Docker Desktop por su política de `restart`, y su contenedor cAdvisor publica el 8080. Se frenó ese stack. Hay que repetirlo antes de cada demo.
3. **Contenedor sin puertos publicados** después del fallo anterior: Compose reutilizó el contenedor que había quedado a medio crear. Se resolvió con `docker compose up -d --force-recreate`, que recrea el contenedor sin tocar el volumen (la contraseña inicial siguió siendo la misma).
4. **Plugins en rojo en el asistente.** Causa raíz: el mirror `ftp-nyc.osuosl.org`, al que redirige `updates.jenkins.io`, fallaba de forma intermitente desde nuestra red (`SSLHandshakeException`, `Read timed out`, `Failed to connect ... after 21033 ms`). Falló una dependencia base (`commons-lang3-api`) y arrastró en cascada a Folders, Credentials, Git, Pipeline, etc. (`Plugin is missing: commons-lang3-api`). Se resolvió con "Retry" y luego un reinicio de Jenkins, que carga todos los plugins juntos y resuelve las dependencias: 92 plugins, 0 errores. **Lección:** instalar plugins desde la UI no es reproducible; motiva `plugins.txt` + `jenkins-plugin-cli` en el Paso 5.
5. (Menor) En Git Bash, `docker exec jenkins cat /var/...` convierte la ruta a `C:/Program Files/Git/var/...`. Se evita con `MSYS_NO_PATHCONV=1`, o usando PowerShell.
**Verificación:** `docker compose down` borró el contenedor y la red, pero no el volumen. Después del `up`, siguen el mismo usuario admin y la misma Jenkins URL, y no vuelve a aparecer el asistente.
**Evidencia guardada:** docs/capturas/paso1-unlock.png, docs/capturas/paso1-plugins-error.png, docs/capturas/paso1-dashboard.png, infra/docker-compose.yml
**Equivalente en ADO:** no hay equivalente directo. En ADO Services el servidor (el "controller") lo administra Microsoft: no se instala, no se actualiza y no se respalda. Lo más parecido es **Azure DevOps Server** (on-premises), donde sí hay que administrar el servidor. Los plugins de Jenkins equivalen a las extensiones del Marketplace de ADO, aunque en Jenkins hasta funciones básicas (Git, Pipeline) son plugins.

---

## Paso 2 — El "hola mundo" literal (2026-10-20)
**Qué se hizo:** se creó un job de tipo *Pipeline* (`test`) con un script declarativo escrito en la UI: un `stage('Saludo')` que hace `echo 'Hola mundo desde Jenkins'`. El build #1 terminó en verde y la consola muestra el mensaje.
**Decisiones y por qué:**
- **Pipeline declarativo** (`pipeline` / `agent` / `stages` / `stage` / `steps`) en lugar de *scripted*: tiene estructura fija, Jenkins lo valida antes de ejecutar y es el estilo recomendado hoy.
- `agent any`: el build corrió en el propio controller (*Built-In Node*, 2 executors), con workspace `/var/jenkins_home/workspace/test`. Sirve para el laboratorio, pero en producción no se recomienda correr builds en el controller; se retoma en el Paso 6 (contenedores) y en el 10c (agente separado).
- El script se escribió a propósito en la UI, para contrastarlo con el Paso 3: queda guardado solo en `jobs/<job>/config.xml` dentro de `JENKINS_HOME`, sin historial, sin revisión y sin forma de compartirlo.
**Problemas y cómo se resolvieron:** el primer intento falló con `Undefined section "stage"` y `Missing required section "stages"`. Causa: un typo, `stage {` en lugar de `stages {` en la línea 3. El build falló en la compilación, **sin ejecutar ningún step** (no aparece `Running on ...`): el declarativo valida la estructura completa antes de arrancar. Se corrigió en la UI. Esa corrección no quedó registrada en ningún lado, lo que ilustra el problema que resuelve el Paso 3. El job original (`hola-mundo`) se borró y se recreó como `test`.
**Evidencia guardada:** docs/capturas/paso2-error-sintaxis.png, docs/capturas/paso2-job-config.png, docs/capturas/paso2-stages.png, docs/capturas/paso2-consola.png
**Equivalente en ADO:** `pipeline {}` ≈ `azure-pipelines.yml`; `agent any` ≈ `pool:` (en ADO el pool lo provee Microsoft, en Jenkins son nodos propios); `stages/stage/steps` ≈ `stages/stage/steps`; job ≈ definición del pipeline; build #N ≈ run #N. Un error de sintaxis en el declarativo equivale al *"pipeline is invalid"* del YAML de ADO, que también falla antes de pedir un agente. Escribir el script en la UI equivale a un pipeline *Classic*.

---

## Paso 3 — Pipeline as code (Jenkinsfile desde el repo) (2026-10-20)
**Qué se hizo:** el pipeline pasó a un `Jenkinsfile` en la raíz del repo. Se creó el job `hola-mundo` de tipo *Pipeline script from SCM* (Git, `https://github.com/AgustinLago22/Implementacion-Jenkins`, rama `*/main`, script path `Jenkinsfile`). El job `test` del Paso 2 se dejó como ejemplo del "antes".
**Decisiones y por qué:**
- Job nuevo en lugar de reconfigurar `test`: quedan los dos modelos lado a lado (pipeline en la UI vs pipeline en el repo), lo que sirve para el informe.
- Sin credenciales: el repo es público. Con un repo privado haría falta un token guardado en el almacén de credenciales de Jenkins.
- Se agregó un stage `Info del build` que imprime `GIT_COMMIT` y `GIT_BRANCH` y hace `ls -la` del workspace: es la prueba visible del checkout implícito.
**Verificación:**
- Build #1: `Obtained Jenkinsfile from git ...`, `Checking out Revision 97e236d...`, `Hola mundo desde Jenkins - build #1`.
- Se cambió el `echo` en el `Jenkinsfile`, se commiteó y pusheó (`d429552`) y se lanzó Build Now **sin tocar la configuración del job**. El build #2 tomó la revisión nueva y mostró `Hola mundo versionado - build #2`.
**Problemas y cómo se resolvieron:** ninguno. Riesgo conocido: Jenkins propone `*/master` como rama por defecto; con un repo que usa `main`, el build falla con `couldn't find any revision to build`.
**Evidencia guardada:** docs/capturas/paso3-job-scm.png, docs/capturas/paso3-consola-build1.png, docs/capturas/paso3-consola-build2.png, Jenkinsfile
**Equivalente en ADO:** *Pipeline script from SCM* + `Jenkinsfile` ≈ pipeline YAML que apunta a `azure-pipelines.yml` del repo. El checkout implícito ≈ el `checkout: self` automático. `BUILD_NUMBER` ≈ `Build.BuildId`, `GIT_COMMIT` ≈ `Build.SourceVersion`, `GIT_BRANCH` ≈ `Build.SourceBranch`. Las credenciales de Jenkins para un repo privado ≈ una *service connection*.

---

## Paso 4 — La app .NET y sus tests (2026-10-20)
**Qué se hizo:**
- `src/HolaMundo.Api`: minimal API en .NET 10 con `GET /`, que devuelve `{ "mensaje", "version" }`.
- `src/HolaMundo.Tests`: 3 tests xUnit con `WebApplicationFactory`.
- `HolaMundo.slnx`: la solución, en el formato nuevo de .NET 10.
- `dotnet test` pasa localmente (3/3), y se probó la API a mano con y sin `APP_VERSION`.
**Decisiones y por qué:**
- **La versión llega de afuera** (`APP_VERSION`, con `"dev"` por defecto): *build once, deploy many*. Se construye una imagen y cada entorno la configura con variables, sin recompilar. Se lee de `IConfiguration` en cada request; así incluye las variables de entorno y los tests la pueden inyectar con `UseSetting`.
- **Ningún test compara el texto exacto del mensaje**: el acto 1 de la demo cambia el saludo y tiene que quedar verde. Los tests verifican el contrato (200, mensaje no vacío, versión correcta), no el contenido.
- **`WebApplicationFactory`** en lugar de testear solo una clase: levanta la API real en memoria y le hace requests HTTP, sin abrir puertos. Es lo más parecido al smoke test, pero dentro del CI.
- El mensaje vive en una constante (`Saludo.MensajeActual`): el acto 1 es cambiar una línea.
- Versiones de paquetes tomadas de la plantilla oficial `dotnet new xunit` del SDK 10.0.401 (xunit 2.9.3, Microsoft.NET.Test.Sdk 17.14.1) + `Microsoft.AspNetCore.Mvc.Testing` 10.0.13.
**Problemas y cómo se resolvieron:** ninguno. Riesgo detectado: si en la terminal queda definida `APP_VERSION` (por ejemplo después de probar la API a mano), el test de la versión por defecto falla. Hay que limpiar la variable con `Remove-Item Env:APP_VERSION`. Lección: los tests no deberían depender del entorno de la máquina.
**Evidencia guardada:** docs/capturas/paso4-tests.png, docs/capturas/paso4-curl-version.png
**Equivalente en ADO:** `APP_VERSION` por entorno ≈ variables por *stage/environment* o *variable groups*. Correr `dotnet test` localmente es lo mismo que haría la tarea `DotNetCoreCLI@2` (command: test), que en Jenkins se arma en el Paso 6.

---

## Paso 5 — Darle a Jenkins acceso a Docker (2026-10-21)
**Qué se hizo:**
- `infra/jenkins/Dockerfile`: parte de `jenkins/jenkins:lts-jdk21` e instala `docker-ce-cli` y `docker-buildx-plugin` desde el repo oficial de Docker. También instala los plugins de `plugins.txt` con `jenkins-plugin-cli`.
- `infra/jenkins/plugins.txt`: `workflow-aggregator`, `git`, `github`, `docker-workflow`, `junit`, `pipeline-graph-view`.
- `infra/docker-compose.yml`: pasa de `image:` a `build: ./jenkins` (imagen `jenkins-hola-mundo:local`), monta `/var/run/docker.sock` y agrega `group_add`.
- `Jenkinsfile`: stage `Docker disponible` con `sh 'docker version'`.
**Decisiones y por qué:**
- **Socket montado en lugar de Docker-in-Docker.** Es más simple y no requiere un contenedor *privileged*. Además, los contenedores que lanza Jenkins (SDK, staging) nacen al lado de Jenkins en el Docker de la PC, así que staging queda accesible en `localhost:8081`. Con DinD quedarían dentro de otro daemon.
- **Solo el cliente de Docker en la imagen.** El daemon sigue siendo el de la PC. Prueba: `docker version` desde Jenkins muestra el mismo Server (`Docker Desktop 4.72.0`, commit `d329809`) que desde PowerShell.
- **Permisos con `group_add` en lugar de `user: root`.** `ls -ln` del socket dio `srw-rw---- 0 0`: dueño root, grupo root (GID 0), lectura y escritura para el grupo. El usuario `jenkins` (UID 1000) no tenía acceso. Se sumó a `jenkins` al grupo 0 (`id` → `groups=1000(jenkins),0(root)`): accede al socket sin correr como root. El GID queda parametrizado (`${DOCKER_GID:-0}`), porque en un Linux nativo el socket suele ser del grupo `docker`, con otro GID.
- **Orden de capas:** el `apt` va antes del `COPY plugins.txt`. Cambiar la lista de plugins no reinstala el CLI, y si falla el mirror, reintentar el build solo repite la última capa. Además, `jenkins-plugin-cli` reintenta 3 veces por la falla intermitente del mirror (Paso 1).
- `pipeline-graph-view` en lugar de `pipeline-stage-view`: es la vista que trae hoy el asistente.
- `docker-buildx-plugin` se incluyó ahora porque es parte de un CLI completo, aunque `docker build` se use recién en el Paso 7.
**Problemas y cómo se resolvieron:** no hubo errores. Observaciones:
1. **La imagen no es 100% reproducible:** el CLI y los plugins no tienen versión fija. Se instaló el CLI 29.9.2 (más nuevo que el daemon, 29.4.2), que negocia la API hacia abajo (`API version: 1.54 (downgraded from 1.56)`). Hoy funciona, pero otro integrante podría obtener otra versión. En un entorno real se fijan las versiones.
2. **Plugins y volumen existente:** los plugins de la imagen van a `/usr/share/jenkins/ref/plugins`, y Jenkins solo copia a `JENKINS_HOME` los que todavía no tiene. Con un volumen ya inicializado, `plugins.txt` agrega los que faltan, pero no actualiza ni quita los que ya están.
3. **Contrato entre pipeline e infraestructura:** el job lee el `Jenkinsfile` desde GitHub. Al pushear el stage `Docker disponible`, el Jenkins del otro integrante (todavía con la imagen vieja) falla con `docker: not found` hasta que reconstruye su imagen. Lección: primero se actualiza la infraestructura y después el pipeline que la necesita.
**Verificación:** build #2 de `hola-mundo` (commit `9dadae9`) en verde. El stage `Docker disponible` muestra Client (`29.9.2`, `linux/amd64`) y Server (`Docker Desktop 4.72.0`).
**Riesgo de seguridad (para el análisis crítico):** quien usa el socket controla todo el Docker de la PC. Un pipeline podría lanzar un contenedor que monte el disco del host, así que en la práctica equivale a acceso root. Se acepta en el laboratorio. En una empresa se usan agentes aislados, Docker-in-Docker o herramientas sin daemon (Kaniko, Buildah).
**Evidencia guardada:** docs/capturas/paso5-docker-version-consola.png, docs/capturas/paso5-stages.png, docs/capturas/paso5-socket-permisos.png, docs/capturas/paso5-plugins-docker.png, infra/jenkins/Dockerfile, infra/jenkins/plugins.txt
**Equivalente en ADO:** un agente *Microsoft-hosted* ya trae Docker. En un agente *self-hosted* hay que instalarlo y darle permiso al usuario del agente sobre el socket, que es el mismo problema. `plugins.txt` ≈ la lista de extensiones del Marketplace instaladas en la organización, pero versionada. El "contrato" entre pipeline y agente ≈ *demands* (pipeline) y *capabilities* (agente).

---

## Paso 6 — CI: build y tests en contenedor (2026-10-21)
**Qué se hizo:**
- `Jenkinsfile`: stages `Build` (`dotnet build -c Release`) y `Test` (`dotnet test --no-build`), cada uno con `agent { docker { image 'mcr.microsoft.com/dotnet/sdk:10.0'; reuseNode true } }`. `junit` en un `post { always }` del stage Test.
- `HolaMundo.Tests.csproj`: paquete `JunitXml.TestLogger` 8.0.0, que genera el reporte en formato JUnit XML.
- Bloque `environment` con `DOTNET_CLI_HOME`, `NUGET_PACKAGES`, `DOTNET_NOLOGO` y `DOTNET_CLI_TELEMETRY_OPTOUT`.
**Decisiones y por qué:**
- **Agentes efímeros:** Jenkins no tiene .NET. Cada stage crea un contenedor del SDK con `docker run` (a través del socket del Paso 5) y lo borra al terminar. Cambiar de versión de .NET es cambiar una línea, sin tocar Jenkins.
- **`reuseNode true`:** los contenedores usan el mismo workspace que el pipeline, así que no se vuelve a clonar y Test encuentra lo que compiló Build.
- **Cómo ve el contenedor el workspace:** el contenedor del SDK nace al lado de Jenkins, no dentro. El plugin `docker-workflow` detecta que Jenkins corre en un contenedor (`Jenkins seems to be running inside container ...`) y le pasa `--volumes-from <jenkins>`, así el SDK ve `/var/jenkins_home/workspace/hola-mundo` con la misma ruta.
- **`-u 1000:1000`:** el plugin corre el contenedor con el UID de Jenkins para que los archivos generados en el workspace le pertenezcan. Ese usuario no tiene home en la imagen del SDK, por eso `DOTNET_CLI_HOME=/tmp/dotnet-home`.
- **`--no-build` en Test:** se prueba exactamente lo que compiló Build.
- **`junit` estricto** (sin `allowEmptyResults`): si no hay reporte, el build falla. Esto atajó el problema 1.
- **`rm -rf TestResults` antes de testear:** el workspace se reutiliza entre builds, y así no se publican resultados viejos.
- **Bug a propósito en el código, no en el test:** la demo muestra un bug atajado, no un test mal escrito.
**Problemas y cómo se resolvieron:**
1. **Build #3: "verde falso" atajado por `junit`.** Con `NUGET_PACKAGES=/tmp/nuget-packages`, `dotnet test` terminó sin error y sin ejecutar ningún test. `junit` falló con `No test report files were found. Configuration error?`. Causa: Build descargó los paquetes en el `/tmp` de *su* contenedor, que se borró al terminar el stage. Test arrancó en un contenedor nuevo con `/tmp` vacío. Los archivos de `obj/` (`*.nuget.g.props`) apuntaban a la caché inexistente, y .NET ignora en silencio esos imports si no existen. Así `HolaMundo.Tests` dejó de ser reconocido como proyecto de tests (lo marca un `.props` de `Microsoft.NET.Test.Sdk`), y no había nada que correr. Solución: `NUGET_PACKAGES=/var/jenkins_home/.nuget/packages`, dentro del volumen, visible para todos los contenedores vía `--volumes-from`. Lección: en un agente efímero, lo que no está en el workspace o en un volumen desaparece entre stages. Sin `junit` estricto, el build habría quedado en verde con cero tests.
2. **Efecto secundario positivo, la caché persistente:** el restore del proyecto de tests pasó de unos 4 s a `All projects are up-to-date for restore`, y el stage Build de 9 s a unos 2,5-5,5 s.
3. **Builds #5 y #6 en verde "con el bug":** el cambio en `Program.cs` no se había guardado, así que no hubo commit ni push, y Jenkins construyó otra vez `a4d5493`. Lección: lo primero que se mira en un build es qué commit construyó (`Checking out Revision` / `Commit message`). Antes de pushear, `git diff`.
**Verificación:**
- Build #4 (`a4d5493`): verde, `Passed: 3`. El reporte de tests aparece en `/job/hola-mundo/4/testReport/`.
- Build #7 (`c92bc7d`, bug `"dev"` → `"desarrollo"`): Build verde y Test rojo, `Expected: "dev"` / `Actual: "desarrollo"`, `Failed: 1, Passed: 2`, `ERROR: script returned exit code 1`. `junit` publicó el fallo igual.
- Build #8 (`775de51`, corrección): verde otra vez.
**Evidencia guardada:** docs/capturas/paso6-error-sin-tests.png, docs/capturas/paso6-stages-rojo-cache.png, docs/capturas/paso6-causa-cache-tmp.png, docs/capturas/paso6-agente-efimero.png, docs/capturas/paso6-test-results-verde.png, docs/capturas/paso6-stages-verde.png, docs/capturas/paso6-consola-test-rojo.png, docs/capturas/paso6-test-results-rojo.png, docs/capturas/paso6-stages-rojo.png, docs/capturas/paso6-tendencia-tests.png
**Equivalente en ADO:** `agent { docker { image ... } }` ≈ `container:` en un job. `dotnet test` con el logger JUnit más `junit` ≈ `DotNetCoreCLI@2` (command: test) con `publishTestResults: true`, o la tarea `PublishTestResults@2`, más la pestaña *Tests* del run. `post { always }` ≈ `condition: always()`. La caché de NuGet en el volumen ≈ la tarea `Cache@2`. Que un test fallido corte el pipeline es igual en ADO: la tarea falla y los stages siguientes no corren salvo que tengan otra `condition:`.

---

## Paso 7 — Imagen Docker de la app (2026-10-21)
**Qué se hizo:**
- `Dockerfile` multi-stage en la raíz: etapa `build` con `sdk:10.0` (`restore` y después `publish`) y etapa final con `aspnet:10.0`, que solo copia lo publicado. Corre como `USER $APP_UID` y expone el 8080.
- `.dockerignore`: excluye `bin/`, `obj/`, `TestResults/`, `.git/`, `docs/` e `infra/`.
- `Jenkinsfile`: stage `Docker Image` con `docker build -t hola-mundo:${BUILD_NUMBER} -t hola-mundo:<sha corto> .`.
**Decisiones y por qué:**
- **Multi-stage:** el compilador (SDK) no viaja en la imagen que se entrega.
- **Primero el `.csproj` y el `restore`, después el código:** si solo cambia el código (acto 1 de la demo), Docker reutiliza la capa del restore.
- **Dos etiquetas para el mismo artefacto:** el número de build (qué build lo produjo) y el SHA corto (qué commit contiene). La imagen es inmutable: un cambio genera una imagen nueva, no modifica la anterior.
- **Sin `APP_VERSION` en la imagen:** se lo pone cada entorno al levantar el contenedor (*build once, deploy many*, Paso 4).
- **`.dockerignore` con `bin/` y `obj/`:** en el workspace de Jenkins existen (los deja el stage Build) y `obj/` apunta a la caché de NuGet del volumen, que no existe dentro de la etapa de compilación. Es el mismo tipo de problema que el build #3 del Paso 6.
- **El stage corre en el controller, sin `--volumes-from`:** `docker build` le envía el contexto (los archivos) al daemon por el socket, así que no importa que el daemon no vea las carpetas de Jenkins. Contraste con el Paso 6, donde el contenedor del SDK necesitaba montar el workspace.
- **Trade-off:** la imagen recompila el mismo commit en lugar de reutilizar lo que compiló el stage Build. El `Dockerfile` queda autónomo (funciona en cualquier máquina, sin Jenkins). La alternativa, copiar a la imagen lo ya compilado, es más estricta con *build once*, pero ata el `Dockerfile` al pipeline.
**Problemas y cómo se resolvieron:** ninguno.
**Verificación:** build #9 (`6d91881`) en verde. `docker images hola-mundo` muestra `hola-mundo:9` y `hola-mundo:6d91881` con el mismo ID (`f43b18d21769`): una imagen con dos etiquetas. Ocupa 340 MB en disco y 96 MB comprimida.
**Qué faltaría en un entorno real:** `docker push` a un registry (Docker Hub, ACR) con una credencial guardada en Jenkins, y una política para borrar las imágenes viejas.
**Evidencia guardada:** docs/capturas/paso7-docker-build-consola.png, docs/capturas/paso7-docker-images.png, docs/capturas/paso7-stages.png, Dockerfile, .dockerignore
**Equivalente en ADO:** la tarea `Docker@2` con `command: build` y `tags: $(Build.BuildId)`. El push sería `command: push` con una *service connection* al registry. `${GIT_COMMIT}` ≈ `$(Build.SourceVersion)`.

---

## Paso 8 — Deploy a staging y smoke test (2026-10-21)
**Qué se hizo:**
- `Jenkinsfile`: stage `Deploy Staging` (`docker rm -f hola-mundo-staging || true` y `docker run -d --name hola-mundo-staging --network ci-local -p 8081:8080 -e APP_VERSION=${BUILD_NUMBER} hola-mundo:${BUILD_NUMBER}`), stage `Smoke Test` (`curl` con reintentos más `grep` de la versión) y bloque `post` con `success` (mensaje y limpieza de imágenes) y `failure` (mensaje).
- `infra/docker-compose.yml`: Jenkins conectado a la red externa `ci-local`, creada una vez con `docker network create ci-local`.
- `Dockerfile`: `LABEL app=hola-mundo`, para que la limpieza toque solo las imágenes de esta app.
**Decisiones y por qué:**
- **Red compartida en lugar de `host.docker.internal`:** el smoke test corre dentro del contenedor de Jenkins, donde `localhost` es Jenkins. Con los dos contenedores en `ci-local`, Jenkins llama a staging por nombre y puerto interno (`http://hola-mundo-staging:8080`). Funciona igual en cualquier Docker, mientras que `host.docker.internal` es propio de Docker Desktop.
- **Red `external`:** si la creara Compose, un `docker compose down` intentaría borrarla con staging todavía conectado.
- **Orden del cambio:** primero se creó la red y se recreó Jenkins, y después se pusheó el `Jenkinsfile` que la usa (lección del Paso 5: la infraestructura antes que el pipeline).
- **Reemplazo simple del contenedor** (`rm` y después `run`): hay unos segundos de corte. En producción se evita con blue/green o rolling update.
- **`APP_VERSION=${BUILD_NUMBER}` al levantar el contenedor:** la imagen es la misma para cualquier entorno, y la versión la pone el deploy (*build once, deploy many*).
- **Smoke test más estricto que el plan:** además del 200 (`curl -f`), verifica con `grep` que la versión devuelta sea la del build. Si siguiera corriendo el contenedor viejo, también respondería 200. `--retry 10 --retry-delay 2 --retry-all-errors`, porque la app tarda un par de segundos en arrancar.
- **Limpieza solo en `success` (cambio respecto del plan, que decía `always`):** si el build falla, staging sigue con la imagen anterior y conviene no tocar nada. Además, todo queda disponible para investigar.
**Problemas y cómo se resolvieron:**
1. **`docker image prune -a` le sacaba la etiqueta del SHA a la imagen desplegada** (build #10: `untagged: hola-mundo:5a16554`, `Total reclaimed space: 0B`). Con el almacén de imágenes de Docker Desktop (containerd), prune evalúa "en uso" por etiqueta: staging se levantó con `hola-mundo:10`, así que `hola-mundo:5a16554` (la misma imagen) se consideró sin uso. Se perdía la trazabilidad imagen → commit del Paso 7. Solución: listar las etiquetas de la app (`docker images --filter label=app=hola-mundo`), excluir las dos del build actual con `grep -v` y pasarle el resto a `docker rmi`. Una imagen que se queda sin etiquetas se borra sola. Verificado en el build #11: `Untagged: hola-mundo:10` y `Deleted`, y `hola-mundo:11` / `hola-mundo:2791879` intactas.
2. (Menor) Se esperaba un `No such container` en el primer `docker rm -f`, pero Docker 29 no lo muestra con `-f`. El `|| true` queda como protección para versiones viejas.
3. (Menor) En Windows PowerShell 5.1, `curl` es un alias de `Invoke-WebRequest`. Para probar staging desde la PC hay que usar `curl.exe`.
**Observación:** en el build #10 todos los pasos del `docker build` salieron `CACHED` (la API no cambió desde el #9) y la imagen se construyó en 0,3 s. Es la caché de capas del Paso 7 funcionando.
**Verificación:** build #11 (`2791879`) completo en verde. El smoke test devolvió `{"mensaje":"Hola mundo desde Jenkins","version":"11"}`, y `curl.exe http://localhost:8081` desde la PC devuelve lo mismo.
**Evidencia guardada:** docs/capturas/paso8-deploy-smoke-consola.png, docs/capturas/paso8-stages-completo.png, docs/capturas/paso8-curl-staging.png
**Equivalente en ADO:** `Deploy Staging` ≈ un *deployment job* con `environment: staging`. El smoke test ≈ un paso de script después del deploy. `post { success }` / `post { failure }` / `post { always }` ≈ `condition: succeeded()` / `failed()` / `always()`. La red compartida no tiene equivalente directo: en ADO, el agente y el destino del deploy suelen comunicarse por la red de la nube.

---

## Paso 9 — Disparo automático por push (2026-10-22)
**Qué se hizo:**
- **Polling primero:** `triggers { pollSCM('H/2 * * * *') }` en el `Jenkinsfile`. Jenkins le pregunta a GitHub cada ~2 minutos (`git ls-remote`) si la rama tiene commits nuevos.
- **Webhook después:** `githubPush()` en `triggers`, con el polling como respaldo.
- **Túnel ngrok como servicio `tunel` en `infra/docker-compose.yml`:** con `profiles: ["tunel"]`, solo arranca con `docker compose --profile tunel up -d`. Llega a Jenkins por nombre (`http jenkins:8080`) a través de la red `ci-local`.
- **Configuración del túnel:**
  - token y dominio fijo en `infra/.env`, que no se versiona; `infra/.env.example` sirve de plantilla;
  - política de tráfico en `infra/ngrok/politica.yml`.
- **Webhook en GitHub:** apunta a `https://comfortably-numeric-antonetta.ngrok-free.dev/github-webhook/` (`application/json`, solo el evento push).
**Decisiones y por qué:**
- **Polling antes que webhook:** no necesita nada expuesto a internet, así que sirve para probar el disparo automático sin depender del túnel. Después queda como plan B: si el túnel se cae, el build arranca igual, solo que con hasta 2 minutos de demora. La `H` reparte el minuto exacto entre jobs, para que no le pregunten todos a GitHub al mismo tiempo.
- **Por qué hace falta un túnel:** la PC está detrás de NAT. GitHub no tiene una dirección a la que mandarle el aviso. El túnel es una conexión saliente desde la PC hacia ngrok, y ngrok reenvía por ella lo que llega a la URL pública.
- **Dominio estático de ngrok** (gratuito, `.ngrok-free.dev`): si la URL cambiara en cada reinicio, habría que editar el webhook en GitHub cada vez.
- **Política de tráfico:** todo lo que no empiece con `/github-webhook` recibe un 404 (`No disponible`) en ngrok, sin llegar a Jenkins. Así la UI, el login y la API de Jenkins no quedan expuestos a internet. Prueba: `curl.exe https://…/` devolvió `No disponible`, y `curl.exe https://…/github-webhook/` devolvió una página de error de Jenkins. Ese error es esperable: el endpoint espera el POST con el JSON de GitHub y recibió un GET vacío; la página prueba que el pedido atravesó el túnel.
- **Túnel en un perfil de Compose:** un integrante que no configuró ngrok puede levantar Jenkins igual con `docker compose up -d`.
- **El authtoken nunca pasó por el chat ni por el repo:** se escribió a mano en `infra/.env` (verificado con `git check-ignore`).
**Problemas y cómo se resolvieron:**
1. **Los triggers se registran recién después de un build:** el job lee el `Jenkinsfile` de GitHub recién cuando corre. Por eso, después de agregar `pollSCM` hubo que lanzar un build manual (#12) para que Jenkins empezara a hacer polling. Recién entonces el build #13 se disparó solo ("Started by an SCM change"). Con `githubPush()` pasó lo mismo: el build #14 (por polling) lo registró, y recién ahí el #15 llegó por webhook. El Git Polling Log del #13 muestra cómo decide Jenkins: compara el último commit remoto con el último construido.
2. **El túnel arrancaba pero `docker logs tunel-ngrok` estaba vacío:** sin una terminal interactiva, ngrok no imprime nada. Se agregó `--log stdout` al comando. A partir de ahí se ve `started tunnel ... url=https://comfortably-numeric-antonetta.ngrok-free.dev`.
3. **Inestabilidad del smoke test (build #12):** el primer `curl` falló (`curl: (7) ... after 1 ms`) porque la app todavía no escuchaba. El segundo intento respondió bien. Es el caso para el que se pusieron los `--retry` en el Paso 8: sin ellos, el build habría quedado rojo sin que la app tuviera ningún problema.
4. (Menor) Se esperaba un dominio `.ngrok-free.app`. Los dominios gratuitos actuales son `.ngrok-free.dev`.
**Riesgos para el análisis crítico:**
- **El webhook no tiene *secret*:** cualquiera que conozca la URL puede mandar un POST a `/github-webhook/`. El daño es acotado: Jenkins no construye lo que dice el aviso, sino que va a GitHub a buscar el código, así que un aviso falso como mucho dispara un build de más. En un entorno real se configura un *secret* en GitHub y en Jenkins, para que se rechacen los avisos sin firma válida.
- **El túnel expone una PC de desarrollo a internet,** aunque la política lo restringe a una sola ruta.
**Verificación:** se cambió el saludo a "disparado por webhook" y se pusheó (`aac50ed`). Sin tocar Jenkins, el build #15 arrancó en segundos con la causa **"Started by GitHub push by AgustinLago22"** y salió completo en verde. El smoke test devolvió `{"mensaje":"Hola mundo desde Jenkins, disparado por webhook","version":"15"}`, y la limpieza borró las etiquetas del #14 (`hola-mundo:14`, `hola-mundo:8e3be45`).
**Evidencia guardada:** docs/capturas/paso9-webhook-github.png, docs/capturas/paso9-started-by-github-push.png, `infra/docker-compose.yml`, `infra/ngrok/politica.yml`
**Equivalente en ADO:** `trigger: branches: include: [main]`. En ADO, el problema de este paso "no existe": Azure Pipelines es un servicio en la nube con dirección pública, y GitHub le avisa directamente (a través de la GitHub App de Azure Pipelines). Lo interesante es que los agentes *self-hosted* de ADO funcionan detrás de NAT por la misma razón que el túnel: la conexión la abren ellos hacia afuera y le preguntan a ADO si hay trabajo (pull). Es la misma idea que el polling, pero del lado del agente.
