using BibliotecaProject.Domain.Entities;
using BibliotecaProject.Domain.Interfaces;
using BibliotecaProject.Domain.Services;
using BibliotecaProject.Infrastructure.Data;
using BibliotecaProject.Presentation.Controllers;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
builder.Services.AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<EmprestimoUsuarioValidator>());
builder.Services.AddScoped<Ilivro, LivroServices>();
builder.Services.AddScoped<IMembro, MembroServices>();
builder.Services.AddScoped<IEmprestimo, EmprestimoServices>();
builder.Services.AddDbContext<BibliotecaDbContext>(options =>
{
  options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
    app.UseSwagger();
    app.UseSwaggerUI();


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
