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
