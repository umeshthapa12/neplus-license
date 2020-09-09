using System;
using System.ComponentModel.DataAnnotations;
using Dapper;

namespace NeplusLicense.Entities
{
    public class RequestedVendors
    {
        [Key]
        public long      Id          { get; set; }

        public Guid?     ClientGuid  { get; set; }
        public string    Name        { get; set; }
        public string    ModuleName  { get; set; }
        public string    Email       { get; set; }
        public string    LicenseType { get; set; }
        public DateTime? CreatedOn   { get; set; }
        public DateTime? UpdatedOn   { get; set; }
    }
}