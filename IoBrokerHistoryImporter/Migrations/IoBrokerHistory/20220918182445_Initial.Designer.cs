﻿// <auto-generated />
using IoBrokerHistoryImporter;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace IoBrokerHistoryImporter.Migrations.IoBrokerHistory
{
    [DbContext(typeof(IoBrokerHistoryContext))]
    [Migration("20220918182445_Initial")]
    partial class Initial
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "6.0.9");

            modelBuilder.Entity("IoBrokerHistoryImporter.Datapoint", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("id");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("name");

                    b.Property<int>("Type")
                        .HasColumnType("INTEGER")
                        .HasColumnName("type");

                    b.HasKey("Id");

                    b.ToTable("datapoints");
                });

            modelBuilder.Entity("IoBrokerHistoryImporter.BoolValue", b =>
                {
                    b.HasBaseType("IoBrokerHistoryImporter.Datapoint");

                    b.Property<long>("Ts")
                        .HasColumnType("INTEGER")
                        .HasColumnName("ts");

                    b.Property<bool>("Val")
                        .HasColumnType("INTEGER")
                        .HasColumnName("val");

                    b.ToTable("ts_bool");
                });

            modelBuilder.Entity("IoBrokerHistoryImporter.DoubleValue", b =>
                {
                    b.HasBaseType("IoBrokerHistoryImporter.Datapoint");

                    b.Property<long>("Ts")
                        .HasColumnType("INTEGER")
                        .HasColumnName("ts");

                    b.Property<double>("Val")
                        .HasColumnType("REAL")
                        .HasColumnName("val");

                    b.ToTable("ts_number");
                });

            modelBuilder.Entity("IoBrokerHistoryImporter.StringValue", b =>
                {
                    b.HasBaseType("IoBrokerHistoryImporter.Datapoint");

                    b.Property<long>("Ts")
                        .HasColumnType("INTEGER")
                        .HasColumnName("ts");

                    b.Property<string>("Val")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("val");

                    b.ToTable("ts_string");
                });

            modelBuilder.Entity("IoBrokerHistoryImporter.BoolValue", b =>
                {
                    b.HasOne("IoBrokerHistoryImporter.Datapoint", null)
                        .WithOne()
                        .HasForeignKey("IoBrokerHistoryImporter.BoolValue", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("IoBrokerHistoryImporter.DoubleValue", b =>
                {
                    b.HasOne("IoBrokerHistoryImporter.Datapoint", null)
                        .WithOne()
                        .HasForeignKey("IoBrokerHistoryImporter.DoubleValue", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("IoBrokerHistoryImporter.StringValue", b =>
                {
                    b.HasOne("IoBrokerHistoryImporter.Datapoint", null)
                        .WithOne()
                        .HasForeignKey("IoBrokerHistoryImporter.StringValue", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}