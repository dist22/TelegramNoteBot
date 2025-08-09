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
      "Connection": "Host=localhost;Port=YOU_PORT;Database=YOU_DB_NAME;Username=YOU_USERNAME;Password=YOU_PASSWORD"
    },
    "Redis": {
      "ConnectionString": "redis:YOU_PORT"
    }
    ```

4. Start PostgreSQL and Redis locally or using Docker.

5. Run the bot:
    ```
    dotnet run --project TelegramNoteBot
    ```

## Running with Docker

- Use `docker-compose.yml` to start PostgreSQL, Redis, and the bot together:
    ```bash
    docker-compose up --build
    ```

- Set the bot token via environment variable in your `docker-compose.yml`:
    ```yaml
    environment:
      - TelegramBot__Token=your_bot_token_here
    ```

## How to Use the Bot

- Send commands in Telegram:
    - `/add` — add a note
    - `/mynotes` — view notes
    - `/delete` — delete a note
    - `/filtertag` — filter notes by tag
    - `/tags` — manage tags

## Contacts
- Developer: Dmytro Stozhok  
- GitHub: [https://github.com/dist22](https://github.com/dist22)
