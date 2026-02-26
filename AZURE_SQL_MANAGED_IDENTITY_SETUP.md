# Azure SQL Database Managed Identity Configuration

## Prerequisites
- Azure SQL Database: `sql-db-aspire-test` on server `sql-srvr-aspire-test.database.windows.net`
- Azure AD authentication enabled on the SQL Server

## Setup Steps

### 1. Enable Azure AD Authentication on SQL Server (If Not Already Enabled)
In Azure Portal:
1. Navigate to your SQL Server: `sql-srvr-aspire-test`
2. Go to **Settings** > **Microsoft Entra ID** (formerly Azure Active Directory)
3. Click **Set admin** and select an Azure AD user or group as the admin
4. Click **Save**

### 2. Grant Database Access to Your Identity

#### Option A: For Local Development (Your Azure AD User)
Connect to the database using Azure AD authentication and run:

```sql
-- Connect to the sql-db-aspire-test database
CREATE USER [your-email@domain.com] FROM EXTERNAL PROVIDER;
ALTER ROLE db_owner ADD MEMBER [your-email@domain.com];
```

#### Option B: For Deployed App (Managed Identity)
When deployed to Azure App Service or Azure Container Apps:

```sql
-- Replace with your App Service or Container App name
CREATE USER [your-app-name] FROM EXTERNAL PROVIDER;
ALTER ROLE db_owner ADD MEMBER [your-app-name];

-- Or grant specific permissions instead of db_owner:
ALTER ROLE db_datareader ADD MEMBER [your-app-name];
ALTER ROLE db_datawriter ADD MEMBER [your-app-name];
GRANT EXECUTE TO [your-app-name];
```

### 3. Local Development Authentication

The `DefaultAzureCredential` will authenticate in this order:
1. **Environment variables** (for CI/CD)
2. **Managed Identity** (when deployed to Azure)
3. **Visual Studio** (for local development)
4. **Azure CLI** (alternative for local development)
5. **Azure PowerShell** (alternative for local development)

#### Setup for Visual Studio:
1. Open Visual Studio
2. Go to **Tools** > **Options** > **Azure Service Authentication**
3. Sign in with your Azure AD account

#### Alternative - Setup for Azure CLI:
```bash
az login
az account set --subscription "your-subscription-id"
```

### 4. Deploy to Azure

When deploying to Azure App Service or Container Apps:

1. **Enable Managed Identity** on your app:
   ```bash
   az webapp identity assign --name your-app-name --resource-group your-rg
   ```

2. The managed identity name will be the same as your app name

3. Grant the managed identity access to SQL Database (see Option B above)

### 5. Verify Connection

The application will now:
- Use your Azure AD credentials when running locally in Visual Studio
- Use the managed identity when deployed to Azure
- No passwords stored in configuration files

## Troubleshooting

### Error: "Login failed for user ''"
- Ensure Azure AD authentication is enabled on SQL Server
- Verify your user/managed identity has been granted access to the database
- Check that you're signed in to Visual Studio with the correct Azure AD account

### Error: "Interactive authentication is not supported"
- Sign in to Visual Studio: Tools > Options > Azure Service Authentication
- Or use Azure CLI: `az login`

### Error: "Could not acquire a token"
- Ensure you have the correct Azure subscription selected
- Verify network connectivity to Azure
- Check Azure AD tenant settings

## Connection String Format

The connection string should NOT include User ID or Password:
```
Server=tcp:sql-srvr-aspire-test.database.windows.net,1433;Database=sql-db-aspire-test;Encrypt=True;TrustServerCertificate=False;
```

Authentication is handled by the `SqlAuthenticationMethod.ActiveDirectoryDefault` setting in code.
