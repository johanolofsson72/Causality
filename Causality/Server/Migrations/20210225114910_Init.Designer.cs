﻿// <auto-generated />
using Causality.Server.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Causality.Server.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20210225114910_Init")]
    partial class Init
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "5.0.2");

            modelBuilder.Entity("Causality.Shared.Models.Cause", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("ClassId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("EventId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Order")
                        .HasColumnType("INTEGER");

                    b.Property<string>("UpdatedDate")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Value")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("ClassId");

                    b.HasIndex("EventId");

                    b.HasIndex("Id");

                    b.HasIndex("Id", "EventId", "ClassId");

                    b.ToTable("Cause");
                });

            modelBuilder.Entity("Causality.Shared.Models.Class", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("EventId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Order")
                        .HasColumnType("INTEGER");

                    b.Property<string>("UpdatedDate")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Value")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("EventId");

                    b.HasIndex("Id");

                    b.HasIndex("Id", "EventId");

                    b.ToTable("Class");
                });

            modelBuilder.Entity("Causality.Shared.Models.Effect", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("CauseId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ClassId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("EventId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("UpdatedDate")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("UserId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Value")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("CauseId");

                    b.HasIndex("ClassId");

                    b.HasIndex("EventId");

                    b.HasIndex("Id");

                    b.HasIndex("Id", "EventId", "CauseId", "ClassId", "UserId");

                    b.ToTable("Effect");
                });

            modelBuilder.Entity("Causality.Shared.Models.Event", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("Order")
                        .HasColumnType("INTEGER");

                    b.Property<string>("UpdatedDate")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Value")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("Id");

                    b.ToTable("Event");
                });

            modelBuilder.Entity("Causality.Shared.Models.Exclude", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("CauseId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("EventId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("UpdatedDate")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("UserId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Value")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("CauseId");

                    b.HasIndex("EventId");

                    b.HasIndex("Id");

                    b.HasIndex("UserId");

                    b.HasIndex("Id", "EventId", "CauseId", "UserId");

                    b.ToTable("Exclude");
                });

            modelBuilder.Entity("Causality.Shared.Models.Meta", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("CauseId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ClassId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("EffectId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("EventId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ExcludeId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Key")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("ProcessId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ResultId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("StateId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("UpdatedDate")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("UserId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Value")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("CauseId");

                    b.HasIndex("ClassId");

                    b.HasIndex("EffectId");

                    b.HasIndex("EventId");

                    b.HasIndex("ExcludeId");

                    b.HasIndex("Id");

                    b.HasIndex("ProcessId");

                    b.HasIndex("ResultId");

                    b.HasIndex("StateId");

                    b.HasIndex("UserId");

                    b.ToTable("Meta");
                });

            modelBuilder.Entity("Causality.Shared.Models.Process", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("EventId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Order")
                        .HasColumnType("INTEGER");

                    b.Property<string>("UpdatedDate")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Value")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("Id");

                    b.HasIndex("Id", "EventId");

                    b.ToTable("Process");
                });

            modelBuilder.Entity("Causality.Shared.Models.Result", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("CauseId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ClassId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("EventId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ProcessId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("UpdatedDate")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("UserId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Value")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("Id");

                    b.HasIndex("Id", "ProcessId", "EventId", "CauseId", "ClassId", "UserId");

                    b.ToTable("Result");
                });

            modelBuilder.Entity("Causality.Shared.Models.State", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("CauseId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ClassId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("EventId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("UpdatedDate")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("UserId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Value")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("Id");

                    b.HasIndex("Id", "EventId", "CauseId", "ClassId", "UserId");

                    b.ToTable("State");
                });

            modelBuilder.Entity("Causality.Shared.Models.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("IP")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("UID")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("UpdatedDate")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("Id");

                    b.ToTable("User");
                });

            modelBuilder.Entity("Causality.Shared.Models.Cause", b =>
                {
                    b.HasOne("Causality.Shared.Models.Class", null)
                        .WithMany("Causes")
                        .HasForeignKey("ClassId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Causality.Shared.Models.Event", null)
                        .WithMany("Causes")
                        .HasForeignKey("EventId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Causality.Shared.Models.Class", b =>
                {
                    b.HasOne("Causality.Shared.Models.Event", null)
                        .WithMany("Classes")
                        .HasForeignKey("EventId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Causality.Shared.Models.Effect", b =>
                {
                    b.HasOne("Causality.Shared.Models.Cause", null)
                        .WithMany("Effects")
                        .HasForeignKey("CauseId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Causality.Shared.Models.Class", null)
                        .WithMany("Effects")
                        .HasForeignKey("ClassId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Causality.Shared.Models.Event", null)
                        .WithMany("Effects")
                        .HasForeignKey("EventId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Causality.Shared.Models.Exclude", b =>
                {
                    b.HasOne("Causality.Shared.Models.Cause", null)
                        .WithMany("Excludes")
                        .HasForeignKey("CauseId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Causality.Shared.Models.Event", null)
                        .WithMany("Excludes")
                        .HasForeignKey("EventId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Causality.Shared.Models.User", null)
                        .WithMany("Excludes")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Causality.Shared.Models.Meta", b =>
                {
                    b.HasOne("Causality.Shared.Models.Cause", null)
                        .WithMany("Metas")
                        .HasForeignKey("CauseId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Causality.Shared.Models.Class", null)
                        .WithMany("Metas")
                        .HasForeignKey("ClassId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Causality.Shared.Models.Effect", null)
                        .WithMany("Metas")
                        .HasForeignKey("EffectId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Causality.Shared.Models.Event", null)
                        .WithMany("Metas")
                        .HasForeignKey("EventId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Causality.Shared.Models.Exclude", null)
                        .WithMany("Metas")
                        .HasForeignKey("ExcludeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Causality.Shared.Models.Process", null)
                        .WithMany("Metas")
                        .HasForeignKey("ProcessId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Causality.Shared.Models.Result", null)
                        .WithMany("Metas")
                        .HasForeignKey("ResultId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Causality.Shared.Models.State", null)
                        .WithMany("Metas")
                        .HasForeignKey("StateId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Causality.Shared.Models.User", null)
                        .WithMany("Metas")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Causality.Shared.Models.Cause", b =>
                {
                    b.Navigation("Effects");

                    b.Navigation("Excludes");

                    b.Navigation("Metas");
                });

            modelBuilder.Entity("Causality.Shared.Models.Class", b =>
                {
                    b.Navigation("Causes");

                    b.Navigation("Effects");

                    b.Navigation("Metas");
                });

            modelBuilder.Entity("Causality.Shared.Models.Effect", b =>
                {
                    b.Navigation("Metas");
                });

            modelBuilder.Entity("Causality.Shared.Models.Event", b =>
                {
                    b.Navigation("Causes");

                    b.Navigation("Classes");

                    b.Navigation("Effects");

                    b.Navigation("Excludes");

                    b.Navigation("Metas");
                });

            modelBuilder.Entity("Causality.Shared.Models.Exclude", b =>
                {
                    b.Navigation("Metas");
                });

            modelBuilder.Entity("Causality.Shared.Models.Process", b =>
                {
                    b.Navigation("Metas");
                });

            modelBuilder.Entity("Causality.Shared.Models.Result", b =>
                {
                    b.Navigation("Metas");
                });

            modelBuilder.Entity("Causality.Shared.Models.State", b =>
                {
                    b.Navigation("Metas");
                });

            modelBuilder.Entity("Causality.Shared.Models.User", b =>
                {
                    b.Navigation("Excludes");

                    b.Navigation("Metas");
                });
#pragma warning restore 612, 618
        }
    }
}