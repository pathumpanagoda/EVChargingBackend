# EV Charging Station Booking System API

A comprehensive .NET 8 Web API for managing EV charging station bookings with MongoDB backend, JWT authentication, and business rule enforcement.

## Features

- **Authentication & Authorization**: JWT-based auth with roles (Backoffice, StationOperator, EVOwner)
- **MongoDB Integration**: Document-based storage with proper indexing
- **Business Rules**: 7-day booking window, 12-hour modification rule, slot availability
- **QR Code Integration**: Booking verification via QR codes
- **Geospatial Queries**: Find nearby charging stations
- **Comprehensive Validation**: FluentValidation for all DTOs
- **Error Handling**: Global middleware with consistent error responses
- **Swagger Documentation**: Complete API documentation with examples
- **IIS Ready**: Configured for IIS deployment

## Tech Stack

- .NET 8 Web API
- MongoDB (official driver)
- JWT Authentication
- FluentValidation
- Swagger/OpenAPI
- BCrypt for password hashing
- QRCoder for QR code generation

## Prerequisites

- .NET 8 SDK
- MongoDB (local or cloud instance)
- Visual Studio 2022 or VS Code (optional)

## Quick Start

### 1. Clone and Setup

```bash
git clone <repository-url>
cd EVChargingBackend
```

### 2. Configure MongoDB

Update `appsettings.json` with your MongoDB connection string:

```json
{
  "Mongo": {
    "ConnectionString": "mongodb://localhost:27017",
    "Database": "ev_charging_db"
  }
}
```

### 3. Update JWT Key

**IMPORTANT**: Replace the JWT key in `appsettings.json` with a secure 64-character random key:

```json
{
  "Jwt": {
    "Key": "YOUR_64_CHARACTER_RANDOM_SECRET_KEY_HERE"
  }
}
```

### 4. Run the Application

```bash
dotnet run --project EVChargingBackend
```

The API will be available at:
- **API**: `https://localhost:7000` or `http://localhost:5000`
- **Swagger UI**: `https://localhost:7000` (root URL)

### 5. Default Admin User

The system automatically creates a default admin user on first run:
- **Username**: `admin`
- **Password**: `Admin123!`
- **Role**: `Backoffice`

## Database Setup

### MongoDB Collections

The system creates the following collections with proper indexes:

1. **users** - System users (Backoffice, StationOperator)
2. **evowners** - EV owners (NIC as primary key)
3. **chargingstations** - Charging stations with geospatial data
4. **bookings** - Booking reservations

### Indexes Created

- Users: `username` (unique)
- EVOwners: `nic` (unique)
- ChargingStations: `is_active`, geospatial index on location
- Bookings: `{station_id, reservation_datetime}`, `{ev_owner_nic, reservation_datetime}`, `{status, reservation_datetime}`

## API Endpoints

### Authentication
- `POST /api/auth/login` - User login
- `POST /api/auth/register` - EV owner self-registration
- `POST /api/auth/refresh` - Token refresh

### User Management (Backoffice only)
- `POST /api/user` - Create system user
- `GET /api/user` - List users (paginated)
- `GET /api/user/{id}` - Get user by ID
- `PUT /api/user/{id}` - Update user
- `DELETE /api/user/{id}` - Delete user

### EV Owner Management
- `POST /api/evowner` - Create EV owner (Backoffice)
- `GET /api/evowner` - List EV owners (Backoffice)
- `GET /api/evowner/{nic}` - Get EV owner by NIC
- `PUT /api/evowner/{nic}` - Update EV owner
- `POST /api/evowner/{nic}/deactivate` - Deactivate EV owner
- `POST /api/evowner/{nic}/reactivate` - Reactivate EV owner (Backoffice)

### Charging Station Management
- `POST /api/station` - Create station (Backoffice)
- `GET /api/station` - List active stations
- `GET /api/station/{id}` - Get station by ID
- `GET /api/station/nearby` - Find nearby stations
- `PUT /api/station/{id}` - Update station
- `PUT /api/station/{id}/schedule` - Update station schedule
- `DELETE /api/station/{id}` - Deactivate station

### Booking Management
- `POST /api/booking` - Create booking
- `GET /api/booking/{id}` - Get booking by ID
- `GET /api/booking/owner/{nic}` - Get owner's bookings
- `GET /api/booking/dashboard/{nic}` - Get dashboard stats
- `PUT /api/booking/{id}` - Update booking
- `DELETE /api/booking/{id}` - Cancel booking
- `POST /api/booking/{id}/approve` - Approve booking
- `POST /api/booking/complete` - Complete booking via QR

## Business Rules

### Booking Rules
1. **7-Day Rule**: Reservations must be within 7 days of booking date
2. **12-Hour Rule**: Bookings cannot be modified/cancelled within 12 hours of reservation time
3. **Slot Availability**: Cannot exceed station's total slot capacity
4. **Status Flow**: Pending → Approved → Completed/Cancelled

### Station Rules
1. **Deactivation**: Cannot deactivate stations with active future bookings
2. **Geospatial**: Supports location-based queries for nearby stations

### Authentication Rules
1. **Role-Based Access**: Different endpoints require different roles
2. **Owner Scoping**: EV owners can only access their own data
3. **JWT Expiration**: Tokens expire after 60 minutes (configurable)

## Testing

Run the unit tests:

```bash
dotnet test EVChargingBackend.Tests
```

## IIS Deployment

### 1. Publish the Application

```bash
dotnet publish -c Release -o ./publish
```

### 2. Install IIS Hosting Bundle

Download and install the .NET 8 Hosting Bundle from Microsoft's website.

### 3. Configure IIS

1. Create a new Application Pool:
   - **Name**: `EVChargingAPI`
   - **.NET CLR Version**: `No Managed Code`
   - **Process Model Identity**: `ApplicationPoolIdentity`

2. Create a new Website:
   - **Site Name**: `EV Charging API`
   - **Application Pool**: `EVChargingAPI`
   - **Physical Path**: Point to your publish folder
   - **Port**: `80` (or your preferred port)

3. Add `web.config` to the publish folder (see below)

### 4. web.config

Create a `web.config` file in the publish folder:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet" arguments=".\EVChargingBackend.dll" stdoutLogEnabled="false" stdoutLogFile=".\logs\stdout" hostingModel="inprocess" />
    </system.webServer>
  </location>
</configuration>
```

### 5. Environment Variables

Set the following environment variables in IIS:
- `ASPNETCORE_ENVIRONMENT`: `Production`
- `Mongo__ConnectionString`: Your MongoDB connection string
- `Jwt__Key`: Your JWT secret key

## Configuration

### appsettings.json

```json
{
  "Jwt": {
    "Issuer": "EVCharging",
    "Audience": "EVClients",
    "Key": "YOUR_64_CHARACTER_RANDOM_SECRET_KEY"
  },
  "Mongo": {
    "ConnectionString": "mongodb://localhost:27017",
    "Database": "ev_charging_db"
  },
  "Cors": {
    "Origins": [
      "http://localhost:5173",
      "http://localhost:3000"
    ]
  }
}
```

## Security Considerations

1. **JWT Key**: Use a strong, random 64-character key in production
2. **MongoDB**: Use authentication and SSL in production
3. **CORS**: Configure allowed origins for your frontend applications
4. **HTTPS**: Always use HTTPS in production
5. **Password Hashing**: Uses BCrypt for secure password storage

## Troubleshooting

### Common Issues

1. **MongoDB Connection**: Ensure MongoDB is running and accessible
2. **JWT Errors**: Check that the JWT key is properly configured
3. **CORS Issues**: Verify CORS origins match your frontend URLs
4. **Port Conflicts**: Change ports in `launchSettings.json` if needed

### Logs

Check the application logs for detailed error information. In IIS, logs are typically found in the `logs` folder within your application directory.

## API Documentation

Once the application is running, visit the Swagger UI at the root URL to explore all available endpoints with interactive documentation.

## Support

For issues and questions, please refer to the API documentation or contact the development team.
