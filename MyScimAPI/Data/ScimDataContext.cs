using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using MyScimAPI.Models;

namespace MyScimAPI.Data
{
    public class ScimDataContext : DbContext
    {
        public ScimDataContext()
        {

        }

        public ScimDataContext(DbContextOptions<ScimDataContext> options)
            : base(options)
        {

        }

        public DbSet<StatisticsData> StatisticsData { get; set; }
        public DbSet<HttpObject> HttpObjects { get; set; }
        //public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<ScimUser> ScimUsers { get; set; }
        public DbSet<ScimUserName> ScimUserNames { get; set; }
        public DbSet<ScimUserEmail> ScimUserEmails { get; set; }
        public DbSet<ScimUserAddress> ScimUserAddresses { get; set; }
        public DbSet<ScimUserPhoneNumber> ScimUserPhoneNumbers { get; set; }
        public DbSet<ScimUserIm> ScimUserIms { get; set; }
        public DbSet<ScimUserPhoto> ScimUserPhotos { get; set; }
        public DbSet<ScimUserGroup> ScimUserGroups { get; set; }
        public DbSet<ScimUserEntitlement> ScimUserEntitlements { get; set; }
        public DbSet<ScimUserRole> ScimUserRoles { get; set; }
        public DbSet<ScimUserX509Certificate> ScimUserX509Certificates { get; set; }
        public DbSet<ScimUserEnterpriseUser> ScimUserEnterpriseUsers { get; set; }
        public DbSet<ScimUserManager> ScimUserManagers { get; set; }
        public DbSet<ScimUserMeta> ScimUserMetas { get; set; }

        public DbSet<ScimGroup> ScimGroups { get; set; }
        public DbSet<ScimGroupMember> ScimGroupMembers { get; set; }
        public DbSet<ScimGroupMeta> ScimGroupMetas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            modelBuilder.Entity<StatisticsData>()
                .ToTable("StatisticsData")
                .HasKey("StatisticsDataId");

            modelBuilder.Entity<StatisticsData>()
               .Property(c => c.DateTime)
               .HasColumnType("datetime2");

            modelBuilder.Entity<HttpObject>()
                .ToTable("HttpObject")
                .HasKey("HttpObjectId");


            modelBuilder.Entity<ScimUser>()
                .ToTable("ScimUser")
                .HasKey("ScimUserId");

            //modelBuilder.Entity<ScimUser>()
              //  .HasOne(s => s.ApplicationUser);


            modelBuilder.Entity<ScimUserName>()
                .ToTable("ScimUserName")
                .HasKey("ScimUserNameId");

            modelBuilder.Entity<ScimUserName>()
                .HasOne(s => s.ScimUser);


            modelBuilder.Entity<ScimUserEmail>()
                .ToTable("ScimUserEmail")
                .HasKey("ScimUserEmailId");

            modelBuilder.Entity<ScimUserEmail>()
                .HasOne(s => s.ScimUser);


            modelBuilder.Entity<ScimUserAddress>()
                .ToTable("ScimUserAddress")
                .HasKey("ScimUserAddressId");

            modelBuilder.Entity<ScimUserAddress>()
                .HasOne(s => s.ScimUser);


            modelBuilder.Entity<ScimUserPhoneNumber>()
                .ToTable("ScimUserPhoneNumber")
                .HasKey("ScimUserPhoneNumberId");

            modelBuilder.Entity<ScimUserPhoneNumber>()
                .HasOne(s => s.ScimUser);


            modelBuilder.Entity<ScimUserIm>()
                .ToTable("ScimUserIm")
                .HasKey("ScimUserImId");

            modelBuilder.Entity<ScimUserIm>()
                .HasOne(s => s.ScimUser);


            modelBuilder.Entity<ScimUserPhoto>()
                .ToTable("ScimUserPhoto")
                .HasKey("ScimUserPhotoId");

            modelBuilder.Entity<ScimUserPhoto>()
                .HasOne(s => s.ScimUser);


            modelBuilder.Entity<ScimUserGroup>()
                .ToTable("ScimUserGroup")
                .HasKey("ScimUserGroupId");

            modelBuilder.Entity<ScimUserGroup>()
                .HasOne(s => s.ScimUser);


            modelBuilder.Entity<ScimUserEntitlement>()
                .ToTable("ScimUserEntitlement")
                .HasKey("ScimUserEntitlementId");

            modelBuilder.Entity<ScimUserEntitlement>()
                .HasOne(s => s.ScimUser);


            modelBuilder.Entity<ScimUserRole>()
                .ToTable("ScimUserRole")
                .HasKey("ScimUserRoleId");

            modelBuilder.Entity<ScimUserRole>()
                .HasOne(s => s.ScimUser);


            modelBuilder.Entity<ScimUserX509Certificate>()
                .ToTable("ScimUserX509Certificate")
                .HasKey("ScimUserX509CertificateId"); 

            modelBuilder.Entity<ScimUserX509Certificate>()
                .HasOne(s => s.ScimUser);


            modelBuilder.Entity<ScimUserEnterpriseUser>()
                .ToTable("ScimUserEnterpriseUser")
                .HasKey("ScimUserEnterpriseUserId");

            modelBuilder.Entity<ScimUserEnterpriseUser>()
                .HasOne(s => s.ScimUser);


            modelBuilder.Entity<ScimUserManager>()
                .ToTable("ScimUserManager")
                .HasKey("ScimUserManagerId");

            modelBuilder.Entity<ScimUserManager>()
                .HasOne(s => s.ScimUserEnterpriseUser);


            modelBuilder.Entity<ScimUserMeta>()
                .ToTable("ScimUserMeta")
                .HasKey("ScimUserMetaId");

            modelBuilder.Entity<ScimUserMeta>()
                .HasOne(s => s.ScimUser);



            modelBuilder.Entity<ScimGroup>()
                .ToTable("ScimGroup")
                .HasKey("ScimGroupId");

            modelBuilder.Entity<ScimGroupMember>()
                .ToTable("ScimGroupMember")
                .HasKey("ScimGroupMemberId");

            modelBuilder.Entity<ScimGroupMember>()
                .HasOne(s => s.ScimGroup);

            modelBuilder.Entity<ScimGroupMeta>()
                .ToTable("ScimGroupMeta")
                .HasKey("ScimGroupMetaId");

            modelBuilder.Entity<ScimGroupMeta>()
                .HasOne(s => s.ScimGroup);
        }
    }
}
