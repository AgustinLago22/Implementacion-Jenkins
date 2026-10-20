# Bitácora del proyecto

Registro paso a paso de lo que se hizo, las decisiones tomadas y los problemas encontrados. Alimenta la sección "Caso práctico" del informe.

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
