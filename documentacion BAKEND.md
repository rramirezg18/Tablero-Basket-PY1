#  PROYECTO 1 - DESARROLLO WEB 

## 🏀 MARCADOR DE BALONCESTO🏀

---

##  Proyecto desarrollado por:

- **Roberto Antonio Ramirez Gomez** — **7690-22-12700**  
- **Jean Klaus Castañeda Santos** — **7690-22-892** 
- **Jonathan Joel Chan Cuellar** — **7690-22-1805**  

---
#  Documentación Técnica – Backend *(Tablero Basket)*

##  Introducción
El backend de Tablero Basketball está desarrollado en ASP.NET Core (v8.0) y expone una API REST ful que provee los servicios para la gestión de equipos, partidos y resultados en tiempo real.  
El sistema está diseñado para integrarse con el frontend Angular y utiliza SQL Server 2022 como base de datos relacional.  
El despliegue y ejecución se gestiona con Docker Compose para simplificar la infraestructura.

---

## ️ Arquitectura General

- **Tipo:** Monolito modular.  
- **Patrones de diseño:** 
  - **MVC (Model-View-Controller)** → en controladores.  
  - **Repository Pattern** → en capa `Data`.  
  - **Service Layer** → para lógica de negocio en `Infrastructure`.  
  - **SignalR** → comunicación en tiempo real.  

### 🔹 Componentes Principales
- **API REST (Controllers)** → expone endpoints HTTP.  
- **Data Layer (Entity Framework Core)** → mapeo ORM y acceso a SQL Server.  
- **SignalR Hubs** → permite comunicación en tiempo real con el frontend.  
- **Models** → entidades y DTOs.  
- **Infrastructure** → configuración de servicios, autenticación, etc.

 La API está contenida en el proyecto:  
 
### estructura del bakend de nuestro proyecto 

```
server/Scoreboard.Api
 ├── Controllers/        # Define los endpoints (Teams, Matches, Scoreboard)
 ├── Data/               # DbContext y Repositorios (Entity Framework Core)
 ├── Hubs/               # Clases SignalR (para tiempo real)
 ├── Infrastructure/     # Servicios, configuración de dependencias
 ├── Models/             # Entidades y DTOs
 ├── Program.cs          # Punto de entrada, configuración inicial
 ├── appsettings.json    # Configuración (conexiones, logs, etc.)
 └── Properties/         # Configuración del proyecto
```

---

### Capas principales
- **Presentación** → `Controllers`  
- **Infraestructura** → `Data`, `Hubs`, `Infrastructure`  
- **Dominio** → `Models`

---

## ⚙️ Program.cs y Middleware
El archivo `Program.cs` inicializa y configura los servicios principales:

- **Swagger** → habilitado para documentar y probar la API.  
- **CORS** → configurado para permitir la conexión del frontend Angular.  
- **Entity Framework Core** → registro de `DbContext` para interactuar con SQL Server.  
- **SignalR** → mapeo del hub de tiempo real:
  ```csharp
  app.MapHub<ScoreboardHub>("/scoreHub");
  ```
---
## ️ Infrastructure

La carpeta **Infrastructure** contiene:

- **Servicios auxiliares**  
  Ejemplo: cálculo de tabla de posiciones.

- **Inyección de dependencias**  
  Para que los servicios y repositorios estén disponibles en los controladores.

- **Configuración de EF Core** y reglas de negocio.

---

##  Despliegue con Docker

El backend se despliega usando docker-compose:

- El contenedor de la API se ejecuta en el puerto `8080`.  
- El contenedor de la base de datos SQL Server se ejecuta en el puerto `1433`.  
- Los datos se almacenan en el volumen persistente `mssqldata`.

**Ejemplo de configuración:**

```yaml
services:
  db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    ports:
      - "1433:1433"
    environment:
      - ACCEPT_EULA=Y
      - MSSQL_SA_PASSWORD=YourStrong!Passw0rd
    volumes:
      - mssqldata:/var/opt/mssql

  api:
    build: ./server/Scoreboard.Api
    ports:
      - "8080:8080"
    depends_on:
      - db
```

---

## Validaciones y Manejo de Errores

El backend incluye varias validaciones importantes:

- No permitir equipos duplicados.  
- No registrar eventos en partidos finalizados.  
- Validar que los equipos existan antes de crear un partido.  
- Validar que los puntos en ScoreEvent sean válidos (`1`, `2` o `3`).  
- Validar que la fecha de inicio de un partido no sea en el pasado.

En caso de errores, se devuelven los siguientes códigos HTTP:

- `400 Bad Request` → errores de validación.  
- `404 Not Found` → recurso inexistente.  
- `500 Internal Server Error` → fallos del servidor.

##  Endpoints / APIs


- **Equipos**
  - `GET /api/teams` → Listar equipos.  
  - `POST /api/teams` → Crear un nuevo equipo.  
  - `PUT /api/teams/{id}` → Actualizar un equipo.  
  - `DELETE /api/teams/{id}` → Eliminar un equipo.  

- **Partidos**
  - `GET /api/matches` → Listar partidos.  
  - `POST /api/matches` → Crear un partido.  
  - `PUT /api/matches/{id}` → Actualizar partido.  
  - `DELETE /api/matches/{id}` → Eliminar partido.  

- **Marcador / Eventos en vivo**
  - `GET /api/scoreboard` → Consultar marcador actual.  
  - `POST /api/scoreboard/event` → Registrar evento (puntos, faltas, etc.).  

### Formato de respuesta
- JSON por defecto.  

Ejemplo:
```json
{
  "id": 1,
  "teamName": "Los Ángeles",
  "victories": 5
}
```

## Códigos de Estado HTTP
- **200 OK** → petición exitosa.  
- **201 Created** → recurso creado.  
- **400 Bad Request** → error de validación.  
- **404 Not Found** → recurso inexistente.  
- **500 Internal Server Error** → error del servidor.  

## ️ Base de Datos
- **Motor:** SQL Server 2022 (Docker)  
- **Conexión:** definida en `appsettings.json` como `DefaultConnection`  
- **ORM:** Entity Framework Core  

### Esquema esperado
- **Teams** → información de equipos  
- **Matches** → información de partidos (fecha, equipos, estado)  
- **ScoreEvents** → registro de eventos (puntos, faltas, etc.)  

### Notas importantes
-  Migraciones se gestionan con `dotnet ef migrations`  
- Volumen persistente configurado en `docker-compose.yml`  

##  Lógica de Negocio

### Servicios principales
- Gestión de equipos  
- Programación de partidos  
- Registro de puntos y estadísticas en tiempo real  

### Procesos críticos
- Actualización del marcador en vivo con SignalR
- Cálculo de tabla de posiciones basado en victorias  


---


 **Explicación de como funcionaria:**

- Usuario accede al Frontend Angular desde el navegador.  
- El Frontend Angular consume la API REST y se conecta en tiempo real al Backend ASP.NET Core mediante SignalR.  
- El Backend ASP.NET Core gestiona la lógica de negocio y se conecta a SQL Server 2022 mediante Entity Framework Core.  
- Todo está orquestado con Docker Compose (contenedor para la API y contenedor para la base de datos).  

---


