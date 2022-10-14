using Microsoft.IdentityModel.Tokens;
using PrettyNeat.Keycloak;
using PrettyNeat.Keycloak.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ExampleAPI
{
    public class OauthEndpoint
    {
        /// <summary>
        /// Process an incoming Oauth code 
        /// </summary>
        /// <param name="req">HttpRequest, from incoming context</param>
        /// <param name="kc">KeyCloak Provider, from Services</param>
        /// <param name="config">Configuration interface, from Services</param>
        /// <returns>Error status code or token in JSON</returns>
        public static async Task<IResult> Process(HttpRequest req, KeyCloakProvider kc, IConfiguration config)
        {
            //check for the presence of a "code" in the query
            if (!req.Query.Any(q => q.Key.Equals("code", StringComparison.InvariantCultureIgnoreCase)))
                return Results.BadRequest("No code could be found");

            //collect the code
            string? code = req.Query["code"].SingleOrDefault();

            //code must not be empty
            if (string.IsNullOrWhiteSpace(code))
                return Results.BadRequest("No valid code could be found");

            //attempt to validate and retrieve user-info
            var kcResults = await kc.BrokeredCodeLogin(code);

            if ( kcResults.Success)
            {
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
            }

            return Results.StatusCode(kcResults.HttpErrorCode ?? 400);
        }

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
    }
}
