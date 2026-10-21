// Pipeline de test
pipeline {
    agent any

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
    }
}
