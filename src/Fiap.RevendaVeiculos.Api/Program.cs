using Fiap.RevendaVeiculos.Application;
using Fiap.RevendaVeiculos.Domain;
using Fiap.RevendaVeiculos.Infrastructure;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var bearerScheme = new OpenApiSecurityScheme
    {
        Description = "Informe somente o token JWT. O Swagger adiciona automaticamente o prefixo Bearer.",
        Name = "Authorization", In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT"
    };
    options.AddSecurityDefinition("Bearer", bearerScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", null, null)] = []
    });
});
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is required.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = true, ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true, ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateLifetime = true, ClockSkew = TimeSpan.FromMinutes(1)
    };
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("ADM"));
    options.AddPolicy("Buyer", policy => policy.RequireRole("COMPRADOR"));
});
builder.Services.AddMongoInfrastructure(o => { o.ConnectionString = builder.Configuration.GetConnectionString("Mongo")!; o.DatabaseName = builder.Configuration["Mongo:DatabaseName"] ?? "revenda_veiculos"; });
builder.Services.AddSingleton<VehicleService>();
builder.Services.AddHealthChecks();
var app = builder.Build();
await app.Services.GetRequiredService<MongoInitializer>().InitializeAsync(CancellationToken.None);
app.UseExceptionHandler(e => e.Run(async c => { var x=c.Features.Get<IExceptionHandlerFeature>()?.Error; var s=x is NotFoundException?404:x is ConflictException or DomainException?409:500; await Results.Problem(statusCode:s,title:s==404?"Resource not found":s==409?"Business rule conflict":"Unexpected error",detail:app.Environment.IsDevelopment()?x?.Message:null).ExecuteAsync(c); }));
app.UseSwagger(); app.UseSwaggerUI(); app.MapHealthChecks("/health/live"); app.MapHealthChecks("/health/ready");
app.UseAuthentication(); app.UseAuthorization();
var v=app.MapGroup("/api/v1/vehicles").WithTags("Vehicles");
v.MapPost("",async(VehicleRequest r,VehicleService s,CancellationToken ct)=>{try{var x=await s.CreateAsync(r.ToInput(),ct);return Results.Created($"/api/v1/vehicles/{x.Id}",x);}catch(DomainException e){return Results.ValidationProblem(new Dictionary<string,string[]>{{"request",[e.Message]}});}}).RequireAuthorization("Admin");
v.MapGet("/{id}",async(string id,VehicleService s,CancellationToken ct)=>Results.Ok(await s.GetAsync(id,ct)));
v.MapPut("/{id}",async(string id,VehicleRequest r,VehicleService s,CancellationToken ct)=>{try{return Results.Ok(await s.UpdateAsync(id,r.ToInput(),ct));}catch(DomainException e){return Results.ValidationProblem(new Dictionary<string,string[]>{{"request",[e.Message]}});}}).RequireAuthorization("Admin");
v.MapGet("/available",async(int? page,int? pageSize,VehicleService s,CancellationToken ct)=>Results.Ok(await s.ListAvailableAsync(Page(page),Size(pageSize),ct)));
v.MapPost("/{id}/purchases",async(string id,PurchaseRequest r,ClaimsPrincipal user,VehicleService s,CancellationToken ct)=>{var userId=user.FindFirstValue(ClaimTypes.NameIdentifier)??user.FindFirstValue("sub");if(string.IsNullOrWhiteSpace(userId))return Results.Unauthorized();try{var x=await s.StartSaleAsync(id,new(userId,r.PaymentCode),ct);return Results.Created($"/api/v1/purchases/{x.Id}",x);}catch(DomainException e){return Results.ValidationProblem(new Dictionary<string,string[]>{{"request",[e.Message]}});}}).RequireAuthorization("Buyer");
v.MapGet("/sold",async(int? page,int? pageSize,VehicleService s,CancellationToken ct)=>Results.Ok(await s.ListSoldAsync(Page(page),Size(pageSize),ct)));
app.MapPost("/api/v1/payments/webhook",async(PaymentWebhookRequest r,HttpRequest h,IConfiguration c,VehicleService s,CancellationToken ct)=>{if(!h.Headers.TryGetValue("X-Webhook-Secret",out var key)||key!=c["Webhook:Secret"])return Results.Unauthorized();if(!Enum.TryParse<PaymentStatus>(r.Status,true,out var status)||status==PaymentStatus.Pending)return Results.ValidationProblem(new Dictionary<string,string[]>{{"status",["Status must be PAID or CANCELED."]}});await s.ProcessPaymentAsync(r.PaymentCode,status,ct);return Results.NoContent();}).WithTags("Payments");
v.RequireAuthorization();
app.Run();
static int Page(int? x)=>x is > 0?x.Value:1; static int Size(int? x)=>x is > 0 and <=100?x.Value:20;
public record VehicleRequest(string Brand,string Model,int Year,string Color,decimal Price){public VehicleInput ToInput()=>new(Brand,Model,Year,Color,Price);} public record PurchaseRequest(string PaymentCode); public record PaymentWebhookRequest(string PaymentCode,string Status); public partial class Program{}
