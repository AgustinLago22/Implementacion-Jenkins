# Imagen de Jenkins del laboratorio: la oficial + Docker CLI + plugins de plugins.txt.
# Por qué una imagen propia: lo instalado a mano dentro de un contenedor se pierde al recrearlo,
# y los plugins instalados desde la UI dependen de que ese día el mirror responda (bitácora, Paso 1).
# Así cualquier integrante obtiene el mismo Jenkins con "docker compose build".
FROM jenkins/jenkins:lts-jdk21

# apt necesita root. Al final se vuelve al usuario jenkins.
USER root

# Solo el CLIENTE de Docker, no el daemon: Jenkins le va a dar órdenes al daemon de la PC
# a través del socket montado en el compose.
# Repo oficial de Docker porque el paquete de Debian (docker.io) trae versiones viejas.
# buildx es el motor que usa "docker build" (se necesita en el Paso 7).
RUN apt-get update \
 && apt-get install -y --no-install-recommends ca-certificates curl \
 && install -m 0755 -d /etc/apt/keyrings \
 && curl -fsSL https://download.docker.com/linux/debian/gpg -o /etc/apt/keyrings/docker.asc \
 && chmod a+r /etc/apt/keyrings/docker.asc \
 && echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/debian $(. /etc/os-release && echo $VERSION_CODENAME) stable" \
    > /etc/apt/sources.list.d/docker.list \
 && apt-get update \
 && apt-get install -y --no-install-recommends docker-ce-cli docker-buildx-plugin \
 && rm -rf /var/lib/apt/lists/*

# Jenkins no debe correr como root.
USER jenkins

# Se copia DESPUÉS del apt: si solo cambia la lista de plugins, Docker reutiliza la capa
# cacheada de arriba y no reinstala el CLI.
COPY --chown=jenkins:jenkins plugins.txt /usr/share/jenkins/ref/plugins.txt

# Los plugins quedan en /usr/share/jenkins/ref/plugins y, al arrancar, Jenkins copia a
# JENKINS_HOME los que todavía no tenga.
# Reintentos: el mirror de plugins falla de forma intermitente desde nuestra red (bitácora, Paso 1).
RUN for intento in 1 2 3; do \
      jenkins-plugin-cli --plugin-file /usr/share/jenkins/ref/plugins.txt && exit 0; \
      echo "Falló la descarga de plugins (intento $intento), reintentando..."; sleep 10; \
    done; exit 1
