// Pipeline de hola-mundo: CI de la API .NET.
pipeline {
    // El controller hace el checkout y los stages livianos.
    // Los stages de .NET piden su propio agente (un contenedor con el SDK).
    agent any

    environment {
        // El contenedor del SDK corre con el usuario de Jenkins (UID 1000), que ahí adentro
        // no tiene un home donde escribir: se le dan carpetas propias a la CLI y a NuGet.
        DOTNET_CLI_HOME = '/tmp/dotnet-home'
        NUGET_PACKAGES  = '/tmp/nuget-packages'
        // Menos ruido en la consola: sin banner de bienvenida ni telemetría.
        DOTNET_NOLOGO               = 'true'
        DOTNET_CLI_TELEMETRY_OPTOUT = 'true'
    }

    stages {
        stage('Saludo') {
            steps {
                // Comillas dobles: Groovy interpola ${...}. Con simples se imprimiría literal.
                echo "Hola mundo versionado - build #${env.BUILD_NUMBER}"
            }
        }

        stage('Info del build') {
            steps {
                // Estas variables las inyecta Jenkins: el checkout del repo ocurrió
                // antes del primer stage (checkout implícito), sin pedirlo.
                echo "Commit: ${env.GIT_COMMIT}"
                echo "Rama:   ${env.GIT_BRANCH}"
                // El workspace contiene el repo clonado: la prueba del checkout.
                sh 'ls -la'
            }
        }

        stage('Docker disponible') {
            steps {
                // Client = el CLI instalado en la imagen de Jenkins.
                // Server = el daemon de la PC, alcanzado por el socket montado.
                // Si falta el socket o el permiso, este stage falla y corta el pipeline.
                sh 'docker version'
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
    }
}
