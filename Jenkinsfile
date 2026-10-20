// Pipeline de test
pipeline {
    agent any

    stages {
        stage('Saludo') {
            steps {
                // Comillas dobles: Groovy interpola ${...}. Con simples se imprimiría literal.
                echo "Hola mundo desde Jenkins - build #${env.BUILD_NUMBER}"
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
    }
}
