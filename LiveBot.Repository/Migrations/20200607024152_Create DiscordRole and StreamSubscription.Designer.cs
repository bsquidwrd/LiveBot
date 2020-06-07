﻿// <auto-generated />
using System;
using LiveBot.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace LiveBot.Repository.Migrations
{
    [DbContext(typeof(LiveBotDBContext))]
    [Migration("20200607024152_Create DiscordRole and StreamSubscription")]
    partial class CreateDiscordRoleandStreamSubscription
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .HasAnnotation("ProductVersion", "3.1.4")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("LiveBot.Core.Repository.Models.Discord.DiscordChannel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<bool>("Deleted")
                        .HasColumnType("boolean");

                    b.Property<int?>("DiscordGuildId")
                        .HasColumnType("integer");

                    b.Property<decimal>("DiscordId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<DateTime>("TimeStamp")
                        .HasColumnType("timestamp without time zone");

                    b.HasKey("Id");

                    b.HasIndex("DiscordGuildId");

                    b.ToTable("DiscordChannel");
                });

            modelBuilder.Entity("LiveBot.Core.Repository.Models.Discord.DiscordGuild", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<bool>("Deleted")
                        .HasColumnType("boolean");

                    b.Property<decimal>("DiscordId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<DateTime>("TimeStamp")
                        .HasColumnType("timestamp without time zone");

                    b.HasKey("Id");

                    b.ToTable("DiscordGuild");
                });

            modelBuilder.Entity("LiveBot.Core.Repository.Models.Discord.DiscordRole", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<bool>("Deleted")
                        .HasColumnType("boolean");

                    b.Property<int?>("DiscordGuildId")
                        .HasColumnType("integer");

                    b.Property<decimal>("DiscordId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<DateTime>("TimeStamp")
                        .HasColumnType("timestamp without time zone");

                    b.HasKey("Id");

                    b.HasIndex("DiscordGuildId");

                    b.ToTable("DiscordRole");
                });

            modelBuilder.Entity("LiveBot.Core.Repository.Models.Streams.StreamSubscription", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<int?>("ChannelId")
                        .HasColumnType("integer");

                    b.Property<bool>("Deleted")
                        .HasColumnType("boolean");

                    b.Property<int?>("GuildId")
                        .HasColumnType("integer");

                    b.Property<string>("Message")
                        .HasColumnType("text");

                    b.Property<int?>("RoleId")
                        .HasColumnType("integer");

                    b.Property<int>("ServiceType")
                        .HasColumnType("integer");

                    b.Property<string>("SourceID")
                        .HasColumnType("text");

                    b.Property<DateTime>("TimeStamp")
                        .HasColumnType("timestamp without time zone");

                    b.HasKey("Id");

                    b.HasIndex("ChannelId");

                    b.HasIndex("GuildId");

                    b.HasIndex("RoleId");

                    b.ToTable("StreamSubscription");
                });

            modelBuilder.Entity("LiveBot.Core.Repository.Models.Discord.DiscordChannel", b =>
                {
                    b.HasOne("LiveBot.Core.Repository.Models.Discord.DiscordGuild", "DiscordGuild")
                        .WithMany("DiscordChannels")
                        .HasForeignKey("DiscordGuildId");
                });

            modelBuilder.Entity("LiveBot.Core.Repository.Models.Discord.DiscordRole", b =>
                {
                    b.HasOne("LiveBot.Core.Repository.Models.Discord.DiscordGuild", "DiscordGuild")
                        .WithMany("DiscordRoles")
                        .HasForeignKey("DiscordGuildId");
                });

            modelBuilder.Entity("LiveBot.Core.Repository.Models.Streams.StreamSubscription", b =>
                {
                    b.HasOne("LiveBot.Core.Repository.Models.Discord.DiscordChannel", "Channel")
                        .WithMany()
                        .HasForeignKey("ChannelId");

                    b.HasOne("LiveBot.Core.Repository.Models.Discord.DiscordGuild", "Guild")
                        .WithMany()
                        .HasForeignKey("GuildId");

                    b.HasOne("LiveBot.Core.Repository.Models.Discord.DiscordRole", "Role")
                        .WithMany()
                        .HasForeignKey("RoleId");
                });
#pragma warning restore 612, 618
        }
    }
}
