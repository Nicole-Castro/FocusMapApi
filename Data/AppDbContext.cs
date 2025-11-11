using System;
using FocusMapApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FocusMapApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<UsersModel> professionals { get; set; }
    public DbSet<PatientModel> patients { get; set; }
    public DbSet<InterestPoints> points_of_interest { get; set; }
    public DbSet<Sessions> sessions { get; set; }
    public DbSet<SessionData> session_data { get; set; }
    public DbSet<AudioDescription> audio_description { get; set; }
}
