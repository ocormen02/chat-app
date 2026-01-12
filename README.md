# ChatApp - Chat Application with Stock Bot

A browser-based chat application  built with .NET, RabbitMQ, SignalR, and React.js, featuring real-time messaging and stock quote commands via a decoupled bot service.


## Technology Stack

### Backend
- .NET 8.0
- ASP.NET Core Web API
- SignalR for real-time communication
- Entity Framework Core 8.0 with SQL Server
- ASP.NET Core Identity
- RabbitMQ.Client 6.8.1
- Serilog for logging

### Frontend
- React 18 with TypeScript
- Material-UI (MUI) v6
- SignalR client
- Axios for HTTP requests
- React Router for navigation

### Testing
- xUnit
- Moq
- FluentAssertions
- Entity Framework Core InMemory for testing

## Prerequisites

- .NET 8.0 SDK
- Node.js 18+ and npm
- Docker Desktop (for SQL Server and RabbitMQ)
- Visual Studio 2022 or VS Code (optional)

# Setup Instructions

### 1. Start Infrastructure Services (Docker)

```bash
docker-compose up -d
```

This will start:
- SQL Server on port 1433
- RabbitMQ on ports 5672 (AMQP) and 15672 (Management UI)

**RabbitMQ Management UI**: http://localhost:15672
- Username: `guest`
- Password: `guest`

### 2. Database Setup

```bash
cd backend/ChatApp.Api
dotnet ef database update --project ../ChatApp.Infrastructure
```

If you need to create a new migration, run this command:

```bash
dotnet ef migrations add InitialCreate --project ../ChatApp.Infrastructure --startup-project .
```

### 3. Setup and Run Frontend

```bash
cd frontend
npm install
```

### 4. Run Backend Services

#### API Service
```bash
cd backend/ChatApp.Api
dotnet run
```

The API will be available at:
- HTTP: http://localhost:5283 (default `http` profile)
- HTTPS: https://localhost:7150 (when using `https` profile, also includes HTTP on port 5283)


#### Bot Service (Separate Terminal)
```bash
cd backend/ChatApp.Bot
dotnet run
```

**Note:** ChatApp.Bot is a Worker Service (Background Service) that:
- Does NOT expose HTTP endpoints
- Runs as a background service consuming RabbitMQ queues
- Does not require a port configuration


### 4. Run Frontend 

```bash
cd frontend
npm run dev
```
The frontend will be available at http://localhost:3000


**Access URLs:**
- Frontend: http://localhost:3000
- API (HTTP): http://localhost:5283
- API (HTTPS): https://localhost:7150
- Swagger UI: http://localhost:5283/swagger (when API is running)
- RabbitMQ Management: http://localhost:15672 (username: `guest`, password: `guest`)

## Usage

### Registration and Login

1. Navigate to http://localhost:3000
2. Register a new account or login
3. You'll be redirected to the chatrooms page

### Using the Chat

1. **Select or Create a Chatroom**: Click on a chatroom from the list or create a new one
2. **Send Messages**: Type a message and press Send
3. **Get Stock Quotes**: Use the command `/stock=CODE` where CODE is a stock symbol (e.g., `/stock=AAPL.US`)
   - The bot will fetch the stock quote from Stooq API
   - The response will appear in the chat as a bot message
   - Format: `"SYMBOL quote is $PRICE per share"`

### Stock Command Examples

- `/stock=AAPL.US` - Get Apple stock quote
- `/stock=MSFT.US` - Get Microsoft stock quote
- `/stock=GOOGL.US` - Get Google stock quote


## Testing

### Run Unit Tests

```bash
cd Tests

# Specific test project
dotnet test ChatApp.Application.Tests
dotnet test ChatApp.Bot.Tests
```

## Development Notes

### Database Migrations

```bash
# Create migration
dotnet ef migrations add MigrationName --project backend/ChatApp.Infrastructure --startup-project backend/ChatApp.Api

# Update database
dotnet ef database update --project backend/ChatApp.Infrastructure --startup-project backend/ChatApp.Api
```

### RabbitMQ Queues

The application uses the following queues:
- `stock-commands` - Queue for stock command requests
- `stock-responses` - Queue for bot responses
- `stock-commands-dlq` - Dead-letter queue for failed commands

### Bot Service

The bot service:
- Consumes from `stock-commands` queue
- Calls Stooq API: `https://stooq.com/q/l/?s={stock_code}&f=sd2t2ohlcv&h&e=csv`
- Parses CSV response
- Publishes to `stock-responses` queue
- Handles errors gracefully with DLQ support



## Author

Created by Olman Cordero Mendieta.
