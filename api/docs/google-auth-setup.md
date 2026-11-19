# Set up Google sign-in for EI Dungeon

This tutorial configures Google Identity Services for registration, sign-in and account linking. Google identifies the user, and the API issues EI Dungeon session tokens. The Client ID used in the browser must match the one configured in the API for that environment.

## 1. Prepare the Google project

Sign in to the [Google Cloud Console](https://console.cloud.google.com/) with your administrator Google Account. You can use an existing account and grant other administrators access to the project.

Create one project for development and another for production. In the project selector, choose the environment you want to configure. Find **Google Auth Platform** in the menu or through the console search. Option names may vary with the console language.

Under **Branding**, configure the EI Dungeon name, support email and developer contacts. For production, provide the homepage, privacy policy and other information requested by Google using actual public project pages. Register the authorized domains and complete domain verification when requested. Check the console for current publishing and brand verification requirements.

Under **Audience**, choose **External** to allow players outside a Workspace organization. During test setup, follow the console instructions about test users. Add test accounts when required. Public sign-in availability depends on the project's publishing status.

See the [official setup instructions](https://developers.google.com/identity/gsi/web/guides/get-google-api-clientid) and [publishing guidance](https://developers.google.com/identity/siwg/best-practices).

## 2. Create the web client

Under **Clients**, create an OAuth client of type **Web application**. Use a name that identifies the environment, such as EI Dungeon Development.

Add the origins under **Authorized JavaScript origins**.

| Environment | Origins |
|---|---|
| Angular with `npm start` | `http://localhost` and `http://localhost:4200` |
| Local web with Docker | `http://localhost` and `http://localhost:8000` |
| Production | `https://dungeon.wildforges.com` |

Register only the origins used by that project. If you change the local port, include the origin with that port. Keep the hostname consistent between the frontend and API during development, using localhost for both.

The field accepts the scheme, host and port. Paths such as `/pt-BR/`, `/en/`, `/es/` and `/login` do not belong in authorized origins. The API domain is not the button's origin when the button is displayed on the web domain.

Our integration receives the credential through a JavaScript callback and sends it to the API. It requires neither an OAuth redirect URL for `/signin-google`, a service account, nor a Client Secret exchange. Copy the **Client ID**, which looks like `123456789-example.apps.googleusercontent.com`. The Client ID is public and identifies the application.

## 3. Configure the development API

In `api/ei-back/`, configure the Client ID through User Secrets.

The `appsettings.json` and `appsettings.Development.json` files also include a `GoogleAuth` section with an empty `ClientId`, leaving the integration disabled by default. The Client ID is public. User Secrets and environment variables override these values without editing versioned files. The Development file overrides the base file, including when the field is empty.

```powershell
dotnet user-secrets set "GoogleAuth:ClientId" "YOUR_CLIENT_ID.apps.googleusercontent.com"
dotnet run --project ei-back.csproj
```

The equivalent environment variable is `GoogleAuth__ClientId`. Preserve the existing database, JWT, Resend and code-protection settings. Traditional access still requires the email service to complete registration and recover passwords.

The frontend uses `http://localhost:5231` by default for the API when running directly. If you change the API address, update `web/src/environments/environment.ts`. The API's CORS configuration must allow the exact frontend origin.

## 4. Configure Angular for development

In `web/src/environments/environment.ts`, fill the public `googleClientId` field with the same Client ID as the API. Run these commands from `web/`.

```powershell
npm ci
npm start
```

Open `http://localhost:4200/login` or the localized version used for development. The Continue with Google button appears when the Client ID is set. An empty value disables Google actions while allowing email and password access.

## 5. Configure local containers

Preserve existing `.env` files. Add the same public value to `api/.env` and `web/.env`.

```dotenv
GOOGLE_CLIENT_ID=YOUR_CLIENT_ID.apps.googleusercontent.com
```

The API receives this value as `GoogleAuth__ClientId`. The frontend uses it when building the image. Rebuild the web image whenever you change the Client ID.

```powershell
# From api/
docker compose up -d --build backend

# From web/
docker compose up -d --build
```

The local frontend normally uses port 8000. Check `API_URL` in `web/.env` and `CORS_ALLOWED_ORIGINS` in `api/.env` before testing.

## 6. Prepare production

Use the Client ID from the production project. In the repository's Actions variables, configure `GOOGLE_CLIENT_ID` alongside `SITE_URL` and `API_URL`. The web workflow embeds the Client ID in the published image. This value can be a public repository variable.

In the production environment file managed by Dungeon infrastructure, also set `GOOGLE_CLIENT_ID` to the same value. Compose passes it to the API. Follow the [production runbook](../../../infra/apps/dungeon/DEPLOYMENT.md) for deployment, backups, provisioned configuration updates and image selection. Changing configuration in the local checkout does not automatically update the provisioned VM configuration.

The frontend and API have new registration and sign-in contracts. The migration removes `user_name` and `full_name`, makes `password` optional and creates the Google tables. The administrator must coordinate replacement of both images and any required downtime. An old API image cannot use the new schema. Restoring a backup is required to recover removed values. The migration refuses a schema-only rollback.

Detailed operational instructions remain in the runbook. Configuring the Google project and performing deployments are administrator actions.

## 7. Check the flows

1. Use a Google Account with Gmail that does not yet have an account in the game. Click Continue with Google and complete account selection/consent. Home should open without asking for a name, username or password.
2. Sign out and sign in again with the same Google Account. Check that the same games and permissions are available.
3. Use an email already registered traditionally, without a Google link. The site prompts you to sign in to the existing account. In Settings, link the Google Account with the same verified local email.
4. Use an unlinked Google Account with an email outside Gmail/Workspace. The site offers traditional registration and email confirmation through a code. After signing in, link the account in Settings.
5. For a Google-only account, open Security in Settings. Confirm the Google Account and create the first password with confirmation. Then verify both Google sign-in and email/password sign-in.
6. Request recovery for a Google-only account. The response remains generic and no code is sent. For an eligible account with a local password, recovery should send the code.

The link associates Google's `sub` with the internal user ID. The `sub` in the EI Dungeon JWT remains the internal ID. Matching email addresses do not automatically merge accounts.

## 8. Troubleshoot

| Symptom | What to check |
|---|---|
| Missing button | `googleClientId` in development or `GOOGLE_CLIENT_ID` during the build. An existing image requires a new build |
| Unauthorized origin | The page's scheme, hostname and port must match the web client's JavaScript origins |
| CORS error | `Cors:AllowedOrigins` must include the actual page origin. `localhost` and `127.0.0.1` are different hosts |
| Rejected credential | API/frontend Client IDs, server clock, outbound network access to Google keys and attempt validity |
| Expired confirmation | Restart the Google flow. Challenges last five minutes and are single use |
| Wrong account when creating a password | Select the Google Account already linked to the authenticated user |
| Blank or blocked popup | Browser permissions and proxy CSP/COOP headers. GIS documents the required permissions and cases needing `same-origin-allow-popups` |
| External user cannot sign in | Audience type, testing/publishing status and any test users configured in the console |
| Configuration unavailable error | `GoogleAuth:ClientId` in the API process. Check the container's effective environment |

Google calls use HttpOnly cookies to bind the attempt to the browser. In production, these require HTTPS. The current deployment places the frontend and API on the same `wildforges.com` site, with different subdomains. Moving these services to different sites requires reviewing the cookie policy before using this configuration.

Never copy ID tokens, passwords or email codes into logs or tickets. Record only the API status and error code. Automated tests use a substituted verifier and an isolated database. Real Google validation requires a configured project and an available test account.
