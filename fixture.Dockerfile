FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /app
ADD CodeChallenge/CodeChallenge.csproj CodeChallenge/CodeChallenge.csproj
ADD CodeChallenge.Baseline/CodeChallenge.Baseline.csproj CodeChallenge.Baseline/CodeChallenge.Baseline.csproj
RUN dotnet restore CodeChallenge/CodeChallenge.csproj
RUN dotnet restore CodeChallenge.Baseline/CodeChallenge.Baseline.csproj
ADD . .

FROM build as publish-fixture
RUN dotnet publish --configuration release --runtime linux-x64 --self-contained --output /app/publish CodeChallenge/CodeChallenge.csproj

FROM build as publish-baseline
RUN dotnet publish --configuration release --runtime linux-x64 --self-contained --output /app/publish CodeChallenge.Baseline/CodeChallenge.Baseline.csproj

# Replace this with an appropriate build environment for your target application
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS publish-target
WORKDIR /app
ADD CodeChallenge.Target/CodeChallenge.Target.csproj CodeChallenge.Target/CodeChallenge.Target.csproj
RUN dotnet restore CodeChallenge.Target/CodeChallenge.Target.csproj
ADD . .
RUN dotnet publish --configuration release --runtime linux-x64 --self-contained --output /app/publish CodeChallenge.Target/CodeChallenge.Target.csproj

# All the applications are self-contained, so you can safetly switch from the dotnet core runtime to any runtime image your implementation requires.
FROM mcr.microsoft.com/dotnet/core/runtime:3.1 AS fixture
WORKDIR /app/CodeChallenge
COPY --from=publish-fixture /app/publish /app/CodeChallenge
COPY --from=publish-baseline /app/publish /app/CodeChallenge/Baseline
COPY --from=publish-target /app/publish /app/CodeChallenge/Target
ENTRYPOINT [ "/app/CodeChallenge/CodeChallenge" ]
