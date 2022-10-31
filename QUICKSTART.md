# Quickstart your Auth

## Our objective

We're going to take a very simple API, written using the Minimal API paradigm and spitting out weather forecasts. We are then going to secure that endpoint using an external identity provider that will guarantee the identity of the user.
It's just a few steps from being a massive SaaS.

## Important warning: YMMV

This Quickstart assumes you are running **Windows 11**. The instructions and tooling will vary on other platforms (you may want to consider [Visual Studio Code](https://code.visualstudio.com/) on MacOS and *nix) but the overall flow and instructions will stay the same.

## The toolkit

### Running KeyCloak

#### Docker or Docker desktop

We will use Docker to compose an image of a KeyCloak server, thus avoiding the need to set one up, which would require three quickstarts alone.

#### KeyCloak docker image

We will use this dockerfile to run a server with a KeyCloak instance that will hold the information about our user and provide identity validation.

#### A local database - Postgres

We will need to have a local database to store, mostly, KeyCloak's information which is not persisted when the container goes down. Postgres is the one that will be used in this Quickstart, since it's also the requirement for the image that will be used.

### Developing the clients

#### Visual Studio 2022 and the default Minimal API template

This is the default project template you will find ready and waiting in VS2022 - it provides just the right amount of simplicity and endpoints.
Visual Studio community is free of charge at time of writing, [here](https://visualstudio.microsoft.com/vs/community/).

#### KeyCloak Provider Nuget

This will help us with REST requests to KeyCloak and provide ready-made classes to make sense of the responses.

#### Node 16 and Vue3

Any IDE of choice will do, Vue needs Node 16 to be installed though.

## The process

### 1) Get Docker

Docker pushes for the usage of Docker Desktop as of this writing, although using the tried and true Docker engine and `docker compose up` will yield exactly the same results.
You can get Docker Desktop on the [Docker website](https://www.docker.com/get-started/).
On Windows, if during installation you get asked for **admin credentials**, then likely you won't be able to properly run Docker Desktop until you add your user to the `docker-users` group using Computer Management.
![Refer to any online tutorial for details](./res/computer-management.png) _Refer to any online guide for more details_

### 2) Run a container with KeyCloak and a database in it

Once you manage to properly run Docker Desktop, it will ask you if you wish to maybe, perhaps, run a **PostgreSQL** example -- don't (it actually complicates network handling).
Rather, create a comfortable folder for a `YAML` file with the name `docker-compose.yml` and the following content:

```yaml
version: '3'

volumes:
  postgres_data:
      driver: local

services:
  postgres:
      image: postgres
      volumes:
        - postgres_data:/var/lib/postgresql/data
      environment:
        POSTGRES_DB: keycloak
        POSTGRES_USER: keycloak
        POSTGRES_PASSWORD: REPLACEME

  keycloak:
      image: bitnami/keycloak:latest
      environment:
        KEYCLOAK_DATABASE_HOST: localhost
        KEYCLOAK_DATABASE_NAME: keycloak
        KEYCLOAK_DATABASE_USER: keycloak
        KEYCLOAK_DATABASE_PASSWORD: REPLACEME
        KEYCLOAK_ADMIN: admin
        KEYCLOAK_ADMIN_PASSWORD: REPLACEMETOO

      ports:
        - 8080:8080
      depends_on:
        - postgres
```

`{` _YAML employs whitespace as a signifier, so be careful if you're writing on your own_ `}`

> **A word of caution**
> The passwords above are, obviously, not production safe.

If you are following from the **git repo** then it's already in `/kc-docker/docker-compose.yml` -- go to your `yml` file and run the magic command (from **Powershell**):

`docker compose up -d`

> **A word of caution**
> Moving around docker files is easy, but moving them with their stores is trickier, ensure the file is where you want it to be before composing

At which point your KeyCloak container should get latest, download, have you wait a significant amount of time (depending on your network) and finally start running, which you should also see reflected in Docker Desktop:
![Containers running](./res/running-container.png)
_If you experience excess of white, check the theme_

At this point, we may want to visit our instance and see what's going on.

### 3) Configure KeyCloak

Using your browser of choice, navigate to [`http://localhost:8080/`](http://localhost:8080/) -- you can see (and configure) the port in Docker.
Then press `Administration Console` and enter the credentials when prompted (they are hardcoded in the YAML file, for the moment):

`
Username: admin
Password: REPLACEMETOO
`

Once greeted by the admin panel, we shall start by creating a **Realm**, the topmost "container" in KeyCloak that can be configured with its own rules, clients, users, providers and so on.
![KeyCloak - Create Realm button](./res/create-realm.png)

**Very important: pick a URI-valid name**
The name should be a simple word or a slug, anything that would not break even the simplest of URI, as the name of the Realm will become part of all of the authentication paths going forward.
**for this tutorial we will name our realm `test`**

First thing to do is check out the **Clients** (menu on the left):

- Among the pre-configured clients there is `account`. Open the client, Scroll down to the "Access settings" section and switch on **Client Authentication**. Next, check "Service account roles". This allows the "account" client to use its own secret to authenticate itself. Save (bottom of the page).
- Navigate to the "Credentials" tab and set the authenticator to "Client Id and Secret", thus allowing this Client to authenticate with the equivalent of a secret key. Regenerate the key (if the button is greyed out, reload) and take note of it.
- Finally, back to the "Settings", we need to add a valid URI where our OIDC path will be redirect. Add `http://127.0.0.1:5173/*` and `http://localhost:5000/*` as valid URIs (default URIs for Vue3, .NET, with a wildcard).
- Now, back to the list of clients, open `realm-management` and re-generate the Secret Key like before (take note of it). Check in the "Roles" tab that the list of roles is not empty

![Addition of valid redirect URIs](./res/adding_uris.png)
_Adding valid URIs like a pro (they can be changed later)_

#### 3b) Create your test user

You may now proceed to create the first of the **Users** in the eponymous menu point:

- When creating the user make sure to tick the "email verified" switch -- it increases the convenience for testing purposes
- You will not see a "Password" field and this is normal -- users in KeyCloak may possess zero to many different credentials, which may or may not be passwords.
- Once the user is created, in the "Credentials" tab you will be able to set a password. Set a simple one (you are _guaranteed_ to mis-type it at least once) and mark it as **not** temporary

This is enough configuration on this side. Time to create something to authenticate.

### 4) Create the API to secure

Using Visual Studio 2022, you want to create a new project using the **ASP.NET Core Web API** template, making sure to honour the following settings to properly generate it:

- Framework: **.NET 6.0 LTS** -- _this is the default at time of writing_
- Use Controllers: **Uncheck** (so, use Minimal APIs)
- Configure for HTTPS: **Uncheck** (again, save on development time)

![Screenshot of correct settings](./res/project_settings.png)
_These screens change every so often..._

Once the app is generated you should end up with one single `.cs` file, `Program.cs`, one single configuration file, `appsettings.json`, and a lot of space to fill.

Start by adding the packages that are going to be needed:

- `Microsoft.AspNetCore.Authentication.JwtBearer` -- the package to easily handle JWT tokens which are overwhelmingly the standard used by Open ID Connect compliant entities (_back on the topic of JWT soon_)
- `PrettyNeat.Keycloak` -- the package to ease attrition with an external KeyCloak instance, by providing helpers for the calls and structured classes for the responses

Once both packages are installed, the app's configuration needs to be amended, but first:

**A disambiguation about `JWT`**

The `JWT` standard is often confused or conflated with `JSON`, although it is, at its core, a specially structured `JSON` object, with well defined fields.
This is how it may appear in the wild:

`
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c
`

the three segments, divided by dots, are base64 encoded (not unlike when you send an Authorization header with username and password):

```json
// First section: the header
{
  "alg": "HS256", //Type of encryption for the key
  "typ": "JWT"    //Specifies that this is a JWT
}

// Second section, the payload
{
  "sub": "1234567890",   //One of the standard claims, the ID of the subject
  "name": "John Doe",    //Standard identity claim (valid in JWT)
  "iat": 1516239022      //One of the standard claims, issue timestamp
}
```

The whole result can be verified by using the signature and a secret key (which the server has, and the general public doesn't):

```code
// using the algorithm specified in alg
alg( 
    base64URIEncode(header) 
    + '.' 
    + base64URIEncode(payload),
    secret-key //use this secret key to generate the hash
  )
```

So, **tl;dr** a JWT is a convenient string that can contain information about some entity, usually a user, and can be verified by means of a secret key, in possession of the server issuing it. When read, its contents are based on JSON, `base64` encoded.

For a colour-coded example: [jwt.io](https://jwt.io/)

**_moving on..._**

#### 4b) Configuration details

The installed packages both require configuration values to work properly.
**For the `JwtBearer`** package, a lightweight configuration can be:

```json
  "Jwt": {
    "issuer": "testIssuer",
    "secret": "secretKeySeedMustBeLongEnough"
  }
```

`{` _(to be placed in `appsettings.json`)_ `}`

Note that the `JwtBearer` has to be explicitly configured with the values during service initialization (this will come next).
**For the `KeyCloakProvider`** package, a valid configuration is:

```json
  "KeyCloak": {
    "ServiceId": "account",
    "ServiceSecret": "yQM2puYysKZZ79jxIHXvTolHyVngtVpu",
    "AdminId": "realm-service",
    "AdminSecret": "hvVhumXVNsYqlZUU2cXxRTCmMURhaP8L",
    "PublicUrl": "http://localhost:5137",
    "OAuthCatchPath": "oauth",
    "ApiUrl": "https://localhost:8080/",
    "PublicLogoutPath": "https://localhost:5137/logged-out",
    "Realm": "test"
  }
```

> **A word of caution**
> Pay close attention to the presence or lack of trailing slashes - these details are crucial when working with KeyCloak

The `README.MD` of the package explains the meaning of these values, however it's important they are correctly replaced by the values of the actual secrets that were generated in KeyCloak, as well as the correct Realm name.
Take note of `OAuthCatchPath` as this, chained with `PublicUrl`, should be the full path where the End User is redirected from OIDC/OAUTH2 calls.

In this example: `http://localhost:5137/oauth` (_trailing slash matters_)

#### 4c) Endpoint to receive authorization codes

In the typical OIDC (Open ID Connection) scenario, the End User will navigate to the **OP** (OIDC Provider, i.e. **KeyCloak**), verify their identity with some method and then be returned to the **Client** (the application who needed to know the identity) with a **code** which can be exchanged with the **OP** to confirm the identity.

_What if..._
In the real world, if a User wants access to a Canteen, the Canteen directs them to an Operator who knows how to recognize forged IDs. The Operator gives them a **code** and they bring it to the Canteen, who can phone the Operator to verify that the **code** is not a forgery itself.
_...or not_

The tasks to achieve are:

- Create an endpoint that can receive the code from the End User
- Exchange the code with information
- Once the information is verified, confirm the user as authenticated

Create a new file to hold the class -- named  `OauthEndpoint.cs` and a static async method that will take charge of the processing -- `Processing()`.

```c#
namespace ExampleAPI
{
    public class OauthEndpoint
    {
        public static async Task<IResult> Process(HttpRequest req, KeyCloakProvider kc, IConfiguration config)
        {
        }
    }
}
```

A note about the parameters:
`HttpRequest` is being taken directly from the `HttpContext` underlying the request
`KeyCloakProvider` and `IConfiguration` are taken from the services

In the **minimal API** way of handling requests, parameter declaration for the handler features way more implication. Unlike **MVC** the `[FromServices]` and so on are not required unless there is an ambiguity.

> **A word of compassion**
> _this is incredibly confusing at first and marking everything is still supported_

Minimal validation:

```c#
//...
// Process(){
            
//check for the presence of a "code" in the query
if (!req.Query.Any(q => q.Key.Equals("code", StringComparison.InvariantCultureIgnoreCase)))
  return Results.BadRequest("No code could be found");

//collect the code
string? code = req.Query["code"].SingleOrDefault();

//code must not be empty
if (string.IsNullOrWhiteSpace(code))
  return Results.BadRequest("No valid code could be found");
  
// }
//...
```

Then the single line to exchange the code for information:

```c#
//...
//  return Results.BadRequest("No valid code could be found");

//attempt to validate and retrieve user-info
var kcResults = await kc.BrokeredCodeLogin(code);

if ( kcResults.Success)
{
}

return Results.StatusCode(kcResults.HttpErrorCode ?? 400);

// }
//...
```

`kcResults` is an object that contains a wealth of information, the success of the compound operation (code exchange and user-info request), the user's claims, refresh token data and more.

Once this information is exchanged all that is left is returning a valid JWT -- an encoded string of claims and a verifiable signature.

```c#
//...
// if ( kcResult.Success) {

//convert the result to a small list of meaningful claims
var identityClaims = MakeClaims(kcResults.Result);

//if the list is null or empty, fail
if (identityClaims?.Count <= 0) return Results.Unauthorized();

var jwtGenerator = new JwtSecurityTokenHandler();

//create the object description of a JWT, including the credentials, claims and algorithm
var token = new JwtSecurityToken(
  config["Jwt:issuer"],           //the issuer
  null,                           //the audience
  identityClaims,                 //the payload claims
  DateTime.Now,                   //the "nbf" - token invalid before time
  DateTime.Now.AddMinutes(10),    //the "exp" - validity
  new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:secret"])), SecurityAlgorithms.HmacSha256)
);

//create the actual string representation
var jwtString = jwtGenerator.WriteToken(token);

//return the string representation of the token
return Results.Json(new { token = jwtString });

// }
//...
```

`JwtSecurityTokenHandler`, `JwtSecurityToken` and `SymmetricSecurityKey` are actually all part of the vendor provided tools for handling the tokens (and have ample documentation available).

The only missing method, `MakeClaims()`, extracts minimal useful information from the response and puts it into claims:

```c#
private static List<Claim>? MakeClaims(KCTokenizedIdentityResponse? identity)
{
    if (identity == null) return null;

    List<Claim> identityClaims = new List<Claim>();

    //specify that the central unique identifier is the UPN
    identityClaims.Add(new Claim(ClaimTypes.NameIdentifier, ClaimTypes.Upn));
    //specify value of the UPN
    identityClaims.Add(new Claim(ClaimTypes.Upn, identity.User.id));
    identityClaims.Add(new Claim(ClaimTypes.Email, identity.User.email));
    //add the refresh token using a custom claim type (specified in the "urn:" format)
    identityClaims.Add(new Claim(Statics.CustomClaims.RefreshToken, identity.Token.refresh_token));

    return identityClaims;
}
```

The `Statics.CustomClaims.RefreshToken` is a static string that can be used to uniquely identify a Claim for the refresh token.
Claims need to specify a valid resource locator, but that includes anything which contains URN-valid characters and starts with `urn:`

The `Statics.cs` file:

```c#
namespace ExampleAPI
{
    public static class Statics
    {
        public static class CustomClaims
        {
            public static string RefreshToken = "urn:refreshtokenvalue";
        }
    }
}
```

Now the mapping of this handler is done by minimal API conventions, right under the existing one in `Program.cs`

```c#
//...
// .WithName("GetWeatherForecast");

app.MapGet("/oauth", OauthEndpoint.Process);

// app.Run();
//...
```

The solution layout should closely resemble the following:
![A screenshot of the Solution Layout](./res/solution_layout.png)
_Packages and contents of the files should match_

#### 4d) Actual AuthN/AuthZ*

*_Auth-e-N-tication/Auth-ori-Z-ation_

All of the necessary configuration is performed in `Program.cs`

```c#
//...
// builder.Services.AddSwaggerGen();

//get configurator
var aot_conf = builder.Configuration as IConfigurationRoot;

//add the Authentication and Authorization suite
builder.Services.AddAuthentication(opts =>
{
    opts.DefaultScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme,
              opts =>
              {
                  opts.TokenValidationParameters =
                  new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                  {
                      ValidateActor = false,                      
                      ValidateAudience = false,
                      ValidIssuer = aot_conf["Jwt:issuer"],
                      IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(aot_conf["Jwt:secret"]))
                  };
              });

builder.Services.AddAuthorization();

builder.Services.AddKeyCloakProvider();

//var app = builder.Build();
//...
```

`aot_conf` here is assigned the value of the Configuration object of the builder. In .NET 6, this is allowed to access configuration values before the configuration is canonically built (before, a separate instance would have to be built).

`AddAuthentication()` is part of the vendor provided middleware as is `AddJwtBearer()` which enforces the check of a valid JWT token passed as `Bearer` in the `Authorization` header of the request.

The only missing part is injecting the authentication middleware and handlers in the correct order:

```c#
//...
//}

app.UseAuthentication();
app.UseAuthorization();

//var summaries = new[]
//...
```

Finally, the routes can now be respectively secured and left anonymous:

```c#
//...
// .WithName("GetWeatherForecast")
.RequireAuthorization();

app.MapGet("/oauth", OauthEndpoint.Process) // remove the ;
    .AllowAnonymous();

//...
```

> **A word of compassion**
> Middleware order does matter, as the execution is top-down and some middleware has to run in specific positions in the chain - that is where it will be registered.
> This specifically concerns Authentication and Authorization middleware that has to be placed after CORS processing and before Routing takes place in most scenarios.

Now, "all that's left" is a frontend to actually use this.

#### 4e) But wait, there's CORS! _(quote)_

While this is not strictly necessary for this tutorial, CORS will be an issue if the app is run "as is" due to the Origins of the frontend and backend being different. 
To address this, in the Minimal API:

```c#
//...
// var aot_conf = builder.Configuration as IConfigurationRoot;

builder.Services.AddCors(opts =>
{
    opts.AddPolicy("vue3",
        pol =>
    {
        pol.AllowAnyHeader();
        pol.AllowAnyMethod();
        pol.AllowCredentials();
        pol.WithOrigins("http://127.0.0.1:5173");
        pol.Build();
    });
});

// builder.Services.AddAuthentication(opts =>
//...
```

A policy does not need to have a name if it is declared default. However, CORS should be explicitly declared per-route at least in .NET6.0:

```c#
//...

app.UseCors("vue3");

// app.UseAuthentication();
//...
```

```c#
// ...
// .WithName("GetWeatherForecast")
.RequireAuthorization()
.RequireCors("vue3");

app.MapGet("/oauth", OauthEndpoint.Process)
    .AllowAnonymous()
    .RequireCors("vue3");

app.Urls.Clear();
app.Urls.Add("http://localhost:5000");  //set correct backend URI

// app.Run();    
// ...
```

This will allow the frontend and the backend to communicate without any issue, provided the URIs are conistent

> **a word of caution**
> Origin is a fickle concept in web practice, accessing the Vue3 application from `localhost` rather than `127.0.0.1` will fail against CORS rules

### 5) The Frontend Vue

Create a new project in Vue3, which is very simple provided you have installed [Node 16 (LTS)](https://nodejs.org/en/):

`# npm init vue@latest` in your console of choice (likely **Powershell** but the new command prompt will work), followed by the selection of the bare minimum options:

```bash
√ Project name: ... api-trigger
√ Add TypeScript? ... [No] / Yes
√ Add JSX Support? ... [No] / Yes
√ Add Vue Router for Single Page Application development? ... No / [Yes]
√ Add Pinia for state management? ... No / [Yes]
√ Add Vitest for Unit Testing? ... [No] / Yes
√ Add Cypress for both Unit and End-to-End testing? ... [No] / Yes
√ Add ESLint for code quality? ... [No] / Yes
```

The features we are trying to create are the following:

- A button that will call the **weather API** and show what the return is, including an error message if not authenticated
- Another button (or link) that will allow authentication via KeyCloak
- Given that Vue Router is being used, have an authentication-only page that uses **frontend** authentication verification

The initial template of the application already has several elements of decoration. Run the application at least once with `# npm run dev` to ensure that all depedencies are correct and then proceed to clean the template:

- In `views/HomeView.vue` everything can be removed inside the `<template></template>` tags and replace with a simple `<div class="home"></div>` (which will suffice for the beginning)
- In `views/AboutView.vue` the inner text may be replaced with something indicating the purpose of the page, such as "Only authenticated users may see this page"
- A new component called `views/APIReaderView.vue` may be created -- it will not contain code initially

The following can be safely deleted to have a clener folder structure:

- The folder `icons` in `components`
- The component `TheWelcome.vue` and the component `WelcomeItem.vue`
- The store generated automatically - this will be replaced by another one soon

> **a word of caution**
> The amount of files generated even by a simple Vue3 CLI command is overwhelming and intended to provide a volume of examples.
> If at any point you notice you deleted too much, you may recreate files and even folders to match the structure of the Git repo.

#### 5a) Setting up the basics: state

Create a new store, `auth.js` in the `stores` folder:

```javascript
import { defineStore } from 'pinia'

export const useAuthStore = defineStore('auth', {
    state: () => ({ tokenValue: '' }),
    getters: {
      isLoggedIn: (state) => state.tokenValue && state.tokenValue.length,
      jwtToken: (state) => state.tokenValue
    },
    actions: {
      login(token) {
        if ( !token || !token.length ) throw('use logout() to reset the token');
        
        if ( this.tokenValue && this.tokenValue.length ) throw('already logged in');

        this.tokenValue = token;
      },
      logout(){
        this.tokenValue = '';
      }
    },
  })
```

`{` _(this is the entire code of the store)_ `}`

This store, using the recommended state manager **Pinia**, contains a simple state, giving access to the token, two getters and two actions to set the state (this is more in the style of older Vuex stores with mutations).
The `isLoggedIn` getter is going to be instrumental in understanding if the user has a token assigned.

This store will be used close to everywhere in the frontend application.

> **a word of caution**
> The application will be presented, trying to avoid excessive modification of the source files, partially in the **Composition API** and partially in the **Options API**.
> Thus files with the same `.vue` extension will look visually and stylistically very different.

#### 5b) Setting up the basics: processing

Opening the `views/HomeView.vue`, the `<script setup>` doesn't do much so far.
Logic in this section should cater for two objectives:

**1)** When the user comes back from an **OAuth redirect** having a `code`, attempt to send that code to the backend:

```javascript
import { onMounted } from 'vue';
import { useRoute } from 'vue-router';
import { useAuthStore } from '../stores/auth';

const route = useRoute();
const authStore = useAuthStore();

onMounted(async () => {

  if ( route.query.code && route.query.code.length ){
    let outcome = await fetch(`http://localhost:5000/oauth?code=${route.query.code}`)
    if ( outcome.ok ){
      let data = await outcome.json();

      authStore.login(data.token);
    }
  }
})
```

The `code` query parameter from the `route` (an object available in any component when using the Vue Router) should exist and be longer than zero to be sent for verification.  
If there is a positive response, then the JSON is extracted and the `login()` action of the store is called.

**2)** When the user attempts to access a protected page when unauthenticated, he is prompted about lack of authentication:

```javascript
import {ref, onMounted} from 'vue'; //add "ref"

// [...]

const fromProtected = ref(false);

// [...]

onMounted(async () => {  
  if ( route.query.unauthorized && route.query.unauthorized=='true')
    fromProtected.value = true;

  // [...]
});
```

`{` _(to be placed in the `<script setup>`)_ `}`

This sets a reactive property (declared with `ref` as is standard for Vue3 when declaring reactive non-objects) when the `unauthorized` query parameter is equal to `'true'`.

The **template** has to be corrected to account for the new reactivity introduced in the code:

```vue
<template>
  <div class="home">
    <h1>This is our home page</h1>
    <div v-if="!authStore.isLoggedIn">
      <p>You can use the below link to log in using KeyCloak, choose to call the protected weather API or visit a front-end protected page:</p>

<a href="http://localhost:8080/realms/test/protocol/openid-connect/auth?client_id=account&redirect_uri=http://127.0.0.1:5173/oauth&response_type=code&scope=openid" title="Link to login via KeyCloak">Login via KeyCloak</a>      
    </div>
    <div v-else>
      You are logged in! Try the about page or the API!
    </div>
  </div> 
  <div class="authorization-error" v-if="fromProtected">You tried accessing a protected page! Log in first.</div>
</template>
```

Notice the `v-if` tag, which exemplifies very well the virtual-DOM driven frameworks.
This may actually be a good starting point to brush up on Vue if completely unaware: [Directives in Vue3](https://vuejs.org/guide/essentials/template-syntax.html#directives)

The notable part about the template (aside from some parts being shown or changed conditionally) is the URI for the authentication:

- `http://localhost:8080/` is the URI of the docker container running KeyCloak
- `realms/test/protocol/openid-connect/auth` is the static URI for the Open ID Connect endpoint
- `client_id=account` sets the client to use on the KeyCloak side
- `redirect_uri=http://127.0.0.1:5173/oauth` is the URI which has been set up in the back-end as well as the target
- `response_type=code` selects the response to be an authorization code
- `scope=openid` sets the scope of authorization required to the minimal information required for authentication

The KeyCloak documentation on how to operate this endpoint leans _heavily_ on the underlying **RFC** and is [not for the faint-hearted](https://www.keycloak.org/docs/latest/securing_apps/#authorization-endpoint).

#### 5c) The API Reader

In the `views/APIReaderView.vue` an introduction should be shown, together with a component that will make the API call on a button press:

```vue
<script setup>
</script>

<template>
    <div class="api">
        Press the button underneath to perform the API call.
        This call will fail if the current user is not authenticated!
        The component will report on this.
    </div>
</template>
```

The component then needs to be created.
This will be done using the classic **Options API**, reason being it is easier to read for those coming from a mostly backend background or Vue1/Vue2, and faster and more consistent in this small example.

The file should be `components/APIReader.vue`:

```vue
<template>
    <div class="reader">
        <button class="reader__start" type="button" v-if="!isCalling" @click="doCall">Call the API</button>
        <div class="reader__state" v-else>
            <p class="reader__state__string"></p>
            <div class="reader__state__loader" v-if="isLoading">Loading...</div>
        </div>
    </div>
</template>
<script>
export default {
}
</script>
```

The CSS in the example is based on the **BEM** paradygm but this choice is arbitrary.

This is a minimal component but it can be already included in the view to ensure it gets rendered in the `views/APIReaderView.vue`:

```vue
<script setup>
import APIReader from '../components/APIReader.vue'
</script>

<template>
    <div class="api">
        // [...
        // ...]
        <APIReader></APIReader>
    </div>
</template>
```

#### 5d) Coding the logic of the API Reader

The code of the component is thoroughly commented in the example and the exact extent of Vue is beyond the scope of the quickstart, however its logic is as follows:

- Set display-state variables to signify that a call can be made:

```javascript
    data(){
        return {
            isCalling: false,   //is the component performing an API call
            isLoading: false,   //is the component awaiting a result
            isSuccess: false,   //the call yielded a result
            stateStrings: [],    //state strings
            resultText: ''      //text of the result, if one is present
        }
    },
```

`{` _(this is the entire data block of the options)_ `}`

- Once button is pressed, signify that a call is being made and disallow further calls

```javascript
        async doCall(){            
            this.isCalling = true;
            this.stateStrings = [];
            
            this.isLoading = true;
            await new Promise( r => setTimeout(r,400));
            this.isLoading = false;
            
            this.stateStrings.push('Calling the API...');
            this.isLoading = true;

            // [...]
```

`{` _(this is the beginning of the doCall method in the methods, in options)_ `}`

- Attempt to fetch the protected resource, add the `Authorization` header if the user is, in fact, authenticated

```javascript
  //  async doCall(){    
  //  [...]
            try {
                this.stateStrings.push('Performing a fetch')
                
                let opts = { mode: 'cors' }

                if (this.authStore.isLoggedIn){
                    opts.headers = {
                        'Authorization': `Bearer ${this.authStore.jwtToken}`
                    }
                }

                let results = await fetch('http://localhost:5000/weatherforecast', opts);

                this.isLoading = false;
                this.stateStrings.push('...completed');
```

- If the attempt is either successful (code 200) or authentication failure (401) state so directly

```javascript
  //  async doCall(){    
  //  [...]
                if (results.status == 401){
                    this.stateStrings.push('fetch returned a 401 error: Unauthenticated');
                }
                else            
                if (results.status == 200){
                    
                    this.isSuccess = true;
                    let weather = await results.json();
                    this.stateStrings.push('Call was successful (status code 200)');
                    
                    for (let cast of weather){ this.resultText += `\r\n ${cast.summary}, ${cast.temperatureC} degrees.` }
                }
```

- Otherwise catch all other errors

```javascript
                else
                {
                    this.stateStrings.push('...the call resulted in a different error. Reload and try again')
                }
            }
            catch(err){
                this.stateStrings.push(`...the call resulted in an error: ${err}`)
                this.stateStrings.push('(Verify the URI of the frontend application for CORS issues)')
            }
        } // end async doCall(){
```

`{` _(this wraps up the doCall() method)_ `}`

The component also accesses the **Pinia** store by using the spread `...` operator in the `computed` section.

#### 5e) A protected page example

To provide a protected page, the `isLoggedIn` can be checked as soon as it's completely loaded (this roughly matches the `mounted` part of the Vue lifecycle).  

The `views/AboutView.vue` provides a good playground to work out this logic:

```vue
<script>
import { mapStores } from 'pinia'
import { useAuthStore } from '../stores/auth';

export default{
  computed:{
    ...mapStores(useAuthStore)
  },
  mounted(){
    if ( !this.authStore.isLoggedIn ){
      this.$router.push('/?unauthorized=true');
    }
  }
}
</script>

<template>
  <div class="about">
    <h1>🔒 Only authenticated users may see this page</h1>
  </div>
</template>
```

To exemplify the difference, this component is using the **Options** API of Vue (of Vue2 legacy) rather than the **Composition** API native to Vue3.

It uses the `$router.push()` programmatic navigation action to immediately redirect a user if they are not logged in, based on the **Pinia** store.

### 6) Running it all together

There is one final addition to be made to the project: the code handling the `code` parameter is in the `views/HomeView.vue`, however the `redirect_uri` has been declared to be `/oauth`, which does not exist in the frontend.  

This is easily fixed in the simplest possible way by ensuring the path `/oauth` uses the same page:

```javascript
  routes: [
    {
      path: '/',
      name: 'home',
      component: HomeView
    },
    {
      path: '/oauth',
      name: 'home (auth)',
      component: HomeView
    },
    {
      path: '/about',
      name: 'about',
      // route level code-splitting
      // this generates a separate chunk (About.[hash].js) for this route
      // which is lazy-loaded when the route is visited.
      component: () => import('../views/AboutView.vue')
    },
    {
      path: '/api',
      name: 'API Reader',
      component: () => import('../views/APIReaderView.vue')
    }
  ]
```

`{` _(in the `router/index.js` file)_ `}`

Onwards, **to testing!**

The smooth functioning of the project requires running all of the components:

- The Docker containers with the POSTGRESQL database and the KeyCloak instance
- The C# backend and Minimal API
- The Vue3 frontend running atop Vite

The **Docker** containers should be easy to start from **Docker Desktop**.  
The **Minimal API** should start normally when run using the appropriate command in Visual studio, or via **dotnet** from a command line.  
The **Frontend** can be run using the usual `# npm run dev` from a command line (directly from _VS Code_ if that is the editor of choice).  

This is what should then appear:

![Screenshot of the frontend](./res/completed-frontend.png)
_This is how the frontend should now appear_

Clicking on the **About** link should display a small warning underneath the menu and the **API Reader** should prompt that a `401 Unauthorized` code was returned.

![Screenshot of the frontend](./res/frontend-unauthenticated-api.png)
_The prompt for a 401_

If a Network error message appears instead, a communication error has occured between the **Frontend** and the **Minimal API** and the URIs on which they are running should be checked (the Minimal api should be running on `http://localhost:5000` and this can be confirmed on the top of the execution console).

Clicking on the link in the home page, the KeyCloak log-in page should come up, without errors. It's possible to **check the URI** to ensure that the parameter `redirect_uri` is in fact correct and matches the running frontend:

![Screenshot of the frontend](./res/frontend-keycloak-login.png)
_The parameters on top should match when the TEST realm login appears_

Once logged in, **enjoy** and test thoroughly the API responses and the changes that should appear in the UI once log-in has been completed.
You now have a three-component application with Authentication driven by a separate, secure Identity Server.
