﻿// <auto-generated />
using System;
using AppBrokerASP.Zigbee2Mqtt;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace AppBrokerASP.Migrations
{
    [DbContext(typeof(HistoryManager.HistoryContext))]
    [Migration("20220918143339_HeaterConfigs")]
    partial class HeaterConfigs
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "6.0.7");

            modelBuilder.Entity("AppBrokerASP.Zigbee2Mqtt.HistoryManager+HistoryDevice", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<long>("DeviceId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Devices");
                });

            modelBuilder.Entity("AppBrokerASP.Zigbee2Mqtt.HistoryManager+HistoryProperty", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("DeviceId")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("Enabled")
                        .HasColumnType("INTEGER");

                    b.Property<string>("PropertyName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("DeviceId");

                    b.ToTable("Properties");
                });

            modelBuilder.Entity("AppBrokerASP.Zigbee2Mqtt.HistoryManager+HistoryValueBase", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<long>("HistoryValueId")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("HistoryValueId");

                    b.ToTable("HistoryValueBase");
                });

            modelBuilder.Entity("AppBrokerASP.Zigbee2Mqtt.HistoryManager+HistoryValueBool", b =>
                {
                    b.HasBaseType("AppBrokerASP.Zigbee2Mqtt.HistoryManager+HistoryValueBase");

                    b.Property<bool>("Value")
                        .HasColumnType("INTEGER");

                    b.ToTable("HistoryValueBool");
                });

            modelBuilder.Entity("AppBrokerASP.Zigbee2Mqtt.HistoryManager+HistoryValueDateTime", b =>
                {
                    b.HasBaseType("AppBrokerASP.Zigbee2Mqtt.HistoryManager+HistoryValueBase");

                    b.Property<DateTime>("Value")
                        .HasColumnType("TEXT");

                    b.ToTable("HistoryValueDateTime");
                });

            modelBuilder.Entity("AppBrokerASP.Zigbee2Mqtt.HistoryManager+HistoryValueDouble", b =>
                {
                    b.HasBaseType("AppBrokerASP.Zigbee2Mqtt.HistoryManager+HistoryValueBase");

                    b.Property<double>("Value")
                        .HasColumnType("REAL");

                    b.ToTable("HistoryValueDouble");
                });

            modelBuilder.Entity("AppBrokerASP.Zigbee2Mqtt.HistoryManager+HistoryValueHeaterConfig", b =>
                {
                    b.HasBaseType("AppBrokerASP.Zigbee2Mqtt.HistoryManager+HistoryValueBase");

                    b.Property<int>("DayOfWeek")
                        .HasColumnType("INTEGER");

                    b.Property<double>("Temperature")
                        .HasColumnType("REAL");

                    b.Property<DateTime>("TimeOfDay")
                        .HasColumnType("TEXT");

                    b.ToTable("HistoryValueHeaterConfig");
                });

            modelBuilder.Entity("AppBrokerASP.Zigbee2Mqtt.HistoryManager+HistoryValueLong", b =>
                {
                    b.HasBaseType("AppBrokerASP.Zigbee2Mqtt.HistoryManager+HistoryValueBase");

                    b.Property<long>("Value")
                        .HasColumnType("INTEGER");

                    b.ToTable("HistoryValueLong");
                });

            modelBuilder.Entity("AppBrokerASP.Zigbee2Mqtt.HistoryManager+HistoryValueString", b =>
                {
                    b.HasBaseType("AppBrokerASP.Zigbee2Mqtt.HistoryManager+HistoryValueBase");

                    b.Property<string>("Value")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.ToTable("HistoryValueString");
                });

            modelBuilder.Entity("AppBrokerASP.Zigbee2Mqtt.HistoryManager+HistoryValueTimeSpan", b =>
                {
                    b.HasBaseType("AppBrokerASP.Zigbee2Mqtt.HistoryManager+HistoryValueBase");

                    b.Property<TimeSpan>("Value")
                        .HasColumnType("TEXT");

                    b.ToTable("HistoryValueTimeSpan");
                });

            modelBuilder.Entity("AppBrokerASP.Zigbee2Mqtt.HistoryManager+HistoryProperty", b =>
                {
                    b.HasOne("AppBrokerASP.Zigbee2Mqtt.HistoryManager+HistoryDevice", "Device")
                        .WithMany("HistoryValues")
                        .HasForeignKey("DeviceId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Device");
                });

            modelBuilder.Entity("AppBrokerASP.Zigbee2Mqtt.HistoryManager+HistoryValueBase", b =>
                {
                    b.HasOne("AppBrokerASP.Zigbee2Mqtt.HistoryManager+HistoryProperty", "HistoryValue")
                        .WithMany("Values")
                        .HasForeignKey("HistoryValueId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("HistoryValue");
                });

            modelBuilder.Entity("AppBrokerASP.Zigbee2Mqtt.HistoryManager+HistoryValueBool", b =>
                {
                    b.HasOne("AppBrokerASP.Zigbee2Mqtt.HistoryManager+HistoryValueBase", null)
                        .WithOne()
                        .HasForeignKey("AppBrokerASP.Zigbee2Mqtt.HistoryManager+HistoryValueBool", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("AppBrokerASP.Zigbee2Mqtt.HistoryManager+HistoryValueDateTime", b =>
                {
                    b.HasOne("AppBrokerASP.Zigbee2Mqtt.HistoryManager+HistoryValueBase", null)
                        .WithOne()
                        .HasForeignKey("AppBrokerASP.Zigbee2Mqtt.HistoryManager+HistoryValueDateTime", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("AppBrokerASP.Zigbee2Mqtt.HistoryManager+HistoryValueDouble", b =>
                {
                    b.HasOne("AppBrokerASP.Zigbee2Mqtt.HistoryManager+HistoryValueBase", null)
                        .WithOne()
                        .HasForeignKey("AppBrokerASP.Zigbee2Mqtt.HistoryManager+HistoryValueDouble", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("AppBrokerASP.Zigbee2Mqtt.HistoryManager+HistoryValueHeaterConfig", b =>
                {
                    b.HasOne("AppBrokerASP.Zigbee2Mqtt.HistoryManager+HistoryValueBase", null)
                        .WithOne()
                        .HasForeignKey("AppBrokerASP.Zigbee2Mqtt.HistoryManager+HistoryValueHeaterConfig", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("AppBrokerASP.Zigbee2Mqtt.HistoryManager+HistoryValueLong", b =>
                {
                    b.HasOne("AppBrokerASP.Zigbee2Mqtt.HistoryManager+HistoryValueBase", null)
                        .WithOne()
                        .HasForeignKey("AppBrokerASP.Zigbee2Mqtt.HistoryManager+HistoryValueLong", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("AppBrokerASP.Zigbee2Mqtt.HistoryManager+HistoryValueString", b =>
                {
                    b.HasOne("AppBrokerASP.Zigbee2Mqtt.HistoryManager+HistoryValueBase", null)
                        .WithOne()
                        .HasForeignKey("AppBrokerASP.Zigbee2Mqtt.HistoryManager+HistoryValueString", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("AppBrokerASP.Zigbee2Mqtt.HistoryManager+HistoryValueTimeSpan", b =>
                {
                    b.HasOne("AppBrokerASP.Zigbee2Mqtt.HistoryManager+HistoryValueBase", null)
                        .WithOne()
                        .HasForeignKey("AppBrokerASP.Zigbee2Mqtt.HistoryManager+HistoryValueTimeSpan", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("AppBrokerASP.Zigbee2Mqtt.HistoryManager+HistoryDevice", b =>
                {
                    b.Navigation("HistoryValues");
                });

            modelBuilder.Entity("AppBrokerASP.Zigbee2Mqtt.HistoryManager+HistoryProperty", b =>
                {
                    b.Navigation("Values");
                });
#pragma warning restore 612, 618
        }
    }
}
