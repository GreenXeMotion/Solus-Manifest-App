# API Documentation

## Game Library Endpoint

### `GET /api/v1/library`

Retrieve the list of games available in the database with manifest information.

**Authentication:** Required (API Key via Bearer token, X-API-Key header, or api_key query param)

**Query Parameters:**

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `limit` | integer | 100 | Number of games to return per page |
| `offset` | integer | 0 | Number of games to skip (for pagination) |
| `search` | string | null | Search term to filter by game name or game ID |
| `sort_by` | string | "updated" | Sort order: `"updated"` (by upload date, newest first) or `"name"` (alphabetical) |

**Example Requests:**

```bash
# Get first 100 games sorted by upload date (newest first)
GET /api/v1/library?limit=100&offset=0

# Get next page
GET /api/v1/library?limit=100&offset=100

# Search for specific game
GET /api/v1/library?search=counter

# Sort by name alphabetically
GET /api/v1/library?sort_by=name

# Combine options
GET /api/v1/library?search=team&sort_by=name&limit=50
```

**Response Headers:**

The API includes RFC 5988 `Link` headers for pagination:

```
Link: <url>; rel="next", <url>; rel="prev", <url>; rel="first", <url>; rel="last"
```

- `rel="next"` - Link to the next page (if available)
- `rel="prev"` - Link to the previous page (if available)
- `rel="first"` - Link to the first page
- `rel="last"` - Link to the last page

**Response Format:**

```json
{
  "status": "success",
  "total_count": 5432,
  "limit": 100,
  "offset": 0,
  "search": null,
  "sort_by": "updated",
  "games": [
    {
      "game_id": "730",
      "game_name": "Counter-Strike 2",
      "header_image": "https://cdn.cloudflare.steamstatic.com/steam/apps/730/header.jpg",
      "uploaded_date": "2025-10-06T15:30:00",
      "manifest_available": true,
      "manifest_size": 1048576,
      "manifest_updated": "2025-10-06T19:30:00"
    },
    {
      "game_id": "440",
      "game_name": "Team Fortress 2",
      "header_image": "https://cdn.cloudflare.steamstatic.com/steam/apps/440/header.jpg",
      "uploaded_date": "2025-10-05T12:20:00",
      "manifest_available": false,
      "manifest_size": null,
      "manifest_updated": null
    }
  ],
  "timestamp": "2025-10-06T19:45:00"
}
```

**Response Fields:**

| Field | Type | Description |
|-------|------|-------------|
| `game_id` | string | Steam App ID |
| `game_name` | string | Name of the game |
| `header_image` | string | URL to game's header image from Steam CDN |
| `uploaded_date` | string (ISO 8601) | Date when game was added to the database |
| `manifest_available` | boolean | Whether manifest file exists |
| `manifest_size` | integer/null | Size of manifest file in bytes (null if not available) |
| `manifest_updated` | string (ISO 8601)/null | Last modification date of manifest file (null if not available) |

**Use Cases:**

- **Building a game library UI:** Use pagination with `limit` and `offset` to load games progressively
- **Showing latest additions:** Default `sort_by=updated` shows newest games first
- **Search functionality:** Filter games by name or ID using the `search` parameter
- **Alphabetical browsing:** Use `sort_by=name` for alphabetical game lists

---

## Search Endpoint

### `GET /api/v1/search`

Search for games by name or game ID. Returns matching results sorted by upload date (newest first).

**Authentication:** Required (API Key via Bearer token, X-API-Key header, or api_key query param)

**Query Parameters:**

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `q` | string | required | Search query (minimum 2 characters) - searches game name and game ID |
| `limit` | integer | 50 | Maximum number of results to return |

**Example Requests:**

```bash
# Search for "counter"
GET /api/v1/search?q=counter

# Search with custom limit
GET /api/v1/search?q=team&limit=25

# Search by game ID
GET /api/v1/search?q=440
```

**Response Headers:**

If there are more results than the limit, the API includes a `Link` header:

```
Link: <url>; rel="all"
```

- `rel="all"` - Link to retrieve all matching results (sets limit to total_matches)

**Response Format:**

```json
{
  "status": "success",
  "query": "counter",
  "total_matches": 15,
  "returned_count": 15,
  "limit": 50,
  "results": [
    {
      "game_id": "730",
      "game_name": "Counter-Strike 2",
      "header_image": "https://cdn.cloudflare.steamstatic.com/steam/apps/730/header.jpg",
      "uploaded_date": "2025-10-06T15:30:00",
      "manifest_available": true
    },
    {
      "game_id": "10",
      "game_name": "Counter-Strike",
      "header_image": "https://cdn.cloudflare.steamstatic.com/steam/apps/10/header.jpg",
      "uploaded_date": "2025-09-20T10:15:00",
      "manifest_available": true
    }
  ],
  "timestamp": "2025-10-06T19:45:00"
}
```

**Response Fields:**

| Field | Type | Description |
|-------|------|-------------|
| `query` | string | The search term used |
| `total_matches` | integer | Total number of games matching the search |
| `returned_count` | integer | Number of results returned (capped by limit) |
| `results` | array | Array of matching games |
| `game_id` | string | Steam App ID |
| `game_name` | string | Name of the game |
| `header_image` | string | URL to game's header image from Steam CDN |
| `uploaded_date` | string (ISO 8601) | Date when game was added to the database |
| `manifest_available` | boolean | Whether manifest file exists for this game |

**Error Responses:**

- `400 Bad Request` - Search query is less than 2 characters
- `401 Unauthorized` - Invalid or missing API key
- `429 Too Many Requests` - Rate limit exceeded

**Use Cases:**

- **Quick search functionality:** Users can type a game name or ID to find specific games
- **Autocomplete:** Use with low limit (e.g., 10) for real-time search suggestions
- **Finding games by ID:** Search directly by Steam App ID (e.g., "440" for Team Fortress 2)

---

## Using Link Headers

Both endpoints return standard RFC 5988 `Link` headers for navigation. Most HTTP clients can parse these automatically.

**Example Link Header:**
```
Link: </api/v1/library?limit=100&offset=100>; rel="next",
      </api/v1/library?limit=100&offset=0>; rel="first",
      </api/v1/library?limit=100&offset=5400>; rel="last"
```

**Parsing in JavaScript:**
```javascript
const response = await fetch('/api/v1/library?limit=100');
const linkHeader = response.headers.get('Link');

// Parse links
const links = {};
if (linkHeader) {
  linkHeader.split(',').forEach(part => {
    const match = part.match(/<([^>]+)>;\s*rel="([^"]+)"/);
    if (match) {
      links[match[2]] = match[1];
    }
  });
}

console.log(links.next); // URL for next page
```

**Parsing in Python:**
```python
import requests

response = requests.get('/api/v1/library?limit=100')
link_header = response.headers.get('Link')

# Parse links
links = {}
if link_header:
    for part in link_header.split(','):
        url = part.split(';')[0].strip('<> ')
        rel = part.split('rel="')[1].split('"')[0]
        links[rel] = url

print(links.get('next'))  # URL for next page
```
