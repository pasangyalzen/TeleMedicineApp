using System.Security.Claims;
using System.Text;
using TeleMedicineApp.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SQLHelper;
using Swashbuckle.AspNetCore.Filters;
using TeleMedicineApp.Areas.Admin.Provider;
//using TeleMedicineApp.Areas.Appointments.Provider;
using TeleMedicineApp.Data;
using TeleMedicineApp.Models;
using TeleMedicineApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

//Add CORS policy 
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        builder =>
        {
            builder
                .WithOrigins("http://localhost:3000","http://localhost:5173") // React app origin
                .AllowAnyMethod() // Allow all HTTP methods (GET, POST, etc.)
                .AllowAnyHeader() // Allow all headers
                .AllowCredentials(); // Allow cookies/credentials if needed
        });
});

builder.Services.AddSwaggerGen(options =>
{
    // Define Swagger docs for each area (Admin, Customer, etc.)
    // options.SwaggerDoc("admin", new OpenApiInfo { Title = "Admin API", Version = "v1" });
    // options.SwaggerDoc("client", new OpenApiInfo { Title = "Client API", Version = "v1" });
    //
    // // Include routes by area in their corresponding Swagger groups
    // options.DocInclusionPredicate((docName, apiDesc) =>
    // {
    //     var actionDescriptor = apiDesc.ActionDescriptor;
    //
    //     // Check if the route has the "area" value (from the route)
    //     var area = actionDescriptor.RouteValues.ContainsKey("area")
    //         ? actionDescriptor.RouteValues["area"]
    //         : "default";  // Default area if none is provided
    //
    //     // Only include API actions in the appropriate Swagger document for the area
    //     return docName.ToLower() == area.ToLower();
    // });

    // Optionally, you can set up custom operation filters for controller-level tagging
    // options.OperationFilter<CustomTagsOperationFilter>();
    
    // Add options to authenticate in Swagger UI 
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Add JWT Configuration
var jwtConfig = builder.Configuration.GetSection("JWT").Get<JwtConfig>();
builder.Services.AddSingleton(jwtConfig);
builder.Services.AddScoped<DoctorManager>();
builder.Services.AddScoped<AppointmentManager>();  // Add this line
builder.Services.AddScoped<PatientManager>();
builder.Services.AddScoped<PharmacistManager>();
builder.Services.AddScoped<IJwtService, JwtService>();
//builder.Services.AddScoped<AppointmentManager>();

builder.Services.AddScoped<EmailService>();
// Add JWT Authentication
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtConfig.Issuer,
            ValidAudience = jwtConfig.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.Secret)),
            RoleClaimType = ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdminOnly", policy => 
        policy.RequireRole("SuperAdmin"));
        
    options.AddPolicy("AdminAndAbove", policy =>
        policy.RequireRole("Admin", "SuperAdmin"));
    
    options.AddPolicy("DoctorOnly", policy => 
        policy.RequireRole("Doctor"));
        
    // Patient only policy
    options.AddPolicy("PatientOnly", policy => 
        policy.RequireRole("Patient"));
        
    // Pharmacist only policy
    options.AddPolicy("PharmacistOnly", policy => 
        policy.RequireRole("Pharmacist"));
    
    options.AddPolicy("DoctorAndAbove", policy =>
        policy.RequireRole("Doctor", "Admin", "SuperAdmin"));
    
    options.AddPolicy("DoctorAndAbove", policy =>
        policy.RequireRole("Doctor", "Admin", "SuperAdmin"));
    
    options.AddPolicy("PatientAndAbove", policy =>
        policy.RequireRole("Patient", "Doctor", "Admin", "SuperAdmin"));
        
    options.AddPolicy("AllUsers", policy =>
        policy.RequireRole("SuperAdmin", "Admin", "Doctor", "Patient", "Pharmacist"));
        
    // Fallback policy - require authentication by default
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});



builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
    {
        // options.SignIn.RequireConfirmedAccount = true;
        // options.SignIn.RequireConfirmedEmail = true;
        // options.Password.RequireDigit = true;
        // options.Password.RequireLowercase = true;
        // options.Password.RequireUppercase = true;
        // options.Password.RequireNonAlphanumeric = true;
        // options.Password.RequiredLength = 6;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();



var app = builder.Build();
SQLHandlerAsync.Connectionconfig = connectionString;

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var roles = new[] { "SuperAdmin","Admin", "Doctor", "Patient", "Pharmacist" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        // // Display Swagger endpoints for different areas
        // c.SwaggerEndpoint("/swagger/admin/swagger.json", "Admin API v1");
        // // c.SwaggerEndpoint("/swagger/client/swagger.json", "Client API v1");
        //
        // // Set Swagger UI route to the root
        // c.RoutePrefix = string.Empty;  // Swagger UI at the root (http://localhost:5000/)
    });
}

app.UseCors("AllowReactApp");
app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();