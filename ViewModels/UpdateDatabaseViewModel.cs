using System.ComponentModel.DataAnnotations;

namespace JapaneseLearningApp.ViewModels;

public class UpdateDatabaseViewModel
{
    [Display(Name = "Excel workbook (.xlsx)")]
    public IFormFile? Spreadsheet { get; set; }

    /// <summary>Shows onboarding hint whenever the vocabulary table is empty.</summary>
    public bool IsDatabaseEmpty { get; set; }
}
