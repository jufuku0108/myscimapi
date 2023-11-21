using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace MyScimAPI.Models
{
    public class ScimUser
    {
        public Guid ScimUserId { get; set; }
        public string Schemas { get; set; }
        public string ExternalId { get; set; }
        public string UserName { get; set; }
        public virtual ScimUserName Name { get; set; }
        public string DisplayName { get; set; }
        public string NickName { get; set; }
        public string ProfileUrl { get; set; }
        public string Title { get; set; }
        public string UserType { get; set; }
        public string PreferredLanguage { get; set; }
        public string Locale { get; set; }
        public string TimeZone { get; set; }
        public bool Active { get; set; }
        public string Password { get; set; }
        public virtual ICollection<ScimUserEmail> Emails { get; set; }
        public virtual ICollection<ScimUserPhoneNumber> PhoneNumbers { get; set; }
        public virtual ICollection<ScimUserIm> Ims { get; set; }
        public virtual ICollection<ScimUserPhoto> Photos { get; set; }
        public virtual ICollection<ScimUserAddress> Addresses { get; set; }
        public virtual ICollection<ScimUserGroup> Groups { get; set; }
        public virtual ICollection<ScimUserEntitlement> Entitlements { get; set; }
        public virtual ICollection<ScimUserRole> Roles { get; set; }
        public virtual ICollection<ScimUserX509Certificate> X509Certificates { get; set; }
        public virtual ScimUserEnterpriseUser EnterpriseUser { get; set; }
        public virtual ScimUserMeta Meta { get; set; }
        
        //public string ApplicationUserId { get; set; }
        //public virtual ApplicationUser ApplicationUser { get; set; }
    }

    public class ScimUserName
    {
        public int ScimUserNameId { get; set; }
        public string Formatted { get; set; }
        public string FamilyName { get; set; }
        public string GivenName { get; set; }
        public string MiddleName { get; set; }
        public string HonorificPrefix { get; set; }
        public string HonorificSuffix { get; set; }
        public Guid ScimUserId { get; set; }
        public virtual ScimUser ScimUser { get; set; }
    }


    public class ScimUserEmail
    {
        public int ScimUserEmailId { get; set; }
        public string Value { get; set; }
        public string Display { get; set; }
        public string Type { get; set; }
        public bool Primary { get; set; }
        public Guid ScimUserId { get; set; }
        public virtual ScimUser ScimUser { get; set; }
    }



    public class ScimUserPhoneNumber
    {
        public int ScimUserPhoneNumberId { get; set; }
        public string Value { get; set; }
        public string Display { get; set; }
        public string Type { get; set; }
        public bool Primary { get; set; }
        public Guid ScimUserId { get; set; }
        public virtual ScimUser ScimUser { get; set; }
    }

    public class ScimUserIm
    {
        public int ScimUserImId { get; set; }
        public string Value { get; set; }
        public string Display { get; set; }
        public string Type { get; set; }
        public bool Primary { get; set; }
        public Guid ScimUserId { get; set; }
        public virtual ScimUser ScimUser { get; set; }
    }

    public class ScimUserPhoto
    {
        public int ScimUserPhotoId { get; set; }
        public string Value { get; set; }
        public string Display { get; set; }
        public string Type { get; set; }
        public bool Primary { get; set; }
        public Guid ScimUserId { get; set; }
        public virtual ScimUser ScimUser { get; set; }
    }

    public class ScimUserAddress
    {
        public int ScimUserAddressId { get; set; }
        public string Formatted { get; set; }
        public string StreetAddress { get; set; }
        public string Locality { get; set; }
        public string Region { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
        public string Type { get; set; }
        public Guid ScimUserId { get; set; }
        public virtual ScimUser ScimUser { get; set; }
    }

    public class ScimUserGroup
    {
        public int ScimUserGroupId { get; set; }
        public string Value { get; set; }
        public string Ref { get; set; }
        public string Display { get; set; }
        public string Type { get; set; }
        public Guid ScimUserId { get; set; }
        public virtual ScimUser ScimUser { get; set; }
    }

    public class ScimUserEntitlement
    {
        public int ScimUserEntitlementId { get; set; }
        public string Value { get; set; }
        public string Display { get; set; }
        public string Type { get; set; }
        public bool Primary { get; set; }
        public Guid ScimUserId { get; set; }
        public virtual ScimUser ScimUser { get; set; }

    }

    public class ScimUserRole
    {
        public int ScimUserRoleId { get; set; }
        public string Value { get; set; }
        public string Display { get; set; }
        public string Type { get; set; }
        public bool Primary { get; set; }
        public Guid ScimUserId { get; set; }
        public virtual ScimUser ScimUser { get; set; }

    }

    public class ScimUserX509Certificate
    {
        public int ScimUserX509CertificateId { get; set; }
        public string Value { get; set; }
        public string Display { get; set; }
        public string Type { get; set; }
        public bool Primary { get; set; }
        public Guid ScimUserId { get; set; }
        public virtual ScimUser ScimUser { get; set; }
    }

    public class ScimUserEnterpriseUser
    {
        public int ScimUserEnterpriseUserId { get; set; }
        public string EmployeeNumber { get; set; }
        public string CostCenter { get; set; }
        public string Organization { get; set; }
        public string Division { get; set; }
        public string Department { get; set; }
        public virtual ScimUserManager Manager { get; set; }
        public Guid ScimUserId { get; set; }
        public virtual ScimUser ScimUser { get; set; }
    }

    public class ScimUserManager
    {
        public int ScimUserManagerId { get; set; }
        public string Value { get; set; }
        public string Ref { get; set; }
        public string DisplayName { get; set; }
        public int ScimUserEnterpriseUserId { get; set; }
        public virtual ScimUserEnterpriseUser ScimUserEnterpriseUser { get; set; }
    }
    public class ScimUserMeta
    {
        public int ScimUserMetaId { get; set; }
        public string ResourceType { get; set; }
        public DateTime Created { get; set; }
        public DateTime LastModified { get; set; }
        public string Version { get; set; }
        public string Location { get; set; }
        public Guid ScimUserId { get; set; }
        public virtual ScimUser ScimUser { get; set; }

    }

}
