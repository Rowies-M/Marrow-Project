# Marrow.API.PostgresTests

Pruebas de integración que se conectan a PostgreSQL real, solo a la base `marrow_test`.

Este proyecto **no pertenece a `Marrow.sln`**, así que `dotnet test backend/Marrow.sln` no lo ejecuta. La suite
aislada `Marrow.API.IntegrationTests` lo compila, pero no se enlaza con él ni lo invoca.

## Ejecución

Requiere autorización expresa, la variable `MARROW_TEST_CONNECTION` y las protecciones de
`Marrow.API.IntegrationTests.Infraestructura`.

```bash
dotnet test backend/Marrow.API.PostgresTests
```

Para comprobar solo que compila, sin ejecutarlas:

```bash
dotnet build backend/Marrow.API.PostgresTests
```
