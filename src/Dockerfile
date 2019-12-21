FROM mcr.microsoft.com/dotnet/core/sdk:3.0

# Tests need GCC to run
RUN apt-get update && \
    apt-get -y install build-essential gcc

WORKDIR /app

COPY ./ ./

CMD dotnet test