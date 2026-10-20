# CLAUDE.md — Jenkins "Hola Mundo" CI/CD (trabajo de facultad)

## Contexto

Proyecto académico grupal sobre **Jenkins**. Se entrega un informe escrito (10–15 páginas) y una presentación oral de 20 minutos con demo en vivo.

El objetivo es un "hola mundo" de CI/CD: simple, pero descriptivo e intuitivo. Tiene que demostrar que escalarlo a un entorno real no implica un salto grande.

Sobre el autor:
- Conoce CI/CD y trabaja con **Azure DevOps (ADO)**.
- **Nunca configuró Jenkins desde cero.**
- Quiere **entender cada paso**, no solo tener algo que funcione.

## Cómo tenés que trabajar (modo aprendizaje)

Estas reglas son más importantes que terminar rápido.

1. **Un paso por vez.** Trabajá solo en el paso marcado como actual en "Estado actual". No adelantes trabajo de pasos futuros, aunque sea "obvio".
2. **Explicá antes de hacer.** Antes de crear o modificar archivos, explicá en pocas líneas:
   - qué vamos a hacer;
   - por qué;
   - qué concepto de Jenkins o CI/CD aparece.
   Cuando sirva, compará con el equivalente en ADO.
3. **El autor ejecuta los comandos.** Vos escribís los archivos. Los comandos de `docker`, `git` y similares se los das para que los corra él en **PowerShell** (Windows), salvo que te pida explícitamente que los corras vos. Explicá qué hace cada comando no trivial.
4. **Cambios chicos y legibles.** Este código se va a mostrar en una presentación. Usá comentarios en español en los archivos clave (`Jenkinsfile`, `docker-compose.yml`, `Dockerfile`) explicando el porqué, no el qué.
5. **Al cerrar cada paso:**
   - verificá el criterio de "terminado" con el autor;
   - recordale qué capturas o evidencias guardar para el informe;
   - agregá una entrada en `docs/bitacora.md` (formato abajo);
   - hacé 1 o 2 preguntas cortas de comprensión;
   - actualizá "Estado actual" en este archivo.
6. **No avances al paso siguiente sin confirmación explícita.**
7. **Los errores son material.** Cuando algo falle, no lo "arregles en silencio": explicá la causa. Los problemas reales van a la bitácora, porque alimentan la sección "problemas surgidos" del informe.
8. Si algo del plan resulta mal planteado, decilo y proponé el cambio antes de hacerlo.

## Decisiones tomadas

| Tema | Decisión |
|---|---|
| Entorno | Windows + Docker Desktop (backend WSL2). Jenkins corre con **Docker Compose** en la PC local. |
| App de ejemplo | **API mínima en .NET** (LTS actual, .NET 10) + tests con **xUnit**. |
| Alcance del pipeline | CI + imagen Docker + **deploy a un contenedor "staging" local** + smoke test. |
| Ejecución de builds | Jenkins **no** tiene .NET instalado. Build y tests corren en contenedores efímeros `mcr.microsoft.com/dotnet/sdk`. |
| Acceso a Docker desde Jenkins | Se monta el socket del Docker del host (`/var/run/docker.sock`) y se instala el Docker CLI en una imagen custom de Jenkins. El riesgo de seguridad está documentado. |
| Disparador | Push a GitHub → **webhook** vía túnel (ngrok con dominio fijo, o cloudflared). **SCM polling** queda como plan B. |
| Repo | Un único repo (app + Jenkinsfile + infra). En una empresa `infra/` sería un repo aparte; el informe lo aclara. |
| Trabajo en equipo | Cada integrante corre su propio Jenkins. Se comparte la **configuración como código** (Jenkinsfile, `plugins.txt`, JCasC), no el volumen `jenkins_home`. El historial de builds es local de cada uno. |

## La historia de la demo

Una API con `GET /` que responde un saludo y la versión (el número de build de Jenkins). La demo tiene dos actos:

1. **Camino feliz:** se cambia el mensaje y se hace `git push`. El pipeline corre solo y `curl http://localhost:8081` devuelve el mensaje nuevo con la versión nueva.
2. **El pipeline protege:** se rompe un test a propósito y se hace push. El pipeline queda en rojo, no se despliega nada y staging sigue con la versión anterior.

Todo lo que se construya tiene que servir a esta historia.

## Arquitectura objetivo

```
GitHub ──webhook──> túnel (ngrok/cloudflared) ──> Jenkins (contenedor)
                                                      │ usa el Docker del host (socket)
                                                      ├─ contenedor efímero dotnet/sdk → build + test
                                                      ├─ docker build → imagen hola-mundo:<build>
                                                      └─ docker run → contenedor "hola-mundo-staging" (puerto 8081)
```

## Estructura objetivo del repo

Se construye de a poco; no crear todo de entrada.

```
jenkins-hola-mundo/
├── src/
│   ├── HolaMundo.Api/          # minimal API
│   └── HolaMundo.Tests/        # tests xUnit
├── Dockerfile                  # imagen de la app (multi-stage)
├── Jenkinsfile                 # el pipeline
├── infra/
│   ├── docker-compose.yml      # Jenkins (+ túnel más adelante)
│   └── jenkins/
│       ├── Dockerfile          # jenkins/jenkins:lts + docker CLI
│       ├── plugins.txt         # plugins preinstalados
│       └── casc.yaml           # (extra) Configuration as Code
├── ado/azure-pipelines.yml     # (extra) mismo pipeline en ADO, para comparar
├── docs/
│   ├── bitacora.md             # decisiones y problemas, paso a paso
│   └── capturas/               # evidencia para el informe
├── .gitattributes
├── .gitignore
└── README.md
```

## Problemas conocidos en Windows (tenerlos presentes)

- **Saltos de línea:** CRLF rompe scripts que corren en contenedores Linux. Usar `.gitattributes` con `* text=auto eol=lf`.
- **`localhost` dentro de un contenedor** es el propio contenedor, no la PC. Para llegar a staging desde Jenkins, usar una red de Docker compartida y el nombre del contenedor (`http://hola-mundo-staging:8080`), o `host.docker.internal`.
- **Permisos del socket de Docker:** el usuario `jenkins` del contenedor puede no tener permiso sobre `/var/run/docker.sock`. Explicar las opciones antes de elegir una (`group_add`, correr como root solo en este entorno de laboratorio, etc.).
- **Recursos:** Jenkins + SDK de .NET consumen bastante RAM. Revisar la memoria asignada a WSL2 en Docker Desktop.
- **URL del túnel:** si cambia en cada reinicio, el webhook de GitHub deja de funcionar. Preferir un dominio fijo.

---

## Plan por pasos

Cada paso define:
- **Objetivo**
- **Conceptos** que aparecen
- **Terminado cuando**: el criterio verificable
- **Evidencia**: qué guardar para el informe

### Paso 0 — Preparación del repo
- **Objetivo:** crear el repo en GitHub y la estructura mínima: `README.md`, `.gitignore` (.NET + Visual Studio), `.gitattributes`, `docs/bitacora.md` y `docs/capturas/`.
- **Conceptos:** por qué el pipeline vive junto al código (pipeline as code).
- **Terminado cuando:** el repo existe en GitHub con el primer commit.
- **Evidencia:** URL del repo y estructura inicial.

### Paso 1 — Levantar Jenkins con Docker Compose
- **Objetivo:** escribir `infra/docker-compose.yml` con la imagen oficial `jenkins/jenkins:lts-jdk21`, un volumen nombrado para `jenkins_home` y los puertos 8080 (UI) y 50000 (agentes). Después, completar el asistente inicial: contraseña de `initialAdminPassword`, plugins sugeridos y usuario admin.
- **Conceptos:**
  - **controller**: qué es y qué guarda `JENKINS_HOME`;
  - por qué un volumen persistente;
  - plugins: Jenkins es casi todo plugins.
- **Terminado cuando:** se puede entrar a `http://localhost:8080` y, después de `docker compose down` y `up`, la configuración sigue ahí.
- **Evidencia:** captura del desbloqueo, del dashboard vacío y el contenido del compose.

### Paso 2 — El "hola mundo" literal
- **Objetivo:** crear un job tipo *Pipeline* con script inline: un stage que hace `echo "Hola mundo desde Jenkins"`.
- **Conceptos:**
  - job, build, consola de salida;
  - pipeline declarativo: `pipeline`, `agent`, `stages`, `stage`, `steps`;
  - comparación con un pipeline mínimo de ADO.
- **Terminado cuando:** el build #1 sale en verde y la consola muestra el mensaje.
- **Evidencia:** captura del job, la vista de stages y la consola.

### Paso 3 — Pipeline as code (Jenkinsfile desde el repo)
- **Objetivo:** pasar el pipeline a un `Jenkinsfile` en la raíz del repo y configurar el job como *Pipeline script from SCM*, apuntando a GitHub. Por ahora se dispara con "Build Now".
- **Conceptos:**
  - por qué el pipeline versionado es mejor que el configurado en la UI;
  - checkout implícito;
  - variables de entorno como `BUILD_NUMBER` y `GIT_COMMIT`.
- **Terminado cuando:** un cambio en el `Jenkinsfile` pusheado a GitHub se refleja en el siguiente build manual.
- **Evidencia:** el `Jenkinsfile` y la configuración del job.

### Paso 4 — La app .NET y sus tests
- **Objetivo:** crear `HolaMundo.Api` (minimal API):
  - `GET /` devuelve `{ mensaje, version }`;
  - la versión se lee de una variable de entorno (`APP_VERSION`), con valor por defecto `"dev"`.

  Crear también `HolaMundo.Tests` con 2 o 3 tests simples, usando `WebApplicationFactory` o la lógica extraída a una clase. Correr build y tests localmente; si el autor no tiene el SDK instalado, dentro de un contenedor `dotnet/sdk`.
- **Conceptos:** por qué la versión viene de afuera: la misma imagen se configura distinto por entorno.
- **Terminado cuando:** `dotnet test` pasa localmente.
- **Evidencia:** salida de los tests.

### Paso 5 — Darle a Jenkins acceso a Docker
- **Objetivo:** crear `infra/jenkins/Dockerfile`, que parte de `jenkins/jenkins:lts-jdk21`, instala el Docker CLI y los plugins de `plugins.txt` con `jenkins-plugin-cli`. Plugins: `workflow-aggregator`, `git`, `github`, `docker-workflow`, `junit` y `pipeline-stage-view`. Montar `/var/run/docker.sock` en el compose y resolver los permisos.
- **Conceptos:**
  - socket montado vs Docker-in-Docker, con sus ventajas y riesgos;
  - plugins como código;
  - imagen de Jenkins reproducible.
- **Terminado cuando:** un stage con `sh 'docker version'` muestra cliente y servidor.
- **Evidencia:** el Dockerfile, `plugins.txt` y la salida de `docker version` en la consola de Jenkins.

### Paso 6 — CI: build y tests en contenedor
- **Objetivo:** en el `Jenkinsfile`, stages *Build* y *Test* con `agent { docker { image 'mcr.microsoft.com/dotnet/sdk:10.0' } }`. Publicar los resultados con el step `junit`, usando el logger `JunitXml.TestLogger` en el proyecto de tests.
- **Conceptos:**
  - agentes efímeros en contenedor;
  - workspace;
  - reporte de tests;
  - qué pasa con el build cuando un test falla.
- **Terminado cuando:** el pipeline muestra los resultados de tests y, si se rompe un test a propósito, el build queda rojo. Después hay que volver a dejarlo en verde.
- **Evidencia:** gráfico o reporte de tests, y un build verde y uno rojo.

### Paso 7 — Imagen Docker de la app
- **Objetivo:** escribir un `Dockerfile` multi-stage (sdk para compilar, aspnet para correr) y un stage *Docker Image* que construya `hola-mundo:${BUILD_NUMBER}`, con el SHA corto del commit como etiqueta adicional.
- **Conceptos:**
  - artefacto inmutable;
  - tagging por build;
  - multi-stage para imágenes chicas;
  - qué faltaría en la vida real: push a un registry.
- **Terminado cuando:** `docker images` muestra la imagen etiquetada con el número de build.
- **Evidencia:** el Dockerfile y el listado de imágenes.

### Paso 8 — Deploy a staging y smoke test
- **Objetivo:**
  - stage *Deploy Staging*: frena y borra `hola-mundo-staging` si existe, y lo levanta con la imagen nueva, `APP_VERSION=${BUILD_NUMBER}`, puerto `8081:8080` y la red compartida con Jenkins;
  - stage *Smoke Test*: `curl` al endpoint, que hace fallar el build si no responde 200;
  - bloque `post`: limpieza de imágenes viejas y mensaje de éxito o fallo.
- **Conceptos:**
  - CD;
  - entorno staging;
  - smoke test como gate;
  - `post { always / success / failure }` como equivalente a las `condition:` de ADO.
- **Terminado cuando:** después de un build, `curl http://localhost:8081` (desde la PC) devuelve el mensaje con la versión de ese build.
- **Evidencia:** vista completa de stages y respuesta del curl con la versión.

### Paso 9 — Disparo automático por push
- **Objetivo:**
  1. Configurar primero **SCM polling** (`pollSCM`) y probarlo.
  2. Después, **webhook**: levantar el túnel (como servicio en el compose o aparte) y configurar en GitHub el webhook a `https://<url-tunel>/github-webhook/`. En el `Jenkinsfile`, usar `githubPush()` en `triggers`.
  3. Dejar el polling como respaldo.
- **Conceptos:**
  - push vs pull (webhook vs polling);
  - por qué GitHub no ve una PC local (NAT) y cómo lo resuelve un túnel;
  - por qué esto en ADO "no existe" como problema.
- **Terminado cuando:** un `git push` dispara el pipeline en segundos sin tocar Jenkins.
- **Evidencia:** configuración del webhook en GitHub (con "delivery" exitosa) y el build con la causa "Started by GitHub push".

### Paso 10 — Extras (elegir según tiempo)

Cada extra se trata como un paso propio, de a uno.
- **10a — Comparación con ADO:** `ado/azure-pipelines.yml` equivalente al `Jenkinsfile` (no hace falta ejecutarlo), más una tabla de equivalencias de conceptos para el informe.
- **10b — Configuration as Code (JCasC):** `casc.yaml` con usuario admin, configuración básica y el job definido como código, para que Jenkins se levante configurado sin clicks.
- **10c — Agente separado:** un contenedor agente (`jenkins/inbound-agent` o SSH) conectado al controller, para mostrar la arquitectura controller/agent.
- **10d — Aprobación manual:** un stage con `input` para "promover a producción" (otro contenedor en otro puerto), equivalente a un approval gate de ADO.

### Paso 11 — Ensayo de la demo
- **Objetivo:** guion de la demo:
  - acto 1: cambio de mensaje y deploy;
  - acto 2: test roto y pipeline rojo, con staging intacto.
- **Checklist previo:**
  - Docker Desktop corriendo;
  - imágenes base ya descargadas;
  - túnel activo y webhook verificado;
  - staging en verde;
  - polling como respaldo.
- **Terminado cuando:** la demo sale completa dos veces seguidas y hay un **video grabado de respaldo**.

---

## Formato de `docs/bitacora.md`

```markdown
## Paso N — <título> (AAAA-MM-DD)
**Qué se hizo:** ...
**Decisiones y por qué:** ...
**Problemas y cómo se resolvieron:** ...
**Evidencia guardada:** docs/capturas/...
**Equivalente en ADO:** ...
```

## Mapa al informe

| Sección del informe | Se alimenta de |
|---|---|
| 1. Introducción y marco conceptual | Conceptos explicados en los pasos 1–3 |
| 2. Arquitectura y funcionamiento | Controller, agentes, plugins, Jenkinsfile (pasos 1, 5, 6, 10c) |
| 3. Caso práctico | `docs/bitacora.md` + capturas, pasos 0–9 |
| 4. Análisis crítico | Paso 10a, riesgos del paso 5, túnel del paso 9, tabla de escalado |
| 5. Conclusiones | Cuándo conviene Jenkins vs ADO/GitHub Actions |

## Tabla de escalado (argumento central)

| En la demo | En un entorno real | Qué cambia |
|---|---|---|
| Imagen en Docker local | Registry (Docker Hub, ACR) | Un `docker push` más y una credencial |
| Contenedor staging | Kubernetes / App Service | Solo la etapa de deploy |
| Túnel | Jenkins con URL pública | Nada en el pipeline |
| Todo en un nodo | Controller + agentes | `agent { label '...' }` |
| Configuración por UI | JCasC + plugins.txt versionados | La infra pasa a ser código |
| Deploy directo | Aprobación manual antes de prod | Una etapa `input` |

---

## Estado actual

> Actualizar al cerrar cada paso.

- **Paso actual:** 3 — Pipeline as code (pendiente de confirmación para arrancar)
- **Pasos completados:** 0, 1, 2
- **Notas:**
  - El stack `argos-platform` (cAdvisor) ocupa el puerto 8080: frenarlo antes de levantar Jenkins y sumarlo al checklist de la demo.
  - El mirror de plugins `ftp-nyc.osuosl.org` falla de forma intermitente desde esta red. En el paso 5, prever reintentos o un mirror alternativo para `jenkins-plugin-cli`.
  - Este Jenkins trae **Pipeline Graph View** (no Stage View). En el paso 5, proponer `pipeline-graph-view` en lugar de `pipeline-stage-view` en `plugins.txt`.
  - Evaluar adelantar el 10b (JCasC) después del paso 5, para que el otro integrante no repita el asistente a mano.