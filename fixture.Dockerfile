FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /app
ADD CodeChallenge.sln CodeChallenge.sln
ADD CodeChallenge/CodeChallenge.csproj CodeChallenge/CodeChallenge.csproj
ADD CodeChallenge.Baseline/CodeChallenge.Baseline.csproj CodeChallenge.Baseline/CodeChallenge.Baseline.csproj
RUN dotnet restore
ADD . .
RUN dotnet build --configuration release

FROM build as publish-fixture
RUN dotnet publish --configuration release --output /app/publish CodeChallenge/CodeChallenge.csproj

FROM build as publish-baseline
RUN dotnet publish --configuration release --output /app/publish CodeChallenge.Baseline/CodeChallenge.Baseline.csproj

FROM mcr.microsoft.com/dotnet/core/runtime:3.1 AS fixture
WORKDIR /app/CodeChallenge
COPY --from=publish-fixture /app/publish /app/CodeChallenge
COPY --from=publish-baseline /app/publish /app/CodeChallenge.Baseline
ENTRYPOINT [ "/app/CodeChallenge/CodeChallenge" ]   