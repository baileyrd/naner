# {{PROJECT_NAME}}

A REST API built with Express.js, created with Naner.

## Getting Started

```bash
# Install dependencies
npm install

# Copy environment file
cp .env.example .env

# Start development server (with auto-reload)
npm run dev

# Start production server
npm start
```

## Tech Stack

- **Express.js** - Web framework
- **CORS** - Cross-origin resource sharing
- **dotenv** - Environment variable management

## API Endpoints

### Root
- `GET /` - API information and endpoints

### Health Check
- `GET /health` - Service health status

### Items (Sample CRUD)
- `GET /api/items` - Get all items
- `GET /api/items/:id` - Get single item
- `POST /api/items` - Create new item
- `PUT /api/items/:id` - Update item
- `DELETE /api/items/:id` - Delete item

## Project Structure

```
{{PROJECT_NAME}}/
├── src/
│   ├── index.js         # Application entry point
│   └── routes/
│       └── api.js       # API routes
├── .env.example         # Environment variables template
├── .gitignore
├── package.json
└── README.md
```

## Example Requests

### Get all items
```bash
curl http://localhost:3000/api/items
```

### Create item
```bash
curl -X POST http://localhost:3000/api/items \
  -H "Content-Type: application/json" \
  -d '{"name":"New Item","description":"A new item"}'
```

### Update item
```bash
curl -X PUT http://localhost:3000/api/items/1 \
  -H "Content-Type: application/json" \
  -d '{"name":"Updated Item"}'
```

### Delete item
```bash
curl -X DELETE http://localhost:3000/api/items/1
```

## Development

The server runs on `http://localhost:3000` by default. You can change this in `.env`.

## Learn More

- [Express.js Documentation](https://expressjs.com/)
- [Node.js Documentation](https://nodejs.org/docs/)
