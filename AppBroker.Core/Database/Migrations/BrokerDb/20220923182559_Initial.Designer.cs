﻿// <auto-generated />
using System;

using AppBroker.Core.Database;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace AppBroker.Core.Database.Migrations.BrokerDb
{
    [DbContext(typeof(BrokerDbContext))]
    [Migration("20220923182559_Initial")]
    partial class Initial
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "6.0.7");

            modelBuilder.Entity("AppBrokerASP.Database.Model.DeviceMappingModel", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<long?>("ChildId")
                        .HasColumnType("INTEGER");

                    b.Property<long?>("ParentId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("ChildId");

                    b.HasIndex("ParentId");

                    b.ToTable("DeviceToDeviceMappings");
                });

            modelBuilder.Entity("AppBrokerASP.Database.Model.DeviceModel", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("FriendlyName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("TypeName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Devices");
                });

            modelBuilder.Entity("AppBrokerASP.Database.Model.HeaterConfigModel", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<byte>("DayOfWeek")
                        .HasColumnType("INTEGER");

                    b.Property<long?>("DeviceId")
                        .HasColumnType("INTEGER");

                    b.Property<double>("Temperature")
                        .HasColumnType("REAL");

                    b.Property<DateTime>("TimeOfDay")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("DeviceId");

                    b.ToTable("HeaterConfigs");
                });

            modelBuilder.Entity("AppBrokerASP.Database.Model.DeviceMappingModel", b =>
                {
                    b.HasOne("AppBrokerASP.Database.Model.DeviceModel", "Child")
                        .WithMany()
                        .HasForeignKey("ChildId");

                    b.HasOne("AppBrokerASP.Database.Model.DeviceModel", "Parent")
                        .WithMany()
                        .HasForeignKey("ParentId");

                    b.Navigation("Child");

                    b.Navigation("Parent");
                });

            modelBuilder.Entity("AppBrokerASP.Database.Model.HeaterConfigModel", b =>
                {
                    b.HasOne("AppBrokerASP.Database.Model.DeviceModel", "Device")
                        .WithMany()
                        .HasForeignKey("DeviceId");

                    b.Navigation("Device");
                });
#pragma warning restore 612, 618
        }
    }
}
