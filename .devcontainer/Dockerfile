FROM mcr.microsoft.com/devcontainers/dotnet:9.0

RUN apt-get update && export DEBIAN_FRONTEND=noninteractive \
&& apt-get -y install --no-install-recommends python3-pip \
&& pip3 install --break-system-packages jprm

WORKDIR /workspaces/
RUN apt-get update && apt-get install -y curl && \
    curl -fsSL https://deb.nodesource.com/setup_20.x | bash - && \
    apt-get install -y nodejs && \
    apt-get clean
RUN node -v && npm -v
RUN apt install extrepo -y
RUN apt update
RUN extrepo enable jellyfin
RUN apt update
RUN apt install jellyfin -y
RUN apt install ffmpeg -y

RUN git clone --depth 1 --branch v10.10.5 https://github.com/jellyfin/jellyfin
RUN git clone --depth 1 --branch v10.10.5 https://github.com/jellyfin/jellyfin-web
RUN mkdir -p /workspaces/jellyfin-data/
RUN chown vscode /workspaces/jellyfin-data
WORKDIR /workspaces/jellyfin-web

RUN npm install
RUN npm run build:development