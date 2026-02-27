using BillingSuite.Application.DTOs;
using BillingSuite.Application.Results;
using BillingSuite.Domain.Entities;

namespace BillingSuite.Web.Models
{
    public class SettingsViewModel
    {
        public CompanySettings CompanySettings { get; set; } = new();
        public PagedResult<TaxSettingsDto> TaxSettings { get; set; } = new();
        public TaxSettingsDto NewTaxSetting { get; set; } = new();
    }
}