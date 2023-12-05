﻿// <auto-generated />
using System;
using MdcAi.ChatUI.LocalDal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace MdcAi.ChatUI.LocalDal.Migrations
{
    [DbContext(typeof(UserProfileDbContext))]
    [Migration("20231205224053_GhostColumnRemoval")]
    partial class GhostColumnRemoval
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "7.0.14");

            modelBuilder.Entity("MdcAi.ChatUI.LocalDal.DbConversation", b =>
                {
                    b.Property<string>("IdConversation")
                        .HasColumnType("TEXT");

                    b.Property<string>("Category")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CreatedTs")
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsTrash")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.HasKey("IdConversation");

                    b.ToTable("Conversations");
                });

            modelBuilder.Entity("MdcAi.ChatUI.LocalDal.DbMessage", b =>
                {
                    b.Property<string>("IdMessage")
                        .HasColumnType("TEXT");

                    b.Property<string>("Content")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CreatedTs")
                        .HasColumnType("TEXT");

                    b.Property<string>("DbConversationIdConversation")
                        .HasColumnType("TEXT");

                    b.Property<string>("HTMLContent")
                        .HasColumnType("TEXT");

                    b.Property<string>("IdConversation")
                        .HasColumnType("TEXT");

                    b.Property<string>("IdMessageParent")
                        .HasColumnType("TEXT");

                    b.Property<int>("Index")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsCurrentVersion")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Role")
                        .HasColumnType("TEXT");

                    b.Property<int>("Version")
                        .HasColumnType("INTEGER");

                    b.HasKey("IdMessage");

                    b.HasIndex("DbConversationIdConversation");

                    b.ToTable("Messages");
                });

            modelBuilder.Entity("MdcAi.ChatUI.LocalDal.DbMessage", b =>
                {
                    b.HasOne("MdcAi.ChatUI.LocalDal.DbConversation", null)
                        .WithMany("Messages")
                        .HasForeignKey("DbConversationIdConversation");
                });

            modelBuilder.Entity("MdcAi.ChatUI.LocalDal.DbConversation", b =>
                {
                    b.Navigation("Messages");
                });
#pragma warning restore 612, 618
        }
    }
}
