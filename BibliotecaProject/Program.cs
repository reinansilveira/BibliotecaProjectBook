using BibliotecaProject.Domain.Interfaces;
using BibliotecaProject.Domain.Services;
using BibliotecaProject.Exceptions;
using BibliotecaProject.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Quartz;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.

builder.Services.AddControllers(); 
builder.Services.AddScoped<Ilivro, LivroServices>();
builder.Services.AddScoped<IMembro, MembroServices>();
builder.Services.AddScoped<IEmprestimo, EmprestimoServices>();
builder.Services.AddScoped<EmprestimoServices>();
builder.Services.AddScoped<BlackListBoot>();
builder.Services.AddDbContext<BibliotecaDbContext>(options =>
{
  options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddQuartz(q =>
{
    q.UseMicrosoftDependencyInjectionJobFactory();

    var jobKey = new JobKey("blacklistJob");
    q.AddJob<BootExecute>(opts => opts.WithIdentity(jobKey));
    
    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("bootTrigger")
        .WithCronSchedule("0 * * ? * *")); 
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var app = builder.Build();

// Configure the HTTP request pipeline.
    app.UseSwagger();
    app.UseSwaggerUI();


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
