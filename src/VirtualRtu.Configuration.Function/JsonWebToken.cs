using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Threading;
using Microsoft.IdentityModel.Tokens;

namespace VirtualRtu.Configuration.Function
{
    public class JsonWebToken : SecurityToken
    {
        private readonly DateTime created;
        private readonly DateTime expires;
        private readonly string tokenString;

        public JsonWebToken(string securityKey, IEnumerable<Claim> claims, double? lifetimeMinutes,
            string issuer = null, string audience = null)
        {
            Issuer = issuer;
            Id = Guid.NewGuid().ToString();
            created = DateTime.UtcNow;
            expires = created.AddMinutes(lifetimeMinutes.HasValue ? lifetimeMinutes.Value : 20);
            SigningKey = new SymmetricSecurityKey(Convert.FromBase64String(securityKey));

            JwtSecurityTokenHandler jwt = new JwtSecurityTokenHandler();
            SecurityTokenDescriptor msstd = new SecurityTokenDescriptor
            {
                Issuer = issuer,
                Subject = new ClaimsIdentity(claims),
                Expires = expires,
                IssuedAt = created,
                NotBefore = created,
                Audience = audience,
                SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256Signature)
            };

            try
            {
                JwtSecurityToken jwtToken = jwt.CreateJwtSecurityToken(msstd);
                tokenString = jwt.WriteToken(jwtToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public JsonWebToken(Uri address, string securityKey, string issuer, IEnumerable<Claim> claims)
        {
            Issuer = issuer;
            Id = Guid.NewGuid().ToString();
            created = DateTime.UtcNow;
            expires = created.AddMinutes(20);
            SigningKey = new SymmetricSecurityKey(Convert.FromBase64String(securityKey));

            JwtSecurityTokenHandler jwt = new JwtSecurityTokenHandler();
            SecurityTokenDescriptor msstd = new SecurityTokenDescriptor
            {
                Issuer = issuer,
                Subject = new ClaimsIdentity(claims),
                Expires = expires,
                IssuedAt = created,
                NotBefore = created,
                Audience = address.ToString(),
                SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256Signature)
            };

            JwtSecurityToken jwtToken = jwt.CreateJwtSecurityToken(msstd);
            tokenString = jwt.WriteToken(jwtToken);
        }

        public JsonWebToken(Uri audience, string securityKey, string issuer, IEnumerable<Claim> claims,
            double lifetimeMinutes)
        {
            Issuer = issuer;
            Id = Guid.NewGuid().ToString();
            created = DateTime.UtcNow;
            expires = created.AddMinutes(lifetimeMinutes);
            SigningKey = new SymmetricSecurityKey(Convert.FromBase64String(securityKey));

            JwtSecurityTokenHandler jwt = new JwtSecurityTokenHandler();
            SecurityTokenDescriptor msstd = new SecurityTokenDescriptor
            {
                Issuer = issuer,
                Subject = new ClaimsIdentity(claims),
                Expires = expires,
                IssuedAt = created,
                NotBefore = created,
                Audience = audience.ToString(),
                SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256Signature)
            };


            JwtSecurityToken jwtToken = jwt.CreateJwtSecurityToken(msstd);
            tokenString = jwt.WriteToken(jwtToken);
        }

        public override string Id { get; }


        public override DateTime ValidFrom => created;

        public override DateTime ValidTo => expires;

        public override string Issuer { get; }


        public override SecurityKey SecurityKey => null;

        public override SecurityKey SigningKey { get; set; }


        public override string ToString()
        {
            return tokenString;
        }

        public void SetSecurityToken(HttpWebRequest request)
        {
            request.Headers.Add("Authorization", string.Format("Bearer {0}", tokenString));
        }

        public static void Authenticate(string token, string issuer, string audience, string signingKey)
        {
            try
            {
                JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();

                TokenValidationParameters validationParameters = new TokenValidationParameters
                {
                    IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(signingKey)),
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    ValidateAudience = true,
                    ValidateIssuer = true,
                    ValidateIssuerSigningKey = true
                };

                SecurityToken stoken = null;

                Thread.CurrentPrincipal = tokenHandler.ValidateToken(token, validationParameters, out stoken);
            }
            catch (SecurityTokenValidationException e)
            {
                Trace.TraceWarning("JWT validation has security token exception.");
                Trace.TraceError(e.Message);
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Exception in JWT validation.");
                Trace.TraceError(ex.Message);
            }
        }
    }
}