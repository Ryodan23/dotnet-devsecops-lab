FROM jenkins/jenkins:lts-jdk17

USER root

RUN apt-get update && apt-get install -y \
    curl \
    wget \
    unzip \
    git \
    docker.io \
    apt-transport-https \
    ca-certificates \
    gnupg \
    lsb-release \
    && rm -rf /var/lib/apt/lists/*

# Install .NET SDK 8
ENV DOTNET_ROOT=/usr/share/dotnet
ENV PATH="${PATH}:/usr/share/dotnet:/usr/share/dotnet/tools"

RUN mkdir -p /usr/share/dotnet \
    && curl -sSL https://dot.net/v1/dotnet-install.sh -o dotnet-install.sh \
    && chmod +x dotnet-install.sh \
    && ./dotnet-install.sh --channel 8.0 --install-dir /usr/share/dotnet \
    && rm dotnet-install.sh \
    && ln -s /usr/share/dotnet/dotnet /usr/bin/dotnet

# Install kubectl
RUN curl -LO "https://dl.k8s.io/release/v1.30.0/bin/linux/amd64/kubectl" \
    && install -o root -g root -m 0755 kubectl /usr/local/bin/kubectl \
    && rm kubectl

# Install Helm
RUN curl https://raw.githubusercontent.com/helm/helm/main/scripts/get-helm-3 | bash

# Install Trivy
RUN wget https://github.com/aquasecurity/trivy/releases/download/v0.71.2/trivy_0.71.2_Linux-64bit.deb \
    && dpkg -i trivy_0.71.2_Linux-64bit.deb \
    && rm trivy_0.71.2_Linux-64bit.deb

# Install OWASP Dependency Check
RUN mkdir -p /opt/dependency-check \
    && wget https://github.com/dependency-check/DependencyCheck/releases/download/v10.0.3/dependency-check-10.0.3-release.zip \
    && unzip dependency-check-10.0.3-release.zip -d /opt \
    && rm dependency-check-10.0.3-release.zip \
    && ln -s /opt/dependency-check/bin/dependency-check.sh /usr/local/bin/dependency-check

COPY plugins.txt /usr/share/jenkins/ref/plugins.txt

RUN jenkins-plugin-cli --plugin-file /usr/share/jenkins/ref/plugins.txt

USER jenkins