# FarmCord

FarmCord is a farming bot for Discord written in C# using .NET 8, Discord.Net, MongoDB, and Docker.

Plant crops, collect rewards, generate farm images, and manage your own farming economy directly inside Discord.

---

## Features

- Slash command support (in progress / migration from prefix commands)
- MongoDB database support
- Farm image generation using ImageMagick
- Daily rewards system
- Custom server prefixes
- Owner-only commands
- Docker support
---

# Self Hosting

## Requirements

- Docker
- Docker Compose
- Discord Bot Token

Optional for local development:

- .NET 8 SDK
- MongoDB

---

# Creating a Discord Bot

1. Go to the Discord Developer Portal  
2. Create a new application  
3. Go to the Bot tab  
4. Create a bot user  
5. Enable required intents:
   - Message Content Intent (if using prefix commands)
   - Server Members Intent (optional depending on features)
6. Copy your bot token  

---

# Configuration

Create a `creds.json` file in the root directory:

```json
{
  "Token": "YOUR_BOT_TOKEN",
  "Prefix": "=>",
  "ClientID": "YOUR_CLIENT_ID",
  "BotVersion": 1,
  "OwnerID": "YOUR_USER_ID",
  "EmbedColor": 65280,
  "ErrorColor": 16711680
}
```

---

# Running with Docker

## Clone the repository

```bash
git clone https://github.com/amanogawa01/FarmCord.git
cd FarmCord
```

## Build and start the bot

```bash
docker compose up --build -d
```

## View logs

```bash
docker compose logs -f
```

## Stop the bot

```bash
docker compose down
```

---

# Running Without Docker

## Restore dependencies

```bash
dotnet restore
```

## Build the project

```bash
dotnet build
```

## Run the bot

```bash
dotnet run
```

---

# Assets

Farm images are generated using:

```text
FarmCord/Assets/
```

Generated farm images are stored in:

```text
FarmOutput/
```

---

# Project Structure

```text
FarmCord/
│  Program.cs
│  creds.cs
│
├─Assets
├─Modules
│  ├─General
│  └─Owner
└─Services
   └─Extensions
```


---

# License

This project is licensed under the Apache-2.0 License.