# PR Reviewer Appointment Service

Микросервис для автоматического назначения ревьюеров на Pull Request'ы и управления командами.

## Возможности

- Управление пользователями  
- Управление командами  
- Создание PR с автоматическим назначением ревьюверов  
- Переназначение ревьюверов  
- Merge PR (идемпотентно)  
- Просмотр статистики

## Технологии

- C# (.NET 9)  
- PostgreSQL  
- Entity Framework Core  
- ASP.NET Core Web API  
- Swagger/OpenAPI

## Быстрый запуск

### Docker Compose
```bash
docker-compose up --build
