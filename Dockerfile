FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /SRC

COPY ChatSpark.slnx .
COPY ChatSpark.Api/ChatSpark.Api.csproj ChatSpark.Api/
COPY ChatSpark.Application/ChatSpark.Application.csproj ChatSpark.Application/
COPY ChatSpark.Domain/ChatSpark.Domain.csproj ChatSpark.Domain/
COPY ChatSpark.Infrastructure/ChatSpark.Infrastructure.csproj ChatSpark.Infrastructure/
COPY ChatSpark.Shared/ChatSpark.Shared.csproj ChatSpark.Shared/

RUN dotnet restore


COPY . .
RUN dotnet publish ChatSpark.Api/ChatSpark.Api.csproj -c Release -o /app/publish --no-restore


FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "ChatSpark.Api.dll"]