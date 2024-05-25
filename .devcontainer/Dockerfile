FROM mcr.microsoft.com/devcontainers/dotnet:8.0

RUN apt-get update && export DEBIAN_FRONTEND=noninteractive \
&& apt-get -y install --no-install-recommends python3-pip \
&& pip3 install --break-system-packages jprm

WORKDIR /workspaces/
RUN git clone https://github.com/ankenyr/jellyfin-plugin-repo
RUN chown -R vscode jellyfin-plugin-repo/
RUN chgrp -R vscode jellyfin-plugin-repo/