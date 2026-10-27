# API Reference

All endpoints are accessed through the Gateway at `http://localhost:5000`.

Each service also exposes Swagger locally at its own port during Development:

| Service | Swagger URL |
|---|---|
| UserService | http://localhost:5001/swagger |
| CatalogService | http://localhost:5002/swagger |
| CommerceService | http://localhost:5003/swagger |
| AuctionService | http://localhost:5004/swagger |
| FulfillmentService | http://localhost:5005/swagger |
| NotificationService | http://localhost:5006/swagger |

## Auth

Endpoints protected with `[Authorize]` require a Bearer token in the `Authorization` header:

```
Authorization: Bearer <access_token>
```

Get a token from `POST /api/auth/login`. Rotate it with `POST /api/auth/refresh` using the `token` field.
