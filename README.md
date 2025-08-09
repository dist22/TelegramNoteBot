# TelegramNoteBot

A simple Telegram bot for creating, viewing, sorting, and deleting notes with tag support.

## Features
- Add and edit notes
- Sort notes by date (ascending/descending)
- Filter notes by tags
- Delete notes and tags
- Uses Redis for caching callback data
- Stores data in PostgreSQL

## Requirements
- .NET 8.0
- PostgreSQL
- Redis
- Telegram Bot Token

## Quick Start

1. Clone the repository:
    ```
    git clone https://github.com/your-username/TelegramNoteBot.git
    cd TelegramNoteBot
    ```

2. Set your bot token in `appsettings.json`.

3. Configure database connection in `appsettings.json`:
    ```json
    "ConnectionStrings": {
        "Redis" : "redis:YOU_PORT",
        "Connection" : "Host=YOU_HOST;Port=YOU_PORT;Database=YOU_DATABASE;Username=YOU_USERNAME;Password=YOU_PASSWORD"
      },
      "Telegram": {
        "Token" : "YOU_BOT_TOKEN"
      }
    ```

4. Start PostgreSQL and Redis locally or using Docker.

5. Run the bot:
    ```
    dotnet run --project TelegramNoteBot
    ```

## Running with Docker

1. **Clone the repository**  
   ```bash
   git clone https://github.com/yourusername/telegram-note-bot.git
   cd telegram-note-bot
   ```

- Edit docker-compose.yml
    Update the environment section in the bot service with your real bot token and database credentials:
    ```bash
    version: "3.9"

    services:
      bot:
        build:
          context: .
          dockerfile: TelegramNoteBot/Dockerfile
        container_name: telegram_note_bot
        depends_on:
          - db
          - redis
        environment:
          ConnectionStrings__Connection: Host=db;Port=5432;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}
          Redis__Host: redis
          Redis__Port: ${REDIS_PORT}
          BotConfiguration__Token: ${BOT_TOKEN}
        restart: unless-stopped
    
      db:
        image: postgres:latest
        container_name: telegram_note_pg
        environment:
          POSTGRES_USER: ${POSTGRES_USER}
          POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
          POSTGRES_DB: ${POSTGRES_DB}
        ports:
          - "${POSTGRES_PORT}:5432"
        volumes:
          - pgdata:/var/lib/postgresql/data
    
      redis:
        image: redis:7-alpine
        container_name: telegram_note_redis
        restart: unless-stopped
        ports:
          - "${REDIS_PORT}:6379"
        volumes:
          - redis_data:/data
        command: redis-server --appendonly yes
    
    volumes:
      pgdata:
        driver: local
      redis_data:
        driver: local
    ```
  - Build and start services
    ```bash
    docker compose up --build -d
    ```

## Contacts
- Developer: Dmytro Stozhok  
- GitHub: [https://github.com/dist22](https://github.com/dist22)
