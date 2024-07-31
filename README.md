# Notification Service with Rate Limiting

## Overview

This project implements a notification service with rate limiting using .NET 8. Notifications are sent based on specific rate limit rules, and any excess notifications are queued and processed in the background. The system uses `Polly` for rate limiting, `BlockingCollection` for managing the queue, and `IHostedService` for background processing.

## Features

- Rate limit notifications based on type and recipient.
- Queue notifications when rate limits are exceeded.
- Background processing of queued notifications.
- Configurable rate limits via `appsettings.json`.

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Installation

1. **Clone the repository:**

   ```bash
   git clone https://github.com/mariogit08/notificationservice.git
   cd notificationservice
2. **Restore an build:**
```
dotnet restore
dotnet build
```
3. **Rate Limit configuration:**
   
***appsettings.json***

```
{
  "RateLimitOptions": {
    "Policies": {
      "Status": {
        "Limit": 2,
        "Period": "00:01:00"
      },
      "News": {
        "Limit": 1,
        "Period": "1.00:00:00"
      },
      "Marketing": {
        "Limit": 3,
        "Period": "01:00:00"
      }
    }
  }
}

```
**Running the Application**

```dotnet run```

**Example of Request:**

```curl -X POST "http://localhost:5000/api/notification" -H "Content-Type: application/json" -d '{"type": "News", "userId": "user1", "message": "news update"}'```

**Testing**

```dotnet test```
