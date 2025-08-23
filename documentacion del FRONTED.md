#  PROYECTO 1 - DESARROLLO WEB 

## 🏀 MARCADOR DE BALONCESTO🏀

---

##  Proyecto desarrollado por:

- **Roberto Antonio Ramirez Gomez** — **7690-22-12700**  
- **Jean Klaus Castañeda Santos** — **7690-22-892** 
- **Jonathan Joel Chan Cuellar** — **7690-22-1805**  

---

#  Documentación Técnica – Frontend Angular *(Tablero Basket)*

##  Descripción General
El frontend de Tablero Basket está desarrollado con Angular, una plataforma moderna para la creación de aplicaciones web SPA (Single Page Applications).  
Este cliente se encarga de la interfaz gráfica y la interacción del usuario con el sistema de gestión de partidos, resultados y estadísticas de baloncesto.  

La aplicación consume la API desarrollada en ASP.NET Core (ubicada en `server/Scoreboard.Api`) y se comunica con ella a través de HTTP y SignalR para funcionalidades en tiempo real.

---

##  Tecnologías y Paquetes Utilizados

###  Lenguajes y Frameworks
- **Angular (15+)** → Framework SPA principal.  
- **TypeScript (4+)** → Lenguaje tipado sobre JavaScript.  
- **RxJS** → Programación reactiva y flujos de datos.  
- **HTML5** → Estructura de la UI.  
- **CSS3 / SCSS** → Estilos y diseño responsivo.  

###  Librerías de Estilos y UI
- **Bootstrap 5** → Grillas y componentes UI.  
- **Angular Material** (si está en dependencias) → UI moderna y consistente.  
- **FontAwesome / Material Icons** → Iconografía escalable.  
- **SCSS (Sass)** (opcional) → Preprocesador para modularizar CSS.  

### ️ Herramientas de Desarrollo
- **Node.js (16+)** y **npm (8+)** → Ejecución y gestión de paquetes.  
- **Angular CLI** → Creación y gestión de módulos, componentes y builds.  
- **Visual Studio Code** → Editor recomendado.  
- **Git** → Control de versiones.  

###  Comunicación con Backend
- **HttpClient Angular** → Consumo de API REST.  
- **SignalR Client** → Comunicación en tiempo real con el backend.  

---

##  Estructura del Proyecto

El frontend está en:
```
public/                  // Archivos estáticos (favicon, logos, etc.)
src/
 └── app/
     ├── components/     // Componentes reutilizables
     ├── pages/          // Vistas principales (equipos, partidos, marcador)
     ├── services/       // Servicios API/SignalR
     ├── models/         // Interfaces y tipado TS
     ├── guards/         // Guards para rutas
     ├── pipes/          // Pipes personalizados
     └── app.module.ts   // Módulo raíz
 ├── assets/             // Imágenes, JSON, CSS globales
 ├── environments/       // Configuración de entornos
 ├── main.ts             // Punto de arranque
 ├── styles.css          // Estilos globales
 ├── angular.json        // Configuración Angular CLI
 ├── package.json        // Dependencias del frontend
 ├── proxy.conf.json     // Proxy para llamadas API
 └── tsconfig.json       // Configuración TypeScript
 ```
---

##  Instalación y Configuración

### 1️ Requisitos previos
- Node.js v16 o superior  
- npm v8 o superior  
- Angular CLI global:
 ```
  npm install -g @angular/cli
 ```
### 2⃣ Instalación de dependencias
Desde la carpeta client/scoreboard:
```bash
npm install
```

### 3⃣ Ejecución en desarrollo
```bash
ng serve
```
 http://localhost:4200

### 4⃣ Compilación para producción
```bash
ng build --configuration production
```
Los archivos compilados se generan en la carpeta dist/.

---
##  Conexión con el Backend

La aplicación Angular se conecta al backend ASP.NET Core mediante un proxy de desarrollo (`proxy.conf.json`):

```json
{
  "/api": {
    "target": "http://localhost:8080",
    "secure": false
  }
}
```

Esto permite consumir rutas como `/api` desde Angular sin problemas de CORS

---

El frontend también usa SignalR Hubs para recibir información en tiempo real:

- ✅ Resultados de partidos  
- ✅ Estadísticas de jugadores  
- ✅ Tabla de posiciones  

---

## ️ Arquitectura del Código

### 🔹 1. **Módulos**
- **`app.module.ts`** → Módulo raíz que agrupa componentes, servicios y dependencias.
- Posibilidad de dividir en módulos por dominio para Lazy Loading:
  - `TeamsModule`
  - `MatchesModule`
  - `ScoreboardModule`

---

### 🔹 2. **Componentes**
Cada vista se implementa como un componente de Angular.

**Ejemplos:**
- `scoreboard.component.ts` → Vista del marcador.
- `teams.component.ts` → Gestión de equipos.
- `matches.component.ts` → Gestión de partidos.

---

### 🔹 3. **Servicios**
Encargados de la comunicación con el backend:

- `scoreboard.service.ts` → API REST + SignalR para marcador.
- `teams.service.ts` → CRUD de equipos.
- `matches.service.ts` → CRUD de partidos.

---

### 🔹 4. **Enrutamiento**
Definido en `app-routing.module.ts`.

**Rutas principales:**
- `/` → Inicio
- `/teams` → Equipos
- `/matches` → Partidos
- `/scoreboard` → Marcador

---

##  Estilos y Diseño

### 🔹 Archivos de Estilos
- `src/styles.css` → Estilos globales (tipografía, variables de color, resets).
- `.css` o `.scss` por componente → Estilos encapsulados que no afectan otras vistas.
- Archivos en `src/assets/` → Fuentes personalizadas, imágenes y hojas CSS adicionales.

---

### 🔹 Librerías de Estilos
- **Bootstrap 5** → Diseño responsivo (grids, tarjetas, botones).
- **Angular Material** → Tablas, inputs, diálogos, toolbars.
- **FontAwesome / Material Icons** → Íconos.

**Ejemplo:**
```html
<div class="container">
  <div class="row">
    <div class="col-md-6">Equipo A</div>
    <div class="col-md-6">Equipo B</div>
  </div>
</div>
```

---
## Buenas Prácticas Implementadas

- **Separación de responsabilidades**: componentes, servicios y modelos claramente definidos.
- **Interfaces TypeScript** para tipado fuerte y mayor robustez.
- **Variables de entorno** mediante `environments.ts`.
- **Proxy de desarrollo** para evitar problemas de CORS.
- **Bootstrap Grid System** para diseño responsivo.
- Preparación para Lazy Loading en módulos.
- **Encapsulación de estilos** por componente para evitar conflictos globales.


