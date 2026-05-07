# ElectronicsShop

## 1. Introduction

**ElectronicsShop** is a web application designed to support the management and online selling of electronic products.  
The main features include:

- Ordering products  
- Shopping cart management  
- Product management  
- Customer management  
- Order management  
- Integration with **Hugging Face AI services** for smart features  
- Online payment integration with **VNPAY**  

---

## 2. Technologies Used

- ASP.NET (.NET Framework 4.7.2), Entity Framework  
- SQL Server  
- HTML / CSS / JavaScript, Bootstrap, jQuery, Ajax  

---

## 3. Get Hugging Face API Key and Configure `web.config`

### Step 1: Create an API Key
1. Go to https://huggingface.co and log in (or sign up).
2. Open **Settings** -> **Access Tokens**.
3. Create a new token (recommended scope: `api`).

### Step 2: Configure in `web.config`

Open the `web.config` file and add the API key to `appSettings`:

```xml
<configuration>
  <appSettings>
    <add key="chatBotAPIKey" value="YOUR_API_KEY_HERE" />
  </appSettings>
</configuration>
```

> **Important:**  
> - Replace `YOUR_API_KEY_HERE` with your real token.  
> - Do **not** commit your API key to a public Git repository.  

---

## 4. Initialize Database with `Database.sql` and Configure `connectionString`

### Step 1: Initialize Data
1. Open the `Database.sql` file.
2. Create a new database or select an existing one.
3. Run the whole `Database.sql` file to create the **schema** and **sample data**.

### Step 2: Configure Connection String

Update the `connectionString` in `web.config`:

```xml
<configuration>
  <connectionStrings>
    <add 
      name="DefaultConnection"
      connectionString="Server=YOUR_SQL_SERVER;Database=ElectronicsShopDb;User Id=YOUR_USER;Password=YOUR_PASSWORD;"
      providerName="System.Data.SqlClient" />
  </connectionStrings>
</configuration>
```

#### Using Windows Authentication

```xml
connectionString="Server=YOUR_SQL_SERVER;Database=ElectronicsShopDb;Integrated Security=True;"
```

> Make sure the **database name** is the same as the one you created when running `Database.sql`.

---

## 5. Install and Run the Application

1. Open the solution:
   - **File -> Open -> Project/Solution**
   - Select the `.sln` file
2. Restore NuGet packages:
   - **NuGet Package Manager -> Restore Packages**  
   - or **Build -> Restore NuGet Packages**
3. Select the **Startup Project** (if there are multiple projects).
4. Build the project:
   - **Build -> Rebuild Solution**
5. Run the application:
   - **Debug -> Start Debugging** (F5)  
   - or **Debug -> Start Without Debugging** (Ctrl + F5)
6. If you get errors:
   - Check the `connectionString` in `web.config`
   - Check database configuration
   - When debugging Hugging Face API, check the request header:
     ```
     Authorization: Bearer <HuggingFaceApiKey>
     ```

---

## 6. Notes

- Do not store API keys or sensitive information in a public repository.
- For production deployment, it is recommended to use:
  - Environment Variables  
  - Secret Manager / Key Vault  
- Make sure all NuGet packages are restored before running the project.

---

**Good luck with your project!**
