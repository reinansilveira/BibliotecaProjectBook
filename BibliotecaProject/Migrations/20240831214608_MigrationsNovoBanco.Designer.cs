﻿// <auto-generated />
using System;
using BibliotecaProject.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace BibliotecaProject.Migrations
{
    [DbContext(typeof(BibliotecaDbContext))]
    [Migration("20240831214608_MigrationsNovoBanco")]
    partial class MigrationsNovoBanco
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("BibliotecaProject.Domain.Entities.Emprestimo", b =>
                {
                    b.Property<Guid>("EmprestimoId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("DataDevolucao")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DataEmprestimo")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("LivroId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("MembroId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<bool>("StatusEmprestimo")
                        .HasColumnType("bit");

                    b.HasKey("EmprestimoId");

                    b.ToTable("Emprestimos");
                });

            modelBuilder.Entity("BibliotecaProject.Domain.Entities.Livro", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Autor")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("DataCriacao")
                        .HasColumnType("datetime2");

                    b.Property<string>("DescricaoLivro")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("LivroDeletado")
                        .HasColumnType("bit");

                    b.Property<string>("NomeLivro")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Livros");
                });

            modelBuilder.Entity("BibliotecaProject.Domain.Entities.Membro", b =>
                {
                    b.Property<Guid>("IdMembro")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("DataNascimento")
                        .HasColumnType("datetime2");

                    b.Property<bool>("Deletado")
                        .HasColumnType("bit");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Nome")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Telefone")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("IdMembro");

                    b.ToTable("Membros");
                });
#pragma warning restore 612, 618
        }
    }
}
