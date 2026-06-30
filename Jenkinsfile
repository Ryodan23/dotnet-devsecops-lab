pipeline {
    agent any

    environment {
        APP_NAME = 'demo-api'
        REGISTRY = 'localhost:5000'
        CLUSTER_REGISTRY = 'k3d-ci-registry:5000'
        IMAGE_TAG = "${env.BUILD_NUMBER}"
        IMAGE_LOCAL = "${REGISTRY}/${APP_NAME}:${IMAGE_TAG}"
        IMAGE_CLUSTER = "${CLUSTER_REGISTRY}/${APP_NAME}:${IMAGE_TAG}"

        SONAR_HOST_URL = 'http://sonarqube:9000'

        SMTP_HOST = 'mailhog'
        SMTP_PORT = '1025'

        DOTNET_PROJECT = 'src/DemoApi/DemoApi.csproj'
    }

    options {
        timestamps()
        disableConcurrentBuilds()
    }

    stages {
        stage('Checkout') {
            steps {
                checkout scm
            }
        }

        stage('Debug Workspace') {
            steps {
                sh '''
                    echo "Workspace actual:"
                    pwd

                    echo "Contenido raíz:"
                    ls -la

                    echo "Proyectos encontrados:"
                    find . -maxdepth 5 \\( -name "*.sln" -o -name "*.csproj" \\)
                '''
            }
        }

        stage('Dotnet Restore') {
            steps {
                sh 'dotnet restore $DOTNET_PROJECT'
            }
        }

        stage('Dotnet Build') {
            steps {
                sh 'dotnet build $DOTNET_PROJECT --configuration Release --no-restore'
            }
        }

        stage('Dotnet Test + Coverage') {
            steps {
                sh '''
                    dotnet test $DOTNET_PROJECT \
                      --configuration Release \
                      --no-build \
                      --collect:"XPlat Code Coverage" \
                      --results-directory coverage
                '''
            }
        }

        // stage('OWASP Dependency Check') {
        //     steps {
        //         withCredentials([string(credentialsId: 'nvd-api-key', variable: 'NVD_API_KEY')]) {
        //             sh '''
        //                 mkdir -p dependency-check-report
        //                 mkdir -p /var/jenkins_home/dependency-check-data

        //                 dependency-check \
        //                 --project "${APP_NAME}" \
        //                 --scan . \
        //                 --format "HTML" \
        //                 --format "JSON" \
        //                 --out dependency-check-report \
        //                 --data /var/jenkins_home/dependency-check-data \
        //                 --nvdApiKey "$NVD_API_KEY" \
        //                 --disableAssembly
        //             '''
        //         }
        //     }
        //     post {
        //         always {
        //             archiveArtifacts artifacts: 'dependency-check-report/**', fingerprint: true, allowEmptyArchive: true
        //         }
        //     }
        // }

        stage('SonarQube Analysis') {
            steps {
                withCredentials([string(credentialsId: 'sonarqube-token', variable: 'SONAR_TOKEN')]) {
                    sh '''
                        dotnet tool install --global dotnet-sonarscanner || true
                        export PATH="$PATH:/root/.dotnet/tools:/var/jenkins_home/.dotnet/tools"

                        dotnet sonarscanner begin \
                          /k:"demo-api" \
                          /n:"Demo API" \
                          /d:sonar.host.url="${SONAR_HOST_URL}" \
                          /d:sonar.token="${SONAR_TOKEN}"

                        dotnet build $DOTNET_PROJECT --configuration Release

                        dotnet sonarscanner end \
                          /d:sonar.token="${SONAR_TOKEN}"
                    '''
                }
            }
        }

        stage('Docker Build') {
            steps {
                sh '''
                    docker build \
                      -t ${IMAGE_LOCAL} \
                      -f src/DemoApi/Dockerfile .
                '''
            }
        }

        stage('Trivy Scan') {
            steps {
                sh '''
                    trivy image \
                      --severity HIGH,CRITICAL \
                      --exit-code 1 \
                      --ignore-unfixed \
                      ${IMAGE_LOCAL}
                '''
            }
        }

        stage('Docker Push') {
            steps {
                sh '''
                    docker push ${IMAGE_LOCAL}
                '''
            }
        }

        stage('Update Kubernetes Manifest') {
            steps {
                withCredentials([usernamePassword(
                    credentialsId: 'github-credentials',
                    usernameVariable: 'GIT_USER',
                    passwordVariable: 'GIT_TOKEN'
                )]) {
                    sh '''
                        git config user.email "jenkins@local.dev"
                        git config user.name "Jenkins CI"

                        sed -i "s|image: .*demo-api:.*|image: ${IMAGE_CLUSTER}|g" k8s/deployment.yaml

                        git add k8s/deployment.yaml
                        git commit -m "ci: deploy demo-api image ${IMAGE_TAG}" || echo "No changes to commit"

                        git push https://${GIT_USER}:${GIT_TOKEN}@github.com/${GIT_USER}/dotnet-devsecops-lab.git HEAD:main
                    '''
                }
            }
        }

        stage('Verify Argo CD / Kubernetes') {
            steps {
                sh '''
                    kubectl rollout status deployment/demo-api -n demo-api --timeout=180s
                    kubectl get pods -n demo-api
                    kubectl get svc -n demo-api
                '''
            }
        }
    }

    post {
        success {
            emailext(
                subject: "Deploy exitoso: demo-api #${BUILD_NUMBER}",
                body: """
                Deploy exitoso.

                App: demo-api
                Build: #${BUILD_NUMBER}
                Imagen: ${IMAGE_CLUSTER}

                Revisar:
                - Jenkins: ${BUILD_URL}
                - Argo CD: https://localhost:8081
                - Grafana: http://localhost:3000
                """,
                to: "devops-lab@local.dev",
                mimeType: "text/plain"
            )
        }

        failure {
            emailext(
                subject: "Deploy fallido: demo-api #${BUILD_NUMBER}",
                body: """
                El pipeline falló.

                App: demo-api
                Build: #${BUILD_NUMBER}
                Jenkins: ${BUILD_URL}
                """,
                to: "devops-lab@local.dev",
                mimeType: "text/plain"
            )
        }

        always {
            archiveArtifacts artifacts: 'coverage/**, dependency-check-report/**', allowEmptyArchive: true
        }
    }
}