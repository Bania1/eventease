# 🗓 EventEase - Aplicación Web de Gestión y Reserva de Eventos

![.NET](https://img.shields.io/badge/.NET-6-blue.svg)
![C#](https://img.shields.io/badge/C%23-8.0-blue.svg)
![SQL Server](https://img.shields.io/badge/SQL_Server-express-lightgrey.svg)
![License](https://img.shields.io/badge/license-MIT-green.svg)

---

## 🚀 Descripción

EventEase es una plataforma web desarrollada en ASP.NET Core que permite a los usuarios reservar eventos y a los organizadores gestionar todos los aspectos de sus eventos. Incluye gestión de usuarios, tickets, notificaciones, y transacciones con autenticación segura y roles administrativos.

---

## 🎯 Características principales

- Gestión completa de eventos: crear, editar, listar y eliminar.
- Sistema de autenticación y autorización basado en cookies.
- CRUD para Usuarios, Invitados, Tickets, Notificaciones y Transacciones.
- Generación y gestión de códigos QR para tickets.
- Panel administrativo para monitorear y controlar la plataforma.
- Seguridad reforzada con hashing de contraseñas personalizado.
- Manejo avanzado de transacciones y pagos.
- Middleware para manejo de errores y experiencia de usuario.

---

## 🛠 Tecnologías

- ASP.NET Core MVC  
- Entity Framework Core  
- Microsoft SQL Server  
- Autenticación basada en Cookies  
- C#  
- HTML5, CSS3 y JavaScript para frontend

---

## ⚙️ Instalación y Ejecución

Sigue estos pasos para ejecutar localmente:

Clona el repositorio
git clone https://github.com/Bania1/eventease.git

Navega al proyecto
cd eventease

Configura la cadena de conexión en el archivo de configuración (appsettings.json)
Restaura dependencias
dotnet restore

Ejecuta la aplicación
dotnet run

text

---

## 🧩 Arquitectura

El proyecto sigue el patrón Modelo-Vista-Controlador (MVC), con modelos que reflejan las entidades principales: Eventos, Usuarios, Tickets, Transacciones, Notificaciones. Se usa Entity Framework Core para la gestión de datos y migraciones hacia SQL Server.

Se implementa seguridad mediante autenticación de cookies y un servicio de hashing para contraseñas.

---

## 🤝 Contribuciones

¡Las contribuciones son bienvenidas! Puedes mejorar funcionalidades, corregir errores o implementar nuevas características. Por favor, abre un issue antes de hacer pull requests para discutir cambios importantes.

---

## 📞 Contacto

Si tienes preguntas o sugerencias, abre un issue en el repositorio o contáctame a través de GitHub.

---

> _"Organiza eventos fácilmente. Gestiona usuarios, tickets y pagos con EventEase."_ 🎉
