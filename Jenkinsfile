// Pipeline de hola-mundo: CI de la API .NET.
pipeline {
    // El controller hace el checkout y los stages livianos.
    // Los stages de .NET piden su propio agente (un contenedor con el SDK).
    agent any

    environment {
        // El contenedor del SDK corre con el usuario de Jenkins (UID 1000), que ahí adentro
        // no tiene un home donde escribir: la CLI de .NET usa una carpeta temporal.
        DOTNET_CLI_HOME = '/tmp/dotnet-home'
        // La caché de NuGet NO puede ir en /tmp: cada stage corre en un contenedor nuevo y el
        // /tmp del anterior desaparece. Sin la caché, Test no reconoce el proyecto de tests y
        // no corre ninguno (bitácora, Paso 6). En el volumen jenkins_home la ven todos los
        // contenedores (--volumes-from) y además sobrevive entre builds.
        NUGET_PACKAGES  = '/var/jenkins_home/.nuget/packages'
        // Menos ruido en la consola: sin banner de bienvenida ni telemetría.
        DOTNET_NOLOGO               = 'true'
        DOTNET_CLI_TELEMETRY_OPTOUT = 'true'
    }

    triggers {
        // Webhook (push): GitHub avisa en /github-webhook/ (por el túnel) apenas llega un push.
        githubPush()
        // Polling (pull), como respaldo si el túnel se cae: Jenkins le pregunta a GitHub cada
        // ~2 minutos si hay commits nuevos. "H" reparte el minuto exacto entre jobs.
        // En un entorno real, con webhook confiable, se espacia mucho más o se quita.
        pollSCM('H/2 * * * *')
    }

    stages {
        stage('Saludo') {
            steps {
                // Comillas dobles: Groovy interpola ${...}. Con simples se imprimiría literal.
                echo "Hola mundo versionado - build #${env.BUILD_NUMBER}"
            }
        }

        stage('Build') {
            agent {
                docker {
                    // Agente efímero: un contenedor con el SDK que nace para este stage y se
                    // borra al terminar. Jenkins no necesita tener .NET instalado.
                    image 'mcr.microsoft.com/dotnet/sdk:10.0'
                    // Mismo workspace que el resto del pipeline (no vuelve a clonar):
                    // así el stage Test encuentra lo que se compiló acá.
                    reuseNode true
                }
            }
            steps {
                sh 'dotnet build HolaMundo.slnx -c Release'
            }
        }

        stage('Test') {
            agent {
                docker {
                    image 'mcr.microsoft.com/dotnet/sdk:10.0'
                    reuseNode true
                }
            }
            steps {
                // El workspace se reutiliza entre builds: se borra el reporte anterior
                // para no publicar resultados viejos si esta corrida no llega a generarlo.
                sh 'rm -rf TestResults'
                // --no-build: se prueba exactamente lo que compiló el stage Build.
                // Si un test falla, dotnet test termina con exit code 1 y el build queda en rojo:
                // los stages siguientes (imagen, deploy) no se ejecutan.
                sh 'dotnet test HolaMundo.slnx -c Release --no-build --logger "junit;LogFilePath=$WORKSPACE/TestResults/junit.xml"'
            }
            post {
                // always: los resultados se publican también cuando hay tests fallidos,
                // que es justo cuando más importa verlos.
                always {
                    junit 'TestResults/*.xml'
                }
            }
        }

        stage('Docker Image') {
            steps {
                // Corre en el controller: el CLI de Docker le manda el workspace (el "contexto")
                // al daemon de la PC, que construye la imagen.
                // Dos etiquetas para el mismo artefacto: el número de build (qué build lo produjo)
                // y el SHA corto (qué commit tiene adentro).
                sh "docker build -t hola-mundo:${env.BUILD_NUMBER} -t hola-mundo:${env.GIT_COMMIT.substring(0, 7)} ."
            }
        }

        stage('Deploy Staging') {
            steps {
                // Reemplazo simple: se borra el contenedor anterior y se levanta uno con la imagen nueva.
                // "|| true": en el primer deploy el contenedor no existe y rm daría error.
                // (En producción se evita el corte con blue/green o rolling update.)
                sh 'docker rm -f hola-mundo-staging || true'
                // Misma imagen que saldría a producción; lo que cambia por entorno va en variables:
                // APP_VERSION es el número de este build (build once, deploy many).
                sh """
                    docker run -d --name hola-mundo-staging \
                        --network ci-local \
                        -p 8081:8080 \
                        -e APP_VERSION=${env.BUILD_NUMBER} \
                        hola-mundo:${env.BUILD_NUMBER}
                """
            }
        }

        stage('Smoke Test') {
            steps {
                // Por nombre y puerto interno: Jenkins y staging comparten la red ci-local.
                // La app tarda unos segundos en arrancar: curl reintenta antes de darla por caída.
                // -f: un HTTP de error (4xx/5xx) también cuenta como fallo.
                sh 'curl -fsS --retry 10 --retry-delay 2 --retry-all-errors -o respuesta.json http://hola-mundo-staging:8080/'
                sh 'cat respuesta.json'
                // No alcanza con que responda: tiene que ser la versión que se acaba de desplegar.
                sh "grep -q '\"version\":\"${env.BUILD_NUMBER}\"' respuesta.json"
            }
        }
    }

    post {
        success {
            echo "Staging actualizado: http://localhost:8081 responde con la versión ${env.BUILD_NUMBER}."
            // Limpieza: cada build deja una imagen nueva. Se borran todas las etiquetas de esta app
            // salvo las dos de este build (número y SHA); una imagen sin etiquetas se borra sola.
            // Solo en success: si el build falla, staging sigue con la imagen anterior y no se toca nada.
            // (No se usa "docker image prune -a": evalúa por etiqueta y le saca el SHA a la imagen en uso.)
            // En un entorno real las versiones viejas quedan en el registry, para poder volver atrás.
            sh """
                docker images --filter label=app=hola-mundo --format '{{.Repository}}:{{.Tag}}' \
                    | grep -v -e ':${env.BUILD_NUMBER}\$' -e ':${env.GIT_COMMIT.substring(0, 7)}\$' \
                    | xargs -r docker rmi
            """
        }
        failure {
            echo "El build #${env.BUILD_NUMBER} falló. Si falló antes de 'Deploy Staging', staging no se tocó y sigue con la versión anterior."
        }
    }
}
