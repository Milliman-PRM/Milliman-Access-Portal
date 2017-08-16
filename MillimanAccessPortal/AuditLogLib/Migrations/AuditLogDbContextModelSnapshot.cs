using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using AuditLogLib;

namespace AuditLogLib.Migrations
{
    [DbContext(typeof(AuditLogDbContext))]
    partial class AuditLogDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .HasAnnotation("ProductVersion", "1.1.2");

            modelBuilder.Entity("AuditLogLib.AuditEvent", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("EventDetailJsonb")
                        .HasColumnType("jsonb");

                    b.Property<string>("EventType");

                    b.Property<string>("Source");

                    b.Property<string>("Summary");

                    b.Property<DateTime>("TimeStamp");

                    b.Property<string>("User");

                    b.HasKey("Id");

                    b.ToTable("AuditEvent");
                });
        }
    }
}
